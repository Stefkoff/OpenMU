namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The ranking pages.
/// </summary>
public class RankingsModel : PageModel
{
    private readonly RankingService _rankingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankingsModel"/> class.
    /// </summary>
    public RankingsModel(RankingService rankingService)
    {
        this._rankingService = rankingService;
    }

    /// <summary>
    /// Gets the selected tab: level, resets, master or guilds.
    /// </summary>
    public string Tab { get; private set; } = "level";

    /// <summary>
    /// Gets the character ranking rows.
    /// </summary>
    public List<CharacterStats> Characters { get; private set; } = [];

    /// <summary>
    /// Gets the guild ranking rows.
    /// </summary>
    public List<GuildRankRow> Guilds { get; private set; } = [];

    /// <inheritdoc/>
    public async Task<IActionResult> OnGetAsync(string? tab, CancellationToken cancellationToken)
    {
        this.Tab = tab switch
        {
            "resets" => "resets",
            "master" => "master",
            "guilds" => "guilds",
            _ => "level",
        };

        if (this.Tab == "guilds")
        {
            this.Guilds = await this._rankingService.GetTopGuildsAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            this.Characters = this.Tab switch
            {
                "resets" => await this._rankingService.GetTopByResetsAsync(cancellationToken).ConfigureAwait(false),
                "master" => await this._rankingService.GetTopByMasterLevelAsync(cancellationToken).ConfigureAwait(false),
                _ => await this._rankingService.GetTopByLevelAsync(cancellationToken).ConfigureAwait(false),
            };
        }

        return this.Page();
    }
}
