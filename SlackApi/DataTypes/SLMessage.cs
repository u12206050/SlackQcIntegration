using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLMessage
    {
        public string type;
        public string subtype;
        public string text;
        public SLFile file_description;
        public string user_id;
        public bool? upload;
        public string ts;

        public SLMessage()
        {
            file_description = new SLFile();
        }
    }
}
