// <copyright file="QuestExtensionsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests for the <see cref="QuestExtensions"/> requirement state lookup.
/// </summary>
[TestFixture]
public class QuestExtensionsTest
{
    /// <summary>
    /// A requirement stub which exposes an id, like the persistence-loaded requirement instances do.
    /// </summary>
    private class TestRequirement : QuestMonsterKillRequirement, IIdentifiable
    {
        public Guid Id { get; set; }
    }

    private class TestQuestState : CharacterQuestState
    {
        public TestQuestState()
        {
            this.RequirementStates = new List<QuestMonsterKillRequirementState>();
        }
    }

    /// <summary>
    /// Tests that a requirement state is found even if the requirement instance of the state is
    /// a different object than the one of the quest definition - as long as they share the same id.
    /// This is the case after a server restart, because the loaded state and the quest definition
    /// can reference different object instances.
    /// </summary>
    [Test]
    public void GetRequirementStateFindsStateWithDifferentInstanceOfSameRequirement()
    {
        var requirementId = Guid.NewGuid();
        var questState = new TestQuestState();
        var state = new QuestMonsterKillRequirementState { Requirement = new TestRequirement { Id = requirementId } };
        questState.RequirementStates.Add(state);

        var found = questState.GetRequirementState(new TestRequirement { Id = requirementId });

        Assert.That(found, Is.SameAs(state));
    }

    /// <summary>
    /// Tests that no requirement state is returned for an unknown requirement.
    /// </summary>
    [Test]
    public void GetRequirementStateReturnsNullForUnknownRequirement()
    {
        var questState = new TestQuestState();
        questState.RequirementStates.Add(new QuestMonsterKillRequirementState { Requirement = new TestRequirement { Id = Guid.NewGuid() } });

        var found = questState.GetRequirementState(new TestRequirement { Id = Guid.NewGuid() });

        Assert.That(found, Is.Null);
    }
}
