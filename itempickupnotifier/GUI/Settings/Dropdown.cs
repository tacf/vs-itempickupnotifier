using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.GUI
{

    public class Dropdown : GuiElementDropDown, IElement
    {
        private readonly int _selectedIndex = 0;
        private readonly SelectionChangedDelegate _onSelectionChanged;
        public Dropdown(ICoreClientAPI capi, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, CairoFont font, bool multiSelect = false) : base(capi, values, names, selectedIndex, onSelectionChanged, bounds, font, multiSelect)
        {
            _selectedIndex = selectedIndex;
            _onSelectionChanged = onSelectionChanged;
        }

        public void RevertSettings()
        {
            SetSelectedIndex(_selectedIndex);
            _onSelectionChanged(SelectedValue, true);
        }
    }
}