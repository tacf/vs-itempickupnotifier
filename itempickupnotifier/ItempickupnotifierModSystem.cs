using System.Collections.Generic;
using HarmonyLib;
using ItemPickupNotifier.Config;
using ItemPickupNotifier.GUI;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace ItemPickupNotifier
{

    [ProtoContract]
    public class ItemStackReceivedPacket
    {
        [ProtoMember(1)]
        public string eventname;
        [ProtoMember(2)]
        public byte[] stackbytes;
    }

    [ProtoContract]
    public class RequestItemStackNotifyPacket
    {
        [ProtoMember(1)]
        public string code;
    }


    public class ItempickupnotifierModSystem : ModSystem
    {
        public static NotifierOverlay NotifierOverlay;
        public static ItemPickupNotifierConfig Config { get; private set; } = new ItemPickupNotifierConfig();
        private static readonly HashSet<string> activeReceivers = new HashSet<string>();

        private ICoreAPI api;
        private static ICoreServerAPI sapi;
        private ICoreClientAPI capi;
        public Harmony harmony;


        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll(); // Applies all harmony patches
            }
            api.Network
                .RegisterChannel("itempickupnotifier")
                .RegisterMessageType<ItemStackReceivedPacket>()
                .RegisterMessageType<RequestItemStackNotifyPacket>();
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            base.StartServerSide(sapi);
            sapi.Network.GetChannel("itempickupnotifier").SetMessageHandler<RequestItemStackNotifyPacket>(onItemStackNotifyRequest);
        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            base.StartClientSide(capi);

            capi.Network.GetChannel("itempickupnotifier").SetMessageHandler<ItemStackReceivedPacket>(onItemStackReceived);

            // Wait 200ms to ensure the channel is connected and request server to register player for notifications
            capi.Event.RegisterGameTickListener(onClientTick200ms, 200);

            Config = capi.LoadModConfig<ItemPickupNotifierConfig>(ItemPickupNotifierConfig.FileName) ?? new ItemPickupNotifierConfig();
            capi.StoreModConfig(Config, ItemPickupNotifierConfig.FileName);

            NotifierOverlay = new NotifierOverlay(capi);
        }


        private void onClientTick200ms(float dt)
        {
            capi.Network.GetChannel("itempickupnotifier").SendPacket(new RequestItemStackNotifyPacket()
            {
                code = "request"
            });
        }


        private void onItemStackNotifyRequest(IServerPlayer byPlayer, RequestItemStackNotifyPacket packet)
        {
            activeReceivers.Add(byPlayer.PlayerUID);
        }


        private void onItemStackReceived(ItemStackReceivedPacket packet)
        {
            //Console.WriteLine($"[{Mod.Info.ModID}] Received ItemStack: " + packet.eventname);
            var itemstack = new ItemStack(packet.stackbytes);
            itemstack.ResolveBlockOrItem(api.World);
            if (itemstack == null) return;

            NotifierOverlay.AddItemStack(itemstack);
            NotifierOverlay.ShowNotification();
        }

        public static void NotifyPlayerItemStackReceived(IServerPlayer plr, ItemStack itemStack)
        {
            if (activeReceivers.Contains(plr.PlayerUID))
            {
                sapi.Network.GetChannel("itempickupnotifier").SendPacket(new ItemStackReceivedPacket()
                {
                    eventname = "onitemcollected", 
                    stackbytes = itemStack.ToBytes()
                }, plr);
            }
        }
    }
}