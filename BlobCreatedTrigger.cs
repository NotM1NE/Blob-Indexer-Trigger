using System.Net.Http.Headers;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using System.Net;
using BlobCreatedIndexerRunner.Models;
using System.Text.Json;

namespace BlobCreatedIndexerRunner;

public class BlobCreatedTrigger
{
    private readonly ILogger<BlobCreatedTrigger> _logger;
    private readonly QueueClient _queueClient;

    public BlobCreatedTrigger(ILogger<BlobCreatedTrigger> logger, IConfiguration config)
    {
        _logger = logger;

        var storageConnectionString = config["AzureWebJobsStorage"];
        var queueName = config["INDEXER_TRIGGER_QUEUE"] ?? "indexer-trigger-queue";

        _queueClient = new QueueClient(storageConnectionString, queueName);

    }

    [Function(nameof(BlobCreatedTrigger))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation("Function was triggered");
        _logger.LogInformation("BlobCreated event recieved. Event type: {type}, Event subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);

        if (!string.Equals(eventGridEvent.EventType, "Microsoft.Storage.BlobCreated", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Skipping unsupported event type: {type}", eventGridEvent.EventType);
            return;
        }

        var message = new BlobCreatedMessage
        (
            eventGridEvent.Id,
            eventGridEvent.Subject,
            eventGridEvent.EventType,
            eventGridEvent.EventTime.UtcDateTime
        );

        var json = JsonSerializer.Serialize(message);

        await _queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)));

        _logger.LogInformation("BlobCreated event queued. Event type: {type}, Event subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);
    }

}
