using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ItemPickupNotifier.GUI
{

    public class Section : GuiElementContainer, IElement
    {
        public double Width => _baseBounds.fixedWidth;

        // Internal reference to title in language files
        private readonly string _titleLangKey;
        private readonly string _settingsUIId;
        public string Title
        {
            get => GetLangString("title");
        }


        private double _currentYOffset = 0;
        private static double _elementHeight;
        private static double _elementPadding;

        private readonly ElementBounds _baseBounds;
        private ElementBounds _settingDescriptionBounds;
        private ElementBounds _settingElementBounds;
        private readonly GuiElementContainer _container;
        private readonly CairoFont _font = CairoFont.WhiteSmallText();
        private readonly ICoreClientAPI _api;



        public Section(string settingsId, string titleLangKey, ICoreClientAPI capi, ElementBounds bounds, double elementHeight = 25, double elementPadding = 10) 
            : base(capi, bounds.WithFixedHeight(_elementHeight + _elementPadding))
        {
            _titleLangKey = titleLangKey;
            _settingsUIId = settingsId;
            _elementHeight = elementHeight;
            _elementPadding = elementPadding;

            _baseBounds = bounds;
            _baseBounds.WithFixedHeight(_elementHeight + _elementPadding);

            Bounds = _baseBounds;
            _api = capi;
            _container = new GuiElementContainer(_api, _baseBounds);
            GenerateTitle();
        }
        private void GenerateTitle()
        {
            CairoFont font = _font.Clone();
            font.FontWeight = Cairo.FontWeight.Bold;
            font.WithFontSize(17f);
            ElementBounds titleBounds = ElementBounds.FixedSize(Width, _elementHeight);
            GuiElementStaticText titleElement = new GuiElementStaticText(_api, Title, EnumTextOrientation.Left, titleBounds, font);
            _container.Add(titleElement);
        }


        private GuiElement GenerateSettingLabel(string key)
        {
            GuiElementStaticText settingLabel = new GuiElementStaticText(_api, GetLangString(key), EnumTextOrientation.Left, _settingDescriptionBounds, _font);
            return settingLabel;
        }

        public Section AddSwitch(string descriptionLangKey, Action<bool> onToggled, bool toggled = false, bool persistState = true)
        {
            UpdateNextChildBounds();
            Switch cbElement = new Switch(_api, toggled, onToggled, _settingElementBounds, persistState: persistState);
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(cbElement);
            return this;
        }

        public Section AddSlider(string descriptionLangKey, ActionConsumable<int> onNewSliderValue, int defaultValue, int maxValue = 100, int minValue = 0, int step = 1, string unit = "")
        {
            UpdateNextChildBounds();
            Slider sliderElement = new Slider(_api,defaultValue, minValue, maxValue, step, unit, onNewSliderValue, _settingElementBounds);
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(sliderElement);
            return this;
        }

        public Section AddDropdown(string descriptionLangKey, SelectionChangedDelegate onSelectionChanged, string[] names, string[] values = null, string defaultName = null)
        {
            UpdateNextChildBounds();
            values ??= names;
            int index = (defaultName == null) ? 0 : names.IndexOf(defaultName);
            Dropdown dropDownElement = new Dropdown(_api, values, names, index, onSelectionChanged, _settingElementBounds, _font);
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(dropDownElement);
            return this;
        }

        public void UpdateChildren()
        {
            foreach(GuiElement element in _container.Elements)
            {
                element.InsideClipBounds = _container.InsideClipBounds;
            }
        }

        public GuiElement Build()
        {
            return _container;
        }

        private void UpdateNextChildBounds()
        {
            _currentYOffset += _elementHeight + _elementPadding;
            _baseBounds.fixedHeight += _elementHeight + _elementPadding;

            _settingDescriptionBounds = ElementBounds.Fixed(Width * 0.05, _currentYOffset, Width * 0.45, _elementHeight);
            _settingElementBounds = _settingDescriptionBounds.RightCopy().WithFixedWidth(Width * 0.45);

        }

        private string GetLangString(string key)
        {
            return UITils.GetLangString(_settingsUIId, "section." + _titleLangKey + "." + key);
        }

        public void RevertSettings()
        {
            foreach (GuiElement element in _container.Elements)
            {
                if (element is IElement iElement) iElement.RevertSettings();
            }
        }

        public void StoreCurrentValues()
        {
            foreach (GuiElement element in _container.Elements)
            {
                if (element is IElement iElement) iElement.StoreCurrentValues();
            }
        }
    }
}
