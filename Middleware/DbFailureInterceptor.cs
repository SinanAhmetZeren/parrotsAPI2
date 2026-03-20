using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

public class DbFailureInterceptor : DbCommandInterceptor
{
    // Override both Sync and Async
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
        if (ex is PostgresException pgEx && IsTransient(pgEx))
        {
            throw new DbUnavailableException("Database unavailable", pgEx);
        }
    }

    private bool IsTransient(PostgresException ex)
    {
        // Postgres transient codes as strings
        string[] transientCodes = { "53300", "53400", "57P01", "57P02", "57P03" };
        return transientCodes.Contains(ex.SqlState);
    }
}