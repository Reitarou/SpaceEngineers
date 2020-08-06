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

namespace SpaceEngineers.DiggyProG
{
    public sealed class Program : MyGridProgram
    {
        const double DiggyMass = 124848;
        const double DiggyMassMax = 520000;
        const string CockpitName = "Diggy Cockpit";
        const string ConnectorName = "Diggy Connector";
        const int IntegrityBatteryDisplayIndex = 0;
        const int CargoCheckDisplayIndex = 2;
        const string UnloadCargoName = "Alpha Main Cargo";

        const string KeepHorizonOnArg = "KeepHorizonOn";
        const string KeepHorizonOffArg = "KeepHorizonOff";

        IMyCockpit m_Cockpit;
        IMyTextSurface m_CockpitBatteryDamagePanel;
        List<IMyBatteryBlock> m_Batteries = new List<IMyBatteryBlock>();
        IMyTextSurface m_CockpitCargoPanel;
        List<IMyCargoContainer> m_Containers = new List<IMyCargoContainer>();
        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();
        List<IMyGyro> m_Gyros;
        private bool m_AutoHorizon = false;
        private IMyShipConnector m_Connector;
        private IMyCargoContainer m_MainCargoContainer = null;

        public Program()
        {
            m_Cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyCockpit;

            m_CockpitBatteryDamagePanel = m_Cockpit.GetSurface(IntegrityBatteryDisplayIndex);
            m_CockpitBatteryDamagePanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            GridTerminalSystem.GetBlocksOfType(m_Batteries, a => a is IMyBatteryBlock);

            m_CockpitCargoPanel = m_Cockpit.GetSurface(CargoCheckDisplayIndex);
            m_CockpitCargoPanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            GridTerminalSystem.GetBlocksOfType(m_Containers, a => a is IMyCargoContainer && a != m_Cockpit);

            m_Gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(m_Gyros, a => a.IsSameConstructAs(m_Cockpit));

            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks);
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            m_Connector = GridTerminalSystem.GetBlockWithName(ConnectorName) as IMyShipConnector;

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        void Main(string arg, UpdateType uType)
        {
            

            //Batteries
            float currentBatteryVolume = 0.0f;
            float maxBatteryVolume = 0.0f;
            foreach (var battery in m_Batteries)
            {
                currentBatteryVolume += battery.CurrentStoredPower;
                maxBatteryVolume += battery.MaxStoredPower;
            }
            var filledBattery = (double)((currentBatteryVolume / maxBatteryVolume) * 100);

            //IntegrityBatteryDisplay
            IntegrityBatteryDisplay(m_CockpitBatteryDamagePanel);

            //CargoDisplay
            CargoDisplay(m_CockpitCargoPanel);

            //KeepHorizon
            KeepHorizon(arg, ref m_AutoHorizon, m_Cockpit, m_Gyros);

            //UnloadCargo
            UnloadCargo();
        }

        private void UnloadCargo()
        {
            if (m_Connector.Status == MyShipConnectorStatus.Connected)
            {
                if (m_MainCargoContainer == null)
                    m_MainCargoContainer = GridTerminalSystem.GetBlockWithName(UnloadCargoName) as IMyCargoContainer;
                if (m_MainCargoContainer != null)
                {
                    var mainInventory = m_MainCargoContainer.GetInventory(0);
                    foreach (var pair in m_AllBlocks)
                    {
                        var container = pair.Key as IMyCargoContainer;
                        if (container != null)
                        {
                            var inventory = container.GetInventory(0);
                            while (inventory.ItemCount != 0)
                            {
                                mainInventory.TransferItemFrom(inventory, 0, null, true);
                            }
                        }
                    }
                }
            }
        }

        private void CargoDisplay(IMyTextSurface display)
        {
            double currentCargoVolume = .0;
            double maxCargoVolume = .0;
            double currentCargoMass = .0;
            foreach (var container in m_Containers)
            {
                var inv = container.GetInventory();
                maxCargoVolume += (double)(inv.MaxVolume);
                currentCargoVolume += (double)(inv.CurrentVolume);
                currentCargoMass += (double)inv.CurrentMass;
            }
            var filledVolume = (double)((currentCargoVolume / maxCargoVolume) * 100);
            var filledMass = (double)((currentCargoMass / (DiggyMassMax - DiggyMass)) * 100);

            display.BackgroundColor = new Color(Math.Min(255, (int)(filledMass * 2)), 100, 50);
            display.WriteText(string.Format(" VOL. : {0}% \n MASS: {1}% ", filledVolume.ToString("F2"), filledMass.ToString("F2")));
        }

        private void IntegrityBatteryDisplay(IMyTextSurface display)
        {
            string displayText;
            var damagedBlocks = new List<IMyTerminalBlock>();
            foreach (var pair in m_AllBlocks)
            {
                if (!pair.Value.IsFullIntegrity)
                {
                    damagedBlocks.Add(pair.Key);
                }
            }

            if (damagedBlocks.Count != 0)
            {
                display.BackgroundColor = Color.DarkRed;
                display.FontSize = 1;
                display.TextPadding = 1;
                displayText = "!!! Damaged Blocks !!!\n";
                foreach (var block in damagedBlocks)
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
                foreach (var battery in m_Batteries)
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

        public static void KeepHorizon(string arg, ref bool keepHorizon, IMyCockpit cockpit, List<IMyGyro> gyros)
        {
            switch (arg)
            {
                case KeepHorizonOffArg:
                    keepHorizon = false;
                    break;

                case KeepHorizonOnArg:
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