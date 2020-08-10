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

namespace SpaceEngineers.AccidentTimer
{
    public sealed class Program : MyGridProgram
    {
        const string AccidentLCD = "Factory Accident LCD";
        const string AccidentTimer = "Factory Accident Timer";

        IMyTextPanel m_LcdAccident;
        IMyTimerBlock m_TimerAccident;
        DateTime? m_LastAccident = null;

        public Program()
        {
            m_LcdAccident = GridTerminalSystem.GetBlockWithName(AccidentLCD) as IMyTextPanel;
            m_TimerAccident = GridTerminalSystem.GetBlockWithName(AccidentTimer) as IMyTimerBlock;
            if (Storage.Length > 0)
            {
                m_LastAccident = DateTime.Parse(Storage);
            }
        }

        public void Main(string args)
        {
            switch (args)
            {
                case "Accident!":
                    m_LcdAccident.CustomData = DateTime.Now.ToString();
                    m_LastAccident = DateTime.Now;
                    break;
            }

            if (m_LcdAccident.CustomData == string.Empty)
            {
                if (m_LastAccident.HasValue)
                    m_LcdAccident.CustomData = m_LastAccident.Value.ToString();
                else
                    m_LcdAccident.CustomData = DateTime.Now.ToString();
            }
            if (!m_LastAccident.HasValue)
                m_LastAccident = DateTime.Parse(m_LcdAccident.CustomData);

            var lastAccidentTime = DateTime.Now - m_LastAccident.Value;
            var min = lastAccidentTime.Minutes.ToString();
            if (min.Length == 1)
                min = "0" + min;
            var sec = lastAccidentTime.Seconds.ToString();
            if (sec.Length == 1)
                sec = "0" + sec;
            m_LcdAccident.WriteText(string.Format("БЕЗ \n ИНЦИДЕНТОВ: \n {0}:{1}:{2} ", (int)lastAccidentTime.TotalHours, min, sec));

            m_TimerAccident.StartCountdown();
        }

        public void Save()
        {
            Storage = m_LastAccident.ToString();
        }
    }
}