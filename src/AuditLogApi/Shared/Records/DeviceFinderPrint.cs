namespace AuditLogApi.Shared.Records;

public readonly record struct DeviceFingerPrint(
    string IpAddress,
    string? Ipv6Address,
    string? MacAddress,
    string? Hostname,
    string? Country,
    string? City,
    string? UserAgent
);
