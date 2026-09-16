// <copyright file="ItemGrantService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Grants and removes inventory items for characters through the data persistence.
/// NOTE: only reliable for OFFLINE characters — an online player's in-memory state
/// overwrites database edits on the server's next save. Keep that discipline.
/// This service is the reusable grant path for future payment/SMS web APIs.
/// </summary>
public class ItemGrantService
{
    private const byte InventorySlotCount = 64;

    private readonly IDataSource<Account> _accountData;
    private readonly ILogger<ItemGrantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemGrantService"/> class.
    /// </summary>
    public ItemGrantService(IDataSource<Account> accountData, ILogger<ItemGrantService> logger)
    {
        this._accountData = accountData;
        this._logger = logger;
    }

    /// <summary>
    /// Finds an account by its login name (minimal data).
    /// </summary>
    public async ValueTask<Account?> FindAccountByLoginAsync(string loginName, CancellationToken cancellationToken = default)
    {
        var context = (IPlayerContext)await this._accountData.GetContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the full account aggregate (including characters and inventories).
    /// </summary>
    public async ValueTask<Account?> LoadAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            return (Account?)await this._accountData.GetOwnerAsync(accountId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to load account {AccountId}", accountId);
            return null;
        }
    }

    /// <summary>
    /// Adds a new item to the character's inventory on the first free slot and saves.
    /// Returns the created item, or null if the save failed or no slot was free.
    /// </summary>
    public async ValueTask<Item?> AddItemToCharacterAsync(Character character, ItemDefinition definition, byte level, byte? durability, CancellationToken cancellationToken = default)
    {
        var context = await this._accountData.GetContextAsync(cancellationToken).ConfigureAwait(false);
        if (character.Inventory is not { } inventory)
        {
            this._logger.LogWarning("Character {Character} has no inventory", character.Name);
            return null;
        }

        var itemSlot = this.FindFirstFreeSlot(inventory);
        if (itemSlot is null)
        {
            this._logger.LogWarning("Character {Character} inventory is full", character.Name);
            return null;
        }

        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Level = level;
        item.Durability = durability ?? definition.Durability;
        item.ItemSlot = itemSlot.Value;
        inventory.Items.Add(item);

        if (!await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false))
        {
            this._logger.LogError("Failed to save the granted item for character {Character}", character.Name);
            return null;
        }

        this._logger.LogInformation("Granted {Item} (level {Level}) to character {Character} in slot {Slot}", definition.Name, item.Level, character.Name, item.ItemSlot);
        return item;
    }

    /// <summary>
    /// Removes an item from the character's inventory and saves.
    /// </summary>
    public async ValueTask<bool> RemoveItemAsync(Character character, Item item, CancellationToken cancellationToken = default)
    {
        var context = await this._accountData.GetContextAsync(cancellationToken).ConfigureAwait(false);
        character.Inventory?.Items.Remove(item);
        context.Delete(item);
        var success = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (success)
        {
            this._logger.LogInformation("Removed item from character {Character}", character.Name);
        }

        return success;
    }

    private static byte? FindFirstFreeSlot(ItemStorage inventory)
    {
        var used = inventory.Items.Select(i => i.ItemSlot).ToHashSet();
        for (byte slot = 0; slot < InventorySlotCount; slot++)
        {
            if (!used.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }
}
