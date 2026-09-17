// <copyright file="AutoResetChatCommandPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// Tests for the <see cref="AutoResetChatCommandPlugIn"/>.
/// </summary>
[TestFixture]
public class AutoResetChatCommandPlugInTest
{
    /// <summary>
    /// Verifies that <c>/autoreset off</c> disables the automatic character reset.
    /// </summary>
    [Test]
    public async ValueTask OffDisablesTheAutomaticResetAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var characterId = Guid.NewGuid();
        player.SelectedCharacter!.Id = characterId;

        // Sanity check of the test setup: the test player is at the maximum level (both are 0),
        // so with the reset distribution active the automatic reset would trigger.
        AutoDistributionManager.SetResetPercentages(characterId, new byte[] { 100, 0, 0, 0, 0 });
        Assert.That(AutoDistributionManager.PerformAutoReset(player), Is.Not.Null);

        var plugin = new AutoResetChatCommandPlugIn();
        await plugin.HandleCommandAsync(player, "/autoreset off").ConfigureAwait(false);

        // Even back at the maximum level, no automatic reset happens anymore.
        player.Attributes![Stats.Level] = 0;
        Assert.That(AutoDistributionManager.PerformAutoReset(player), Is.Null);
    }
}
