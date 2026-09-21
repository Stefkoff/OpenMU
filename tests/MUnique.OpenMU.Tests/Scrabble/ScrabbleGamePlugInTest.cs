// <copyright file="ScrabbleGamePlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Scrabble;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

/// <summary>
/// Flow tests for <see cref="ScrabbleGamePlugIn"/>.
/// </summary>
[TestFixture]
public class ScrabbleGamePlugInTest
{
    private GameContext gameContext = null!;

    [SetUp]
    public void SetUp()
    {
        // Fresh context per test: the base plugin keeps a static per-(plugin type, context)
        // state dictionary, so sharing a context between tests leaks game state into the next test.
        this.gameContext = (GameContext)GameContextTestHelper.CreateGameContext();
    }

    private static ScrabbleConfiguration CreateConfiguration()
    {
        return new ScrabbleConfiguration
        {
            StartMessage = "Scrabble game starting in {0} seconds!",
            EndMessage = "Scrabble game over! Thanks for playing!",
            TaskDuration = TimeSpan.FromMinutes(10), // safety margin; rounds self-pace at 1 min each
            PreStartMessageDelay = TimeSpan.Zero, // no wait in tests
            Rounds =
            [
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "hello" }, new() { Word = "world" } }, Reward = new() { RewardZen = 1000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "apple" }, new() { Word = "orange" } }, Reward = new() { RewardZen = 1000 } },
                new() { Words = new List<ScrabbleWordConfiguration> { new() { Word = "banana" }, new() { Word = "cherry" } }, Reward = new() { RewardZen = 1000 } },
            ],
        };
    }

    [Test]
    public async ValueTask Flow_StartWrongGuessWinReminderSlotEnd_FullGameAsync()
    {
        var plugin = new ScrabbleGamePlugIn { Configuration = CreateConfiguration() };

        // Drive the base into Prepared, then Started (round 1 begins in OnStartedAsync).
        plugin.ForceStart();
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);

        var state = plugin.GetStateForTest(this.gameContext);
        Assert.That(state.CurrentRoundIndex, Is.EqualTo(0));
        Assert.That(state.ScrambledWord, Is.Not.Null);

        var player = await PlayerTestHelper.CreatePlayerAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(player.Money, Is.EqualTo(0));

        // Wrong guess: private, no win.
        var wrongResult = await plugin.TryGuessAsync(this.gameContext, player, "pizza").ConfigureAwait(false);
        Assert.That(wrongResult, Is.EqualTo(GuessResult.Wrong));
        Assert.That(state.RoundWon, Is.False);

        // Mid-round reminder fires exactly once (30 s mark, not won yet).
        state.RoundStartUtc = DateTime.UtcNow.AddSeconds(-31);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(state.MidRoundReminderSent, Is.True);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(state.MidRoundReminderSent, Is.True, "Reminder must be sent only once.");

        // Correct guess: win, reward granted, next round NOT started yet (fixed slot).
        state.RoundStartUtc = DateTime.UtcNow;
        var word = state.CurrentWord!;
        var winResult = await plugin.TryGuessAsync(this.gameContext, player, word).ConfigureAwait(false);
        Assert.That(winResult, Is.EqualTo(GuessResult.Win));
        Assert.That(state.RoundWon, Is.True);
        Assert.That(state.CurrentRoundIndex, Is.EqualTo(0), "Next round must wait for the slot to expire.");
        Assert.That(player.Money, Is.EqualTo(1000), "Zen reward must be granted.");

        // Slot expired after a win → next round starts.
        state.RoundStartUtc = DateTime.UtcNow.AddMinutes(-1.5);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(state.CurrentRoundIndex, Is.EqualTo(1));
        Assert.That(state.MidRoundReminderSent, Is.False, "Fresh round must reset the reminder flag.");

        // Win round 2 BEFORE the 30 s mark: no reminder may fire after a win.
        var word2 = state.CurrentWord!;
        var win2 = await plugin.TryGuessAsync(this.gameContext, player, word2).ConfigureAwait(false);
        Assert.That(win2, Is.EqualTo(GuessResult.Win));
        Assert.That(state.RoundWon, Is.True);
        state.RoundStartUtc = DateTime.UtcNow.AddSeconds(-45); // past midpoint, before slot end
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(state.MidRoundReminderSent, Is.False, "No reminder may fire after the round is won.");
        Assert.That(player.Money, Is.EqualTo(2000));

        // Slot expired → last round starts.
        state.RoundStartUtc = DateTime.UtcNow.AddMinutes(-1.5);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        Assert.That(state.CurrentRoundIndex, Is.EqualTo(2));

        // Winning the last round ends the game immediately.
        var word3 = state.CurrentWord!;
        var win3 = await plugin.TryGuessAsync(this.gameContext, player, word3).ConfigureAwait(false);
        Assert.That(win3, Is.EqualTo(GuessResult.Win));
        Assert.That(state.State, Is.EqualTo(PeriodicTaskState.NotStarted), "Game must end after the last round.");
        Assert.That(state.CurrentRoundIndex, Is.EqualTo(-1));
        Assert.That(player.Money, Is.EqualTo(3000));
    }

    [Test]
    public async ValueTask TryGuessAsync_NoActiveGame_ReturnsNoActiveRoundAsync()
    {
        var plugin = new ScrabbleGamePlugIn { Configuration = CreateConfiguration() };
        var player = await PlayerTestHelper.CreatePlayerAsync(this.gameContext).ConfigureAwait(false);

        var result = await plugin.TryGuessAsync(this.gameContext, player, "hello").ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(GuessResult.NoActiveRound));
    }

    [Test]
    public async ValueTask Flow_ItemReward_GrantedIntoInventoryAsync()
    {
        var itemDefinition = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition
        {
            Name = "Test Sword",
            Group = 0,
            Number = 1,
            Durability = 100,
            Width = 1,
            Height = 2,
        };
        var luckOptionDefinition = new MUnique.OpenMU.Persistence.BasicModel.ItemOptionDefinition();
        luckOptionDefinition.PossibleOptions.Add(new MUnique.OpenMU.Persistence.BasicModel.IncreasableItemOption
        {
            OptionType = ItemOptionTypes.Luck,
        });
        itemDefinition.PossibleItemOptions.Add(luckOptionDefinition);
        var config = new ScrabbleConfiguration
        {
            PreStartMessageDelay = TimeSpan.Zero,
            TaskDuration = TimeSpan.FromMinutes(10),
            Rounds =
            [
                new()
                {
                    Words = new List<ScrabbleWordConfiguration> { new() { Word = "hello" }, new() { Word = "world" } },
                    Reward = new() { ItemDefinition = itemDefinition, Level = 3, Luck = true },
                },
            ],
        };
        var plugin = new ScrabbleGamePlugIn { Configuration = config };

        plugin.ForceStart();
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);
        await plugin.ExecuteTaskAsync(this.gameContext).ConfigureAwait(false);

        var state = plugin.GetStateForTest(this.gameContext);
        var player = await PlayerTestHelper.CreatePlayerAsync(this.gameContext).ConfigureAwait(false);

        var result = await plugin.TryGuessAsync(this.gameContext, player, state.CurrentWord!).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(GuessResult.Win));
        var inventoryItem = player.Inventory!.Items.Single();
        Assert.That(inventoryItem, Is.TypeOf<MUnique.OpenMU.Persistence.BasicModel.Item>(), "The reward item must be created through the persistence context (EF requires its own Item type).");
        Assert.That(inventoryItem.Definition, Is.SameAs(itemDefinition));
        Assert.That(inventoryItem.Level, Is.EqualTo(3));
        Assert.That(inventoryItem.ItemOptions, Has.Count.EqualTo(1), "Luck option must be applied.");
    }
}
