// <copyright file="JewelLuckBuffConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.JewelLuckBuff;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the "Gem of Success" item: activates the 1-hour buff during which
/// every jewel upgrade succeeds with 100% chance. A second use while active is refused.
/// </summary>
[Guid("3B6E4C2A-8F1D-4E5A-9B0C-2D7F5A1E6C93")]
[PlugIn]
[Display(Name = nameof(PlugInResources.JewelLuckBuffConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.JewelLuckBuffConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class JewelLuckBuffConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn, ISupportCustomConfiguration<JewelLuckBuffConfiguration>, ISupportDefaultCustomConfiguration
{
    private const byte ItemGroup = 14;
    private const short ItemNumber = 12;

    /// <inheritdoc />
    public override ItemIdentifier Key => new(ItemNumber, ItemGroup);

    /// <inheritdoc />
    public JewelLuckBuffConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new JewelLuckBuffConfiguration();

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item))
        {
            return false;
        }

        if (JewelLuckBuffService.IsActive(player))
        {
            var remaining = player.SelectedCharacter is { JewelLuckBuffEndsAt: { } endsAt }
                ? (int)Math.Ceiling(JewelLuckBuffService.RemainingSeconds(endsAt, JewelLuckBuffService.Clock()) / 60)
                : 0;
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.JewelLuckBuffAlreadyActive), remaining).ConfigureAwait(false);
            return false; // refuse; the item is not consumed
        }

        var duration = this.Configuration?.DurationSeconds ?? JewelLuckBuffConfiguration.DefaultDurationSeconds;
        JewelLuckBuffService.Activate(player, duration);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.JewelLuckBuffActivated), (int)(duration / 60)).ConfigureAwait(false);
        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);
        return true;
    }
}
