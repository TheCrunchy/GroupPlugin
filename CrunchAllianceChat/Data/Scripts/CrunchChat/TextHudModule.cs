using Draygo.API;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Crunch
{
    public class TextHudModule
    {
        public TextHudModule() { }

        HudAPIv2 HUD_Base;

        HudAPIv2.HUDMessage HUD_ChatStatus;
        StringBuilder ChatStatusText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_ChatInfo;
        StringBuilder ChatInfoText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_AreaName;
        StringBuilder AreaNameText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_AreaPvPEnabled;
        StringBuilder AreaPvPEnabledText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_AreaHeader;
        StringBuilder AreaHeaderText = new StringBuilder("");



        public bool HudInit = false;

        private void HUD_Init_Complete()
        {
            // Init AreaHeader line
            HUD_AreaHeader = new HudAPIv2.HUDMessage(AreaHeaderText, new Vector2D(0.75, 0.95), null, -1, 1.25, true, false, null, BlendTypeEnum.PostPP, "white");

            // Init AreaName line
            HUD_AreaName = new HudAPIv2.HUDMessage(AreaNameText, new Vector2D(0.75, 0.90), null, -1, 1, true, false, null, BlendTypeEnum.PostPP, "white");

            // Init AreaPvPEnabled line
            HUD_AreaPvPEnabled = new HudAPIv2.HUDMessage(AreaPvPEnabledText, new Vector2D(0.75, 0.85), null, -1, 1, true, false, null, BlendTypeEnum.PostPP, "white");

            // Init status line
            HUD_ChatStatus = new HudAPIv2.HUDMessage(ChatStatusText, new Vector2D(-0.7, -0.60), null, -1, 1, true, false, null, BlendTypeEnum.PostPP, "white");

            // Init info line
            HUD_ChatInfo = new HudAPIv2.HUDMessage(ChatInfoText, new Vector2D(-0.7, -0.65), null, -1, 1, true, false, null, BlendTypeEnum.PostPP, "white");


            SetChatStatus(true);
            SetAreaHeader(true);
            SetAreaName();
            SetAreaPvPEnabled(false);
            SetChatInfo(false);
            HudInit = true;
  
        }

        public void Init()
        {
            HUD_Base = new HudAPIv2(HUD_Init_Complete);
        }
		public void SetAreaNotVisible(){
			
            HUD_AreaHeader.Visible = false;
            HUD_AreaName.Visible = false;
            HUD_AreaPvPEnabled.Visible = false;
		}
		
		public void SetAreaVisible(){
            HUD_AreaHeader.Visible = true;
            HUD_AreaName.Visible = true;
            HUD_AreaPvPEnabled.Visible = true;
			
		}
        public void SetAreaHeader(bool check)
        {
            AreaHeaderText.Clear();
            if (check)
            {
                HUD_AreaHeader.InitialColor = Color.Cyan;
                AreaHeaderText.Append("PvP Area Info");
            }

            HUD_AreaHeader.Visible = true;
            HUD_AreaName.Visible = true;
            HUD_AreaPvPEnabled.Visible = true;

        }

        public void SetAreaName(string name = "")
        {
            AreaNameText.Clear();
            if (!name.Equals(""))
            {
                HUD_AreaName.InitialColor = Color.White;
                AreaNameText.Append($"Area: {name}");
            }
            else
            {
                HUD_AreaName.InitialColor = Color.White;
                AreaNameText.Append($"Area: No Area");
            }

        }

        public void SetAreaPvPEnabled(bool status)
        {
            AreaPvPEnabledText.Clear();
            if (status)
            {
                HUD_AreaPvPEnabled.InitialColor = Color.Red;
                AreaPvPEnabledText.Append($"PvP Enabled");
            }
            else
            {
                HUD_AreaPvPEnabled.InitialColor = Color.Green;
                AreaPvPEnabledText.Append($"PvP Disabled");
            }

        }

        public void SetChatStatus(bool status)
        {
            ChatStatusText.Clear();
            // Check for active
            if (status)
            {
                HUD_ChatStatus.InitialColor = Color.Cyan;
                ChatStatusText.Append("Alliance Chat Status");
            }
			
            HUD_ChatStatus.Visible = true;
			HUD_ChatInfo.Visible = true;
        }

        public void SetChatInfo(bool status)
        {
            ChatInfoText.Clear();
			if (status){
				   HUD_ChatInfo.InitialColor = Color.Green;
                    ChatInfoText.Append("Enabled");
			}
			else {
					   HUD_ChatInfo.InitialColor = Color.Red;
                    ChatInfoText.Append("Disabled");
			}
        }
    }
}
