using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ItemPickupNotifier.GUI
{

    public class Slider :  GuiElementSlider, IElement
    {
        private readonly int _defaultValue = 0;
        private readonly ActionConsumable<int> _onNewSliderValue;

        public Slider(ICoreClientAPI capi, int defaultValue, int minValue, int maxValue, int step, string unit, ActionConsumable<int> onNewSliderValue, ElementBounds bounds) : base(capi, onNewSliderValue, bounds)
        {
            _defaultValue = defaultValue;
            _onNewSliderValue = onNewSliderValue;
            SetValues(defaultValue, minValue, maxValue, step, unit);
        }

        public void RevertSettings()
        {
            SetValue(_defaultValue);
            _onNewSliderValue(_defaultValue);
        }
    }
}