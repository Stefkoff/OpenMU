// <copyright file="EnterMiniGameActionTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;

/// <summary>
/// Tests for <see cref="EnterMiniGameAction"/> ticket level acceptance rules.
/// </summary>
[TestFixture]
public class EnterMiniGameActionTest
{
    /// <summary>
    /// A required ticket level of 0 means "any level qualifies" (universal ticket),
    /// otherwise the ticket level has to match the required level exactly.
    /// </summary>
    /// <param name="requiredLevel">The configured <see cref="EnterMiniGameAction.IsAcceptedTicketLevel" /> required level.</param>
    /// <param name="itemLevel">The level of the ticket item.</param>
    /// <param name="expected">Whether the ticket level is accepted.</param>
    [TestCase(0, 0, true)]
    [TestCase(0, 7, true)]
    [TestCase(0, 30, true)]
    [TestCase(3, 3, true)]
    [TestCase(3, 0, false)]
    [TestCase(3, 7, false)]
    [TestCase(8, 8, true)]
    [TestCase(8, 1, false)]
    public void IsAcceptedTicketLevelTest(byte requiredLevel, byte itemLevel, bool expected)
    {
        Assert.That(EnterMiniGameAction.IsAcceptedTicketLevel(requiredLevel, itemLevel), Is.EqualTo(expected));
    }
}
