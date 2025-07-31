using System.Net;
using APIMonitor.server.Models;

namespace APIMonitor.server.ViewModels;

public class ScanResponse
{
    public ApiMetrics Metrics { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public string ResponseSnippet { get; set; }
}