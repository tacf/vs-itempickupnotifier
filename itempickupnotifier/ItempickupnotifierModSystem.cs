using System;
using System.Collections.Generic;
using System.Linq;
using ItemPickupNotifier.Config;
using ItemPickupNotifier.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
namespace ItemPickupNotifier
{

    public class ItempickupnotifierModSystem : ModSystem
    {
        private static NotifierOverlay NotifierOverlay;
        private static SettingsUI GuiSettings;
        public static ItemPickupNotifierConfig Config { get; private set; } = new();

        private ICoreClientAPI capi;
        private IClientPlayer player;
        private static readonly Dictionary<string, ItemStack[]> cachedInventories = new();
        private ItemStack _lastItemStackRemoved;
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
            GuiSettings = CreateSettingsUI();
            GuiSettings.Build();
            RegisterHotKeys();
        }

        private SettingsUI CreateSettingsUI()
        {
            SettingsUI ui = new("itempickupnotifier", capi);

            ui.Section("font").AddCheckbox("bold", OnBoldToggled);
            ui.Section("position")
                .AddSlider("xoffset", OnSliderNewValue)
                .AddSlider("yoffset", OnSliderNewValue)
                .AddDropdown("pos", OnSelectionChanged, Enum.GetNames(typeof(EnumDialogArea)), defaultName: EnumDialogArea.LeftBottom.ToString());
            ui.Section("dev")
                .AddCheckbox("overlay-background", OnDevBackgroundToggled)
                .AddCheckbox("preview-mode", OnDevPreviewToggled);

            return ui;
        }

        private void OnDevPreviewToggled(bool obj)
        {
            return;
        }

        private void OnDevBackgroundToggled(bool obj)
        {
            return;
        }

        private void OnSelectionChanged(string code, bool selected)
        {
            return;
        }

        private bool OnSliderNewValue(int t1)
        {
            return true;
        }

        private void OnBoldToggled(bool obj)
        {
            return;
        }


        private void RegisterHotKeys()
        {
            capi.Input.RegisterHotKey("itempickupnotifier:config", "Item Pickup Notifier Config", GlKeys.Z, type: HotkeyType.GUIOrOtherControls, ctrlPressed: true);
            capi.Input.SetHotKeyHandler("itempickupnotifier:config", OnConfigChanged);
        }

        private bool OnConfigChanged(KeyCombination keyCombination)
        {
            if (GuiSettings.IsOpened()) GuiSettings.TryClose();
            else GuiSettings.TryOpen();
        
            return true;
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
            _lastItemStackRemoved = null;
            player = null;
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

            var slotFilled = (newItemStack != null) && (currentItemStack == null);
            var slotEmptied = !slotFilled;
            var slotChange = newItemStack != null && currentItemStack != null;
            var slotNoOp = !slotChange && newStackSize == 0 && currentStackSize == 0;
            var slotChangedAmmount = slotChange && newItemStack.Id == currentItemStack.Id && newStackSize != currentStackSize;
            var slotChangedItemType = slotChange && (currentItemStack?.Id != newItemStack?.Id);
            var isStackSwap = slotChange && slotChangedItemType;
            var isLastRemovedItem = (slotFilled || slotChangedAmmount || slotChangedItemType) && _lastItemStackRemoved != null && _lastItemStackRemoved.Id == newItemStack.Id && _lastItemStackRemoved.StackSize == newStackSize;

            cachedInventories[invKey][slotId] = newItemStack?.Clone();
            if (currentStackSize < newStackSize && !isStackSwap && !isLastRemovedItem)
            {
                NotifyItemPickup(newItemStack, currentStackSize);
                _lastItemStackRemoved = null;
            }

            if (!isLastRemovedItem || slotEmptied || !slotNoOp) _lastItemStackRemoved = currentItemStack?.Clone();      
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

        public static int GetTotalItemCountInInventories(int itemCode)
        {
            return cachedInventories.Values
                .SelectMany(inv => inv.Where(stack => stack?.Id == itemCode))
                .Sum(stack => stack.StackSize);
        }
    }
}