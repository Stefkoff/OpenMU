// <copyright file="JewelLuckBuffConsumeHandlerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;

/// <summary>
/// Tests the "Gem of Success" consume handler: activation consumes the item and
/// starts the buff; a second use while active is refused without consuming.
/// </summary>
[TestFixture]
public class JewelLuckBuffConsumeHandlerTest
{
    private const int ItemSlot = 12;

    [Test]
    public async ValueTask Consume_ActivatesBuffAndConsumesItemAsync()
    {
        var player = await this.GetPlayerAsync().ConfigureAwait(false);
        Mock.Get(player.GameContext.Configuration)
            .Setup(c => c.MagicEffects)
            .Returns(new List<MagicEffectDefinition>());
        player.GameContext.Configuration.MagicEffects.Add(new Persistence.BasicModel.MagicEffectDefinition
        {
            Number = JewelLuckBuffService.GemEffectNumber,
            Name = JewelLuckBuffService.GemEffectName,
            InformObservers = false,
        });

        var item = this.GetItem();
        await player.Inventory!.AddItemAsync(ItemSlot, item).ConfigureAwait(false);

        var handler = new JewelLuckBuffConsumeHandlerPlugIn();
        var success = await handler.ConsumeItemAsync(player, item, null, FruitUsage.Undefined).ConfigureAwait(false);

        Assert.That(success, Is.True);
        Assert.That(item.Durability, Is.EqualTo(0));
        Assert.That(player.SelectedCharacter!.JewelLuckBuffEndsAt, Is.Not.Null);
        Assert.That(player.MagicEffectList.ActiveEffects.ContainsKey(JewelLuckBuffService.GemEffectNumber), Is.True);
    }

    [Test]
    public async ValueTask Consume_RefusedWhenAlreadyActiveAsync()
    {
        var player = await this.GetPlayerAsync().ConfigureAwait(false);
        var endsAt = DateTime.UtcNow.AddHours(1);
        player.SelectedCharacter!.JewelLuckBuffEndsAt = endsAt;

        var item = this.GetItem();
        await player.Inventory!.AddItemAsync(ItemSlot, item).ConfigureAwait(false);

        var handler = new JewelLuckBuffConsumeHandlerPlugIn();
        var success = await handler.ConsumeItemAsync(player, item, null, FruitUsage.Undefined).ConfigureAwait(false);

        Assert.That(success, Is.False); // refused
        Assert.That(item.Durability, Is.EqualTo(1)); // item NOT consumed
        Assert.That(player.SelectedCharacter!.JewelLuckBuffEndsAt, Is.EqualTo(endsAt)); // unchanged (no refresh)
    }

    private async ValueTask<Player> GetPlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);

        player.SelectedCharacter!.Attributes.Add(new StatAttribute(Stats.Level, 100));
        player.SelectedCharacter.Attributes.Add(new StatAttribute(Stats.CurrentHealth, 0));
        player.SelectedCharacter.Attributes.Add(new StatAttribute(Stats.CurrentMana, 0));
        player.SelectedCharacter.Attributes.Add(new StatAttribute(Stats.CurrentShield, 0));

        return player;
    }

    private Item GetItem()
    {
        return new()
        {
            Definition = new ItemDefinition { Width = 1, Height = 1 },
            Durability = 1,
        };
    }
}
