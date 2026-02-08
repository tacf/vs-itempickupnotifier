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
        private int _totalEntries;
        private EnumDialogArea _anchor;
        private double _xOffset;
        private double _yOffset;
        private bool _invertedAlignment;
        
        private long _positionChangeStartMs;
        private double _lastStackPositionFromTop;
        
        private float _lastAlpha = -1f;
        private float _lastSlide = -1f;
        private float _lastPositionT = -1f;

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

        public bool IsExpired(long nowMs)
        {
            if (_expireAtMs == -1)
                return false;
            return nowMs > _expireAtMs;
        }

        public bool IsFullyFadedOut(long nowMs)
        {
            if (_expireAtMs == -1)
                return false;
            // Fully faded out after expiry + fade duration
            return nowMs > _expireAtMs + FadeOutMs;
        }

        public void UpdateLayout(int stackIndexFromTop, int totalEntries, EnumDialogArea anchor, double xOffset,
            double yOffset,
            bool invertedAlignment)
        {
            const double itemEntrySize = 35;
            
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

            bool layoutChanged = Math.Abs(itemEntrySize - _itemEntrySize) > 0.001 || anchor != _anchor ||
                                 Math.Abs(xOffset - _xOffset) > 0.001 ||
                                 Math.Abs(yOffset - _yOffset) > 0.001 || invertedAlignment != _invertedAlignment;

            _itemEntrySize = itemEntrySize;
            _totalEntries = totalEntries;
            _anchor = anchor;
            _xOffset = xOffset;
            _yOffset = yOffset;
            _invertedAlignment = invertedAlignment;

            if (positionChanged || layoutChanged)
                Rebuild();
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
            
            
            ElementBounds iconBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(40, 40)
                .WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(5,5);
            ElementBounds textBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(60, 60)
                .WithAlignment(EnumDialogArea.RightBottom).WithFixedPadding(2,0);

            string text = _stack.StackSize.ToString();

            _font.Orientation = EnumTextOrientation.Right;
            _font.WithColor(new double[] { _colour.R, _colour.G, _colour.B, _colour.A * alpha });
            _font.WithStroke(new double[] { 0, 0, 0, 0.3 * alpha }, 2); // Apply alpha to stroke (reduced opacity)

            ItemstackTextComponent isComp = new(capi, _stack, 50);

            ElementBounds bgBounds = ElementBounds.Fill.WithSizing(ElementSizing.FitToChildren)
                .WithChildren(iconBounds, textBounds);
            
            ElementBounds dialogBounds =
                ElementBounds.Fill.WithAlignment(EnumDialogArea.CenterBottom)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithFixedOffset((_targetStackIndexFromTop * 70) - _totalEntries * 70 / 2, -130)
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

                if (remaining <= 0)
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

            (EnumDialogArea dialogBaseAlign, EnumTextOrientation textOrientation) = GetAlignment();


            ElementBounds iconBounds = ElementBounds.Fill.WithSizing(ElementSizing.Fixed).WithFixedSize(35, 0)
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

            ItemstackTextComponent isComp = new(capi, _stack, 30);

            ElementBounds bgBounds = ElementBounds.Fill.WithSizing(ElementSizing.FitToChildren)
                .WithAlignment(dialogBaseAlign)
                .WithChildren(iconBounds, textBounds)
                .WithFixedPadding(2, 2);
            
            ElementBounds dialogBounds =
                ElementBounds.Fill.WithAlignment(_anchor)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithFixedOffset(_xOffset + slideSigned, _yOffset - (_stackPositionFromTop * _itemEntrySize))
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
        }


        private (EnumDialogArea dialogBaseAlign, EnumTextOrientation textOrientation) GetAlignment()
        {
            if (_invertedAlignment)
                return (EnumDialogArea.RightTop, EnumTextOrientation.Left);

            return (EnumDialogArea.LeftTop, EnumTextOrientation.Right);
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
    }
}