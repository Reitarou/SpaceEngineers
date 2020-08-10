using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRageMath;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;

namespace SpaceEngineers.ControlPanelOnOff
{
    public sealed class Program : MyGridProgram
    {
        const string LcdName = "LcdName";

        const string GroupName = "GroupName";

        List<IMyTerminalBlock> m_Group = new List<IMyTerminalBlock>();
        IMyTextSurface m_Lcd;

        public Program()
        {
            GridTerminalSystem.GetBlockGroupWithName(GroupName).GetBlocks(m_Group);
            m_Lcd = GridTerminalSystem.GetBlockWithName(LcdName) as IMyTextSurface;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            var text = "Functional Blocks:";
            foreach (var block in m_Group)
            {
                var funcBlock = block as IMyFunctionalBlock;
                if (funcBlock != null)
                {
                    var enabled = funcBlock.Enabled ? "Enabled" : "DISABLED";
                    text += string.Format("\n{0}: {1}", block.CustomName, enabled);
                }
            }
            m_Lcd.WriteText(text);
        }

        public void Save()
        { }
    }
}