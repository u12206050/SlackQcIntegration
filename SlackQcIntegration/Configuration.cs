using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SlackQcIntegration
{
    internal static class Configuration
    {
        private static string cConfigurationFolderPath;
        private static string cAlmUrlFilePath;
        private static string cAlmUserAndPasswordFilePath;
        private static string cAlmDomainAndProjectFilePath;
        private static string cAlmQueriesFilePath;
        private static string cSlWebApiTokenFilePath;
        private static string cGroupIDsForSubareasFilePath;
        private static string cGroupIDsForRepositoriesFilePath;
        private static string cAlmPullIntervalFilePath;
        private static string cCorrectionSecondsFilePath;
        private static string cProxyUrlFilePath;
        private static string cEmailPullInterval;
        private static string cBuildGroupIDsFilePath;
        private static string cEmailServerFilePath;
        private static string cEmailUserAndPasswordFilePath;
        private static string cEmailFolderFilePath;
        private static string cCommitFolderFilePath;
        private static string cEmailPullIntervalFolderPath;

        static Configuration()
        {
            cConfigurationFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Configuration\";
            cAlmUrlFilePath = cConfigurationFolderPath + "alm_url.txt";
            cAlmUserAndPasswordFilePath = cConfigurationFolderPath + "alm_UserAndPassword.txt";
            cAlmDomainAndProjectFilePath = cConfigurationFolderPath + "alm_DomainAndProject.txt";
            cAlmQueriesFilePath = cConfigurationFolderPath + "alm_queries.txt";
            cSlWebApiTokenFilePath = cConfigurationFolderPath + "sl_webApiToken.txt";
            cGroupIDsForSubareasFilePath = cConfigurationFolderPath + "groupIDsForSubareas.txt";
            cGroupIDsForRepositoriesFilePath = cConfigurationFolderPath + "groupIDsForRepositories.txt";
            cAlmPullIntervalFilePath = cConfigurationFolderPath + "alm_PullInterval.txt";
            cCorrectionSecondsFilePath = cConfigurationFolderPath + "correctionSeconds.txt";
            cProxyUrlFilePath = cConfigurationFolderPath + "sl_proxyUrl.txt";
            cEmailPullInterval = cConfigurationFolderPath + "email_PullInterval.txt";
            cBuildGroupIDsFilePath = cConfigurationFolderPath + "buildGroupIDs.txt";
            cEmailServerFilePath = cConfigurationFolderPath + "email_server.txt";
            cEmailUserAndPasswordFilePath = cConfigurationFolderPath + "email_UserAndPassword.txt";
            cEmailFolderFilePath = cConfigurationFolderPath + "email_folderPath.txt";
            cCommitFolderFilePath = cConfigurationFolderPath + "commit_folderPath.txt";
            cEmailPullIntervalFolderPath = cConfigurationFolderPath + "emailPullInteval.txt";
        }

        public static string ReadApiTokenFromFile()
        {
            string filePath = cSlWebApiTokenFilePath;
            using (StreamReader reader = new StreamReader(filePath))
            {
                return reader.ReadLine();
            }
        }

        public static string ReadAlmUrl()
        {
            return ReadString(cAlmUrlFilePath);
        }

        public static string ReadEmailServer()
        {
            return ReadString(cEmailServerFilePath);
        }

        public static string ReadEmailFolderPath()
        {
            return ReadString(cEmailFolderFilePath);
        }
        public static string ReadCommitFolderPath()
        {
            return ReadString(cCommitFolderFilePath);
        }

        public static void ReadEmailUserAndPassword(out string name, out string password)
        {
            ReadUserAndPassword(cEmailUserAndPasswordFilePath, out name, out password);
        }

        public static void ReadAlmUserAndPassword(out string name, out string password)
        {
            ReadUserAndPassword(cAlmUserAndPasswordFilePath, out name, out password);
        }

        public static void ReadALMDomainAndProject(out string domain, out string project)
        {
            string filePath = cAlmDomainAndProjectFilePath;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = reader.ReadLine();
                string[] domainAndProject = line.Split(' ');
                if (domainAndProject.Length >= 2)
                {
                    domain = domainAndProject[0];
                    project = domainAndProject[1];
                }
                else
                {
                    throw new Exception("File " + filePath + " should contain domain and project separated by space");
                }
            }
        }

        public static List<string> ReadAlmQueryStrings()
        {
            string filePath = cAlmQueriesFilePath;
            List<string> queryStrings = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    queryStrings.Add(line);
                }
            }
            return queryStrings;
        }

        public static Dictionary<string, List<string>> ReadGroupIDsForSubareas()
        {
            string filePath = cGroupIDsForSubareasFilePath;
            Dictionary<string, List<string>> groupIDsForSubareas = new Dictionary<string, List<string>>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] subareaAndGroupID = line.Split('/');
                    if (subareaAndGroupID.Length >= 3)
                    {
                        if (groupIDsForSubareas.ContainsKey(subareaAndGroupID[0]))
                        {
                            groupIDsForSubareas[subareaAndGroupID[0]].Add(subareaAndGroupID[1]);
                        }
                        else
                        {
                            List<string> groupIDs = new List<string>();
                            groupIDs.Add(subareaAndGroupID[1]);
                            groupIDsForSubareas.Add(subareaAndGroupID[0], groupIDs);
                        }
                    }
                }
            }
            return groupIDsForSubareas;
        }

        public static Dictionary<string, HashSet<string>> ReadGroupIDsForRepositories()
        {
            string filePath = cGroupIDsForRepositoriesFilePath;
            Dictionary<string, HashSet<string>> groupIDsForRepositories = new Dictionary<string, HashSet<string>>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] repositoryAndGroupID = line.Split('/');
                    if (repositoryAndGroupID.Length >= 3)
                    {
                        if (groupIDsForRepositories.ContainsKey(repositoryAndGroupID[0]))
                        {
                            groupIDsForRepositories[repositoryAndGroupID[0]].Add(repositoryAndGroupID[1]);
                        }
                        else
                        {
                            HashSet<string> groupIDs = new HashSet<string>();
                            groupIDs.Add(repositoryAndGroupID[1]);
                            groupIDsForRepositories.Add(repositoryAndGroupID[0], groupIDs);
                        }
                    }
                }
            }
            return groupIDsForRepositories;
        }

        public static int ReadAlmPullInterval()
        {
            return ReadPullInterval(cAlmPullIntervalFilePath);
        }

        public static int ReadEmailPullInterval()
        {
            return ReadPullInterval(cEmailPullInterval);
        }

        public static string ReadProxyUrl()
        {
            string filePath = cProxyUrlFilePath;
            string line = null;
            using (StreamReader reader = new StreamReader(filePath))
            {
                line = reader.ReadLine();
            }
            return line;
        }

        public static List<string> ReadBuildGroupIDs()
        {
            string filePath = cBuildGroupIDsFilePath;
            HashSet<string> buildGroupIDs = new HashSet<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(new char[] { '/' });
                    string groupID = parts[0];
                    buildGroupIDs.Add(groupID);
                }
            }
            return buildGroupIDs.ToList();
        }
        private static int ReadPullInterval(string filePath)
        {
            string line = null;
            using (StreamReader reader = new StreamReader(filePath))
            {
                line = reader.ReadLine();
            }

            int updateInterval = -1;
            if (Int32.TryParse(line, out updateInterval))
            {
                if (updateInterval > 0)
                {
                    return updateInterval;
                }
                else
                {
                    throw new Exception("File " + cAlmPullIntervalFilePath + " should contain positive integer value, amount of seconds. E.g: 30");
                }
            }
            else
            {
                throw new Exception("File " + cAlmPullIntervalFilePath + " should contain positive integer value, amount of seconds. E.g: 30");
            }
        }

        private static void ReadUserAndPassword(string filePath, out string name, out string password)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = reader.ReadLine();
                string[] nameAndPassword = line.Split(' ');
                if (nameAndPassword.Length >= 2)
                {
                    name = nameAndPassword[0];
                    password = nameAndPassword[1];
                }
                else
                {
                    throw new Exception("File " + filePath + " should contain username and password separated by space");
                }
            }
        }

        private static string ReadString(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                return reader.ReadLine();
            }
        }
    }
}
