using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.GUI
{

    public class Dropdown : GuiElementDropDown, IElement
    {
        private int _initialValue = 0;
        private readonly SelectionChangedDelegate _onSelectionChanged;
        public Dropdown(ICoreClientAPI capi, string[] values, string[] names, int initialValue, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, CairoFont font, bool multiSelect = false) : base(capi, values, names, initialValue, onSelectionChanged, bounds, font, multiSelect)
        {
            _initialValue = initialValue;
            _onSelectionChanged = onSelectionChanged;
        }

        public void RevertSettings()
        {
            SetSelectedIndex(_initialValue);
            _onSelectionChanged(SelectedValue, true);
        }

        public void StoreCurrentValues()
        {
            _initialValue = SelectedIndices[0];
        }
    }
}