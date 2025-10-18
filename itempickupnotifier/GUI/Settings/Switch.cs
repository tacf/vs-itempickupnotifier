using System;
using Vintagestory.API.Client;

namespace ItemPickupNotifier.GUI
{

    public class Switch : GuiElementSwitch, IElement
    {
        private bool _initialValue = false;
        private bool _persistValue = false;
        private readonly Action<bool> _onToggled;

        public Switch(ICoreClientAPI capi, bool currentState, Action<bool> OnToggled, ElementBounds bounds, double size = 30, double padding = 4, bool persistState = true) : base(capi, OnToggled, bounds, size, padding)
        {
            _initialValue = currentState;
            _onToggled = OnToggled;
            _persistValue = persistState;
            On = currentState;
        }

        public void RevertSettings()
        {
            On = _initialValue;
            _onToggled(_initialValue);
        }

        public void StoreCurrentValues()
        {
            if (!_persistValue) return;
            _initialValue = On;
        }
    }
}