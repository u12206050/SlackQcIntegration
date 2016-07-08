using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Slack;
using ALM;

namespace SlackQcIntegration
{
    internal class SLLogic
    {
        private const string cDefectStartLine = "```Defect id: ";
        private const string cDefectFieldsSeparator = "    ";
        private string cConfigurationFolderPath;
        private string lastPostedDefectDateTimeFilePath;
        private SLWebApiClient slWebApiClient;
        private ALMApiClient almApiClient;
        

        public SLLogic(SLWebApiClient slWebApiClient, ALMApiClient almApiClient)
        {
            this.slWebApiClient = slWebApiClient;
            this.almApiClient = almApiClient;

            cConfigurationFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Configuration\";
            lastPostedDefectDateTimeFilePath = cConfigurationFolderPath + "lastPostedDefectDateTime.txt";
        }

        public async void UpdateSlack(string almDomain, string almProject, List<string> almQueries, Dictionary<string, List<string>> groupsIDsForSubareas)
        {
            Dictionary<string, List<SLMessage>> messagesForGroupIDs = await GetMessagesForGroupIDs(groupsIDsForSubareas);
            if (almApiClient.Authenticate())
            {
                DateTime lastPostDate = ReadLastPostedDateTime(lastPostedDefectDateTimeFilePath);
                List<ALMDefect> almDefectsForQueries = await GetAlmDefectsForQueries(almDomain, almProject, almQueries, lastPostDate);
                bool allDefectsPosted = true;
                DateTime newestDefectModifyDate = DateTime.Now;
                if (almDefectsForQueries.Count != 0)
                {
                    newestDefectModifyDate = almDefectsForQueries.Max(defect => Convert.ToDateTime(defect.last_modified_date));
                    Dictionary<string, List<ALMDefect>> almDefectsForSubareas = OrganizeDefectsBySubareas(almDefectsForQueries, groupsIDsForSubareas.Keys.ToList());
                    bool defectsPosted = await PostDefects(almDefectsForSubareas, messagesForGroupIDs, groupsIDsForSubareas);
                    if (!defectsPosted)
                    {
                        allDefectsPosted = false;
                    }
                }
                else
                {
                    // No defects received from query? Or there is no connection to server? T_T
                    allDefectsPosted = false;
                }
                if (allDefectsPosted)
                {
                    WriteLastPostedDateTime(lastPostedDefectDateTimeFilePath, newestDefectModifyDate);
                }
            }
            else
            {
                // Failed to authenticate on ALM
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

        private async Task<List<ALMDefect>> GetAlmDefectsForQueries(string almDomain, string almProject, List<string> almQueries, DateTime lastPostDate)
        {
            List<ALMDefect> almDefectsForQueries = new List<ALMDefect>();
            foreach (string almQuery in almQueries)
            {
                int indexOfSemicolon = almQuery.IndexOf(";");
                if (indexOfSemicolon != -1)
                {
                    string almQueryModified = almQuery.Insert(indexOfSemicolon + 1, "last-modified[> \"" + lastPostDate.Year + "-" + lastPostDate.Month + "-" + lastPostDate.Day + " " + lastPostDate.Hour + ":" + lastPostDate.Minute + ":" + lastPostDate.Second + "\"" + "];");
                    List<ALMDefect> almDefects = await almApiClient.GetDefectsAsync(almDomain, almProject, almQueryModified);
                    almDefectsForQueries.AddRange(almDefects);
                }
            }
            return almDefectsForQueries;
        }

        private async Task<bool> PostDefects(Dictionary<string, List<ALMDefect>> almDefectsForSubareas, Dictionary<string, List<SLMessage>> messagesForGroupIDs, Dictionary<string, List<string>> groupsIDsForSubareas)
        {
            bool posted = false;
            foreach (KeyValuePair<string, List<string>> groupsIDsForSubarea in groupsIDsForSubareas)
            {
                if (almDefectsForSubareas.ContainsKey(groupsIDsForSubarea.Key))
                {
                    foreach (ALMDefect defect in almDefectsForSubareas[groupsIDsForSubarea.Key])
                    {
                        foreach (string groupID in groupsIDsForSubarea.Value)
                        {
                            bool defectWasPostedBefore = false;
                            DateTime? messageDate = null;
                            string status = null;
                            FindDefectInMessages(defect, messagesForGroupIDs[groupID], out defectWasPostedBefore, out messageDate, out status);

                            if (defectWasPostedBefore)
                            {
                                if (!defect.status.Equals(status))
                                {
                                    SLChatPostMessageResult result = await PostDefect(groupID, defect);
                                    if (result.ok.HasValue)
                                    {
                                        if (result.ok.Value) posted = true;
                                    }
                                }
                                else
                                {
                                    // Updates without changed state are pesky
                                    // DateTime defectLastModifiedDate = DateTime.Parse(defect.last_modified_date);
                                    // if (defectLastModifiedDate > messageDate)
                                    // {
                                    //     await PostDefect(groupID, defect);
                                    // }
                                    // System.Diagnostics.Debug.WriteLine("Defect: " + defect.id + "  Body:" + defect.name);
                                }
                            }
                            else
                            {
                                if (!defect.status.Equals("Closed"))
                                {
                                    SLChatPostMessageResult result = await PostDefect(groupID, defect);
                                    if (result.ok.HasValue)
                                    {
                                        if (result.ok.Value) posted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return posted;
        }

        private async Task<SLChatPostMessageResult> PostDefect(string groupId, ALMDefect defect)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append(cDefectStartLine);
            sb.Append("");
            sb.Append(defect.id.Replace("&", "'"));
            sb.Append(cDefectFieldsSeparator);
            sb.Append("Status: ");
            sb.Append(defect.status.Replace("&", "'"));
            sb.Append(cDefectFieldsSeparator);
            sb.Append("Severity: ");
            sb.Append(defect.priority.Replace("&", "'"));
            sb.Append(cDefectFieldsSeparator);
            sb.Append("Devlead: ");
            sb.Append(defect.owner_dev.Replace("&", "'"));
            sb.Append(cDefectFieldsSeparator);
            sb.Append("Qalead: ");
            sb.Append(defect.user_40_qalead.Replace("&", "'"));
            sb.Append("\n");
            sb.Append(defect.name.Replace("&", "'"));
            sb.Append("\n");
            sb.Append("```");
            return await slWebApiClient.ChatPostMessageAsync(groupId, sb.ToString(), true);
        }

        private void FindDefectInMessages(ALMDefect defect, List<SLMessage> messages, out bool defectWasPostedBefore, out DateTime? messageDate, out string status)
        {
            defectWasPostedBefore = false;
            messageDate = null;
            status = null;
            foreach (var message in messages)
            {
                if (message.text.StartsWith(cDefectStartLine))
                {
                    string[] messageParts = message.text.Split('\n');
                    if (messageParts.Length >= 2)
                    {
                        string[] firstLineElements = messageParts[0].Split(new string[] { cDefectFieldsSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        if (firstLineElements.Length >= 2)
                        {
                            string defectId = firstLineElements[0].Remove(0, cDefectStartLine.Length);
                            messageDate = DateTimeHelper.DateTimeFromSLMessage(message);
                            status = firstLineElements[1].Remove(0, "Status: ".Length);
                            if (defect.id.Equals(defectId))
                            {
                                defectWasPostedBefore = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, List<ALMDefect>> OrganizeDefectsBySubareas(List<ALMDefect> almDefects, List<string> subareas)
        {
            Dictionary<string, List<ALMDefect>> defectsForSubareas = new Dictionary<string, List<ALMDefect>>();
            foreach (var defect in almDefects)
            {
                foreach (string subarea in subareas)
                {
                    if (defect.user_04_subarea.Equals(subarea))
                    {
                        if (defectsForSubareas.ContainsKey(subarea))
                        {
                            defectsForSubareas[subarea].Add(defect);
                        }
                        else
                        {
                            List<ALMDefect> defectsForSubarea = new List<ALMDefect>();
                            defectsForSubarea.Add(defect);
                            defectsForSubareas.Add(subarea, defectsForSubarea);
                        }
                    }
                }
            }
            return defectsForSubareas;
        }

        private async Task<Dictionary<string, List<SLMessage>>> GetMessagesForGroupIDs(Dictionary<string, List<string>> groupsIDsForSubareas)
        {
            Dictionary<string, List<SLMessage>> messagesForGroups = new Dictionary<string, List<SLMessage>>();
            HashSet<string> groupsIDs = new HashSet<string>();
            foreach (KeyValuePair<string, List<string>> groupsIDsForSubarea in groupsIDsForSubareas)
            {
                foreach (string groupID in groupsIDsForSubarea.Value)
                {
                    if (!groupsIDs.Contains(groupID))
                    {
                        groupsIDs.Add(groupID);
                        List<SLMessage> messages = await slWebApiClient.GroupsHistoryAsync(groupID);
                        messagesForGroups.Add(groupID, messages);
                    }
                }
            }
            return messagesForGroups;
        }
    }
}
