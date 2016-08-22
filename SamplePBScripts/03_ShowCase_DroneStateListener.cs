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
    public sealed class ShowCase_DroneStateListener : MyGridProgram
    {
        //#script begin

        public const string STATE_BROADCAST_TAG = "DRONE_STATE";

        public IMyTextPanel TextPanel { get; }
        public IMyIntergridCommunicationSystem IGC { get; }
        public IMyIGCBroadcastListener BroadcastListener { get; }
        public Dictionary<string, DroneInfo> Drones { get; } = new Dictionary<string, DroneInfo>();

        public struct DroneInfo
        {
            public string Name { get; set; }
            public Vector3D Velocity { get; set; }
            public Vector3D Acceleration { get; set; }

            public override string ToString()
                => this.Name                                                + Environment.NewLine +
                   $"Velocity: {this.Velocity.ToString("F2")} m/s"          + Environment.NewLine +
                   $"Acceleration: {this.Acceleration.ToString("F2")} m/s2" + Environment.NewLine;
        }

        public ShowCase_DroneStateListener() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);

            var lcds = new List<IMyTextPanel>();
            this.GridTerminalSystem.GetBlocksOfType(lcds);

            if(lcds.Count == 0)
                throw new Exception("No LCD found!");

            //Get closest text panel to this programmable block
            double distance = double.MaxValue;
            foreach(var x in lcds)
            {
                var lcdDst = Vector3D.Distance(x.GetPosition(), this.Me.GetPosition());
                if(lcdDst < distance)
                {
                    distance = lcdDst;
                    this.TextPanel = x;
                }
            }

            this.BroadcastListener = this.IGC.RegisterBroadcastListener(STATE_BROADCAST_TAG);
            this.BroadcastListener.SetMessageCallback();
        }

        public void Main()
        {
            while(this.BroadcastListener.IsMessageWaiting)
            {
                var messageData = this.BroadcastListener.AcceptMessage().Data.Split(new[] {Environment.NewLine }, StringSplitOptions.None);
                var droneInfo = new DroneInfo
                {
                    Name = messageData[0],
                    Velocity = ParseVector(messageData[1]),
                    Acceleration = ParseVector(messageData[2]),
                };

                this.Drones[droneInfo.Name] = droneInfo;
            }

            var sb = new StringBuilder();
            foreach(var x in this.Drones.Values)
                sb.AppendLine(x.ToString());

            this.TextPanel.WritePublicText(sb.ToString());
        }

        private static Vector3D ParseVector(string s)
        {
            Vector3D vec;
            Vector3D.TryParse(s, out vec);
            return vec;
        }

        //#include<IGC.cs>
        //#script end
    }

}