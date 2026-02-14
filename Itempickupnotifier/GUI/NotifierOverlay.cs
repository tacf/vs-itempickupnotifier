using System.Collections.Generic;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.Linq;

namespace ItemPickupNotifier.GUI
{
    public class NotifierOverlay : HudElement
    {
        // Prevent conficts with UIs and allow click through
        public override double DrawOrder => 0.06;
        public override bool ShouldReceiveMouseEvents() => false;
        public int DisplayTime = ItempickupnotifierModSystem.Config.NotificationDisplayTimeSeconds;
        private CairoFont _font;
        private readonly Vec4f _colour = new(0.91f, 0.87f, 0.81f, 1);
        private readonly float _backgroundPreviewAlpha = 0.6f;
        private bool _debugModeEnabled;
        private bool _enabled = true;
        private double _windowSizeDetector;
        private double _winWidth;
        private double _winHeight;
        private readonly LinkedList<ItemNotificationOverlay> _entries = new();

        public NotifierOverlay(ICoreClientAPI capi) : base(capi)
        {
            _font = InitFont();
            _enabled = ItempickupnotifierModSystem.Config.Enabled;
            capi.Event.RegisterGameTickListener(ExpireEntries, 200);
        }


        public void AddItemStack(ItemStack itemStack)
        {
            if (itemStack == null || !IsEnabled()) return;

            // Merge with existing overlay for the same item id
            ItemNotificationOverlay existing = _entries.FirstOrDefault(e => e.ItemId == itemStack.Id);
            if (existing != null)
            {
                existing.AddToStack(itemStack.StackSize, GetExpireAtMs());
                existing.Rebuild(forceRebuild: true);
                return;
            }

            ItemNotificationOverlay overlay = CreateEntryOverlay(itemStack.Clone());
            overlay.SetExpireAt(GetExpireAtMs());
            if (!overlay.TryOpen(withFocus: false)) return;
            _entries.AddFirst(overlay);
            RefreshUIElements();
        }

        public void RefreshOverlay()
        {
            if (!IsEnabled())
            {
                CloseAllEntries();
                return;
            }

            UpdateFontFromConfig();
            foreach (ItemNotificationOverlay entry in _entries)
            {
                entry.SyncFont(_font);
                entry.DebugBackgroundAlpha = _backgroundPreviewAlpha;
                entry.Rebuild();
            }

            RefreshUIElements();
        }

        public bool IsDebugMode() => _debugModeEnabled;
        public bool IsEnabled() => _enabled;

        public void SetEnabled(bool toggled)
        {
            _enabled = toggled;
            if (!_enabled) CloseAllEntries();
        }

        public void Debug(bool enabled)
        {
            _debugModeEnabled = enabled;
            if (_debugModeEnabled)
            {
                GenerateFakeData();
            }
            else
            {
                // Leaving debug: give entries a normal expiry
                long expiry = GetExpireAtMs();
                foreach (ItemNotificationOverlay entry in _entries)
                {
                    entry.SetDebugMode(false);
                    entry.SetExpireAt(expiry);
                }

                RefreshUIElements();
            }
        }

        private long GetExpireAtMs()
        {
            return _debugModeEnabled ? -1 : capi.World.ElapsedMilliseconds + (long)(DisplayTime * 1000);
        }

        private void ExpireEntries(float dt)
        {
            long nowMs = capi.World.ElapsedMilliseconds;
            var toRemove = _entries.Where(entry => entry.IsFullyFadedOut(nowMs)).ToList();
            if (toRemove.Count <= 0) return;
            foreach (ItemNotificationOverlay entry in toRemove)
            {
                entry.TryClose();
                entry.Dispose();
                _entries.Remove(entry);
            }

            RefreshUIElements();
        }

        private void RefreshUIElements()
        {
            int i = 0;
            foreach (ItemNotificationOverlay entry in _entries)
            {
                entry.UpdateLayout(i, _entries.Count, ItempickupnotifierModSystem.Config.GetOverlayAnchor(),
                    ItempickupnotifierModSystem.Config.HorizontalOffset,
                    ItempickupnotifierModSystem.Config.VerticalOffset);
                i++;
            }
        }

        private void CloseAllEntries()
        {
            foreach (ItemNotificationOverlay entry in _entries)
            {
                entry.Dispose();
            }

            _entries.Clear();
        }

        private ItemNotificationOverlay CreateEntryOverlay(ItemStack stack)
        {
            return new ItemNotificationOverlay(capi, stack, _font, _colour)
            {
                DebugBackgroundAlpha = _backgroundPreviewAlpha,
            };
        }

        private void UpdateFontFromConfig()
        {
            _font.WithFontSize(ItempickupnotifierModSystem.Config.FontSize);
            _font.WithWeight(ItempickupnotifierModSystem.Config.FontBold
                ? Cairo.FontWeight.Bold
                : Cairo.FontWeight.Normal);
        }

        private CairoFont InitFont()
        {
            return new CairoFont().WithColor(new double[] { _colour.R, _colour.G, _colour.B, _colour.A })
                .WithFont(GuiStyle.StandardFontName).WithOrientation(EnumTextOrientation.Right)
                .WithFontSize(ItempickupnotifierModSystem.Config.FontSize)
                .WithWeight(ItempickupnotifierModSystem.Config.FontBold
                    ? Cairo.FontWeight.Bold
                    : Cairo.FontWeight.Normal).WithStroke(new double[] { 0, 0, 0, 0.5 }, 2);
        }

        private void GenerateFakeData()
        {
            CloseAllEntries();
            var fakeStacks = new[]
            {
                new ItemStack(992, EnumItemClass.Item, 1, new TreeAttribute(), capi.World),
                new ItemStack(6414, EnumItemClass.Block, 31, new TreeAttribute(), capi.World),
                new ItemStack(2312, EnumItemClass.Item, 1, new TreeAttribute(), capi.World),
                new ItemStack(1, EnumItemClass.Item, 1, new TreeAttribute(), capi.World),
                new ItemStack(1928, EnumItemClass.Item, 1, new TreeAttribute(), capi.World),
                new ItemStack(294, EnumItemClass.Block, 54, new TreeAttribute(), capi.World),
                new ItemStack(263, EnumItemClass.Block, 7, new TreeAttribute(), capi.World),
            };
            foreach (ItemStack stack in fakeStacks)
            {
                ItemNotificationOverlay overlay = CreateEntryOverlay(stack);
                overlay.SetDebugMode(true);
                overlay.SetExpireAt(-1);
                _entries.AddLast(overlay);
            }

            RefreshUIElements();
            foreach (ItemNotificationOverlay entry in _entries)
            {
                entry.TryOpen(withFocus: false);
            }
        }

        private bool CheckWindowResize()
        {
            double winDim = (capi?.Gui.WindowBounds.absX ?? 0) + (capi?.Gui.WindowBounds.absY ?? 0);
            if (Math.Abs(winDim - _windowSizeDetector) > 0.001)
            {
                _windowSizeDetector = winDim;
                return true;
            }

            return false;
        }
    }
}