using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            SetupDialog();
        }

        public void ShowNotification()
        {
            showUntilMs = capi.World.ElapsedMilliseconds + (long)(showDuration * 1000);

            if (!IsOpened())
            {
                SetupDialog();
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

        private void SetupDialog()
        {
            if (!itemStacks.Any()) return;

            double itemEntrySize = ElementBounds.scaled(30);
            double overlayWidth = ElementBounds.scaled(500);

            // Dialog base bound
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(ItempickupnotifierModSystem.Config.GetOverlayAnchor())
                .WithFixedOffset(ItempickupnotifierModSystem.Config.HorizontalOffset, ItempickupnotifierModSystem.Config.VerticalOffset)
                .WithFixedPadding(ElementBounds.scaled(5));

            // Background boundaries
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, overlayWidth, itemEntrySize * itemStacks.Count);

            var guiComposer = capi.Gui.CreateCompo("itemPickupNotifier", dialogBounds)
                .AddGameOverlay(bgBounds, new double[] { 0.0, 0.0, 0.0, 0.0 })
                .BeginChildElements();

            // Create stacked text elements
            double yOffset = itemEntrySize*(itemStacks.Count-1);
            
            foreach (var itemStack in itemStacks)
            {
                if (itemStack.ResolveBlockOrItem(capi.World))
                {
                    CompositeTexture texture = itemStack.Item != null ? itemStack.Item.FirstTexture : itemStack.Block.FirstTextureInventory;
                    if (texture != null)
                    {
                        ElementBounds textItemStackBounds = ElementBounds.Fixed(0, yOffset, overlayWidth, itemEntrySize);
                        var isComp = new ItemstackTextComponent(capi, itemStack, 35, 0, EnumFloat.Right);
                        isComp.offY -= 10;
                        guiComposer.AddRichtext(new RichTextComponentBase[] { isComp, new RichTextComponent(capi, Regex.Replace(itemStack.StackSize + "x " + itemStack.GetName(), "<.*?>", string.Empty), font) }, textItemStackBounds);
                        yOffset -= itemEntrySize;
                    }
                }
            }

            guiComposer.EndChildElements();
            SingleComposer = guiComposer.Compose();
        }

        public void AddItemStack(ItemStack itemStack)
        {
            var index = itemStacks.FindIndex(i => i.Id == itemStack.Id);
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

            // If it's already opened, rebuild the dialog
            if (IsOpened())
            {
                SetupDialog();
            }
        }

        protected virtual CairoFont InitFont()
        {
            const bool bold = true;

            return new CairoFont()
                .WithColor(new double[] { colour.R, colour.G, colour.B, colour.A })
                .WithFont(GuiStyle.StandardFontName)
                .WithOrientation(EnumTextOrientation.Right)
                .WithFontSize(ItempickupnotifierModSystem.Config.FontSize)
                .WithWeight(bold ? Cairo.FontWeight.Bold : Cairo.FontWeight.Normal)
                .WithStroke(new double[] { 0, 0, 0, 0.5 }, 2);
        }
    }
}
