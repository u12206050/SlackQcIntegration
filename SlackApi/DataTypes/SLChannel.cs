using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLChannel
    {
        public string id;
        public string name;
        public bool? is_channel;
        public long? created;
        public string creator_id;
        public bool? is_archived;
        public List<string> members_ids;
        public SLTopic topic;
        public SLPurpose purpose;

        public SLChannel()
        {
            members_ids = new List<string>();
            topic = new SLTopic();
            purpose = new SLPurpose();
        }
    }
}
