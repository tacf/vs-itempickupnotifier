using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace ItemPickupNotifier.GUI
{

    public class Section : GuiElementContainer
    {
        private const string modName = "itempickupnotifier";
        public double Width => _baseBounds.fixedWidth;

        // Internal reference to title in language files
        private string _titleLangKey;
        public string Title
        {
            get => Lang.Get(modName + ":section." + _titleLangKey + ".title");
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



        public Section(string titleLangKey, ICoreClientAPI capi, ElementBounds bounds) : base(capi, bounds)
        {
            _titleLangKey = titleLangKey;
            _baseBounds = bounds.FlatCopy();
            _api = capi;
            _container = new GuiElementContainer(_api, _baseBounds);
            GenerateTitle();
        }

        public static ElementBounds GetBaseBounds(double width)
        {
            return ElementBounds
                    .FixedOffseted(EnumDialogArea.CenterTop, 0, 0, ElementBounds.scaled(width * 0.9), _elementHeight)
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
            _baseBounds.fixedHeight = _elementHeight;
        }


        private GuiElement GenerateSettingLabel(string key)
        {
            return new GuiElementStaticText(_api, GetLangString(key), EnumTextOrientation.Left, _settingDescriptionBounds, _font);
        }

        public Section AddCheckbox(string descriptionLangKey, Action<bool> onToggled)
        {
            UpdateNextChildBounds();
            var cbSize = ElementBounds.scaled(10);
            var cbElement = new GuiElementSwitch(_api, onToggled, _settingElementBounds.FlatCopy().WithFixedSize(cbSize, cbSize));
            _container.Add(GenerateSettingLabel(descriptionLangKey));
            _container.Add(cbElement);
            return this;
        }

        public Section AddSlider(string descriptionLangKey, ActionConsumable<int> onNewSliderValue)
        {
            UpdateNextChildBounds();
            var sliderElement = new GuiElementSlider(_api, onNewSliderValue, _settingElementBounds);
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
            var dropDownElement = new GuiElementDropDown(_api,values, names, index, onSelectionChanged, _settingElementBounds, _font, false);
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
            var descLeftPadding = ElementBounds.scaled(Width*0.05);
            _settingDescriptionBounds = ElementBounds.Fixed(descLeftPadding, _currentYOffset, Width / 2 - descLeftPadding, _elementHeight);
            _settingElementBounds = ElementBounds.Fixed(Width / 2, _currentYOffset, Width / 2, _elementHeight);
            _baseBounds.fixedHeight += _elementHeight + _elementPadding;
        }

        private string GetLangString(string key)
        {
            return UITils.GetLangString(modName, "section." + _titleLangKey + "." + key);
        }
    }
}
