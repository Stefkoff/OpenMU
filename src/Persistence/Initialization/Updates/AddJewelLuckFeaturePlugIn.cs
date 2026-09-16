// <copyright file="AddJewelLuckFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the "Gem of Success" feature: the item definition, its magic effect definition
/// (dedicated number, player-only indicator) and the ~0.02% drop group on the
/// strongest regularly-spawning monster.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5E8F1A2B-3C4D-4E5F-9A0B-1C2D3E4F5A6B")]
public class AddJewelLuckFeaturePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Gem of Success feature";
    internal const string PlugInDescription = "Adds the Gem of Success item, its buff effect, and its drop group on the strongest monster.";

    private const byte ItemGroup = 14;
    private const short ItemNumber = 12;
    private const short StrongestMonsterNumber = 565; // Dark Iron Knight, LaCleon (map 57)
    // Effective rate = Chance / max(1, poolTotal); pool total measured 1.076 on LaCleon (2026-09-16) -> ~0.02% per kill.
    private const float DropGroupChance = 0.00022f;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddJewelLuckFeature;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 16, 0, 0, 0, DateTimeKind.Utc);

#pragma warning disable CS1998
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
#pragma warning restore CS1998
    {
        // 1. Buff effect definition (dedicated number, resolved by name at runtime).
        if (!gameConfiguration.MagicEffects.Any(e => e.Name.ValueInNeutralLanguage == JewelLuckBuffService.GemEffectName))
        {
            var effect = context.CreateNew<MagicEffectDefinition>();
            effect.Number = JewelLuckBuffService.GemEffectNumber;
            effect.Name = JewelLuckBuffService.GemEffectName;
            effect.InformObservers = false; // player-only indicator: OTHER players see nothing
            effect.StopByDeath = false;
            effect.Duration = context.CreateNew<PowerUpDefinitionValue>();
            effect.Duration.ConstantValue.Value = 3600; // fallback; exact remaining time is passed at runtime
            gameConfiguration.MagicEffects.Add(effect);
            // NOTE: no SetGuid — leave the id random like NpcInitialization does (the number is dedicated, so no guid collisions).
        }

        // 2. Item definition (idempotent).
        var itemDefinition = gameConfiguration.Items.FirstOrDefault(item => item.Group == ItemGroup && item.Number == ItemNumber);
        if (itemDefinition is null)
        {
            itemDefinition = context.CreateNew<ItemDefinition>();
            itemDefinition.Name = "Gem of Success";
            itemDefinition.Number = ItemNumber;
            itemDefinition.Group = ItemGroup;
            itemDefinition.DropLevel = 0;
            itemDefinition.DropsFromMonsters = false;
            itemDefinition.Durability = 1;
            itemDefinition.Width = 1;
            itemDefinition.Height = 1;
            itemDefinition.SetGuid(itemDefinition.Group, itemDefinition.Number);
            gameConfiguration.Items.Add(itemDefinition);
        }

        // 3. Monster drop group (idempotent re-wire, like AddRenaItemUpdatePlugIn).
        var dropItemGroup = gameConfiguration.DropItemGroups.FirstOrDefault(group => group.PossibleItems.Contains(itemDefinition));
        var strongestMonster = gameConfiguration.Monsters.FirstOrDefault(monster => monster.Number == StrongestMonsterNumber);
        if (dropItemGroup is null && strongestMonster is { })
        {
            dropItemGroup = context.CreateNew<DropItemGroup>();
            dropItemGroup.SetGuid(ItemGroup, ItemNumber);
            dropItemGroup.PossibleItems.Add(itemDefinition);
            dropItemGroup.Chance = DropGroupChance;
            dropItemGroup.Description = "The drop item group for Gem of Success";
            gameConfiguration.DropItemGroups.Add(dropItemGroup);
        }

        if (dropItemGroup is { } group && strongestMonster is { } monster && !monster.DropItemGroups.Contains(group))
        {
            monster.DropItemGroups.Add(group);
        }
    }
}
