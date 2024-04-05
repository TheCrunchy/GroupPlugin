using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Scripts
{
    public class SargSetAllScripter : CommandModule
    {
        [Command("setallranks", "set all players ranks")]
        [Permission(MyPromoteLevel.Admin)]
        public void JoinGroup(int rankNum)
        {
            var manager = Core.Session.Managers.GetManager<CommandManager>();

            foreach (var player in MySession.Static.Players.GetAllPlayers())
            {
                manager?.HandleCommandFromServer($"admin setrank {player.SteamId} {rankNum}");
            }
        }
    }
}
