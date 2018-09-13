# LambdaSharp.Slack

Creating a [Slack Command](https://api.slack.com/slash-commands) handler is super easy with `LambdaSharp.Slack`. The abstract base class (`ASlackFunction`) implements the deserialization of Slack requests and provides helper methods to send in-channel and ephemeral responses.

## Getting Started

First add the `LambdaSharp.Slack` nuget package to your project.
```bash
dotnet add LambdaSharp.Slack
```

```csharp
using System;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using LambdaSharp.Slack;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SlackDemo {

    public class Function : ASlackFunction {

        //--- Methods ---
        protected override Task HandleMessageAsync(SlackRequest request) {
            Console.WriteLine("Hello World!");
            return Task.CompletedTask;
        }
    }
}
```