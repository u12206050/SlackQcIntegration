using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLFile
    {
        public string id;
        public long? created;
        public long? timestamp;
        public string name;
        public string title;
        public string mimetype;
        public string filetype;
        public string user_id;
        //public string pretty_type;
        //public bool? editable;
        //public int? size;
        //public string mode;
        //public bool? is_external;
        //public string external_type;
        //public bool is_public;
        //public bool public_url_shared;
        //public string url;
        //public string url_download;
        //public string url_private;
        //public string url_private_download;
        //public string permalink;
        //public string permalink_public;
        //public string edit_link;
        //public string preview;
        //public string preview_highlight;
        //public int? lines;
        //public int? lines_more;
        //public List<string> channels_ids;
        //public List<string> groups_ids;
        //public List<string> ims_ids;
        //public int? comments_count;

        public SLFile()
        {
            //channels_ids = new List<string>();
            //groups_ids = new List<string>();
            //ims_ids = new List<string>();
        }
    }
}
