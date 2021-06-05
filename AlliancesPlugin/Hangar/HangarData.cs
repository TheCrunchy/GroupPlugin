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
using VRage.Game;
using VRageMath;
using AlliancesPlugin.Alliances;
namespace AlliancesPlugin.Hangar
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
        public void SaveHangar(Alliance alliance)
        {
            utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
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

        public Boolean CheckCharacters(Vector3 Position, long PlayerIdentityId)
        {


            MyFaction PlayersFaction = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);

            foreach (MyCharacter Player in MyEntities.GetEntities().OfType<MyCharacter>())
            {
                if (Player == null || Player.MarkedForClose)
                    continue;

                long PlayerID = Player.GetPlayerIdentityId();
                if (PlayerID == 0L || PlayerID == PlayerIdentityId)
                    continue;


                MyFaction CheckFaction = MySession.Static.Factions.GetPlayerFaction(PlayerID);
                if (PlayersFaction != null && CheckFaction != null)
                {
                    if (PlayersFaction.FactionId == CheckFaction.FactionId)
                        continue;
                   
                    MyRelationsBetweenFactions Relation = MySession.Static.Factions.GetRelationBetweenFactions(PlayersFaction.FactionId, CheckFaction.FactionId).Item1;
                    if (Relation == MyRelationsBetweenFactions.Neutral || Relation == MyRelationsBetweenFactions.Friends)
                        continue;
                }

                if (Vector3D.Distance(Position, Player.PositionComp.GetPosition()) <= 15000)
                {
                    return false;
                }
            }
            return true;
        }
        public Boolean CheckGrids(Vector3 Position, long PlayerIdentityId)
        {
            MyFaction PlayersFaction = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);
            BoundingSphereD sphere = new BoundingSphereD(Position, 15000);
            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                if (FacUtils.IsOwnerOrFactionOwned(grid, PlayerIdentityId, true))
                    continue;

                if (grid.Projector != null)
                    continue;

                MyFaction CheckFaction = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner(grid));
                if (PlayersFaction != null && CheckFaction != null)
                {
                    MyRelationsBetweenFactions Relation = MySession.Static.Factions.GetRelationBetweenFactions(PlayersFaction.FactionId, CheckFaction.FactionId).Item1;
                    if (Relation == MyRelationsBetweenFactions.Neutral || Relation == MyRelationsBetweenFactions.Friends)
                        continue;
                }

                if (Vector3D.Distance(Position, grid.PositionComp.GetPosition()) <= 15000)
                {
                    return false;
                }
            }
            return true;
        }
        public Boolean SaveGridToHangar(String gridName, ulong steamid, Alliance alliance, Vector3D position, MyFaction faction, List<MyCubeGrid> gridsToSave, long IdentityId)
        {
            if (!CheckGrids(position, IdentityId))
            {
                AlliancePlugin.Log.Info("Failed grid check");
                return false;
            }

            if (!CheckCharacters(position, IdentityId))
            {
                AlliancePlugin.Log.Info("Failed character check");
                return false;
            }
            HangarLog log = GetHangarLog(alliance);
            HangarLogItem item = new HangarLogItem();
            item.action = "Saved";
            item.steamid = steamid;
            item.GridName = gridName;
            item.time = DateTime.Now;
            log.log.Add(item);
            utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
            HangarItem hangItem = new HangarItem();
            hangItem.name = gridName;
                hangItem.steamid = steamid;
            hangItem.position = position;
            ItemsInHangar.Add(getAvailableSlot(), hangItem);
            GridManager.SaveGridNoDelete(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + gridName + ".xml"), gridName, true, false, gridsToSave);
            if (AlliancePlugin.GridBackupInstalled)
            {
                List<MyObjectBuilder_CubeGrid> obBuilders = new List<MyObjectBuilder_CubeGrid>();
                foreach (MyCubeGrid grid in gridsToSave)
                {
                    obBuilders.Add(grid.GetObjectBuilder() as MyObjectBuilder_CubeGrid);
                }
                AlliancePlugin.BackupGridMethod(obBuilders, IdentityId);
            }
            utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
            return true;
        }
        public Boolean LoadGridFromHangar(int slotNum, ulong steamid, Alliance alliance, MyIdentity identity, MyFaction faction)
        {
            HangarItem hangItem = ItemsInHangar[slotNum];
            if (!CheckGrids(hangItem.position, identity.IdentityId))
                return false;

            if (!CheckCharacters(hangItem.position, identity.IdentityId))
                return false;

            if (!GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml"), ItemsInHangar[slotNum].position, true, steamid, ItemsInHangar[slotNum].name))
            {
                if (!GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml"), ItemsInHangar[slotNum].position, false, steamid, ItemsInHangar[slotNum].name))
                {
                    return false;
                }
            }

                HangarLog log = GetHangarLog(alliance);
                HangarLogItem item = new HangarLogItem();
                item.action = "Loaded";
                item.steamid = steamid;
                item.GridName = ItemsInHangar[slotNum].name;
                item.time = DateTime.Now;
                log.log.Add(item);
                utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);


            if (AlliancePlugin.GridBackupInstalled)
            {
                List<MyObjectBuilder_CubeGrid> obBuilders = new List<MyObjectBuilder_CubeGrid>();
                obBuilders = GridManager.GetObjectBuilders(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml");
                if (obBuilders != null)
                {
                    AlliancePlugin.BackupGridMethod(obBuilders, identity.IdentityId);
                }
                else
                {
                    AlliancePlugin.Log.Error("Error saving a backup when loading this grid");
                }
            }

            File.Delete(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml");
                ItemsInHangar.Remove(slotNum);
                List<HangarItem> temp = new List<HangarItem>();
                foreach (HangarItem hangitem in ItemsInHangar.Values)
                {
                    temp.Add(hangitem);
                }
                ItemsInHangar.Clear();
                int i = 1;
                foreach (HangarItem hangitem in temp)
                {
                    ItemsInHangar.Add(i, hangitem);
                    i++;
                }
                utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
            
            return true;
        }
    }
}
