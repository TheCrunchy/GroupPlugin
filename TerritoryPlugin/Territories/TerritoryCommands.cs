using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Territory.Territory_Version_2.Interfaces;
using Territory.Territory_Version_2.PointOwners;
using Territory.Territory_Version_2.Models;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace Territory.Territory_Version_2
{
    [Category("territory")]
    public class TerritoryCommands : CommandModule
    {
        [Command("give", "give territory to faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void Give(string territoryName, string factionTag)
        {
            var territory = TerritoryPlugin.Territories.First(x => x.Value.Name == territoryName);
            if (territory.Value == null)
            {
                Context.Respond("Territory Not found.");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found.");
                return;
            }

            territory.Value.Owner = new FactionPointOwner() { FactionId = faction.FactionId };
            TerritoryPlugin.Territories[territory.Key] = territory.Value;
            TerritoryPlugin.utils.WriteToXmlFile<Models.Territory>(TerritoryPlugin.path + "//Territories//" + territory.Value.Name + ".xml", territory.Value);
        }

        [Command("reload", "reload territories")]
        [Permission(MyPromoteLevel.Admin)]
        public void Load()
        {
            TerritoryPlugin.LoadAllTerritories();
            Context.Respond(TerritoryPlugin.Territories.Count.ToString() + " loaded");
        }


        [Command("create", "create territory at current position")]
        [Permission(MyPromoteLevel.Admin)]
        public void Create(string name)
        {
            Models.Territory territory = new Models.Territory();
            territory.Position = Context.Player.GetPosition();
            territory.Name = name;
            territory.SecondaryLogics = new List<ISecondaryLogic>();
            territory.CapturePoints = new List<ICapLogic>();
            territory.WorldName = MyMultiplayer.Static.HostName;
            TerritoryPlugin.utils.WriteToJsonFile<Models.Territory>(TerritoryPlugin.path + "//Territories//" + territory.Name + ".json", territory);
            Context.Respond("Created");
        }

        [Command("addpoint", "add a point to a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddPoint(string name, string pointtype)
        {
            var territory = TerritoryPlugin.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
            if (territory == null)
            {
                Context.Respond($"{name} not found");
                return;
            }
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.Namespace == "AlliancesPlugin.Territory_Version_2.CapLogics" && t.Name.Contains("Logic")
                    select t;


            if (q.Any(x => x.Name == pointtype))
            {
                Type point = q.FirstOrDefault(x => x.Name == pointtype);
                
                var instance = Activator.CreateInstance(point);
                territory.CapturePoints.Add((ICapLogic)instance);
                Context.Respond("Added cap logic?");
                TerritoryPlugin.utils.WriteToJsonFile<Models.Territory>(TerritoryPlugin.path + "//Territories//" + territory.Name + ".json", territory);
            }
            else
            {
                Context.Respond("Point type not found, available are");
                foreach (var type in q)
                {
                    Context.Respond(type.Name);
                }
                return;
            }

        }

        [Command("addlogic", "add a point to a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddLogic(string name, string pointnameOrbase, string secondarylogic)
        {
            var territory = TerritoryPlugin.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
            if (territory == null)
            {
                Context.Respond($"{name} not found");
                return;
            }

            var foundpoint = territory.CapturePoints.FirstOrDefault(x => x.PointName == pointnameOrbase);

            if (foundpoint == null && pointnameOrbase != "base")
            {
                Context.Respond($"{pointnameOrbase} not found");
                return;
            }
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.Namespace == "AlliancesPlugin.Territory_Version_2.SecondaryLogics" && t.Name.Contains("Logic")
                    select t; 

            if (q.Any(x => x.Name == secondarylogic))
            {
                Type point = q.FirstOrDefault(x => x.Name == secondarylogic);

                var instance = Activator.CreateInstance(point);
                if (foundpoint != null)
                {
                    foundpoint.AddSecondaryLogic((ISecondaryLogic)instance);
                }
                else
                {
                    territory.SecondaryLogics.Add((ISecondaryLogic)instance);
                }
        
                Context.Respond("Added secondary logic?");
                TerritoryPlugin.utils.WriteToJsonFile<Models.Territory>(TerritoryPlugin.path + "//Territories//" + territory.Name + ".json", territory);
            }
            else
            {
                Context.Respond("Logic type not found, available are");
                foreach (var type in q)
                {
                    Context.Respond(type.Name);
                }
                return;
            }

        }
    }
}
