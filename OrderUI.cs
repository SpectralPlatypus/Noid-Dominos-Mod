using DominoSharp;
using LitJson;
using Pepperoni;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Logger = Pepperoni.Logger;

namespace NoidDominos
{
    class OrderUI : MonoBehaviour
    {
        /// <summary>
        /// Utility class for HTTP Requests via coroutines
        /// </summary>
        class JsonCoroutine
        {
            public JsonData Response { get; private set; }
            public JsonCoroutine() { }

            public IEnumerator Post(string url, string referer, JsonData jObject)
            {
                string replacedData = Regex.Replace(jObject.ToJson(), "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                string response;

                #region HTTP Handling
                using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(replacedData);
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("referer", referer);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    yield return www.SendWebRequest();

                    if (www.responseCode != 200)
                    {
                        Response = new JsonData();
                        Response["Status"] = "-1";
                        yield break;
                    }
                    response = www.downloadHandler.text;
                }
                #endregion

                Response = JsonMapper.ToObject(response);
            }

            public IEnumerator Get(string url)
            {
                using (var uwr = UnityWebRequest.Get(url))
                {
                    yield return uwr.SendWebRequest();
                    string responseString = uwr.downloadHandler.text;
                    Response = JsonMapper.ToObject(responseString);
                }
            }

            public JsonData GetTracker(string url, URLs.Country country)
            {
                using (var uwr = UnityWebRequest.Get(url))
                {
                    uwr.SetRequestHeader("Accept", "application/json");
                    uwr.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                    uwr.SetRequestHeader("DPZ-Language", "en");
                    uwr.SetRequestHeader("DPZ-Market", country == URLs.Country.USA ? "UNITED_STATES" : "CANADA");
                    var x = uwr.SendWebRequest();
                    while (!x.isDone) System.Threading.Thread.Sleep(5);

                    Logger.Log(uwr.responseCode);
                    if (uwr.responseCode != 200)
                    {
                        var ret = new JsonData();
                        ret["Status"] = -1;
                        return ret;
                    }

                    string responseString = uwr.downloadHandler.text;
                    return JsonMapper.ToObject(responseString);
                }
            }
        }

        enum State
        {
            /// <summary>
            /// Early dialog 
            /// </summary>
            IDLE = 0,
            /// <summary>
            ///  UI sequence for User Info
            /// </summary>
            INFO,
            /// <summary>
            /// Webrequest for store info
            /// </summary>
            INFO_DONE,
            /// <summary>
            /// Store info follow-up
            /// </summary>
            DIAL1,
            /// <summary>
            /// UI Sequence for Order Selection
            /// </summary>
            SELECTION,
            /// <summary>
            /// Webrequest for order validation
            /// </summary>
            SELECTION_DONE,
            /// <summary>
            /// Order response follow-up
            /// </summary>
            DIAL2,
            /// <summary>
            ///  Waiting for player to order
            /// </summary>
            ORDER_PENDING,
            /// <summary>
            /// Webrequest for the order
            /// </summary>
            ORDER_IN_PROGRESS,
            /// <summary>
            /// Tracking dialog after a successful order
            /// </summary>
            TRACKING,
            /// <summary>
            /// Something went wrong
            /// </summary>
            FAILED
        }
        State OrderState { get; set; } = State.IDLE;

        Font maru;
        Dictionary<string, TextAsset> presetDialogs = new Dictionary<string, TextAsset>();

        Order order = null;
        Payment.PaymentType paymentType;
        List<Menu.MenuItem> pizzas = null;
        string[] pizzaNames = null;

        static Dictionary<string, string> TRACKING_STATES = new Dictionary<string, string>
        {
            {"makeline", "Being Prepared" },
            {"oven", "In the oven" },
            {"routing station", "Waiting for Delivery" },
            {"out the door", "Out for Delivery" },
            {"complete", "Delivered" },
            {"bad", "Invalid or Cancelled Order." }
        }; 


        private readonly Vector3 NPC_POS = new Vector3(789.7f, 63.1f, 453.2f);
        private readonly float NPC_DIST = 10.0f;

