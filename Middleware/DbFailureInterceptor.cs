using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Data.Common;

public class DbFailureInterceptor : DbCommandInterceptor
{
    // Override both Sync and Async to ensure no failures are missed
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        CheckAndThrow(eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken ct = default)
    {
        CheckAndThrow(eventData.Exception);
        return base.CommandFailedAsync(command, eventData, ct);
    }

    private void CheckAndThrow(Exception ex)
    {
        if (ex is SqlException sqlEx && IsTransient(sqlEx))
        {
            throw new DbUnavailableException("Database unavailable", sqlEx);
        }
    }

    private bool IsTransient(SqlException ex)
    {
        // Added 18456 (Login failed/Firewall) and 258 (Wait timeout)
        int[] codes = { 40613, 40501, 49918, 49919, 49920, 11001, 4060, 18456, 258 };
        return codes.Contains(ex.Number);
    }
}