using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ItemPickupNotifier.GUI
{

    public class NotifierOverlay : HudElement
    {
        private double showDuration = 4.0; // Duration in seconds to show notification
        private long showUntilMs;
        private List<ItemStack> itemStacks = new List<ItemStack>();
        private CairoFont font;
        private readonly Vec4f colour = new(0.91f, 0.87f, 0.81f, 1);


        public NotifierOverlay(ICoreClientAPI capi) : base(capi)
        {
            font = InitFont();
            BuildDialog();
        }

        public void ShowNotification()
        {
            showUntilMs = capi.World.ElapsedMilliseconds + (long)(showDuration * 1000);

            if (!IsOpened())
            {
                TryOpen(withFocus: false);
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);

            if (IsOpened() && capi.World.ElapsedMilliseconds > showUntilMs)
            {
                TryClose();
                itemStacks.Clear();
            }
        }


        private void BuildDialog()
        {
            /*
             * ELementBounds are essentially a parented 2D rectangle which are used to determine the positions of UI elements.
             * In this case, we're using an autosized dialog that is centered in the center middle of the screen.
             */
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(ItempickupnotifierModSystem.Config.GetOverlayAnchor()).WithFixedOffset(ItempickupnotifierModSystem.Config.HorizontalOffset, ItempickupnotifierModSystem.Config.VerticalOffset);

            // Create a container for all item stack texts
            ElementBounds containerBounds = ElementBounds.Fixed(0, 0, 300, 300).WithFixedPadding(GuiStyle.ElementToDialogPadding).WithAlignment(EnumDialogArea.LeftBottom);

            // Background boundaries
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(containerBounds);

            var guiComposer = capi.Gui.CreateCompo("itemPickupNotifier", dialogBounds)
                .AddGameOverlay(bgBounds, new double[] { 0.0, 0.0, 0.0, 0.0 })
                .BeginChildElements(containerBounds);

            // Create stacked text elements
            double yOffset = 200;
            foreach (var itemStack in itemStacks)
            {
                ElementBounds textItemStackBounds = ElementBounds.Fixed(0, yOffset, 300, 50);
                guiComposer.AddStaticText(itemStack.StackSize + "x " + itemStack.GetName(), font, textItemStackBounds);
                yOffset -= ElementBounds.scaled(20); // Move down by the height of each text element
            }

            guiComposer.EndChildElements();
            SingleComposer = guiComposer.Compose();
        }

        public void AddItemStack(ItemStack itemStack)
        {
            var index = itemStacks.FindIndex(i => i.GetName() == itemStack.GetName());
            if (index >= 0)
            {
                // Refresh current value and push it to the top of the list
                itemStacks.Add(itemStack);
                itemStacks.Last().StackSize += itemStacks[index].StackSize;
                itemStacks.RemoveAt(index);
            }
            else
            {
                itemStacks.Add(itemStack);
            }

            // Rebuild Dialog and Show
            BuildDialog();
            ShowNotification();
        }

        protected virtual CairoFont InitFont()
        {
            const bool bold = true;

            return new CairoFont()
                .WithColor(new double[] { colour.R, colour.G, colour.B, colour.A })
                .WithFont(GuiStyle.StandardFontName)
                .WithFontSize(ItempickupnotifierModSystem.Config.FontSize)
                .WithWeight(bold ? Cairo.FontWeight.Bold : Cairo.FontWeight.Normal)
                .WithStroke(new double[] { 0, 0, 0, 0.5 }, 2);
        }
    }
}
