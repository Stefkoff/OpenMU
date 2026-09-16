namespace MUnique.OpenMU.Web.PublicSite.Services;

using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Reads ranking data directly from the game database with a limited-privilege read-only path.
/// Character levels, resets and master levels are stat-attribute rows; bot accounts are excluded.
/// </summary>
public sealed class RankingService
{
    private const string CharacterStatsSql = """
        SELECT c."Name" AS "CharacterName",
               cc."Name" AS "ClassName",
               COALESCE(lv."Value", 0)::int AS "Level",
               COALESCE(rs."Value", 0)::int AS "Resets",
               COALESCE(ml."Value", 0)::int AS "MasterLevel"
        FROM data."Character" c
        JOIN config."CharacterClass" cc ON cc."Id" = c."CharacterClassId"
        LEFT JOIN LATERAL (
            SELECT sa."Value" FROM data."StatAttribute" sa
            JOIN config."AttributeDefinition" ad ON ad."Id" = sa."DefinitionId"
            WHERE sa."CharacterId" = c."Id" AND ad."Designation" = 'Level'
        ) lv ON true
        LEFT JOIN LATERAL (
            SELECT sa."Value" FROM data."StatAttribute" sa
            JOIN config."AttributeDefinition" ad ON ad."Id" = sa."DefinitionId"
            WHERE sa."CharacterId" = c."Id" AND ad."Designation" = 'Resets'
        ) rs ON true
        LEFT JOIN LATERAL (
            SELECT sa."Value" FROM data."StatAttribute" sa
            JOIN config."AttributeDefinition" ad ON ad."Id" = sa."DefinitionId"
            WHERE sa."CharacterId" = c."Id" AND ad."Designation" = 'Master Level'
        ) ml ON true
        WHERE c."AccountId" IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM data."Account" a WHERE a."Id" = c."AccountId" AND a."IsBot"
          )
        """;

    private const string GuildRankingSql = """
        SELECT g."Name" AS "GuildName",
               g."Score" AS "Score",
               COUNT(gm."Id") AS "Members"
        FROM guild."Guild" g
        LEFT JOIN guild."GuildMember" gm ON gm."GuildId" = g."Id"
        GROUP BY g."Id"
        ORDER BY g."Score" DESC
        """;

    private readonly IOptions<SiteSettings> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankingService"/> class.
    /// </summary>
    public RankingService(IOptions<SiteSettings> settings)
    {
        this._settings = settings;
    }

    /// <summary>
    /// Gets the characters sorted by level.
    /// </summary>
    public Task<List<CharacterStats>> GetTopByLevelAsync(CancellationToken cancellationToken)
        => this.GetCharacterStatsAsync("lv.\"Value\" DESC NULLS LAST", cancellationToken);

    /// <summary>
    /// Gets the characters sorted by resets.
    /// </summary>
    public Task<List<CharacterStats>> GetTopByResetsAsync(CancellationToken cancellationToken)
        => this.GetCharacterStatsAsync("rs.\"Value\" DESC NULLS LAST", cancellationToken);

    /// <summary>
    /// Gets the characters sorted by master level.
    /// </summary>
    public Task<List<CharacterStats>> GetTopByMasterLevelAsync(CancellationToken cancellationToken)
        => this.GetCharacterStatsAsync("ml.\"Value\" DESC NULLS LAST", cancellationToken);

    /// <summary>
    /// Gets the characters of a single account.
    /// </summary>
    public Task<List<CharacterStats>> GetCharactersOfAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var top = Math.Max(1, this._settings.Value.RankingSize);
        return DbQueryHelper.QueryAsync(
            $"""
             {CharacterStatsSql}
             AND c."AccountId" = @accountId
             ORDER BY lv."Value" DESC NULLS LAST
             LIMIT {top}
             """,
            command =>
            {
                command.Add(new Npgsql.NpgsqlParameter("accountId", accountId));
            },
            CharacterStats.FromReader,
            cancellationToken);
    }

    /// <summary>
    /// Gets the guild ranking.
    /// </summary>
    public Task<List<GuildRankRow>> GetTopGuildsAsync(CancellationToken cancellationToken)
    {
        var top = Math.Max(1, this._settings.Value.RankingSize);
        return DbQueryHelper.QueryAsync(
            $"{GuildRankingSql} LIMIT {top}",
            null,
            GuildRankRow.FromReader,
            cancellationToken);
    }

    private Task<List<CharacterStats>> GetCharacterStatsAsync(string orderBy, CancellationToken cancellationToken)
    {
        var top = Math.Max(1, this._settings.Value.RankingSize);
        return DbQueryHelper.QueryAsync(
            $"{CharacterStatsSql} ORDER BY {orderBy} LIMIT {top}",
            null,
            CharacterStats.FromReader,
            cancellationToken);
    }
}
