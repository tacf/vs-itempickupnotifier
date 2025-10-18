using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ItemPickupNotifier.GUI
{

    public class SettingsUI : GuiDialog, IElement
    {
        public override string ToggleKeyCombinationCode { get; }

        private readonly string _dialogTitle;
        private readonly string _settingsUIId;
        private const int _width = 425;
        private const int _height = 500;
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


        public void Build()
        {

            // Dialog base bound
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog;

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo(_settingsUIId + "-settings", dialogBounds)
                .AddDialogTitleBar(_dialogTitle, CloseWithoutSaving)
                .AddDialogBG(bgBounds)
                .BeginChildElements(bgBounds);

            double scrollBarWidth = _width * 0.05;
            double scrollAreaHeight = _height * 0.7;
            
            ElementBounds insetBounds = ElementBounds
                .FixedSize(_width - scrollBarWidth, scrollAreaHeight)
                .WithAlignment(EnumDialogArea.CenterTop)
                .WithFixedOffset(-scrollBarWidth / 2 - GuiStyle.HalfPadding, GuiStyle.TitleBarHeight);
            
            ElementBounds scrollBarBounds = ElementBounds.FixedSize(scrollBarWidth, scrollAreaHeight)
                .RightOf(insetBounds).WithFixedOffset(GuiStyle.ElementToDialogPadding + GuiStyle.HalfPadding, GuiStyle.TitleBarHeight);
            GuiElementScrollbar scrollBar = new GuiElementScrollbar(capi, OnNewScrollbarValue, scrollBarBounds);

            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = clipBounds.ForkChild();

            GuiElementContainer scrollSettings = new GuiElementContainer(capi, containerBounds);
            scrollSettings.InsideClipBounds = clipBounds;
            foreach (Section section in _sections)
            {
                GuiElement sectionElement = section.Build();
                sectionElement.InsideClipBounds = clipBounds;
                section.UpdateChildren();
                scrollSettings.Add(sectionElement);
            }
            

            SingleComposer
                .AddInset(insetBounds)
                .BeginClip(clipBounds)
                .AddInteractiveElement(scrollSettings, "scroll-settings")
                .EndClip()
                .AddInteractiveElement(scrollBar, "scroll-bar");
            
            // Vars for improved readability and reuse in dynamic position calculations
            int buttonWidth = _width / 3;
            double buttonHeigth = _height * 0.05;

            ElementBounds buttonBaseBounds = ElementBounds.FixedSize(buttonWidth, buttonHeigth)
                .FixedUnder(insetBounds, GuiStyle.ElementToDialogPadding)
                .WithAlignment(EnumDialogArea.CenterFixed);

            // Reset button Bounds 
            ElementBounds cancelButtonBounds = buttonBaseBounds.FlatCopy().WithFixedOffset(-buttonWidth/2 - GuiStyle.HalfPadding, 0);


            // Save button Bounds
            ElementBounds saveButtonBounds = buttonBaseBounds.FlatCopy().WithFixedOffset(buttonWidth/2+ GuiStyle.HalfPadding, 0);


            SingleComposer
                .AddSmallButton(UITils.GetLangString(_settingsUIId, "global.cancel"), _onCancel, cancelButtonBounds, key: "cancel-button")
                .AddSmallButton(UITils.GetLangString(_settingsUIId, "global.save"), _onSave, saveButtonBounds, key: "save-button");
            // End BGBounds Child Elements and Compose
            SingleComposer.EndChildElements().Compose();
            
            scrollBar.SetHeights((float)scrollAreaHeight, scrollSettings.Bounds.OuterHeightInt);
            
        }
        

        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("scroll-settings").Bounds;
            bounds.fixedY = 5 - value;
            SingleComposer.ReCompose();
        }

        public void CloseWithoutSaving()
        {
            if (!IsOpened()) return;
            RevertSettings(); // Need to update the values so we don't get wrong on hover
            TryClose();
        }

        public override bool TryOpen()
        {
            SingleComposer.GetContainer("scroll-settings").Bounds.fixedY = 0;
            SingleComposer.GetScrollbar("scroll-bar").CurrentYPosition = 0;
            SingleComposer.ReCompose();
            StoreCurrentValues();
            return base.TryOpen();
        }
        
        public override bool OnEscapePressed()
        {
            RevertSettings();
            SingleComposer.ReCompose();
            return base.OnEscapePressed();
        }

        public override bool TryClose()
        {
            SingleComposer.ReCompose();
            return base.TryClose();
        }

        public void RevertSettings()
        {
            foreach (Section element in _sections)
            {
                element.RevertSettings();
            }
        }

        public void StoreCurrentValues()
        {
            foreach (Section element in _sections)
            {
                element.StoreCurrentValues();
            }
        }
    }
}
