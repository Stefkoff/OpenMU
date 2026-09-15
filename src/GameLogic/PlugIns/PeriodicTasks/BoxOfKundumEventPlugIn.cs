// <copyright file="BoxOfKundumEventPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin which drops Box of Luck / Box of Kundum items at configured times and locations,
/// one by one with a configurable delay, fires a fireworks effect at each drop point and
/// announces the event to all players in advance.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.BoxOfKundumEventPlugIn_Name), Description = nameof(PlugInResources.BoxOfKundumEventPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A4F6C3E2-7B1D-4E9A-9C5F-2D8A1B3C4D5E")]
public class BoxOfKundumEventPlugIn : IPeriodicTaskPlugIn, ISupportCustomConfiguration<BoxOfKundumEventConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    // Per game context and per entry: the last fire/announcement timestamps and the
    // state of a running drop stream (timestamp of the last dropped box, boxes left).
    private static readonly ConcurrentDictionary<IGameContext, (DateTime Fired, DateTime Announced, DateTime LastDropUtc, int Remaining)[]> EventStates = new();

    /// <inheritdoc />
    public BoxOfKundumEventConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BoxOfKundumEventConfiguration
    {
        StartMessage = "Box of Kundum event has started!",
        AnnouncementMinutesBefore = 2,
        AnnouncementMessage = "A Box of Kundum event starts soon!",
        DropIntervalSeconds = 3,
        Entries =
        [
            new BoxOfKundumEventConfiguration.BoxOfKundumEventEntry
            {
                Time = new TimeOnly(20, 0),
                Level = 7,
                Amount = 100,
            },
        ],
    };

    /// <inheritdoc />
    public void ForceStart()
    {
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var configuration = this.Configuration;
        if (configuration is null || configuration.Entries.Count == 0)
        {
            return;
        }

        var states = EventStates.GetOrAdd(gameContext, _ => new (DateTime, DateTime, DateTime, int)[configuration.Entries.Count]);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, gameContext.ServerTimeZone);
        var nowTime = TimeOnly.FromDateTime(nowLocal);
        var dropInterval = TimeSpan.FromSeconds(Math.Max(1, (int)configuration.DropIntervalSeconds));

        for (var i = 0; i < configuration.Entries.Count; i++)
        {
            var entry = configuration.Entries.ElementAt(i);
            var state = states[i];

            // Optional pre-event announcement, "AnnouncementMinutesBefore" minutes in advance.
            if (configuration.AnnouncementMinutesBefore > 0
                && BoxOfKundumEventSchedule.IsDue(nowTime, entry.Time.Add(TimeSpan.FromMinutes(-configuration.AnnouncementMinutesBefore)), state.Announced == default ? null : state.Announced, nowUtc))
            {
                state.Announced = nowUtc;
                states[i] = state;
                await this.AnnounceEventAsync(gameContext, configuration).ConfigureAwait(false);
            }

            // Start a new drop stream when the time window matches and no stream is running.
            if (state.Remaining == 0
                && BoxOfKundumEventSchedule.IsDue(nowTime, entry.Time, state.Fired == default ? null : state.Fired, nowUtc))
            {
                state.Fired = nowUtc;
                if (entry.Item is null || entry.Map is null)
                {
                    var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
                    logger.LogWarning("Box of kundum event at {Time} is not fully configured (item or map missing); skipping.", entry.Time);
                    states[i] = state;
                    continue;
                }

                state.Remaining = entry.Amount;
                state.LastDropUtc = DateTime.MinValue; // the first box drops immediately in this tick
                states[i] = state;
                await this.ShowStartMessageAsync(gameContext, configuration, entry, nowLocal, dropInterval).ConfigureAwait(false);
            }

            // Drop one box per interval while a stream is running.
            if (state.Remaining > 0 && nowUtc - state.LastDropUtc >= dropInterval)
            {
                state.LastDropUtc = nowUtc;
                state.Remaining--;
                states[i] = state;
                await this.DropSingleBoxAsync(gameContext, entry, state.Remaining).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask AnnounceEventAsync(GameContext gameContext, BoxOfKundumEventConfiguration configuration)
    {
        var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
        logger.LogInformation("Box of kundum event announcement fired: {Message}", configuration.AnnouncementMessage);
        await gameContext.ForEachPlayerAsync(async player =>
        {
            try
            {
                if (configuration.AnnouncementMessage.GetTranslation(player.Culture) is { Length: > 0 } message)
                {
                    await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.GoldenCenter)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Unexpected error showing the box of kundum event announcement.");
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the configurable start message to all players and logs the start of the drop stream.
    /// </summary>
    private async ValueTask ShowStartMessageAsync(GameContext gameContext, BoxOfKundumEventConfiguration configuration, BoxOfKundumEventConfiguration.BoxOfKundumEventEntry entry, DateTime nowLocal, TimeSpan dropInterval)
    {
        var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
        logger.LogInformation("Box of kundum event started at {Time}: dropping {Amount} '{ItemName}' (level {Level}) on map {MapNumber} at {Point}, one box every {Interval} seconds.", nowLocal, entry.Amount, entry.Item!.Name, entry.Level, entry.Map!.Number, new Point(entry.X, entry.Y), dropInterval.TotalSeconds);

        await gameContext.ForEachPlayerAsync(async player =>
        {
            try
            {
                if (configuration.StartMessage.GetTranslation(player.Culture) is { Length: > 0 } startMessage)
                {
                    await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(startMessage, MessageType.GoldenCenter)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Unexpected error showing the box of kundum event start message.");
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a single box at the configured coordinates and fires a fireworks effect at the
    /// drop point for every player on the map.
    /// </summary>
    private async ValueTask DropSingleBoxAsync(GameContext gameContext, BoxOfKundumEventConfiguration.BoxOfKundumEventEntry entry, int remaining)
    {
        var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
        if (entry.Item is null || entry.Map is null)
        {
            logger.LogWarning("Box of kundum event is not fully configured (item or map missing); skipping a box drop.");
            return;
        }

        var map = await gameContext.GetMapAsync((ushort)entry.Map.Number).ConfigureAwait(false);
        if (map is null)
        {
            logger.LogWarning("Box of kundum event: map {MapNumber} not found; skipping a box drop.", entry.Map.Number);
            return;
        }

        var point = new Point(entry.X, entry.Y);

        // TemporaryItem initializes the item option collections which the serializers
        // require - exactly like the regular drop generators do.
        var item = new TemporaryItem { Definition = entry.Item, Level = entry.Level };
        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        var droppedItem = new DroppedItem(item, point, map, null, null);
        await map.AddAsync(droppedItem).ConfigureAwait(false);

        logger.LogInformation("Box of kundum event: dropped '{ItemName}' (level {Level}) on map {MapNumber} at {Point}; {Remaining} boxes left.", entry.Item.Name, entry.Level, entry.Map.Number, point, remaining);

        await gameContext.ForEachPlayerAsync(async player =>
        {
            try
            {
                if (player.CurrentMap == map)
                {
                    await player.InvokeViewPlugInAsync<IShowItemDropEffectPlugIn>(p => p.ShowEffectAsync(ItemDropEffect.Fireworks, point)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Unexpected error showing box of kundum event effects.");
            }
        }).ConfigureAwait(false);
    }
}
