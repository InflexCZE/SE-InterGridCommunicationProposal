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
    public class ShowCase_RemoteControlledDrone : MyGridProgram
    {
        //#script begin

        public const string DRONE_NAME = "Remote controlled drone 1";

        public const string MESSAGE_CALLBACK = "NEW_MESSAGE";
        public const string REMOTE_CONTROLLER_PROBE_TAG = "WHO_IS_REMOTE_CONTROLLER";

        public IMyTimerBlock Timer { get; }
        public IMyRemoteControl AutopilotBlock { get; }
        public IMyIntergridCommunicationSystem IGC { get; }

        private long RemoteControllerAddress = -1;

        public ShowCase_RemoteControlledDrone() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);
            this.IGC.UnicastListener.SetMessageCallback(MESSAGE_CALLBACK);

            this.Timer = GetFirstBlockOfType<IMyTimerBlock>();
            this.AutopilotBlock = GetFirstBlockOfType<IMyRemoteControl>();

            this.Timer.ApplyAction("Start");
        }

        public void Main(string arg)
        {
            if(arg == MESSAGE_CALLBACK)
            {
                AcceptMessage();
            }

            if(this.AutopilotBlock.IsAutoPilotEnabled)
                return;

            var controllerEndpoint = this.IGC.GetEndpointForAddress(this.RemoteControllerAddress);
            if(controllerEndpoint == null || controllerEndpoint.IsReachable == false)
            {//Controller address is no longer valid or is currently unavailable. Lets ask for new one.
                this.IGC.SendBroadcastMessage(DRONE_NAME, REMOTE_CONTROLLER_PROBE_TAG);
                return;
            }

            //Ask for new target
            this.IGC.SendUnicastMessage(DRONE_NAME, controllerEndpoint);
        }

        void AcceptMessage()
        {
            var message = this.IGC.UnicastListener.AcceptMessage();
            this.RemoteControllerAddress = message.Source.Address;

            if(string.IsNullOrWhiteSpace(message.Data))
                return;

            Vector3D target;
            if(Vector3D.TryParse(message.Data, out target) == false)
                return;

            this.AutopilotBlock.ClearWaypoints();
            this.AutopilotBlock.AddWaypoint(target, "Target");
            this.AutopilotBlock.SetAutoPilotEnabled(true);
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