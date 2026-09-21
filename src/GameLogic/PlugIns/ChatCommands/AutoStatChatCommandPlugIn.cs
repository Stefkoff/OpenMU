// <copyright file="AutoStatChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles the <c>/autostat</c> chat command: with
/// <c>/autostat 1000 40 30 20 10 0</c> the level-up points of the character are
/// automatically distributed with 40% to strength, 30% to agility, 20% to vitality,
/// 10% to energy and 0% to command, as soon as they reach or exceed 1000 points.
/// The setting stays active until the character logs out, and it also applies while
/// an offline leveling session (<see cref="OfflineLevelingChatCommandPlugIn"/>) keeps
/// leveling the character. It can be disabled with <c>/autostat off</c>.
/// </summary>
[Guid("B8D5C4F2-6E7A-4B3C-9F1D-2A3B4C5D6E7F")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.AutoStatChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.AutoStatChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, "Distributes the level-up points automatically once they reach the given threshold; use '/autostat off' to disable it.", typeof(AutoStatChatCommandArgs))]
public sealed class AutoStatChatCommandPlugIn : ChatCommandPlugInBase<AutoStatChatCommandArgs>, IPlayerStateChangedPlugIn, ICharacterLevelUpPlugIn
{
    private const string Command = "/autostat";

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

            AutoDistributionManager.SetStatDistribution(character.Id, null, null);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoStatDeactivated)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, $"Unexpected error handling the chat command '{this.Key}'.");
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, AutoStatChatCommandArgs arguments)
    {
        if (player.SelectedCharacter is not { } character)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnteredTheGame)).ConfigureAwait(false);
            return;
        }

        if (arguments.PointsThreshold < 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoStatPercentagesInvalid)).ConfigureAwait(false);
            return;
        }

        var percentages = arguments.Percentages;
        if (percentages.Any(p => p > 100) || percentages.Sum(p => (int)p) > 100)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoStatPercentagesInvalid)).ConfigureAwait(false);
            return;
        }

        if (percentages.Sum(p => (int)p) == 0)
        {
            AutoDistributionManager.SetStatDistribution(character.Id, null, null);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoStatDeactivated)).ConfigureAwait(false);
            return;
        }

        AutoDistributionManager.SetStatDistribution(character.Id, arguments.PointsThreshold, percentages);
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.AutoStatActivated),
            arguments.PointsThreshold,
            arguments.StrengthPercentage,
            arguments.AgilityPercentage,
            arguments.VitalityPercentage,
            arguments.EnergyPercentage,
            arguments.CommandPercentage).ConfigureAwait(false);

        // The command was issued while the character already has enough points:
        // distribute once immediately, so the automation takes effect right away.
        if (AutoDistributionManager.PerformStatDistribution(player) is { } result)
        {
            await this.UpdateAfterStatAsync(player, result).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        if (player.Account is { IsBot: true } || player.SelectedCharacter is null)
        {
            return;
        }

        AutoStatResult? result;
        try
        {
            result = AutoDistributionManager.PerformStatDistribution(player);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, $"Unexpected error during the automatic stat distribution of {player}.");
            return;
        }

        if (result is not null)
        {
            // The level-up plug-in is synchronous; the mutations already happened above, so the
            // view and persistence updates can safely run in the background.
            _ = this.UpdateAfterStatAsync(player, result);
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

    private async ValueTask UpdateAfterStatAsync(Player player, AutoStatResult result)
    {
        try
        {
            // These view updates are no-ops for offline leveling ghosts without a client connection.
            await player.InvokeViewPlugInAsync<IUpdateCharacterBaseStatsPlugIn>(p => p.UpdateCharacterBaseStatsAsync()).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.AutoStatPerformed),
                result.Strength,
                result.Agility,
                result.Vitality,
                result.Energy,
                result.Command,
                result.LeftoverPoints).ConfigureAwait(false);
            await player.SaveProgressAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, $"Couldn't update the client view after the automatic stat distribution of {player}.");
        }
    }
}
