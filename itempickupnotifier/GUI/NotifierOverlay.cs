using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace itempickupnotifier.GUI
{

    public class NotifierOverlay : HudElement
    {
        private double showDuration = 4.0; // Duration in seconds to show notification
        private long showUntilMs;
        private List<ItemStack> itemStacks = new List<ItemStack>();


        public NotifierOverlay(ICoreClientAPI capi) : base(capi)
        {
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
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom);

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
                guiComposer.AddStaticText(itemStack.StackSize + "x " + itemStack.GetName(), CairoFont.WhiteSmallText(), textItemStackBounds);
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
    }
}
