using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using CrunchGroup.Handlers;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers.ChatManager;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Commands
{
    [PatchShim]
    public class ChatCommands : CommandModule
    {
        public static void Patch(PatchContext ctx)
        {

            Core.Session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += LoadLogin;
            Core.Session.Managers.GetManager<ChatManagerServer>().MessageProcessing += DoChatMessage;
            NexusHandler.NexusMessage += NexusMessage;
        }

        public static void DoChatMessage(TorchChatMessage msg, ref bool consumed)
        {

            if (msg.AuthorSteamId == null)
            {
                return;
            }

            if (msg.Channel == Sandbox.Game.Gui.ChatChannel.Private || msg.Channel == Sandbox.Game.Gui.ChatChannel.Faction)
            {
                return;
            }

            if (msg.Message.StartsWith("!"))
            {
                return;
            }

            if (InGroupChat.TryGetValue((ulong)msg.AuthorSteamId, out var inChat))
            {
                if (!inChat)
                {
                    return;
                }
                var group = GroupHandler.GetPlayersGroup((long)msg.AuthorSteamId);
                if (group == null)
                {
                    InGroupChat.Remove((ulong)msg.AuthorSteamId);
                    return;
                }
                else
                {

                }
            }
        }



        public static void LoadLogin(IPlayer player)
        {
            //load the player to see if they are in chat 
        }

        public static void NexusMessage(GroupEvent message)
        {
            if (message.EventType is "GroupChatMessage")
            {

            }
        }

        private static Dictionary<ulong, bool> InGroupChat = new Dictionary<ulong, bool>();


        [Command("gc", "toggle group chat")]
        [Permission(MyPromoteLevel.None)]
        public void Chat()
        {
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("Group not found.", $"{Core.PluginName}");
                InGroupChat.Remove(Context.Player.SteamUserId);
                return;
            }

            if (InGroupChat.ContainsKey(Context.Player.SteamUserId))
            {
                Context.Respond("Leaving group chat");
                InGroupChat[Context.Player.SteamUserId] = false;
                return;
            }
            Context.Respond("Entering group chat");
            InGroupChat[Context.Player.SteamUserId] = true;
        }
    }
}
