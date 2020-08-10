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

namespace SpaceEngineers.Battery
{
    public sealed class Program : MyGridProgram
    {
        //Константы экрана:
        //Имя блока с экраном (блок экрана или блок кокпита/кресла с ними)
        const string DisplayProviderName = "";
        //Номер экрана (если выше указан блок LCD экрана, эта константа должна быть 0)
        const int DisplayIndex = 0;

        //Размер шрифта информации по батареям
        const float BatteryFontSize = 3f;
        //Вертикальный отступ информации по батареям
        const float BatteryPadding = 10f;

        //Цвет фона при отображении информации по батареям
        const int BackgroundRed = 0;
        const int BackgroundGreen = 100;
        const int BackgroundBlue = 200;

        //Дальше ничего менять не нужно
        IMyTextSurface m_BatteryIntegrityDisplay = null;
        List<IMyBatteryBlock> m_Batteries = new List<IMyBatteryBlock>();

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

            GridTerminalSystem.GetBlocksOfType(m_Batteries, a => a is IMyBatteryBlock && a.IsSameConstructAs(me));

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            if (m_BatteryIntegrityDisplay == null)
            {
                Echo("Не удалось найти экран!");
                return;
            }

            BatteryDisplay(m_BatteryIntegrityDisplay, m_Batteries);
        }

        private static void BatteryDisplay(IMyTextSurface display, List<IMyBatteryBlock> batteries)
        {
            string displayText;

            display.BackgroundColor = new Color(BackgroundRed, BackgroundGreen, BackgroundBlue);
            display.FontSize = BatteryFontSize;
            display.TextPadding = BatteryPadding;
            float currentBatteryVolume = 0.0f;
            float maxBatteryVolume = 0.0f;
            float currentBatteryInput = 0.0f;
            float currentBatteryOutput = 0.0f;
            foreach (var battery in batteries)
            {
                currentBatteryVolume += battery.CurrentStoredPower;
                maxBatteryVolume += battery.MaxStoredPower;
                currentBatteryInput += battery.CurrentInput;
                currentBatteryOutput += battery.CurrentOutput;

                if (battery.ChargeMode != ChargeMode.Auto)
                    display.BackgroundColor = Color.DarkRed;
            }
            var filledBattery = (double)((currentBatteryVolume / maxBatteryVolume) * 100);
            if (filledBattery < 20)
                display.BackgroundColor = Color.DarkRed;

            var filledBatteryString = filledBattery.ToString("F2");
            var currentBattery = currentBatteryVolume.ToString("F3") + " MWh";
            var maxBattery = maxBatteryVolume.ToString("F3") + " MWh";
            var batteryInOut = currentBatteryInput - currentBatteryOutput;
            var batteryInOutString = (batteryInOut).ToString("F3") + " MW";
            if (batteryInOut > 0)
                batteryInOutString = "+" + batteryInOutString;
            displayText = string.Format(" BATTERY \n {0}% \n C:{1} \n M:{2} \n\n {3} ", filledBatteryString, currentBattery, maxBattery, batteryInOutString);

            display.WriteText(displayText);
        }
    }
}