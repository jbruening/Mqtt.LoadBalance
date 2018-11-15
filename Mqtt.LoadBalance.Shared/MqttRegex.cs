using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mqtt.LoadBalance
{
    //based on https://github.com/RangerMauve/mqtt-regex/blob/master/index.js#L34
    class MqttRegex
    {
        public static Regex TopicRegex(string sub)
        {
            var tokens = sub.Split('/');
            var sb = new StringBuilder("^");
            for (var i = 0; i < tokens.Length; i++)
            {
                var last = i == tokens.Length - 1;
                var token = tokens[i];
                if (token[0] == '+')
                    sb.Append(PlusRegex(last));
                else if (token[0] == '#')
                    sb.Append(HashRegex(last));
                else
                    sb.Append(RawRegex(token, last));
            }
            sb.Append("$");

            return new Regex(sb.ToString());
        }

        private static string RawRegex(string token, bool last)
            => last ? token + @"\/?" : token + @"\/";

        private static string PlusRegex(bool last)
            => last ? @"([^/#+]+)\/?" : @"([^/#+]+)\/";

        private static string HashRegex(bool last)
        {
            if (!last)
                throw new Exception("hashes are only allowed at the end of topics");
            return @"((?:[^/#+]+\/?)*)";
        }
    }
}
