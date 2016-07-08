using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace ALM
{
    public class ALMApiClient
    {
        private const int cPageSize = 100;
        private string serverRootUrl;
        private string username;
        private string password;
        private string lwSsoCookieKey;
        private string qcSession;
        private string almUser;
        private string xsrfToken;
        private JavaScriptSerializer javascriptSerializer;
        
        public ALMApiClient(string rootServerUrl, string username, string password)
        {
            this.serverRootUrl = rootServerUrl;
            this.username = username;
            this.password = password;
            javascriptSerializer = new JavaScriptSerializer();
        }

        public bool Authenticate()
        {
            this.lwSsoCookieKey = null;
            this.qcSession = null;
            this.almUser = null;
            this.xsrfToken = null;

            HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(this.serverRootUrl + "/qcbin/api/authentication/sign-in");
            request.Method = "GET";
            string base64EncodedUserAndPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(this.username + ":" + this.password));
            request.Headers.Add("Authorization: Basic " + base64EncodedUserAndPassword);

            try
            {
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
                else
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            string[] values = response.Headers.GetValues(i);
                            StringBuilder sb = new StringBuilder("");
                            foreach (string str in values)
                            {
                                if (str.StartsWith("LWSSO_COOKIE_KEY="))
                                {
                                    this.lwSsoCookieKey = str;
                                    continue;
                                }

                                if (str.StartsWith("QCSession="))
                                {
                                    this.qcSession = str;
                                    continue;
                                }

                                if (str.StartsWith("ALM_USER="))
                                {
                                    this.almUser = str;
                                    continue;
                                }

                                if (str.StartsWith("XSRF-TOKEN="))
                                {
                                    this.xsrfToken = str;
                                    continue;
                                }
                            }
                        }
                    }
                    
                    if ((this.lwSsoCookieKey != null) && (this.qcSession != null) && (this.almUser != null) && (this.xsrfToken != null))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception during ALM authentication: " + ex.Message);
            }
            return false;
        }

        #region GetDefectsAsync()
        public async Task<List<ALMDefect>> GetDefectsAsync(string domain, string project, string queryParameter)
        {
            int startIndex = 1;
            string responseBody = await GetEntitiesAsync(domain, project, "defects", cPageSize, startIndex, queryParameter);
   
            if (responseBody != null)
            {
                dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);
                int? totalResults = null;
                List<ALMDefect> defects = ExtractDefects(data, out totalResults);
                if (totalResults > cPageSize)
                {
                    startIndex += cPageSize;
                    await GetDefectsByPagesAsync(defects, domain, project, cPageSize, startIndex, queryParameter);
                }
                if (defects.Count == totalResults)
                {
                    return defects;
                }
                defects.Clear();
            }
            return new List<ALMDefect>();
        }

        private async Task GetDefectsByPagesAsync(List<ALMDefect> defects, string domain, string project, int pageSize, int startIndex, string queryParameter)
        {
            string responseBody = await GetEntitiesAsync(domain, project, "defects", cPageSize, startIndex, queryParameter);

            if (responseBody != null)
            {
                dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);
                int? totalResults = null;
                List<ALMDefect> defectsOnPage = ExtractDefects(data, out totalResults);

                if (defects.Count > 0)
                {
                    defects.AddRange(defectsOnPage);
                    if (defectsOnPage.Count == cPageSize)
                    {
                        startIndex += cPageSize;
                        await GetDefectsByPagesAsync(defects, domain, project, cPageSize, startIndex, queryParameter);
                    }
                }
            }
        }

        private List<ALMDefect> ExtractDefects(dynamic data, out int? totalResults)
        {
            List<ALMDefect> defectsList = new List<ALMDefect>();
            bool convertationResult = false;

            dynamic entities = DictionaryExtension.TryGetValue(data, "entities", out convertationResult);
            if (entities != null)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    ALMDefect defect = new ALMDefect();
                    dynamic fields = DictionaryExtension.TryGetValue(entities[i], "Fields", out convertationResult);

                    if (fields != null)
                    {
                        for (int j = 0; j < fields.Length; j++)
                        {
                            dynamic someName = DictionaryExtension.TryGetValue(fields[j], "Name", out convertationResult);
                            dynamic someValue = DictionaryExtension.TryGetValue(fields[j], "values", out convertationResult);

                            if ((someName != null) && (someValue != null))
                            {
                                if (someValue.Length > 0)
                                {
                                    if (someName.Equals("product")) defect.product = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("version")) defect.version = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("id")) defect.id = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("name")) defect.name = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("priority")) defect.priority = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("status")) defect.status = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("detected-by")) defect.detected_by = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("owner")) defect.owner_dev = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-01")) defect.user_01_productarea = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-04")) defect.user_04_subarea = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-14")) defect.user_14_devteam = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-40")) defect.user_40_qalead = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("last-modified")) defect.last_modified_date = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("planned-closing-ver")) defect.planned_closing_ver = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                }
                            }
                        }
                    }
                    defectsList.Add(defect);
                }
            }

            totalResults = DictionaryExtension.TryGetValue(data, "TotalResults", out convertationResult);
            return defectsList;
        }
        #endregion GetDefectsAsync()

        #region GetRequirementsAsync()
        public async Task<List<ALMRequirement>> GetRequirementsAsync(string domain, string project, string queryParameter)
        {
            int startIndex = 1;
            string responseBody = await GetEntitiesAsync(domain, project, "requirements", cPageSize, startIndex, queryParameter);

            if (responseBody != null)
            {
                dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);
                int? totalResults = null;
                List<ALMRequirement> requirements = ExtractRequirements(data, out totalResults);
                if (requirements.Count == cPageSize)
                {
                    startIndex += cPageSize;
                    await GetRequirementsByPagesAsync(requirements, domain, project, cPageSize, startIndex, queryParameter);
                }
                if (requirements.Count == totalResults)
                {
                    return requirements;
                }
                requirements.Clear();
            }
            return new List<ALMRequirement>();
        }

        private async Task GetRequirementsByPagesAsync(List<ALMRequirement> requirements, string domain, string project, int pageSize, int startIndex, string queryParameter)
        {
            string responseBody = await GetEntitiesAsync(domain, project, "requirements", cPageSize, startIndex, queryParameter);

            if (responseBody != null)
            {
                dynamic data = javascriptSerializer.Deserialize<dynamic>(responseBody);
                int? totalResults = null;
                List<ALMRequirement> requirementsOnPage = ExtractRequirements(data, out totalResults);

                if (requirementsOnPage.Count > 0)
                {
                    requirements.AddRange(requirementsOnPage);
                    if (requirementsOnPage.Count == cPageSize)
                    {
                        startIndex += cPageSize;
                        await GetRequirementsByPagesAsync(requirements, domain, project, cPageSize, startIndex, queryParameter);
                    }
                }
            }
        }

        private List<ALMRequirement> ExtractRequirements(dynamic data, out int? totalResults)
        {
            List<ALMRequirement> requirementsList = new List<ALMRequirement>();
            bool convertationResult = false;

            dynamic entities = DictionaryExtension.TryGetValue(data, "entities", out convertationResult);
            if (entities != null)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    ALMRequirement requirement = new ALMRequirement();
                    dynamic fields = DictionaryExtension.TryGetValue(entities[i], "Fields", out convertationResult);

                    if (fields != null)
                    {
                        for (int j = 0; j < fields.Length; j++)
                        {
                            dynamic someName = DictionaryExtension.TryGetValue(fields[j], "Name", out convertationResult);
                            dynamic someValue = DictionaryExtension.TryGetValue(fields[j], "values", out convertationResult);

                            if ((someName != null) && (someValue != null))
                            {
                                if (someValue.Length > 0)
                                {
                                    if (someName.Equals("id")) requirement.id = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("parent-id")) requirement.parent_id = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("name")) requirement.name = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("father-name")) requirement.father_name = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("req-priority")) requirement.req_priority = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-90")) requirement.user_90_sprint = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-06")) requirement.user_06_status = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("req-status")) requirement.req_status = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-27")) requirement.user_27_qa_lead = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-08")) requirement.user_08_dev_team = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-17")) requirement.user_17_dev_lead = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("owner")) requirement.owner_author = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-37")) requirement.user_37_qa_effort = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-28")) requirement.user_28_dev_effort = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-38")) requirement.user_38_FA = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-41")) requirement.user_41_FA_notes = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-66")) requirement.user_66_back_to_dev_count = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("last-modified")) requirement.last_modified_date = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("req-ver-stamp")) requirement.req_ver_stamp = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                    if (someName.Equals("user-97")) requirement.user_97_theme = DictionaryExtension.TryGetString(someValue[0], "value", out convertationResult);
                                }
                            }
                        }
                    }
                    requirementsList.Add(requirement);
                }
            }

            totalResults = DictionaryExtension.TryGetValue(data, "TotalResults", out convertationResult);
            return requirementsList;
        }

        #endregion GetRequrementAsync()

        public async Task<string> GetEntitiesAsync(string domain, string project, string entityType, int pageSize, int startIndex, string queryParameter)
        {
            WebRequest request = HttpWebRequest.Create(serverRootUrl + "/qcbin/rest/domains/" + domain + "/projects/" + project + "/" + entityType + "?" + "alt=application/json" + "&page-size=" + cPageSize.ToString() + "&start-index=" + startIndex.ToString() + queryParameter);
            request.Method = "GET";
            request.Headers.Add("Cookie:" + lwSsoCookieKey + ";" + qcSession + ";" + almUser + ";" + xsrfToken);

            WebResponse response = null;
            try
            {
                response = await request.GetResponseAsync();
                if (response != null)
                {
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    return streamReader.ReadToEnd().Trim();
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }
    }
}
