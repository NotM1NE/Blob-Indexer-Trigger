using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlobCreatedIndexerRunner.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlobCreatedIndexerRunner;

public class IndexerRunnerFromQueue
{
    private readonly ILogger<IndexerRunnerFromQueue> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public IndexerRunnerFromQueue(ILogger<IndexerRunnerFromQueue> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }
    [Function(nameof(IndexerRunnerFromQueue))]
    public async Task Run([QueueTrigger("%INDEXER_TRIGGER_QUEUE%", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("IndexerRunnerFromQueue triggered.");
        _logger.LogInformation("Raw queue message: {queueMessage}", queueMessage);

        BlobCreatedMessage? message = null; 
        _logger.LogInformation("IndexerRunnerFromQueue triggered. Raw message: {queueMessage}", queueMessage);

        message = JsonSerializer.Deserialize<BlobCreatedMessage>(queueMessage);

        _logger.LogInformation("Processing queued event. EventId: {eventId}, Subject: {subject}", message?.EventId, message?.Subject);

        var searchServiceName = _config["SEARCH_SERVICE_NAME"]?.Trim();
        var indexerName = _config["SEARCH_INDEXER_NAME"]?.Trim();
        var apiKey = _config["SEARCH_ADMIN_KEY"]?.Trim();

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

        _logger.LogInformation("Index started succesfully");

        try
        {

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}
