// <copyright file="ScrambleHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

/// <summary>
/// Pure word-picking and scrambling logic for the Scrabble game — no game-state dependencies.
/// Deterministic given the injected <see cref="Random"/>.
/// </summary>
public static class ScrambleHelper
{
    /// <summary>Picks one random word from the list. Empty/null list → null.</summary>
    public static string? PickWord(IReadOnlyList<string> words, Random random)
        => words.Count == 0 ? null : words[random.Next(words.Count)];

    /// <summary>
    /// Fisher-Yates shuffle; re-shuffles (up to 10 tries) if the result equals the original,
    /// so the puzzle is never trivially solved by retyping the scrambled word.
    /// </summary>
    public static string Scramble(string word, Random random)
    {
        var letters = word.ToCharArray();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            for (var i = letters.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }

            if (new string(letters) != word)
            {
                return new string(letters);
            }
        }

        return new string(letters);
    }

    /// <summary>Case-insensitive, trimmed equality for guess validation.</summary>
    public static bool IsCorrectGuess(string guess, string expected)
        => string.Equals(guess?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
}
