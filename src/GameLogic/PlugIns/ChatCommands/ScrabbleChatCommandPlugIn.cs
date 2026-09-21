// <copyright file="ScrabbleChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.GameLogic.PlugIns.Scrabble;
using MUnique.OpenMU.GameLogic.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The <c>/scrabble &lt;word&gt;</c> chat command: submits a guess to the active Scrabble round.
/// </summary>
[Guid("3B7C8D9E-0F1A-4B2C-9D3E-4F5A6B7C8D92")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.ScrabbleChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.ScrabbleChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(ScrabbleChatCommandArgs), CharacterStatus.Normal)]
public sealed class ScrabbleChatCommandPlugIn : ChatCommandPlugInBase<ScrabbleChatCommandArgs>
{
    private const string Command = "/scrabble";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, ScrabbleChatCommandArgs arguments)
    {
        var guess = arguments.Word;
        if (string.IsNullOrWhiteSpace(guess))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ScrabbleNoWord)).ConfigureAwait(false);
            return;
        }

        var gameContext = player.GameContext;
        var scrabblePlugIn = gameContext.PlugInManager
            .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
            .OfType<ScrabbleGamePlugIn>()
            .FirstOrDefault();
        if (scrabblePlugIn is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ScrabbleNoActiveGame)).ConfigureAwait(false);
            return;
        }

        var result = await scrabblePlugIn.TryGuessAsync(gameContext, player, guess).ConfigureAwait(false);
        switch (result)
        {
            case GuessResult.NoActiveRound:
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ScrabbleNoActiveGame)).ConfigureAwait(false);
                break;
            case GuessResult.Wrong:
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ScrabbleWrongGuess)).ConfigureAwait(false);
                break;
            case GuessResult.Win:
                break; // winner announcement is broadcast by the game plugin
            default:
                break;
        }
    }
}