        bool pizzaKnown = false;

        void Awake()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(
                 System.IO.Path.Combine(Application.dataPath, @"Managed\Mods\Assets\dominosmod"));
            maru = bundle.LoadAsset<Font>("ftmaru400a");

            foreach (var txt in bundle.LoadAllAssets<TextAsset>())
            {
                Logger.Log(txt.name);
                presetDialogs.Add(txt.name, txt);
            }

            ModHooks.Instance.OnParseScriptHook += Instance_OnParseScriptHook;
            On.DialogueSystem.End += DialogueSystem_End;
            On.Kueido.FixedUpdate += Kueido_FixedUpdate;
        }

        // Prevent Movement during UI and Callback Sections
        private void Kueido_FixedUpdate(On.Kueido.orig_FixedUpdate orig, Kueido self)
        {
            if (OrderState == State.INFO || 
                OrderState == State.INFO_DONE || 
                OrderState == State.SELECTION || 
                OrderState == State.SELECTION_DONE)
                return;

            orig(self);
        }

        private void DialogueSystem_End(On.DialogueSystem.orig_End orig, DialogueSystem self)
        {
            orig(self);

            switch (OrderState)
            {
                case State.IDLE:
                    {
                        var playerPos = Manager.Player.GetComponent<PlayerMachine>().transform.position;
                        if (SceneManager.GetActiveScene().name == "void" && Vector3.Distance(NPC_POS, playerPos) <= NPC_DIST)
                        {
                            if (!pizzaKnown)
                            {
                                pizzaKnown = true;
                                return;
                            }
                            OrderState = State.INFO;
                        }
                    }
                    break;
                case State.DIAL1: OrderState = State.SELECTION; break;
                case State.DIAL2: OrderState = State.ORDER_PENDING; break;
                case State.ORDER_IN_PROGRESS:
                    // TODO: Prevent dialogue while order coroutine is running to avoid double orders
                    StartCoroutine(PlaceOrder());
                    break;
            }
        }

        private string Instance_OnParseScriptHook(string text)
        {
            if (OrderState == State.IDLE || OrderState == State.ORDER_PENDING || OrderState == State.TRACKING)
            {
                if (SceneManager.GetActiveScene().name != "void" || !text.Contains("Oleia"))
                {
                    return text;
                }

                var playerPos = Manager.Player.GetComponent<PlayerMachine>().transform.position;
                if (Vector3.Distance(NPC_POS, playerPos) > NPC_DIST)
                    return text;

                if (OrderState == State.IDLE)
                {
                    if (!pizzaKnown)
                    {
                        return presetDialogs["PizzaIntro"].text;
                    }

                    return presetDialogs["PizzaIntro2"].text;
                }
                else if (OrderState == State.ORDER_PENDING)
                {
                    OrderState = State.ORDER_IN_PROGRESS;
                    return "%n10%v0%\r\nBirb\r\n%m1%Placing your order now...\r\n\r\n%n";
                }
                else
                {
                    var reply = GetTrackerStatus();
                    var lowerReply = reply.ToLower();
                    if (TRACKING_STATES.ContainsKey(lowerReply))
                    {
                        if (lowerReply == "routing station" && order.address.delivery == Address.DeliveryMethod.Takeout)
                        {
                            reply = "Ready for Pickup";
                        }
                        else
                            reply = TRACKING_STATES[lowerReply];
                    }
                    text = $"%n10%v0%\r\nBirb\r\n%m1%Your order's status is: {reply}\r\n\r\n%n";
                    return text;
                }
            }
            else if (text == "")
            {
                switch (OrderState)
                {
                    case State.DIAL1:
                        if (order != null)
                        {
                            string storeAddr = order.store.data["AddressDescription"].GetString();
                            storeAddr = storeAddr.Substring(0, storeAddr.IndexOf('\n')).Trim();

                            text += $"%n10%v0%\r\nBirb\r\n%m1%Looks like there is a store at %m0%%s.5%%m1%%sD%{storeAddr}.\r\n\r\n" +
                              "%r10%v0%\r\nBirb\r\n%m1%They sure have a lot of pizzas. So, what will it be?\r\n\r\n%n";
                        }
                        break;
                    case State.DIAL2:
                        text = $"%n10%v0%\r\nBirb\r\n%m1%Your order comes to " +
                            $"${order.PriceResponse["Order"]["Amounts"]["Payment"].GetReal()}.\r\n\r\n" +
                            "%n2%v8%\r\nNoid\r\n%m1%How are you communicating with the store?\r\n\r\n" +
                            "%n10%v0%\r\nBirb\r\n%s.3%...\r\n\r\n" +
                            "%r10%v0%\r\nBirb\r\n%m1%Let me know when you are ready to place that order.\r\n\r\n%n";
                        break;
                }
            }
            return text;
        }

