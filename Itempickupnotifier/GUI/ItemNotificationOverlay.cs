using System;
using System.Text.RegularExpressions;
using Cairo;
using ItemPickupNotifier.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ItemPickupNotifier.GUI
{

    internal class ItemNotificationOverlay : HudElement
    {
        // Prevent conficts with UIs and allow click through
        public override double DrawOrder => 0.06;
        public override bool ShouldReceiveMouseEvents() => false;

        public int ItemId { get; }
        public bool DebugBackgroundVisible { get; set; }
        public float DebugBackgroundAlpha { get; set; } = 0.6f;

        private EnumBackgroundMode BackgroundMode =>
            Enum.TryParse<EnumBackgroundMode>(ItempickupnotifierModSystem.Config.Background,
                out EnumBackgroundMode result)
                ? result
                : EnumBackgroundMode.None;

        private readonly ItemStack _stack;
        private CairoFont _font;
        private readonly Vec4f _colour;

        private long _expireAtMs;
        private bool _debugMode;
        
        private const float FadeOutMs = 300f;
        private const float SlideOutPx = 100f;
        private const float PositionTransitionMs = 200f;
        
        private double _stackPositionFromTop;
        private int _targetStackIndexFromTop;
        private double _itemEntrySize;
        private double _measuredRowHeightUnscaled;
        private int _totalEntries;
        private EnumNotifierMode _mode;
        private EnumAnchor _anchor;
        private double _xOffset;
        private double _yOffset;
        
        private long _positionChangeStartMs;
        private double _lastStackPositionFromTop;
        
        private float _lastAlpha = -1f;
        private float _lastSlide = -1f;
        private float _lastPositionT = -1f;
        private bool _invertedAlignment => _anchor is EnumAnchor.TopLeft or EnumAnchor.BottomLeft;

        public ItemNotificationOverlay(ICoreClientAPI capi, ItemStack stack, CairoFont sharedFont, Vec4f colour)
            : base(capi)
        {
            _stack = stack ?? throw new System.ArgumentNullException(nameof(stack));
            _font = sharedFont.Clone();
            _colour = colour;
            ItemId = stack.Id;
        }


        public void SyncFont(CairoFont sharedFont)
        {
            _font = sharedFont.Clone();
        }

        public void AddToStack(int addAmount, long newExpireAtMs)
        {
            _stack.StackSize += addAmount;
            SetExpireAt(newExpireAtMs);
            Rebuild();
        }

        public void SetExpireAt(long expireAtMs) => _expireAtMs = expireAtMs;
        public void SetDebugMode(bool debugMode) => _debugMode = debugMode;

        public bool IsFullyFadedOut(long nowMs)
        {
            if (_expireAtMs == -1)
                return false;
            
            // Expire immediately if animations disabled
            if (nowMs > _expireAtMs && !ItempickupnotifierModSystem.Config.Animations) return true;
            
            // Fully faded out after expiry + fade duration
            return nowMs > _expireAtMs + FadeOutMs;
        }

        public void UpdateLayout(int stackIndexFromTop, int totalEntries, EnumAnchor anchor, double xOffset,
            double yOffset, double sharedItemEntrySize)
        {
            EnumNotifierMode mode = Enum.Parse<EnumNotifierMode>(ItempickupnotifierModSystem.Config.Mode);
            double fallbackItemEntrySize = GetEntrySpacingUnscaled(mode);
            double itemEntrySize = sharedItemEntrySize > 0 ? sharedItemEntrySize : fallbackItemEntrySize;
            
            if (_positionChangeStartMs == 0)
            {
                _stackPositionFromTop = stackIndexFromTop;
                _lastStackPositionFromTop = stackIndexFromTop;
                _targetStackIndexFromTop = stackIndexFromTop;
                _positionChangeStartMs = capi.World.ElapsedMilliseconds - (long)PositionTransitionMs;
            }

            bool positionChanged = stackIndexFromTop != _targetStackIndexFromTop;

            if (positionChanged)
            {
                _lastStackPositionFromTop = _stackPositionFromTop;
                _targetStackIndexFromTop = stackIndexFromTop;
                _positionChangeStartMs = capi.World.ElapsedMilliseconds;
            }

            bool layoutChanged = Math.Abs(itemEntrySize - _itemEntrySize) > 0.001 || anchor != _anchor || mode != _mode ||
                                 Math.Abs(xOffset - _xOffset) > 0.001 ||
                                 Math.Abs(yOffset - _yOffset) > 0.001;

            _itemEntrySize = itemEntrySize;
            _totalEntries = totalEntries;
            _mode = mode;
            _anchor = anchor;
            _xOffset = xOffset;
            _yOffset = yOffset;

            if (positionChanged || layoutChanged)
                Rebuild();
        }

        public double GetPreferredEntrySpacingUnscaled()
        {
            EnumNotifierMode mode = Enum.Parse<EnumNotifierMode>(ItempickupnotifierModSystem.Config.Mode);
            double fallbackSpacing = GetEntrySpacingUnscaled(mode);
            if (_measuredRowHeightUnscaled > 0)
            {
                double measuredWithGap = _measuredRowHeightUnscaled + GetMinimumGapUnscaled(mode);
                return Math.Max(fallbackSpacing, measuredWithGap);
            }

            return fallbackSpacing;
        }


        public override void OnRenderGUI(float deltaTime)
        {
            bool needsRebuild = false;
            
            if (!_debugMode && _expireAtMs != -1)
            {
                float remaining = _expireAtMs - capi.World.ElapsedMilliseconds;
                if (remaining is > (-FadeOutMs) and <= FadeOutMs)
                {
                    needsRebuild = true;
                }
            }
            
            long elapsed = capi.World.ElapsedMilliseconds - _positionChangeStartMs;
            if (elapsed < PositionTransitionMs)
            {
                needsRebuild = true;
            }

            if (needsRebuild)
                Rebuild();

            base.OnRenderGUI(deltaTime);
        }

        public void Rebuild(bool forceRebuild = false)
        {
            if (Enum.Parse<EnumNotifierMode>(ItempickupnotifierModSystem.Config.Mode) == EnumNotifierMode.Standard)
            {
                RebuildStandardMode(forceRebuild);
            }
            else
            {
                RebuildIconMode(forceRebuild);
            }
        }

        private void RebuildIconMode(bool forceRebuild)
        {
            if (!_stack.ResolveBlockOrItem(capi.World))
                return;

            CompositeTexture texture =
                _stack.Item != null ? _stack.Item.FirstTexture : _stack.Block?.FirstTextureInventory;

            if (texture == null)
                return;
            
            float alpha = 1f;

            if (!_debugMode && _expireAtMs != -1)
            {
                float remaining = _expireAtMs - capi.World.ElapsedMilliseconds;

                if (remaining <= 0)
                {
                    // Fading out after expiry
                    float fadeProgress = GameMath.Clamp(-remaining / FadeOutMs, 0f, 1f);
                    alpha = 1f - fadeProgress;
                }
            }
            
            _lastAlpha = alpha;
            
            
            double iconSize = GetIconSizeUnscaled(EnumNotifierMode.IconsOnly);
            double spacing = _itemEntrySize > 0 ? _itemEntrySize : GetEntrySpacingUnscaled(EnumNotifierMode.IconsOnly);
            double itemStackSizeUnscaled = GetItemStackComponentSizeUnscaled(iconSize);
            
            ElementBounds iconBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(iconSize, iconSize)
                .WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(5,5);
            ElementBounds textBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(iconSize * 1.5, iconSize * 1.5)
                .WithAlignment(EnumDialogArea.RightBottom).WithFixedPadding(2,0);

            string text = _stack.StackSize.ToString();

            _font.Orientation = EnumTextOrientation.Right;
            _font.WithColor(new double[] { _colour.R, _colour.G, _colour.B, _colour.A * alpha });
            _font.WithStroke(new double[] { 0, 0, 0, 0.3 * alpha }, 2); // Apply alpha to stroke (reduced opacity)

            ItemstackTextComponent isComp = new(capi, _stack, itemStackSizeUnscaled);

            ElementBounds bgBounds = ElementBounds.Fill.WithSizing(ElementSizing.FitToChildren)
                .WithChildren(iconBounds, textBounds);
            
            ElementBounds dialogBounds =
                ElementBounds.Fill.WithAlignment(EnumDialogArea.CenterBottom)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithFixedOffset((_targetStackIndexFromTop * spacing) - _totalEntries * spacing / 2, -130)
                    .WithChildren(bgBounds);

            string composerKey = "itemPickupNotifierItem-" + ItemId;

            GuiComposer guiComposer = capi.Gui.CreateCompo(composerKey, dialogBounds);

            if (BackgroundMode != EnumBackgroundMode.Native || DebugBackgroundVisible)
            {
                guiComposer.AddGameOverlay(bgBounds,
                    new double[] { 0, 0, 0, 0.2f * alpha * (BackgroundMode == EnumBackgroundMode.None ? 0 : 1.0f) });
            }
            else
            {
                guiComposer.AddDialogBG(bgBounds, false, alpha);
            }


            guiComposer.BeginChildElements(bgBounds);
            guiComposer.AddRichtext(text, _font, textBounds);
            guiComposer.AddRichtext(new[] { isComp }, iconBounds);
            guiComposer.EndChildElements();

            SingleComposer = guiComposer.Compose();
            _measuredRowHeightUnscaled = GetUnscaledHeight(SingleComposer.Bounds.OuterHeight);
        }

        private void RebuildStandardMode(bool forceRebuild)
        {
            if (!_stack.ResolveBlockOrItem(capi.World))
                return;

            CompositeTexture texture =
                _stack.Item != null ? _stack.Item.FirstTexture : _stack.Block?.FirstTextureInventory;

            if (texture == null)
                return;
            
            float alpha = 1f;
            float slide = 0f;

            if (!_debugMode && _expireAtMs != -1)
            {
                float remaining = _expireAtMs - capi.World.ElapsedMilliseconds;

                if (remaining <= 0 && ItempickupnotifierModSystem.Config.Animations)
                {
                    // Fading out after expiry
                    float fadeProgress = GameMath.Clamp(-remaining / FadeOutMs, 0f, 1f);
                    alpha = 1f - fadeProgress;
                    slide = fadeProgress * SlideOutPx;
                }
            }
            
            float positionT = 1f;
            long elapsed = capi.World.ElapsedMilliseconds - _positionChangeStartMs;
            if (elapsed < PositionTransitionMs)
            {
                positionT = GameMath.Clamp(elapsed / PositionTransitionMs, 0f, 1f);
                // Ease out cubic
                positionT = 1f - (float)Math.Pow(1f - positionT, 3);
            }
            
            _stackPositionFromTop =
                _lastStackPositionFromTop + (_targetStackIndexFromTop - _lastStackPositionFromTop) * positionT;

            // Skip rebuild when nothing visual changed
            if (Math.Abs(alpha - _lastAlpha) < 0.005f && Math.Abs(slide - _lastSlide) < 0.5f &&
                Math.Abs(positionT - _lastPositionT) < 0.005f && SingleComposer != null && !forceRebuild)
            {
                return;
            }

            _lastAlpha = alpha;
            _lastSlide = slide;
            _lastPositionT = positionT;
            float slideSigned = _invertedAlignment ? -slide : slide;

            (EnumDialogArea dialogBaseAlign, EnumTextOrientation textOrientation, int xSign, int ySign) = GetAlignment();

            double iconSize = GetIconSizeUnscaled(EnumNotifierMode.Standard);
            double itemStackSizeUnscaled = GetItemStackComponentSizeUnscaled(iconSize);

            ElementBounds iconBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(iconSize, iconSize)
                .WithFixedOffset(0, -2); 
            ElementBounds textBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed)
                .WithFixedPadding(2, 0);
            if (_invertedAlignment)
            {
                textBounds.LeftOfBounds = iconBounds;
            }
            else
            {
                iconBounds.LeftOfBounds = textBounds;
            }

            string text = BuildDisplayLine();

            _font.Orientation = textOrientation;
            _font.WithColor(new double[] { _colour.R, _colour.G, _colour.B, _colour.A * alpha });
            _font.WithStroke(new double[] { 0, 0, 0, 0.3 * alpha }, 2); // Apply alpha to stroke (reduced opacity)
            _font.AutoBoxSize(text, textBounds);

            ItemstackTextComponent isComp = new(capi, _stack, itemStackSizeUnscaled);

            ElementBounds bgBounds = ElementBounds.Fill.WithSizing(ElementSizing.FitToChildren)
                .WithAlignment(dialogBaseAlign)
                .WithChildren(iconBounds, textBounds)
                .WithFixedPadding(2, 2);
            
            ElementBounds dialogBounds =
                ElementBounds.Fill.WithAlignment(ItemPickupNotifierConfig.AnchorToPosition(_anchor))
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithFixedOffset(_xOffset*xSign + slideSigned, _yOffset*ySign + ySign*(_stackPositionFromTop * _itemEntrySize))
                    .WithChildren(bgBounds);

            string composerKey = "itemPickupNotifierItem-" + ItemId;

            GuiComposer guiComposer = capi.Gui.CreateCompo(composerKey, dialogBounds);

            if (BackgroundMode != EnumBackgroundMode.Native || DebugBackgroundVisible)
            {
                guiComposer.AddGameOverlay(bgBounds,
                    new double[] { 0, 0, 0, 0.2f * alpha * (BackgroundMode == EnumBackgroundMode.None ? 0 : 1.0f) });
            }
            else
            {
                guiComposer.AddDialogBG(bgBounds, false, alpha);
            }


            guiComposer.BeginChildElements(bgBounds);
            guiComposer.AddRichtext(new[] { isComp }, iconBounds);
            guiComposer.AddRichtext(text, _font, textBounds);
            guiComposer.EndChildElements();

            SingleComposer = guiComposer.Compose();
            _measuredRowHeightUnscaled = GetUnscaledHeight(SingleComposer.Bounds.OuterHeight);
        }


        private (EnumDialogArea dialogBaseAlign, EnumTextOrientation textOrientation, int xSign, int ySign) GetAlignment()
        {
            int hSign = (_anchor is EnumAnchor.BottomRight or EnumAnchor.TopRight) ? -1 : 1;
            int vSign = (_anchor is EnumAnchor.BottomLeft or EnumAnchor.BottomRight) ? -1 : 1;
            
            if (_invertedAlignment)
                return (EnumDialogArea.RightTop, EnumTextOrientation.Left, hSign, vSign);

            return (EnumDialogArea.LeftTop, EnumTextOrientation.Right, hSign, vSign);
        }

        private string BuildDisplayLine()
        {
            string totalCount = "";

            if (ItempickupnotifierModSystem.Config.TotalAmountEnabled)
            {
                int inBags = ItempickupnotifierModSystem.GetTotalItemCountInInventories(_stack.Id);
                if (inBags > 1)
                    totalCount = " (" + inBags + ")";
                if (_debugMode)
                    totalCount = " (99)";
            }

            string raw = _stack.StackSize + "x " + _stack.GetName() + totalCount;
            return Regex.Replace(raw, "<.*?>", string.Empty);
        }

        private static double GetIconSizeUnscaled(EnumNotifierMode mode)
        {
            double uiScale = GetUiScale();
            double fontPx = ItempickupnotifierModSystem.Config.GetUnscaledFontSize() * uiScale;
            double multiplier = mode == EnumNotifierMode.Standard ? 1.9 : 2.2;
            double iconScaleFactor = GetIconScaleFactor(uiScale);
            double iconPx = Math.Clamp(
                fontPx * multiplier * iconScaleFactor,
                14d * uiScale * iconScaleFactor,
                96d * uiScale * iconScaleFactor
            );
            return iconPx / uiScale;
        }

        private static double GetEntrySpacingUnscaled(EnumNotifierMode mode)
        {
            double uiScale = GetUiScale();
            double fontPx = ItempickupnotifierModSystem.Config.GetUnscaledFontSize() * uiScale;
            double iconPx = GetIconSizeUnscaled(mode) * uiScale;
            double contentPx = mode == EnumNotifierMode.IconsOnly
                ? Math.Max(iconPx * 1.55, fontPx * 1.25)
                : Math.Max(iconPx + (2d * uiScale), fontPx * 1.45);
            double verticalGapPx = mode == EnumNotifierMode.IconsOnly
                ? Math.Max(8d * uiScale, contentPx * 0.18)
                : Math.Max(6d * uiScale, contentPx * 0.12);
            return (contentPx + verticalGapPx) / uiScale;
        }

        private static double GetItemStackComponentSizeUnscaled(double iconSizeUnscaled)
        {
            return Math.Max(1d, iconSizeUnscaled * 0.9d);
        }

        private static double GetUiScale()
        {
            return Math.Max(0.01d, ElementBounds.scaled(1.0));
        }

        private static double GetIconScaleFactor(double uiScale)
        {
            const double lowUiScale = 0.7;
            const double highUiScale = 1.7;
            const double lowReduction = 0.07;  // 7% smaller at low scales
            const double highReduction = 0.30; // 30% smaller at high scales

            double t = Math.Clamp((uiScale - lowUiScale) / (highUiScale - lowUiScale), 0d, 1d);
            double reduction = lowReduction + (highReduction - lowReduction) * t;
            return 1d - reduction;
        }

        private static double GetMinimumGapUnscaled(EnumNotifierMode mode)
        {
            double uiScale = GetUiScale();
            double gapPx = mode == EnumNotifierMode.IconsOnly ? 8d * uiScale : 6d * uiScale;
            return gapPx / uiScale;
        }

        private static double GetUnscaledHeight(double scaledHeight)
        {
            double uiScale = GetUiScale();
            return scaledHeight / uiScale;
        }
    }
}
