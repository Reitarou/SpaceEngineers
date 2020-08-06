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

namespace SpaceEngineers.Scotty
{
    public sealed class Program : MyGridProgram
    {
        IMyCockpit m_Cockpit;

        const int BatteryIntegrityDisplayIndex = 1;
        IMyTextSurface m_BatteryIntegrityDisplay;
        List<IMyBatteryBlock> m_Batteries = new List<IMyBatteryBlock>();
        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();
        Dictionary<IMyTerminalBlock, bool> m_DamagedBlocks = new Dictionary<IMyTerminalBlock, bool>();

        List<IMyGyro> m_Gyros;
        private bool m_AutoHorizon = false;

        public Program()
        {
            m_Cockpit = GridTerminalSystem.GetBlockWithName("Aquila Cockpit") as IMyCockpit;

            m_BatteryIntegrityDisplay = m_Cockpit.GetSurface(BatteryIntegrityDisplayIndex);
            GridTerminalSystem.GetBlocksOfType(m_Batteries, a => a is IMyBatteryBlock);
            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks);
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            m_Gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(m_Gyros, a => a.IsSameConstructAs(m_Cockpit));

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string args)
        {
            //IntegrityBatteryDisplay
            IntegrityBatteryDisplay(m_BatteryIntegrityDisplay, m_AllBlocks, m_DamagedBlocks, m_Batteries);

            //KeepHorizon
            KeepHorizon(args, ref m_AutoHorizon, m_Cockpit, m_Gyros);
        }

        public void Save()
        { }


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

        public static void KeepHorizon(string arg, ref bool keepHorizon, IMyShipController cockpit, List<IMyGyro> gyros)
        {
            switch (arg)
            {
                case "KeepHorizonOff":
                    keepHorizon = false;
                    break;

                case "KeepHorizonOn":
                    keepHorizon = true;
                    break;
            }

            foreach (var gyro in gyros)
                gyro.GyroOverride = keepHorizon;

            if (keepHorizon)
            {
                Vector3D grav = Vector3D.Normalize(cockpit.GetNaturalGravity());
                Vector3D axis = grav.Cross(cockpit.WorldMatrix.Down);
                var signal = cockpit.WorldMatrix.Up * cockpit.RollIndicator;
                if (grav.Dot(cockpit.WorldMatrix.Down) < 0)
                    axis = Vector3D.Normalize(axis);

                axis += signal;
                foreach (var gyro in gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }
        }
    }
}