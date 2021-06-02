using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GameSystems.BankingAndCurrency;
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
using VRageMath;

namespace AlliancesPlugin
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
        public static AccessLevel StringToAccessLevel(string input)
        {
            switch (input.ToLower())
            {
                case "hangarsave":
                    return AccessLevel.HangarSave;
                case "hangarload":
                    return AccessLevel.HangarLoad;
                case "hangarloadother":
                    return AccessLevel.HangarLoadOther;
                case "bankwithdraw":
                    return AccessLevel.BankWithdraw;
                case "shipyardstart":
                    return AccessLevel.ShipyardStart;
                case "shipyardclaim":
                    return AccessLevel.ShipyardClaim;
                case "shipyardclaimother":
                    return AccessLevel.ShipyardClaimOther;
                case "dividendpay":
                    return AccessLevel.DividendPay;
                case "invite":
                    return AccessLevel.Invite;
                case "kick":
                    return AccessLevel.Kick;
                case "revokelowertitle":
                    return AccessLevel.RevokeLowerTitle;
                case "grantlowertitle":
                    return AccessLevel.GrantLowerTitle;
                case "removeenemy":
                    return AccessLevel.RemoveEnemy;
                case "addenemy":
                    return AccessLevel.AddEnemy;
                case "payfrombank":
                    return AccessLevel.PayFromBank;
                case "unabletoparse":
                    return AccessLevel.UnableToParse;
            }
            return AccessLevel.UnableToParse;
        }
        [Command("rank permissions", "set a ranks permissions")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePermissions(string rank, string permission, Boolean enabled)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            AccessLevel level = StringToAccessLevel(permission);
            if (level == AccessLevel.UnableToParse)
            {
                Context.Respond("Unable to read that permission, you can change, HangarSave, HangarLoad, HangarLoadOther, Kick, Invite, ShipyardStart, ShipyardClaim, ShipyardClaimOther, DividendPay, BankWithdraw, PayFromBank, AddEnemy, RemoveEnemy, GrantLowerTitle, Vote, RevokeLowerTitle.");
                return;
            }
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId) && alliance.CustomRankPermissions.ContainsKey(rank))
                {
                    if (rank.ToLower().Equals("citizen"))
                    {
                        if (enabled)
                        {
                            if (!alliance.CitizenPerms.permissions.Contains(level))
                                alliance.CitizenPerms.permissions.Add(level);
                        }
                        else
                        {
                            if (alliance.CitizenPerms.permissions.Contains(level))
                                alliance.CitizenPerms.permissions.Remove(level);
                        }
                    }
                    else
                    {
                        if (enabled)
                        {
                            if (!alliance.CustomRankPermissions[rank].permissions.Contains(level))
                                alliance.CustomRankPermissions[rank].permissions.Add(level);
                        }
                        else
                        {
                            if (alliance.CustomRankPermissions[rank].permissions.Contains(level))
                                alliance.CustomRankPermissions[rank].permissions.Remove(level);
                        }
                    }

                    Context.Respond("Updated that permission level for Citizens.");


                    AlliancePlugin.SaveAllianceData(alliance);
                }
                else
                {
                    Context.Respond("You dont have permission or that rank doesnt exist.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("view permissions", "set a players permissions")]
        [Permission(MyPromoteLevel.None)]
        public void ViewPermissions(string playerName, string permission, Boolean enabled)
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
                StringBuilder sb = new StringBuilder();
                StringBuilder perms = new StringBuilder();
                foreach (String key in alliance.CustomRankPermissions.Keys)
                {
                    foreach (AccessLevel level in alliance.CustomRankPermissions[key].permissions)
                    {
                        perms.Append(level.ToString() + ", ");
                    }
                    sb.AppendLine(key + " Permissions : " + perms.ToString());
                    sb.AppendLine(key + " Permission Level " + alliance.CustomRankPermissions[key].permissionLevel);
                }
                perms.Clear();
                sb.AppendLine("");
                foreach (AccessLevel level in alliance.CitizenPerms.permissions)
                {
                    perms.Append(level.ToString() + ", ");
                }
                sb.AppendLine("Citizen Permissions : " + perms.ToString());
                sb.AppendLine("");
                foreach (KeyValuePair<ulong, RankPermissions> player in alliance.playerPermissions)
                {
                    perms.Clear();
                    foreach (AccessLevel level in player.Value.permissions)
                    {
                        perms.Append(level.ToString() + ", ");
                    }
                    sb.AppendLine(MyMultiplayer.Static.GetMemberName(player.Key) + " " + perms.ToString());
                }

                DialogMessage m = new DialogMessage("Alliance Permissions", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You arent a member of an alliance.");
            }
        }
        [Command("make rank", "make a rank")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceCreateRank(string rankName, int permissionLevel)
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


                        if (alliance.CustomRankPermissions.ContainsKey(rankName))
                        {
                        Context.Respond("Rank with that name already exists!");
                        }
                        else
                        {
                            RankPermissions bob = new RankPermissions();
                        bob.permissionLevel = permissionLevel;
                        alliance.CustomRankPermissions.Add(rankName, bob);
                        Context.Respond("Rank created!");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
         
                }
                else
                {
                    Context.Respond("You dont have permission to create ranks");
                }

            }


            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("delete rank", "create a rank")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceDeleteRank(string rankName)
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


                    if (alliance.CustomRankPermissions.ContainsKey(rankName))
                    {
                        List<ulong> yeetThese = new List<ulong>();
                        foreach (ulong id in alliance.PlayersCustomRank.Keys)
                        {
                            if (alliance.PlayersCustomRank[id].Equals(rankName))
                            {
                                yeetThese.Add(id);
                            }
                        }
                        foreach (ulong id in yeetThese)
                        {
                            alliance.PlayersCustomRank.Remove(id);
                        }
                        alliance.CustomRankPermissions.Remove(rankName);
                        Context.Respond("Rank deleted.");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("Rank with that name doesnt exist!");
                     
                    }

                }
                else
                {
                    Context.Respond("You dont have permission to delete ranks");
                }

            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("player permissions", "set a players permissions")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePlayerPermissions(string playerName, string permission, Boolean enabled)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            AccessLevel level = StringToAccessLevel(permission);
            if (level == AccessLevel.UnableToParse)
            {
                Context.Respond("Unable to read that permission, you can change, HangarSave, HangarLoad, HangarLoadOther, Kick, Invite, ShipyardStart, ShipyardClaim, ShipyardClaimOther, DividendPay, BankWithdraw, PayFromBank, AddEnemy, RemoveEnemy, GrantLowerTitle, Vote, RevokeLowerTitle.");
                return;
            }
            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction.");
                return;
            }

            if (alliance != null)
            {
                if (!alliance.AllianceMembers.Contains(playerFac.FactionId))
                {
                    Context.Respond("That target player isnt a member of the alliance.");
                    return;
                }
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {

                    if (enabled)
                    {
                        if (!alliance.playerPermissions.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.playerPermissions[MySession.Static.Players.TryGetSteamId(id.IdentityId)].permissions.Add(level);
                        }
                        else
                        {
                            RankPermissions bob = new RankPermissions();
                            bob.permissions.Add(level);
                            alliance.playerPermissions.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId), bob);

                        }

                    }
                    else
                    {
                        if (alliance.playerPermissions.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.playerPermissions[MySession.Static.Players.TryGetSteamId(id.IdentityId)].permissions.Remove(level);
                        }

                    }
                    Context.Respond("Updated that permission level for the player.");
                    AlliancePlugin.SaveAllianceData(alliance);
                }
                else
                {
                    Context.Respond("You dont have permission to set permissions.");
                }

            }


            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
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
        [Command("reload", "reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceInfo()
        {
            AlliancePlugin.LoadConfig();
            Context.Respond("Reloaded");
        }
        [Command("takepoints", "take points from an alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceInfo(string name, int amount)
        {
            Boolean console = false;
            Alliance alliance = null;

            alliance = AlliancePlugin.GetAlliance(name);
            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }

            if (alliance.CurrentMetaPoints >= amount)
            {
                alliance.CurrentMetaPoints -= amount;
                AlliancePlugin.SaveAllianceData(alliance);
                Context.Respond("Points taken, new balance " + alliance.CurrentMetaPoints);
            }
            else
            {
                Context.Respond("Alliance does not have enough points.");
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
                return;
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
                if (alliance == null)
                {
                    Context.Respond("Could not find that alliance.");
                    return;
                }
                foreach (MyFactionMember m in fac.Members.Values)
                {
                    if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        Context.Respond("The " + alliance.LeaderTitle + " Cannot leave the alliance, Leadership must be transferred first.");
                        return;
                    }
                    if (alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        alliance.PlayersCustomRank.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
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
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.Kick))
                {
                    if (alliance.AllianceMembers.Contains(fac2.FactionId))
                    {
                        bool CanKick = true;
                        foreach (MyFactionMember m in fac2.Members.Values)
                        {
                            if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)) || alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
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
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.RemoveEnemy))
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
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.AddEnemy))
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
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.Invite))
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
                    if (title.ToLower().Equals("leader"))
                    {
                        alliance.LeaderTitle = newName;
                        AlliancePlugin.SaveAllianceData(alliance);

                        Context.Respond("Updated");
                        return;
                    }
                    else
                    {
                        if (alliance.CustomRankPermissions.ContainsKey(title) && !alliance.CustomRankPermissions.ContainsKey(newName))
                        {
                            RankPermissions temp = alliance.CustomRankPermissions[title];
                            alliance.CustomRankPermissions.Remove(title);
                            alliance.CustomRankPermissions.Add(newName, temp);

                            foreach (KeyValuePair<ulong, string> fuck in alliance.PlayersCustomRank)
                            {
                                if (fuck.Value.Equals(title))
                                {
                                    alliance.PlayersCustomRank[fuck.Key] = newName;
                                }
                            }
                        }
                        else
                        {
                            Context.Respond("Could not find that title. Or changing it would conflict.");
                        }
                    }
                }

            }
            else
            {
                Context.Respond("Only the " + alliance.LeaderTitle + " can change titles.");
            }
        }
        public static Dictionary<long, DateTime> confirmations = new Dictionary<long, DateTime>();
        [Command("dividend", "pay dividends to members online within the last 10 days, or the optional input")]
        [Permission(MyPromoteLevel.None)]
        public void Dividend(string inputAmount, int cutoffDays = 10)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }

            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);


            var cutoff = DateTime.Now - TimeSpan.FromDays(cutoffDays);
            if (alliance != null)
            {

                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.DividendPay))
                {
                    if (alliance.bankBalance >= amount)
                    {
                        List<long> idsToPay = new List<long>();
                        foreach (long id in alliance.AllianceMembers)
                        {
                            IMyFaction faction = MySession.Static.Factions.TryGetFactionById(id);
                            if (faction != null)
                            {
                                foreach (KeyValuePair<long, MyFactionMember> mem in faction.Members)
                                {
                                    MyIdentity idenid = MySession.Static.Players.TryGetIdentity(mem.Value.PlayerId);
                                    DateTime referenceTime = idenid.LastLoginTime;
                                    if (idenid.LastLogoutTime > referenceTime)
                                        referenceTime = idenid.LastLogoutTime;
                                    if (referenceTime >= cutoff)
                                    {
                                        idsToPay.Add(mem.Value.PlayerId);

                                    }

                                }
                            }
                        }
                        alliance.PayDividend(amount, idsToPay, Context.Player.SteamUserId);
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("Alliance bank cannot afford that.");
                        return;
                    }
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can pay dividends");
                }
            }
            else
            {
                Context.Respond("You are not a member of an alliance.");
            }

        }


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
                            File.Delete(AlliancePlugin.path + "//AllianceData//" + alliance.AllianceId+ ".json");
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
                Context.Respond("New Name does not validate, try again.");
                return;
            }
            if (AlliancePlugin.AllAlliances.ContainsKey(name))
            {
                Context.Respond("Alliance with that name already exists.");
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
        [Command("withdraw", "withdraw from the bank")]
        [Permission(MyPromoteLevel.None)]
        public void BankWithdraw(string inputAmount)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may access a bank.");
                return;
            }
            if (alliance != null)
            {

                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.BankWithdraw))
                {
                    if (alliance.bankBalance >= amount)
                    {
                        EconUtils.addMoney(Context.Player.IdentityId, amount);
                        alliance.WithdrawMoney(amount, Context.Player.SteamUserId);
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                    }

                }
                else
                {
                    Context.Respond("You do not have access to the bank.");
                }
                return;

            }
        }
        [Command("log", "View the bank log")]
        [Permission(MyPromoteLevel.None)]
        public void BankLog(string timeformat = "MM-dd-yyyy")
        {

            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("You must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }

                BankLog log = alliance.GetLog();
                StringBuilder sb = new StringBuilder();
                log.log.Reverse();
                foreach (BankLogItem item in log.log)
                {
                    if (item.FactionPaid > 0)
                    {
                        IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                        if (fac != null)
                        {
                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + MyMultiplayer.Static.GetMemberName(item.SteamId) + " " + item.Action + " " + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + " : new balance " + String.Format("{0:n0}", item.BankAmount));
                        }
                        else
                        {
                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + MyMultiplayer.Static.GetMemberName(item.SteamId) + " " + item.Action + " a now dead faction " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                        }
                        continue;
                    }
                    if (item.PlayerPaid > 0)
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + MyMultiplayer.Static.GetMemberName(item.SteamId) + " " + item.Action + " " + MyMultiplayer.Static.GetMemberName(item.PlayerPaid) + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                    }
                    else
                    {

                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + MyMultiplayer.Static.GetMemberName(item.SteamId) + " " + item.Action + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                    }
                }
                DialogMessage m = new DialogMessage("Alliance Bank Records", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }

        }

        [Command("deposit", "deposit to the bank")]
        [Permission(MyPromoteLevel.None)]
        public void BankDeposit(string inputAmount)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may access a bank.");
                return;
            }
            if (alliance != null)
            {
                if (EconUtils.getBalance(Context.Player.IdentityId) >= amount)
                {
                    EconUtils.takeMoney(Context.Player.IdentityId, amount);
                    alliance.DepositMoney(amount, Context.Player.SteamUserId);
                    AlliancePlugin.SaveAllianceData(alliance);
                }
                else
                {
                    Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                }

            }

            return;

        }

        public void DoAlliancePay(string type, string nameortag, Int64 amount, Alliance alliance, ulong steamid)
        {
            if (type.ToLower().Equals("player"))
            {
                MyIdentity id = AlliancePlugin.TryGetIdentity(nameortag);
                if (id == null)
                {
                    Context.Respond("Could not find that player");
                    return;
                }
                EconUtils.addMoney(id.IdentityId, amount);
                alliance.PayPlayer(amount, steamid, MySession.Static.Players.TryGetSteamId(id.IdentityId));
            }
            else
            {
                MyFaction playerFac = MySession.Static.Factions.TryGetFactionByTag(nameortag);

                if (playerFac == null)
                {
                    Context.Respond("That target player has no faction.");
                    return;
                }
                EconUtils.addMoney(playerFac.FactionId, amount);
                alliance.PayFaction(amount, steamid, playerFac.FactionId);
            }
        }

        [Command("pay", "pay a player from the bank")]
        [Permission(MyPromoteLevel.None)]
        public void GiveTitleName(string type, string nameortag, string inputAmount)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("You must be in an alliance to use the bank.");
                return;
            }

            if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.PayFromBank))
            {
                if (alliance.bankBalance >= amount)
                {
                    DoAlliancePay(type, nameortag, amount, alliance, Context.Player.SteamUserId);

                }
                else
                {
                    Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                }
            }
            else
            {
                Context.Respond("You do not have access to the bank.");
            }

        }
        [Command("chat", "toggle alliance chat")]
        [Permission(MyPromoteLevel.None)]
        public void DoAllianceChat(string message = "")
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
                if (AllianceChat.PeopleInAllianceChat.ContainsKey(Context.Player.SteamUserId))
                {
                    AllianceChat.PeopleInAllianceChat.Remove(Context.Player.SteamUserId);
                    Context.Respond("Leaving alliance chat.", Color.Red);
                }
                else
                {
                    AllianceChat.PeopleInAllianceChat.Add(Context.Player.SteamUserId, alliance.AllianceId);
                    Context.Respond("Entering alliance chat.", Color.Cyan);
                }
            }
            else
            {
                Context.Respond("You must be in an alliance to use alliance chat.");
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

                if (alliance.CustomRankPermissions.ContainsKey(Title))
                {


                    if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                    {
                        if (!alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.PlayersCustomRank.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                        }
                        AlliancePlugin.SaveAllianceData(alliance);

                        Context.Respond("Updated");
                    }
                    else
                    {
                        Context.Respond("Only the " + alliance.LeaderTitle + " can grant this title.");
                    }
                    return;
                }
                else
                {
                    if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.GrantLowerTitle))
                    {
                        RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[Context.Player.SteamUserId]];
                        RankPermissions newTitle = alliance.CustomRankPermissions[Title];

                        if (thisGuy.permissionLevel > newTitle.permissionLevel)
                        {
                            alliance.SetTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                            AlliancePlugin.SaveAllianceData(alliance);
                        }
                        else
                        {
                            Context.Respond("That rank is higher or same rank as you.");
                        }
                   

                    }
                    else
                    {
                        Context.Respond("No permission to grant titles.");
                    }
                }
            }
        }

        [Command("change leader", "change the leader of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void Abdicate(string playerName)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
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


                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    alliance.SupremeLeader = MySession.Static.Players.TryGetSteamId(id.IdentityId);
                    AlliancePlugin.SaveAllianceData(alliance);

                    Context.Respond("They are now the alliance leader.");
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can change the leader.");
                }
                return;


            }
        }
        [Command("revoke title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void RevokeTitleName(string playerName, string Title)
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
            if (alliance != null)
            {
                if (alliance.CustomRankPermissions.ContainsKey(Title))
                {


                    if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                    {
                        if (alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.PlayersCustomRank.Remove(MySession.Static.Players.TryGetSteamId(id.IdentityId));
                        }
                        AlliancePlugin.SaveAllianceData(alliance);

                        Context.Respond("Updated");
                    }
                    else
                    {
                        Context.Respond("Only the " + alliance.LeaderTitle + " can grant this title.");
                    }
                    return;
                }
                else
                {
                    if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.RevokeLowerTitle))
                    {
                        RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[Context.Player.SteamUserId]];
                        RankPermissions newTitle = alliance.CustomRankPermissions[Title];

                        if (thisGuy.permissionLevel > newTitle.permissionLevel)
                        {
                            alliance.SetTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                            AlliancePlugin.SaveAllianceData(alliance);
                        }
                        else
                        {
                            Context.Respond("That rank is higher or same rank as you.");
                        }


                    }
                    else
                    {
                        Context.Respond("No permission to revoke titles.");
                    }
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
                        newAlliance.CustomRankPermissions.Add("Admiral", new RankPermissions());
                        foreach (MyFactionMember m in fac.Members.Values)
                        {
                            if (m.IsLeader)
                            {
                                ulong steamId = Sync.Players.TryGetSteamId(m.PlayerId);
                                if (steamId > 0)
                                {
                                    newAlliance.PlayersCustomRank.Add(steamId, "Admiral");
                                }
                            }
                        }
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.HangarSave);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.HangarLoad);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.ShipyardClaim);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.ShipyardStart);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Invite);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Kick);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Vote);
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
