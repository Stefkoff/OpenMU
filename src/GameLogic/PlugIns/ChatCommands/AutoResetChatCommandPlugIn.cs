// <copyright file="AutoResetChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
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
/// </summary>
[Guid("A7C4B3E1-5F6D-4A2B-9C8E-0D1F2E3A4B5C")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.AutoResetChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.AutoResetChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, "Sets the stat point distribution of the automatic character resets and activates them.", typeof(AutoResetChatCommandArgs))]
public sealed class AutoResetChatCommandPlugIn : ChatCommandPlugInBase<AutoResetChatCommandArgs>, IPlayerStateChangedPlugIn, ICharacterLevelUpPlugIn
{
    private const string Command = "/autoreset";

    private static readonly ConcurrentDictionary<Guid, IReadOnlyList<byte>> ActiveAutoResets = new();

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

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
            ActiveAutoResets.TryRemove(character.Id, out _);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AutoResetDeactivated)).ConfigureAwait(false);
            return;
        }

        ActiveAutoResets[character.Id] = percentages;
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
        if (player.Attributes is { } attributes
            && attributes[Stats.Level] == player.GameContext.Configuration.MaximumLevel
            && PerformAutoReset(player, character, percentages) is { } result)
        {
            await this.UpdateAfterResetAsync(player, result).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        var character = player.SelectedCharacter;
        if (character?.Id is not { } characterId
            || player.Attributes is null
            || player.Account is { IsBot: true })
        {
            return;
        }

        // Only the level-up to exactly the maximum level triggers the automatic reset.
        if (player.Attributes[Stats.Level] != player.GameContext.Configuration.MaximumLevel
            || !ActiveAutoResets.TryGetValue(characterId, out var percentages))
        {
            return;
        }

        AutoResetResult? result;
        try
        {
            result = PerformAutoReset(player, character, percentages);
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
        if (currentState != PlayerState.Disconnected || player.SelectedCharacter?.Id is not { } characterId)
        {
            return;
        }

        // An offline leveling session takes over the same character, so the setting stays
        // active there; only a real logout ends the automation.
        var loginName = player.Account?.LoginName ?? string.Empty;
        if (player.GameContext.OfflinePlayerManager.IsActive(loginName))
        {
            return;
        }

        ActiveAutoResets.TryRemove(characterId, out _);
    }

    /// <summary>
    /// Performs the automatic character reset: the reset count increases, the level and
    /// experience go back to the configured values, the increasable stats are reset to
    /// their base values and the granted reset points are distributed by the percentages.
    /// No reset costs are consumed - the reset is fully automatic - and the character
    /// stays in place (no logout, no teleport), even if the reset configuration
    /// <see cref="ResetConfiguration.MoveHome"/> or <see cref="ResetConfiguration.LogOut"/>.
    /// </summary>
    /// <param name="player">The player whose character resets.</param>
    /// <param name="character">The selected character.</param>
    /// <param name="percentages">The five stat distribution percentages.</param>
    /// <returns>The result of the reset, or <see langword="null"/> when it didn't happen (e.g. reset limit reached).</returns>
    private static AutoResetResult? PerformAutoReset(Player player, Character character, IReadOnlyList<byte> percentages)
    {
        var attributes = player.Attributes!;
        if (character.CharacterClass is not { } characterClass)
        {
            return null;
        }

        var resetConfiguration = player.GameContext.FeaturePlugIns.GetPlugIn<ResetFeaturePlugIn>()?.Configuration ?? new ResetConfiguration();
        var resetProgression = ResetProgressionCalculator.Calculate(
            (int)attributes[Stats.Resets],
            (int)attributes[Stats.PointsPerReset],
            resetConfiguration);

        if (resetConfiguration.ResetLimit is > 0 && resetProgression.NextResetCount > resetConfiguration.ResetLimit)
        {
            return null;
        }

        attributes[Stats.Resets] = resetProgression.NextResetCount;
        attributes[Stats.Level] = resetConfiguration.LevelAfterReset;
        character.Experience = 0;

        var statDefinitions = characterClass.StatAttributes.Where(s => s.IncreasableByPlayer).ToList();
        var pool = resetConfiguration.ReplacePointsPerReset
            ? resetProgression.TotalPointsAfterReset
            : character.LevelUpPoints + resetProgression.PointsForReset;
        pool = Math.Max(0, pool);

        var allocation = AutoResetDistribution.Calculate(pool, percentages, statDefinitions);
        foreach (var (attribute, share) in allocation.Allocations)
        {
            var statDefinition = statDefinitions.First(definition => definition.Attribute == attribute);
            attributes[attribute] = statDefinition.BaseValue + share;
        }

        character.LevelUpPoints = allocation.LeftoverPoints;

        var shares = AutoResetDistribution.TargetAttributes
            .Select(attribute => allocation.Allocations.TryGetValue(attribute, out var share) ? share : 0)
            .ToArray();
        return new AutoResetResult(
            allocation.LeftoverPoints,
            shares[0],
            shares[1],
            shares[2],
            shares[3],
            shares[4],
            (int)resetConfiguration.LevelAfterReset);
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
