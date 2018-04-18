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

using System;
using System.Text;
using Newtonsoft.Json;

namespace LambdaSharp.Slack {

    public class SlackResponse {

        //--- Types ---

        //--- Class Methods ---
        public static SlackResponse InChannel(string text, params SlackResponseAttachment[] attachments) {
            return new SlackResponse("in_channel", text, attachments);
        }

        public static SlackResponse Ephemeral(string text, params SlackResponseAttachment[] attachments) {
            return new SlackResponse("ephemeral ", text, attachments);
        }

        //--- Fields ---
        [JsonProperty("response_type")]
        public readonly string ResponseType;

        [JsonProperty("text")]
        public readonly string Text;

        [JsonProperty("attachments")]
        public readonly SlackResponseAttachment[] Attachments;

        //--- Constructors ---
        private SlackResponse(string responseType, string text, SlackResponseAttachment[] attachments) {
            if(string.IsNullOrWhiteSpace(responseType)) {
                throw new ArgumentException("Argument is null or whitespace", nameof(responseType));
            }
            this.ResponseType = responseType;
            this.Text = text ?? "";
            this.Attachments = attachments;
        }

        //--- Methods ---
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine(Text);
            if(Attachments != null) {
                foreach(var attachment in Attachments) {
                    sb.AppendLine(attachment.Text);
                }
            }
            return sb.ToString();
        }
    }
}
