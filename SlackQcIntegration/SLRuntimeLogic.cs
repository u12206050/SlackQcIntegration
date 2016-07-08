using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Slack;
using ALM;
using DB;

namespace SlackQcIntegration
{
    internal class SLRuntimeLogic
    {
        private const string cReplyCommand = "!reply";
        private const string cDefCommand = "!def";
        private const string cReqCommand = "!req";
        private const string cNoteCommand = "!note";
        private const string cWhereCommand = "!where";
        private const string cReadmeCommand = "!readme";
        private string cDatabaseFolderPath;
        private string cReadmeFilePath;

        private SLWebApiClient slWebApiClient;
        private SLRuntimeApiClient slRuntimeApiClient;
        private ALMApiClient almApiClient;
        private string almDomain;
        private string almProject;

        public SLRuntimeLogic(SLWebApiClient slWebApiClient, SLRuntimeApiClient slRuntimeApiClient, ALMApiClient almApiClient, string almDomain, string almProject)
        {
            this.slWebApiClient = slWebApiClient;

            this.slRuntimeApiClient = slRuntimeApiClient;
            this.slRuntimeApiClient.OnWebsocketOpened += new EventHandler<EventArgs>(OnWebsocketOpened);
            this.slRuntimeApiClient.OnWebsocketMessage += new EventHandler<SLRuntimeEventArgs>(OnWebsocketMessage);
            this.slRuntimeApiClient.OnWebsocketError += new EventHandler<SLExceptionEventArgs>(OnWebsocketError);
            this.slRuntimeApiClient.OnWebsocketClosed += new EventHandler<EventArgs>(OnWebsocketClosed);

            this.almApiClient = almApiClient;
            this.almDomain = almDomain;
            this.almProject = almProject;

            string assemblyFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            cDatabaseFolderPath = assemblyFolderPath + @"\Database\";
            cReadmeFilePath = assemblyFolderPath + @"\Readme\Readme.txt";
        }

        private void OnWebsocketOpened(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Opened");
        }

        private void OnWebsocketClosed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Websocket closed");
        }

        private void OnWebsocketMessage(object sender, SLRuntimeEventArgs e)
        {
            if (e.text != null)
            {
                string[] parameters = e.text.Split(new char[] { ' ' });
                string commandName = parameters[0];

                switch (commandName)
                {
                    case cReplyCommand:
                        //HandleReplyCommand(parameters, e);
                        break;

                    case cWhereCommand:
                        //HandleWhereCommand(parameters, e);
                        break;

                    case cReadmeCommand:
                        //HandleReadmeCommand(parameters, e);
                        break;

                    case cDefCommand:
                        //HandleDefCommand(parameters, e);
                        break;

                    case cReqCommand:
                        //HandleReqCommand(parameters, e);
                        break;

                    case cNoteCommand:
                        //HandleNoteCommand(parameters, e);
                        break;

                    default: break;
                }
            }
            System.Diagnostics.Debug.WriteLine("Websocket message received: " + e.text);
        }

        private void OnWebsocketError(object sender, SLExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Websocket exception = " + e.exception.Message);
        }

