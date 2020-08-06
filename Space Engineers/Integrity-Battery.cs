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

namespace SpaceEngineers.IntegrityBattery
{
    public sealed class Program : MyGridProgram
    {
        IMyCockpit m_Cockpit;

        const int BatteryIntegrityDisplayIndex = 0;
        IMyTextSurface m_BatteryIntegrityDisplay;
        List<IMyBatteryBlock> m_Batteries = new List<IMyBatteryBlock>();
        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();
        Dictionary<IMyTerminalBlock, bool> m_DamagedBlocks = new Dictionary<IMyTerminalBlock, bool>();

        public Program()
        {
            m_Cockpit = GridTerminalSystem.GetBlockWithName("Diggy Cockpit") as IMyCockpit;

            m_BatteryIntegrityDisplay = m_Cockpit.GetSurface(BatteryIntegrityDisplayIndex);
            GridTerminalSystem.GetBlocksOfType(m_Batteries, a => a is IMyBatteryBlock);
            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks);
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Номер дисплея</param>
        public void Main(string args)
        {
            //IntegrityBatteryDisplay
            IntegrityBatteryDisplay(m_BatteryIntegrityDisplay, m_AllBlocks, m_DamagedBlocks, m_Batteries);
        }

        private static void IntegrityBatteryDisplay(IMyTextSurface display, List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> allBlocks, Dictionary<IMyTerminalBlock, bool> damagedBlocks, List<IMyBatteryBlock> batteries)
        {
            string displayText;
            foreach (var pair in allBlocks)
            {
                if (pair.Value.IsFullIntegrity)
                {
                    if (damagedBlocks.ContainsKey(pair.Key))
                        damagedBlocks[pair.Key] = false;
                }
                else
                {
                    damagedBlocks[pair.Key] = true;
                }
            }

            var showDamaged = new List<IMyTerminalBlock>();
            var hideDamaged = new List<IMyTerminalBlock>();

            foreach (var pair in damagedBlocks)
            {
                if (pair.Value)
                    showDamaged.Add(pair.Key);
                else
                    hideDamaged.Add(pair.Key);
            }

            foreach (var block in hideDamaged)
            {
                damagedBlocks.Remove(block);
                block.ShowOnHUD = false;
            }

            if (showDamaged.Count != 0)
            {
                display.BackgroundColor = Color.DarkRed;
                display.FontSize = 1;
                display.TextPadding = 1;
                displayText = "!!! Damaged Blocks !!!\n";
                foreach (var block in showDamaged)
                {
                    block.ShowOnHUD = true;
                    displayText += string.Format("{0}\n", block.CustomName);
                }
            }
            else
            {
                display.BackgroundColor = new Color(0, 100, 200);
                display.FontSize = 4;
                display.TextPadding = 15;
                float currentBatteryVolume = 0.0f;
                float maxBatteryVolume = 0.0f;
                foreach (var battery in batteries)
                {
                    currentBatteryVolume += battery.CurrentStoredPower;
                    maxBatteryVolume += battery.MaxStoredPower;
                    if (battery.ChargeMode != ChargeMode.Auto)
                        display.BackgroundColor = Color.DarkRed;
                }
                var filledBattery = (double)((currentBatteryVolume / maxBatteryVolume) * 100);
                displayText = string.Format("BATTERY\n{0}%", filledBattery.ToString("F2"));
            }

            display.WriteText(displayText);
        }
    }
}