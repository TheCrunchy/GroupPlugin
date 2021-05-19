﻿using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Alliances
{
    [Category("alliance")]
    public class Commands : CommandModule
    {
        public static Dictionary<long, DateTime> cooldowns = new Dictionary<long, DateTime>();

        public string GetCooldownMessage(DateTime time)
        {
            var diff = time.Subtract(DateTime.Now);
            string output = String.Format("{0} Seconds", diff.Seconds) + " until command can be used.";
            return output;
        }

        [Command("join", "join an alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceJoin(string name)
        {
            if (cooldowns.TryGetValue(Context.Player.IdentityId, out DateTime value))
            {
                if (DateTime.Now <= value)
                {
                    Context.Respond(GetCooldownMessage(value));
                    return;
                }
                else
                {
                    cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
                }
            }
            else
            {
                cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
            }

            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can join alliances");
                return;
            }
            if (fac.IsLeader(Context.Player.IdentityId) || fac.IsFounder(Context.Player.IdentityId))
            {
                Alliance alliance = AlliancePlugin.GetAlliance(name);
                if (alliance != null)
                {
                    if (alliance.Invites.Contains(fac.FactionId))
                    {
                        if (alliance.JoinAlliance(fac))
                        {
                            Context.Respond("Joined alliance!");
                            AlliancePlugin.SaveAllianceData(alliance);
                            alliance.ForceFriendlies();
                            AlliancePlugin.FactionsInAlliances.Add(fac.FactionId, alliance.name);
                            
                        }
                        else
                        {
                            Context.Respond("Couldnt join alliance. Your faction may have been banned.");
                        }
                    }
                }
                else
                {
                    Context.Respond("That alliance doesnt exist.");
                }
            }
            else
            {
                Context.Respond("Only leaders and founders can join an alliance.");
            }
        }
        [Command("description", "change the description")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceDescription(string description)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.description = Context.RawArgs;
                    AlliancePlugin.SaveAllianceData(alliance);
             
                }
            }
        }
        [Command("invite", "invite a faction to alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceInvite(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.SendInvite(fac2.FactionId);
                    AlliancePlugin.SaveAllianceData(alliance);
              
                    Context.Respond("Invite sent, they can join using !alliance join " + alliance.name);
                }
                else
                {
                    Context.Respond("You dont have permission to send invites.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("info", "output info about an alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceInfo(string name = "")
        {
            Boolean console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Alliance alliance = null;
            if (name.Equals(""))
            {
                if (MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) != null)
                {
                    alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) as MyFaction);


                }
            }
            else
            {
                alliance = AlliancePlugin.GetAllianceNoLoading(name);
            }
            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
            }
            if (!console)
            {
                DialogMessage m = new DialogMessage("Alliance Info", alliance.name, alliance.OutputAlliance());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("Alliance Info" + " " + alliance.name + alliance.OutputAlliance());
            }
        }
        [Command("leave", "leave the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceLeave(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            if (fac.IsFounder(Context.Player.IdentityId))
            {
                Alliance alliance = AlliancePlugin.GetAlliance(fac);

                foreach (MyFactionMember m in fac.Members.Values)
                {
                    if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        Context.Respond("The " + alliance.LeaderTitle + " Cannot leave the alliance, Leadership must be transferred first.");
                        return;
                    }
                    if (alliance.officers.Contains(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        alliance.officers.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
                    }

                    if (alliance.admirals.Contains(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        alliance.admirals.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
                    }

                }
                alliance.AllianceMembers.Remove(fac.FactionId);
                AlliancePlugin.SaveAllianceData(alliance);
            
            }
            else
            {
                Context.Respond("Only a Founder can leave the alliance");
            }
        }
        [Command("kick", "kick a faction from the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceKick(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    if (alliance.AllianceMembers.Contains(fac2.FactionId))
                    {
                        bool CanKick = true;
                        foreach (MyFactionMember m in fac2.Members.Values)
                        {
                            if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)) || alliance.officers.Contains(MySession.Static.Players.TryGetSteamId(m.PlayerId)) || alliance.admirals.Contains(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                            {
                                CanKick = false;
                            }
                        }
                        if (CanKick)
                        {
                            alliance.AllianceMembers.Remove(fac2.FactionId);
                            AlliancePlugin.SaveAllianceData(alliance);
                          
                            foreach (long id in alliance.AllianceMembers)
                            {
                                IMyFaction member = MySession.Static.Factions.TryGetFactionById(id);
                                if (member != null)
                                {
                                    MyFactionCollection.DeclareWar(member.FactionId, fac2.FactionId);
                                    MySession.Static.Factions.SetReputationBetweenFactions(id, fac2.FactionId, -1500);
                                }
                            }
                        }
                        else
                        {
                            Context.Respond("Cannot kick that faction while their members hold a rank.");
                        }
                    }
                    else
                    {
                        Context.Respond("That faction isnt a member of the alliance.");
                    }
                }
                else
                {
                    Context.Respond("You dont have permission to kick members.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("peace", "remove an enemy of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePeace(string type, string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId) || alliance.officers.Contains(Context.Player.SteamUserId))
                {
                    switch (type.ToLower())
                    {
                        case "faction":
                        case "fac":
                            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Remove(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);
                    

                            }
                            Context.Respond("Removed from enemy list.");
                            break;
                        case "alliance":
                            if (AlliancePlugin.AllAlliances.ContainsKey(tag))
                            {
                                if (alliance.enemies.Contains(tag))
                                {
                                    alliance.enemies.Remove(tag);
                                    AlliancePlugin.SaveAllianceData(alliance);
                                

                                }
                            }
                            Context.Respond("Removed from enemy list.");
                            break;
                    }
                }
                else
                {
                    Context.Respond("You dont have permission to declare enemies.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("enemy", "declare an enemy of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceEnemy(string type, string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId) || alliance.officers.Contains(Context.Player.SteamUserId))
                {
                    switch (type.ToLower())
                    {
                        case "faction":
                        case "fac":
                            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (!alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Add(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);
                     
                                alliance.ForceEnemies();
                            }
                            Context.Respond("War declared");
                            break;
                        case "alliance":
                            if (AlliancePlugin.AllAlliances.ContainsKey(tag))
                            {
                                if (!alliance.enemies.Contains(tag))
                                {
                                    alliance.enemies.Add(tag);
                                    AlliancePlugin.SaveAllianceData(alliance);
                       
                                    alliance.ForceEnemies();
                                }
                            }
                            Context.Respond("War declared");
                            break;
                    }
                }
                else
                {
                    Context.Respond("You dont have permission to declare enemies.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("revoke", "revoke a factions invite to alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceRevoke(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.RevokeInvite(fac2.FactionId);
                    AlliancePlugin.SaveAllianceData(alliance);
         
                    Context.Respond("Invite revoked.");
                }
                else
                {
                    Context.Respond("You dont have permission to revoke invites.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("set title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void SetTitleName(string title, string newName)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(newName, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(newName))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
            {
                if (alliance != null)
                {
                    switch (title.ToLower())
                    {
                        case "leader":
                            alliance.LeaderTitle = newName;
                            AlliancePlugin.SaveAllianceData(alliance);
              
                            Context.Respond("Updated");
                            return;
                        case "admiral":
                            alliance.AdmiralTitle = newName;
                            AlliancePlugin.SaveAllianceData(alliance);
                         
                            Context.Respond("Updated");
                            return;
                        case "officer":
                            alliance.OfficerTitle = newName;
                            AlliancePlugin.SaveAllianceData(alliance);
                       
                            Context.Respond("Updated");
                            return;
                        default:
                            Context.Respond("Could not find that title. You can change Leader, Admiral and Officer");
                            break;
                    }
                }
            }
            else
            {
                Context.Respond("Only the " + alliance.LeaderTitle + " can change titles.");
            }
        }
        public static Dictionary<long, DateTime> confirmations = new Dictionary<long, DateTime>();
        [Command("disband", "disband the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void Disband()
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);



            if (alliance != null)
            {

                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    if (confirmations.ContainsKey(Context.Player.IdentityId))
                    {
                      if (confirmations[Context.Player.IdentityId] >= DateTime.Now)
                        {
                            File.Delete(AlliancePlugin.path + "//" + alliance.name.Replace(" ", "_") + ".json");
                            foreach (long id in alliance.AllianceMembers)
                            {
                                AlliancePlugin.FactionsInAlliances.Remove(id);
                            }
                            AlliancePlugin.AllAlliances.Remove(alliance.name);
                            Context.Respond("Alliance disbanded.");
                        }
                        else
                        {
                            Context.Respond("Time ran out, start again");
                            confirmations[Context.Player.IdentityId] = DateTime.Now.AddSeconds(20);
                        }
                    }
                    else
                    {
                        Context.Respond("Run command again within 20 seconds to confirm.");
                        confirmations.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(20));
                    }
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can disband the alliance.");
                }
            }
        }

        [Command("name", "change the alliance name")]
        [Permission(MyPromoteLevel.None)]
        public void SetAllianceName(string name)
        {
            name = Context.RawArgs;
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(name, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(name))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);

            if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
            {
                alliance.name = name;
                AlliancePlugin.SaveAllianceData(alliance);
 
                Context.Respond("Name updated");
                return;
            }
            
        }

        [Command("grant title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void GiveTitleName(string playerName, string Title)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(Title, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(Title))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }
            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction.");
                return;
            }
            if (!alliance.AllianceMembers.Contains(playerFac.FactionId))
            {
                Context.Respond("That target player isnt a member of the alliance.");
                return;
            }
            if (alliance != null)
            {
                switch (Title.ToLower())
                {
                    case "admiral":
                        if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                        {
                            if (!alliance.admirals.Contains(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                            {
                                alliance.admirals.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId));
                            }
                            AlliancePlugin.SaveAllianceData(alliance);
                   
                            Context.Respond("Updated");
                        }
                        else
                        {
                            Context.Respond("Only the " + alliance.LeaderTitle + " can grant this title.");
                        }
                        return;
                    case "officer":
                        if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                        {
                            if (!alliance.officers.Contains(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                            {
                                alliance.officers.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId));
                            }
                            AlliancePlugin.SaveAllianceData(alliance);
                         
                        }
                        else
                        {
                            Context.Respond("Only the " + alliance.LeaderTitle + " can grant this title.");
                        }
                        return;
                    default:
                        if (alliance.admirals.Contains(Context.Player.SteamUserId) || alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                        {
                            alliance.SetTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                            AlliancePlugin.SaveAllianceData(alliance);
                         
                        }
                        else
                        {
                            Context.Respond("Only the " + alliance.LeaderTitle + " or " + alliance.AdmiralTitle + " can grant titles.");
                        }
                        break;
                }
            }
        }


        [Command("create", "create a new alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceCreate(string name)
        {
            IMyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            name = Context.RawArgs;
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(name, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(name))
            {
                Context.Respond("Name does not validate, try again.");
                return;
            }
            if (AlliancePlugin.AllAlliances.ContainsKey(name))
            {
                Context.Respond("Alliance with that name already exists.");
                return;
            }
            if (MyBankingSystem.GetBalance(Context.Player.IdentityId) >= AlliancePlugin.config.PriceNewAlliance)
            {


                if (fac != null)
                {
                    if (fac.IsFounder(Context.Player.IdentityId))
                    {
                        foreach (KeyValuePair<String, Alliance> alliance in AlliancePlugin.AllAlliances)
                        {
                            if (alliance.Value.AllianceMembers.Contains(fac.FactionId))
                            {
                                Context.Respond("You cannot create an alliance while being a member of an alliance.");
                                return;
                            }
                        }
                        Alliance newAlliance = new Alliance();
                        newAlliance.name = name;

                        newAlliance.SupremeLeader = Context.Player.SteamUserId;
                        newAlliance.ForceAddMember(fac.FactionId);
                        EconUtils.takeMoney(Context.Player.IdentityId, AlliancePlugin.config.PriceNewAlliance);
                        foreach (MyFactionMember m in fac.Members.Values)
                        {
                            if (m.IsLeader)
                            {
                                ulong steamId = Sync.Players.TryGetSteamId(m.PlayerId);
                                if (steamId > 0)
                                {
                                    newAlliance.admirals.Add(steamId);
                                }
                            }
                        }

                        AlliancePlugin.AllAlliances.Add(name, newAlliance);
                        AlliancePlugin.FactionsInAlliances.Add(fac.FactionId, newAlliance.name);
                        AlliancePlugin.SaveAllianceData(newAlliance);
                    }
                    else
                    {
                        Context.Respond("Only the founder may create an alliance.");
                        return;
                    }
                }
                else
                {
                    Context.Respond("You must be a member of a faction to create an alliance.");
                    return;
                }
            }
            else
            {
                Context.Respond("Cannot afford to create an alliance, it costs " + String.Format("{0:n0}", AlliancePlugin.config.PriceNewAlliance) + " SC.");
                return;
            }
        }
    }
}