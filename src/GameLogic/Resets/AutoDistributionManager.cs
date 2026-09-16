// <copyright file="AutoDistributionManager.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Collections.Concurrent;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Holds the per-character settings of the automatic distribution features
/// (<c>/autoreset</c> and <c>/autostat</c>) and performs the automatic character
/// reset and stat distribution.
/// </summary>
/// <remarks>
/// The settings are session-scoped: they are active until the character logs out,
/// and they stay active while an offline leveling session keeps the same character
/// in the world (see <see cref="RemoveOnLogout"/>).
/// </remarks>
internal static class AutoDistributionManager
{
    private static readonly ConcurrentDictionary<Guid, AutoDistributionSettings> ActiveSettings = new();

    /// <summary>
    /// Sets the reset point distribution (<c>/autoreset</c>) for the given character.
    /// </summary>
    /// <param name="characterId">The identifier of the selected character.</param>
    /// <param name="percentages">The five stat distribution percentages.</param>
    public static void SetResetPercentages(Guid characterId, IReadOnlyList<byte> percentages)
    {
        ActiveSettings.AddOrUpdate(
            characterId,
            _ => new AutoDistributionSettings(percentages, null, null),
            (_, existing) => existing with { ResetPercentages = percentages });
    }

    /// <summary>
    /// Sets the stat distribution (<c>/autostat</c>) for the given character, or removes
    /// it when <paramref name="threshold"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="characterId">The identifier of the selected character.</param>
    /// <param name="threshold">The minimum amount of level-up points which triggers a distribution.</param>
    /// <param name="percentages">The five stat distribution percentages.</param>
    public static void SetStatDistribution(Guid characterId, int? threshold, IReadOnlyList<byte>? percentages)
    {
        if (threshold is null || percentages is null)
        {
            if (ActiveSettings.TryGetValue(characterId, out var existing))
            {
                var updated = existing with { StatThreshold = null, StatPercentages = null };
                if (updated.ResetPercentages is null)
                {
                    Remove(characterId);
                }
                else
                {
                    ActiveSettings[characterId] = updated;
                }
            }

            return;
        }

        ActiveSettings.AddOrUpdate(
            characterId,
            _ => new AutoDistributionSettings(null, threshold, percentages),
            (_, existing) => existing with { StatThreshold = threshold, StatPercentages = percentages });
    }

    /// <summary>
    /// Removes the automatic distribution settings of the given character.
    /// </summary>
    /// <param name="characterId">The identifier of the selected character.</param>
    public static void Remove(Guid characterId)
    {
        ActiveSettings.TryRemove(characterId, out _);
    }

    /// <summary>
    /// Removes the automatic distribution settings when the character really logged out.
    /// If an offline leveling session takes over the same character, the settings are kept.
    /// </summary>
    /// <param name="player">The player which disconnected.</param>
    public static void RemoveOnLogout(Player player)
    {
        if (player.SelectedCharacter?.Id is not { } characterId)
        {
            return;
        }

        var loginName = player.Account?.LoginName ?? string.Empty;
        if (player.GameContext.OfflinePlayerManager.IsActive(loginName))
        {
            return;
        }

        Remove(characterId);
    }

