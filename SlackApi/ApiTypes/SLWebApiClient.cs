using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Web.Script.Serialization;

namespace Slack
{
    public class SLWebApiClient
    {
        private string rootUrl;
        private string apiToken;
        private string proxyUrl;
        private JavaScriptSerializer javascriptSerializer;
 
        public SLWebApiClient(string apiToken, string proxyUrl = null)
        {
            rootUrl = "https://slack.com/api/";
            javascriptSerializer = new JavaScriptSerializer();
            this.apiToken = apiToken;
            this.proxyUrl = proxyUrl;
        }

        #region AuthTestAsync()
        public async Task<SLAuthTestResult> AuthTestAsync()
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "auth.test?token=", apiToken);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractAuthTestResult(data);
            }
            return new SLAuthTestResult();
        }
        private SLAuthTestResult ExtractAuthTestResult(dynamic data)
        {
            SLAuthTestResult authTestResult = new SLAuthTestResult();
            bool convertationResult = false;

            authTestResult.ok = DictionaryExtension.TryGetValue(data, "ok", out convertationResult);
            authTestResult.url = DictionaryExtension.TryGetValue(data, "url", out convertationResult);
            authTestResult.team_name = DictionaryExtension.TryGetValue(data, "team", out convertationResult);
            authTestResult.user_name = DictionaryExtension.TryGetValue(data, "user", out convertationResult);
            authTestResult.team_id = DictionaryExtension.TryGetValue(data, "team_id", out convertationResult);
            authTestResult.user_id = DictionaryExtension.TryGetValue(data, "user_id", out convertationResult);

            return authTestResult;
        }
        #endregion AuthTestAsync()

        #region ChatPostMessageAsync()
        public async Task<SLChatPostMessageResult> ChatPostMessageAsync(string channel_id, string text, bool asUser = false, bool parseFull = false, string attachments = "")
        {
            string userStr = "false";
            string parseFullStr = "none";
            if (asUser) userStr = "true";
            if (parseFull) parseFullStr = "full";

            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "chat.postMessage?token=", apiToken, "&channel=" + channel_id, "&text=" + text, "&as_user=" + userStr, "&parse=" + parseFullStr, "&attachments=" + attachments);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractChatPostMessageResult(data);
            }
            return new SLChatPostMessageResult();
        }

        private SLChatPostMessageResult ExtractChatPostMessageResult(dynamic data)
        {
            SLChatPostMessageResult slChatPostMessageResult = new SLChatPostMessageResult();
            bool convertationResult = false;

            slChatPostMessageResult.ok = DictionaryExtension.TryGetValue(data, "ok", out convertationResult);
            slChatPostMessageResult.channel_id = DictionaryExtension.TryGetValue(data, "channel", out convertationResult);
            slChatPostMessageResult.ts = DictionaryExtension.TryGetValue(data, "ts", out convertationResult);
            
            dynamic messageData = DictionaryExtension.TryGetValue(data, "message", out convertationResult);
            if (messageData != null)
            {
                slChatPostMessageResult.message.user_id = DictionaryExtension.TryGetValue(messageData, "user", out convertationResult);
                slChatPostMessageResult.message.type = DictionaryExtension.TryGetValue(messageData, "type", out convertationResult);
                slChatPostMessageResult.message.text = DictionaryExtension.TryGetValue(messageData, "text", out convertationResult);
                slChatPostMessageResult.message.ts = DictionaryExtension.TryGetValue(messageData, "ts", out convertationResult);
            }
            return slChatPostMessageResult;
        }
        #endregion ChatPostMessageAsync()

        #region ChannelsListAsync()
        public async Task<List<SLChannel>> ChannelsListAsync()
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "channels.list?token=", apiToken, "&exclude_archived=1");
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractChannelsList(data);
            }
            return new List<SLChannel>();
        }

        private List<SLChannel> ExtractChannelsList(dynamic data)
        {
            List<SLChannel> channelsList = new List<SLChannel>();
            bool convertationResult;

            dynamic groups = DictionaryExtension.TryGetValue(data, "channels", out convertationResult);
            foreach (dynamic item in groups)
            {
                SLChannel channel = new SLChannel();
                channel.id = DictionaryExtension.TryGetValue(item, "id", out convertationResult);
                channel.name = DictionaryExtension.TryGetValue(item, "name", out convertationResult);
                channel.is_channel = DictionaryExtension.TryGetValue(item, "is_channel", out convertationResult);
                channel.created = DictionaryExtension.TryGetValue(item, "created", out convertationResult);
                channel.creator_id = DictionaryExtension.TryGetValue(item, "creator", out convertationResult);
                channel.is_archived = DictionaryExtension.TryGetValue(item, "is_archived", out convertationResult);

                var membersData = DictionaryExtension.TryGetValue(item, "members", out convertationResult);
                if (membersData != null)
                {
                    foreach (string member_id in membersData)
                    {
                        channel.members_ids.Add(member_id);
                    }
                }

                var topicData = DictionaryExtension.TryGetValue(item, "topic", out convertationResult);
                if (topicData != null)
                {
                    channel.topic.creator_id = DictionaryExtension.TryGetValue(topicData, "creator", out convertationResult);
                    channel.topic.value = DictionaryExtension.TryGetValue(topicData, "value", out convertationResult);
                    channel.topic.last_set = DictionaryExtension.TryGetValue(topicData, "last_set", out convertationResult);
                }
                channelsList.Add(channel);
            }
            return channelsList;
        }
        #endregion ChannelsListAsync()

        #region ChannelsHistoryAsync()
        public async Task<List<SLMessage>> ChannelsHistoryAsync(string channel_id)
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "channels.history?token=", apiToken, "&channel=" + channel_id);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractChannelsHistory(data);
            }
            return new List<SLMessage>();
        }

        private List<SLMessage> ExtractChannelsHistory(dynamic data)
        {
            List<SLMessage> messageList = new List<SLMessage>();
            bool convertationResult = false;

            dynamic messages = DictionaryExtension.TryGetValue(data, "messages", out convertationResult);
            foreach (dynamic item in messages)
            {
                SLMessage message = new SLMessage();
                message.type = DictionaryExtension.TryGetValue(item, "type", out convertationResult);
                message.text = DictionaryExtension.TryGetValue(item, "text", out convertationResult);
                //TODO: message.file_description
                message.user_id = DictionaryExtension.TryGetValue(item, "user", out convertationResult);
                message.upload = DictionaryExtension.TryGetValue(item, "upload", out convertationResult);
                message.ts = DictionaryExtension.TryGetValue(item, "ts", out convertationResult);
                messageList.Add(message);
            }

            messageList.OrderBy(message => DateTimeHelper.DateTimeFromSLMessage(message)).ToList().Reverse();
            return messageList;
        }
        #endregion ChannelsHistoryAsync

        #region GroupsListAsync()
        public async Task<List<SLGroup>> GroupsListAsync()
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "groups.list?token=", apiToken, "&exclude_archived=1");
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractGroupsList(data);
            }
            return new List<SLGroup>();
        }

        private List<SLGroup> ExtractGroupsList(dynamic data)
        {
            List<SLGroup> groupList = new List<SLGroup>();
            bool convertationResult = false;

            dynamic groups = DictionaryExtension.TryGetValue(data, "groups", out convertationResult);
            foreach (dynamic item in groups)
            {
                SLGroup group = new SLGroup();
                group.id = DictionaryExtension.TryGetValue(item, "id", out convertationResult);
                group.name = DictionaryExtension.TryGetValue(item, "name", out convertationResult);
                group.is_group = DictionaryExtension.TryGetValue(item, "is_group", out convertationResult);
                group.created = DictionaryExtension.TryGetValue(item, "created", out convertationResult);
                group.creator_id = DictionaryExtension.TryGetValue(item, "creator", out convertationResult);
                group.is_archived = DictionaryExtension.TryGetValue(item, "is_archived", out convertationResult);

                var membersData = DictionaryExtension.TryGetValue(item, "members", out convertationResult);
                if (membersData != null)
                {
                    foreach (string member_id in membersData)
                    {
                        group.members_ids.Add(member_id);
                    }
                }

                var topicData = DictionaryExtension.TryGetValue(item, "topic", out convertationResult);
                if (topicData != null)
                {
                    group.topic.creator_id = DictionaryExtension.TryGetValue(topicData, "creator", out convertationResult);
                    group.topic.value = DictionaryExtension.TryGetValue(topicData, "value", out convertationResult);
                    group.topic.last_set = DictionaryExtension.TryGetValue(topicData, "last_set", out convertationResult);
                }
                groupList.Add(group);
            }
            return groupList;
        }
        #endregion GroupsListAsync()

        #region GroupsHistoryAsync()
        public async Task<List<SLMessage>> GroupsHistoryAsync(string channel_id)
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "groups.history?token=", apiToken, "&channel=" + channel_id);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractGroupsHistory(data);
            }
            return new List<SLMessage>();
        }

        private List<SLMessage> ExtractGroupsHistory(dynamic data)
        {
            List<SLMessage> messageList = new List<SLMessage>();
            bool convertationResult = false;

            dynamic messages = DictionaryExtension.TryGetValue(data, "messages", out convertationResult);
            foreach (dynamic item in messages)
            {
                SLMessage message = new SLMessage();
                message.type = DictionaryExtension.TryGetValue(item, "type", out convertationResult);
                message.text = DictionaryExtension.TryGetValue(item, "text", out convertationResult);

                var fileData = DictionaryExtension.TryGetValue(item, "file", out convertationResult);
                if (fileData != null)
                {
                    message.file_description.id = DictionaryExtension.TryGetValue(fileData, "id", out convertationResult);
                    message.file_description.timestamp = DictionaryExtension.TryGetValue(fileData, "timestamp", out convertationResult);
                    message.file_description.name = DictionaryExtension.TryGetValue(fileData, "name", out convertationResult);
                    message.file_description.title = DictionaryExtension.TryGetValue(fileData, "title", out convertationResult);
                    message.file_description.mimetype = DictionaryExtension.TryGetValue(fileData, "mimetype", out convertationResult);
                    message.file_description.filetype = DictionaryExtension.TryGetValue(fileData, "filetype", out convertationResult);
                    message.file_description.user_id = DictionaryExtension.TryGetValue(fileData, "user", out convertationResult);
                    //message.file_description.pretty_type = DictionaryExtension.TryGetValue(fileData, "pretty_type", out convertationResult);
                    //message.file_description.editable = DictionaryExtension.TryGetValue(fileData, "editable", out convertationResult);
                    //message.file_description.size = DictionaryExtension.TryGetValue(fileData, "size", out convertationResult);
                    //message.file_description.mode = DictionaryExtension.TryGetValue(fileData, "mode", out convertationResult);
                    //message.file_description.is_external = DictionaryExtension.TryGetValue(fileData, "is_external", out convertationResult);
                    //message.file_description.external_type = DictionaryExtension.TryGetValue(fileData, "external_type", out convertationResult);
                    //message.file_description.is_public = DictionaryExtension.TryGetValue(fileData, "is_public", out convertationResult);
                    //message.file_description.public_url_shared = DictionaryExtension.TryGetValue(fileData, "public_url_shared", out convertationResult);
                    //message.file_description.url = DictionaryExtension.TryGetValue(fileData, "url", out convertationResult);
                    //message.file_description.url_download = DictionaryExtension.TryGetValue(fileData, "url_download", out convertationResult);
                    //message.file_description.url_private = DictionaryExtension.TryGetValue(fileData, "url_private", out convertationResult);
                    //message.file_description.url_private_download = DictionaryExtension.TryGetValue(fileData, "url_private_download", out convertationResult);
                    //message.file_description.permalink = DictionaryExtension.TryGetValue(fileData, "permalink", out convertationResult);
                    //message.file_description.permalink_public = DictionaryExtension.TryGetValue(fileData, "permalink_public", out convertationResult);
                    //message.file_description.edit_link = DictionaryExtension.TryGetValue(fileData, "editlink", out convertationResult);
                    //message.file_description.preview = DictionaryExtension.TryGetValue(fileData, "preview", out convertationResult);
                    //message.file_description.preview_highlight = DictionaryExtension.TryGetValue(fileData, "preview_highlight", out convertationResult);
                    //message.file_description.lines = DictionaryExtension.TryGetValue(fileData, "lines", out convertationResult);
                    //message.file_description.lines_more = DictionaryExtension.TryGetValue(fileData, "lines_more", out convertationResult);

                    //var channelsData = DictionaryExtension.TryGetValue(fileData, "channels", out convertationResult);
                    //if (channelsData != null)
                    //{
                    //    foreach (string channel_id in channelsData)
                    //    {
                    //        message.file_description.channels_ids.Add(channel_id);
                    //    }
                    //}

                    //var groupsData = DictionaryExtension.TryGetValue(fileData, "groups", out convertationResult);
                    //if (groupsData != null)
                    //{
                    //    foreach (string group_id in channelsData)
                    //    {
                    //        message.file_description.groups_ids.Add(group_id);
                    //    }
                    //}

                    //var imsData = DictionaryExtension.TryGetValue(fileData, "ims", out convertationResult);
                    //if (imsData != null)
                    //{
                    //    foreach (string ims_id in imsData)
                    //    {
                    //        message.file_description.ims_ids.Add(ims_id);
                    //    }
                    //}

                    //message.file_description.comments_count = DictionaryExtension.TryGetValue(fileData, "comments_count", out convertationResult);
                }

                message.user_id = DictionaryExtension.TryGetValue(item, "user", out convertationResult);
                message.upload = DictionaryExtension.TryGetValue(item, "upload", out convertationResult);
                message.ts = DictionaryExtension.TryGetValue(item, "ts", out convertationResult);
                messageList.Add(message);
            }

            messageList.OrderBy(message => DateTimeHelper.DateTimeFromSLMessage(message)).ToList().Reverse();
            return messageList;
        }
        #endregion GroupsHistoryAsync()

        #region UsersListAsync()
        public async Task<List<SLUser>> UsersListAsync()
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "users.list?token=", apiToken);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractUsersList(data);
            }
            return new List<SLUser>();
        }

        private List<SLUser> ExtractUsersList(dynamic data)
        {
            List<SLUser> usersList = new List<SLUser>();
            bool convertationResult = false;

            dynamic groups = DictionaryExtension.TryGetValue(data, "members", out convertationResult);
            foreach (dynamic item in groups)
            {
                SLUser user = new SLUser();
                user.id = DictionaryExtension.TryGetValue(item, "id", out convertationResult);
                user.name = DictionaryExtension.TryGetValue(item, "name", out convertationResult);
                user.status = DictionaryExtension.TryGetValue(item, "status", out convertationResult);
                user.color = DictionaryExtension.TryGetValue(item, "color", out convertationResult);
                user.real_name = DictionaryExtension.TryGetValue(item, "real_name", out convertationResult);
                user.tz = DictionaryExtension.TryGetValue(item, "tz", out convertationResult);
                user.tz_label = DictionaryExtension.TryGetValue(item, "tz_label", out convertationResult);
                user.tz_offset = DictionaryExtension.TryGetValue(item, "tz_offset", out convertationResult);
                //user.profile
                user.is_admin = DictionaryExtension.TryGetValue(item, "is_admin", out convertationResult);
                user.is_owner = DictionaryExtension.TryGetValue(item, "is_owner", out convertationResult);
                user.is_primary_owner = DictionaryExtension.TryGetValue(item, "is_primary_owner", out convertationResult);
                user.is_restricted = DictionaryExtension.TryGetValue(item, "is_restricted", out convertationResult);
                user.is_ultra_restricted = DictionaryExtension.TryGetValue(item, "is_ultra_restricted", out convertationResult);
                user.is_bot = DictionaryExtension.TryGetValue(item, "is_bot", out convertationResult);
                user.has_files = DictionaryExtension.TryGetValue(item, "has_files", out convertationResult);
                user.has_2fa = DictionaryExtension.TryGetValue(item, "has_2fa", out convertationResult);

                usersList.Add(user);
            }
            return usersList;
        }
        #endregion UserListAsync()

        #region FilesListAsync()
        public async Task<List<SLFile>> FilesListAsync(string channel, string types)
        {
            string responseBody = await SendWebApiGetRequestAsync(rootUrl, "files.list?token=", apiToken, "&user=" + "U0K5APCTB", "&channel=" + channel, "&types=" + types, "&count=1000");
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                List<SLFile> files = new List<SLFile>();
                KeyValuePair<SLPaging, List<SLFile>> filesListData = ExtractFilesList(data);
                files.AddRange(filesListData.Value);
                if (filesListData.Key.pages > 1)
                {
                    for (int i = 2; i <= filesListData.Key.pages; i++)
                    {
                        responseBody = await SendWebApiGetRequestAsync(rootUrl, "files.list?token=", apiToken, "&user=" + "U0K5APCTB", "&channel=" + channel, "&types=" + types, "&count=1000", "&page=" + i.ToString());
                        data = javascriptSerializer.Deserialize<dynamic>(responseBody);
                        if (IsResponseStatusOk(data, ref error))
                        {
                            filesListData = ExtractFilesList(data);
                            files.AddRange(filesListData.Value);
                        }
                        else
                        {
                            return new List<SLFile>();
                        }
                    }
                } 
                files.OrderBy(message => message.created).ToList().Reverse();
                return files;
            }
            return new List<SLFile>();
        }

        private KeyValuePair<SLPaging, List<SLFile>> ExtractFilesList(dynamic data)
        {
            List<SLFile> fileList = new List<SLFile>();
            bool convertationResult = false;

            dynamic files = DictionaryExtension.TryGetValue(data, "files", out convertationResult);
            foreach (dynamic item in files)
            {
                SLFile file = new SLFile();
                file.id = DictionaryExtension.TryGetValue(item, "id", out convertationResult);
                file.created = DictionaryExtension.TryGetValue(item, "created", out convertationResult);
                file.timestamp = DictionaryExtension.TryGetValue(item, "timestamp", out convertationResult);
                file.name = DictionaryExtension.TryGetValue(item, "name", out convertationResult);
                file.title = DictionaryExtension.TryGetValue(item, "title", out convertationResult);
                file.mimetype = DictionaryExtension.TryGetValue(item, "mimetype", out convertationResult);
                file.filetype = DictionaryExtension.TryGetValue(item, "filetype", out convertationResult);
                file.user_id = DictionaryExtension.TryGetValue(item, "user", out convertationResult);
                fileList.Add(file);
            }

            dynamic pagingData = DictionaryExtension.TryGetValue(data, "paging", out convertationResult);
            SLPaging paging = new SLPaging();
            paging.count = DictionaryExtension.TryGetValue(pagingData, "count", out convertationResult);
            paging.total = DictionaryExtension.TryGetValue(pagingData, "total", out convertationResult);
            paging.page = DictionaryExtension.TryGetValue(pagingData, "page", out convertationResult);
            paging.pages = DictionaryExtension.TryGetValue(pagingData, "pages", out convertationResult);
            return new KeyValuePair<SLPaging, List<SLFile>>(paging, fileList);
        }
        #endregion FilesListAsync()

        public async Task<SLFileUploadResult> UploadFile(string content, string type, string name, string channels, string initialComment = "")
        {
            string responseBody = await SendWebApiPostRequestAsync(content, type, name, channels, initialComment);
            dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);

            string error = null;
            if (IsResponseStatusOk(data, ref error))
            {
                return ExtractFileUploadResult(data);
            }
            return new SLFileUploadResult();
        }

        private SLFileUploadResult ExtractFileUploadResult(dynamic data)
        {
            SLFileUploadResult slFileUploadResult = new SLFileUploadResult();
            bool convertationResult = false;
            
            slFileUploadResult.ok = DictionaryExtension.TryGetValue(data, "ok", out convertationResult);

            dynamic messageData = DictionaryExtension.TryGetValue(data, "file", out convertationResult);
            //if (fileData != null)
            //{
            //   TODO: extra file data to slFileUploadResult.slFile
            //}
            return slFileUploadResult; 
        }

        public string WrapWithConsoles(string src)
        {
            return "```" + src + "```";
        }

        private bool IsResponseStatusOk(dynamic data, ref string error)
        {
            bool convertationResult = false;
            dynamic dataOk = DictionaryExtension.TryGetValue(data, "ok", out convertationResult);
            if (dataOk != null)
            {
                if (dataOk == true)
                {
                    return true;
                }
                else
                {
                    error = DictionaryExtension.TryGetValue(data, "error", out convertationResult);
                    if (error != null)
                    {
                        if (error.Equals("not_authed"))
                        {
                            //MessageBox.Show("No authentication token provided.");
                        }
                        else if (error.Equals("invalid_auth"))
                        {
                            //MessageBox.Show("Invalid authentication token.");
                        }
                        else if (error.Equals("account_inactive"))
                        {
                            //MessageBox.Show("Authentication token is for a deleted user or team.");
                        }
                    }
                }
            }
            else
            {
                error = "There is no \"ok\" key in response";
            }
            return false;
        }

        private async Task<string> SendWebApiGetRequestAsync(string rootUrl, string apiParam, string apiToken, params string[] additionalParams)
        {
            StringBuilder fullUrl = new StringBuilder("");
            fullUrl.Append(rootUrl);
            fullUrl.Append(apiParam);
            fullUrl.Append(apiToken);
            foreach (string param in additionalParams)
            {
                string[] parts = param.Split('=');
                if (parts.Length >= 2)
                {
                    string paramName = parts[0];
                    StringBuilder paramValue = new StringBuilder();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        paramValue.Append(parts[i]);
                        if (! (i + 1 == parts.Length)) paramValue.Append("=");
                    }
                    fullUrl.Append(paramName);
                    fullUrl.Append("=");
                    fullUrl.Append(Uri.EscapeDataString(paramValue.ToString()));
                }
            }

            WebRequest request = WebRequest.Create(fullUrl.ToString());
            request.Method = "GET";
            WebProxy proxy = new WebProxy(proxyUrl);
            request.Proxy = proxy;

            WebResponse response = null;
            string responseBody = null;
            try
            {
                response = await request.GetResponseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            if (response != null)
            {
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                responseBody = streamReader.ReadToEnd().Trim();
            }
            return responseBody;
        }

        private async Task<string> SendWebApiPostRequestAsync(string content, string type, string name, string channels, string initialComment = "")
        {
            string response = null;
            HttpClientHandler handler = new HttpClientHandler()
            {
                Proxy = new WebProxy(proxyUrl, true),
                UseProxy = true
            };
            using (var client = new HttpClient(handler))
            {
                var values = new Dictionary<string, string>();
                values.Add("content", content);

                var contentCollection = new FormUrlEncodedContent(values);
                string requestUri = rootUrl + "files.upload?token=" + apiToken + "&filetype=" + type + "&filename=" + name + "&channels=" + channels + "&initial_comment=" + initialComment;
                HttpResponseMessage responseMsg = await client.PostAsync(requestUri, contentCollection);
                response = await responseMsg.Content.ReadAsStringAsync();
            }
            return response;
        }
    }
}
