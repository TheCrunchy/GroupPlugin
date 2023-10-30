//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using AlliancesPlugin.Alliances;
//using AlliancesPlugin.Shipyard;
//using Newtonsoft.Json;
//using Sandbox.Game.World;
//using Torch.Commands;
//using Torch.Commands.Permissions;
//using Torch.Managers.PatchManager;
//using VRage.Game.ModAPI;
//using VRage.Library.Net;
//using VRageMath;

//namespace AlliancesPlugin
//{
//    public class MQPatching
//    {
//        public static class MQPluginPatch
//        {
//            internal static readonly MethodInfo HandleMessagePatch = typeof(MQPluginPatch).GetMethod(nameof(HandleMessage), BindingFlags.Static | BindingFlags.Public) ??
//                                                                     throw new Exception("Failed to find patch method");

//            private static Dictionary<string, Action<string>> Handlers = new Dictionary<string, Action<string>>();

//            public static string AllianceMessage = "AllianceMessage";
//            public static string AllianceSendToDiscord = "AllianceSendToDiscord";

//            internal static readonly MethodInfo getPCUPatch =
//                typeof(MQPluginPatch).GetMethod(nameof(ReturnPCU), BindingFlags.Static | BindingFlags.Public) ??
//                throw new Exception("Failed to find patch method");
//            public static void Patch(PatchContext ctx)
//            {
//                var HandleMessageMethod = TerritoryPlugin.MQ.GetType().GetMethod("MessageHandler", BindingFlags.Instance | BindingFlags.Public);
//                if (HandleMessageMethod == null) return;
//                TerritoryPlugin.Log.Info("3");
//                ctx.GetPattern(HandleMessageMethod).Suffixes.Add(HandleMessagePatch);

//                Handlers.Add(AllianceMessage, HandleAllianceChat);
//                Handlers.Add(AllianceSendToDiscord, SendToIngame);

//            }
//            public static void ReturnPCU(ref int __result)
//            {
//                //AlliancePlugin.Log.Info("Getting PCU");
//               // __result = 5;
//            }

//            public static void HandleAllianceChat(string MessageBody)
//            {
//                AllianceChat.ReceiveChatMessage(JsonConvert.DeserializeObject<AllianceChatMessage>(MessageBody));
//            }
//            public static void SendToIngame(string MessageBody)
//            {
//                var message = JsonConvert.DeserializeObject<AllianceSendToDiscord>(MessageBody);
//                if (message.SendToIngame)
//                {
//                    TerritoryPlugin.SendChatMessage(message.SenderPrefix, message.MessageText, 0L);
//                }
//            }

//            public static void HandleMessage(string MessageType, string MessageBody)
//            {
//                if (Handlers.TryGetValue(MessageType, out var action))
//                {
//                    action.Invoke(MessageBody);
//                }
//            }
//        }

//        public class RabbitCommands : CommandModule
//        {
//            [Command("alliancerabbit", "test alliances connection")]
//            [Permission(MyPromoteLevel.Admin)]
//            public void MQTest()
//            {
//                if (!TerritoryPlugin.MQPluginInstalled)
//                {
//                    Context.Respond("Plugin not installed");
//                    return;
//                }

//                var input = JsonConvert.SerializeObject("Alliance Test Message");
//                var methodInput = new object[] { "Alliance Test", input };
//                TerritoryPlugin.SendMessage?.Invoke(TerritoryPlugin.MQ, methodInput);
//            }
//        }
//    }
//}
