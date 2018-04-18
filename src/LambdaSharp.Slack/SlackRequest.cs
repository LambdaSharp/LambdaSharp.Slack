/*
 * MIT License
 * 
 * Copyright (c) 2018 λ#
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text;
using Newtonsoft.Json;

namespace LambdaSharp.Slack {

    public class SlackRequest {

        //--- Fields ---
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("team_id")]
        public string TeamId;

        [JsonProperty("team_domain")]
        public string TeamDomain;

        [JsonProperty("enterprise_id")]
        public string EnterpriseId;

        [JsonProperty("enterprise_name")]
        public string EnterpriseName;

        [JsonProperty("channel_id")]
        public string ChannelId;

        [JsonProperty("channel_name")]
        public string ChannelName;

        [JsonProperty("user_id")]
        public string UserId;

        [JsonProperty("user_name")]
        public string UserName;

        [JsonProperty("command")]
        public string Command;

        [JsonProperty("text")]
        public string Text;

        [JsonProperty("response_url")]
        public string ResponseUrl;

        //--- Methods ---
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"Token: ###");
            sb.AppendLine($"TeamId: {TeamId}");
            sb.AppendLine($"TeamDomain: {TeamDomain}");
            sb.AppendLine($"ChannelId: {ChannelId}");
            sb.AppendLine($"ChannelName: {ChannelName}");
            sb.AppendLine($"UserId: {UserId}");
            sb.AppendLine($"UserName: {UserName}");
            sb.AppendLine($"Command: {Command}");
            sb.AppendLine($"Text: {Text}");
            sb.AppendLine($"ResponseUrl: {ResponseUrl}");
            return sb.ToString();
        }
    }
}
