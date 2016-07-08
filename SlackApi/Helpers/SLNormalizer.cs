using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public static class SLNormalizer
    {
        public static string EscapeJSONString(string str)
        {
            if (str != null)
            {
                str = str.Replace("\\", @"\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("/", "\\/").Replace("\b", "\\b").Replace("\f", "\\f").Replace("\"", "\\\"");
            }
            return str;
        }

        public static string Normalize(string str)
        {
            if (str != null)
            {
                //str = str.Replace("\"", "'");
                str = str.Replace("&", "&amp;");
                str = str.Replace("<", "&lt;");
                str = str.Replace(">", "&gt;");
                //str = str.Replace("\n", "\\n");
            }
            return str;
        }

        public static string Denormalize(string str)
        {
            if (str != null)
            {
                str = str.Replace("<", "");
                str = str.Replace(">", "");
                str = str.Replace("&lt;", "<");
                str = str.Replace("&gt;", ">");
                str = str.Replace("&amp;", "%26");
                str = str.Replace("#", "%23");
            }
            return str;
        }
    }
}
