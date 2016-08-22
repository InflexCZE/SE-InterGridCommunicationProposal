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
    public class ShowCase_Test : MyGridProgram
    {
        //#script begin
        public IMyIntergridCommunicationSystem IGC { get; }

        public ShowCase_Test() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //It's necessary evil to make first instance of IGC implematation instances.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);

            this.Echo($"My address: {this.IGC.Me.Address}");

            var bcListeners = new List<IMyIGCBroadcastListener>();
            this.IGC.GetBroadcastListeners(bcListeners);

            foreach(var bcListener in bcListeners)
                this.Echo(bcListener.Tag);

            IMyIGCMessage message;
            while((message = this.IGC.UnicastListener.AcceptMessage()) != null)
                this.Echo($"Message:{message.Data}");

        }

        void Main(string arg)
        {
            this.Echo($"Arg: {arg}");

            switch(arg.ToUpper())
            {
                case "SEND":
                    this.IGC.UnicastListener.SetMessageCallback("CALLBACK");
                    this.IGC.SendUnicastMessage(DateTime.Now.ToString(), this.IGC.Me);
                    
                    break;

                case "SETUP":
                {
                    var bcListener = this.IGC.RegisterBroadcastListener("TAG");
                    bcListener.SetMessageCallback("CALLBACK");
                }
                break;

                case "ACCEPT":
                case "CALLBACK":
                {
                        this.Echo($"IsMessageWaiting: {this.IGC.UnicastListener.IsMessageWaiting}");
                    break;

                        var message = this.IGC.UnicastListener.AcceptMessage();
                    if(message == null)
                        this.Echo("Message is null!");

                    this.Echo($"Message: {message}");
                }
                break;
            }
        }

        //#include<IGC.cs>
        //#script end
    }
}