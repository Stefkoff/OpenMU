// <copyright file="BoxOfKundumEventScheduleTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Tests for the <see cref="BoxOfKundumEventSchedule"/>.
/// </summary>
[TestFixture]
public class BoxOfKundumEventScheduleTest
{
    /// <summary>
    /// Tests that a time within the 5 second match window of the entry time is due.
    /// </summary>
    [Test]
    public void TimeInWindowMatches()
    {
        var now = DateTime.UtcNow;
        Assert.That(BoxOfKundumEventSchedule.IsDue(new TimeOnly(20, 0, 2), new TimeOnly(20, 0), null, now), Is.True);
    }

    /// <summary>
    /// Tests that a time before the entry window is not due.
    /// </summary>
    [Test]
    public void TimeBeforeWindowDoesNotMatch()
    {
        var now = DateTime.UtcNow;
        Assert.That(BoxOfKundumEventSchedule.IsDue(new TimeOnly(19, 59, 59), new TimeOnly(20, 0), null, now), Is.False);
    }

    /// <summary>
    /// Tests that a time after the entry window is not due.
    /// </summary>
    [Test]
    public void TimeAfterWindowDoesNotMatch()
    {
        var now = DateTime.UtcNow;
        Assert.That(BoxOfKundumEventSchedule.IsDue(new TimeOnly(20, 0, 6), new TimeOnly(20, 0), null, now), Is.False);
    }

    /// <summary>
    /// Tests that the entry does not fire twice within one minute, but fires again afterwards.
    /// </summary>
    [Test]
    public void NoRefireWithinOneMinute()
    {
        var nowUtc = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var entryTime = new TimeOnly(20, 0);
        Assert.That(BoxOfKundumEventSchedule.IsDue(new TimeOnly(20, 0, 2), entryTime, nowUtc.AddSeconds(-30), nowUtc), Is.False);
        Assert.That(BoxOfKundumEventSchedule.IsDue(new TimeOnly(20, 0, 2), entryTime, nowUtc.AddSeconds(-90), nowUtc), Is.True);
    }
}
