using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CrunchGroup.Handlers;
using CrunchGroup.Territories.Interfaces;
using CrunchGroup.Territories.Models;
using CrunchGroup.Territories.PointOwners;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

namespace CrunchGroup.Commands
{
    [Category("territory")]
    public class TerritoryCommands : CommandModule
    {
        [Command("give", "give territory to faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void GiveFac()
        {
            Context.Respond("Use !territory givefac or !territory givegroup");
        }

        [Command("givefac", "give territory to faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void GiveFac(string territoryName, string factionTag)
        {
            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == territoryName);
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

            GiveToOwner(territory.Value, new FactionPointOwner() { FactionId = faction.FactionId });
            Context.Respond("Giving ownership.");
        }
        [Command("givegroup", "give territory to faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void GiveGroup(string territoryName, string groupName)
        {
            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == territoryName);
            if (territory.Value == null)
            {
                Context.Respond("Territory Not found.");
                return;
            }

            var group = GroupHandler.GetGroupByTag(groupName);
            if (group == null)
            {
                Context.Respond("Group not found.");
                return;
            }

            GiveToOwner(territory.Value, new GroupPointOwner() { GroupId = group.GroupId });
            Context.Respond("Giving ownership.");
        }

        public void GiveToOwner(Territory territory, IPointOwner owner)
        {
            territory.Owner = owner;
            foreach (var cap in territory.CapturePoints)
            {
                cap.PointOwner = owner;
            }
            Core.Territories[territory.Id] = territory;
            
            Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
        }

        [Command("reload", "reload territories")]
        [Permission(MyPromoteLevel.Admin)]
        public void Load()
        {
            Core.LoadAllTerritories();
            Context.Respond(Core.Territories.Count.ToString() + " loaded");
        }


        [Command("create", "create territory at current position")]
        [Permission(MyPromoteLevel.Admin)]
        public void Create(string name)
        {
            Territories.Models.Territory territory = new Territories.Models.Territory();
            territory.Position = Context.Player.GetPosition();
            territory.Name = name;
            territory.SecondaryLogics = new List<ISecondaryLogic>();
            territory.CapturePoints = new List<ICapLogic>();
            territory.WorldName = MyMultiplayer.Static.HostName;
            Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            Context.Respond("Created");
        }
        [Command("list", "list all valid names from scripts")]
        [Permission(MyPromoteLevel.Admin)]
        public void List()
        {
            var configs = new List<Type>();
            var configs2 = new List<Type>();

            configs.AddRange(from t in Core.myAssemblies.Select(x => x)
                    .SelectMany(x => x.GetTypes())
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);
            configs.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);

            configs2.AddRange(from t in Core.myAssemblies.Select(x => x)
                    .SelectMany(x => x.GetTypes())
                              where t.IsClass && t.GetInterfaces().Contains(typeof(ISecondaryLogic))
                              select t);
            configs2.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
                              where t.IsClass && t.GetInterfaces().Contains(typeof(ISecondaryLogic))
                              select t);

            foreach (var config in configs)
            {
                Context.Respond($"CapLogic {config.Name}");
            }

            foreach (var config in configs2)
            {
                Context.Respond($"SecondaryLogic {config.Name}");
            }


        }
        [Command("addpoint", "add a point to a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddPoint(string name, string pointtype)
        {
            var configs = new List<Type>();


            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
            if (territory == null)
            {
                Context.Respond($"{name} not found");
                return;
            }
            configs.AddRange(from t in Core.myAssemblies.Select(x => x)
                    .SelectMany(x => x.GetTypes())
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);
            configs.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);

            if (configs.Any(x => x.Name == pointtype))
            {
                Type point = configs.FirstOrDefault(x => x.Name == pointtype);

                var instance = Activator.CreateInstance(point);
                territory.CapturePoints.Add((ICapLogic)instance);
                Context.Respond("Added cap logic?");
                Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            }
            else
            {
                Context.Respond("Point type not found, available are");
                foreach (var type in configs)
                {
                    Context.Respond(type.Name);
                }
                return;
            }

        }
        public static Vector3D GetRandomPosition(Vector3D GPSofPoint, int maxDistance)
        {
            // Generate random angles for spherical coordinates
            double theta = Core.rand.NextDouble() * 2 * Math.PI; // Azimuthal angle
            double phi = Core.rand.NextDouble() * Math.PI; // Polar angle

            // Generate random direction (positive or negative) for each coordinate
            int xDirection = Core.rand.Next(0, 2) == 0 ? -1 : 1;
            int yDirection = Core.rand.Next(0, 2) == 0 ? -1 : 1;
            int zDirection = Core.rand.Next(0, 2) == 0 ? -1 : 1;

            // Convert spherical coordinates to Cartesian coordinates
            double x = GPSofPoint.X + xDirection * maxDistance * Math.Sin(phi) * Math.Cos(theta);
            double y = GPSofPoint.Y + yDirection * maxDistance * Math.Sin(phi) * Math.Sin(theta);
            double z = GPSofPoint.Z + zDirection * maxDistance * Math.Cos(phi);

            return new Vector3D(x, y, z);
        }

        [Command("generatepoints", "generate capture points for a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddPoint(string name, string pointtype, int amount, int expectedMaxDistance)
        {
            var configs = new List<Type>();
            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
            if (territory == null)
            {
                Context.Respond($"{name} not found");
                return;
            }
            configs.AddRange(from t in Core.myAssemblies.Select(x => x)
                    .SelectMany(x => x.GetTypes())
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);
            configs.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
                             where t.IsClass && t.GetInterfaces().Contains(typeof(ICapLogic))
                             select t);

            if (configs.Any(x => x.Name == pointtype))
            {
                Type point = configs.FirstOrDefault(x => x.Name == pointtype);
                var count = territory.CapturePoints.Count;
                for (int i = count; i < count + amount; i++)
                {
                    //add a random position
                    var position = GetRandomPosition(Context.Player.GetPosition(), expectedMaxDistance);
                    //while (MyGravityProviderSystem.IsPositionInNaturalGravity(position))
                    //{
                    //    expectedMaxDistance *= 2;
                    //    position = GetRandomPosition(Context.Player.GetPosition(), expectedMaxDistance);
                    //}
                    if (MyGravityProviderSystem.IsPositionInNaturalGravity(position))
                    {
                        Context.Respond("Generated position in natural gravity, skipping.");
                        continue;
                    }

                    ICapLogic instance = (ICapLogic)Activator.CreateInstance(point);
                    instance.SetPosition(position);
                    instance.PointName = $"{i}";
                    territory.CapturePoints.Add(instance);
                    Context.Respond($"Added cap logic {i}");
                }
                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                foreach (var cap in territory.CapturePoints)
                {
                    MyGps gpsRef = new MyGps();
                    gpsRef.Coords = cap.GetPointsLocationIfSet();
                    gpsRef.Name = $"{territory.Name} Cap Point: {cap.PointName}";
                    gpsRef.GPSColor = Color.Lime;
                    gpsRef.ShowOnHud = true;
                    gpsRef.AlwaysVisible = true;
                    gpsRef.DiscardAt = new TimeSpan?();
                    gpscol.SendAddGpsRequest(Context.Player.IdentityId, ref gpsRef);

                }
                Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            }
            else
            {
                Context.Respond("Point type not found, available are");
                foreach (var type in configs)
                {
                    Context.Respond(type.Name);
                }
                return;
            }

        }

        [Command("getpoints", "get capture point gpses for a territory")]
        [Permission(MyPromoteLevel.None)]
        public void GetPoints(string name)
        {
            var configs = new List<Type>();
            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
            if (territory == null)
            {
                Context.Respond($"{name} not found");
                return;
            }
            MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

            foreach (var point in territory.CapturePoints)
            {
                MyGps gpsRef = new MyGps();
                gpsRef.Coords = point.GetPointsLocationIfSet();
                gpsRef.Name = $"{territory.Name} Cap Point: {point.PointName}";
                gpsRef.GPSColor = Color.Lime;
                gpsRef.ShowOnHud = true;
                gpsRef.AlwaysVisible = true;
                gpsRef.DiscardAt = new TimeSpan?();
                gpscol.SendAddGpsRequest(Context.Player.IdentityId, ref gpsRef);

            }
            Context.Respond("Sent GPS");
        }


        [Command("compile", "recompile, do not run on live server")]
        [Permission(MyPromoteLevel.Admin)]
        public void Recompile()
        {
            try
            {
                Compiler.Compile($"{Core.path}/Scripts/");
                Core.LoadAllTerritories();
                Core.LoadConfig();
                Context.Respond("Compiling done, dont do this on live server.");
            }
            catch (Exception e)
            {
                Context.Respond("Territory not found");
                return;
            }

        }

        [Command("reset", "reset ownership of a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddPoint(string name)
        {
            try
            {
                var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == name).Value ?? null;
                if (territory == null)
                {
                    Context.Respond("Territory not found");
                    return;
                }

                territory.Owner = null;
                foreach (var point in territory.CapturePoints)
                {
                    point.PointOwner = null;
                }

                Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            }
            catch (Exception e)
            {
                Context.Respond("Territory not found");
                return;
            }

        }

        [Command("resetall", "reset ownership of all territories")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddPoint()
        {
            foreach (var territory in Core.Territories.Values)
            {
                territory.Owner = null;
                foreach (var point in territory.CapturePoints)
                {
                    point.PointOwner = null;
                }

                Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            }
        }


        [Command("addlogic", "add a point to a territory")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddLogic(string name, string pointnameOrbase, string secondarylogic)
        {
            var configs2 = new List<Type>();
            var territory = Core.Territories.FirstOrDefault(x => x.Value.Name == name).Value;
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
            configs2.AddRange(from t in Core.myAssemblies.Select(x => x)
                    .SelectMany(x => x.GetTypes())
                              where t.IsClass && t.GetInterfaces().Contains(typeof(ISecondaryLogic))
                              select t);
            configs2.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
                              where t.IsClass && t.GetInterfaces().Contains(typeof(ISecondaryLogic))
                              select t);

            if (configs2.Any(x => x.Name == secondarylogic))
            {
                Type point = configs2.FirstOrDefault(x => x.Name == secondarylogic);

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
                Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + territory.Name + ".json", territory);
            }
            else
            {
                Context.Respond("Logic type not found, available are");
                foreach (var type in configs2)
                {
                    Context.Respond(type.Name);
                }
                return;
            }

        }
    }
}
