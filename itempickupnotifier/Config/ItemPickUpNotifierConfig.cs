#nullable enable
using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.Config
{
    /// <summary>
    /// Configure the ItemPickupNotifier Overlay.
    /// </summary>
    public class ItemPickupNotifierConfig
    {

        private EnumDialogArea anchor = EnumDialogArea.RightBottom;
        public const string FileName = "itempickupnotifier.json";
        
        /// <summary>Overlay Anchor (Base position - based on EnumDialogArea)</summary>
        public string Anchor 
        { 
            get => anchor.ToString();
            set => anchor = Enum.TryParse<EnumDialogArea>(value, out var result) ? result : EnumDialogArea.RightBottom;
        }

        /// <summary>Horizontal offset (in pixels)</summary>
        public float HorizontalOffset { get; set; } = 0.0f;

        /// <summary>Vertical Offset (in pixels)</summary>
        public float VerticalOffset { get; set; } = 0.0f;

        public float FontSize { get; set; } = 16.0f;

        public bool TotalAmountEnabled = false;

        public EnumDialogArea GetOverlayAnchor()
        {
            return anchor;
        }

        public void Save(ICoreClientAPI capi)
        {
            var config = new ItemPickupNotifierConfig();
            capi.StoreModConfig(config, FileName);
        }

        public void Load(ICoreClientAPI capi)
        {
            var loaded = capi.LoadModConfig<ItemPickupNotifierConfig>(FileName);
            if (loaded != null)
            {
                Anchor = loaded.Anchor;
                HorizontalOffset = loaded.HorizontalOffset;
                VerticalOffset = loaded.VerticalOffset;
                FontSize = loaded.FontSize;
                TotalAmountEnabled = loaded.TotalAmountEnabled;
            }
        }

        public void ResetToDefaults()
        {
            var defaults = new ItemPickupNotifierConfig();
            Anchor = defaults.Anchor;
            HorizontalOffset = defaults.HorizontalOffset;
            VerticalOffset = defaults.VerticalOffset;
            FontSize = defaults.FontSize;
            TotalAmountEnabled = defaults.TotalAmountEnabled;
        }
    }
}