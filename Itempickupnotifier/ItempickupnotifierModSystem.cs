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
        private static NotifierOverlay _NotifierOverlay;
        private static SettingsUI _GuiSettings;
        public static ItemPickupNotifierConfig Config;

        private static ICoreClientAPI _capi;
        private IClientPlayer _player;
        private static Dictionary<string, ItemStack[]> _cachedInventories = new();
        private ItemStack _lastItemStackRemoved;
        private long _playerAwaitListenerId;

        public override void StartClientSide(ICoreClientAPI api)
        {
            _capi = api;
            base.StartClientSide(_capi);

            _capi.Event.LeftWorld += OnClientLeave;
            _playerAwaitListenerId = _capi.Event.RegisterGameTickListener(CheckPlayerReady, 200);

            Config = new(_capi);
            Config.Load(_capi);
            SaveSettings();

            _NotifierOverlay = new(_capi);
            _GuiSettings = CreateSettingsUI();
            _GuiSettings.Build();
            RegisterHotKeys();
        }

        public static void SaveSettings()
        {
            // Create a simplified version of the config for serialization
            Config.Save();
        }

        private SettingsUI CreateSettingsUI()
        {
            SettingsUI ui = new("itempickupnotifier", _capi, OnSettingsSavedClicked, CloseWithoutSaving);

            ui.Section("global")
                .AddSwitch("enabled", OnModToggled, Config.Enabled)
                .AddDropdown("mode", OnModeChanged, Enum.GetNames(typeof(EnumNotifierMode)), defaultName: Config.Mode)
                .AddSlider("displaytime", OnDisplayTimeChanged, Config.NotificationDisplayTimeSeconds, minValue: 4,
                    maxValue: 30);
            ui.Section("font")
                .AddSlider("size", OnFontSizeChanged, Config.GetUnscaledFontSize(), minValue: 5, maxValue: 20)
                .AddSwitch("bold", OnBoldToggled, Config.FontBold);
            ui.Section("position")
                .AddSwitch("invertalignment", OnAlignmentChanged, Config.InvertedAlignment)
                .AddSlider("xoffset", OnXOffsetChanged, Config.GetUnscaledHorizontalOffset(), minValue: -100)
                .AddSlider("yoffset", OnYOffsetChanged, Config.GetUnscaledVerticalOffset(), minValue: -100)
                .AddDropdown("pos", OnSelectionChanged, Enum.GetNames(typeof(EnumDialogArea)), defaultName: Config.Anchor.ToString());
            ui.Section("features")
                .AddSwitch("total-amount-bags", OnTotalAmountToggled, Config.TotalAmountEnabled)
                .AddDropdown("background", OnBackgroundTypeChanged, Enum.GetNames(typeof(EnumBackgroundMode)), defaultName: Config.Background.ToString());
            ui.Section("dev")
                .AddSwitch("preview-mode", OnDevPreviewToggled, persistState: false);
            return ui;
        }

        private bool OnDisplayTimeChanged(int displayTime)
        {
            Config.NotificationDisplayTimeSeconds = displayTime;
            _NotifierOverlay.DisplayTime = displayTime;
            return true;
        }

        private void OnAlignmentChanged(bool toggled)
        {
            Config.InvertedAlignment = toggled;
            _NotifierOverlay.RefreshOverlay();
        }

        private void OnModeChanged(string code, bool selected)
        {
            Config.Mode = code;
            _NotifierOverlay.RefreshOverlay();
        }
        
        private void OnBackgroundTypeChanged(string code, bool selected)
        {
            Config.Background = code;
            _NotifierOverlay.RefreshOverlay();
        }

        private void OnModToggled(bool toggled)
        {
            Config.Enabled = toggled;
            _NotifierOverlay.SetEnabled(toggled);
            _NotifierOverlay.Debug(_NotifierOverlay.IsDebugMode());
            _NotifierOverlay.RefreshOverlay();
        }


        private void OnTotalAmountToggled(bool totalAmountEnabled)
        {
            Config.TotalAmountEnabled = totalAmountEnabled;
            _NotifierOverlay.RefreshOverlay();
        }


        private static bool OnFontSizeChanged(int fontSize)
        {
            Config.FontSize = fontSize;
            _NotifierOverlay.RefreshOverlay();
            return true;
        }


        private static bool OnXOffsetChanged(int offset)
        {
            Config.HorizontalOffset = offset;
            _NotifierOverlay.RefreshOverlay();
            return true;
        }

        private static bool OnYOffsetChanged(int offset)
        {
            Config.VerticalOffset = offset;
            _NotifierOverlay.RefreshOverlay();
            return true;
        }
        

        private static bool OnSettingsSavedClicked()
        {
            OnDevPreviewToggled(false);
            OnDevPreviewToggled(false);
            _GuiSettings.StoreCurrentValues();
            SaveSettings();
            _NotifierOverlay.RefreshOverlay();

            _GuiSettings.TryClose();
            return true;
        }

        private bool CloseWithoutSaving()
        {
            OnDevPreviewToggled(false);
            OnDevPreviewToggled(false);
            _GuiSettings.CloseWithoutSaving();
            return true;
        }

        private static void OnDevPreviewToggled(bool toggle)
        {
            _NotifierOverlay.Debug(toggle);
        }

        private static void OnSelectionChanged(string code, bool selected)
        {
            if (!selected) return;
            Config.Anchor = code;
            _NotifierOverlay.RefreshOverlay();
        }

        private static void OnBoldToggled(bool bold)
        {
            Config.FontBold = bold;
            _NotifierOverlay.RefreshOverlay();
        }


        private void RegisterHotKeys()
        {
            _capi.Input.RegisterHotKey("itempickupnotifier:config", "Item Pickup Notifier Config", GlKeys.Z, type: HotkeyType.GUIOrOtherControls, ctrlPressed: true);
            _capi.Input.SetHotKeyHandler("itempickupnotifier:config", OnConfigHotKeyPressed);
        }

        private bool OnConfigHotKeyPressed(KeyCombination keyCombination)
        {
            if (_GuiSettings.IsOpened())
            {
                CloseWithoutSaving();
            }
            else _GuiSettings.TryOpen();
        
            return true;
        }

        
        
        private void CheckPlayerReady(float dt)
        {
            if (_capi.PlayerReadyFired)
            {
                _capi.Logger.Debug("Player is ready - Caching Inventories");
                _player = _capi.World.Player;
                foreach ((string invKey, IInventory inv) in _player.InventoryManager.Inventories)
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
            InventoryBase inv = (InventoryBase)_player.InventoryManager.Inventories[invKey];
            var cachedInvStacks = _cachedInventories[invKey];
            if (cachedInvStacks == null || cachedInvStacks.Length != inv.Count)
            {
                // Refresh cached inventories -> mainly happens dues to changes in equipped bags
                _cachedInventories[invKey] = CopyInventorySlots(inv);
                return;
            }

            ItemStack currentItemStack = cachedInvStacks[slotId];
            ItemStack newItemStack = inv[slotId].Itemstack;

            int currentStackSize = currentItemStack?.StackSize ?? 0;
            int newStackSize = newItemStack?.StackSize ?? 0;

            bool slotFilled = (newItemStack != null) && (currentItemStack == null);
            bool slotEmptied = !slotFilled;
            bool slotChange = newItemStack != null && currentItemStack != null;
            bool slotNoOp = !slotChange && newStackSize == 0 && currentStackSize == 0;
            bool slotChangedAmmount = slotChange && newItemStack.Id == currentItemStack.Id && newStackSize != currentStackSize;
            bool slotChangedItemType = slotChange && (currentItemStack?.Id != newItemStack?.Id);
            bool isStackSwap = slotChange && slotChangedItemType;
            bool isLastRemovedItem = (slotFilled || slotChangedAmmount || slotChangedItemType) && _lastItemStackRemoved != null && _lastItemStackRemoved.Id == newItemStack.Id && _lastItemStackRemoved.StackSize == newStackSize;

            _cachedInventories[invKey][slotId] = newItemStack?.Clone();
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
            if (_NotifierOverlay == null || newStack == null) return;

            ItemStack notifyStack = newStack.Clone();
            notifyStack.StackSize -= currentSize;
            _NotifierOverlay.AddItemStack(notifyStack);
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
            string typeName = inv.GetType().Name;
            return typeName is "InventoryPlayerHotbar" or "InventoryPlayerBackPacks";
        }

        public static int GetTotalItemCountInInventories(int itemCode)
        {
            return _cachedInventories.Values
                .SelectMany(inv => inv.Where(stack => stack?.Id == itemCode))
                .Sum(stack => stack.StackSize);
        }
    }
}