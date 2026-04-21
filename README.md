# Azure Blob to AI Search Indexer Trigger

This repository contains an Azure Functions project that listens for **Azure Blob Storage** creation events through **Event Grid** and triggers an **Azure AI Search indexer** run.

The solution is intended for document ingestion pipelines where newly uploaded files in Blob Storage should be indexed automatically for search or RAG scenarios.

## How it works

1. A file is uploaded to an Azure Blob Storage container.
2. Event Grid emits a `BlobCreated` event.
3. The Azure Function receives the event.
4. The function calls the Azure AI Search REST API to trigger the configured indexer.
5. If the indexer is already running or the service rejects frequent on-demand runs, the function skips the duplicate trigger gracefully.

## Features

- Event-driven indexing workflow
- Azure Blob Storage + Event Grid integration
- Azure AI Search indexer execution via REST API
- Basic duplicate-trigger protection
- Simple configuration through environment variables

## Project structure

- `TriggerIndexerOnBlobUpdate.cs` – Azure Function that handles `BlobCreated` events and triggers the search indexer

## Requirements

Before running this project, make sure you have:

- [.NET SDK](https://dotnet.microsoft.com/) installed
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) installed
- An Event Grid subscription for the `Microsoft.Storage.BlobCreated` event
- An Azure Storage account with Event Grid events enabled
- An Azure AI Search service
- A configured Azure AI Search indexer
- Valid Azure AI Search admin key

## Configuration

## Event Grid requirement

This solution also requires an **Event Grid subscription** on the Azure Blob Storage account.

The subscription must be configured to listen for the specific event type:

- `Microsoft.Storage.BlobCreated`

Without this event subscription, the Azure Function will not be triggered when new blobs are uploaded.

## Environment variables

Set the following application settings in `local.settings.json` for local development or in Azure Function App configuration for deployment:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SEARCH_SERVICE_NAME": "<your-search-service-name>",
    "SEARCH_INDEXER_NAME": "<your-indexer-name>",
    "SEARCH_ADMIN_KEY": "<your-search-admin-key>"
  }
}
