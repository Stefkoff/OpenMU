// <copyright file="JewelLuckBuffServiceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;

/// <summary>
/// Tests for the "Gem of Success" buff state: real-time expiry, activation,
/// the client indicator effect, and login-restore.
/// </summary>
[TestFixture]
public class JewelLuckBuffServiceTest
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private Player? _player;

    [SetUp]
    public async Task Setup()
    {
        this._player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        JewelLuckBuffService.Clock = () => Now;

        // The mocked GameConfiguration returns null for MagicEffects unless set up.
        Mock.Get(this._player.GameContext.Configuration)
            .Setup(c => c.MagicEffects)
            .Returns(new List<MagicEffectDefinition>());
    }

    [TearDown]
    public void Teardown()
    {
        JewelLuckBuffService.Clock = () => DateTime.UtcNow;
    }

    [Test]
    public void Inactive_WhenEndsAtNull()
    {
        Assert.That(this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt, Is.Null);
        Assert.That(JewelLuckBuffService.IsActive(this.GetPlayer()), Is.False);
    }

    [Test]
    public void Active_WithinWindow()
    {
        this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt = Now.AddMinutes(30);
        Assert.That(JewelLuckBuffService.IsActive(this.GetPlayer()), Is.True);
    }

    [Test]
    public void Inactive_AfterExpiry_ClearsColumn()
    {
        this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt = Now.AddMinutes(-1);
        Assert.That(JewelLuckBuffService.IsActive(this.GetPlayer()), Is.False);
        Assert.That(this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt, Is.Null);
    }

    [Test]
    public async ValueTask Activate_SetsEndsAtAndEffectAsync()
    {
        this.AddGemEffectDefinition();

        JewelLuckBuffService.Activate(this.GetPlayer(), 3600);

        Assert.That(this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt, Is.Not.Null);
        Assert.That(JewelLuckBuffService.RemainingSeconds(this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt!.Value, Now), Is.EqualTo(3600).Within(1));
        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects.ContainsKey(JewelLuckBuffService.GemEffectNumber), Is.True);
        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects[JewelLuckBuffService.GemEffectNumber].Duration.TotalSeconds, Is.EqualTo(3600).Within(1));
    }

    [Test]
    public async ValueTask Activate_ReplacesExistingGemEffectAsync()
    {
        this.AddGemEffectDefinition();
        await this.GetPlayer().MagicEffectList.AddEffectAsync(
            new MagicEffect(TimeSpan.FromSeconds(60), this.GetGemDefinition())).ConfigureAwait(false);

        JewelLuckBuffService.Activate(this.GetPlayer(), 3600);

        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects.Count, Is.EqualTo(1));
        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects[JewelLuckBuffService.GemEffectNumber].Duration.TotalSeconds, Is.EqualTo(3600).Within(1));
    }

    [Test]
    public async ValueTask RestoreEffect_OnLoginAddsRemainingAsync()
    {
        this.AddGemEffectDefinition();
        this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt = Now.AddSeconds(600);

        await JewelLuckBuffService.RestoreEffectAsync(this.GetPlayer()).ConfigureAwait(false);

        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects.ContainsKey(JewelLuckBuffService.GemEffectNumber), Is.True);
        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects[JewelLuckBuffService.GemEffectNumber].Duration.TotalSeconds, Is.EqualTo(600).Within(1));
    }

    [Test]
    public async ValueTask RestoreEffect_ExpiredClearsColumnAsync()
    {
        this.AddGemEffectDefinition();
        this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt = Now.AddSeconds(-5);

        await JewelLuckBuffService.RestoreEffectAsync(this.GetPlayer()).ConfigureAwait(false);

        Assert.That(this.GetPlayer().SelectedCharacter!.JewelLuckBuffEndsAt, Is.Null);
        Assert.That(this.GetPlayer().MagicEffectList.ActiveEffects.ContainsKey(JewelLuckBuffService.GemEffectNumber), Is.False);
    }

    [Test]
    public void RemainingSeconds_ClampsAtZero()
    {
        Assert.That(JewelLuckBuffService.RemainingSeconds(Now.AddSeconds(-1), Now), Is.EqualTo(0));
    }

    private Player GetPlayer() => this._player!;

    private MagicEffectDefinition GetGemDefinition()
    {
        return new Persistence.BasicModel.MagicEffectDefinition
        {
            Number = JewelLuckBuffService.GemEffectNumber,
            Name = JewelLuckBuffService.GemEffectName,
            InformObservers = false,
            StopByDeath = false,
        };
    }

    private void AddGemEffectDefinition()
    {
        var config = this.GetPlayer().GameContext.Configuration;
        config.MagicEffects.Add(this.GetGemDefinition());
    }
}
