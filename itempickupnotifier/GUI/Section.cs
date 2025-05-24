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
        private static readonly double _elementHeight = ElementBounds.scaled(25);
        private static readonly double _elementPadding = ElementBounds.scaled(5);

        private readonly ElementBounds _baseBounds;
        private ElementBounds _settingDescriptionBounds;
        private ElementBounds _settingElementBounds;
        private readonly GuiElementContainer _container;
        private readonly CairoFont _font = CairoFont.WhiteSmallText();
        private readonly ICoreClientAPI _api;



        public Section(string settingsId, string titleLangKey, ICoreClientAPI capi, ElementBounds bounds) : base(capi, bounds)
        {
            _titleLangKey = titleLangKey;
            _settingsUIId = settingsId;
            _baseBounds = bounds;
            _api = capi;
            _container = new GuiElementContainer(_api, _baseBounds);
            GenerateTitle();
        }

        public static ElementBounds GetBaseBounds(double width, double yOffset = 0)
        {
            return ElementBounds
                    .FixedOffseted(EnumDialogArea.CenterTop, 0, yOffset, ElementBounds.scaled(width * 0.9), _elementHeight + _elementPadding)
                    .WithFixedPadding(ElementBounds.scaled(5));
        }

        private void GenerateTitle()
        {
            var font = _font.Clone();
            font.FontWeight = Cairo.FontWeight.Bold;
            font.WithFontSize(17f);
            var titleBounds = ElementBounds.Fixed(0, 0, Width, _elementHeight);
            var titleElement = new GuiElementStaticText(_api, Title, EnumTextOrientation.Left, titleBounds, font);
            _container.Add(titleElement);
        }


        private GuiElement GenerateSettingLabel(string key)
        {
            return new GuiElementStaticText(_api, GetLangString(key), EnumTextOrientation.Left, _settingDescriptionBounds, _font);
        }

        public Section AddSwitch(string descriptionLangKey, Action<bool> onToggled, bool toggled = false)
        {
            UpdateNextChildBounds();
            var cbSize = ElementBounds.scaled(10);
            var cbElement = new Switch(_api, toggled, onToggled, _settingElementBounds.FlatCopy().WithFixedSize(cbSize, cbSize));
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(cbElement);
            return this;
        }

        public Section AddSlider(string descriptionLangKey, ActionConsumable<int> onNewSliderValue, int defaultValue, int maxValue = 100, int minValue = 0, int step = 1, string unit = "")
        {
            UpdateNextChildBounds();
            var sliderElement = new Slider(_api,defaultValue, minValue, maxValue, step, unit, onNewSliderValue, _settingElementBounds);
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(sliderElement);
            return this;
        }

        public Section AddDropdown(string descriptionLangKey, SelectionChangedDelegate onSelectionChanged, string[] names, string[] values = null, string defaultName = null)
        {
            UpdateNextChildBounds();
            values ??= names;
            var bounds = ElementBounds.Empty;
            var index = (defaultName == null) ? 0 : names.IndexOf(defaultName);
            var dropDownElement = new Dropdown(_api, values, names, index, onSelectionChanged, _settingElementBounds, _font, false);
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(dropDownElement);
            return this;
        }


        public GuiElement Build()
        {
            return _container;
        }

        private void UpdateNextChildBounds()
        {
            _currentYOffset += _elementHeight + _elementPadding;
            var descLeftPadding = ElementBounds.scaled(Width * 0.05);
            _settingDescriptionBounds = ElementBounds.Fixed(descLeftPadding, _currentYOffset, Width / 2 - descLeftPadding, _elementHeight);
            _settingElementBounds = ElementBounds.Fixed(Width / 2, _currentYOffset, Width / 2, _elementHeight);
            _baseBounds.fixedHeight += _elementHeight;
            api.Logger.Debug("fixedY: {0}, fixedHeight: {1}", _baseBounds.fixedY, _baseBounds.fixedHeight);
        }

        private string GetLangString(string key)
        {
            return UITils.GetLangString(_settingsUIId, "section." + _titleLangKey + "." + key);
        }

        public void RevertSettings()
        {
            foreach (var element in _container.Elements)
            {
                if (element is IElement iElement) iElement.RevertSettings();
            }
        }
    }
}
