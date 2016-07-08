using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLFileUploadResult
    {
        public bool? ok;
        public SLFile slFile;
        public SLFileUploadResult()
        {
            slFile = new SLFile();
        }
    }
}
