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
    public ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState == PlayerState.EnteredWorld)
        {
            return JewelLuckBuffService.RestoreEffectAsync(player);
        }

        return ValueTask.CompletedTask;
    }
}
