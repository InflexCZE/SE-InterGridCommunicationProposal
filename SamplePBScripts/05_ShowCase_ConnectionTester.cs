using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

using IGC_PB;

namespace SamplePBScripts
{
    public sealed class ShowCase_ConnectionTester : MyGridProgram
    {
        //#script begin

        const long TestedEndpointAddress = -1;

        IMyLightingBlock Light { get; }
        IMyIntergridCommunicationSystem IGC { get; }

        public ShowCase_ConnectionTester() //#ctor
        {
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);
            this.Light = GetFirstBlockOfType<IMyLightingBlock>();

            var timers = new List<IMyTimerBlock>();
            this.GridTerminalSystem.GetBlocksOfType(timers);

            foreach(var x in timers)
                x.ApplyAction("TriggerNow");
        }

        public void Main()
        {
            this.Echo($"My address is {this.IGC.Me.Address}");
            this.Light.SetValueColor("Color", IsTargetReachable() ? Color.Green : Color.Red);
        }

        private bool IsTargetReachable()
        {
            var endpoint = this.IGC.GetEndpointForAddress(TestedEndpointAddress);
            return endpoint != null && endpoint.IsReachable;
        }

        private BlockT GetFirstBlockOfType<BlockT>() where BlockT : class
        {
            var blocks = new List<BlockT>();
            this.GridTerminalSystem.GetBlocksOfType(blocks);

            if(blocks.Count == 0)
                throw new InvalidOperationException($"No block of type {typeof(BlockT).Name} found!");

            return blocks[0];
        }

        //#include<IGC.cs>
        //#script end
    }

}