        private IEnumerator FindStore()
        {
            URLs.Country country;
            if (Regex.IsMatch(zip, @"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z]\d[ABCEGHJ-NPRSTV-Z]\d$", RegexOptions.IgnoreCase))
                country = URLs.Country.CANADA;
            else if (Regex.IsMatch(zip, @"^\d{5}$"))
                country = URLs.Country.USA;
            else
            {
                Logger.LogError("Invalid zip/postal code");
                InitDialogue(presetDialogs["AddressError"]);
                OrderState = State.FAILED;
                yield break;
            }

            var deliveryMethod =
                selected == 0 ? Address.DeliveryMethod.Delivery : Address.DeliveryMethod.Takeout;

            // Resolve the payment type here
            if (deliveryMethod == Address.DeliveryMethod.Delivery)
            {
                if (paymentIndex <= 2 || paymentIndex >= 0)
                {
                    paymentType = (Payment.PaymentType)paymentIndex;
                }
            }
            else
                paymentType = Payment.PaymentType.Cash;

            var address = new Address(streetAddress, city, state, zip, country, deliveryMethod);
            var customer = new Customer(firstName.Trim(), lastName.Trim(), address, email, phone);
            JsonCoroutine jsonCr = new JsonCoroutine();

            yield return jsonCr.Get(address.GetStoreURL());
            Store store = null;
            try
            {
                store = address.ParseGetNearestStores(jsonCr.Response, address.Country);
            }
            catch
            {
                OrderState = State.FAILED;
                InitDialogue(presetDialogs["StoreNotFound"]);
                yield break;
            }

            string url = URLs.menuURL(store.country).Replace("{store_id}", store.ID).Replace("{lang}", "en");
            yield return jsonCr.Get(url);

            // TODO: Check response "Status", non-zero code means error

            var menu = store.ParseGetMenu(jsonCr.Response);
            pizzas = menu.searchInMenu(x =>
            {
                bool output = false;
                if (x["Tags"]["DefaultToppings"].GetString() != string.Empty)
                {
                    var productCode = x["ProductCode"].GetString();
                    Logger.LogDebug(productCode);
                    output = menu.products[productCode]["ProductType"].GetString() == "Pizza";
                }
                return output;
            });
            List<string> pizzaList = new List<string>();
            foreach (var p in pizzas)
            {
                pizzaList.Add(p.name);
            }

            pizzaNames = pizzaList.ToArray();

            // Finally create the order that will be used from here on out
            order = new Order(store, customer, menu);
            OrderState = State.DIAL1;
            InitDialogue();
        }

        private IEnumerator GetPrice(int selectionIndex)
        {
            string pizzaChoice = pizzaNames[selectionIndex];
            var item = pizzas.Find(p => p.name.Equals(pizzaChoice, StringComparison.OrdinalIgnoreCase));

            Logger.Log($"Code is {item.code}");
            if (item == null)
            {
                throw new Exception("Something went wrong");
            }

            order.addItem(item.code, 1);

            JsonData jObject = new JsonData();
            jObject["Order"] = order.data;

            var coroutine = new JsonCoroutine();
            var country = order.store.country;
            yield return coroutine.Post(URLs.validateURL(country), URLs.refererURL(country), jObject);

            if (order.validateOrder(coroutine.Response))
            {

                yield return coroutine.Post(URLs.priceURL(country), URLs.refererURL(country), jObject);
                if (!order.priceOrder(coroutine.Response))
                {
                    InitDialogue(presetDialogs["PriceFailed"]);
                    OrderState = State.FAILED;
                }

                double price = coroutine.Response["Order"]["Amounts"]["Payment"].GetReal();
                Logger.LogDebug("Total is: $" + price);
            }
            else
            {
                InitDialogue(presetDialogs["PriceFailed"]);
                OrderState = State.FAILED;
            }

            OrderState = State.DIAL2;
            InitDialogue();
        }