        private void HandleReplyCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            StringBuilder text = new StringBuilder("");
            if (parameters.Length > 1)
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    text.Append(parameters[i]);
                    text.Append(" ");
                }
            }
            slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, false);
        }

        private async void HandleDefCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            if (almApiClient.Authenticate())
            {
                if (parameters.Length == 2)
                {
                    string defectId = parameters[1];

                    List<ALMDefect> defects = await almApiClient.GetDefectsAsync(almDomain, almProject, "&query={id[" + defectId + "];}");
                    StringBuilder text = new StringBuilder("");
                    if (defects.Count > 0)
                    {
                        text.Append("Title: " + defects[0].name + "\n");
                        text.Append("State: " + defects[0].status + "\n");
                        text.Append("Dev: " + defects[0].owner_dev + "\n");
                        text.Append("QA Lead: " + defects[0].user_40_qalead + "\n");
                        slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                    }
                    else
                    {
                        text.Append("Either such defect doesn't exist, either connection to ALM server is broken");
                        slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                    }
                }
                else if (parameters.Length == 3)
                {
                    string userType = parameters[1];
                    if (userType.Equals("-d"))
                    {
                        string developerQcName = parameters[2];
                        List<ALMDefect> defects = await almApiClient.GetDefectsAsync(almDomain, almProject, "&query={planned-closing-ver[Not \"Obsolete\"];owner[" + developerQcName + "];status[Not \"Closed\"];}");
                        StringBuilder text = new StringBuilder("");
                        if (defects.Count > 0)
                        {
                            foreach (ALMDefect defect in defects)
                            {
                                text.Append(defect.id + " - ");
                                text.Append(defect.name + "\n");
                                slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                                text.Clear();
                            }
                        }
                        else
                        {
                            text.Append("There is no 'Not Closed' defect on this developer, or developer name specified incorrectly");
                            slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                        }
                    }
                    else if (userType.Equals("-ql"))
                    {
                        string qaLeadQcName = parameters[2];
                        List<ALMDefect> defects = await almApiClient.GetDefectsAsync(almDomain, almProject, "&query={planned-closing-ver[Not \"Obsolete\"];user-40[" + qaLeadQcName + "];status[Not \"Closed\"];}");
                        StringBuilder text = new StringBuilder("");
                        if (defects.Count > 0)
                        {
                            foreach (ALMDefect defect in defects)
                            {
                                text.Append(defect.id + " - ");
                                text.Append(defect.name + "\n");
                                slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                                text.Clear();
                            }
                        }
                        else
                        {
                            text.Append("There is no 'Not Closed' defect on this qalead, or qalead name specified incorrectly");
                            slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                        }
                    }
                }
            }
            else
            {
                slRuntimeApiClient.SendWithMessageType(14235325, e.channel, "Connection to ALM server is broken", true, true);
            }
        }

        private async void HandleReqCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            if (almApiClient.Authenticate())
            {
                if (parameters.Length == 2)
                {
                    string reqId = parameters[1];
                    List<ALMRequirement> requirements = await almApiClient.GetRequirementsAsync(almDomain, almProject, "&query={id[" + reqId + "];}");
                    StringBuilder text = new StringBuilder("");
                    if (requirements.Count > 0)
                    {
                        ALMRequirement requirement = requirements[0];
                        text.Append("Title: " + requirement.name + "\n");
                        text.Append("State: " + requirement.user_06_status + "\n");
                        text.Append("Dev: " + requirement.user_17_dev_lead + "\n");
                        text.Append("QA Lead: " + requirement.user_27_qa_lead + "\n");
                        text.Append("Sprint: " + requirement.user_90_sprint + "\n");
                        slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                    }
                    else
                    {
                        text.Append("Either such requirement doesn't exist, either connection to ALM server is broken");
                        slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                    }
                }
                else if (parameters.Length == 5)
                {
                    string userType = parameters[1];
                    string sprintOption = parameters[3];
                    if ((userType.Equals("-d")) && sprintOption.Equals("-s"))
                    {
                        if (almApiClient.Authenticate())
                        {
                            string developerQcName = parameters[2];
                            string sprintNumber = parameters[4];
                            List<ALMRequirement> requirements = await almApiClient.GetRequirementsAsync(almDomain, almProject, "&query={user-17[" + developerQcName + "];user-90[" + sprintNumber + "];user-06[(Not \"5-Done\") And (Not \"8-Done\")];}");
                            StringBuilder text = new StringBuilder("");
                            if (requirements.Count > 0)
                            {
                                foreach (ALMRequirement requirement in requirements)
                                {
                                    text.Append(requirement.id);
                                    text.Append(" - ");
                                    text.Append(requirement.name);
                                    text.Append("\n");
                                    slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                                    text.Clear();
                                }
                            }
                            else
                            {
                                text.Append("There is no 'Not Done' requirements on this developer, or developer name specified incorrectly");
                                slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                            }
                        }
                    }
                    else if (userType.Equals("-ql") && sprintOption.Equals("-s"))
                    {
                        string qaLeadQcName = parameters[2];
                        string sprintNumber = parameters[4];
                        List<ALMRequirement> requirements = await almApiClient.GetRequirementsAsync(almDomain, almProject, "&query={user-27[" + qaLeadQcName + "];user-90[" + sprintNumber + "];user-06[(Not \"5-Done\") And (Not \"8-Done\")];}");
                        StringBuilder text = new StringBuilder("");
                        if (requirements.Count > 0)
                        {
                            foreach (ALMRequirement requirement in requirements)
                            {
                                text.Append(requirement.id);
                                text.Append(" - ");
                                text.Append(requirement.name);
                                text.Append("\n");
                                slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                                text.Clear();
                            }
                        }
                        else
                        {
                            text.Append("There is no 'Not Done' requirements on this qalead, or qalead name specified incorrectly");
                            slRuntimeApiClient.SendWithMessageType(1, e.channel, text.ToString(), true, true);
                        }
                    }
                }
            }
            else
            {
                slRuntimeApiClient.SendWithMessageType(1, e.channel, "Connection to ALM server is broken", true, true);
            }
        }

        private async void HandleNoteCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            if (parameters.Length >= 2)
            {
                string option = parameters[1];
                DBWorker dbWorker = new DBWorker(cDatabaseFolderPath + e.user + ".sqlite");
                dbWorker.Open();
                if (option.Equals("-l"))
                {
                    List<DBNotesEntry> dbNotesEntriesList = dbWorker.SelectNotes();
                    if (dbNotesEntriesList.Count > 0)
                    {
                        foreach (DBNotesEntry dbNoteEntry in dbNotesEntriesList)
                        {
                            StringBuilder message = new StringBuilder("");
                            message.Append("Id: ");
                            message.Append(dbNoteEntry.id);
                            message.Append("\n");
                            message.Append("Note: ");
                            //string normalized = SLNormalizer.Normalize(dbNoteEntry.note);
                            //message.Append(normalized);
                            message.Append(dbNoteEntry.note);
                            message.Append("\n");
                            await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message.ToString()), true, true);
                        }
                    }
                    else
                    {
                        string message = "There is 0 notes found in database";
                        await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message), true);
                    }
                }
                else if (option.Equals("-d"))
                {
                    if (parameters.Length >= 3)
                    {
                        string id = parameters[2];
                        if (dbWorker.DeleteNote(id) > 0)
                        {
                            string message = "Note " + id + " was deleted";
                            await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message.ToString()), true);
                        }
                        else
                        {
                            string message = "Note was not deleted or there is no note to delete";
                            await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message), true);
                        }
                    }
                }
                else if (option.Equals("-da"))
                { 
                    int deletedNotesCount = dbWorker.DeleteAllNotes();
                    if (deletedNotesCount > 0)
                    {
                        string message = deletedNotesCount + " notes was deleted";
                        await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message.ToString()), true);
                    }
                    else
                    {
                        string message = "Notes was not deleted or there is no notes to delete";
                        await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message), true);
                    }
                }
                else
                {
                    string note = e.text.Substring((cNoteCommand + " ").Length);
                    if ((dbWorker.InsertNote(SLNormalizer.Denormalize(note))) > 0)
                    {
                        string message = "Note was added";
                        await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message.ToString()), true);
                    }
                    else
                    {
                        string message = "For some reason note was not inserted";
                        await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(message), true);
                    }
                }
                dbWorker.Close();
            }
        }

        private async void HandleWhereCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            try
            {
                string fullQualifiedDomainName = "";
                string domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                string name = System.Net.Dns.GetHostName();
                fullQualifiedDomainName = name + "." + domain;
                string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

                StringBuilder sb = new StringBuilder("");
                sb.Append("Machine: ");
                sb.Append(fullQualifiedDomainName);
                sb.Append("\n");
                sb.Append("Location: ");
                sb.Append(assemblyPath.Replace(@"\", "/"));
                slRuntimeApiClient.SendWithMessageType(1, e.channel, sb.ToString(), true, true);
            }
            catch (Exception ex)
            {

            }
        }

        private async void HandleReadmeCommand(string[] parameters, SLRuntimeEventArgs e)
        {
            try
            {
                string readmeContent = File.ReadAllText(cReadmeFilePath);
                await slWebApiClient.ChatPostMessageAsync(e.channel, slWebApiClient.WrapWithConsoles(readmeContent), true, false);
            }
            catch
            {

            }
        }
    }
}
