# SendiT

The ideia of this project is to send messages using Azure Functions and others Azure resources, taking advantage of the [serveless](https://azure.microsoft.com/solutions/serverless/) environment.

Messages are usually JSONs strings that needs to passed from one system to another, such as an integration with external systems or libraries.

## Prerequisites

* Install [Visual Studio 2017](https://azure.microsoft.com/downloads/) and ensure that the Azure development workload is also installed.
* Make sure you have the [latest Azure Functions tools](https://docs.microsoft.com/pt-br/azure/azure-functions/functions-develop-vs#check-your-tools-version).

Reference: [Create your first function using Visual Studio]("https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio).

## Running locally

Functions uses Azure Storage account internally, so you need to install [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) in order to run it in your local machine.
After installing the emulator, make sure you have a local.settings.json file on the root of your project, with the content bellow:

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

I recommend that you get the [Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) to be able to browse your local storage emulation.

---

## API key authorization
Most HTTP trigger templates require an API key in the request. So your HTTP request normally looks like the following URL:
```
https://<APP_NAME>.azurewebsites.net/api/<FUNCTION_NAME>?code=<API_KEY>
```
The key can be included in a query string variable named ```code```, as above. It can also be included in an ```x-functions-key``` HTTP header. The value of the key can be any function key defined for the function, or any host key.

## SendiT APIs

### Send Email

The send email function recieves a request of an email and store it into a queue to be sent through Send Grid.
```
/api/SendEmail
```

Request Json
```json
{
    "FromAddress":
    {
      "Email": "no-reply@mycompany.com",
      "Name": "My System Name"
    },
    "ToAddress":
    {
      "Email": "carlos@email.com",
      "Name": "Carlos Coelho"
    },
    "Subject": "Testing sendit",
    "Body": "This is just a test",
    "Origin": "TEST APP", //Caller's application name
    "Type": "Test Message" //Type of the message
}
```
Response Json
```json
{
    "trackerId": "11111111-2222-3333-4444-555555555555"
}
```

### SendGrid Hook

This API receives the status of each message that has been sent. Check the link for instructions on how to configure [SendGrid web hook events](https://sendgrid.com/docs/API_Reference/Event_Webhook/event.html). 
```
/api/SendGridHook
```

Request Json
```json
[
  {
    "email": "example@test.com",
    "timestamp": 1542650370,
    "smtp-id": "<14c5d00ce93.dfd.64b469@ismtpd-555>",
    "event": "bounce",
    "category": "cat facts",
    "sg_event_id": "40HcwXtOcz43-tqJf0O7tQ==",
    "sg_message_id": "14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0",
    "reason": "500 unknown recipient",
    "status": "5.0.0"
  }
]
