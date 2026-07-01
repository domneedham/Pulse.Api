using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

/// <summary>
/// Assigns every active connection its daily Moment (today + tomorrow, in each connection's timezone).
/// Ticks hourly because couples roll over at different local midnights — an hourly sweep catches each
/// one shortly after its local day changes. Assignment is idempotent (unique (connection, date) index),
/// so overlapping ticks or the on-demand fallback in <see cref="MomentService"/> can't double-create.
///
/// TODO(push): when a NEW today's Moment is created here, send the couple a "today's Moment is ready"
/// push via <c>IPushNotificationSender</c> (the device registration + sender already exist).
/// </summary>
public class MomentAssignmentJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MomentAssignmentJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A short initial delay lets the app finish starting (and migrations apply) before the first run.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Moment assignment sweep failed; will retry next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var assignment = scope.ServiceProvider.GetRequiredService<MomentAssignment>();

        var connections = await db.Connections
            .Where(c => c.Status == ConnectionStatus.Active && c.UserBId != null)
            .ToListAsync(ct);

        var assigned = 0;
        foreach (var connection in connections)
        {
            try
            {
                await assignment.EnsureScheduledAsync(connection, ct);
                assigned++;
            }
            catch (Exception ex)
            {
                // One bad connection (e.g. an empty pool) shouldn't stop the sweep.
                logger.LogWarning(ex, "Could not schedule Moments for connection {ConnectionId}.", connection.Id);
            }
        }

        logger.LogInformation("Moment assignment sweep complete: {Count} connection(s) ensured.", assigned);
    }
}
