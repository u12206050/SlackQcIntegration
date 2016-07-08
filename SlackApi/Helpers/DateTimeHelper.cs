using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public static class DateTimeHelper
    {
        public static DateTime? DateTimeFromSLMessage(SLMessage message)
        {
            string timestamp = message.ts.Substring(0, message.ts.IndexOf('.'));
            DateTime initialDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long timeInterval = 0;

            if (long.TryParse(timestamp, out timeInterval))
            {
                return initialDateTime = initialDateTime.AddSeconds(timeInterval).ToLocalTime();
            }
            return null;
        }
    }
}
