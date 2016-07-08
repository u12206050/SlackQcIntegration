using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLChatPostMessageResult
    {
        public bool? ok;
        public string channel_id;
        public string ts;
        public SLMessage message;
        public SLChatPostMessageResult()
        {
            message = new SLMessage();
        }
    }
}
