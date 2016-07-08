using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Timers;
using System.IO;
using Slack;
using ALM;

namespace SlackQcIntegration
{
    public class MainService : ServiceBase
    {
        private const int cSlRuntimeRetryInterval = 10;
        private const int cTimerInterval = 1000;

        private SLLogic slLogic;
        private SLRuntimeLogic slRuntimeLogic;
        private SLWebApiClient slWebApiClient;
        private SLRuntimeApiClient slRuntimeApiClient;
        private ALMApiClient almApiClient;
        private string almDomain;
        private string almProject;
        private System.Timers.Timer almTimer;
        private int almTickCounter;
        private int almPullInterval;
        private BSLogic bsLogic;
        private CommitLogic commitLogic;
        private CommitFileLogic commitFileLogic;
        private System.Timers.Timer emailTimer;
        private int emailTickCounter;
        private int emailPullInterval;
        
        private Thread thread;

        public MainService()
        {

        }

        protected override void OnStart(string[] args)
        {
            thread = new Thread(new ThreadStart(ThreadStart));
            thread.Start();
        }

        protected override void OnStop()
        {
            thread.Abort();
        }

        private void ThreadStart()
        {
            try
            {
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
                almTimer.Enabled = true;
                almTimer.Start();

                emailTickCounter = 0;
                emailPullInterval = Configuration.ReadEmailPullInterval();
                emailTimer = new System.Timers.Timer(cTimerInterval);
                emailTimer.Elapsed += new ElapsedEventHandler(OnEmailTimerTick);
                emailTimer.Enabled = true;
                emailTimer.Start();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + "dump.txt", ex.Message);
            }
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
                    List<string> almQueries = Configuration.ReadAlmQueryStrings();
                    Dictionary<string, List<string>> groupIDsForSubareas = Configuration.ReadGroupIDsForSubareas();
                    //slLogic.UpdateSlack(almDomain, almProject, almQueries, groupIDsForSubareas);
                    almPullInterval = Configuration.ReadAlmPullInterval();
                    almTickCounter = 0;
                }

                if ((almTickCounter % cSlRuntimeRetryInterval) == 0)
                {
                    if (!(slRuntimeApiClient.IsOpen() || slRuntimeApiClient.IsConnecting()))
                    {
                        // Slack limits rate of API request to 1 second.
                        // This may cause fails due to error 429.
                        // Let's try to authenticate each cSlRuntimeRetryInterval seconds.
                        slRuntimeApiClient.Authenticate();
                    }
                }
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
                    List<string> buildGroupIDs = Configuration.ReadBuildGroupIDs();
                    emailPullInterval = Configuration.ReadEmailPullInterval();
                    bsLogic.Update(buildGroupIDs);

                    Dictionary<string, HashSet<string>> groupIDsForRepositories = Configuration.ReadGroupIDsForRepositories();
                    //commitLogic.Update(groupIDsForRepositories);
                    commitFileLogic.Update(groupIDsForRepositories);

                    emailTickCounter = 0;
                }
            }
            finally
            {
                emailTimer.Start();
            }
        }
    }
}
