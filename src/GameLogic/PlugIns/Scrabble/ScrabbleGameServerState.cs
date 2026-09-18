// <copyright file="ScrabbleGameServerState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>Per-game-context state of the Scrabble game.</summary>
public class ScrabbleGameServerState : PeriodicTaskGameServerState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScrabbleGameServerState"/> class.
    /// </summary>
    /// <param name="context">The game context.</param>
    public ScrabbleGameServerState(IGameContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Description => "Scrabble";

    /// <summary>Gets or sets the 0-based index of the active round, or -1 if none is running.</summary>
    public int CurrentRoundIndex { get; set; } = -1;

    /// <summary>Gets or sets the word currently being guessed (the unscrambled answer).</summary>
    public string? CurrentWord { get; set; }

    /// <summary>Gets or sets the scrambled word announced to the players.</summary>
    public string? ScrambledWord { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the current round started (1-minute deadline).</summary>
    public DateTime RoundStartUtc { get; set; }

    /// <summary>Gets or sets a value indicating whether the current round has been won already.</summary>
    public bool RoundWon { get; set; }

    /// <summary>Gets or sets a value indicating whether the mid-round reminder (word reprint at 30s) was already sent.</summary>
    public bool MidRoundReminderSent { get; set; }

    /// <summary>Gets or sets the name of the player who won the current round.</summary>
    public string? WinnerName { get; set; }
}
