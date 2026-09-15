// <copyright file="AutoResetChatCommandArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;

/// <summary>
/// Arguments of the <c>/autoreset</c> chat command: the percentages of the reset points
/// which are distributed to strength, agility, vitality, energy and command after each
/// automatic reset, e.g. <c>/autoreset 40 30 20 10 0</c>.
/// </summary>
public class AutoResetChatCommandArgs : ArgumentsBase
{
    /// <summary>
    /// Gets or sets the percentage of the reset points which go to strength.
    /// </summary>
    [Argument("str", true)]
    public byte StrengthPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the reset points which go to agility.
    /// </summary>
    [Argument("agi", true)]
    public byte AgilityPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the reset points which go to vitality.
    /// </summary>
    [Argument("vit", true)]
    public byte VitalityPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the reset points which go to energy.
    /// </summary>
    [Argument("ene", true)]
    public byte EnergyPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the reset points which go to command.
    /// </summary>
    [Argument("cmd", true)]
    public byte CommandPercentage { get; set; }

    /// <summary>
    /// Gets the percentages in the order of the assigned stat attributes.
    /// </summary>
    public IReadOnlyList<byte> Percentages =>
        [this.StrengthPercentage, this.AgilityPercentage, this.VitalityPercentage, this.EnergyPercentage, this.CommandPercentage];
}
