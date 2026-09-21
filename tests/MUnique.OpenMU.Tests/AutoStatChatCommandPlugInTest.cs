// <copyright file="AutoStatChatCommandPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// Tests for the <see cref="AutoStatChatCommandPlugIn"/>.
/// </summary>
[TestFixture]
public class AutoStatChatCommandPlugInTest
{
    /// <summary>
    /// Verifies that <c>/autostat off</c> disables the automatic stat distribution.
    /// </summary>
    [Test]
    public async ValueTask OffDisablesTheAutomaticDistributionAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var characterId = Guid.NewGuid();
        player.SelectedCharacter!.Id = characterId;
        player.SelectedCharacter.LevelUpPoints = 100;

        // Sanity check of the test setup: with the distribution active and enough points,
        // points are distributed.
        AutoDistributionManager.SetStatDistribution(characterId, 1, new byte[] { 100, 0, 0, 0, 0 });
        Assert.That(AutoDistributionManager.PerformStatDistribution(player), Is.Not.Null);

        var plugin = new AutoStatChatCommandPlugIn();
        await plugin.HandleCommandAsync(player, "/autostat off").ConfigureAwait(false);

        // Even with enough level-up points, nothing is distributed anymore.
        Assert.That(AutoDistributionManager.PerformStatDistribution(player), Is.Null);
    }
}
