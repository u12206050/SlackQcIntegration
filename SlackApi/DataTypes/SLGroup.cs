using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLGroup
    {
        public string id;
        public string name;
        public bool? is_group;
        public long? created;
        public string creator_id;
        public bool? is_archived;
        public List<string> members_ids;
        public SLTopic topic;
        public SLPurpose purpose;

        public SLGroup()
        {
            members_ids = new List<string>();
            topic = new SLTopic();
            purpose = new SLPurpose();
        }
    }
}
