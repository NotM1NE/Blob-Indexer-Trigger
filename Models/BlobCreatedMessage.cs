using System;

namespace BlobCreatedIndexerRunner.Models;

public record BlobCreatedMessage
(
    string EventId,
    string Subject,
    string EventType,
    DateTime EventTimeUtc
);
