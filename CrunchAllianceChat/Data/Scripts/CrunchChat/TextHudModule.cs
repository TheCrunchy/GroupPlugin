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

        HudAPIv2.HUDMessage HUD_Status;
        StringBuilder StatusText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_Info;
        StringBuilder InfoText = new StringBuilder("");

        HudAPIv2.HUDMessage HUD_Channels;
        StringBuilder ChannelsText = new StringBuilder("");



        public bool HudInit = false;

        private void HUD_Init_Complete()
        {
			          // Init status line
                HUD_Status = new HudAPIv2.HUDMessage(StatusText, new Vector2D(-0.7, -0.65), null, -1, 0.7, true, false, null, BlendTypeEnum.PostPP, "white");

                // Init info line
                HUD_Info = new HudAPIv2.HUDMessage(InfoText, new Vector2D(-0.7, -0.65), null, -1, 1, true, false, null, BlendTypeEnum.PostPP, "white");
				
				
                SetStatus(true);
				SetInfo(false);
                HudInit = true;
  
        }

        public void Init()
        {
            HUD_Base = new HudAPIv2(HUD_Init_Complete);
        }

     


        public void SetStatus(bool status)
        {
            StatusText.Clear();
            // Check for active
            if (status)
            {
                HUD_Status.InitialColor = Color.Cyan;
                StatusText.Append("Alliance Chat Status");
            }
			
            HUD_Status.Visible = true;
			   HUD_Info.Visible = true;
        }

        public void SetInfo(bool status)
        {
            InfoText.Clear();
			if (status){
				   HUD_Info.InitialColor = Color.Green;
                    InfoText.Append("Enabled");
			}
			else {
					   HUD_Info.InitialColor = Color.Red;
                    InfoText.Append("Disabled");
			}
        }
    }
}
