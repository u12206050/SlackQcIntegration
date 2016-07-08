using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Slack;
using InterIMAP;
using HtmlAgilityPack;

namespace SlackQcIntegration
{
    internal class BSLogic
    {
        private const string cBuildStartLine = "```Uid:";
        private const string cLastPostedBuildDateTimeFilePath = "lastPostedBuildDateTime.txt";

        private string cConfigurationFolderPath;
        private SLWebApiClient slWebApiClient;
        private string imapServer;
        private string username;
        private string password;
        private string folderName;
        private List<string> subFolderNames;
        private IMAPConfig imapConfig;

        public BSLogic(SLWebApiClient slWebApiClient, string imapServer, string username, string password, bool useSsl, string folderPathToPoll)
        {
            this.slWebApiClient = slWebApiClient;
            this.imapServer = imapServer;
            this.username = username;
            this.password = password;
            this.subFolderNames = new List<string>();

            string[] pathParts = folderPathToPoll.Split(new char[] { '/' });
            this.folderName = pathParts[0];

            if (pathParts.Length > 1)
            {
                for (int i = 1; i < pathParts.Length; i++)
                {
                    subFolderNames.Add(pathParts[i]);
                }
            }

            imapConfig = new IMAPConfig(imapServer, username, password, useSsl, false, folderPathToPoll);
            cConfigurationFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Configuration\";
        }

        public async void Update(List<string> groupIDs)
        {
            try
            {
                IMAPClient client = new IMAPClient(imapConfig, null, 5);
                client.Logon();
                IMAPFolder folder = client.Folders[folderName];
                for (int i = 0; i < subFolderNames.Count; i++)
                {
                    folder = folder.SubFolders[i];
                    if (folder.FolderName == "Build_System") break;   
                }
                
                List<IMAPMessage> messages = folder.Messages.OrderByDescending(msg => msg.Date).ToList();
                DateTime lastPostDate = ReadLastPostedDateTime(cConfigurationFolderPath + cLastPostedBuildDateTimeFilePath);
                List<IMAPMessage> newMessages = messages.TakeWhile((msg) => msg.Date > lastPostDate).ToList();

                if (newMessages.Count > 0)
                {
                    DateTime newestMessageDate = newMessages.Max((msg) => msg.Date);
                    bool buildPosted = await PostBuilds(newMessages, groupIDs);
                    if (buildPosted)
                    {
                        WriteLastPostedDateTime(cConfigurationFolderPath + cLastPostedBuildDateTimeFilePath, newestMessageDate);
                    }
                }
                client.Logoff();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in BSLogic.Update(): " + ex.Message);
            }
        }

        private DateTime ReadLastPostedDateTime(string filePath)
        {
            string line = null;
            using (StreamReader reader = new StreamReader(filePath))
            {
                line = reader.ReadLine();
            }
            if (line != null)
            {
                try
                {
                    return Convert.ToDateTime(line);
                }
                catch (Exception ex)
                {
                    throw new Exception("String should contain valid DateTime, e.g: 1/28/2016 10:34:02 AM", ex);
                }
            }
            else
            {
                return DateTime.Now;
            }
        }

        private void WriteLastPostedDateTime(string filePath, DateTime dateTime)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            File.WriteAllText(filePath, dateTime.ToString());
        }

        private async Task<bool> PostBuilds(List<IMAPMessage> messages, List<string> groupIDs)
        {
            bool posted = false;
            foreach (IMAPMessage message in messages)
            {
                foreach (string groupID in groupIDs)
                {
                    bool buildWasPosted = await BuildWasPosted(message.Uid, groupID);
                    if (!buildWasPosted)
                    {
                        SLChatPostMessageResult result = await PostBuild(message, groupID);
                        if ((!result.ok.HasValue) || (!result.ok.Value))
                        {
                            posted = false;
                            return posted;
                        }
                        else
                        {
                            posted = true;
                        }
                    }
                }
            }
            return posted;
        }

        private async Task<SLChatPostMessageResult> PostBuild(IMAPMessage message, string groupID)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append(cBuildStartLine);
            sb.Append(" ");
            sb.Append(message.Uid);
            sb.Append("    ");
            sb.Append(message.Subject);
            sb.AppendLine();
            sb.Append(FindLoadRunnerBuildUrl(message));
            sb.Append("```");
            return await slWebApiClient.ChatPostMessageAsync(groupID, sb.ToString(), true, true);
        }

        private async Task<bool> BuildWasPosted(int messageUid, string channelID)
        {
            List<SLMessage> messages = await slWebApiClient.GroupsHistoryAsync(channelID);
            int uID = 0;
            foreach (SLMessage message in messages)
            {
                if (message.text.StartsWith(cBuildStartLine))
                {
                    string[] parts = message.text.Split(new char[] { ' ' });
                    string uidStr = parts[0].Substring(cBuildStartLine.Length - 1, parts[0].Length - cBuildStartLine.Length);
                    Int32.TryParse(uidStr, out uID);
                    if (uID != 0) return true;
                }
            }
            return false;
        }

        private string FindLoadRunnerBuildUrl(IMAPMessage message)
        {
            string loadRunnerUrl = "";
            try
            {
                string htmlPart = "";
                if (message.BodyParts != null)
                {
                    foreach (IMAPMessageContent content in message.BodyParts)
                    {
                        if (content.ContentType.StartsWith("text/html"))
                        {
                            htmlPart = content.TextData;
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(htmlPart);
                            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//td[@style]"))
                            {
                                string innerText = node.InnerHtml;
                                if (innerText.Equals("LoadRunner Setup"))
                                {
                                    loadRunnerUrl = node.NextSibling.ChildNodes[0].Attributes["href"].Value;
                                    return loadRunnerUrl;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return loadRunnerUrl;
        }

    }
}
