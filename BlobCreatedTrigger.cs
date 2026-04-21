using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
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

        if (string.IsNullOrWhiteSpace(storageConnectionString))
            throw new InvalidOperationException("AzureWebJobsStorage is empty");

        if (string.IsNullOrWhiteSpace(queueName))
            throw new InvalidOperationException("INDEXER_TRIGGER_QUEUE is empty");

        _queueClient = new QueueClient(storageConnectionString, queueName,
        new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
    }

    [Function(nameof(BlobCreatedTrigger))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            _logger.LogInformation("Function was triggered");
            await _queueClient.CreateIfNotExistsAsync();
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
        catch (Exception ex)
        {
            _logger.LogError("Blob trigger failed: {ex}", ex);
        }
    }
}