        private IEnumerator PlaceOrder()
        {
            var jObject = order.PriceResponse;
            jObject["Order"]["Payments"].Add(Payment.GetPaymentJson(paymentType));

            var coroutine = new JsonCoroutine();
            var country = order.store.country;
            yield return coroutine.Post(URLs.placeURL(country), URLs.refererURL(country), jObject);

            if (coroutine.Response["Status"].GetNatural() != -1)
            {
                InitDialogue(presetDialogs["OrderSuccess"]);
                OrderState = State.TRACKING;
            }
            else
            {
                InitDialogue(presetDialogs["OrderFailed"]);
                OrderState = State.FAILED;
            }
        }

        public string GetTrackerStatus()
        {
            JsonCoroutine jsonCr = new JsonCoroutine();
            var country =  order.address.Country;
            string phone = order.customer.phoneNumber;

            JsonData resp = jsonCr.GetTracker(
                URLs.trackPhone(country).Replace("{phone}", phone),
                country);

            // Non-array or empty array response
            if (resp.GetJsonType() != JsonType.Array || resp.Count < 1)
            {
                Logger.LogDebug(resp.ToJson());
                return "Bad";
            }

            string orderUrl = resp[0]["Actions"]["Track"].GetString();
            Logger.Log(orderUrl);
            resp = jsonCr.GetTracker(URLs.trackBase(country) + orderUrl, country);
            Logger.Log(resp.ToJson());

            return resp["OrderStatus"].GetString();
        }

        private void InitDialogue()
        {
            // Dynamic text: OrderState will be used to infer the dialogue
            GameObject.FindGameObjectWithTag("Manager").GetComponent<DialogueSystem>().Begin(new TextAsset(), null);
        }

        private void InitDialogue(TextAsset asset)
        {
            GameObject.FindGameObjectWithTag("Manager").GetComponent<DialogueSystem>().Begin(asset, null);
        }

#if DEBUG
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Manager.Player.GetComponent<PlayerMachine>().transform.position = NPC_POS;
            }
        }
