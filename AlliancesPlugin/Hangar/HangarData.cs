using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin
{
   public class HangarData
    {
        public int SlotsAmount = 1;
        public int SlotUpgradeNum = 0;
        public Guid allianceId;
        FileUtils utils = new FileUtils();
        public Dictionary<int, HangarItem> ItemsInHangar = new Dictionary<int, HangarItem>();
        public HangarLog GetHangarLog(Alliance alliance)
        {
           
            if (!Directory.Exists(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//");
            }

            if (!File.Exists(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json"))
            {
               HangarLog log = new HangarLog
                {
                    allianceId = alliance.AllianceId
                };
                utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
                return log;
            }
            return utils.ReadFromJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json");
        }
        public int getAvailableSlot()
        {
            for (int i = 1; i <= SlotsAmount; i++)
            {
                if (!ItemsInHangar.ContainsKey(i))
                {
                    return i;
                }
            }
            return 0;
        }
        public Boolean SaveGridToHangar(String gridName, ulong steamid, Alliance alliance, Vector3D position, MyFaction faction, List<MyCubeGrid> gridsToSave)
        {
            List<VRage.ModAPI.IMyEntity> inRange = new List<VRage.ModAPI.IMyEntity>();
            BoundingSphereD sphere = new BoundingSphereD(position, 15000);
            inRange = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            foreach (VRage.ModAPI.IMyEntity ent in inRange)
            {
                if (ent is MyCubeGrid grid)
                {
                    if (FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)) != null)
                    {
                        if (!MySession.Static.Factions.AreFactionsFriends(faction.FactionId, FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)).FactionId))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (ent is MyCharacter character)
                    {
                        if (MySession.Static.Factions.GetPlayerFaction(character.GetIdentity().IdentityId) != null)
                        {
                            if (!MySession.Static.Factions.AreFactionsFriends(faction.FactionId, MySession.Static.Factions.GetPlayerFaction(character.GetIdentity().IdentityId).FactionId))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            HangarLog log = GetHangarLog(alliance);
            HangarLogItem item = new HangarLogItem();
            item.action = "Saved";
            item.steamid = steamid;
            item.GridName = gridName;
            item.time = DateTime.Now;
            log.log.Add(item);
            utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
            GridManager.SaveGridNoDelete(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + gridName + ".xml"), gridName, false, true, gridsToSave);
            return true;
        }
        public Boolean LoadGridFromHangar(int slotNum, ulong steamid, Alliance alliance, MyIdentity identity, MyFaction faction)
        {
           
            List<VRage.ModAPI.IMyEntity> inRange = new List<VRage.ModAPI.IMyEntity>();
            BoundingSphereD sphere = new BoundingSphereD(ItemsInHangar[slotNum].position, 15000);
           inRange = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            foreach (VRage.ModAPI.IMyEntity ent in inRange)
            {
                if (ent is MyCubeGrid grid)
                {
                   if (FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)) != null)
                    {
                        if (!MySession.Static.Factions.AreFactionsFriends(faction.FactionId, FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)).FactionId))
                        {
                            return false;
                        }
                    }
                   else
                    {
                        return false;
                    }
                }
                else
                {
                    if (ent is MyCharacter character)
                    {
                        if (MySession.Static.Factions.GetPlayerFaction(character.GetIdentity().IdentityId) != null)
                        {
                            if (!MySession.Static.Factions.AreFactionsFriends(faction.FactionId, MySession.Static.Factions.GetPlayerFaction(character.GetIdentity().IdentityId).FactionId))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            if (GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml"), ItemsInHangar[slotNum].position, false, steamid))
                {
                HangarLog log = GetHangarLog(alliance);
                HangarLogItem item = new HangarLogItem();
                item.action = "Loaded";
                item.steamid = steamid;
                item.GridName = ItemsInHangar[slotNum].name;
                item.time = DateTime.Now;
                log.log.Add(item);
                utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
            }
            else
            {
                return false;
            }
                    //check for enemies
 


            return true;
        }
    }
}
