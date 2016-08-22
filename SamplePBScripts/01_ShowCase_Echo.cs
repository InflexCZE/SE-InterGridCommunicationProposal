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
    // ReSharper disable once SuggestVarOrType_SimpleTypes
    public class ShowCase_Echo : MyGridProgram
    {
        //#script begin

        private IMyIntergridCommunicationSystem IGC { get; }

        public ShowCase_Echo() //#ctor
        {
            //This is the only case when you are allowed to touch MyIntergridCommunicationSystem.
            //It's necessary evil to make first instance of IGC implementation.
            //From now on use IMyIntergridCommunicationSystem interface only.
            this.IGC = MyIntergridCommunicationSystem.Initialize(this);
            
            //This is my communication endpoint
            IMyIGCEndpoint myIGCEndpoing = this.IGC.Me;

            //And others can contact me directly on this address
            long myAddress = myIGCEndpoing.Address;

            //This is your unicast listener. You should check your mail so it doesn't go to spam ;)
            IMyUnicastListener myUnicastListener = this.IGC.UnicastListener;

            //Let's setup message callback with empty argument
            myUnicastListener.SetMessageCallback();
        }

        //Activated message callback invokes this method each time we get new message
        public void Main()
        {
            //Check if there is message waiting
            if(this.IGC.UnicastListener.IsMessageWaiting == false)
                return;

            //Accept waiting message
            IMyIGCMessage message = this.IGC.UnicastListener.AcceptMessage();

            //And echo it right back to sender
            this.IGC.SendUnicastMessage(message.Data, message.Source);
        }

        //#include<PBInterface_Proposal.cs>
        //#include<PBImplementaion_Sample.cs>
        //#script end
    }
}
