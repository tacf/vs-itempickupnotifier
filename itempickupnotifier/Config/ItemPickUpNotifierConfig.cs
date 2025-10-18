#nullable enable
using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ItemPickupNotifier.Config
{

    public enum EnumNotifierMode
    {
        Standard,
        IconsOnly,
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
        
        /// <summary>Overlay Anchor (Base position - based on EnumDialogArea)</summary>
        public string Anchor
        {
            get => _anchor.ToString();
            set => _anchor = Enum.TryParse<EnumDialogArea>(value, out EnumDialogArea result) ? result : EnumDialogArea.RightBottom;
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
            get => (float)ElementBounds.scaled(_fontSize);
            set => _fontSize = value;
        }

        public bool FontBold = true;
        public bool TotalAmountEnabled = false;
        public bool Enabled = true;
        public bool InvertedAlignment = false;
        public int NotificationDisplayTimeSeconds = 4;

        private EnumDialogArea _anchor = EnumDialogArea.RightBottom;
        private EnumNotifierMode _mode = EnumNotifierMode.Standard;
        private float _fontSize = 16.0f;
        private double _horizontalOffset = 0.0f;
        private double _verticalOffset = 0.0f;

        public ItemPickupNotifierConfig(ICoreClientAPI capi)
        {
            _capi = capi;
        }

        public EnumDialogArea GetOverlayAnchor()
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
                NotificationDisplayTimeSeconds,
                InvertedAlignment,
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
            var loaded = capi.LoadModConfig<ItemPickupNotifierConfig>(FileName);
            if (loaded != null)
            {
                Enabled = loaded.Enabled;
                Anchor = loaded.Anchor;
                Mode = loaded.Mode;
                NotificationDisplayTimeSeconds = loaded.NotificationDisplayTimeSeconds;
                InvertedAlignment = loaded.InvertedAlignment;
                // The '% 100' is to ensure proper migration of existing configs (avoids settings windows out of bounds elements)
                HorizontalOffset = loaded._horizontalOffset % 100;
                VerticalOffset = loaded._verticalOffset % 100;
                // Ensure Proper migration of old configs (avoids settings windows out of bounds elements)
                FontSize = Math.Max(loaded._fontSize % MaxFontSize, MinFontSize);
                FontBold = loaded.FontBold;
                TotalAmountEnabled = loaded.TotalAmountEnabled;
            }
        }

        public void ResetToDefaults()
        {
            var defaults = new ItemPickupNotifierConfig(_capi);
            Enabled = defaults.Enabled;
            Anchor = defaults.Anchor;
            Mode = defaults.Mode;
            NotificationDisplayTimeSeconds = defaults.NotificationDisplayTimeSeconds;
            InvertedAlignment = defaults.InvertedAlignment;
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
    }
}