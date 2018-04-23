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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Newtonsoft.Json;


namespace LambdaSharp.Slack {

    public class SlackVerificationTokenMismatchException : Exception {

        //--- Constructors ---
        public SlackVerificationTokenMismatchException() : base("Slack verification token does not match") {}
    }

    public abstract class ASlackFunction {

        //--- Class Fields ---
        public static HttpClient HttpClient = new HttpClient();

        //--- Fields ---
        private readonly string _slackVerificationToken;

        //--- Constructors ---
        public ASlackFunction() {
            _slackVerificationToken = Environment.GetEnvironmentVariable("slack_token");
        }

        //--- Abstract Methods ---
        protected abstract Task HandleMessageAsync(SlackRequest request);

        //--- Methods ---
        public async Task FunctionHandler(SlackRequest request, ILambdaContext context) {
            LambdaLogger.Log($"*** INFO: request received\n{JsonConvert.SerializeObject(request)}\n");
            using(var consoleOutWriter = new StringWriter())
            using(var consoleErrorWriter = new StringWriter()) {
                var consoleOutOriginal = Console.Out;
                var consoleErrorOriginal = Console.Error;
                try {

                    // redirect the console output and error streams so we can emit them later to slack
                    Console.SetOut(consoleOutWriter);
                    Console.SetError(consoleErrorWriter);

                    // validate the slack token (assuming one was configured)
                    if(!(_slackVerificationToken?.Equals(request.Token) ?? true)) {
                        throw new SlackVerificationTokenMismatchException();
                    }

                    // handle slack request
                    await HandleMessageAsync(request);
                } catch(Exception e) {
                    LambdaLogger.Log($"*** EXCEPTION: {e.ToString()}\n");
                    Console.Error.WriteLine(e);
                } finally {
                    Console.SetOut(consoleOutOriginal);
                    Console.SetError(consoleErrorOriginal);
                }

                // send console output to slack as an in_channel response
                var output = consoleOutWriter.ToString();
                if(output.Length > 0) {
                    await RespondInChannel(request, output);
                }

                // send console error to slack as an ephemeral response (only visible to the requesting user)
                var error = consoleErrorWriter.ToString();
                if(error.Length > 0) {
                    await RespondEphemeral(request, error);
                }
            }
        }

        protected Task<bool> RespondInChannel(SlackRequest request, string text, params SlackResponseAttachment[] attachments) 
            => Respond(request, SlackResponse.InChannel(text, attachments));

        protected Task<bool> RespondEphemeral(SlackRequest request, string text, params SlackResponseAttachment[] attachments) 
            => Respond(request, SlackResponse.Ephemeral(text, attachments));

        protected async Task<bool> Respond(SlackRequest request, SlackResponse response) {
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri(request.ResponseUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
            });
            return httpResponse.StatusCode == HttpStatusCode.OK;
        }
    }
}