using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using Newtonsoft.Json;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;

namespace AlliancesPlugin
{
    public class RabbitTest
    {
        public static class MQPluginPatch
        {
            internal static readonly MethodInfo HandleMessagePatch = typeof(MQPluginPatch).GetMethod(nameof(HandleMessage), BindingFlags.Static | BindingFlags.Public) ??
                                                                     throw new Exception("Failed to find patch method");

            private static Dictionary<string, Action<string>> Handlers = new Dictionary<string, Action<string>>();
            public static void Patch(PatchContext ctx)
            {
                var HandleMessageMethod = AlliancePlugin.MQ.GetType().GetMethod("MessageHandler", BindingFlags.Instance | BindingFlags.Public);
                if (HandleMessageMethod == null) return;

                ctx.GetPattern(HandleMessageMethod).Suffixes.Add(HandleMessagePatch);
                Handlers.Add("AllianceMessage", HandleAllianceChat);
            }

            public static void HandleAllianceChat(string MessageBody)
            {
               AllianceChat.ReceiveChatMessage(JsonConvert.DeserializeObject<AllianceChatMessage>(MessageBody));
            }

            public static void HandleMessage(string MessageType, string MessageBody)
            {
                if (Handlers.TryGetValue(MessageType, out var action))
                {
                    action.Invoke(MessageBody);
                }
            }
        }

        public class RabbitCommands : CommandModule
        {
            [Command("alliancerabbit", "test alliances connection")]
            [Permission(MyPromoteLevel.Admin)]
            public void MQTest()
            {
                if (!AlliancePlugin.MQPluginInstalled)
                {
                    Context.Respond("Plugin not installed");
                    return;
                }

                var input = JsonConvert.SerializeObject("Alliance Test Message");
                var methodInput = new object[] { "Alliance Test", input };
                AlliancePlugin.SendMessage?.Invoke(AlliancePlugin.MQ, methodInput);
            }
        }
    }
}
