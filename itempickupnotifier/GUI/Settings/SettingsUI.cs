using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ItemPickupNotifier.GUI
{

    public class SettingsUI : GuiDialog, IElement
    {
        public override string ToggleKeyCombinationCode { get; }

        private readonly string _dialogTitle;
        private readonly string _settingsUIId;
        private const int _width = 500;
        private const int _height = 425;
        private List<Section> _sections = new();
        private ElementBounds _nextSectionBounds;
        private readonly ActionConsumable _onSave;
        private readonly ActionConsumable _onCancel;



        public SettingsUI(string id, ICoreClientAPI capi, ActionConsumable onSave, ActionConsumable onReset) : base(capi)
        {
            _settingsUIId = id;
            _onSave = onSave;
            _onCancel = onReset;
            _dialogTitle = UITils.GetLangString(id, "global.settings-title");
            Init();
        }

        public Section Section(string id)
        {
            Section section = new(_settingsUIId, id, capi, GetNextSectionBounds());
            _sections.Add(section);
            return section;
        }

        private ElementBounds GetNextSectionBounds()
        {
            // Either we start a new section under the previous or initialize based on the settings container
            _nextSectionBounds = _nextSectionBounds?.BelowCopy()?? ElementBounds.FixedSize(_width * 0.85, 0);
            return _nextSectionBounds;
        }


        public void Init()
        {

            // Dialog base bound
            var dialogBounds = ElementStdBounds.AutosizedMainDialog;

            // Dialog background bounds
            var bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo(_settingsUIId + "-settings", dialogBounds)
                .AddDialogTitleBar(_dialogTitle, OnTitleBarClose)
                .AddShadedDialogBG(bgBounds)
                .BeginChildElements(bgBounds);

            var insetBounds = ElementBounds
                .Fixed(0,
                    GuiStyle.TitleBarHeight,
                    _width * 0.85,
                    _height * 0.9);

            var clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            var containerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            var scrollBarBounds = insetBounds.RightCopy().WithFixedWidth(_width * 0.05);
            scrollBarBounds.fixedPaddingX = GuiStyle.HalfPadding;

            SingleComposer
                .AddInset(insetBounds)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "scroll-settings")
                    .EndClip()
                .AddVerticalScrollbar(OnNewScrollbarValue, scrollBarBounds, "scroll-bar");

        }

        public GuiComposer Build(){
            var height = BuildSettingsSections();
            BuildButtons();
            // End BGBounds Child Elements and Compose
            SingleComposer.EndChildElements().EndChildElements().Compose();
            
            GuiElementContainer scrollSettings = SingleComposer.GetContainer("scroll-settings");
            SingleComposer.GetScrollbar("scroll-bar").SetHeights((float)(_height * 0.85f), height);
            return SingleComposer;
        }

        private float BuildSettingsSections()
        {
            var height = 0.0;
            GuiElementContainer scrollSettings = SingleComposer.GetContainer("scroll-settings");
            foreach (var section in _sections)
            {
                scrollSettings.Add(section.Build());
                height += section.Bounds.fixedHeight;
            }
            return (float)height;
        }

        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("scroll-settings").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void BuildButtons()
        {
            // Vars for improved readability and reuse in dynamic position calculations
            var buttonWidth = _width / 3;
            var buttonHeigth = _height * 0.05;

            var buttonBaseBounds = ElementBounds
                .Fixed(_width/2,
                    _height,
                    buttonWidth,
                    buttonHeigth);

            // Reset button Bounds 
            var cancelButtonBounds = buttonBaseBounds.FlatCopy().WithFixedOffset(-buttonWidth-2, 0);


            // Save button Bounds
            var saveButtonBounds = buttonBaseBounds.FlatCopy().WithFixedOffset(2, 0);

            SingleComposer
                .AddSmallButton(UITils.GetLangString(_settingsUIId, "global.cancel"), _onCancel, cancelButtonBounds)
                .AddSmallButton(UITils.GetLangString(_settingsUIId, "global.save"), _onSave, saveButtonBounds);
        }

        private void OnTitleBarClose()
        {
            if (IsOpened())
            {
                TryClose();
            }
        }


        public void RevertSettings()
        {
            foreach (var element in _sections)
            {
                if (element is IElement iElement) iElement.RevertSettings();
            }
        }
    }
}
