// <copyright file="ScrambleHelperTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Scrabble;

using MUnique.OpenMU.GameLogic.PlugIns.Scrabble;

/// <summary>
/// Tests for <see cref="ScrambleHelper"/>.
/// </summary>
[TestFixture]
public class ScrambleHelperTest
{
    [Test]
    public void PickWord_ReturnsOneOfTheConfiguredWords()
    {
        var random = new Random(42);
        var words = new[] { "hello", "world", "scrabble" };
        for (var i = 0; i < 50; i++)
        {
            var picked = ScrambleHelper.PickWord(words, random);
            Assert.That(words, Contains.Item(picked));
        }
    }

    [Test]
    public void PickWord_EmptyList_ReturnsNull()
    {
        Assert.That(ScrambleHelper.PickWord(Array.Empty<string>(), new Random(1)), Is.Null);
    }

    [Test]
    public void Scramble_UsesSameLettersInDifferentOrder()
    {
        var random = new Random(7);
        const string word = "hello";
        for (var i = 0; i < 50; i++)
        {
            var scrambled = ScrambleHelper.Scramble(word, random);
            Assert.That(scrambled, Is.Not.EqualTo(word));
            Assert.That(scrambled.OrderBy(c => c), Is.EqualTo(word.OrderBy(c => c)));
        }
    }

    [Test]
    public void IsCorrectGuess_IgnoresCaseAndWhitespace()
    {
        Assert.That(ScrambleHelper.IsCorrectGuess(" HELLO ", "hello"), Is.True);
        Assert.That(ScrambleHelper.IsCorrectGuess("world", "hello"), Is.False);
    }
}
