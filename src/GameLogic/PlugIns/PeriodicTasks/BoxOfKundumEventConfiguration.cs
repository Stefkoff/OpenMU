// <copyright file="BoxOfKundumEventConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Configuration of the box of kundum drop event.
/// </summary>
public class BoxOfKundumEventConfiguration
{
    /// <summary>
    /// Gets or sets the golden-center message which is shown to all players when the event fires.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventConfiguration_StartMessage_Name))]
    public LocalizedString StartMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of minutes before the event at which the announcement is shown to all players.
    /// A value of 0 disables the pre-event announcement.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventConfiguration_AnnouncementMinutesBefore_Name))]
    public byte AnnouncementMinutesBefore { get; set; } = 2;

    /// <summary>
    /// Gets or sets the golden-center message which is shown to all players before the event fires.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventConfiguration_AnnouncementMessage_Name))]
    public LocalizedString AnnouncementMessage { get; set; }

    /// <summary>
    /// Gets or sets the drop entries of the event. Each entry defines one daily drop at its time.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventConfiguration_Entries_Name))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<BoxOfKundumEventEntry> Entries { get; set; } = [];

    /// <summary>
    /// An entry of the box of kundum drop event.
    /// </summary>
    public class BoxOfKundumEventEntry
    {
        /// <summary>
        /// Gets or sets the time of day (server timezone) at which the drop happens.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Time_Name))]
        public TimeOnly Time { get; set; }

        /// <summary>
        /// Gets or sets the item which is dropped. Typically the Box of Luck definition.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Item_Name))]
        public ItemDefinition? Item { get; set; }

        /// <summary>
        /// Gets or sets the level of the dropped box (drives the Kundum content on drop).
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Level_Name))]
        public byte Level { get; set; } = 7;

        /// <summary>
        /// Gets or sets the number of boxes which are dropped.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Amount_Name))]
        public int Amount { get; set; } = 100;

        /// <summary>
        /// Gets or sets the map on which the boxes are dropped.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Map_Name))]
        public GameMapDefinition? Map { get; set; }

        /// <summary>
        /// Gets or sets the x coordinate of the drop point.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_X_Name))]
        public byte X { get; set; }

        /// <summary>
        /// Gets or sets the y coordinate of the drop point.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.BoxOfKundumEventEntry_Y_Name))]
        public byte Y { get; set; }
    }
}
