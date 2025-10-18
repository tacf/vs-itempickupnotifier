using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ItemPickupNotifier.GUI
{

    public class Slider :  GuiElementSlider, IElement
    {
        private int _initialValue = 0;
        private readonly ActionConsumable<int> _onNewSliderValue;

        public Slider(ICoreClientAPI capi, int initialValue, int minValue, int maxValue, int step, string unit, ActionConsumable<int> onNewSliderValue, ElementBounds bounds) : base(capi, onNewSliderValue, bounds)
        {
            _initialValue = initialValue;
            _onNewSliderValue = onNewSliderValue;
            SetValues(initialValue, minValue, maxValue, step, unit);
        }

        public void RevertSettings()
        {
            SetValue(_initialValue);
            _onNewSliderValue(_initialValue);
        }

        public void StoreCurrentValues()
        {
            _initialValue = GetValue();
        }
    }
}