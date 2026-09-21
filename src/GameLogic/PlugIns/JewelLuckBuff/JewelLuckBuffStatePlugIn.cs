// <copyright file="JewelLuckBuffStatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-adds the "Gem of Success" buff icon (dedicated effect number) with the remaining
/// real-time duration when a character enters the world. No action on disconnect: the
/// expiry runs on the persisted <see cref="Character.JewelLuckBuffEndsAt"/> clock
/// regardless of sessions.
/// </summary>
[Guid("7C9D2E4F-1A3B-4C5D-8E6F-0A1B2C3D4E5F")]
[PlugIn]
[Display(Name = nameof(PlugInResources.JewelLuckBuffStatePlugIn_Name), Description = nameof(PlugInResources.JewelLuckBuffStatePlugIn_Description), ResourceType = typeof(PlugInResources))]
public class JewelLuckBuffStatePlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        await JewelLuckBuffService.RestoreEffectAsync(player).ConfigureAwait(false);

        // Explicit indicator on login: the buff runs in real time, so a relog mid-buff
        // must tell the player it is still active and for how long.
        if (player.SelectedCharacter is { JewelLuckBuffEndsAt: { } endsAt })
        {
            var remaining = (int)Math.Ceiling(JewelLuckBuffService.RemainingSeconds(endsAt, JewelLuckBuffService.Clock()) / 60);
            if (remaining > 0)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.JewelLuckBuffStillActive), remaining).ConfigureAwait(false);
            }
        }
    }
}
