namespace MUnique.OpenMU.Web.PublicSite.Services;

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Executes parameterized read-only SQL queries against the game database and maps the rows.
/// </summary>
public static class DbQueryHelper
{
    /// <summary>
    /// Runs a query and maps each row with the given mapper.
    /// </summary>
    public static async Task<List<T>> QueryAsync<T>(
        string sql,
        Action<DbParameterCollection>? addParameters,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        await using var db = new EntityDataContext();
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        addParameters?.Invoke(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(map(reader));
        }

        return result;
    }
}
