using System;
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
        private static NotifierOverlay _NotifierOverlay;
        private static SettingsUI _GuiSettings;
        public static ItemPickupNotifierConfig Config { get; private set; } = new();

        private static ICoreClientAPI _capi;
        private IClientPlayer _player;
        private readonly Dictionary<string, ItemStack[]> _cachedInventories = new();
        private ItemStack _lastItemStackRemoved;
        private long _playerAwaitListenerId;

        public override void StartClientSide(ICoreClientAPI api)
        {
            _capi = api;
            base.StartClientSide(_capi);

            _capi.Event.LeftWorld += OnClientLeave;
            _playerAwaitListenerId = _capi.Event.RegisterGameTickListener(CheckPlayerReady, 200);

            Config = _capi.LoadModConfig<ItemPickupNotifierConfig>(ItemPickupNotifierConfig.FileName) ?? new();
            SaveSettings();

            _NotifierOverlay = new(_capi);
            _GuiSettings = CreateSettingsUI();
            _GuiSettings.Build();
            RegisterHotKeys();
        }

        public static void SaveSettings()
        {
            _capi.StoreModConfig(Config, ItemPickupNotifierConfig.FileName);
        }

        private SettingsUI CreateSettingsUI()
        {
            SettingsUI ui = new("itempickupnotifier", _capi, OnSettingsSavedClicked, OnSettingsResetClicked);

            ui.Section("font").AddCheckbox("bold", OnBoldToggled);
            ui.Section("position")
                .AddSlider("xoffset", OnSliderNewValue, (int)Config.HorizontalOffset)
                .AddSlider("yoffset", OnSliderNewValue, (int)Config.VerticalOffset)
                .AddDropdown("pos", OnSelectionChanged, Enum.GetNames(typeof(EnumDialogArea)), defaultName: EnumDialogArea.LeftBottom.ToString());
            ui.Section("dev")
                .AddCheckbox("overlay-background", OnDevBackgroundToggled)
                .AddCheckbox("preview-mode", OnDevPreviewToggled);

            return ui;
        }

        private bool OnSettingsSavedClicked()
        {
            SaveSettings();
            return true;
        }

        private bool OnSettingsResetClicked()
        {
            return true;
        }

        private void OnDevPreviewToggled(bool toggle)
        {
            _NotifierOverlay.Debug(toggle);
        }

        private void OnDevBackgroundToggled(bool toggle)
        {
            _NotifierOverlay.BackgroundVisible(toggle);
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
            _capi.Input.RegisterHotKey("itempickupnotifier:config", "Item Pickup Notifier Config", GlKeys.Z, type: HotkeyType.GUIOrOtherControls, ctrlPressed: true);
            _capi.Input.SetHotKeyHandler("itempickupnotifier:config", OnConfigChanged);
        }

        private bool OnConfigChanged(KeyCombination keyCombination)
        {
            if (_GuiSettings.IsOpened()) _GuiSettings.TryClose();
            else _GuiSettings.TryOpen();
        
            return true;
        }

        private void CheckPlayerReady(float dt)
        {
            if (_capi.PlayerReadyFired)
            {
                _capi.Logger.Debug("Player is ready - Caching Inventories");
                _player = _capi.World.Player;
                foreach (var (invKey, inv) in _player.InventoryManager.Inventories)
                {
                    if (!IsValidInventoryType(inv)) continue;

                    inv.SlotModified += slotId => SlotModified(invKey, slotId);
                    _cachedInventories[invKey] = CopyInventorySlots((InventoryBase)inv);
                }
                _capi.Logger.Debug("Unregistering Player Await Listener");
                _capi.Event.UnregisterGameTickListener(_playerAwaitListenerId);
            }
        }

        private void OnClientLeave()
        {
            _cachedInventories.RemoveAll((_, _) => true);
            _lastItemStackRemoved = null;
            _player = null;
        }

        private void SlotModified(string invKey, int slotId)
        {
            var inv = (InventoryBase)_player.InventoryManager.Inventories[invKey];
            var cachedInvStacks = _cachedInventories[invKey];
            if (cachedInvStacks == null || cachedInvStacks.Length != inv.Count)
            {
                // Refresh cached inventories -> mainly happens dues to changes in equipped bags
                _cachedInventories[invKey] = CopyInventorySlots(inv);
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
            if (currentStackSize < newStackSize && !isStackSwap && !isLastRemovedItem)
            {
                NotifyItemPickup(newItemStack, currentStackSize);
                _lastItemStackRemoved = null;
            }

            if (!isLastRemovedItem || slotEmptied || !slotNoOp) _lastItemStackRemoved = currentItemStack?.Clone();
            _cachedInventories[invKey][slotId] = newItemStack?.Clone();
        }

        private static void NotifyItemPickup(ItemStack newStack, int currentSize)
        {
            var notifyStack = newStack.Clone();
            notifyStack.StackSize -= currentSize;
            _NotifierOverlay.AddItemStack(notifyStack);
            _NotifierOverlay.ShowNotification();
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