// <copyright file="ScrabbleChatCommandArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;

/// <summary>Arguments of the /scrabble chat command: the guessed word.</summary>
public class ScrabbleChatCommandArgs : ArgumentsBase
{
    /// <summary>Gets or sets the guessed word.</summary>
    [Argument("word", true)]
    public string? Word { get; set; }
}
