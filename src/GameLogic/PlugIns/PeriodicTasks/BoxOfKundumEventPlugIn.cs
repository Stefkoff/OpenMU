// <copyright file="BoxOfKundumEventPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin which drops Box of Luck / Box of Kundum items at configured times and locations,
/// fires fireworks at the drop point and announces the event to all players in advance.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.BoxOfKundumEventPlugIn_Name), Description = nameof(PlugInResources.BoxOfKundumEventPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A4F6C3E2-7B1D-4E9A-9C5F-2D8A1B3C4D5E")]
public class BoxOfKundumEventPlugIn : IPeriodicTaskPlugIn, ISupportCustomConfiguration<BoxOfKundumEventConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    // Per game context and per entry: (last event fire, last pre-announcement).
    private static readonly ConcurrentDictionary<IGameContext, (DateTime Fired, DateTime Announced)[]> LastFiredPerEntry = new();

    /// <inheritdoc />
    public BoxOfKundumEventConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BoxOfKundumEventConfiguration
    {
        StartMessage = "Box of Kundum event has started!",
        AnnouncementMinutesBefore = 2,
        AnnouncementMessage = "A Box of Kundum event starts soon!",
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

        var lastFired = LastFiredPerEntry.GetOrAdd(gameContext, _ => new (DateTime, DateTime)[configuration.Entries.Count]);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, gameContext.ServerTimeZone);
        var nowTime = TimeOnly.FromDateTime(nowLocal);

        for (var i = 0; i < configuration.Entries.Count; i++)
        {
            var entry = configuration.Entries.ElementAt(i);

            // Optional pre-event announcement, "AnnouncementMinutesBefore" minutes in advance.
            if (configuration.AnnouncementMinutesBefore > 0
                && BoxOfKundumEventSchedule.IsDue(nowTime, entry.Time.Add(TimeSpan.FromMinutes(-configuration.AnnouncementMinutesBefore)), lastFired[i].Announced == default ? null : lastFired[i].Announced, nowUtc))
            {
                lastFired[i].Announced = nowUtc;
                await this.AnnounceEventAsync(gameContext, configuration).ConfigureAwait(false);
            }

            if (!BoxOfKundumEventSchedule.IsDue(nowTime, entry.Time, lastFired[i].Fired == default ? null : lastFired[i].Fired, nowUtc))
            {
                continue;
            }

            lastFired[i].Fired = nowUtc;
            await this.SpawnEventAsync(gameContext, entry, nowLocal).ConfigureAwait(false);
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

    private async ValueTask SpawnEventAsync(GameContext gameContext, BoxOfKundumEventConfiguration.BoxOfKundumEventEntry entry, DateTime nowLocal)
    {
        var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
        if (entry.Item is null || entry.Map is null)
        {
            logger.LogWarning("Box of kundum event at {Time} is not fully configured (item or map missing); skipping.", entry.Time);
            return;
        }

        var map = await gameContext.GetMapAsync((ushort)entry.Map.Number).ConfigureAwait(false);
        if (map is null)
        {
            logger.LogWarning("Box of kundum event: map {MapNumber} not found; skipping.", entry.Map.Number);
            return;
        }

        var point = new Point(entry.X, entry.Y);
        for (var i = 0; i < entry.Amount; i++)
        {
            var item = new Item { Definition = entry.Item, Level = entry.Level };
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            var droppedItem = new DroppedItem(item, point, map, null, null);
            await map.AddAsync(droppedItem).ConfigureAwait(false);
        }

        logger.LogInformation("Box of kundum event fired at {Time}: dropped {Amount} '{ItemName}' (level {Level}) on map {MapNumber} at {Point}; every player can pick them up.", nowLocal, entry.Amount, entry.Item.Name, entry.Level, entry.Map.Number, point);

        await gameContext.ForEachPlayerAsync(async player =>
        {
            try
            {
                if (player.CurrentMap == map)
                {
                    await player.InvokeViewPlugInAsync<IShowItemDropEffectPlugIn>(p => p.ShowEffectAsync(ItemDropEffect.Fireworks, point)).ConfigureAwait(false);
                }

                if (this.Configuration?.StartMessage.GetTranslation(player.Culture) is { Length: > 0 } startMessage)
                {
                    await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(startMessage, MessageType.GoldenCenter)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Unexpected error showing box of kundum event effects.");
            }
        }).ConfigureAwait(false);
    }
}
