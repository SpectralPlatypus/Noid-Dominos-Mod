using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using LitJson;

namespace DominoSharp
{
    /// <summary>
    /// A class for an address
    /// </summary>
    public class Address
    {
        #region Constructors
        /// <summary>
        /// Creates a new Address object based on all of the parameters.
        /// </summary>
        /// <param name="street">The street of this address</param>
        /// <param name="city">The city of this address</param>
        /// <param name="region">The region of this address</param>
        /// <param name="zip">The ZIP Code of this address</param>
        /// <param name="country">The Country of this address</param>
        /// <param name="method">The DeliveryMethod for this Address</param>
        public Address(string street, string city, string region, string zip, URLs.Country country, DeliveryMethod method)
        {
            this.Street = street;
            this.City = city;
            this.Region = region;
            this.ZIP = zip;
            this.Country = country;
            this.delivery = method;
        }

        /// <summary>
        /// This function is generally more useful compared to Address(street, city...) due to the ease.
        /// </summary>
        /// <param name="combined">The combined name of the Address.</param>
        public Address(string combined)
        {
            this.Street = combined.Split(',')[0].Trim();
            // City naming mangling. Screw the US.
            if (Regex.Split(combined, "[0-9]{5}")[0].Count(x => x == ',') > 1)
            {
                this.City = Regex.Split(combined, "[0-9]{5}")[0].Split(',')[1].Trim() +
                            Regex.Split(combined, "[0-9]{5}")[0].Split(',')[2].Insert(0, ",").Trim();
            }
            else
            {
                this.City = Regex.Split(combined, "[0-9]{5}")[0].Split(',')[1].Trim();
            }
            this.ZIP = Regex.Matches(combined, "[0-9]{5}")[0].Value.Trim();
            this.Country = URLs.Country.USA;
            this.delivery = DeliveryMethod.Delivery;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Gets a List of Stores of the nearby currently open stores according to the Dominos API
        /// </summary>
        /// <returns>A List of Stores of the nearby stores according to the Dominos API</returns>
        public List<Store> getNearbyStores()
        {
            // Queries the nearest stores
            string properURL = URLs.findURL(Country).Replace("{street}", Street).Replace("{city}", $"{City},{Region}").Replace("{type}", delivery.ToString());

            // Get JObject from our JSON data gathered.
            JsonData data = Utils.request_JSON(properURL);

            List<Store> stores = new List<Store>();

            JsonData storeArray = data["Stores"];
            for (int i = 0; i < storeArray.Count; i++)
            {
                var store = storeArray[i];
                if (store["IsOnlineNow"].ToString() != "True" || 
                    store["ServiceIsOpen"][delivery.ToString()].ToString() != "True" || store == null)
                {
                    continue;
                }
                stores.Add(new Store(store["StoreID"].ToString(), store));
            }
            return stores;
        }


        public Store ParseGetNearestStores(JsonData data, URLs.Country country)
        {
            List<Store> stores = new List<Store>();

            JsonData storeArray = data["Stores"];
            for (int i = 0; i < storeArray.Count; i++)
            {
                var store = storeArray[i];
                if (store["IsOnlineNow"].ToString() != "True" ||
                    store["ServiceIsOpen"][delivery.ToString()].ToString() != "True" || store == null)
                {
                    continue;
                }
                stores.Add(new Store(store["StoreID"].ToString(), store, country));
            }
            if (stores.Count <= 0)
            {
                throw new Exception("No local stores are currently open!");
            }
            return stores.ElementAt(0);
        }

        public string GetStoreURL()
        {
            return URLs.findURL(Country).Replace("{street}", Street).Replace("{city}", $"{City},{Region}").Replace("{type}", delivery.ToString());
        }

        /// <summary>
        /// Returns the closest currently open store to the current Address.
        /// </summary>
        /// <returns>The closest currently tore to the current Address.</returns>
        public Store getNearestStore()
        {
            List<Store> stores = getNearbyStores();
            if (stores.Count <= 0)
            {
                throw new Exception("No local stores are currently open!");
            }
            return stores.ElementAt(0);
        }

        public enum DeliveryMethod
        {
            /// <summary>
            /// The Enum for Deliver
            /// </summary>
            Delivery = 0,
            /// <summary>
            /// The enum for carryout
            /// </summary>
            Takeout = 1
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Returns a string reperesentation of the address
        /// </summary>
        /// <returns>A string reperesentation of the address</returns>
        public override string ToString()
        {
            return (Street + ", " + City + ", " + Region + ", " + ZIP);
        }
        #endregion


        #region Properties
        /// <summary>
        /// The street of the Address
        /// </summary>
        public string Street { get; set; }
        /// <summary>
        /// The city of the address
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// The region of the address
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// The ZIP code of the address
        /// </summary>
        public string ZIP { get; set; }
        /// <summary>
        /// The country of the addres
        /// </summary>
        public URLs.Country Country { get; set; }
        /// <summary>
        /// The delivery method associated with the address
        /// </summary>
        public DeliveryMethod delivery { get; set; }
        #endregion
    }
}
