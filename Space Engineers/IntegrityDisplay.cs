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

namespace SpaceEngineers.Integrity
{
    public sealed class Program : MyGridProgram
    {
        //Константы экрана:
        //Имя блока с экраном (блок экрана или блок кокпита/кресла с ними)
        const string DisplayProviderName = "";
        //Номер экрана (если выше указан блок LCD экрана, эта константа должна быть 0)
        const int DisplayIndex = 0;

        //Размер шрифта информации о повреждениях
        const float IntegrityFontSize = 1f;
        //Вертикальный отступ информации о повреждениях
        const float IntegrityPadding = 1f;

        //Цвет фона при отображении информации об отсутствии повреждений
        const int BackgroundRed = 0;
        const int BackgroundGreen = 100;
        const int BackgroundBlue = 200;

        //Дальше ничего менять не нужно
        IMyTextSurface m_BatteryIntegrityDisplay = null;

        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();
        Dictionary<IMyTerminalBlock, bool> m_DamagedBlocks = new Dictionary<IMyTerminalBlock, bool>();

        public Program()
        {
            var displayBlock = GridTerminalSystem.GetBlockWithName(DisplayProviderName);
            var provider = displayBlock as IMyTextSurfaceProvider;
            if (provider != null)
            {
                if (provider.SurfaceCount <= DisplayIndex)
                {
                    Echo("Неверно указан индекс дисплея!");
                    return;
                }
                m_BatteryIntegrityDisplay = provider.GetSurface(DisplayIndex);
            }
            if (m_BatteryIntegrityDisplay == null)
            {
                var surface = displayBlock as IMyTextSurface;
                if (surface != null)
                    m_BatteryIntegrityDisplay = surface;
            }
            if (m_BatteryIntegrityDisplay == null)
            {
                Echo("Не удалось найти экран!");
                return;
            }

            var me = Me;

            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks, a => a .IsSameConstructAs(me));
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            if (m_BatteryIntegrityDisplay == null)
            {
                Echo("Не удалось найти экран!");
                return;
            }

            IntegrityDisplay(m_BatteryIntegrityDisplay, m_AllBlocks, m_DamagedBlocks);
        }

        private static void IntegrityDisplay(IMyTextSurface display, List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> allBlocks, Dictionary<IMyTerminalBlock, bool> damagedBlocks)
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
                display.FontSize = IntegrityFontSize;
                display.TextPadding = IntegrityPadding;
                displayText = "!!! Damaged Blocks !!!\n";
                foreach (var block in showDamaged)
                {
                    block.ShowOnHUD = true;
                    displayText += string.Format("{0}\n", block.CustomName);
                }
            }
            else
            {
                display.BackgroundColor = new Color(BackgroundRed, BackgroundGreen, BackgroundBlue);
                displayText = "All blocks undamaged =)";
            }

            display.WriteText(displayText);
        }
    }
}