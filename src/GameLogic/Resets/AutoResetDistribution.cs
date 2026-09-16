// <copyright file="AutoResetDistribution.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Calculates how a pool of reset points is distributed to the five base stats
/// (strength, agility, vitality, energy, command) by the given percentages.
/// </summary>
internal static class AutoResetDistribution
{
    /// <summary>
    /// Gets the stat attributes which are targeted by the <c>/autoreset</c> command,
    /// in the order the percentages are provided: strength, agility, vitality, energy, command.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> TargetAttributes { get; } =
        [Stats.BaseStrength, Stats.BaseAgility, Stats.BaseVitality, Stats.BaseEnergy, Stats.BaseLeadership];

    /// <summary>
    /// The fallback maximum value of a stat if no <see cref="AttributeDefinition.MaximumValue"/> is configured
    /// (the classic MU client protocol can only display values up to 65535).
    /// </summary>
    public const int DefaultMaximumValue = 65535;

    /// <summary>
    /// Gets the base values of the five target attributes from the given stat definitions,
    /// in the order of <see cref="TargetAttributes"/>. Stats which the class doesn't support get 0.
    /// </summary>
    /// <param name="statDefinitions">The stat attributes of the character class.</param>
    /// <returns>The base values of the five target attributes.</returns>
    public static float[] GetBaseValues(IReadOnlyCollection<StatAttributeDefinition> statDefinitions)
    {
        var result = new float[TargetAttributes.Count];
        for (var i = 0; i < TargetAttributes.Count; i++)
        {
            var statDefinition = statDefinitions.FirstOrDefault(definition => definition.Attribute == TargetAttributes[i]);
            result[i] = statDefinition?.BaseValue ?? 0;
        }

        return result;
    }

    /// <summary>
    /// Calculates the stat allocations for the given point pool and percentages.
    /// Each stat is capped at its configured <see cref="AttributeDefinition.MaximumValue"/>
    /// (or <see cref="DefaultMaximumValue"/>); points which exceed a stat's cap are
    /// automatically re-distributed to the following stats, and everything that cannot
    /// be placed ends up in the leftover points.
    /// </summary>
    /// <param name="pool">The total amount of points to distribute.</param>
    /// <param name="percentages">The five percentages (0-100) for strength, agility, vitality, energy and command.</param>
    /// <param name="statDefinitions">The stat attributes of the character class; percentages for stats which the
    /// class doesn't support are not applied.</param>
    /// <param name="currentValues">The current values of the five target attributes, in the order of
    /// <see cref="TargetAttributes"/>; the caps are applied relative to these values.</param>
    /// <returns>The allocation result, including the leftover points which are not distributed.</returns>
    public static AutoResetAllocation Calculate(int pool, IReadOnlyList<byte> percentages, IReadOnlyCollection<StatAttributeDefinition> statDefinitions, IReadOnlyList<float> currentValues)
    {
        var allocations = new Dictionary<AttributeDefinition, int>(TargetAttributes.Count);
        long allocatedSum = 0;
        long carry = 0;

        for (var i = 0; i < TargetAttributes.Count; i++)
        {
            var attribute = TargetAttributes[i];
            var percentage = percentages.Count > i ? percentages[i] : 0;

            var statDefinition = statDefinitions.FirstOrDefault(definition => definition.Attribute == attribute);
            if (statDefinition is null)
            {
                // The character class doesn't support this stat (e.g. command on most classes);
                // its share stays available for the following stats / the leftover points.
                continue;
            }

            // Each stat gets its percentage share of the original pool - plus the overflow
            // which the previous stat(s) couldn't place due to their maximum values.
            var available = (long)pool * percentage / 100 + carry;
            if (available <= 0)
            {
                continue;
            }

            var maximumValue = statDefinition.Attribute?.MaximumValue ?? DefaultMaximumValue;
            var currentValue = currentValues.Count > i ? currentValues[i] : 0;
            var freeSpace = Math.Max(0, maximumValue - currentValue);

            var allocated = (int)Math.Min(available, (long)freeSpace);
            carry = available - allocated;
            allocatedSum += allocated;
            if (allocated > 0)
            {
                allocations[attribute] = allocated;
            }
        }

        return new AutoResetAllocation(allocations, (int)Math.Max(0, pool - allocatedSum));
    }
}

/// <summary>
/// The result of an <see cref="AutoResetDistribution.Calculate"/> call.
/// </summary>
internal record AutoResetAllocation(IReadOnlyDictionary<AttributeDefinition, int> Allocations, int LeftoverPoints);

/// <summary>
/// The result of an automatic character reset, used to inform the player about the distribution.
/// </summary>
internal record AutoResetResult(int LeftoverPoints, int Strength, int Agility, int Vitality, int Energy, int Command, int LevelAfterReset);

/// <summary>
/// The result of an automatic stat distribution, used to inform the player about the distribution.
/// </summary>
internal record AutoStatResult(int LeftoverPoints, int Strength, int Agility, int Vitality, int Energy, int Command);

