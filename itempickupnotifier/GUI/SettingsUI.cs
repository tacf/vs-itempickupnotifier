using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ItemPickupNotifier.GUI
{

    public class SettingsUI : GuiDialog
    {
        public override string ToggleKeyCombinationCode { get; }

        private readonly string _dialogTitle;
        private readonly string _settingsUIId;
        private const int _width = 400;
        private const int _height = 500;
        private readonly List<Section> _sections = new();
        private ElementBounds _nextSectionBounds;
        private readonly ActionConsumable _onSave;
        private readonly ActionConsumable _onReset;



        public SettingsUI(string id, ICoreClientAPI capi, ActionConsumable onSave, ActionConsumable onReset) : base(capi)
        {
            _settingsUIId = id;
            _onSave = onSave;
            _onReset = onReset;
            _dialogTitle = UITils.GetLangString(id, "global.settings-title");
        }

        public Section Section(string id)
        {
            Section section = new(_settingsUIId, id, capi, GetNextSectionBounds());
            _sections.Add(section);
            return section;
        }

        private ElementBounds GetNextSectionBounds()
        {
            var baseBounds = GUI.Section.GetBaseBounds(_width);
            if (_nextSectionBounds == null)
            {
                _nextSectionBounds = baseBounds;
            }
            else
            {
                // Hack - possibly fixed if true that https://github.com/anegostudios/vsapi/issues/45
                var fY = _nextSectionBounds.fixedY + _nextSectionBounds.fixedOffsetY + _nextSectionBounds.fixedHeight + _nextSectionBounds.fixedPaddingY*4;
                _nextSectionBounds = GUI.Section.GetBaseBounds(_width, fY);
            }


            return _nextSectionBounds;
        }


        public void Build()
        {

            // Dialog base bound
            var dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);



            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo(_settingsUIId + "-settings", dialogBounds)
                .AddDialogTitleBar(_dialogTitle, OnTitleBarClose);

            BuildSettingsSections();
            BuildButtons();
            SingleComposer.EndChildElements().Compose();
        }

        private void BuildSettingsSections()
        {
            // Background boundaries
            var bgBounds = ElementBounds.FixedSize(
                    ElementBounds.scaled(_width),
                    ElementBounds.scaled(_height))
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            // Settings Inset Bounds
            var insetBounds = ElementBounds
                .FixedOffseted(EnumDialogArea.CenterTop,
                    0,
                    GuiStyle.TitleBarHeight,
                    ElementBounds.scaled(_width),
                    ElementBounds.scaled(_height * 0.9));

            SingleComposer
                .AddShadedDialogBG(bgBounds)
                .BeginChildElements(bgBounds)
                .AddInset(insetBounds)
                .BeginChildElements();
            foreach (var section in _sections)
            {
                SingleComposer.AddInteractiveElement(section.Build());
            }
            SingleComposer.EndChildElements();
        }

        private void BuildButtons()
        {
            // Vars for improved readability and reuse in dynamic position calculations
            var buttonWidth = ElementBounds.scaled(_width / 3);
            var buttonHeigth = ElementBounds.scaled(_height * 0.05);

            var buttonBaseBounds = ElementBounds
                .FixedOffseted(EnumDialogArea.CenterBottom, 0, ElementBounds.scaled(_height * 0.02), buttonWidth, buttonHeigth);


            // Reset button Bounds 
            var resetButtonBounds = buttonBaseBounds.FlatCopy()
                .WithFixedOffset(-(buttonWidth / 1.5), 0);

            // Save button Bounds
            var saveButtonBounds = buttonBaseBounds.FlatCopy()
                .WithFixedOffset(buttonWidth / 1.5, 0);

            SingleComposer
                .AddSmallButton("Reset Defaults", _onReset, resetButtonBounds)
                .AddSmallButton("Save", _onSave, saveButtonBounds);
        }

        private void OnTitleBarClose()
        {
            if (IsOpened())
            {
                TryClose();
            }
        }
    }
}
