using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.NexusStuff;
using CrunchGroup.NexusStuff.V3;
using Sandbox.Game.SessionComponents;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game.Components;

namespace CrunchGroup.Patches
{
    [PatchShim]
    public static class SessionLoadPatch
    {
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(methodToPatch).Prefixes.Add(replaceWith);
        }
        internal static readonly MethodInfo methodToPatch =
            typeof(MySessionComponentBase).GetMethod("LoadData",
                BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method contract");

        internal static readonly MethodInfo replaceWith =
            typeof(SessionLoadPatch).GetMethod(nameof(LoadData), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");


        public static bool Loaded = false;

        public static void LoadData()
        {
            if (!Loaded)
            {
                Core.Log.Info("Registering MES API");
                Core.NexusGlobalAPI = new NexusGlobalAPI(SetupNetworking);
                Loaded = true;
            }
        }

        public static void SetupNetworking()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(4398, ReceiveData);
        }

        private static void ReceiveData(object obj)
        {
           NexusHandler.HandleNexusMessage(4398, (byte[])obj, 0, true);
        }
    }
}
