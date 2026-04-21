// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System.Net.Http.Headers;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BlobCreatedIndexerRunner;

public class TriggerIndexerOnBlobUpdate
{
    private readonly ILogger<TriggerIndexerOnBlobUpdate> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public TriggerIndexerOnBlobUpdate(ILogger<TriggerIndexerOnBlobUpdate> logger, IConfiguration config)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _config = config;
    }

    [Function(nameof(TriggerIndexerOnBlobUpdate))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            _logger.LogInformation("Function was triggered");
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);

            var searchServiceName = _config["SEARCH_SERVICE_NAME"]?.Trim();
            var indexerName = _config["SEARCH_INDEXER_NAME"]?.Trim();
            var apiKey = _config["SEARCH_ADMIN_KEY"]?.Trim();

            _logger.LogInformation("SEARCH_SERVICE_NAME: {service}", searchServiceName);
            _logger.LogInformation("SEARCH_INDEXER_NAME: {service}", indexerName);
            _logger.LogInformation("SEARCH_ADMIN_KEY: {service}", apiKey);

            if (string.IsNullOrEmpty(searchServiceName))
                throw new InvalidOperationException("SEARCH_SERVICE_NAME is empty");

            if (string.IsNullOrEmpty(indexerName))
                throw new InvalidOperationException("SEARCH_INDEXER_NAME is empty");

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("SEARCH_ADMIN_KEY is empty");

            var url = $"https://{searchServiceName}.search.windows.net/indexers/{indexerName}/run?api-version=2024-07-01";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Indexer run response status: {status}", response.StatusCode);
            _logger.LogInformation("Indexer run response body: {body}", responseBody);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Indexer already running, skipping duplicate trigger.");
                return;
            }
            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("Indexer already running, skipping duplicate trigger.");
                return;
            }


            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}