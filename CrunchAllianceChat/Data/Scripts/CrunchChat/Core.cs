using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Scripting;
using VRage.Utils;
using VRageMath;

namespace Crunch
{
    // This object is always present, from the world load to world unload.
    // The MyUpdateOrder arg determines what update overrides are actually called.
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class CrunchChat : MySessionComponentBase
    {

        private bool _isInitialized; // Is this instance is initialized
        public bool _isClientRegistered;
        public bool _isServerRegistered;

        public static PlayerDataPvP PlayerData;
        public ulong TickCounter { get; private set; } // Big enough for years
        public bool WarStatus = false;
        public bool IsClientRegistered => _isClientRegistered; // Is this instance a client
        public bool IsServerRegistered => _isServerRegistered; // Is this instance a server

        public bool isDebug = true; // Switch debug mode

        public static TextHudModule HudModule = new TextHudModule();

        #region ingame overrides

        /*
		 * Ingame Init
		 * 
		 * Main ingame initialization override
		 */
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            // Set TickCounter always to zero at startup
            TickCounter = 0;
            if (MyAPIGateway.Utilities == null) { MyAPIGateway.Utilities = MyAPIUtilities.Static; }
        }

        /*
		 * BeforeStart
		 * 
		 * Init networking
		 */
        public override void BeforeStart()
        {
            base.BeforeStart();
            HudModule.Init();
            // Register network handling
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8544, MessageHandler);
        }

        private void MessageHandler(ushort handlerId, byte[] message, ulong steamId, bool isServer)
        {

            try
            {
                if (HudModule == null)
                {
                    HudModule = new TextHudModule();
                    HudModule.Init();
                }

                if (!HudModule.HudInit)
                {
                    HudModule.Init();
                }
                var data = (ModMessage)MyAPIGateway.Utilities.SerializeFromBinary<ModMessage>(message);

                switch (data.Type)
                {

                    case "Chat":
                        var status = MyAPIGateway.Utilities.SerializeFromBinary<BoolStatus>(data.Member);

                        if (status.Enabled)
                        {
                            HudModule.SetChatStatus(true);
                            HudModule.SetChatInfo(true);
                        }
                        else
                        {
                            HudModule.SetChatStatus(true);
                            HudModule.SetChatInfo(false);
                        }
                        break;
                    case "WarStatus":
                        var warstatus = MyAPIGateway.Utilities.SerializeFromBinary<BoolStatus>(data.Member);
                        WarStatus = warstatus.Enabled;
                        break;
                    case "PvPAreas":
                        {
                            var playerData = MyAPIGateway.Utilities.SerializeFromBinary<PlayerDataPvP>(data.Member);
                            PlayerData = playerData;
                            break;
                        }
                    case "SinglePVPArea":
                        {
                            var areaData = MyAPIGateway.Utilities.SerializeFromBinary<PvPArea>(data.Member);
                            if (PlayerData != null)
                            {
                                var tempData = PlayerData;
                                foreach (var area in PlayerData.PvPAreas.Where(area => area.Name.Equals(areaData.Name)))
                                {
                                    tempData.PvPAreas.Remove(area);
                                    tempData.PvPAreas.Add(areaData);
                                }

                                PlayerData = tempData;
                            }

                            break;
                        }
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message} \n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification(
                        $"[ Alliance Error: | Send SpaceEngineers.Log to mod author ]", 10000,
                        MyFontEnum.Red);
            }
        }
        /*
		 * UpdateBeforeSimulation
		 * 
		 * UpdateBeforeSimulation override
		 */
        public override void UpdateBeforeSimulation()
        {
            // Init Block
            try
            {
                // Check if Instance exists


                // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
                // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
                if (!_isInitialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
                {
                    if (MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE)) // pretend single player instance is also server.
                    {
                        InitServer();
                    }

                    if (!MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) && MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                    {
                        InitServer();
                    }

                    InitClient();
                }

                // Dedicated Server.
                if (!_isInitialized && MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null
                    && MyAPIGateway.Session != null && MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
                {
                    InitServer();
                    return;
                }

                //TODO: Why not before everything else?
                base.UpdateBeforeSimulation();
            }
            catch (Exception ex)
            {
                // ignored
            }
        }

        public DateTime NextUpdate = DateTime.Now.AddMinutes(1);
        public bool FirstRun = false;
        /*
                 * UpdateAfterSimulation
                 * 
                 * UpdateAfterSimulation override
                 */
        public override void UpdateAfterSimulation()
        {
            TickCounter += 1;
		   // MyLog.Default.WriteLineAndConsole($"1");
		
            if (TickCounter < 10 && !FirstRun)
            {
                return;
            }
			 //   MyLog.Default.WriteLineAndConsole($"2");
		
			if (HudModule == null)
            {
                return;
            }
			//    MyLog.Default.WriteLineAndConsole($"3");
			
		    if (MyAPIGateway.Session == null)
			{
				return;
			}
			 //   MyLog.Default.WriteLineAndConsole($"4");
		
			if (MyAPIGateway.Session.LocalHumanPlayer == null)
			{
				return;
	     	}
			//    MyLog.Default.WriteLineAndConsole($"5");
		
			if (MyAPIGateway.Multiplayer == null){
				return;
			}
			 //   MyLog.Default.WriteLineAndConsole($"6");
		
            if (DateTime.Now >= NextUpdate || !FirstRun)
            {
			//	MyLog.Default.WriteLineAndConsole($"7");
	
                var player = MyAPIGateway.Session.LocalHumanPlayer.SteamUserId;
			//	    MyLog.Default.WriteLineAndConsole($"8");

				if (player == null){
					return;
				}
			//	    MyLog.Default.WriteLineAndConsole($"9");
	
				FirstRun = true;
                var territoryRequest = new DataRequest()
                {
                    SteamId = player,
                    DataType = "Territory"
                };
		//		    MyLog.Default.WriteLineAndConsole($"10");
	
                var request1 = MyAPIGateway.Utilities.SerializeToBinary(territoryRequest);
  //  MyLog.Default.WriteLineAndConsole($"11");
	
                var warStatusRequest = new DataRequest()
                {
                    SteamId = player,
                    DataType = "WarStatus"
                };
		//		    MyLog.Default.WriteLineAndConsole($"12");
		
                var request2 = MyAPIGateway.Utilities.SerializeToBinary(warStatusRequest);
  //  MyLog.Default.WriteLineAndConsole($"13");
	
                var modmessage1 = new ModMessage()
                {
                    Type = "DataRequest",
                    Member = request1
                };
				//    MyLog.Default.WriteLineAndConsole($"14");
		
                var modmessage2 = new ModMessage()
                {
                    Type = "DataRequest",
                    Member = request2
                };
			//	    MyLog.Default.WriteLineAndConsole($"15");
			
                var bytes1 = MyAPIGateway.Utilities.SerializeToBinary(modmessage1);
			//	    MyLog.Default.WriteLineAndConsole($"16");
			
                var bytes2 = MyAPIGateway.Utilities.SerializeToBinary(modmessage2);
  //  MyLog.Default.WriteLineAndConsole($"17");
		
                MyAPIGateway.Multiplayer.SendMessageToServer(8544, bytes1);
				//    MyLog.Default.WriteLineAndConsole($"18");
			
                MyAPIGateway.Multiplayer.SendMessageToServer(8544, bytes2);
				//    MyLog.Default.WriteLineAndConsole($"19");
		
                NextUpdate = DateTime.Now.AddMinutes(1);
				//    MyLog.Default.WriteLineAndConsole($"20");
		
            }

            if (TickCounter % 64 == 0) // Check if player is in an area every 10 seconds
            {
				 //   MyLog.Default.WriteLineAndConsole($"21");
		
          
				//    MyLog.Default.WriteLineAndConsole($"22");
		
				//    MyLog.Default.WriteLineAndConsole($"23");
			
                if (PlayerData != null && PlayerData.PvPAreas != null)
                {
					      HudModule.SetAreaName("Not in Area");
						      //     HudModule.SetAreaPvPEnabled(false);
                    var player = MyAPIGateway.Session.LocalHumanPlayer;
								//    MyLog.Default.WriteLineAndConsole($"24");
					if (player.Character == null){
						return;
					}
                    var position = player.Character.GetPosition();
								//    MyLog.Default.WriteLineAndConsole($"25");
					if (position == null){
						return;
					}
                    foreach (var area in PlayerData.PvPAreas)
                    {
									 //   MyLog.Default.WriteLineAndConsole($"26");
				
                        var distance = Vector3.Distance(position, area.Position);
						//  MyLog.Default.WriteLineAndConsole($"27");
                        if (distance <= area.Distance)
                        {
                            if (area.Name != null)
                            {
                                //HudModule.SetAreaName(area.Name);
                                HudModule.SetAreaName($"{area.Name}");
                            }

                            // Set PvP Area PvP Enabled
                            HudModule.SetAreaPvPEnabled(area.AreaForcesPvP);
                        }
                    }
                }
				
                if (WarStatus)
                {
                    HudModule.SetAreaPvPEnabled(true);
                }
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(8544, MessageHandler);
        }


        private void InitClient()
        {
            _isInitialized = true; // Set this first to block any other calls from UpdateAfterSimulation().
            _isClientRegistered = true;
            //  ClientLogger.Init("CrunchChat_Client.Log", false, 0); // comment this out if logging is not required for the Client. "AppData\Roaming\SpaceEngineers\Storage"
            // ClientLogger.WriteStart("CrunchChat Client Log Started");

        }

        private void InitServer()
        {
            try
            {
                _isInitialized = true; // Set this first to block any other calls from UpdateAfterSimulation().
                _isServerRegistered = true;
                //   ServerLogger.Init("CrunchChat.Log", false, 0); // comment this out if logging is not required for the Server.
                //   ServerLogger.WriteStart("CrunchChat Server Log Started");
                //  ServerLogger.WriteInfo("CrunchChat Server Version {0}", Mod_Config.modVersion.ToString());
                //  ServerLogger.WriteInfo("CrunchChat Communiction Server Version {0}", Mod_Config.ModCommunicationVersion);
                //  if (ServerLogger.IsActive)
                //     VRage.Utils.MyLog.Default.WriteLine(string.Format("##Mod## LST Server Logging File: {0}", ServerLogger.LogFile));



            }
            catch (Exception e)
            {
                //   CrunchChat.Instance.ServerLogger.WriteException(e, "Core::InitServer");
            }
        }

        #endregion



    }
}
