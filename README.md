# LambdaSharp.Slack Setup

This tutorial shows how to create an asynchronous [Slack Command](https://api.slack.com/slash-commands) handler in C# using [.Net Core](https://dotnet.github.io).

## Overview

C# Lambda functions can be slow to respond on cold start. This can be a problem when they are used to respond to Slack requests which time-out after only 3 seconds. The solution is to funnel the Slack request as an event to the Lambda function and create a fake successful empty response. The following steps show you how to achieve just that.

1. [Create an API Gateway](#1-create-an-api-gateway)
2. [Create an API Resource](#2-create-an-api-resource)
3. [Publish the Lambda Function](#3-publish-the-lambda-function)
4. [Invoke the Lambda Function from the API](#4-invoke-the-lambda-function-from-the-api-resource)
5. [Deploy the API Gateway](#5-deploy-the-api-gateway)
6. [Setup the Slack Integration](#6-setup-the-slack-integration)

The AWS setup is shown as [AWS CLI](https://docs.aws.amazon.com/cli/latest/reference/index.html) commands that can be run from a `bash` shell.

Before we begin, make note of the AWS region you want the code to run in (e.g. `us-east-1`). It will be needed in later steps as `$AWS_REGION`.

## 1. Create an API Gateway

First, we need to create an API Gateway which we will use to invoke our Lambda function from Slack. For the purpose of this tutorial, we will christen the API Gateway `SlackDemo`.

```bash
aws apigateway create-rest-api \
    --name 'SlackDemo'
```

Make note of the API Gateway ID in the response. It will be needed in later steps as `$API_ID`.

## 2. Create an API Resource

Next, we need to find out the root resource ID in our API Gateway instance. The following command queries all resources, but only shows the root resource with a `path` value of `"/"`.

```bash
aws apigateway get-resources \
    --rest-api-id $API_ID \
    --query "items[?path=='/'] | [0]"
```

Make note of the root resource ID. It will be needed in later steps as `$ROOT_ID`.

Now, let's create a new resource at `/slack` using the root resource ID.

```bash
aws apigateway create-resource \
    --rest-api-id $API_ID \
    --parent-id $ROOT_ID \
    --path-part 'slack'
```

Make note of the resource ID. It will be needed in later steps as `$RESOURCE_ID`.

## 3. Publish the Lambda Function

Now, let's build and deploy our C# Lambda function as `slackdemo`.

```bash
cd src/SlackDemo/
dotnet lambda deploy-function
```

Unfortunately, the `dotnet` tool does not show the ARN of the deployed Lambda function, so we will need to query it separately.

```bash
aws lambda get-function \
    --function 'slackdemo'
```

Make note of the Lambda ARN. It will be needed in later as `$LAMBDA_ARN`.

## 4. Invoke the Lambda Function from the API Resource

We need an IAM role that is allowed to invoke our Lambda function. It will be needed by the resource when we setup the invocation. Let's create a new IAM role called `slackdemo-endpoint`.

```bash
aws iam create-role \
    --role-name 'slackdemo-endpoint' \
    --assume-role-policy-document "{ \"Version\": \"2008-10-17\", \"Statement\": [{ \"Effect\": \"Allow\", \"Principal\": { \"Service\": \"apigateway.amazonaws.com\" }, \"Action\": \"sts:AssumeRole\" }]}"
```

Make note of the role ARN. It will be needed in later as `$ENDPOINT_ARN`.

Now, let's grant the newly created IAM role permission to invoke our Lambda function.

```bash
aws iam put-role-policy \
    --role-name 'slackdemo-endpoint' \
    --policy-name 'InvokeEndPointLambda' \
    --policy-document "{ \"Version\": \"2012-10-17\", \"Statement\": [{ \"Effect\": \"Allow\", \"Action\": [ \"lambda:InvokeFunction\" ], \"Resource\": [ \"$LAMBDA_ARN\" ]}]}"
```

Next, we add a `POST` method to our API resource to respond to Slack requests. 

```bash
aws apigateway put-method \
    --rest-api-id $API_ID \
    --resource-id $RESOURCE_ID \
    --http-method 'POST' \
    --authorization-type 'NONE' \
    --no-api-key-required
```

This step is where the asynchronous magic happens. Instead of wiring the API resource method to invoke the Lambda function directly, we configure it to do a few things for us. First, we add the the `X-Amz-Invocation-Type` custom HTTP header with the value `'Event'` to indicate we want our Lambda function to be invoked asynchronously. Second, we use a request mapping template to convert the form URL-encoded payload sent by Slack into a nice JSON document that can be understood by our Lambda function.

```bash
aws apigateway put-integration \
    --rest-api-id $API_ID \
    --resource-id $RESOURCE_ID \
    --http-method 'POST' \
    --type 'AWS' \
    --integration-http-method 'POST' \
    --credentials $ENDPOINT_ARN \
    --uri "arn:aws:apigateway:$AWS_REGION:lambda:path//2015-03-31/functions/$LAMBDA_ARN/invocations" \
    --request-parameters "{ \"integration.request.header.X-Amz-Invocation-Type\": \"'Event'\" }" \
    --request-templates file://api-gateway-request-mapping.json \
    --passthrough-behavior 'NEVER'
```

The `api-gateway-request-mapping.json` file contains the [Apache Velocity](http://velocity.apache.org/) rules for converting a form URL-encoded payload into a JSON document. Althought the syntax is an acquired taste, the logic is straightforward.

Mapping for `application/x-www-form-urlencoded`
```
{
    #foreach($token in $input.path('$').split('&'))
        #set($keyVal = $token.split('='))
        #set($keyValSize = $keyVal.size())
        #if($keyValSize == 2)
            #set($key = $util.escapeJavaScript($util.urlDecode($keyVal[0])))
            #set($val = $util.escapeJavaScript($util.urlDecode($keyVal[1])))
            "$key": "$val"#if($foreach.hasNext),#end
        #end
    #end
}
```

In addition to the request mapping, we also need to create a proper response for our asynchronous invocation. Since our Lambda function is invoked without waiting for a response, we need to provide one. Fortunately, Slack accepts and ignores empty messages. This allows us to respond later from the Lambda function without causing artifacts in the Slack channel.

In short, this is the response we want to send for every Slack request.
```json
{
    "response_type": "in_channel",
    "text": ""
}
```

Since every asynchronous Lambda invocation is successful (status code 200), we just need to map a hard-coded response for it.

```bash
aws apigateway  put-integration-response \
    --rest-api-id $API_ID \
    --resource-id $RESOURCE_ID \
    --http-method 'POST' \
    --status-code '200'  \
    --response-templates "{ \"application/json\": \"{\\r\\n    \\\"response_type\\\": \\\"in_channel\\\",\r\n    \\\"text\\\": \\\"\\\"\\r\\n}\" }"
```

Finally, we need to let ApiGateway know that `application/json` responses are valid and don't need to be schema checked.

```bash
aws apigateway put-method-response \
    --rest-api-id $API_ID \
    --resource-id $RESOURCE_ID \
    --http-method 'POST' \
    --status-code '200'  \
    --response-models "{ \"application/json\": \"Empty\" }"
```

## 5. Deploy the API Gateway

This is the step that gets me every time. You **HAVE TO** deploy any changes to the API before they become accessible.

```bash
aws apigateway create-deployment \
    --rest-api-id $API_ID \
    --stage-name 'prod'
```

## 6. Setup the Slack Integration

In order to invoke our API resource endpoint, we need to know the URL. Unfortunately, there is now AWS CLI command to print it, but it can be constructed by using `$API_ID` and `$AWS_REGION`.

```bash
echo https://$API_ID.execute-api.$AWS_REGION.amazonaws.com/prod/slack
```

Copy the complete URL to the Lambda function and follow these steps:
1. Select *Customize Slack* from your Slack client.
2. Click *Configure Apps*.
3. Click *Custom Integrations*.
4. Click *Slack Commands*.
5. Click *Add Configuration*.
6. Enter `slackdemo` as command.
7. Click *Add Slash Command Integration*.
8. Paste the Lambda function URL into the appropriate box.
9. Click *Save Integration*.
10. Go back to your Slack client.
11. Click on `slackbot`.
12. Type `/slackdemo` and hit **ENTER**.

If all went well, you should see `Hello World!` as response (which may take a few seconds).

# License

MIT License

Copyright (c) 2018 λ#

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
