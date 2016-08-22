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
    public sealed class ShowCase_DroneRemoteController : MyGridProgram
    {
        //#script begin

        public const string REMOTE_CONTROLLER_PROBE_TAG = "WHO_IS_REMOTE_CONTROLLER";

        public readonly Random Random = new Random();
        public readonly Vector3D GridSize = new Vector3D(-100, 100, -100);
        public readonly Vector3D BaseGridPosition = new Vector3D(-100, 10, 50);

        public IMyTextPanel TextPanel { get; }
        public IMyIntergridCommunicationSystem IGC { get; }
        public IMyIGCBroadcastListener BroadcastListener { get; }

        public const int MaxTextFeedLength = 16;
        private Queue<string> TextFeed { get; } = new Queue<string>(MaxTextFeedLength);

        public ShowCase_DroneRemoteController() //#ctor
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

            this.BroadcastListener = this.IGC.RegisterBroadcastListener(REMOTE_CONTROLLER_PROBE_TAG);
            this.IGC.UnicastListener.SetMessageCallback();
            this.BroadcastListener.SetMessageCallback();
        }

        public void Main()
        {
            IMyIGCMessage message;
            while((message = this.BroadcastListener.AcceptMessage()) != null)
            {//Someone is asking for my address.
                this.IGC.SendUnicastMessage("", message.Source);
                WriteToLCD($"Sent my address to {message.Data}");
            }

            while((message = this.IGC.UnicastListener.AcceptMessage()) != null)
            {//Someone is asking for new target
                var baseOffset = this.GridSize * new Vector3D(this.Random.NextDouble(), this.Random.NextDouble(), this.Random.NextDouble());
                var newTarget = this.BaseGridPosition + baseOffset;
                this.IGC.SendUnicastMessage(newTarget.ToString(), message.Source);

                WriteToLCD($"{message.Data} sent to");
                WriteToLCD(newTarget.ToString("F2"));
            }
        }

        private void WriteToLCD(string line)
        {
            if(this.TextFeed.Count >= MaxTextFeedLength)
                this.TextFeed.Dequeue();

            this.TextFeed.Enqueue(line);

            var sb = new StringBuilder();
            sb.AppendLine($"My address is: \"{this.IGC.Me.Address}\"");

            foreach(var x in this.TextFeed)
                sb.AppendLine(x);

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