using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Xml;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

namespace DominoSharp
{
    /// <summary>
    /// General utilites for gathering out stuff
    /// </summary>
    public class Utils
    {
        #region Functions
        /// <summary>
        /// Returns an HTTP GET Request on url as a JObject
        /// </summary>
        /// <param name="url">The URL to GET </param>
        /// <returns>an HTTP GET Request on url as a JObject</returns>
        public static JsonData request_JSON(string url)
        {
            //JsonUtility.FromJson<Store>(requestData(url));
            return JsonMapper.ToObject(requestData(url));
        }

        /// <summary>
        /// Returns an HTTP GET Request on URL as a JObject but the URL is for XML
        /// </summary>
        /// <param name="URL">An XML page</param>
        /// <returns>an HTTP GET Request on XML URL as a JObject</returns>
        public static JsonData request_XML(string URL)
        {
            // Get our XML data
            string XML = requestData(URL);
            // Remove some of the junk lines of XML.
            XML = string.Join(Environment.NewLine, XML.Split(Environment.NewLine.ToCharArray()).Skip(4).ToArray().Reverse().Skip(3).Reverse().ToArray());

            // Create a new XML doc to fill with our XML.
            XmlDocument doc = new XmlDocument();
            // Fill our XML
            doc.LoadXml(XML);
            // Convert the XML to JSON
            string JSON = "";//JsonConvert.SerializeXmlNode(doc);
            // Parse it as a JObject
            var defaultReader = new JsonReader(JSON);
            return JsonMapper.ToJson(defaultReader);
        }

        /// <summary>
        /// A general helper function to return a string of all data returned from an HTTP GET Request
        /// </summary>
        /// <param name="URL">The URL to get</param>
        /// <returns>A string of all data returned from an HTTP GET Request</returns>
        private static string requestData(string URL)
        {
           string responseString;

            using (var uwr = UnityWebRequest.Get(URL))
            {
                var wait =  uwr.SendWebRequest();
                while(!wait.isDone)
                {
                    System.Threading.Thread.Sleep(10);
                }
                // Logger.Log(uwr.responseCode);
                responseString = uwr.downloadHandler.text;
                //Logger.Log(responseContent);
            }
            return responseString;
        }
        #endregion

        #region Constants
        public const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
        public const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;
        #endregion
    }
}
