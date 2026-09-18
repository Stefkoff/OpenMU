// <copyright file="ScrabbleChatCommandPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Scrabble;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// Tests for <see cref="ScrabbleChatCommandPlugIn"/>.
/// </summary>
[TestFixture]
public class ScrabbleChatCommandPlugInTest
{
    private IGameContext _gameContext = null!;

    [SetUp]
    public void SetUp()
    {
        this._gameContext = GameContextTestHelper.CreateGameContext();
    }

    [Test]
    public async ValueTask HandleCommandAsync_NoScrabblePlugin_DoesNotThrowAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        var plugin = new ScrabbleChatCommandPlugIn();

        // No ScrabbleGamePlugIn registered in this test context - must not throw.
        await plugin.HandleCommandAsync(player, "/scrabble hello").ConfigureAwait(false);
        await plugin.HandleCommandAsync(player, "/scrabble").ConfigureAwait(false);

        Assert.That(player, Is.Not.Null); // smoke: nothing crashed
    }
}
