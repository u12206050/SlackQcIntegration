using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using System.Net;
using Slack;
using ALM;

namespace SlackQcIntegration
{
    public partial class Form1 : Form
    {
        private const string cSlChannelsFilePath = "sl_channels.txt";
        private const string cSlGroupsFilePath = "sl_groups.txt";
        private const int cSlRuntimeRetryInterval = 10;
        private const int cTimerInterval = 1000;

        private SLLogic slLogic;
        private SLRuntimeLogic slRuntimeLogic;
        private SLWebApiClient slWebApiClient;
        private SLRuntimeApiClient slRuntimeApiClient;
        private ALMApiClient almApiClient;
        private string almDomain;
        private string almProject;
        private BSLogic bsLogic;
        private CommitLogic commitLogic;
        private CommitFileLogic commitFileLogic;

        private Button button;
        private System.Timers.Timer almTimer;
        private int almTickCounter;
        private int almPullInterval;
        private ProgressBar almProgressBar;
        private Label almProgressLabel;
        private Label slWebsocketStatusLabel;
        private System.Timers.Timer emailTimer;
        private int emailTickCounter;
        private int emailPullInterval;
        private ProgressBar emailProgressBar;
        private Label emailProgressLabel;

        public Form1()
        {
            InitializeComponent();

            this.Text = "Slack/QC integration";

            button = new Button();
            button.Location = new Point(10, 10);
            button.Size = new Size(470, 30);
            button.Text = "Get Slack groups and channels. After press - see " + cSlGroupsFilePath + " and " + cSlChannelsFilePath;
            button.Click += new EventHandler(OnSendButtonClick);
            this.Controls.Add(button);

            almProgressBar = new ProgressBar();
            almProgressBar.Location = new Point(10, 100);
            almProgressBar.Size = new Size(515, 30);
            almProgressBar.Minimum = 0;
            almProgressBar.Maximum = 100;
            this.Controls.Add(almProgressBar);

            almProgressLabel = new Label();
            almProgressLabel.Location = new Point(10, 130);
            almProgressLabel.Size = new Size(100, 15);
            almProgressLabel.Text = "0";
            this.Controls.Add(almProgressLabel);

            slWebsocketStatusLabel = new Label();
            slWebsocketStatusLabel.Location = new Point(540, 100);
            slWebsocketStatusLabel.Size = new Size(30, 30);
            slWebsocketStatusLabel.BackColor = Color.Red;
            this.Controls.Add(slWebsocketStatusLabel);

            emailProgressBar = new ProgressBar();
            emailProgressBar.Location = new Point(10, 150);
            emailProgressBar.Size = new Size(515, 30);
            emailProgressBar.Minimum = 0;
            emailProgressBar.Maximum = 100;
            this.Controls.Add(emailProgressBar);

            emailProgressLabel = new Label();
            emailProgressLabel.Location = new Point(10, 180);
            emailProgressLabel.Size = new Size(100, 30);
            emailProgressLabel.Text = "0";
            this.Controls.Add(emailProgressLabel);

            string almServerUrl = null;
            string almUser = null;
            string almPassword = null;
            almServerUrl = Configuration.ReadAlmUrl();
            Configuration.ReadAlmUserAndPassword(out almUser, out almPassword);
            Configuration.ReadALMDomainAndProject(out almDomain, out almProject);
            almApiClient = new ALMApiClient(almServerUrl, almUser, almPassword);

            string proxyUrl = Configuration.ReadProxyUrl();

            string slWebApiToken = Configuration.ReadApiTokenFromFile();
            slWebApiClient = new SLWebApiClient(slWebApiToken, proxyUrl);

            if (proxyUrl != null)
            {
                slRuntimeApiClient = new SLRuntimeApiClient(slWebApiToken, proxyUrl, "", "");
            }
            else
            {
                slRuntimeApiClient = new SLRuntimeApiClient(slWebApiToken);
            }

            slLogic = new SLLogic(slWebApiClient, almApiClient);
            slRuntimeLogic = new SLRuntimeLogic(slWebApiClient, slRuntimeApiClient, almApiClient, almDomain, almProject);

            string emailServer = null;
            string emailUser = null;
            string emailPassword = null;
            string emailFolderPath = null;
            emailServer = Configuration.ReadEmailServer();
            Configuration.ReadEmailUserAndPassword(out emailUser, out emailPassword);
            emailFolderPath = Configuration.ReadEmailFolderPath();
            bsLogic = new BSLogic(slWebApiClient, emailServer, emailUser, emailPassword, true, emailFolderPath);

            string commitFolderPath = Configuration.ReadCommitFolderPath();
            commitLogic = new CommitLogic(slWebApiClient, emailServer, emailUser, emailPassword, true, commitFolderPath);

            commitFileLogic = new CommitFileLogic(slWebApiClient, emailServer, emailUser, emailPassword, true, commitFolderPath);

            almTickCounter = 0;
            almPullInterval = Configuration.ReadAlmPullInterval();
            almTimer = new System.Timers.Timer(cTimerInterval);
            almTimer.Elapsed += new ElapsedEventHandler(OnAlmTimerTick);
            almTimer.Start();

            emailTickCounter = 0;
            emailPullInterval = Configuration.ReadEmailPullInterval();
            emailTimer = new System.Timers.Timer(cTimerInterval);
            emailTimer.Elapsed += new ElapsedEventHandler(OnEmailTimerTick);
            emailTimer.Start();
        }

