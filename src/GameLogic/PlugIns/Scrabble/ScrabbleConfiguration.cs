// <copyright file="ScrabbleConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Configuration for <see cref="ScrabbleGamePlugIn"/>. Start times come from the inherited
/// <see cref="PeriodicTaskConfiguration.Timetable"/> (multiple per day).
/// </summary>
public class ScrabbleConfiguration : PeriodicTaskConfiguration
{
    /// <summary>
    /// Gets or sets the rounds, played in order.
    /// </summary>
    [Display(Name = "Rounds", Description = "Rounds of the game, played in order (max 5).")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [MaxLength(5, ErrorMessage = "At most 5 rounds per game.")]
    public ICollection<ScrabbleRoundConfiguration> Rounds { get; set; } = new List<ScrabbleRoundConfiguration>();
}

/// <summary>
/// A single word of the pool. Wrapper class so the admin panel can render the pool as an
/// editable list (the panel's generic list editor handles <see cref="ICollection{T}"/> of classes).
/// </summary>
public class ScrabbleWordConfiguration
{
    /// <summary>
    /// Gets or sets the word (the unscrambled answer).
    /// </summary>
    [Display(Name = "Word", Description = "The correct answer; one of these is picked per round.")]
    [Required]
    public string Word { get; set; } = string.Empty;
}

/// <summary>
/// One round: a word pool and a reward item with full attributes.
/// </summary>
public class ScrabbleRoundConfiguration
{
    /// <summary>
    /// Gets or sets the pool of words; one is picked at random per round.
    /// </summary>
    [Display(Name = "Words", Description = "Word pool; one word is picked randomly per round.")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [MinLength(1, ErrorMessage = "Add at least one word.")]
    public ICollection<ScrabbleWordConfiguration> Words { get; set; } = new List<ScrabbleWordConfiguration>();

    /// <summary>
    /// Gets or sets the reward item and its attributes. This is a full item configuration:
    /// pick the item definition, then refine level, durability, luck, option, excellent
    /// options, skill, ancient set and ancient bonus level.
    /// </summary>
    [Display(Name = "Reward", Description = "The item granted to the winner of this round.")]
    [MemberOfAggregate]
    public ScrabbleRewardConfiguration? Reward { get; set; }
}

/// <summary>
/// Full reward-item configuration: a zen amount and/or an item picker with every attribute the
/// /item GM command supports. If <see cref="ItemDefinition"/> is set, IT is granted instead of zen.
/// </summary>
public class ScrabbleRewardConfiguration
{
    /// <summary>
    /// Gets or sets the zen reward. Used when no <see cref="ItemDefinition"/> is configured.
    /// </summary>
    [Display(Name = "Zen Reward", Description = "Zen granted if no item is configured (e.g. 1000000).")]
    [Range(0, int.MaxValue, ErrorMessage = "Zen reward can't be negative or exceed int.MaxValue.")]
    public int RewardZen { get; set; }

    /// <summary>
    /// Gets or sets the item definition (picked in the admin panel). When set, the item is granted
    /// instead of zen.
    /// </summary>
    [Display(Name = "Item", Description = "The item to grant (pick it in the panel). Overrides the zen reward.")]
    public ItemDefinition? ItemDefinition { get; set; }

    /// <summary>
    /// Gets or sets the item level.
    /// </summary>
    [Display(Name = "Level", Description = "Item level (0 = base).")]
    public byte Level { get; set; }

    /// <summary>
    /// Gets or sets the durability override. Leave at 0 to use the item's default durability
    /// (stackable items get 1).
    /// </summary>
    [Display(Name = "Durability", Description = "Durability override; 0 = item default (stackables 1).")]
    public byte Durability { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item has luck (if applicable).
    /// </summary>
    [Display(Name = "Luck", Description = "Apply luck option, if the item supports it.")]
    public bool Luck { get; set; }

    /// <summary>
    /// Gets or sets the option level of the item (if applicable).
    /// </summary>
    [Display(Name = "Option", Description = "Option level, e.g. 4 for +4 item option.")]
    public byte Option { get; set; }

    /// <summary>
    /// Gets or sets the excellent-option bitmask of the item (if applicable).
    /// Bit N (1-based) = excellent option N, like the /item command's ex argument.
    /// </summary>
    [Display(Name = "Excellent Options", Description = "Bitmask of excellent options (1,2,4,...).")]
    public byte ExcellentNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item has skill (if applicable).
    /// </summary>
    [Display(Name = "Skill", Description = "Apply skill option, if the item supports it.")]
    public bool Skill { get; set; }

    /// <summary>
    /// Gets or sets the ancient set discriminator (0 = none, 1/2 = ancient type).
    /// </summary>
    [Display(Name = "Ancient", Description = "Ancient set discriminator: 0 none, 1 or 2.")]
    [Range(0, 2, ErrorMessage = "Ancient must be 0, 1 or 2.")]
    public byte Ancient { get; set; }

    /// <summary>
    /// Gets or sets the ancient bonus level (1 or 2; only when <see cref="Ancient"/> &gt; 0).
    /// </summary>
    [Display(Name = "Ancient Bonus Level", Description = "Ancient bonus option level (1 or 2).")]
    [Range(1, 2, ErrorMessage = "Ancient bonus level must be 1 or 2.")]
    public byte AncientBonusLevel { get; set; } = 1;
}
