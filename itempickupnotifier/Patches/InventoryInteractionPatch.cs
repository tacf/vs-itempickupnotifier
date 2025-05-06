using HarmonyLib;
using ItemPickupNotifier;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace itempickupnotifier.Patches
{
    [HarmonyPatch(typeof(PlayerInventoryManager))]
    public static class InventoryInteractionPatch
    {
        [HarmonyPatch(nameof(PlayerInventoryManager.TryGiveItemstack))]
        [HarmonyPrefix]
        private static void OnTryGiveItemStack(ItemStack itemstack, out int __state)
        {
            __state = itemstack.StackSize;
        }

        [HarmonyPatch(nameof(PlayerInventoryManager.TryGiveItemstack))]
        [HarmonyPostfix]
        private static void OnPlayerReceiveItemStack(ItemStack itemstack, PlayerInventoryManager __instance, bool __result, int __state)
        {
            if (__result && __instance.player.Entity.Api.Side == EnumAppSide.Server)
            {
                ItemStack itemStack = itemstack.Clone();
                itemStack.StackSize = __state - itemstack.StackSize;
                ItempickupnotifierModSystem.NotifyPlayerItemStackReceived(__instance.player as IServerPlayer, itemStack);
            }
        }
    }
}


