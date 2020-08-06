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

namespace SpaceEngineers.Crane
{
    public sealed class Program : MyGridProgram
    {
        // НАЧАЛО СКРИПТА
        IMyCockpit Cockpit;
        List<IMyPistonBase> VerticalPistons = new List<IMyPistonBase>();
        List<IMyPistonBase> HorizontalPistons = new List<IMyPistonBase>();
        IMyMotorStator Stator;
        double TargetRotation = 0;
        double TargetHeight = 0;
        double TargetExtend = 0;
        double Speed = 1;
        bool rotationClockwise;
        ValueComparer FloatComparer01 = new ValueComparer(0.1);

        public Program()
        {
            Cockpit = GridTerminalSystem.GetBlockWithName("Factory Crane Cockpit") as IMyCockpit;
            GridTerminalSystem.GetBlockGroupWithName("Factory Crane Pistons Vertical").GetBlocksOfType(VerticalPistons);
            GridTerminalSystem.GetBlockGroupWithName("Factory Crane Pistons Horizontal").GetBlocksOfType(HorizontalPistons);
            Stator = GridTerminalSystem.GetBlockWithName("Factory Crane Rotor") as IMyMotorStator;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string args)
        {
            var x = Cockpit.MoveIndicator.X;
            var y = Cockpit.MoveIndicator.Y;
            var z = Cockpit.MoveIndicator.Z;
            var r = Cockpit.RollIndicator;

            Speed = Math.Max(1, Speed + r);
            Speed = Math.Min(10, Speed + r);

            var rotation = Stator.Angle * 180 / Math.PI;
            TargetRotation = TargetRotation + Speed * x;
            TargetHeight = Math.Max(-180, Speed + r);
            TargetHeight = Math.Min(180, Speed + r);

            if (FloatComparer01.Compare(TargetRotation, rotation) == 0)
            {
                Stator.Enabled = false;
                Echo("Rotation stopped");
            }
            else
            {
                Stator.TargetVelocityRPM = (float)((TargetRotation - rotation) * 0.1);
                Echo(string.Format("TR: {0}\nR: {1}\nS: {2}", TargetRotation, rotation, Stator.TargetVelocityRPM));
                Stator.Enabled = true;
            }

            TargetHeight = Math.Max(1, Speed + r);
            TargetHeight = Math.Min(10, Speed + r);

            TargetExtend = Math.Max(1, Speed + r);
            TargetExtend = Math.Min(10, Speed + r);

            var height = 0.0;
            foreach (var piston in VerticalPistons)
            {
                height = piston.CurrentPosition;
            }

            var rotationString = (rotation % 360.0).ToString("F0");

            Cockpit.GetSurface(4).WriteText(string.Format(" Rotation: {0}/{1} \n Y: {2} \n Z: {3} \n Speed:{4} ", rotationString, TargetRotation, y, z, Speed));
        }

        public void Save()
        { }

        private class ValueComparer : IComparer<double>
        {
            double m_Eps;

            public ValueComparer(double eps)
            {
                m_Eps = eps;
            }

            public int Compare(double x, double y)
            {
                var dif = Math.Abs(x - y);
                if (dif < m_Eps)
                    return 0;
                return x.CompareTo(y);
            }
        }
        // КОНЕЦ СКРИПТА
    }
}