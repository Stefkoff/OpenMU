// <copyright file="OfflineStoreChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles the <c>/offstore</c> chat command: disconnects the client while the character
/// stays standing in the safe zone with the personal store open, so other players can
/// continue to browse and buy from it.
/// <list type="bullet">
///   <item>Requires a currently opened personal store (prices set, store name set).</item>
///   <item>Requires the player to stand in a safe zone - the ghost is then unattackable.</item>
///   <item>The ghost stops automatically when the player logs back in.</item>
///   <item>When the last store item is sold, the ghost session ends automatically.</item>
/// </list>
/// </summary>
[Guid("8F3D9E42-6A1B-4C7D-9E2F-5B8A1C4D7E30")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.OfflineStoreChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.OfflineStoreChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public sealed class OfflineStoreChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/offstore";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.SelectedCharacter is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreNoCharacterSelected)).ConfigureAwait(false);
            return;
        }

        if (!player.IsAlive)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreMustBeAlive)).ConfigureAwait(false);
            return;
        }

        if (player.CurrentMap is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreNotOnMap)).ConfigureAwait(false);
            return;
        }

        if (!player.IsAtSafezone())
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreNotInSafezone)).ConfigureAwait(false);
            return;
        }

        if (!(player.ShopStorage?.StoreOpen ?? false) || string.IsNullOrWhiteSpace(player.SelectedCharacter.StoreName))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreStoreNotOpen)).ConfigureAwait(false);
            return;
        }

        var loginName = player.Account?.LoginName;
        if (loginName is null)
        {
            return;
        }

        var manager = player.GameContext.OfflinePlayerManager;

        if (manager.IsActive(loginName))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreAlreadyActive)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreStarted)).ConfigureAwait(false);

        if (!await manager.StartStoreAsync(player, loginName).ConfigureAwait(false))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineStoreFailed)).ConfigureAwait(false);
        }
    }
}