    /// <summary>
    /// Performs an automatic character reset (<c>/autoreset</c>), if the character just reached
    /// the maximum level, reset settings are active, and the reset limit isn't reached.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The result of the reset for the client view, or <see langword="null"/> if nothing was done.</returns>
    public static AutoResetResult? PerformAutoReset(Player player)
    {
        var character = player.SelectedCharacter;
        if (character?.Id is not { } characterId
            || player.Attributes is null
            || player.Account is { IsBot: true })
        {
            return null;
        }

        if (player.Attributes[Stats.Level] != player.GameContext.Configuration.MaximumLevel
            || !ActiveSettings.TryGetValue(characterId, out var settings)
            || settings.ResetPercentages is not { } percentages)
        {
            return null;
        }

        if (character.CharacterClass is not { } characterClass)
        {
            return null;
        }

        var resetConfiguration = player.GameContext.FeaturePlugIns.GetPlugIn<ResetFeaturePlugIn>()?.Configuration ?? new ResetConfiguration();
        var resetProgression = ResetProgressionCalculator.Calculate(
            (int)player.Attributes[Stats.Resets],
            (int)player.Attributes[Stats.PointsPerReset],
            resetConfiguration);

        if (resetConfiguration.ResetLimit is > 0 && resetProgression.NextResetCount > resetConfiguration.ResetLimit)
        {
            return null;
        }

        var oldResets = (int)player.Attributes[Stats.Resets];
        var oldLevel = (int)player.Attributes[Stats.Level];
        var oldLevelUpPoints = character.LevelUpPoints;
        var oldStats = GetStatValues(player.Attributes, AutoResetDistribution.TargetAttributes);

        player.Attributes[Stats.Resets] = resetProgression.NextResetCount;
        player.Attributes[Stats.Level] = resetConfiguration.LevelAfterReset;
        character.Experience = 0;

        var statDefinitions = characterClass.StatAttributes.Where(s => s.IncreasableByPlayer).ToList();
        var pool = resetConfiguration.ReplacePointsPerReset
            ? resetProgression.TotalPointsAfterReset
            : character.LevelUpPoints + resetProgression.PointsForReset;
        pool = Math.Max(0, pool);

        // The reset sets every stat back to its base value first, so the caps must be
        // measured relative to those base values - not against the pre-reset stat values.
        var currentValues = AutoResetDistribution.GetBaseValues(statDefinitions);
        var allocation = AutoResetDistribution.Calculate(pool, percentages, statDefinitions, currentValues);
        foreach (var (attribute, share) in allocation.Allocations)
        {
            var statDefinition = statDefinitions.First(definition => definition.Attribute == attribute);
            player.Attributes[attribute] = statDefinition.BaseValue + share;
        }

        character.LevelUpPoints = allocation.LeftoverPoints;

        var newStats = GetStatValues(player.Attributes, AutoResetDistribution.TargetAttributes);
        player.Logger.LogInformation(
            "Auto reset for character '{CharacterName}' (account '{AccountName}') completed: resets {OldResets} -> {NewResets}, level {OldLevel} -> {NewLevel}, level-up points {OldLevelUpPoints} -> {NewLevelUpPoints}, strength {OldStrength} -> {NewStrength}, agility {OldAgility} -> {NewAgility}, vitality {OldVitality} -> {NewVitality}, energy {OldEnergy} -> {NewEnergy}, command {OldCommand} -> {NewCommand}; {StrengthPercent}% STR, {AgilityPercent}% AGI, {VitalityPercent}% VIT, {EnergyPercent}% ENE, {CommandPercent}% CMD.",
            character.Name,
            player.Account?.LoginName,
            oldResets,
            resetProgression.NextResetCount,
            oldLevel,
            (int)resetConfiguration.LevelAfterReset,
            oldLevelUpPoints,
            character.LevelUpPoints,
            oldStats[0],
            newStats[0],
            oldStats[1],
            newStats[1],
            oldStats[2],
            newStats[2],
            oldStats[3],
            newStats[3],
            oldStats[4],
            newStats[4],
            percentages.Count > 0 ? percentages[0] : 0,
            percentages.Count > 1 ? percentages[1] : 0,
            percentages.Count > 2 ? percentages[2] : 0,
            percentages.Count > 3 ? percentages[3] : 0,
            percentages.Count > 4 ? percentages[4] : 0);

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

    /// <summary>
    /// Performs an automatic stat distribution (<c>/autostat</c>), if the character's level-up
    /// points reached the configured threshold and stat distribution settings are active.
    /// The points are distributed ADDITIVELY to the stats (like a player would assign them
    /// manually) - previously invested points are never touched.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The result of the distribution for the client view, or <see langword="null"/> if nothing was done.</returns>
    public static AutoStatResult? PerformStatDistribution(Player player)
    {
        var character = player.SelectedCharacter;
        if (character?.Id is not { } characterId
            || player.Attributes is null
            || player.Account is { IsBot: true }
            || character.CharacterClass is not { } characterClass
            || !ActiveSettings.TryGetValue(characterId, out var settings)
            || settings.StatThreshold is not { } threshold
            || settings.StatPercentages is not { } percentages)
        {
            return null;
        }

        var pool = character.LevelUpPoints;
        if (pool < threshold)
        {
            return null;
        }

        var oldLevelUpPoints = pool;
        var oldStats = GetStatValues(player.Attributes, AutoResetDistribution.TargetAttributes);

        var statDefinitions = characterClass.StatAttributes.Where(s => s.IncreasableByPlayer).ToList();
        var currentValues = AutoResetDistribution.TargetAttributes
            .Select(attribute => player.Attributes[attribute])
            .ToArray();
        var allocation = AutoResetDistribution.Calculate(pool, percentages, statDefinitions, currentValues);
        foreach (var (attribute, share) in allocation.Allocations)
        {
            player.Attributes[attribute] += share;
        }

        character.LevelUpPoints = allocation.LeftoverPoints;

        var newStats = GetStatValues(player.Attributes, AutoResetDistribution.TargetAttributes);
        player.Logger.LogInformation(
            "Auto stat distribution for character '{CharacterName}' (account '{AccountName}') completed: {DistributedPoints} points distributed - strength {OldStrength} -> {NewStrength}, agility {OldAgility} -> {NewAgility}, vitality {OldVitality} -> {NewVitality}, energy {OldEnergy} -> {NewEnergy}, command {OldCommand} -> {NewCommand}; level-up points {OldLevelUpPoints} -> {NewLevelUpPoints}; {StrengthPercent}% STR, {AgilityPercent}% AGI, {VitalityPercent}% VIT, {EnergyPercent}% ENE, {CommandPercent}% CMD.",
            character.Name,
            player.Account?.LoginName,
            pool - allocation.LeftoverPoints,
            oldStats[0],
            newStats[0],
            oldStats[1],
            newStats[1],
            oldStats[2],
            newStats[2],
            oldStats[3],
            newStats[3],
            oldStats[4],
            newStats[4],
            oldLevelUpPoints,
            character.LevelUpPoints,
            percentages.Count > 0 ? percentages[0] : 0,
            percentages.Count > 1 ? percentages[1] : 0,
            percentages.Count > 2 ? percentages[2] : 0,
            percentages.Count > 3 ? percentages[3] : 0,
            percentages.Count > 4 ? percentages[4] : 0);

        var shares = AutoResetDistribution.TargetAttributes
            .Select(attribute => allocation.Allocations.TryGetValue(attribute, out var share) ? share : 0)
            .ToArray();
        return new AutoStatResult(
            allocation.LeftoverPoints,
            shares[0],
            shares[1],
            shares[2],
            shares[3],
            shares[4]);
    }

    private static int[] GetStatValues(AttributeSystem attributes, IReadOnlyList<AttributeDefinition> targetAttributes)
    {
        return targetAttributes.Select(attribute => (int)attributes[attribute]).ToArray();
    }

    /// <summary>
    /// The session-scoped settings of the automatic distribution features for one character.
    /// </summary>
    internal sealed record AutoDistributionSettings(
        IReadOnlyList<byte>? ResetPercentages,
        int? StatThreshold,
        IReadOnlyList<byte>? StatPercentages);
}
