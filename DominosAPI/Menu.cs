using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DominoSharp
{
    /// <summary>
    /// A menu of all of our items gathered from our API.
    /// </summary>
    public class Menu
    {
        #region Classes
        /// <summary>
        /// An class for an item on our menu
        /// </summary>
        public class MenuItem
        {
            #region Constructors
            /// <summary>
            /// A generic constructor 
            /// </summary>
            public MenuItem()
            {
                obj = null;
                price = null;
                code = null;
                name = null;
            }

            /// <summary>
            /// Creates a new MenuItem based on the array param
            /// </summary>
            /// <param name="obj">The JObject based on the menu item</param>
            public MenuItem(JsonData obj)
            {
                this.obj = obj;

                this.price = obj["Price"].ToString();
                this.code = obj["Code"].ToString();
                this.name = obj["Name"].ToString();
            }
            #endregion

            #region Properties
            /// <summary>
            /// A JObject of all of our properties
            /// </summary>
            public JsonData obj { get; set; }
            /// <summary>
            /// The price of our current MenuItem
            /// </summary>
            public string price { get; set; }
            /// <summary>
            /// The Code for this menu item
            /// </summary>
            public string code { get; set; }
            /// <summary>
            /// The proper name of this menu item
            /// </summary>
            public string name { get; set; }

            #endregion

            #region Overrides
            /// <summary>
            /// Returns the code property of the menu item.
            /// </summary>
            /// <returns>The code property of the menu item.</returns>
            public override string ToString()
            {
                return code;
            }

            #endregion
        }

        #endregion

        #region Constructors
        /// <summary>
        /// A menu object based on our data
        /// </summary>
        /// <param name="data">A JObject of all of our data gathered</param>
        /// <param name="country">The country of our Store</param>
        public Menu(JsonData data, URLs.Country country)
        {
            this.data = data;
            this.variants = data["Variants"];
            this.products = data["Products"];
            this.coupons = data["Coupons"];
            this.preconfigured = data["PreconfiguredProducts"];
            this.country = country;
        }

        #endregion

        #region Functions
        /// <summary>
        /// Returns a dictionary of the CodeName (key) and the proper name (value)
        /// </summary>
        /// <param name="s">A string to search in the menu for</param>
        /// <returns></returns>
        public List<MenuItem> searchInMenu(string s)
        {
            List<MenuItem> searches = new List<MenuItem>();
            var variantEntries = variants.Keys;
            foreach (string entry in variantEntries)
            {
                // Get the variant like, B8PCPT, 20BCOKE, etc
                JsonData variant = variants[entry];
                // Case Insensitivity
                if (variant["Name"].ToString().ToLower().Contains(s.ToLower()))
                {
                    MenuItem item = new MenuItem(variant);
                    searches.Add(item);
                }
            }

            return searches;
        }

        public List<MenuItem> searchInMenu(Func<JsonData, bool> Predicate)
        {
            List<MenuItem> searches = new List<MenuItem>();
            var variantEntries = variants.Keys;
            foreach (string entry in variantEntries)
            {
                // Get the variant like, B8PCPT, 20BCOKE, etc
                JsonData variant = variants[entry];
                // Case Insensitivity
                if (Predicate.Invoke(variant))
                {
                    MenuItem item = new MenuItem(variant);
                    searches.Add(item);
                }
            }

            return searches;
        }

        #endregion

        #region Properties

        #region All of our arrays noting our possible products
        /// <summary>
        /// All of the variants of our menu
        /// </summary>
        public JsonData variants { get; set; }
        /// <summary>
        /// The products from our menu
        /// </summary>
        public JsonData products { get; set; }
        /// <summary>
        /// The coupons possible for our menu
        /// </summary>
        public JsonData coupons { get; set; }
        /// <summary>
        /// The preconfigured orders from our menu
        /// </summary>
        public JsonData preconfigured { get; set; }
        #endregion

        /// <summary>
        /// The JObject data to pull our information from
        /// </summary>
        public JsonData data { get; set; }
        /// <summary>
        /// The country our Store is located in.
        /// </summary>
        public URLs.Country country { get; set; }
        #endregion
    }
}
