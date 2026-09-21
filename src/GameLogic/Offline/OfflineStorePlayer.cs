// <copyright file="OfflineStorePlayer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>
/// An offline player which stands still at its position in a safe zone and keeps its
/// personal store open, so other players can continue to buy from it while the account
/// owner's client is closed. It deliberately does not move, fight, or pick up items.
/// </summary>
public class OfflineStorePlayer : OfflinePlayer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineStorePlayer"/> class.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    public OfflineStorePlayer(IGameContext gameContext)
        : base(gameContext)
    {
    }

    /// <inheritdoc />
    protected override void StartIntelligence()
    {
        // A store keeper stays put: no hunting, no movement, no item pickup.
    }
}
