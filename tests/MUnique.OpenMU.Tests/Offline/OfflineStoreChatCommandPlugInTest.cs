// <copyright file="OfflineStoreChatCommandPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Offline;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// Tests for <see cref="OfflineStoreChatCommandPlugIn"/>.
/// </summary>
[TestFixture]
public class OfflineStoreChatCommandPlugInTest
{
    private IGameContext _gameContext = null!;

    [SetUp]
    public void SetUp()
    {
        this._gameContext = GameContextTestHelper.CreateGameContext();
    }

    [Test]
    public async ValueTask HandleCommandAsync_StoreNotOpen_DoesNotStartSessionAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        player.Account!.LoginName = "storetest";
        var plugin = new OfflineStoreChatCommandPlugIn();
        var manager = player.GameContext.OfflinePlayerManager;

        await plugin.HandleCommandAsync(player, "/offstore").ConfigureAwait(false);

        Assert.That(manager.IsActive("storetest"), Is.False);
    }
}
