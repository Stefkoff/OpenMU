// <copyright file="JewelLuckBuffService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The "Gem of Success" buff: for the duration of <see cref="Character.JewelLuckBuffEndsAt"/>
/// every jewel upgrade succeeds with 100% chance. The end time is stored on the character and
/// runs in real time (no pause while offline). The client indicator is a magic effect on its
/// own dedicated number, which stacks with every other buff and is invisible to other players.
/// </summary>
public static class JewelLuckBuffService
{
    /// <summary>
    /// The dedicated effect number; MUST be absent from the live config."MagicEffectDefinition"
    /// (checked in Task 0.2b), so this effect always has its own ActiveEffects slot and can
    /// never replace or be replaced by another buff.
    /// </summary>
    public const short GemEffectNumber = 150;

    /// <summary>The name of our effect definition; looked up by name for safety.</summary>
    public const string GemEffectName = "Gem of Success";

    /// <summary>Allows tests to control time; production uses UTC.</summary>
    public static Func<DateTime> Clock = () => DateTime.UtcNow;

    public static bool IsActive(Player player)
    {
        var character = player.SelectedCharacter;
        if (character?.JewelLuckBuffEndsAt is not { } endsAt)
        {
            return false;
        }

        if (endsAt > Clock())
        {
            return true;
        }

        character.JewelLuckBuffEndsAt = null; // expired; persists on next save
        _ = TryRemoveGemEffectAsync(player);  // fire-and-forget icon cleanup, guarded inside
        return false;
    }

    public static void Activate(Player player, double durationSeconds)
    {
        var character = player.SelectedCharacter!;
        character.JewelLuckBuffEndsAt = Clock().AddSeconds(durationSeconds);
        var definition = player.GameContext.Configuration.MagicEffects.FirstOrDefault(e => e.Name.ValueInNeutralLanguage == GemEffectName);
        if (definition is null)
        {
            return; // buff is already enforced via EndsAt; the icon is cosmetic
        }

        // Dedicated effect number => its own ActiveEffects slot, so this never replaces
        // or is replaced by any other buff (they all stack).
        _ = player.MagicEffectList.AddEffectAsync(new MagicEffect(TimeSpan.FromSeconds(durationSeconds), definition));
    }

    /// <summary>
    /// Re-adds the client indicator effect with the remaining real-time duration (login hook).
    /// Clears the column if the buff already expired while offline.
    /// </summary>
    public static async ValueTask RestoreEffectAsync(Player player)
    {
        var character = player.SelectedCharacter;
        if (character?.JewelLuckBuffEndsAt is not { } endsAt)
        {
            return;
        }

        var remaining = RemainingSeconds(endsAt, Clock());
        if (remaining <= 0)
        {
            character.JewelLuckBuffEndsAt = null;
            return;
        }

        var definition = player.GameContext.Configuration.MagicEffects.FirstOrDefault(e => e.Name.ValueInNeutralLanguage == GemEffectName);
        if (definition is not null)
        {
            await player.MagicEffectList.AddEffectAsync(new MagicEffect(TimeSpan.FromSeconds(remaining), definition)).ConfigureAwait(false);
        }
    }

    /// <summary>Remaining seconds between <paramref name="expireAt"/> and <paramref name="now"/>, clamped to 0.</summary>
    public static double RemainingSeconds(DateTime expireAt, DateTime now) => Math.Max(0, (expireAt - now).TotalSeconds);

    private static async ValueTask TryRemoveGemEffectAsync(Player player)
    {
        try
        {
            if (player.MagicEffectList.ActiveEffects.TryGetValue(GemEffectNumber, out var existing))
            {
                await existing.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            player.GameContext.LoggerFactory.CreateLogger(nameof(JewelLuckBuffService)).LogWarning(ex, "Failed to remove the gem of success effect.");
        }
    }
}