        private async void OnSendButtonClick(object sender, EventArgs e)
        {
            GetChannelsAndGroups(cSlChannelsFilePath, cSlGroupsFilePath);
            //List<string> buildGroupIDs = Configuration.ReadBuildGroupIDs();
            //int buildsUpdateInterval = Configuration.ReadEmailPullInterval();
            //bsLogic.Update(buildGroupIDs);
            //SLFilePostResult filePostResult = await slWebApiClient.SendFile("HOH\n\nOHOH\n\nO\n\nH\n\nOOhfhdfhdfh\n\nfdhdfhdfh\n\n", "txt", "myfile2", "G0JEAR3NK");
            List<SLFile> files = await slWebApiClient.FilesListAsync("G0JEAR3NK", "all");
            MessageBox.Show("Done");
        }

        private async void OnAlmTimerTick(object sender, ElapsedEventArgs e)
        {
            try 
            {
                almTimer.Stop();
                if (almTickCounter < almPullInterval)
                {
                    almTickCounter++;
                }
                else
                {
                    //List<string> almQueries = Configuration.ReadAlmQueryStrings();
                    //Dictionary<string, List<string>> groupIDsForSubareas = Configuration.ReadGroupIDsForSubareas();
                    //slLogic.UpdateSlack(almDomain, almProject, almQueries, groupIDsForSubareas);
                    //almPullInterval = Configuration.ReadAlmPullInterval();
                    almTickCounter = 0;
                }
                UpdateAlmUI();
            }
            finally 
            {
                almTimer.Start();
            }

        }

        private async void OnEmailTimerTick(object sender, ElapsedEventArgs e)
        {
            try 
            {
                emailTimer.Stop();
                if (emailTickCounter < emailPullInterval)
                {
                    emailTickCounter++;
                }
                else
                {
                    //List<string> buildGroupIDs = Configuration.ReadBuildGroupIDs();
                    emailPullInterval = Configuration.ReadEmailPullInterval();
                    //bsLogic.Update(buildGroupIDs);

                    //List<string> commitGroupIDs = Configuration.ReadCommitGroupIDs();
                    Dictionary<string, HashSet<string>> groupIDsForRepositories = Configuration.ReadGroupIDsForRepositories();
                    //commitLogic.Update(groupIDsForRepositories);

                    commitFileLogic.Update(groupIDsForRepositories);

                    emailTickCounter = 0;
                }

                if (slRuntimeApiClient.IsOpen())
                {
                    MethodInvoker invoker = new MethodInvoker(() => slWebsocketStatusLabel.BackColor = Color.Green);
                    slWebsocketStatusLabel.Invoke(invoker);
                }
                else if (slRuntimeApiClient.IsConnecting())
                {
                    MethodInvoker invoker = new MethodInvoker(() => slWebsocketStatusLabel.BackColor = Color.Yellow);
                    slWebsocketStatusLabel.Invoke(invoker);
                }
                else
                {
                    // Slack limits rate of API request to 1 second.
                    // This may cause fails due to error 429.
                    // Let's try to authenticate each 5 seconds.
                    if ((almTickCounter % cSlRuntimeRetryInterval) == 0) slRuntimeApiClient.Authenticate();
                }

                UpdateEmailUI();
            }
            finally 
            {
                emailTimer.Start();
            }
        }

        private void UpdateAlmUI()
        {
            MethodInvoker almPbInvoker = new MethodInvoker(() => almProgressBar.Value = (int)((double)almTickCounter / (double)almPullInterval * 100));
            MethodInvoker almLbInvoker = new MethodInvoker(() => almProgressLabel.Text = almTickCounter.ToString() + " / " + almPullInterval.ToString());
            almProgressBar.Invoke(almPbInvoker);
            almProgressLabel.Invoke(almLbInvoker);
        }

        private void UpdateEmailUI()
        {
            MethodInvoker emailPbInvoker = new MethodInvoker(() => emailProgressBar.Value = (int)((double)emailTickCounter / (double)emailPullInterval * 100));
            MethodInvoker emailLbInvoker = new MethodInvoker(() => emailProgressLabel.Text = emailTickCounter.ToString() + " / " + emailPullInterval.ToString());
            emailProgressBar.Invoke(emailPbInvoker);
            emailProgressLabel.Invoke(emailLbInvoker);
        }

        public async void GetChannelsAndGroups(string slChannelsFileToWrite, string slGroupsFileToWrite)
        {
            List<SLChannel> channels = await slWebApiClient.ChannelsListAsync();
            List<SLGroup> groups = await slWebApiClient.GroupsListAsync();

            using (StreamWriter wr = new StreamWriter(slChannelsFileToWrite))
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    wr.WriteLine(channels[i].id + "_" + channels[i].name);
                }
            }

            using (StreamWriter wr = new StreamWriter(slGroupsFileToWrite))
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    wr.WriteLine(groups[i].id + "_" + groups[i].name);
                }
            }
        }

    }
}
