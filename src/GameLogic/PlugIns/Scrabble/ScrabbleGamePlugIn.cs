// <copyright file="ScrabbleGamePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.Properties;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Scrabble game: at configured Timetable times, announces a scrambled word to all players
/// (with a game-level pre-start message). The first /scrabble guess wins the round and gets the
/// round's reward (zen or picked item). Maximum 5 rounds, 1 minute each (fixed slots; the word is
/// reprinted at the 30s mark if still unguessed; a won round waits out its slot with a "next round
/// in X seconds" countdown, a timed-out round advances immediately). After the last round the game
/// ends and waits for the next scheduled start.
/// </summary>
[Guid("2A6B7C8D-9E0F-4A3B-8C2D-1E5F6A7B8C91")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.ScrabbleGamePlugIn_Name),
    Description = nameof(PlugInResources.ScrabbleGamePlugIn_Description),
    ResourceType = typeof(PlugInResources))]
public class ScrabbleGamePlugIn : PeriodicTaskBasePlugIn<ScrabbleConfiguration, ScrabbleGameServerState>, IPeriodicTaskPlugIn, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>Fixed per-round deadline (user decision: max 1 minute per round).</summary>
    public static readonly TimeSpan RoundTimeout = TimeSpan.FromMinutes(1);

    /// <summary>Random used for word picking/scrambling (fine for a game, not security).</summary>
    private static readonly Random Random = new();

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        // Seed: 5 rounds with zen-only rewards (1M * round number) so the game works on install.
        // Admins later replace the zen with a picked item per round (full attribute set).
        return new ScrabbleConfiguration
        {
            StartMessage = "Scrabble game starting in {0} seconds!",
            EndMessage = "Scrabble game over! Thanks for playing!",
            TaskDuration = TimeSpan.FromMinutes(10), // safety margin; rounds self-pace at 1 min each
            PreStartMessageDelay = TimeSpan.FromSeconds(30), // game-level pre-start announcement
            Timetable = new List<TimeOnly> { new(20, 0), new(21, 0), new(22, 0) },
            Rounds =
            [
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "cat" }, new() { Word = "dog" }, new() { Word = "sun" } }, Reward = new() { RewardZen = 1_000_000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "hello" }, new() { Word = "world" }, new() { Word = "apple" } }, Reward = new() { RewardZen = 2_000_000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "banana" }, new() { Word = "orange" }, new() { Word = "purple" } }, Reward = new() { RewardZen = 3_000_000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "elephant" }, new() { Word = "guitar" }, new() { Word = "rainbow" } }, Reward = new() { RewardZen = 4_000_000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "adventure" }, new() { Word = "chocolate" }, new() { Word = "dinosaur" } }, Reward = new() { RewardZen = 5_000_000 } },
            ],
        };
    }

    /// <inheritdoc />
    protected override ScrabbleGameServerState CreateState(IGameContext gameContext)
        => new(gameContext);

    /// <summary>Test seam: exposes the per-context state (friend assembly MUnique.OpenMU.Tests).</summary>
    internal ScrabbleGameServerState GetStateForTest(IGameContext gameContext)
        => this.GetStateByGameContext(gameContext);

    /// <inheritdoc />
    protected override async ValueTask OnPrepareEventAsync(ScrabbleGameServerState state)
        => await ValueTask.CompletedTask;

    /// <inheritdoc />
    protected override async ValueTask OnPreparedAsync(ScrabbleGameServerState state)
    {
        // Game-level pre-start announcement: "Scrabble game starting in {delay} seconds!",
        // then the base waits PreStartMessageDelay before transitioning into Started.
        var config = this.Configuration;
        var message = config?.StartMessage.ToString();
        if (message?.Contains("{0}") == true && config?.PreStartMessageDelay is { } delay)
        {
            message = string.Format(message, (int)delay.TotalSeconds);
        }

        await this.BroadcastAsync(state, message ?? "Scrabble game starting! Unscramble the word!").ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask OnStartedAsync(ScrabbleGameServerState state)
    {
        // Round 1 starts exactly when the game enters Started (after the pre-start delay).
        this.StartRound(state, roundIndex: 0);
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask OnFinishedAsync(ScrabbleGameServerState state)
    {
        await this.BroadcastAsync(state, this.Configuration?.EndMessage.ToString() ?? "Scrabble game over! Thanks for playing!").ConfigureAwait(false);
        state.CurrentRoundIndex = -1;
        state.CurrentWord = null;
        state.ScrambledWord = null;
        state.RoundWon = false;
        state.MidRoundReminderSent = false;
        state.WinnerName = null;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var state = this.GetStateByGameContext(gameContext);

        if (state.CurrentRoundIndex >= 0)
        {
            // Round is running: fixed 1-minute slot from RoundStartUtc.
            var slotEndsAtUtc = state.RoundStartUtc + RoundTimeout;
            if (DateTime.UtcNow >= slotEndsAtUtc)
            {
                if (state.RoundWon)
                {
                    // Won early: the slot is respected, now it expired → advance or finish.
                    var config = this.Configuration ?? (ScrabbleConfiguration)this.CreateDefaultConfig();
                    this.StartNextRound(state, config);
                }
                else
                {
                    // Nobody guessed in time: next round starts immediately (same tick).
                    await this.OnRoundTimedOutAsync(gameContext, state).ConfigureAwait(false);
                }
            }
            else if (!state.RoundWon && !state.MidRoundReminderSent && DateTime.UtcNow >= state.RoundStartUtc + (RoundTimeout / 2))
            {
                // Mid-round reminder: reprint the scrambled word once at the 30-second mark
                // (only if the round hasn't been won yet).
                state.MidRoundReminderSent = true;
                await this.BroadcastAsync(state, $"Round {state.CurrentRoundIndex + 1}: Unscramble: {state.ScrambledWord} (30 seconds left!) - answer with /scrabble <word>!").ConfigureAwait(false);
            }

            return;
        }

        await base.ExecuteTaskAsync(gameContext).ConfigureAwait(false);
    }

    /// <summary>Handles a /scrabble guess. First correct answer wins the round.</summary>
    public async ValueTask<GuessResult> TryGuessAsync(IGameContext gameContext, Player player, string guess)
    {
        var state = this.GetStateByGameContext(gameContext);
        if (state.CurrentRoundIndex < 0 || state.RoundWon || state.CurrentWord is null)
        {
            return GuessResult.NoActiveRound;
        }

        if (!ScrambleHelper.IsCorrectGuess(guess, state.CurrentWord))
        {
            return GuessResult.Wrong; // private blue message only - never broadcast
        }

        state.RoundWon = true;
        state.WinnerName = player.SelectedCharacter?.Name ?? player.Name;
        var config = this.Configuration ?? (ScrabbleConfiguration)this.CreateDefaultConfig();
        var round = config.Rounds.ElementAt(state.CurrentRoundIndex);

        var rewardMessage = $"Player {state.WinnerName} won round {state.CurrentRoundIndex + 1}!";
        var granted = await this.TryGrantRewardAsync(player, round, state.CurrentRoundIndex).ConfigureAwait(false);
        rewardMessage += $" Reward: {granted}.";
        await this.BroadcastAsync(state, rewardMessage).ConfigureAwait(false);

        player.Logger.LogInformation(
            "Scrabble round {Round} won by {Winner} (word: {Word}, reward: {Granted}).",
            state.CurrentRoundIndex + 1, state.WinnerName, state.CurrentWord, granted);

        var hasNextRound = state.CurrentRoundIndex + 1 < config.Rounds.Count;
        if (hasNextRound)
        {
            // Fixed slot: announce the countdown, next round starts when this slot expires.
            var remainingSeconds = (int)Math.Max(1, Math.Floor((state.RoundStartUtc + RoundTimeout - DateTime.UtcNow).TotalSeconds));
            await this.BroadcastAsync(state, $"Next round start in {remainingSeconds} seconds").ConfigureAwait(false);
        }
        else
        {
            // Last round won: game over immediately (no next round, no slot wait).
            await this.FinishGameAsync(state).ConfigureAwait(false);
        }

        return GuessResult.Win;
    }

    private async ValueTask FinishGameAsync(ScrabbleGameServerState state)
    {
        state.State = PeriodicTaskState.NotStarted;
        await this.BroadcastAsync(state, this.Configuration?.EndMessage.ToString() ?? "Scrabble game over! Thanks for playing!").ConfigureAwait(false);
        state.CurrentRoundIndex = -1;
        state.CurrentWord = null;
        state.ScrambledWord = null;
        state.RoundWon = false;
        state.MidRoundReminderSent = false;
        state.WinnerName = null;
    }

    private void StartRound(ScrabbleGameServerState state, int roundIndex, ScrabbleConfiguration? config = null)
    {
        config ??= this.Configuration ?? (ScrabbleConfiguration)this.CreateDefaultConfig();
        if (roundIndex < 0 || roundIndex >= config.Rounds.Count)
        {
            return;
        }

        var round = config.Rounds.ElementAt(roundIndex);
        var word = ScrambleHelper.PickWord(round.Words.Select(w => w.Word).ToList(), Random);
        if (word is null)
        {
            return;
        }

        state.CurrentRoundIndex = roundIndex;
        state.CurrentWord = word;
        state.ScrambledWord = ScrambleHelper.Scramble(word, Random);
        state.RoundStartUtc = DateTime.UtcNow;
        state.RoundWon = false;
        state.MidRoundReminderSent = false;
        state.WinnerName = null;

        _ = this.BroadcastAsync(state, $"Round {roundIndex + 1}: Unscramble: {state.ScrambledWord} - answer with /scrabble <word>!").ConfigureAwait(false);
    }

    private void StartNextRound(ScrabbleGameServerState state, ScrabbleConfiguration config)
    {
        var next = state.CurrentRoundIndex + 1;
        if (next >= config.Rounds.Count)
        {
            return; // last round already ended; FinishGameAsync resets the state
        }

        this.StartRound(state, next, config);
    }

    private async ValueTask OnRoundTimedOutAsync(GameContext gameContext, ScrabbleGameServerState state)
    {
        var timedOutRound = state.CurrentRoundIndex;
        state.CurrentRoundIndex = -1;
        var message = $"Nobody guessed round {timedOutRound + 1}!";
        await this.BroadcastAsync(state, message).ConfigureAwait(false);

        var config = this.Configuration ?? (ScrabbleConfiguration)this.CreateDefaultConfig();
        if (timedOutRound + 1 >= config.Rounds.Count)
        {
            await this.FinishGameAsync(state).ConfigureAwait(false);
            return;
        }

        this.StartRound(state, timedOutRound + 1, config);
    }

    /// <summary>Grants the round reward: zen if no item is configured, the picked item otherwise. Returns a display description.</summary>
    private async ValueTask<string> TryGrantRewardAsync(Player player, ScrabbleRoundConfiguration round, int roundNumber)
    {
        var reward = round.Reward;
        if (reward is null || (reward.ItemDefinition is null && reward.RewardZen <= 0))
        {
            player.Logger.LogWarning("Scrabble round {Round} has no reward configured.", roundNumber + 1);
            return "none (no reward configured)";
        }

        if (reward.ItemDefinition is { } itemDefinition)
        {
            var arguments = new ItemChatCommandArgs
            {
                Group = itemDefinition.Group,
                Number = itemDefinition.Number,
                Level = reward.Level,
                Skill = reward.Skill,
                Luck = reward.Luck,
                Opt = reward.Option,
                ExcellentNumber = reward.ExcellentNumber,
                Ancient = reward.Ancient,
                AncientBonusLevel = reward.AncientBonusLevel,
            };

            // Build the item as a persistence-created entity so the EF inventory storage accepts
            // it (CollectionAdapter requires EntityFramework.Model.Item; TemporaryItem is rejected).
            var item = player.PersistenceContext.CreateNew<Item>();
            ItemChatCommandPlugIn.CreateItem(item, itemDefinition, arguments, player.PersistenceContext);

            // Durability override: CreateItem already applied the item default (stackables 1);
            // only override when the admin configured an explicit value (0 = use default).
            if (reward.Durability > 0)
            {
                item.Durability = reward.Durability;
            }

            // Add to inventory; drop at the player's feet if the inventory is full (minigame pattern).
            if (player.Inventory is not null && await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.InvokeViewPlugInAsync<MUnique.OpenMU.GameLogic.Views.Inventory.IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
                return $"{itemDefinition.Name} (item added to inventory)";
            }

            // The inventory rejected the item - remove the dangling persistence object again and
            // drop a fresh TemporaryItem at the player's feet instead.
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            if (player.CurrentMap is { } map)
            {
                var droppedItem = new DroppedItem(ItemChatCommandPlugIn.CreateItem(itemDefinition, arguments), player.RandomPosition, map, player, player.GetAsEnumerable());
                await map.AddAsync(droppedItem).ConfigureAwait(false);
                return $"{itemDefinition.Name} (inventory full - dropped at your feet)";
            }

            return $"{itemDefinition.Name} (could not be granted)";
        }

        // Zen reward (seed default). TryAddMoney only rejects negative totals; RewardZen is int.
        var zenGranted = player.TryAddMoney(reward.RewardZen);
        return zenGranted ? $"{reward.RewardZen:N0} zen" : "zen (could not be added)";
    }

    private async ValueTask BroadcastAsync(ScrabbleGameServerState state, string message)
    {
        await state.Context.ForEachPlayerAsync(async player =>
        {
            await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.GoldenCenter)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}

/// <summary>Result of a /scrabble guess.</summary>
public enum GuessResult
{
    Win,
    Wrong,
    NoActiveRound,
}
