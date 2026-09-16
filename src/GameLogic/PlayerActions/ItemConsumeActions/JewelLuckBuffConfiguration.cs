// <copyright file="JewelLuckBuffConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.ComponentModel;

/// <summary>
/// Configuration of the "Gem of Success" buff consume handler.
/// </summary>
public class JewelLuckBuffConfiguration
{
    /// <summary>
    /// The default duration in seconds (1 hour).
    /// </summary>
    public const float DefaultDurationSeconds = 3600;

    /// <summary>
    /// Gets or sets the buff duration in seconds. The stored value is an exact float and
    /// far below 2^24, so no precision is lost.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.JewelLuckBuffDuration_Name), Description = nameof(PlugInResources.JewelLuckBuffDuration_Description))]
    public float DurationSeconds { get; set; } = DefaultDurationSeconds;
}
