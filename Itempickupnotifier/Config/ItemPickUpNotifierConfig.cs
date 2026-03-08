#nullable enable
using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.Config
{

    public enum EnumNotifierMode
    {
        Standard,
        IconsOnly,
    }
    
    public enum EnumBackgroundMode
    {
        None,
        Simple,
        Native,
    }
    
    public enum EnumAnchor
    {
        TopLeft,
        BottomLeft,
        TopRight,
        BottomRight,
    }
    
    /// <summary>
    /// Configure the ItemPickupNotifier Overlay.
    /// </summary>
    public class ItemPickupNotifierConfig
    {

        public const string FileName = "itempickupnotifier.json";
        public const int MaxFontSize = 20;
        public const int MinFontSize = 5;

        public ICoreClientAPI _capi;

        // <summary> Display mode for notifications </summary>
        public string Mode
        {
            get => _mode.ToString();
            set => _mode = Enum.TryParse<EnumNotifierMode>(value, out EnumNotifierMode result) ? result : EnumNotifierMode.Standard;
        }
        
        // <summary> Background Type for notifications </summary>
        public string Background
        {
            get => _backgroundMode.ToString();
            set => _backgroundMode = Enum.TryParse<EnumBackgroundMode>(value, out EnumBackgroundMode result) ? result : EnumBackgroundMode.None;
        }
        
        /// <summary>Overlay Anchor (Base position - based on EnumDialogArea)</summary>
        public string Anchor
        {
            get => _anchor.ToString();
            set => _anchor = Enum.TryParse<EnumAnchor>(value, out EnumAnchor result) ? result : EnumAnchor.BottomRight;
        }

        /// <summary>Horizontal offset (in pixels)</summary>
        public double HorizontalOffset
        {
            get => GetRelativeScaledOffset(_horizontalOffset, _capi?.Gui.WindowBounds.InnerWidth);
            set => _horizontalOffset = value;
        }

        /// <summary>Vertical Offset (in pixels)</summary>
        public double VerticalOffset
        {
            get => GetRelativeScaledOffset(_verticalOffset, _capi?.Gui.WindowBounds.InnerHeight);
            set => _verticalOffset = value;
        }

        public float FontSize
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        public bool FontBold = true;
        public bool TotalAmountEnabled = false;
        public bool Enabled = true;
        public bool Animations = true;
        public int NotificationDisplayTimeSeconds = 4;

        private EnumAnchor _anchor = EnumAnchor.BottomRight;
        private EnumNotifierMode _mode = EnumNotifierMode.Standard;
        private EnumBackgroundMode _backgroundMode = EnumBackgroundMode.None;
        private float _fontSize = 16.0f;
        private double _horizontalOffset = 0.0f;
        private double _verticalOffset = 0.0f;

        public ItemPickupNotifierConfig(ICoreClientAPI capi)
        {
            _capi = capi;
        }

        public EnumAnchor GetOverlayAnchor()
        {
            return _anchor;
        }

        public void Save()
        {
            var configToSave = new
            {
                Enabled,
                Anchor,
                Mode,
                Background,
                Animations,
                NotificationDisplayTimeSeconds,
                HorizontalOffset = GetUnscaledHorizontalOffset(),
                VerticalOffset = GetUnscaledVerticalOffset(),
                FontSize = GetUnscaledFontSize(),
                FontBold,
                TotalAmountEnabled,

            };

            _capi?.StoreModConfig(configToSave, FileName);
        }

        public void Load(ICoreClientAPI capi)
        {
            ItemPickupNotifierConfig? loaded = capi.LoadModConfig<ItemPickupNotifierConfig>(FileName);
            if (loaded != null)
            {
                Enabled = loaded.Enabled;
                Anchor = loaded.Anchor;
                Mode = loaded.Mode;
                Background = loaded.Background;
                Animations = loaded.Animations;
                NotificationDisplayTimeSeconds = loaded.NotificationDisplayTimeSeconds;
                // The '% 100' is to ensure proper migration of existing configs (avoids settings windows out of bounds elements)
                HorizontalOffset = loaded._horizontalOffset % 100;
                VerticalOffset = loaded._verticalOffset % 100;
                // Clamp directly so max value (20) does not wrap to 0 on reload.
                FontSize = Math.Clamp(loaded._fontSize, MinFontSize, MaxFontSize);
                FontBold = loaded.FontBold;
                TotalAmountEnabled = loaded.TotalAmountEnabled;
            }
        }

        public void ResetToDefaults()
        {
            ItemPickupNotifierConfig defaults = new ItemPickupNotifierConfig(_capi);
            Enabled = defaults.Enabled;
            Anchor = defaults.Anchor;
            Mode = defaults.Mode;
            Background = defaults.Background;
            Animations = defaults.Animations;
            NotificationDisplayTimeSeconds = defaults.NotificationDisplayTimeSeconds;
            HorizontalOffset = defaults._horizontalOffset;
            VerticalOffset = defaults._verticalOffset;
            FontSize = defaults._fontSize;
            FontBold = defaults.FontBold;
            TotalAmountEnabled = defaults.TotalAmountEnabled;
        }

        private static double GetRelativeScaledOffset(double offset, double? reference)
        {
            if (reference == null) return (float)offset;
            return ((reference / 2) ?? 0) * (offset / 100);
        }

        public int GetUnscaledHorizontalOffset()
        {
            return (int)_horizontalOffset;
        }

        public int GetUnscaledVerticalOffset()
        {
            return (int)_verticalOffset;
        }

        public int GetUnscaledFontSize()
        {
            return (int)_fontSize;
        }

        public static EnumDialogArea AnchorToPosition(EnumAnchor anchor)
        {
            return anchor switch
            {
                EnumAnchor.TopLeft => EnumDialogArea.LeftTop,
                EnumAnchor.BottomLeft => EnumDialogArea.LeftBottom,
                EnumAnchor.TopRight => EnumDialogArea.RightTop,
                EnumAnchor.BottomRight => EnumDialogArea.RightBottom,
                _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
            };
        }
    }
}
