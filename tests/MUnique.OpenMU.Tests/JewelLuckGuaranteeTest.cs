// <copyright file="JewelLuckGuaranteeTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;

/// <summary>
/// Tests that the "Gem of Success" buff forces jewel upgrade success in the two
/// shared upgrade base classes (Soul via UpgradeItemLevelJewelConsumeHandlerPlugIn,
/// Life via ItemUpgradeConsumeHandlerPlugIn).
/// </summary>
[TestFixture]
public class JewelLuckGuaranteeTest
{
    private const int ItemSlot = 12;

    [Test]
    public async ValueTask SoulJewel_GuaranteedWhenBuffActiveAsync()
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextRandomBool(50)).Returns(false); // hard-fail at the default rate
        randomizer.Setup(r => r.NextRandomBool(100)).Returns(true); // forced success at the buffed rate
        var consumeHandler = new SoulJewelConsumeHandlerPlugIn(randomizer.Object);

        var player = await this.GetPlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.JewelLuckBuffEndsAt = DateTime.UtcNow.AddHours(1);

        var upgradeableItem = this.GetItemWithPossibleOption();
        upgradeableItem.Level = 5;
        await player.Inventory!.AddItemAsync((byte)(ItemSlot + 1), upgradeableItem).ConfigureAwait(false);
        var soul = this.GetItem();
        await player.Inventory.AddItemAsync(ItemSlot, soul).ConfigureAwait(false);
        soul.Durability = 1;

        var consumed = await consumeHandler.ConsumeItemAsync(player, soul, upgradeableItem, FruitUsage.Undefined).ConfigureAwait(false);

        Assert.That(consumed, Is.True);
        Assert.That(upgradeableItem.Level, Is.EqualTo(6)); // forced success despite failing randomizer
    }

    [Test]
    public async ValueTask SoulJewel_FailsWithoutBuffAsync()
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextRandomBool(50)).Returns(false);
        var consumeHandler = new SoulJewelConsumeHandlerPlugIn(randomizer.Object);

        var player = await this.GetPlayerAsync().ConfigureAwait(false);
        var upgradeableItem = this.GetItemWithPossibleOption();
        upgradeableItem.Level = 5;
        await player.Inventory!.AddItemAsync((byte)(ItemSlot + 1), upgradeableItem).ConfigureAwait(false);
        var soul = this.GetItem();
        await player.Inventory.AddItemAsync(ItemSlot, soul).ConfigureAwait(false);
        soul.Durability = 1;

        var consumed = await consumeHandler.ConsumeItemAsync(player, soul, upgradeableItem, FruitUsage.Undefined).ConfigureAwait(false);

        Assert.That(consumed, Is.True);
        Assert.That(upgradeableItem.Level, Is.EqualTo(4)); // fail path: -1
    }

    [Test]
    public async ValueTask LifeJewel_GuaranteedWhenBuffActiveAsync()
    {
        var consumeHandler = new LifeJewelConsumeHandlerPlugIn();
        consumeHandler.Configuration.SuccessChance = 0; // would never add without the buff
        var player = await this.GetPlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.JewelLuckBuffEndsAt = DateTime.UtcNow.AddHours(1);
        var upgradeableItem = this.GetItemWithPossibleOption();
        await player.Inventory!.AddItemAsync((byte)(ItemSlot + 1), upgradeableItem).ConfigureAwait(false);
        var life = this.GetItem();
        await player.Inventory.AddItemAsync(ItemSlot, life).ConfigureAwait(false);
        life.Durability = 1;

        var consumed = await consumeHandler.ConsumeItemAsync(player, life, upgradeableItem, FruitUsage.Undefined).ConfigureAwait(false);

        Assert.That(consumed, Is.True);
        Assert.That(upgradeableItem.ItemOptions.Count, Is.EqualTo(1));
        Assert.That(upgradeableItem.ItemOptions.First().Level, Is.EqualTo(1));
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

    private Item GetItemWithPossibleOption()
    {
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        definition.Setup(d => d.BasePowerUpAttributes).Returns(new List<ItemBasePowerUpDefinition>());
        definition.Object.MaximumItemLevel = 15;
        var itemSlot = new Mock<ItemSlotType>();
        itemSlot.Setup(s => s.ItemSlots).Returns(new List<int> { InventoryConstants.LeftHandSlot });
        definition.Setup(d => d.ItemSlot).Returns(itemSlot.Object);
        item.Object.Definition = definition.Object;
        item.Object.Durability = 1;
        item.Object.Definition.Width = 1;
        item.Object.Definition.Height = 2;
        var option = new Mock<ItemOptionDefinition>();
        option.SetupAllProperties();
        option.Setup(o => o.PossibleOptions).Returns(new List<IncreasableItemOption>());
        option.Object.MaximumOptionsPerItem = 4;
        option.Object.AddsRandomly = true;
        option.Name = "Damage Option";

        var possibleOption = new Mock<IncreasableItemOption>();
        possibleOption.SetupAllProperties();
        possibleOption.Setup(o => o.LevelDependentOptions).Returns(new List<ItemOptionOfLevel>());
        possibleOption.Object.OptionType = ItemOptionTypes.Option;
        option.Object.PossibleOptions.Add(possibleOption.Object);
        for (int level = 1; level <= 4; level++)
        {
            var levelDependentOption = new ItemOptionOfLevel();
            levelDependentOption.Level = level;
            possibleOption.Object.LevelDependentOptions.Add(levelDependentOption);
        }

        item.Object.Definition.PossibleItemOptions.Add(option.Object);
        return item.Object;
    }
}
