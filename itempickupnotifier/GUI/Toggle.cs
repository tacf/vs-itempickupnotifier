using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.GUI
{

    public class Switch : GuiElementSwitch, IElement
    {
        private readonly bool _defaultValue = false;
        private readonly Action<bool> _onToggled;

        public Switch(ICoreClientAPI capi, bool currentState, Action<bool> OnToggled, ElementBounds bounds, double size = 30, double padding = 4) : base(capi, OnToggled, bounds, size, padding)
        {
            _defaultValue = currentState;
            _onToggled = OnToggled;
            On = currentState;
        }

        public void RevertSettings()
        {
            On = _defaultValue;
            _onToggled(_defaultValue);
        }
    }
}