using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLRuntimeEventArgs : EventArgs 
    {
        public bool? ok;
        public int? reply_to;
        public string type;
        public string channel;
        public string user;
        public string text;
        public string ts;
        public string team;
    }
}
