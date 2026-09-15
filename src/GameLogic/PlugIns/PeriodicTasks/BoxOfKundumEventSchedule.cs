// <copyright file="BoxOfKundumEventSchedule.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Pure schedule logic of the box of kundum event: an entry fires when the server-local
/// time is within a 5 second window of the configured time, and it never fires twice
/// within one minute.
/// </summary>
internal static class BoxOfKundumEventSchedule
{
    /// <summary>
    /// The width of the match window around the configured event time.
    /// </summary>
    public static readonly TimeSpan MatchWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The minimum delay between two firings of the same entry.
    /// </summary>
    public static readonly TimeSpan MinFiringInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Determines whether the event entry is due at the given server-local time.
    /// </summary>
    /// <param name="nowTime">The current server-local time of day.</param>
    /// <param name="entryTime">The configured time of day of the entry.</param>
    /// <param name="lastFiredUtc">The UTC timestamp of the last firing, or <see langword="null"/> if it never fired.</param>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <returns><c>true</c> if the event should fire now; otherwise <c>false</c>.</returns>
    public static bool IsDue(TimeOnly nowTime, TimeOnly entryTime, DateTime? lastFiredUtc, DateTime nowUtc)
    {
        if (nowTime < entryTime || nowTime > entryTime.Add(MatchWindow))
        {
            return false;
        }

        return lastFiredUtc is null || nowUtc - lastFiredUtc.Value > MinFiringInterval;
    }
}
