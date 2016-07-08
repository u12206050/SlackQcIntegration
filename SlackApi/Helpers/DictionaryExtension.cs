using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slack
{
    public static class DictionaryExtension
    {
        public static TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey value, out bool found)
        {
            if (dict.ContainsKey(value))
            {
                TValue result;
                found = dict.TryGetValue(value, out result);
                return result;
            }
            found = false;
            return default(TValue);
        }
    }
}
