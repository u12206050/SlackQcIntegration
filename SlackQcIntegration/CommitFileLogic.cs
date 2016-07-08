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
    internal class CommitFileLogic
    {
        private const string cFileNamePrefix = "cmts ";
        private const string cCommitStartLine = "```";
        private const string cCommitEndLIne = "```";
        private const string cCommitFieldsSeparator = "    ";
        private const string cCommitFromName = "From: ";
        private const string cCommitSubjectName = "Subject: ";
        private const string cLastPostedCommitDateTimeFilePath = "lastPostedCommitDateTime.txt";

        private string cConfigurationFolderPath;
        private SLWebApiClient slWebApiClient;
        private string imapServer;
        private string username;
        private string password;
        private string folderName;
        private List<string> subFolderNames;
        private IMAPConfig imapConfig;

        public CommitFileLogic(SLWebApiClient slWebApiClient, string imapServer, string username, string password, bool useSsl, string folderPathToPoll)
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

        public async void Update(Dictionary<string, HashSet<string>> channelIDsForRepositories)
        {
            try
            {
                IMAPClient client = new IMAPClient(imapConfig, null, 5);
                client.Logon();
                IMAPFolder folder = client.Folders[folderName];
                for (int i = 0; i < folder.SubFolders.Count; i++)
                {
                    folder = folder.SubFolders[i];
                    if (folder.FolderName == "Commit_Notifications") break;
                }

                List<IMAPMessage> messages = folder.Messages.OrderByDescending(msg => msg.Date).ToList();
                DateTime lastPostDate = ReadLastPostedDateTime(cConfigurationFolderPath + cLastPostedCommitDateTimeFilePath);
                List<IMAPMessage> newImapMessages = messages.TakeWhile((msg) => msg.Date > lastPostDate).ToList();

                if (newImapMessages.Count > 0)
                {
                    newImapMessages.OrderBy(msg => msg.Date).Reverse();
                    HashSet<string> channelIDs = new HashSet<string>(channelIDsForRepositories.Values.SelectMany(c => c).ToList());
                    Dictionary<string, HashSet<string>> commitUidsPostedInChannel = new Dictionary<string, HashSet<string>>();
                    foreach (string channelID in channelIDs)
                    {
                        HashSet<string> commitUids = await getCommitUidsForChannel(channelID);
                        commitUidsPostedInChannel.Add(channelID, commitUids);
                    }

                    foreach (IMAPMessage imapMsg in newImapMessages)
                    {
                        string[] parts = imapMsg.Subject.Split(':');
                        string commitToPrefix = "Commit to ";
                        string repositoryName = "";
                        if (parts[0].Length > commitToPrefix.Length)
                        {
                            repositoryName = parts[0].Substring(commitToPrefix.Length, parts[0].Length - commitToPrefix.Length);
                        }

                        bool repositoryFound = false;
                        HashSet<string> channelIDsForRepository = channelIDsForRepositories.TryGetValue(repositoryName, out repositoryFound);
                        if (repositoryFound)
                        {
                            bool postResult = await PostCommitToChannels(imapMsg, channelIDsForRepository, commitUidsPostedInChannel);
                            if (!postResult)
                            {
                                return;
                            }
                        }
                    }
                    DateTime newestMessageDate = newImapMessages.Max((msg) => msg.Date);
                    WriteLastPostedDateTime(cConfigurationFolderPath + cLastPostedCommitDateTimeFilePath, newestMessageDate);
                }
                client.Logoff();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in CommitLogic.Update(): " + ex.Message);
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

        private async Task<bool> PostCommitToChannels(IMAPMessage imapMsg, HashSet<string> channelIDsForRepository, Dictionary<string, HashSet<string>> commitUidsPostedInChannels)
        {
            bool posted = false;
            foreach (string channelID in channelIDsForRepository)
            {
                bool channelFound = false;
                HashSet<string> commitUidsPostedInChannel = commitUidsPostedInChannels.TryGetValue(channelID, out channelFound);
                if (channelFound)
                {
                    if (!commitUidsPostedInChannel.Contains(imapMsg.Uid.ToString()))
                    {
                        SLChatPostMessageResult commentResult = await PostCommitComment(imapMsg, channelID);
                        if ((!commentResult.ok.HasValue) || (!commentResult.ok.Value))
                        {
                            posted = false;
                            return posted;
                        }
                        else
                        {
                            posted = true;
                        }

                        SLFileUploadResult bodyResult = await PostCommitBody(imapMsg, channelID);
                        if ((!bodyResult.ok.HasValue) || (!bodyResult.ok.Value))
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

        private async Task<SLChatPostMessageResult> PostCommitComment(IMAPMessage imapMsg, string channelID)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append(cCommitStartLine);
            //sb.Append(imapMsg.Uid);
            //sb.Append(cCommitFieldsSeparator);
            sb.Append(cCommitFromName);
            sb.Append(imapMsg.From[0]);
            sb.Append(cCommitFieldsSeparator);
            sb.Append(cCommitSubjectName);
            sb.Append(imapMsg.Subject);
            sb.Append(cCommitEndLIne);
            return await slWebApiClient.ChatPostMessageAsync(channelID, sb.ToString(), true, false);
        }

        private async Task<SLFileUploadResult> PostCommitBody(IMAPMessage imapMsg, string channelID)
        {
            string commitBody = GetCommitBody(imapMsg);
            return await slWebApiClient.UploadFile(commitBody, "txt", cFileNamePrefix + imapMsg.Uid, channelID);
        }

        private async Task<HashSet<string>> getCommitUidsForChannel(string channelID)
        {
            HashSet<string> commitUids = new HashSet<string>();
            List<SLFile> files = await slWebApiClient.FilesListAsync(channelID, "all");
            foreach (SLFile file in files)
            {
                if (file.title.StartsWith(cFileNamePrefix))
                {
                    string uidStr = file.title.Substring(cFileNamePrefix.Length, file.title.Length - cFileNamePrefix.Length);
                    commitUids.Add(uidStr);
                }
            }
            return commitUids;
        }

        private string GetCommitBody(IMAPMessage message)
        {
            try
            {
                if (message.BodyParts != null)
                {
                    foreach (IMAPMessageContent content in message.BodyParts)
                    {
                        if (content.ContentType.StartsWith("text/plain"))
                        {
                            return content.TextData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return "";
        }
    }
}
