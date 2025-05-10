using System.Collections.Generic;
using ItemPickupNotifier.Config;
using ItemPickupNotifier.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
namespace ItemPickupNotifier
{

    public class ItempickupnotifierModSystem : ModSystem
    {
        public static NotifierOverlay NotifierOverlay;
        public static ItemPickupNotifierConfig Config { get; private set; } = new();

        private ICoreClientAPI capi;
        private IClientPlayer player;
        private readonly Dictionary<string, ItemStack[]> cachedInventories = new();
        private long playerAwaitListenerId;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            base.StartClientSide(capi);

            capi.Event.LeftWorld += OnClientLeave;
            playerAwaitListenerId = capi.Event.RegisterGameTickListener(CheckPlayerReady, 200);

            Config = capi.LoadModConfig<ItemPickupNotifierConfig>(ItemPickupNotifierConfig.FileName) ?? new();
            capi.StoreModConfig(Config, ItemPickupNotifierConfig.FileName);

            NotifierOverlay = new(capi);
        }

        private void CheckPlayerReady(float dt)
        {
            if (capi.PlayerReadyFired)
            {
                capi.Logger.Debug("Player is ready - Caching Inventories");
                player = capi.World.Player;
                foreach (var (invKey, inv) in player.InventoryManager.Inventories)
                {
                    if (!IsValidInventoryType(inv)) continue;

                    inv.SlotModified += slotId => SlotModified(invKey, slotId);
                    cachedInventories[invKey] = CopyInventorySlots((InventoryBase)inv);
                }
                capi.Logger.Debug("Unregistering Player Await Listener");
                capi.Event.UnregisterGameTickListener(playerAwaitListenerId);
            }
        }

        private void OnClientLeave()
        {
            cachedInventories.RemoveAll((_, _) => true);
            this.player = null;
        }

        private void SlotModified(string invKey, int slotId)
        {
            var inv = (InventoryBase)player.InventoryManager.Inventories[invKey];
            var cachedInvStacks = cachedInventories[invKey];
            if (cachedInvStacks == null || cachedInvStacks.Length != inv.Count)
            {
                // Refresh cached inventories -> mainly happens dues to changes in equipped bags
                cachedInventories[invKey] = CopyInventorySlots(inv);
                return;
            }

            var currentItemStack = cachedInvStacks[slotId];
            var newItemStack = inv[slotId].Itemstack;

            var currentStackSize = currentItemStack?.StackSize ?? 0;
            var newStackSize = newItemStack?.StackSize ?? 0;

            var isMoveOperation = currentItemStack != null && newItemStack != null && currentItemStack?.Id != newItemStack?.Id;
            if (currentStackSize < newStackSize && !isMoveOperation)
            {
                NotifyItemPickup(newItemStack, currentStackSize);
            }

            cachedInventories[invKey][slotId] = newItemStack?.Clone();
        }

        private static void NotifyItemPickup(ItemStack newStack, int currentSize)
        {
            var notifyStack = newStack.Clone();
            notifyStack.StackSize -= currentSize;
            NotifierOverlay.AddItemStack(notifyStack);
            NotifierOverlay.ShowNotification();
        }

        private static ItemStack[] CopyInventorySlots(InventoryBase inv)
        {
            var copiedStacks = new ItemStack[inv.Count];
            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i].Itemstack != null)
                {
                    copiedStacks[i] = inv[i].Itemstack.Clone();
                }
            }
            return copiedStacks;
        }

        private static bool IsValidInventoryType(IInventory inv)
        {
            var typeName = inv.GetType().Name;
            return typeName is "InventoryPlayerHotbar" or "InventoryPlayerBackPacks";
        }
    }
}