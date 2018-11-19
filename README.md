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

## Deploying to Azure

* *Coming soon*

---

## Features

### Send Email

The send email function is responsible for get a request of an email data, store it into a queue and send it later using a Send Grid account.

---
