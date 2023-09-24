using System.Net;
using System.Security.Authentication;
using LitJson;
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
            return JsonMapper.ToObject(requestData(url));
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
                responseString = uwr.downloadHandler.text;
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
