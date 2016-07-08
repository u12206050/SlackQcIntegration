using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ALM;

namespace ALM
{
    public static class ALMNormalizer
    {
        public static List<ALMRequirement> Normalize(List<ALMRequirement> requirements)
        {
            foreach (ALMRequirement req in requirements)
            {
                if (req.user_27_qa_lead == null) req.user_27_qa_lead = "_null";
                if (req.user_17_dev_lead == null) req.user_17_dev_lead = "_null";
                if (req.name == null) req.name = "_null";
                if (req.user_97_theme == null) req.user_97_theme = "_null";
                req.name = ReplaceEscapeCharacters(req.name);
                req.user_97_theme = ReplaceEscapeCharacters(req.user_97_theme);
            }
            return requirements;
        }

        private static string ReplaceEscapeCharacters(string str)
        {
            if (str != null)
            {
                str = str.Replace("\"", "&quot;");
                str = str.Replace("&", "&amp;");
                str = str.Replace("<", "&lt;");
                str = str.Replace(">", "&gt;");
            }
            return str;
        }
    }
}