using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LitJson;
using NoidDominos.Properties;
using UnityEngine.Networking;

namespace DominoSharp
{
    /// <summary>
    /// The core interface to our payment API.
    /// </summary>
    public class Order
    {
        #region Enums
        /// <summary>
        /// The two delivery methods for an Order: Carryout and Delivery.
        /// </summary>
        public enum deliveryMethod
        {
            /// <summary>
            /// The Enum for Deliver
            /// </summary>
            Delivery = 0,
            /// <summary>
            /// The enum for carryout
            /// </summary>
            Carryout = 1
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Order object based on the Store, Customer, and Country
        /// </summary>
        /// <param name="store">The store this order takes place at</param>
        /// <param name="customer">The customer placing the order</param>
        /// <param name="country">The country this order is taking place in</param>
        public Order(Store store, Customer customer) : this(store, customer, store.getMenu())
        {
        }

        public Order(Store store, Customer customer, Menu menu)
        {
            this.store = store;
            this.menu = menu;
            this.customer = customer;
            this.address = customer.address;

            data = JsonMapper.ToObject(Encoding.UTF8.GetString(Resources.order));
            data["Address"]["Street"] = address.Street;
            data["Address"]["City"] = address.City;
            data["Address"]["Region"] = address.Region;
            data["Address"]["PostalCode"] = address.ZIP;

            data["StoreID"] = store.ID;
            data["Email"] = customer.email;
            data["FirstName"] = customer.firstName;
            data["LastName"] = customer.lastName;
            data["Phone"] = customer.phoneNumber;
        }
        #endregion

        #region Functions
        #region Item / Coupon Handling

        #region Add Item
        /// <summary>
        /// Adds an item to the products to order given an Item Code and Quantity
        /// </summary>
        /// <param name="itemCode">The ItemCode to order </param>
        /// <param name="quantity">The quantity of the Item we're ordering</param>
        public void addItem(string itemCode, int quantity)
        {
            JsonData item = new JsonData();
            item["Code"] = menu.variants[itemCode]["Code"];
            item["ID"] = 1;
            item["isNew"] = true;
            item["qty"] = quantity;
            item["AutoRemove"] = false;
            data["Products"].Add(item);
        }

        /// <summary>
        /// Does the same function as addItem() but takes arrays
        /// The 'quantity' property can be null if you want to avoid having a lengthy array of integers, the default value will be one.
        /// </summary>
        /// <param name="itemCodes">An array of the item codes we're adding</param>
        /// <param name="quantity">An array of the quantity of the items we're ordering</param>
        /*public void addItems(string[] itemCodes, int[] quantity)
        {
            JArray products = JArray.Parse(data.GetValue("Products").ToString());
            for (int i = 0; i < itemCodes.Length; i++)
            {
                JToken item = menu.variants[itemCodes[i]];
                item["ID"] = 1;
                item["isNew"] = true;
                item["qty"] = quantity[i];
                item["AutoRemove"] = false;
                products.AddFirst(item);
            }
            data["Products"] = products;
        }*/
        #endregion

