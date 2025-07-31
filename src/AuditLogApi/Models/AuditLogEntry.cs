using System;

namespace AuditLogApi.Models;

internal sealed class AuditLogEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    public required string ActorId { get; internal set; }
    public string Action { get; private set; }
    public string Resource { get; private set; }
    public string Result { get; private set; }

    public string ServiceName { get; private set; }
    public string? CorrelationId { get; private set; }



    public string? ExtraData { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime TimeStamp { get; private set; }
    


}
