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

namespace SpaceEngineers.ToolieProg
{
    public sealed class Program : MyGridProgram
    {
        const string CockpitName = "Toolie Cockpit";
        const string ConnectorName = "Toolie Connector";
        const string BIOSBatteryName = "Toolie BIOS Battery";
        const string ToolieHingeName = "Toolie Hinge";
        const int IntegrityBatteryDisplayIndex = 0;
        const int CargoCheckDisplayIndex = 2;
        const string UnloadCargoName = "Alpha Main Cargo";

        const string KeepHorizonOnArg = "KeepHorizonOn";
        const string KeepHorizonOffArg = "KeepHorizonOff";

        IMyCockpit m_Cockpit;

        IMyTextSurface m_CockpitCargoPanel;

        IMyTextSurface m_CockpitBatteryDamagePanel;
        List<IMyBatteryBlock> m_Batteries = new List<IMyBatteryBlock>();
        IMyBatteryBlock m_BIOSBattery;
        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();

        List<IMyGyro> m_Gyros;
        bool m_AutoHorizon = true;

        IMyShipConnector m_Connector;
        IMyCargoContainer m_MainCargoContainer = null;
        List<IMyCargoContainer> m_Cargos = new List<IMyCargoContainer>();

        IMyMotorStator m_Hinge;

        double m_TargetToolsAngle = 0.0;

        public Program()
        {
            m_Cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyCockpit;

            m_CockpitBatteryDamagePanel = m_Cockpit.GetSurface(IntegrityBatteryDisplayIndex);
            m_CockpitBatteryDamagePanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            m_CockpitBatteryDamagePanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

            GridTerminalSystem.GetBlocksOfType(m_Batteries, a => a is IMyBatteryBlock && a.IsSameConstructAs(m_Cockpit) && a.CustomName != BIOSBatteryName);
            m_BIOSBattery = GridTerminalSystem.GetBlockWithName(BIOSBatteryName) as IMyBatteryBlock;

            GridTerminalSystem.GetBlocksOfType(m_Cargos, a => a is IMyCargoContainer && a.IsSameConstructAs(m_Cockpit));

            m_CockpitCargoPanel = m_Cockpit.GetSurface(CargoCheckDisplayIndex);
            m_CockpitCargoPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            m_CockpitCargoPanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            m_CockpitCargoPanel.FontSize = 2.5f;

            m_Gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(m_Gyros, a => a.IsSameConstructAs(m_Cockpit));

            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks, a => a.IsSameConstructAs(m_Cockpit));
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            m_Connector = GridTerminalSystem.GetBlockWithName(ConnectorName) as IMyShipConnector;

            m_Hinge = GridTerminalSystem.GetBlockWithName(ToolieHingeName) as IMyMotorStator;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void Main(string args, UpdateType uType)
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
            KeepHorizon(args, ref m_AutoHorizon, m_Cockpit, m_Gyros);

            //UnloadCargo
            //UnloadCargo();

            //RotateHands
            RotateHands(args);

            switch (m_Connector.Status)
            {
                case MyShipConnectorStatus.Connected:
                    m_Cockpit.DampenersOverride = false;
                    foreach (var battery in m_Batteries)
                    {
                        battery.ChargeMode = ChargeMode.Recharge;
                    }
                    m_BIOSBattery.ChargeMode = ChargeMode.Auto;
                    break;

                case MyShipConnectorStatus.Unconnected:
                case MyShipConnectorStatus.Connectable:
                    m_Cockpit.DampenersOverride = true;
                    foreach (var battery in m_Batteries)
                    {
                        battery.ChargeMode = ChargeMode.Auto;
                    }
                    m_BIOSBattery.ChargeMode = ChargeMode.Recharge;
                    break;
            }
        }

        private void RotateHands(string args)
        {
            double angleChange = 0.0;
            if (double.TryParse(args, out angleChange))
                m_TargetToolsAngle = angleChange;

            var angleDelta = m_TargetToolsAngle - m_Hinge.Angle / Math.PI * 180;
            if (Math.Abs(angleDelta) > 0.5)
            {
                var velocity = Math.Max(1f, (float)(Math.Abs(angleDelta) * 0.1));
                velocity = Math.Min(5f, velocity);
                velocity *= Math.Sign(angleDelta);
                m_Hinge.TargetVelocityRPM = velocity;
            }
            else
            {
                m_Hinge.TargetVelocityRPM = 0f;
            }
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
            var cargoDict = new Dictionary<MyItemType, MyFixedPoint>();
            foreach (var cargo in m_Cargos)
            {
                var items = new List<MyInventoryItem>();
                for (int i = 0; i < cargo.InventoryCount; i++)
                {
                    var inventory = cargo.GetInventory(i);
                    inventory.GetItems(items);
                }
                foreach (var item in items)
                {
                    if (!cargoDict.ContainsKey(item.Type))
                        cargoDict[item.Type] = item.Amount;
                    else
                        cargoDict[item.Type] += item.Amount;
                }
            }

            string textString = "=== CARGO ===\n";
            foreach (var pair in cargoDict)
            {
                textString += string.Format("{0}: {1}\n", pair.Key.SubtypeId, pair.Value.ToIntSafe());
            }
            m_CockpitCargoPanel.WriteText(textString);
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