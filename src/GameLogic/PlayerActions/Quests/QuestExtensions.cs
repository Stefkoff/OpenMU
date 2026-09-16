// <copyright file="QuestExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Extensions regarding quests.
/// </summary>
public static class QuestExtensions
{
    /// <summary>
    /// Gets the monster-kill requirement state which belongs to the specified kill requirement.
    /// The requirement objects are matched by their ids, because they may be loaded as different
    /// instances (e.g. one in the quest definition and another one in the requirement state), and
    /// <see cref="QuestMonsterKillRequirement"/> doesn't override equality by id.
    /// </summary>
    /// <param name="questState">The quest state.</param>
    /// <param name="requirement">The kill requirement of the active quest.</param>
    /// <returns>The matching requirement state, if it exists.</returns>
    public static QuestMonsterKillRequirementState? GetRequirementState(this CharacterQuestState questState, QuestMonsterKillRequirement requirement)
    {
        var requirementId = requirement.GetId();
        if (requirementId is null)
        {
            return null;
        }

        return questState.GetRequirementState(requirementId.Value);
    }

    /// <summary>
    /// Gets the monster-kill requirement state with the specified requirement id.
    /// </summary>
    /// <param name="questState">The quest state.</param>
    /// <param name="requirementId">The id of the kill requirement.</param>
    /// <returns>The matching requirement state, if it exists.</returns>
    public static QuestMonsterKillRequirementState? GetRequirementState(this CharacterQuestState questState, Guid requirementId)
    {
        return questState.RequirementStates.FirstOrDefault(s => s.Requirement.GetId() == requirementId);
    }

    private static Guid? GetId(this QuestMonsterKillRequirement? requirement)
    {
        return (requirement as MUnique.OpenMU.Persistence.IIdentifiable)?.Id;
    }

    /// <summary>
    /// Clears the quest state for the specified player.
    /// </summary>
    /// <param name="questState">State of the quest.</param>
    /// <param name="persistenceContext">The persistence context of the player.</param>
    public static async ValueTask ClearAsync(this CharacterQuestState questState, IContext persistenceContext)
    {
        questState.ActiveQuest = null;
        questState.ClientActionPerformed = false;
        foreach (var requirementState in questState.RequirementStates)
        {
            await persistenceContext.DeleteAsync(requirementState).ConfigureAwait(false);
        }

        questState.RequirementStates.Clear();
    }
}
