// <copyright file="AutoStatChatCommandArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;

/// <summary>
/// Arguments of the <c>/autostat</c> chat command: the minimum amount of level-up points
/// which trigger a distribution, and the percentages of the points which are distributed
/// to strength, agility, vitality, energy and command, e.g. <c>/autostat 1000 40 30 20 10 0</c>.
/// The command percentage is optional - most character classes don't have a command attribute.
/// </summary>
public class AutoStatChatCommandArgs : ArgumentsBase
{
    /// <summary>
    /// Gets or sets the minimum amount of level-up points which trigger a distribution.
    /// </summary>
    [Argument("points", true)]
    public int PointsThreshold { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the points which go to strength.
    /// </summary>
    [Argument("str", true)]
    public byte StrengthPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the points which go to agility.
    /// </summary>
    [Argument("agi", true)]
    public byte AgilityPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the points which go to vitality.
    /// </summary>
    [Argument("vit", true)]
    public byte VitalityPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the points which go to energy.
    /// </summary>
    [Argument("ene", true)]
    public byte EnergyPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the points which go to command
    /// (optional - most character classes don't have a command attribute).
    /// </summary>
    [Argument("cmd", false)]
    public byte CommandPercentage { get; set; }

    /// <summary>
    /// Gets the percentages in the order of the assigned stat attributes.
    /// </summary>
    public IReadOnlyList<byte> Percentages =>
        [this.StrengthPercentage, this.AgilityPercentage, this.VitalityPercentage, this.EnergyPercentage, this.CommandPercentage];
}