#endif

        #region IMGUI_Variables
        string firstName;// = "Pizza";
        string lastName;// = "President";
        string phone = " ";
        string email = " ";
        string streetAddress = " ";
        string city = " ";
        string state = " ";
        string zip = " ";
        int selected = 0;
        int paymentIndex = 0;
        Vector2 scrollPos = Vector2.zero;
        GUIStyle tintableText;
        #endregion

        void OnGUI()
        {
            const float INC_OFFSET = 0.07f;

            if (OrderState == State.INFO)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
                tintableText = new GUIStyle(GUI.skin.box);
                tintableText.normal.background = Texture2D.whiteTexture;
                tintableText.normal.textColor = Color.white;

                float offsetY = 0.30f;
                var origFont = GUI.skin.font;
                GUI.skin.font = maru;

                GUI.backgroundColor = new Color(0.48f, 0.33f, 0.36f, 0.8f);
                // Make a background box
                float bgMultip = selected == 0 ? 0.62f : 0.56f;
                GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.22f, Screen.width * 0.5f, Screen.height * bgMultip), "Enter your Address", tintableText);

                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Name:");
                firstName = GUI.TextField(new Rect(Screen.width * 0.35f, Screen.height * offsetY, 250, 50), firstName);

                GUI.Label(new Rect(Screen.width * 0.485f, Screen.height * offsetY, 500, 60), "Last Name:");
                lastName = GUI.TextField(new Rect(Screen.width * 0.58f, Screen.height * offsetY, 260, 50), lastName);

                offsetY += INC_OFFSET;
                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Email:");
                email = GUI.TextField(new Rect(Screen.width * 0.35f, Screen.height * offsetY, 300, 50), email);

                GUI.Label(new Rect(Screen.width * 0.52f, Screen.height * offsetY, 500, 60), "Phone:");
                phone = GUI.TextField(new Rect(Screen.width * 0.58f, Screen.height * offsetY, 260, 50), phone, 10);
                offsetY += INC_OFFSET;

                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Street:");
                streetAddress = GUI.TextField(new Rect(Screen.width * 0.35f, Screen.height * offsetY, 700, 50), streetAddress);
                offsetY += INC_OFFSET;
                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "City:");
                city = GUI.TextField(new Rect(Screen.width * 0.35f, Screen.height * offsetY, 700, 50), city);
                offsetY += INC_OFFSET;
                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Region:");
                state = GUI.TextField(new Rect(Screen.width * 0.35f, Screen.height * offsetY, 100, 50), state, 2);
                GUI.Label(new Rect(Screen.width * 0.5f, Screen.height * offsetY, 500, 60), "Postal Code:");
                zip = GUI.TextField(new Rect(Screen.width * 0.62f, Screen.height * offsetY, 180, 50), zip, 6);
                offsetY += INC_OFFSET;

                GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Method:");
                GUI.skin.button.normal.background = Texture2D.whiteTexture;
                selected = GUI.SelectionGrid(
                    new Rect(Screen.width * 0.35f, Screen.height * offsetY, 600, 46.0f), selected, new[] { "Delivery", "Takeout" }, 2);

                offsetY += INC_OFFSET;
                if (selected == 0)
                {
                    GUI.Label(new Rect(Screen.width * 0.28f, Screen.height * offsetY, 500, 60), "Payment:");
                    GUI.skin.button.normal.background = Texture2D.whiteTexture;
                    paymentIndex = GUI.SelectionGrid(
                        new Rect(Screen.width * 0.36f, Screen.height * offsetY, 600, 46.0f), paymentIndex, new[] { "Cash", "Debit", "Credit" }, 3);
                }

                // Make the second button.
                float btnMult = selected == 0 ? 0.78f : 0.72f;
                if (GUI.Button(new Rect(1920 * 0.5f - 90, 1080 * btnMult, 180, 50), "Submit"))
                {
                    // Not a very thorough check -- validate zip code later for dialogue
                    if (firstName != "" && lastName != "" && phone != "" && email != "" &&
                        streetAddress != "" && city != "" && state != "" && zip != "")
                    {
                        OrderState = State.INFO_DONE;
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                        GUI.skin.font = origFont;

                        StartCoroutine(FindStore());
                    }
                }
                GUI.skin.font = origFont;
            }
            else if (OrderState == State.SELECTION)
            {
                var origFont = GUI.skin.font;
                GUI.skin.font = maru;
                float heightfix = 46.0f * pizzaNames.Length;
                GUI.skin.button.active.background = Texture2D.whiteTexture;
                GUI.skin.button.active.textColor = Color.black;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
                GUI.backgroundColor = new Color(0.48f, 0.33f, 0.36f, 0.8f);
                GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f), "Select Your Pizza", tintableText);
                scrollPos = GUI.BeginScrollView(new Rect(Screen.width * 0.3f, Screen.height * 0.32f, 820, 400), scrollPos, new Rect(0, 0, 800, heightfix));
                selected = GUI.SelectionGrid(new Rect(0, 0, 800, heightfix), selected, pizzaNames, 1);
                GUI.EndScrollView();
                if (GUI.Button(new Rect(1920 * 0.5f - 90, 1080 * 0.7f, 180, 50), "Submit"))
                {
                    OrderState = State.SELECTION_DONE;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    GUI.skin.font = origFont;

                    StartCoroutine(GetPrice(selected));
                }
                GUI.skin.font = origFont;
            }
        }
    }
}

