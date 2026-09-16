// <copyright file="AutoResetDistributionTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.AttributeSystem;
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
        var allocation = AutoResetDistribution.Calculate(1500, new byte[] { 40, 30, 20, 10, 0 }, this.CreateCompleteStatDefinitions(), FloatZeros);

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
        var allocation = AutoResetDistribution.Calculate(1500, new byte[] { 40, 40, 10, 0, 0 }, this.CreateCompleteStatDefinitions(), FloatZeros);

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
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 50, 20, 10, 0, 20 }, statDefinitions, FloatZeros);

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
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 33, 33, 33, 1, 0 }, this.CreateCompleteStatDefinitions(), FloatZeros);

        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(330));
        Assert.That(allocation.Allocations[Stats.BaseEnergy], Is.EqualTo(10));
        // 1000 - 330 - 330 - 330 - 10 = 0
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that a stat which reaches its configured maximum value is set to the maximum
    /// and the overflowing points are re-distributed to the following stats.
    /// </summary>
    [Test]
    public void DistributionWithCappedStatRollsOverToTheNextStat()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions();
        this.SetMaximumValue(statDefinitions, Stats.BaseStrength, 100);
        var allocation = AutoResetDistribution.Calculate(800, new byte[] { 40, 30, 20, 10, 0 }, statDefinitions, FloatZeros);

        // strength share would be 320, but only 100 fit: the 220 overflow points are re-distributed.
        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(100));
        // agility gets its 240 plus the 220 overflow, vitality its 160, energy its 80.
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(460));
        Assert.That(allocation.Allocations[Stats.BaseVitality], Is.EqualTo(160));
        Assert.That(allocation.Allocations[Stats.BaseEnergy], Is.EqualTo(80));
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that points which can't be placed anywhere (all stats saturated) stay as leftover.
    /// </summary>
    [Test]
    public void DistributionWithSaturatedStatsLeavesTheRestAsLeftover()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions();
        foreach (var statDefinition in statDefinitions)
        {
            statDefinition.Attribute!.MaximumValue = AutoResetDistribution.DefaultMaximumValue;
        }

        var allocation = AutoResetDistribution.Calculate(100000, new byte[] { 50, 50, 0, 0, 0 }, statDefinitions, new[] { 65535f, 65535f, 65535f, 65535f, 65535f });

        // every stat is already at its maximum, so nothing fits anymore.
        Assert.That(allocation.Allocations.Count, Is.EqualTo(0));
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(100000));
    }

    /// <summary>
    /// Tests that the configured maximum value also caps a stat which is not yet saturated.
    /// </summary>
    [Test]
    public void DistributionUsesTheConfiguredMaximumValue()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions();
        this.SetMaximumValue(statDefinitions, Stats.BaseStrength, 200);
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 50, 50, 0, 0, 0 }, statDefinitions, FloatZeros);

        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(200));
        // agility gets its 500 plus the 300 which didn't fit strength.
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(800));
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the caps are measured relative to the class base values (which the stats are
    /// reset to) - not relative to pre-reset stat values.
    /// </summary>
    [Test]
    public void DistributionMeasuredCapsRelativeToTheBaseValue()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions();
        this.SetMaximumValue(statDefinitions, Stats.BaseStrength, 50);
        var allocation = AutoResetDistribution.Calculate(1000, new byte[] { 100, 0, 0, 0, 0 }, statDefinitions, AutoResetDistribution.GetBaseValues(statDefinitions));

        // base strength is 28, so only 22 fit - the stat ends exactly at the maximum of 50,
        // and the 978 overflow points are re-distributed to agility.
        Assert.That(allocation.Allocations[Stats.BaseStrength], Is.EqualTo(22));
        Assert.That(allocation.Allocations[Stats.BaseAgility], Is.EqualTo(978));
        Assert.That(allocation.LeftoverPoints, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the base values are returned in the order of the target attributes.
    /// </summary>
    [Test]
    public void GetBaseValuesReturnsTheClassBaseValues()
    {
        var statDefinitions = this.CreateCompleteStatDefinitions();

        var baseValues = AutoResetDistribution.GetBaseValues(statDefinitions);

        Assert.That(baseValues, Is.EqualTo(new[] { 28f, 20f, 25f, 10f, 25f }));
    }

    private static readonly float[] FloatZeros = [0f, 0f, 0f, 0f, 0f];

    private void SetMaximumValue(IEnumerable<StatAttributeDefinition> statDefinitions, AttributeDefinition attribute, float maximumValue)
    {
        statDefinitions.First(definition => definition.Attribute == attribute).Attribute!.MaximumValue = maximumValue;
    }

    /// <summary>
    /// Creates stat definitions which reference copies of the shared <see cref="Stats"/> attribute
    /// definitions, so that test mutations of <see cref="AttributeDefinition.MaximumValue"/> don't
    /// leak into other tests (the distribution matches the attributes by id).
    /// </summary>
    private IReadOnlyCollection<StatAttributeDefinition> CreateCompleteStatDefinitions()
    {
        static AttributeDefinition CopyOf(AttributeDefinition source) => new(source.Id, source.Designation ?? string.Empty, source.Description ?? string.Empty);

        return
        [
            new StatAttributeDefinition(CopyOf(Stats.BaseStrength), 28, true),
            new StatAttributeDefinition(CopyOf(Stats.BaseAgility), 20, true),
            new StatAttributeDefinition(CopyOf(Stats.BaseVitality), 25, true),
            new StatAttributeDefinition(CopyOf(Stats.BaseEnergy), 10, true),
            new StatAttributeDefinition(CopyOf(Stats.BaseLeadership), 25, true),
        ];
    }
}
