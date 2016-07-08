using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public class SLUser
    {
        public string id;
        public string name;
        public bool? deleted;
        public string status;
        public string color;
        public string real_name;
        public string tz;
        public string tz_label;
        public int? tz_offset;
        public SLProfile profile;
        public bool? is_admin;
        public bool? is_owner;
        public bool? is_primary_owner;
        public bool? is_restricted;
        public bool? is_ultra_restricted;
        public bool? is_bot;
        public bool? has_files;
        public bool? has_2fa;

        public SLUser()
        {
            profile = new SLProfile();
        }
        //"id": "U056NGXK5",
        //    "name": "alexander.zavaliy",
        //    "deleted": false,
        //    "status": null,
        //    "color": "e7392d",
        //    "real_name": "",
        //    "tz": "EET",
        //    "tz_label": "Eastern European Summer Time",
        //    "tz_offset": 10800,
        //    "profile": {
        //        "real_name": "",
        //        "real_name_normalized": "",
        //        "email": "alexander.zavaliy@hp.com",
        //        "image_24": "https:\/\/secure.gravatar.com\/avatar\/7f977b03334f1579aabb1312440f7a35.jpg?s=24&d=https%3A%2F%2Fslack.global.ssl.fastly.net%2F3654%2Fimg%2Favatars%2Fava_0011-24.png",
        //        "image_32": "https:\/\/secure.gravatar.com\/avatar\/7f977b03334f1579aabb1312440f7a35.jpg?s=32&d=https%3A%2F%2Fslack.global.ssl.fastly.net%2F3654%2Fimg%2Favatars%2Fava_0011-32.png",
        //        "image_48": "https:\/\/secure.gravatar.com\/avatar\/7f977b03334f1579aabb1312440f7a35.jpg?s=48&d=https%3A%2F%2Fslack.global.ssl.fastly.net%2F3654%2Fimg%2Favatars%2Fava_0011-48.png",
        //        "image_72": "https:\/\/secure.gravatar.com\/avatar\/7f977b03334f1579aabb1312440f7a35.jpg?s=72&d=https%3A%2F%2Fslack.global.ssl.fastly.net%2F3654%2Fimg%2Favatars%2Fava_0011-72.png",
        //        "image_192": "https:\/\/secure.gravatar.com\/avatar\/7f977b03334f1579aabb1312440f7a35.jpg?s=192&d=https%3A%2F%2Fslack.global.ssl.fastly.net%2F3654%2Fimg%2Favatars%2Fava_0011.png"
        //    },
        //    "is_admin": false,
        //    "is_owner": false,
        //    "is_primary_owner": false,
        //    "is_restricted": false,
        //    "is_ultra_restricted": false,
        //    "is_bot": false,
        //    "has_files": true,
        //    "has_2fa": false
    }
}
