// <copyright file="AutoResetChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles the <c>/autoreset</c> chat command: with
/// <c>/autoreset 40 30 20 10 0</c> the reset points of each automatic reset are
/// distributed with 40% to strength, 30% to agility, 20% to vitality, 10% to energy
/// and 0% to command. The setting stays active until the character logs out, and it
/// also applies while an offline leveling session (<see cref="OfflineLevelingChatCommandPlugIn"/>)
/// keeps leveling the character. The automatic reset behaves like the regular reset
/// (see <see cref="ResetCharacterAction"/>): the same reset configuration and point
/// progression are used, but no costs are consumed and the character stays in place.
/// It can be disabled with <c>/autoreset off</c>.
/// </summary>
[Guid("A7C4B3E1-5F6D-4A2B-9C8E-0D1F2E3A4B5C")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.AutoResetChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.AutoResetChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, "Sets the stat point distribution of the automatic character resets and activates them; use '/autoreset off' to disable them.", typeof(AutoResetChatCommandArgs))]
public sealed class AutoResetChatCommandPlugIn : ChatCommandPlugInBase<AutoResetChatCommandArgs>, IPlayerStateChangedPlugIn, ICharacterLevelUpPlugIn
{
    private const string Command = "/autoreset";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public override async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (!IsOffCommand(command))
        {
            await base.HandleCommandAsync(player, command).ConfigureAwait(false);
            return;
        }

        try
        {
            if (player.SelectedCharacter is not { } character)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnteredTheGame)).ConfigureAwait(false);
                return;
            }

            AutoDistributionManager.Remove(character.Id);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoResetDeactivated)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, $"Unexpected error handling the chat command '{this.Key}'.");
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, AutoResetChatCommandArgs arguments)
    {
        if (player.SelectedCharacter is not { } character)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnteredTheGame)).ConfigureAwait(false);
            return;
        }

        var percentages = arguments.Percentages;
        if (percentages.Any(p => p > 100) || percentages.Sum(p => (int)p) > 100)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoResetPercentagesInvalid)).ConfigureAwait(false);
            return;
        }

        if (percentages.Sum(p => (int)p) == 0)
        {
            AutoDistributionManager.Remove(character.Id);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoResetDeactivated)).ConfigureAwait(false);
            return;
        }

        AutoDistributionManager.SetResetPercentages(character.Id, percentages);
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.AutoResetActivated),
            arguments.StrengthPercentage,
            arguments.AgilityPercentage,
            arguments.VitalityPercentage,
            arguments.EnergyPercentage,
            arguments.CommandPercentage,
            player.GameContext.Configuration.MaximumLevel).ConfigureAwait(false);

        // The command was issued while the character is already at the maximum level:
        // reset once immediately, so the automation takes effect right away.
        if (AutoDistributionManager.PerformAutoReset(player) is { } result)
        {
            await this.UpdateAfterResetAsync(player, result).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        if (player.Account is { IsBot: true } || player.SelectedCharacter is null)
        {
            return;
        }

        AutoResetResult? result;
        try
        {
            result = AutoDistributionManager.PerformAutoReset(player);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, $"Unexpected error during the automatic character reset of {player}.");
            return;
        }

        if (result is not null)
        {
            // The level-up plug-in is synchronous; the mutations already happened above, so the
            // view and persistence updates can safely run in the background.
            _ = this.UpdateAfterResetAsync(player, result);
        }
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState == PlayerState.Disconnected)
        {
            AutoDistributionManager.RemoveOnLogout(player);
        }
    }

    private static bool IsOffCommand(string command)
    {
        var arguments = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return arguments.Length == 2 && arguments[1].Equals("off", StringComparison.OrdinalIgnoreCase);
    }

    private async ValueTask UpdateAfterResetAsync(Player player, AutoResetResult result)
    {
        try
        {
            // These view updates are no-ops for offline leveling ghosts without a client connection.
            await player.InvokeViewPlugInAsync<IUpdateCharacterBaseStatsPlugIn>(p => p.UpdateCharacterBaseStatsAsync()).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateLevelPlugIn>(p => p.UpdateLevelAsync()).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.AutoResetPerformed),
                result.LevelAfterReset,
                result.LeftoverPoints,
                result.Strength,
                result.Agility,
                result.Vitality,
                result.Energy,
                result.Command).ConfigureAwait(false);
            await player.SaveProgressAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, $"Couldn't update the client view after the automatic reset of {player}.");
        }
    }
}
