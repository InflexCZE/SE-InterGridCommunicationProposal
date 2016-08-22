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
    public class ShowCase_StateBroadcastingDrone :MyGridProgram
    {
        //#script begin

        public const string DRONE_NAME = "Autonomous drone 4";
        public const string STATE_BROADCAST_TAG = "DRONE_STATE";

        public readonly Random Random = new Random();
        public readonly Vector3D GridSize = new Vector3D(-100, 100, 100);
        public readonly Vector3D BaseGridPosition = new Vector3D(-100, 10, 50);

        public IMyRemoteControl RemoteBlock { get; }
        public IMyIntergridCommunicationSystem IGC { get; }

        public Vector3D LastSpeed;

        public ShowCase_StateBroadcastingDrone() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);

            this.RemoteBlock = GetFirstBlockOfType<IMyRemoteControl>();
        }

        public void Main()
        {
            var currentSpeed = this.RemoteBlock.GetShipVelocities().LinearVelocity;
            var speedDifference = this.LastSpeed - currentSpeed;
            var acceleration = speedDifference * this.Runtime.TimeSinceLastRun.TotalSeconds;
            this.LastSpeed = currentSpeed;

            if(this.RemoteBlock.IsAutoPilotEnabled == false)
            {
                this.RemoteBlock.ClearWaypoints();
                var baseOffset = this.GridSize * new Vector3D(this.Random.NextDouble(), this.Random.NextDouble(), this.Random.NextDouble());
                this.RemoteBlock.AddWaypoint(this.BaseGridPosition + baseOffset, "Target");
                this.RemoteBlock.SetAutoPilotEnabled(true);
            }


            var nl = Environment.NewLine;
            this.IGC.SendBroadcastMessage($"{DRONE_NAME}{nl}{currentSpeed}{nl}{acceleration}", STATE_BROADCAST_TAG);
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