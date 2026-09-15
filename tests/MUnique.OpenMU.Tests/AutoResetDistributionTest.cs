// <copyright file="AutoResetDistributionTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// Tests for the <see cref="AutoResetDistribution"/> of the automatic character reset.
/// </summary>
[TestFixture]
public class AutoResetDistributionTest
{
    /// <summary>
    /// Tests that the whole pool is distributed when the percentages sum up to 100.
    /// </summary>
    [Test]
    public void DistributionWithFullPercentagesDistributesTheWholePool()
    {
        var allocation = AutoResetDistribution.Calculate(1500, new byte[] { 40, 30, 20, 10, 0 }, this.CreateCompleteStatDefinitions());

        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(600));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(450));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(300));
        Assert.That(allocation.Allocations[Stats.BaseEnergy], Is.EqualTo(150));
        Assert.That(allocation.Allocations.ContainsKey(Stats.BaseLeadership), Is.False);
    }

    /// <summary>
    /// Tests that the leftover of percentages below 100 stays in the level-up points.
    /// </summary>
    [Test]
    public void DistributionWithPartialPercentagesLeavesLeftoverPoints()
    {
        var allocation = AutoResetDistribution.Calculate(1500, new byte[] { 40, 40, 10, 0, 0 }, this.CreateCompleteStatDefinitions());

        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(600));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(600));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(150));
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(150));
    }

    /// <summary>
    /// Tests that a stat which the character class doesn't support (command on most classes)
    /// is skipped and its share stays in the level-up points.
    /// </summary>
    [Test]
    public void DistributionWithUnsupportedStatKeepsItsShareAsLeftover()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions()
            .Where(definition => definition.Attribute != Stats.BaseLeadership)
            .ToList();
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 50, 20, 10, 0, 20 }, statDefinitions);

        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(500));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(200));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(100));
        Assert.That(allocation.Allocations.ContainsKey(Stats.BaseLeadership), Is.False);
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(200));
    }

    /// <summary>
    /// Tests that shares are truncated to whole points and the remainder is not lost.
    /// </summary>
    [Test]
    public void DistributionWithUnevenPercentagesDistributesWholePointsOnly()
    {
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 33, 33, 33, 1, 0 }, this.CreateCompleteStatDefinitions());

        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseEnergy], Is.EqualTo(10));
        // 1000 - 330 - 330 - 330 - 10 = 0
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
    }

    private IReadOnlyCollection<StatAttributeDefinition> CreateCompleteStatDefinitions()
    {
        return
        [
            new StatAttributeDefinition(Stats.BaseStrength, 28, true),
            new StatAttributeDefinition(Stats.BaseAgility, 20, true),
            new StatAttributeDefinition(Stats.BaseVitality, 25, true),
            new StatAttributeDefinition(Stats.BaseEnergy, 10, true),
            new StatAttributeDefinition(Stats.BaseLeadership, 25, true),
        ];
    }
}
