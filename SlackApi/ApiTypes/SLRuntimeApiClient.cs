using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using WebSocketSharp;

namespace Slack
{
    public class SLRuntimeApiClient
    {
        public event EventHandler<EventArgs> OnWebsocketOpened;
        public event EventHandler<SLRuntimeEventArgs> OnWebsocketMessage;
        public event EventHandler<SLExceptionEventArgs> OnWebsocketError;
        public event EventHandler<EventArgs> OnWebsocketClosed;
        private JavaScriptSerializer javascriptSerializer;
        private string authenticateResponse;
        private WebSocket websocket;
        private string websocketUrl;
        private string apiToken;
        private string proxyUrl;
        private string proxyUser;
        private string proxyPassword;

        public SLRuntimeApiClient(string apiToken)
        { 
            javascriptSerializer = new JavaScriptSerializer();
            this.apiToken = apiToken;
        }

        public SLRuntimeApiClient(string apiToken, string proxyUrl, string proxyUser, string proxyPassword) : this(apiToken)
        {
            this.proxyUrl = proxyUrl;
            this.proxyUser = proxyUser;
            this.proxyPassword = proxyPassword;
        }

        public bool IsOpen()
        { 
            if (websocket != null)
            {
                if (websocket.ReadyState == WebSocketState.Open)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsConnecting()
        {
            if (websocket != null)
            { 
                if (websocket.ReadyState == WebSocketState.Connecting)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Authenticate()
        {
            authenticateResponse = SendAuthenticateRequest();
            if (authenticateResponse != null)
            {
                GetWsUrlFromAuthenticateResponse(authenticateResponse, ref websocketUrl);
                try
                {
                    if (websocket != null)
                    {
                        websocket.OnOpen -= new EventHandler(websocket_Opened);
                        websocket.OnError -= new EventHandler<WebSocketSharp.ErrorEventArgs>(websocket_Error);
                        websocket.OnClose -= new EventHandler<WebSocketSharp.CloseEventArgs>(websocket_Closed);
                        websocket.OnMessage -= new EventHandler<WebSocketSharp.MessageEventArgs>(websocket_MessageReceived);
                        websocket = null;
                    }

                    websocket = new WebSocketSharp.WebSocket(websocketUrl);
                    websocket.SslConfiguration.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
                    websocket.OnOpen += new EventHandler(websocket_Opened);
                    websocket.OnError += new EventHandler<WebSocketSharp.ErrorEventArgs>(websocket_Error);
                    websocket.OnClose += new EventHandler<WebSocketSharp.CloseEventArgs>(websocket_Closed);
                    websocket.OnMessage += new EventHandler<WebSocketSharp.MessageEventArgs>(websocket_MessageReceived);
                    if ((proxyUrl != null) && (proxyUser != null) && (proxyPassword != null))
                    {
                        websocket.SetProxy(proxyUrl, proxyUser, proxyPassword);
                    }
                    websocket.Connect();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void Send(long id, string type, string channelId, string text, bool wrapWithConsoles = false)
        {
            if (wrapWithConsoles)
            {
                text = WrapWithConsoles(text);
            }

            text = SLNormalizer.Normalize(text);

            StringBuilder sb = new StringBuilder("");
            sb.AppendLine("{");
            sb.AppendLine("\"id\":" + id + ",");
            sb.AppendLine("\"type\":\"" + type + "\",");
            sb.AppendLine("\"channel\":\"" + channelId + "\",");
            sb.AppendLine("\"text\":\"" + text + "\"");
            sb.AppendLine("}");
            websocket.Send(sb.ToString());
        }

        public void SendWithMessageType(long id, string channelId, string text, bool splitLongMessages = false, bool wrapWithConsoles = false)
        {
            const int maxMessageLength = 1000;
            if (splitLongMessages)
            {
                int i = 0;
                while (text.Length > maxMessageLength)
                {
                    string part = text.Substring(i * maxMessageLength, maxMessageLength);
                    text = text.Substring(maxMessageLength, text.Length - maxMessageLength);
                    Send(id, "message", channelId, part, wrapWithConsoles);
                    i++;
                }
                Send(id, "message", channelId, text, wrapWithConsoles);
            }
            else
            {
                Send(id, "message", channelId, text, wrapWithConsoles);
            }
        }

        public string WrapWithConsoles(string src)
        {
            return "```" + src + "```";
        }

        private string SendAuthenticateRequest()
        {
            string textToSend = "";

            WebRequest request = WebRequest.Create("https://slack.com/api/rtm.start?token=" + apiToken);
            request.Method = "POST";
            byte[] bytes = Encoding.UTF8.GetBytes(textToSend);
            request.ContentLength = bytes.Length;

            WebResponse response = null;
            try
            {
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
                response = request.GetResponse();
            }
            catch (WebException ex)
            {
                return null;
            }
            if (response != null)
            {
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                string responseBody = streamReader.ReadToEnd().Trim();
                return responseBody;
            }
            return null;
        }

        private void GetWsUrlFromAuthenticateResponse(string authenticateResponse, ref string websocketUrl)
        {
            if (authenticateResponse != null)
            {
                Dictionary<string, object> values = javascriptSerializer.Deserialize<Dictionary<string, object>>(authenticateResponse);
                dynamic data = javascriptSerializer.Deserialize<dynamic>(authenticateResponse);
                websocketUrl = (string)data["url"];
                System.Diagnostics.Debug.WriteLine(websocketUrl);
            }
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            if (OnWebsocketOpened != null)
            {
                OnWebsocketOpened(this, e);
            }
        }

        private void websocket_MessageReceived(object sender, WebSocketSharp.MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Data);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(e.Data);

            try
            {
                SLRuntimeEventArgs message = new SLRuntimeEventArgs();
                bool convertionResult;
                message.ok = DictionaryExtension.TryGetValue(data, "ok", out convertionResult);
                message.type = DictionaryExtension.TryGetValue(data, "type", out convertionResult);
                message.reply_to = DictionaryExtension.TryGetValue(data, "reply_to", out convertionResult);
                message.channel = DictionaryExtension.TryGetValue(data, "channel", out convertionResult);
                message.user = DictionaryExtension.TryGetValue(data, "user", out convertionResult);
                message.text = DictionaryExtension.TryGetValue(data, "text", out convertionResult);
                message.ts = DictionaryExtension.TryGetValue(data, "ts", out convertionResult);
                message.team = DictionaryExtension.TryGetValue(data, "team", out convertionResult);
                if (OnWebsocketMessage != null)
                {
                    OnWebsocketMessage(this, message);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void websocket_Error(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            SLExceptionEventArgs args = new SLExceptionEventArgs();
            args.exception = e.Exception;
            args.message = e.Message;
            if (OnWebsocketError != null)
            {
                OnWebsocketError(this, args);
            }
        }

        private void websocket_Closed(object sender, WebSocketSharp.CloseEventArgs e)
        {
            if (OnWebsocketClosed != null)
            {
                OnWebsocketClosed(this, e);
            }
        }
    }
}
