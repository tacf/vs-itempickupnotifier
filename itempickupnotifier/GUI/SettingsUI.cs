using System.Collections.Generic;
using Vintagestory.API.Client;

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



        public SettingsUI(string id, ICoreClientAPI capi) : base(capi)
        {
            _settingsUIId = id;
            _dialogTitle = UITils.GetLangString(id, "global.settings-title");
        }

        public Section Section(string id)
        {
            Section section = new(id, capi, GetNextSectionBounds());
            _sections.Add(section);
            return section;
        }

        private ElementBounds GetNextSectionBounds()
        {
            if (_nextSectionBounds == null)
            {
                _nextSectionBounds = GUI.Section.GetBaseBounds(_width);
            }
            else
            {
                _nextSectionBounds = UITils.Under(_nextSectionBounds);
                _nextSectionBounds.fixedHeight = GUI.Section.GetBaseBounds(_width).fixedHeight;
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
                .AddSmallButton("Reset Defaults", OnResetClicked, resetButtonBounds)
                .AddSmallButton("Save", OnSaveClicked, saveButtonBounds);
        }

        private bool OnSaveClicked()
        {
            return true;
        }

        private bool OnResetClicked()
        {
            return true;
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
