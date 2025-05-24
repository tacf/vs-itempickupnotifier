using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ItemPickupNotifier.GUI
{

    public class NotifierOverlay : HudElement
    {
        public override double DrawOrder => 0.06;

        private readonly double _showDuration = 4.0; // Duration in seconds to show notification
        private long _showUntilMs;
        private readonly List<ItemStack> _itemStacks = new List<ItemStack>();
        private readonly CairoFont _font;
        private readonly Vec4f _colour = new(0.91f, 0.87f, 0.81f, 1);
        private bool _debugBackgroundVisible = false;
        private readonly float _backgroundPreviewAlpha = 0.6f;
        private bool _debugModeEnabled = false;
        private bool _enabled = true;
        private double _windowSizeDetector = 0;



        public NotifierOverlay(ICoreClientAPI capi) : base(capi)
        {
            _font = InitFont();
            _enabled = ItempickupnotifierModSystem.Config.Enabled;
            CheckWindowResize();
            BuildDialog();
        }

        public void ShowNotification()
        {
            _showUntilMs = capi.World.ElapsedMilliseconds + (long)(_showDuration * 1000);

            if (!IsOpened() && IsEnabled())
            {
                BuildDialog();
                TryOpen(withFocus: false);
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            if (CheckWindowResize())
            {
                BuildDialog();
            }

            if (IsOpened() && (capi.World.ElapsedMilliseconds > _showUntilMs) && !IsDebugMode())
            {
                if (!_debugModeEnabled) TryClose();
                _itemStacks.Clear();
            }
        }

        private void BuildDialog()
        {
            if (!_itemStacks.Any() || !IsEnabled()) return;

            UpdateFontSize(ItempickupnotifierModSystem.Config.FontSize);
            UpdateFontWeight(ItempickupnotifierModSystem.Config.FontBold);

            double itemEntrySize = ElementBounds.scaled(30);
            double overlayWidth = ElementBounds.scaled(500);

            // Dialog base bound
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(ItempickupnotifierModSystem.Config.GetOverlayAnchor())
                .WithFixedOffset(ItempickupnotifierModSystem.Config.HorizontalOffset, ItempickupnotifierModSystem.Config.VerticalOffset)
                .WithFixedPadding(ElementBounds.scaled(5));

            // Background boundaries
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, overlayWidth, itemEntrySize * _itemStacks.Count);

            var bgColor = new double[] { 0.0, 0.0, 0.0, _debugBackgroundVisible ? _backgroundPreviewAlpha : 0.0 };

            var guiComposer = capi.Gui.CreateCompo("itemPickupNotifier", dialogBounds)
                .AddGameOverlay(bgBounds, bgColor)
                .BeginChildElements();

            // Create stacked text elements
            double yOffset = itemEntrySize * (_itemStacks.Count - 1);

            foreach (var itemStack in _itemStacks)
            {
                if (itemStack.ResolveBlockOrItem(capi.World))
                {
                    CompositeTexture texture = itemStack.Item != null ? itemStack.Item.FirstTexture : itemStack.Block.FirstTextureInventory;
                    if (texture != null)
                    {
                        ElementBounds textItemStackBounds = ElementBounds.Fixed(0, yOffset, overlayWidth, itemEntrySize);
                        var isComp = new ItemstackTextComponent(capi, itemStack, 35, 0, EnumFloat.Right);
                        isComp.offY -= 10;
                        string totalCount = "";
                        if (ItempickupnotifierModSystem.Config.TotalAmountEnabled) totalCount = " (" + ItempickupnotifierModSystem.GetTotalItemCountInInventories(itemStack.Id) + ")";
                        guiComposer.AddRichtext(new RichTextComponentBase[] { isComp, new RichTextComponent(capi, Regex.Replace(itemStack.StackSize + "x " + itemStack.GetName() + totalCount, "<.*?>", string.Empty), _font) }, textItemStackBounds);
                        yOffset -= itemEntrySize;
                    }
                }
            }

            guiComposer.EndChildElements();
            guiComposer.zDepth = 49f;
            SingleComposer = guiComposer.Compose();
        }

        public void AddItemStack(ItemStack itemStack)
        {
            var index = _itemStacks.FindIndex(i => i.Id == itemStack.Id);
            if (index >= 0)
            {
                // Refresh current value and push it to the top of the list
                _itemStacks.Add(itemStack);
                _itemStacks.Last().StackSize += _itemStacks[index].StackSize;
                _itemStacks.RemoveAt(index);
            }
            else
            {
                _itemStacks.Add(itemStack);
            }

            RefreshOverlay();
        }

        protected virtual CairoFont InitFont()
        {
            return new CairoFont()
                .WithColor(new double[] { _colour.R, _colour.G, _colour.B, _colour.A })
                .WithFont(GuiStyle.StandardFontName)
                .WithOrientation(EnumTextOrientation.Right)
                .WithFontSize(ItempickupnotifierModSystem.Config.FontSize)
                .WithWeight(ItempickupnotifierModSystem.Config.FontBold ? Cairo.FontWeight.Bold : Cairo.FontWeight.Normal)
                .WithStroke(new double[] { 0, 0, 0, 0.5 }, 2);
        }

        private void UpdateFontSize(float size)
        {
            _font.WithFontSize(size);
        }

        private void UpdateFontWeight(bool bold)
        {
            _font.WithWeight(bold ? Cairo.FontWeight.Bold : Cairo.FontWeight.Normal);
        }

        public bool IsDebugMode()
        {
            return _debugModeEnabled;
        }

        public bool IsEnabled() {
            return _enabled;
        }

        public void SetEnabled(bool toggled)
        {
            _enabled = toggled;
        }

        public void Debug(bool enabled)
        {
            _debugModeEnabled = enabled;
            if (IsDebugMode()) GenerateFakeData();
            RefreshOverlay();
        }

        public void BackgroundVisible(bool visible)
        {
            _debugBackgroundVisible = visible;
            RefreshOverlay();
        }

        public void RefreshOverlay()
        {
            if (!IsEnabled())
            {
                if (IsOpened())
                {
                    _itemStacks.Clear();
                    _debugModeEnabled = false;

                }
            }
            // TODO: If is opened update only contents
            if (IsOpened())
            {
                BuildDialog();
            }
            else if (IsDebugMode())
            {
                BuildDialog();
                if (!IsOpened()) TryOpen();
            }


        }

        private void GenerateFakeData()
        {
            _itemStacks.Clear();
            // Sealed Fired Crock
            _itemStacks.Add(new ItemStack(992, EnumItemClass.Item, 1, new TreeAttribute(), capi.World));
            // High Fertility soil
            _itemStacks.Add(new ItemStack(6414, EnumItemClass.Block, 31, new TreeAttribute(), capi.World));
            // Leather Backpak
            _itemStacks.Add(new ItemStack(2312, EnumItemClass.Item, 1, new TreeAttribute(), capi.World));
            // Magic Wand
            _itemStacks.Add(new ItemStack(1, EnumItemClass.Item, 1, new TreeAttribute(), capi.World));
            // Ruined Sword
            _itemStacks.Add(new ItemStack(1928, EnumItemClass.Item, 1, new TreeAttribute(), capi.World));
            // Vertex Eater
            _itemStacks.Add(new ItemStack(294, EnumItemClass.Block, 54, new TreeAttribute(), capi.World));
            // Creative Light
            _itemStacks.Add(new ItemStack(263, EnumItemClass.Block, 7, new TreeAttribute(), capi.World));

        }

        private bool CheckWindowResize()
        {
            var winDim = (capi?.Gui.WindowBounds.absX ?? 0) + (capi?.Gui.WindowBounds.absY ?? 0);
            if (winDim != _windowSizeDetector)
            {
                _windowSizeDetector = winDim;
                return true;
            }
            return false;
        }

    }
}
