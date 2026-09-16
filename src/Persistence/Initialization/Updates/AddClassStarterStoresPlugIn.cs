// <copyright file="AddClassStarterStoresPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update turns seven existing merchant NPCs into per-class starter shops.
/// Each store is replaced by a starter kit of its class: a full set and weapons at
/// +6 with luck and a +8 option, weapons with skill, the class skill orbs/scrolls,
/// potions, and the first-level wing of the class.
/// </summary>
/// <remarks>
/// Affected NPCs and their classes:
/// Zienna the Weapons Merchant (246) - Dark Knight, Pasi The Mage (254) - Dark Wizard,
/// Elf Lala (242) - Fairy Elf, Hanzo The Blacksmith (251) - Magic Gladiator,
/// Wandering Merchant Zyro (568) - Dark Lord, Rhea (416) - Summoner,
/// Moss The Merchant (492) - Rage Fighter.
/// Knights/wizards/elves get their shop instead of the old stock; Zyro and Moss get a
/// newly created store because they had none.
/// </remarks>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("360c91ed-28cf-46dd-be55-2e1d0ffe1098")]
public class AddClassStarterStoresPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Class starter stores";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Replaces the stores of Zienna (Dark Knight), Pasi (Dark Wizard), Elf Lala (Fairy Elf), Hanzo (Magic Gladiator), Wandering Merchant Zyro (Dark Lord), Rhea (Summoner) and Moss The Merchant (Rage Fighter) with per-class starter kits: sets and weapons at +6 with luck and a +8 option, weapons with skill, class skill orbs/scrolls, potions and the first-level wing of each class.";

    /// <summary>
    /// The item level of the starter equipment - everything is sold at +6.
    /// </summary>
    private const byte StarterItemLevel = 6;

    /// <summary>
    /// The option level of the starter equipment - renders as a +8 option in the client.
    /// </summary>
    private const byte StarterOptionLevel = 2;

    /// <summary>
    /// Whether starter equipment gets the luck option.
    /// </summary>
    private const bool StarterLuck = true;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddClassStarterStores;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var itemHelper = new ItemHelper(context, gameConfiguration);

        // NPC number -> shop items, in store slot order.
        var starterShops = new (short NpcNumber, List<Item> Items)[]
        {
            (246, this.CreateDarkKnightShop(itemHelper)),
            (254, this.CreateDarkWizardShop(itemHelper)),
            (242, this.CreateFairyElfShop(itemHelper)),
            (251, this.CreateMagicGladiatorShop(itemHelper)),
            (568, this.CreateDarkLordShop(itemHelper)),
            (416, this.CreateSummonerShop(itemHelper)),
            (492, this.CreateRageFighterShop(itemHelper)),
        };

        foreach (var (npcNumber, items) in starterShops)
        {
            var npc = gameConfiguration.Monsters.FirstOrDefault(m => m.Number == npcNumber);
            if (npc is null)
            {
                continue;
            }

            var storage = npc.MerchantStore;
            if (storage is not null)
            {
                // Replace the store contents while keeping the existing store (and its GUID).
                // Note: storage.Items.Clear() is broken in this fork's CollectionAdapter
                // (NotifyCollectionChangedAction.Reset with changed items throws), so remove
                // the items one by one instead.
                foreach (var existingItem in storage.Items.ToList())
                {
                    storage.Items.Remove(existingItem);
                }
            }
            else
            {
                // The NPC has no store yet - create a new one.
                storage = context.CreateNew<ItemStorage>();
                storage.SetGuid(npc.Number);
                npc.MerchantStore = storage;
            }

            foreach (var item in items)
            {
                storage.Items.Add(item);
            }
        }

        return default;
    }

    private List<Item> CreateDarkKnightShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            itemHelper.CreateSetItem(0, 6, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 6, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 6, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 6, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(4, 6, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(5, ItemGroups.Swords, 6, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Gladius
            itemHelper.CreateWeapon(6, ItemGroups.Swords, 7, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Falchion
            itemHelper.CreateWeapon(7, ItemGroups.Swords, 9, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Sword of Salamander
            itemHelper.CreateWeapon(8, ItemGroups.Swords, 10, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Light Saber
            itemHelper.CreateShield(9, 6, true, null, StarterItemLevel, StarterOptionLevel, StarterLuck),                     // Skull Shield

            itemHelper.CreateOrb(10, 7),   // Orb of Twisting Slash
            itemHelper.CreateOrb(11, 13),  // Orb of Impale
            itemHelper.CreateOrb(12, 19),  // Orb of Death Stab
            itemHelper.CreateOrb(13, 17),  // Orb of Penetration

            itemHelper.CreateEquippableItem(14, ItemGroups.Orbs, 2, StarterItemLevel, 0, false, null), // Wings of Satan

            itemHelper.CreatePotion(15, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreateItem(16, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateDarkWizardShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            itemHelper.CreateSetItem(0, 4, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 4, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 4, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 4, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(4, 4, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(5, ItemGroups.Staff, 1, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Angelic Staff
            itemHelper.CreateWeapon(6, ItemGroups.Staff, 2, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Serpent Staff
            itemHelper.CreateWeapon(7, ItemGroups.Staff, 3, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Thunder Staff
            itemHelper.CreateShield(8, 14, false, null, StarterItemLevel, StarterOptionLevel, StarterLuck),                   // Legendary Shield

            itemHelper.CreateScroll(9, 0),   // Scroll of Poison
            itemHelper.CreateScroll(10, 3),  // Scroll of Fire Ball
            itemHelper.CreateScroll(11, 10), // Scroll of Power Wave
            itemHelper.CreateScroll(12, 1),  // Scroll of Meteorite
            itemHelper.CreateScroll(13, 2),  // Scroll of Lighting
            itemHelper.CreateScroll(14, 6),  // Scroll of Ice
            itemHelper.CreateScroll(15, 5),  // Scroll of Teleport
            itemHelper.CreateScroll(16, 4),  // Scroll of Flame
            itemHelper.CreateScroll(17, 7),  // Scroll of Twister
            itemHelper.CreateScroll(18, 8),  // Scroll of Evil Spirit
            itemHelper.CreateScroll(19, 9),  // Scroll of Hellfire

            itemHelper.CreateEquippableItem(20, ItemGroups.Orbs, 1, StarterItemLevel, 0, false, null), // Wings of Heaven

            itemHelper.CreatePotion(21, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreatePotion(22, 6, 30, 1),      // Large Mana Potion +1 x30
            itemHelper.CreateItem(23, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateFairyElfShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            itemHelper.CreateSetItem(0, 11, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 11, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 11, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 11, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(4, 11, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(5, ItemGroups.Bows, 0, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Short Bow
            itemHelper.CreateWeapon(6, ItemGroups.Bows, 2, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Elven Bow
            itemHelper.CreateWeapon(7, ItemGroups.Bows, 3, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Battle Bow
            itemHelper.CreateWeapon(8, ItemGroups.Bows, 4, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Tiger Bow
            itemHelper.CreateWeapon(9, ItemGroups.Bows, 8, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Crossbow
            itemHelper.CreateWeapon(10, ItemGroups.Bows, 9, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Golden Crossbow
            itemHelper.CreateWeapon(11, ItemGroups.Bows, 11, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Light Crossbow
            itemHelper.CreateShield(12, 3, false, null, StarterItemLevel, StarterOptionLevel, StarterLuck),                   // Elven Shield

            itemHelper.CreateItem(13, 15, (byte)ItemGroups.Bows, 200, 0), // Arrows x200
            itemHelper.CreateItem(14, 7, (byte)ItemGroups.Bows, 200, 0),  // Bolts x200

            itemHelper.CreateOrb(15, 8),   // Orb of Healing
            itemHelper.CreateOrb(16, 9),   // Orb of Greater Defense
            itemHelper.CreateOrb(17, 10),  // Orb of Greater Damage
            itemHelper.CreateOrb(18, 11),  // Orb of Summoning
            itemHelper.CreateOrb(19, 18),  // Orb of Ice Arrow

            itemHelper.CreateEquippableItem(20, ItemGroups.Orbs, 0, StarterItemLevel, 0, false, null), // Wings of Elf

            itemHelper.CreatePotion(21, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreateItem(22, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateMagicGladiatorShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            // Note: the Magic Gladiator can't wear a helm, so the set has no helm piece.
            itemHelper.CreateSetItem(0, 6, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 6, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 6, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 6, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(4, ItemGroups.Swords, 6, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Gladius
            itemHelper.CreateWeapon(5, ItemGroups.Swords, 10, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Light Saber
            itemHelper.CreateWeapon(6, ItemGroups.Swords, 13, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Double Blade
            itemHelper.CreateWeapon(7, ItemGroups.Spears, 0, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null),  // Light Spear
            itemHelper.CreateShield(8, 6, true, null, StarterItemLevel, StarterOptionLevel, StarterLuck),                      // Skull Shield

            itemHelper.CreateOrb(9, 16),   // Orb of Fire Slash

            itemHelper.CreateScroll(10, 3),  // Scroll of Fire Ball
            itemHelper.CreateScroll(11, 1),  // Scroll of Meteorite
            itemHelper.CreateScroll(12, 10), // Scroll of Power Wave
            itemHelper.CreateScroll(13, 29), // Scroll of Gigantic Storm

            itemHelper.CreateEquippableItem(14, ItemGroups.Orbs, 1, StarterItemLevel, 0, false, null), // Wings of Heaven

            itemHelper.CreatePotion(15, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreateItem(16, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateDarkLordShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            itemHelper.CreateSetItem(0, 25, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 25, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 25, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 25, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(4, 25, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(5, ItemGroups.Scepters, 0, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Mace
            itemHelper.CreateWeapon(6, ItemGroups.Scepters, 10, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Great Scepter
            itemHelper.CreateWeapon(7, ItemGroups.Scepters, 11, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Lord Scepter
            itemHelper.CreateShield(8, 2, false, null, StarterItemLevel, StarterOptionLevel, StarterLuck),                       // Kite Shield

            itemHelper.CreateOrb(9, 21),  // Scroll of FireBurst
            itemHelper.CreateOrb(10, 22), // Scroll of Summon
            itemHelper.CreateOrb(11, 23), // Scroll of Critical Damage
            itemHelper.CreateOrb(12, 24), // Scroll of Electric Spark
            itemHelper.CreateOrb(13, 35), // Scroll of Fire Scream

            itemHelper.CreateEquippableItem(14, ItemGroups.Misc1, 30, StarterItemLevel, 0, false, null), // Cape of Lord

            itemHelper.CreatePotion(15, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreateItem(16, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateSummonerShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            itemHelper.CreateSetItem(0, 40, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 40, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 40, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 40, ItemGroups.Gloves, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(4, 40, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(5, ItemGroups.Staff, 14, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Mistery Stick
            itemHelper.CreateWeapon(6, ItemGroups.Staff, 15, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Violent Wind Stick
            itemHelper.CreateWeapon(7, ItemGroups.Staff, 16, StarterItemLevel, StarterOptionLevel, StarterLuck, false, null), // Red Wing Stick

            itemHelper.CreateScroll(8, 19),  // Chain Lightning Parchment
            itemHelper.CreateScroll(9, 20),  // Drain Life Parchment
            itemHelper.CreateScroll(10, 21), // Lightning Shock Parchment
            itemHelper.CreateScroll(11, 22), // Damage Reflection Parchment
            itemHelper.CreateScroll(12, 23), // Berserker Parchment
            itemHelper.CreateScroll(13, 24), // Sleep Parchment
            itemHelper.CreateScroll(14, 26), // Weakness Parchment
            itemHelper.CreateScroll(15, 27), // Innovation Parchment

            itemHelper.CreateEquippableItem(16, ItemGroups.Orbs, 41, StarterItemLevel, 0, false, null), // Wings of Curse

            itemHelper.CreatePotion(17, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreatePotion(18, 6, 30, 1),      // Large Mana Potion +1 x30
            itemHelper.CreateItem(19, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }

    private List<Item> CreateRageFighterShop(ItemHelper itemHelper)
    {
        List<Item> itemList = new()
        {
            // Note: the Rage Fighter has no regular gloves - his weapons are the gloves (group 0).
            itemHelper.CreateSetItem(0, 59, ItemGroups.Helm, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(1, 59, ItemGroups.Armor, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(2, 59, ItemGroups.Pants, null, StarterItemLevel, StarterOptionLevel, StarterLuck),
            itemHelper.CreateSetItem(3, 59, ItemGroups.Boots, null, StarterItemLevel, StarterOptionLevel, StarterLuck),

            itemHelper.CreateWeapon(4, ItemGroups.Swords, 32, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Sacred Glove
            itemHelper.CreateWeapon(5, ItemGroups.Swords, 33, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Storm Hard Glove
            itemHelper.CreateWeapon(6, ItemGroups.Swords, 34, StarterItemLevel, StarterOptionLevel, StarterLuck, true, null), // Piercing Blade Glove

            itemHelper.CreateScroll(7, 30),  // Chain Drive Parchment
            itemHelper.CreateScroll(8, 31),  // Dark Side Parchment
            itemHelper.CreateScroll(9, 32),  // Dragon Roar Parchment
            itemHelper.CreateScroll(10, 33), // Dragon Slasher Parchment
            itemHelper.CreateScroll(11, 34), // Ignore Defense Parchment
            itemHelper.CreateScroll(12, 35), // Increase Health Parchment

            itemHelper.CreateEquippableItem(13, ItemGroups.Orbs, 49, StarterItemLevel, 0, false, null), // Cape of Fighter

            itemHelper.CreatePotion(14, 3, 30, 1),      // Large Healing Potion +1 x30
            itemHelper.CreateItem(15, 10, (byte)ItemGroups.Misc2, 1, 0), // Town Portal Scroll
        };

        return itemList;
    }
}