        #region Remove Item
        /// <summary>
        /// Removes an item from the products to order given an item code.
        /// </summary>
        /// <param name="itemCode">The item code to remove.</param>
        public void removeItem(string itemCode)
        {
            var products = data["Products"];
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i]["Code"].GetString() == itemCode)
                {
                    products.RemoveAt(i);
                }
            }
            data["Products"] = products;
        }

        /// <summary>
        /// The same function as removeItem() but given a string[].
        /// </summary>
        /// <param name="itemCodes"></param>
        public void removeItems(string[] itemCodes)
        {
            JsonData products = data["Products"];
            foreach (string itemCode in itemCodes)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    if (products[i]["Code"].GetString() == itemCode)
                    {
                        products.RemoveAt(i);
                    }
                }
            }
            data["Products"] = products;
        }
        #endregion

        #region Add Coupon
        /// <summary>
        /// Adds a coupon to the current order
        /// </summary>
        /// <param name="coupon">The coupon to add</param>
        public void addCoupon(Coupon coupon)
        {
            JsonData token = menu.coupons[coupon.code];
            token["ID"] = 1;
            token["isNew"] = true;
            token["qty"] = coupon.quantity;
            token["AutoRemove"] = false;

            var coupons = data["Coupons"];
            coupons.Add(token);
            data["Coupons"] = coupons;
        }

        /// <summary>
        /// Adds a List of Coupons onto the order
        /// </summary>
        /// <param name="coupons">The list of coupons to add</param>
        public void addCoupons(Coupon[] coupons)
        {
            var originalCoupons = data["Coupons"];
            for (int i = 0; i < coupons.Length; i++)
            {
                var coupon = menu.coupons[coupons[i].code];
                coupon["ID"] = 1;
                coupon["isNew"] = true;
                coupon["qty"] = coupons[i].quantity;
                coupon["AutoRemove"] = false;
                originalCoupons.Add(coupon);
            }
            data["Coupons"] = originalCoupons;
        }
        #endregion

        #region Remove Coupon

        /// <summary>
        /// Removes the coupon from the Order.
        /// </summary>
        /// <param name="coupon">The coupon to remove</param>
        public void removeCoupon(Coupon coupon)
        {
            JsonData coupons = data["Coupons"];
            for (int i = 0; i < coupons.Count; i++)
            {
                if (coupons[i]["Code"].GetString() == coupon.code)
                {
                    coupons.RemoveAt(i);
                }
            }
            data["Coupons"] = coupons;
        }

        /// <summary>
        /// Removes all coupons from the coupon[] from the Order
        /// </summary>
        /// <param name="coupons">The coupons to remove</param>
        public void removeCoupons(Coupon[] coupons)
        {
            JsonData originalCoupons = data["Coupons"];
            Debug.Assert(originalCoupons.GetJsonType() == JsonType.Array);

            foreach (Coupon c in coupons)
            {
                for (int i = 0; i < originalCoupons.Count; i++)
                {
                    if (originalCoupons[i]["Code"].ToString() == c.code)
                    {
                        originalCoupons.RemoveAt(i);
                    }
                }
            }
            data["Coupons"] = originalCoupons;
        }

        #endregion

        /// <summary>
        /// Returns the items currently ordered in this Order
        /// </summary>
        /// <returns>The items currently ordered in this Order</returns>
        public JsonData itemsCurrentlyOrdered()
        {
            return data["Products"];
        }

        /// <summary>
        /// Returns the coupons currently applied to this order
        /// </summary>
        /// <returns>The coupons currently applied to this order</returns>
        public JsonData couponsCurrentlyApplied()
        {
            return data["Coupons"];
        }

        #endregion

        /// <summary>
        /// POST data onto a given URL
        /// </summary>
        /// <param name="url">The URL to POST to</param>
        /// <param name="merge">If we want to merge data</param>
        private JsonData send(string url, bool merge)
        {
            JsonData jsonToReturn = null;
            JsonData jObject = new JsonData();
            jObject["Order"] = data;

            string replacedData = Regex.Replace(jObject.ToJson(), "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            string response;

            #region HTTP Handling
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(replacedData);
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("referer", "https://order.dominos.ca/en/pages/order/");
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                var resp = www.SendWebRequest();
                while (!resp.webRequest.isDone)
                {
                    System.Threading.Thread.Sleep(10);
                }

                if (resp.webRequest.responseCode != 200)
                {
                    var failed = new JsonData();
                    failed["Status"] = "-1";
                    return failed;
                }
                response = www.downloadHandler.text;
            }
            #endregion

            jsonToReturn = JsonMapper.ToObject(response);

            return jsonToReturn;
        }

        /// <summary>
        /// Checks the validation of an order
        /// </summary>
        /// <returns>If the order is valid</returns>
        public bool validateOrder(JsonData validation)
        {
            ValidationResponse = validation;
            return ValidationResponse["Status"].GetNatural() != -1;
        }

        /// <summary>
        /// Checks the price of an order
        /// </summary>
        /// <returns>If the order price is valid</returns>
        public bool priceOrder(JsonData priceResponse)
        {
            PriceResponse = priceResponse;
            return ValidationResponse["Status"].GetNatural() != -1;
        }

        /// <summary>
        /// This *hopefully* places an Order to Dominos.
        /// Not really sure if this works, not really going to pay. 
        /// This requires testing.
        /// </summary>
        /// <param name="creditCard">The credit card one is paying with. null if paying in cash.</param>
        public JsonData placeOrder(Payment.CreditCard creditCard)
        {
            if (creditCard.cardType == Payment.CreditCard.CreditCardType.MAX)
            {
                throw new Exception("Credit Card is not a valid type!");
            }
            //if (creditCard == null) payWith();
            //else payWith(creditCard);
            payWith();
            JsonData response = send(URLs.placeURL(store.country), false);

            if (response["Status"].GetString() == "-1")
                throw new Exception("Dominos returned -1 due to order being, \"" + errorReason(response["Order"]) + "\" | Response: " + response.ToString());

            return response;
        }

        #region Pay With
        /// <summary>
        /// Returns the price of the current Order combined. 
        /// Use this instead of place() when testing
        /// </summary>
        public JsonData payWith()
        {

            // Get the price to check that everything worked okay
            JsonData response = send(URLs.priceURL(store.country), true);
            // Throw an exception if we messed up.
            if (response["Status"].GetNatural() == -1)
                throw new Exception("Dominos returned -1 due to order being, \"" + errorReason(response["Order"]) + "\" | Response: " + response.ToString());

            //data["Payments"][0]["Type"] = "Cash";
            return response;
        }

        /// <summary>
        /// Returns the price of the current Order combined. 
        /// Use this instead of place() when testing.
        /// </summary>
        /// <param name="card">The Payment.CreditCard object to pay with.</param>
        /*
        public JObject payWith(Payment.CreditCard card)
        {
            // Get the price to check that everything worked okay
            JObject response = send(URLs.priceURL(store.country), true);

            // Throw an exception if we messed up.
            if (response["Status"].ToString() == "-1")
                throw new Exception("Dominos returned -1 due to order being, \"" + errorReason(response["Order"]) + "\" | Response: " + response.ToString());

            data["Payments"] = new JArray
            {
                new JObject
                {
                    {"Type", "CreditCard"},
                    {"Expiration",  card.expirationDate},
                    {"Amount", double.Parse(response["Order"]["Amounts"]["Customer"].ToString()) },
                    {"CardType", card.cardType.ToString().ToUpper() },
                    {"Number", long.Parse(card.number) },
                    {"SecurityCode", long.Parse(card.cvv) },
                    {"PostalCode", long.Parse(card.zip) }
                }
            };

            return response;
        }
        */

        #endregion
        #endregion

        #region Properties
        /// <summary>
        /// The store of our order
        /// </summary>
        public Store store { get; }
        /// <summary>
        /// The customer buying the order
        /// </summary>
        public Customer customer { get; }
        /// <summary>
        /// The address of the customer.
        /// </summary>
        public Address address { get; }
        /// <summary>
        /// The data of our order.
        /// </summary>
        public JsonData data { get; }

        public JsonData ValidationResponse { get; private set; }

        public JsonData PriceResponse { get; private set; }
        
        /// <summary>
        /// The Menu associated with the store / their order
        /// </summary>
        public Menu menu { get; }
        #endregion

        #region Overrides
        /// <summary>
        /// A string representation of the Order
        /// </summary>
        /// <returns>A string representation of the Order</returns>
        public override string ToString()
        {
            return string.Format("An order for {0} {1} with {2} items in it",
                customer.firstName,
                customer.lastName,
                data["Products"].ToString().Count(f => f == '\n'));
        }
        #endregion

        #region Error Helpers
        private string errorReason(JsonData data)
        {
            try
            {
                var statusItems = data["StatusItems"];
                if (statusItems.Count > 2)
                {
                    JsonData lastCode = statusItems[statusItems.Count-1];
                    string code = (lastCode["Code"]).GetString();
                    return code;
                }

                return "None";
            }
            catch (Exception) { }

            return "";
        }
        #endregion
    }
}
