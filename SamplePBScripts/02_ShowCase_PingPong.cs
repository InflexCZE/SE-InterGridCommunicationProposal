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
    public sealed class ShowCase_PingPong : MyGridProgram
    {
        //#script begin

        public const string MY_TAG = "PING";
        //private const string MY_TAG = "PONG";

        private long OtherEndpointAddress { get; set; } = -1;

        public const string MESSAGE_CALLBACK = "NEW_MESSAGE";

        public IMyTimerBlock Timer { get; }
        public IMyLightingBlock Light { get; }
        public IMyIntergridCommunicationSystem IGC { get; }

        public IMyTextPanel TextPanel { get; }
        public const int MaxTextFeedLength = 16;
        private Queue<string> TextFeed { get; } = new Queue<string>(MaxTextFeedLength);


        public ShowCase_PingPong() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);
            this.IGC.UnicastListener.SetMessageCallback(MESSAGE_CALLBACK);

            this.Timer = GetFirstBlockOfType<IMyTimerBlock>();
            this.TextPanel = GetFirstBlockOfType<IMyTextPanel>();
            this.Light = GetFirstBlockOfType<IMyLightingBlock>();

            if(string.IsNullOrEmpty(this.Storage))
            {//Lets do some initialization

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if(MY_TAG == "PING")
                    this.Timer.ApplyAction("TriggerNow");
            }
        }

        public void Main(string arg)
        {
            if(arg == MESSAGE_CALLBACK)
            {//I was called from message callback

                var message = this.IGC.UnicastListener.AcceptMessage();

                //Should NEVER happen
                if(message == null)
                    throw new Exception("Message was null while notified by message callback!");

                this.OtherEndpointAddress = message.Source.Address;
                WriteToLCD($"{message.Source.Address}: {message.Data}");

                this.Timer.ApplyAction("Start");
                this.Light.ApplyAction("OnOff_On");
            }
            else
            {//I was called from timer block

                if(this.OtherEndpointAddress == -1)
                    throw new Exception("Please set address of other endpoint!");

                var destination = this.IGC.GetEndpointForAddress(this.OtherEndpointAddress);

                if(destination == null)
                    throw new Exception("Destination address is invalid!");

                if(destination.IsReachable == false)
                {
                    //Other side is currently unavailable. Let's try it later
                    this.Timer.ApplyAction("Start");
                    return;
                }

                WriteToLCD($"Me: {MY_TAG}");
                this.Light.ApplyAction("OnOff_Off");
                this.IGC.SendUnicastMessage(MY_TAG, destination);
            }
        }

        public void Save()
        {
            this.Storage = "Initialized";
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