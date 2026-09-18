// <copyright file="OfflineStorePlayerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Offline;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests for the offline store ghost (<see cref="OfflinePlayerManager.StartStoreAsync"/>).
/// </summary>
[TestFixture]
public class OfflineStorePlayerTests
{
    private const string TestUserLoginName = "storetest";
    private const string TestCharacterName = "storeChar";

    private IGameContext _gameContext = null!;
    private IPersistenceContextProvider _contextProvider = null!;

    [SetUp]
    public void SetUp()
    {
        this._gameContext = GameContextTestHelper.CreateGameContext();
        this._contextProvider = this._gameContext.PersistenceContextProvider;
    }

    [Test]
    public async ValueTask StartStoreAsync_WithOpenStore_CreatesGhostWithOpenStoreAsync()
    {
        var realPlayer = await this.CreatePlayerWithPersistedAccountAsync(openStore: true).ConfigureAwait(false);
        var manager = realPlayer.GameContext.OfflinePlayerManager;

        var result = await manager.StartStoreAsync(realPlayer, TestUserLoginName).ConfigureAwait(false);

        Assert.That(result, Is.True);
        Assert.That(manager.TryGetPlayer(TestUserLoginName, out var ghost), Is.True);
        Assert.That(ghost, Is.TypeOf<OfflineStorePlayer>());
        Assert.That(ghost!.SelectedCharacter!.IsStoreOpened, Is.True);
        Assert.That(ghost.SelectedCharacter.StoreName, Is.EqualTo("Test Store"));
        Assert.That(ghost.ShopStorage?.StoreOpen, Is.True);
        Assert.That(ghost.ShopStorage?.Items.ToList(), Has.Count.EqualTo(1));
    }

    [Test]
    public async ValueTask StartStoreAsync_WithClosedStore_GhostStoreStaysClosedAsync()
    {
        var realPlayer = await this.CreatePlayerWithPersistedAccountAsync(openStore: false).ConfigureAwait(false);
        var manager = realPlayer.GameContext.OfflinePlayerManager;

        await manager.StartStoreAsync(realPlayer, TestUserLoginName).ConfigureAwait(false);

        Assert.That(manager.TryGetPlayer(TestUserLoginName, out var ghost), Is.True);
        Assert.That(ghost!.SelectedCharacter!.IsStoreOpened, Is.False);
        Assert.That(ghost.ShopStorage?.StoreOpen, Is.False);
    }

    [Test]
    public async ValueTask BuyFromOfflineStore_MovesItemAndZenAsync()
    {
        var realPlayer = await this.CreatePlayerWithPersistedAccountAsync(openStore: true).ConfigureAwait(false);
        var manager = realPlayer.GameContext.OfflinePlayerManager;
        await manager.StartStoreAsync(realPlayer, TestUserLoginName).ConfigureAwait(false);
        Assert.That(manager.TryGetPlayer(TestUserLoginName, out var ghost), Is.True);

        var buyer = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        buyer.TryAddMoney(1_000_000);

        var buyAction = new BuyRequestAction();
        await buyAction.BuyItemAsync(buyer, ghost!, InventoryConstants.FirstStoreItemSlotIndex).ConfigureAwait(false);

        // Item moved to the buyer.
        Assert.That(buyer.SelectedCharacter!.Inventory!.Items, Has.Count.EqualTo(1));
        // The store is emptied and closed...
        Assert.That(ghost!.ShopStorage?.Items, Is.Empty);
        Assert.That(ghost.ShopStorage?.StoreOpen, Is.False); // auto-closed when empty
        // ...and the ghost session ended, which flushed the zen to the persisted account.
        using (var ctx = this._contextProvider.CreateNewPlayerContext(this._gameContext.Configuration))
        {
            var persistedAccount = await ctx.GetAccountByLoginNameAsync(TestUserLoginName).ConfigureAwait(false);
            Assert.That(persistedAccount?.Characters.Single().Inventory?.Money, Is.EqualTo(5_000));
        }
    }

    [Test]
    public async ValueTask BuyLastItemFromOfflineStore_StopsTheGhostSessionAsync()
    {
        var realPlayer = await this.CreatePlayerWithPersistedAccountAsync(openStore: true).ConfigureAwait(false);
        var manager = realPlayer.GameContext.OfflinePlayerManager;
        await manager.StartStoreAsync(realPlayer, TestUserLoginName).ConfigureAwait(false);
        Assert.That(manager.TryGetPlayer(TestUserLoginName, out var ghost), Is.True);

        var buyer = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        buyer.TryAddMoney(1_000_000);

        var buyAction = new BuyRequestAction();
        await buyAction.BuyItemAsync(buyer, ghost!, InventoryConstants.FirstStoreItemSlotIndex).ConfigureAwait(false);

        // The last sale must end the whole offline store session, not just close the shop.
        Assert.That(manager.IsActive(TestUserLoginName), Is.False);
    }

    private async ValueTask<Player> CreatePlayerWithPersistedAccountAsync(bool openStore)
    {
        var config = this._gameContext.Configuration;

        using (var ctx = this._contextProvider.CreateNewPlayerContext(config))
        {
            var account = ctx.CreateNew<Account>();
            account.LoginName = TestUserLoginName;

            var character = ctx.CreateNew<Character>();
            character.Name = TestCharacterName;
            if (config.CharacterClasses.FirstOrDefault() is { } existingClass)
            {
                character.CharacterClass = existingClass;
            }
            else
            {
                var characterClass = ctx.CreateNew<MUnique.OpenMU.DataModel.Configuration.CharacterClass>();
                characterClass.HomeMap = config.Maps.FirstOrDefault();
                character.CharacterClass = characterClass;
            }

            character.Inventory = ctx.CreateNew<ItemStorage>();
            if (openStore)
            {
                var itemDef = ctx.CreateNew<ItemDefinition>();
                itemDef.Name = "Store Test Item";
                itemDef.Group = 14;
                itemDef.Number = 30;
                itemDef.Width = 1;
                itemDef.Height = 1;

                var item = ctx.CreateNew<Item>();
                item.Definition = itemDef;
                item.ItemSlot = InventoryConstants.FirstStoreItemSlotIndex;
                item.StorePrice = 5_000;
                character.Inventory.Items.Add(item);

                character.StoreName = "Test Store";
                character.IsStoreOpened = true;
            }

            account.Characters.Add(character);
            await ctx.SaveChangesAsync().ConfigureAwait(false);
        }

        var player = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        player.Account!.LoginName = TestUserLoginName;
        var mockCharacter = player.SelectedCharacter!;
        mockCharacter.Name = TestCharacterName;
        mockCharacter.CharacterClass ??= player.Account.UnlockedCharacterClasses.FirstOrDefault();
        if (mockCharacter.CharacterClass is null && config.CharacterClasses.FirstOrDefault() is { } cc)
        {
            mockCharacter.CharacterClass = cc;
        }

        return player;
    }
}
