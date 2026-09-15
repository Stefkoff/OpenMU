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
    /// Calculates the stat allocations for the given point pool and percentages.
    /// </summary>
    /// <param name="pool">The total amount of points to distribute.</param>
    /// <param name="percentages">The five percentages (0-100) for strength, agility, vitality, energy and command.</param>
    /// <param name="statDefinitions">The stat attributes of the character class; percentages for stats which the
    /// class doesn't support are not applied.</param>
    /// <returns>The allocation result, including the leftover points which are not distributed.</returns>
    public static AutoResetAllocation Calculate(int pool, IReadOnlyList<byte> percentages, IReadOnlyCollection<StatAttributeDefinition> statDefinitions)
    {
        var allocations = new Dictionary<AttributeDefinition, int>(TargetAttributes.Count);
        long distributed = 0;

        for (var i = 0; i < TargetAttributes.Count; i++)
        {
            var attribute = TargetAttributes[i];
            var percentage = percentages.Count > i ? percentages[i] : 0;
            if (percentage == 0)
            {
                continue;
            }

            if (statDefinitions.All(definition => definition.Attribute != attribute))
            {
                // The character class doesn't support this stat (e.g. command on most classes);
                // its share stays in the level-up points.
                continue;
            }

            var share = (int)((long)pool * percentage / 100);
            allocations[attribute] = share;
            distributed += share;
        }

        return new AutoResetAllocation(allocations, (int)(pool - distributed));
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

