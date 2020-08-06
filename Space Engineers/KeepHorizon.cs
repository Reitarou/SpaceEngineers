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
using System.Linq;

namespace SpaceEngineers.KeepHorizon
{
    public sealed class Program : MyGridProgram
    {
        IMyShipController m_Cockpit;

        List<IMyGyro> m_Gyros;
        private bool m_AutoHorizon = false;

        public Program()
        {
            var cockpits = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(cockpits);
            m_Cockpit = cockpits.First();

            m_Gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(m_Gyros, a => a.IsSameConstructAs(m_Cockpit));
        }

        void Main(string args)
        {
            //KeepHorizon
            KeepHorizon(args, ref m_AutoHorizon, m_Cockpit, m_Gyros);
        }

        public static void KeepHorizon(string args, ref bool keepHorizon, IMyShipController cockpit, List<IMyGyro> gyros)
        {
            switch (args)
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