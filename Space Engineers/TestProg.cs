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

namespace SpaceEngineers.TestProg
{
    public sealed class Program : MyGridProgram
    {
        public Program()
        {
            var thisBlock = GridTerminalSystem.GetBlockWithName("Test Frame") as IMyProgrammableBlock;
            var diggyCockpit = GridTerminalSystem.GetBlockWithName("Diggy Cockpit");

            Echo((thisBlock.CubeGrid == diggyCockpit.CubeGrid).ToString());
        }

        public void Main(string args)
        {

        }

        public void Save()
        { }
    }
}