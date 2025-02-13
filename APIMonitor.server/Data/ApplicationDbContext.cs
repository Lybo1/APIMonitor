using APIMonitor.server.Identity;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }
    
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<ApiMetrics> ApiMetrics { get; set; }
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
    public DbSet<EventLog> EventLogs { get; set; }
    public DbSet<IpBlock> IpBlocks { get; set; }
    public DbSet<RequestStatistics> RequestStatistics { get; set; }
    public DbSet<ThreatAlert> ThreatAlerts { get; set; }
    public DbSet<TokenResponse> TokenResponses { get; set; }
}