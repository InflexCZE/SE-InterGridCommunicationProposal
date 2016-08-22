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

namespace IGC_PB
{
    //#script begin

    public class MyIGCContext
    {
        public const string IGC_CONTEXT_GETTER = "IGC_GET_CONTEXT";

        public readonly long Address;

        public readonly Func<long, bool> IsReachable;
        public readonly Func<long, bool> IsValidEndpoint;

        public readonly Func<object[]> AcceptUnicastMessage;
        public readonly Func<string, object[]> AcceptBroadcastMessage;

        public readonly Action<string, string> SendBroadcastMessage;
        public readonly Func<string, long, bool> SendUnicastMessage;

        public readonly Func<string[]> GetBroadcastListeners;
        public readonly Action<string> DisableBroadcastListener;
        public readonly Action<string> RegisterBroadcastListener;
        public readonly Func<string, bool> IsBroadcastListenerActive;

        public readonly Func<bool> IsUnicastMessageWaiting;
        public readonly Func<string, bool> IsBroadcastMessageWaiting;

        public readonly Action<string> SetUnicastListenerCallback;
        public readonly Action<string, string> SetBroadcastListenerCallback;

        public MyIGCContext(IMyProgrammableBlock pb)
        {
            var IGCData = pb.GetProperty(IGC_CONTEXT_GETTER).As<object[]>().GetValue(pb);

            this.Address = (long) IGCData[0];
            this.IsReachable = (Func<long, bool>) IGCData[1];
            this.IsValidEndpoint = (Func<long, bool>) IGCData[2];
            this.AcceptUnicastMessage = (Func<object[]>) IGCData[3];
            this.AcceptBroadcastMessage = (Func<string, object[]>) IGCData[4];
            this.SendBroadcastMessage = (Action<string, string>) IGCData[5];
            this.SendUnicastMessage = (Func<string, long, bool>) IGCData[6];
            this.GetBroadcastListeners = (Func<string[]>) IGCData[7];
            this.DisableBroadcastListener = (Action<string>) IGCData[8];
            this.RegisterBroadcastListener = (Action<string>) IGCData[9];
            this.IsBroadcastListenerActive = (Func<string, bool>) IGCData[10];
            this.IsUnicastMessageWaiting = (Func<bool>) IGCData[11];
            this.IsBroadcastMessageWaiting = (Func<string, bool>) IGCData[12];
            this.SetUnicastListenerCallback = (Action<string>) IGCData[13];
            this.SetBroadcastListenerCallback = (Action<string, string>) IGCData[14];
        }
    }

    /// <summary>
    /// This is necessary static evil to make broadcast listeners persistent even upon <see cref="MyIntergridCommunicationSystem"/> context loss in potentially "broken" scripts.
    /// </summary>
    public static class MyBCListenerCache
    {
        private static
            Dictionary<IMyProgrammableBlock,
                Dictionary<string, IMyIGCBroadcastListener>> BCListeners
        = new Dictionary<IMyProgrammableBlock, Dictionary<string, IMyIGCBroadcastListener>>();

        private static Dictionary<string, IMyIGCBroadcastListener> GetBlockListeners(IMyProgrammableBlock block)
        {
            var blockListeners = BCListeners.GetValueOrDefault(block);

            // ReSharper disable once InvertIf
            if(blockListeners == null)
            {
                blockListeners = new Dictionary<string, IMyIGCBroadcastListener>();
                BCListeners.Add(block, blockListeners);
            }

            return blockListeners;
        }

        /// <summary>
        /// !!! Do not touch this !!!
        /// </summary>
        public static IMyIGCBroadcastListener EnsureBroadcastListener(IMyProgrammableBlock block, string tag, MyIGCContext childLock)
        {
            if(childLock == null)
                throw new ArgumentException("I sad do not touch this!");
                //throw new InvalidProgramException("I sad do not touch this!");

            var blockListeners = GetBlockListeners(block);
            var tagListener = blockListeners.GetValueOrDefault(tag);

            if(tagListener == null)
            {
                tagListener = new MyIGCBroadcastListener(childLock, tag);
                blockListeners.Add(tag, tagListener);
            }

            return tagListener;
        }

        /// <summary>
        /// !!! Do not touch this !!!
        /// </summary>
        public static void RegisterExistingBCListener(IMyProgrammableBlock block, string tag, MyIGCContext childLock)
        {
            if(childLock == null)
                throw new ArgumentException("I sad do not touch this!");
                //throw new InvalidProgramException("I sad do not touch this!");

            var blockListeners = GetBlockListeners(block);
            var tagListener = blockListeners.GetValueOrDefault(tag);

            // ReSharper disable once InvertIf
            if(tagListener == null)
            {
                tagListener = new MyIGCBroadcastListener(childLock, tag);
                blockListeners.Add(tag, tagListener);
            }
        }
    }

    public class MyIntergridCommunicationSystem : IMyIntergridCommunicationSystem
    {
        private readonly MyIGCContext Context;
        private readonly IMyProgrammableBlock ProgrammableBlock;

        public IMyIGCEndpoint Me { get; }
        public IMyUnicastListener UnicastListener { get; }

        private MyIntergridCommunicationSystem(IMyProgrammableBlock programmableBlock)
        {
            this.ProgrammableBlock = programmableBlock;
            this.Context = new MyIGCContext(programmableBlock);

            this.UnicastListener = new MyUnicastListener(this.Context);
            this.Me = new MyIGCEndpoint(this.Context, this.Context.Address);

            foreach(var tag in this.Context.GetBroadcastListeners())
                MyBCListenerCache.RegisterExistingBCListener(programmableBlock, tag, this.Context);
        }

        public IMyIGCEndpoint GetEndpointForAddress(long address)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if(this.Context.IsValidEndpoint(address) == false)
                return null;

            return new MyIGCEndpoint(this.Context, address);
        }

        public IMyIGCBroadcastListener RegisterBroadcastListener(string tag)
        {
            if(string.IsNullOrEmpty(tag))
                throw new ArgumentException(nameof(tag));

            var bcListener = MyBCListenerCache.EnsureBroadcastListener(this.ProgrammableBlock, tag, this.Context);

            if(bcListener.IsActive == false)
                this.Context.RegisterBroadcastListener(tag);

            return bcListener;
        }

        public void DisableBroadcastListener(IMyIGCBroadcastListener broadcastListener)
        {
            if(broadcastListener == null)
                throw new ArgumentNullException(nameof(broadcastListener));

            if(broadcastListener.GetType() != typeof(MyIGCBroadcastListener))
                throw new ArgumentException("Do not implement IMyIGCBroadcastListener on your own!");
                //throw new InvalidProgramException("Do not implement IMyIGCBroadcastListener on your own!");

                broadcastListener.DisableMessageCallback();
            this.Context.DisableBroadcastListener(broadcastListener.Tag);
        }

        public void GetBroadcastListeners(List<IMyIGCBroadcastListener> broadcastListeners, Func<IMyIGCBroadcastListener, bool> collect = null)
        {
            if(broadcastListeners == null)
                throw new ArgumentNullException(nameof(broadcastListeners));

            broadcastListeners.Clear();

            //LINQ is officially gray zone so I'm not gonna use it
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(var bclTag in this.Context.GetBroadcastListeners())
            {
                var bcListener = MyBCListenerCache.EnsureBroadcastListener(this.ProgrammableBlock, bclTag, this.Context);
                if(collect == null || collect(bcListener))
                    broadcastListeners.Add(bcListener);
            }
        }

        public void SendBroadcastMessage(string data, string tag)
        {
            if(string.IsNullOrEmpty(tag))
                throw new ArgumentException(nameof(tag));

            if(data == null)
                throw new ArgumentNullException(nameof(data));

            this.Context.SendBroadcastMessage(data, tag);
        }

        public bool SendUnicastMessage(string data, IMyIGCEndpoint addressee)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            if(addressee == null)
                throw new ArgumentNullException(nameof(addressee));

            if(addressee.GetType() != typeof(MyIGCEndpoint))
                throw new ArgumentException("Do not implement IMyIGCEndpoint on your own!");
                //throw new InvalidProgramException("Do not implement IMyIGCEndpoint on your own!");

            return this.Context.SendUnicastMessage(data, addressee.Address);
        }

        public static IMyIntergridCommunicationSystem Initialize(MyGridProgram @this)
        {
            if(@this == null)
                throw new ArgumentNullException(nameof(@this));

            return new MyIntergridCommunicationSystem(@this.Me);
        }

        /// <summary>
        /// !!Dangerous method!!
        /// For testing purposes only!
        /// </summary>
        public static IMyIntergridCommunicationSystem IReallyKnowWhatImDoingAndIWantContextForSpecificPB(IMyProgrammableBlock pb)
        {
            if(pb == null)
                throw new ArgumentNullException(nameof(pb));

            if(DateTime.Now > DateTime.MinValue)
                throw new InvalidOperationException("No, you don't know what are you doing!");

            return new MyIntergridCommunicationSystem(pb);
        }
    }

    public class MyIGCEndpoint : IMyIGCEndpoint
    {
        private readonly MyIGCContext Context;

        public long Address { get; }
        public bool IsReachable => this.Context.IsReachable(this.Address);

        public MyIGCEndpoint(MyIGCContext context, long address)
        {
            this.Context = context;
            this.Address = address;
        }

        public override int GetHashCode()
            => this.Address.GetHashCode();

        public override bool Equals(object obj)
            => this.Address == (obj as MyIGCEndpoint)?.Address;

        public override string ToString()
            => $"{{ Address: {this.Address}; IsReachable: {this.IsReachable}}}";
    }

    public class MyIGCMessage : IMyIGCMessage
    {
        public string Data { get; }
        public IMyIGCEndpoint Source { get; }

        public MyIGCMessage(IMyIGCEndpoint source, string data)
        {
            this.Data = data;
            this.Source = source;
        }

        public override string ToString()
            => $"{{ Data: {this.Data}; Source: {this.Source} }}";
    }

    public abstract class MyIGCMessageProvider : IMyIGCMessageProvider
    {
        protected MyIGCContext Context;

        public abstract bool IsMessageWaiting { get; }

        protected MyIGCMessageProvider(MyIGCContext context)
        {
            this.Context = context;
        }

        protected abstract object[] AcceptMessageData();

        public abstract void DisableMessageCallback();
        public abstract void SetMessageCallback(string argument = "");

        protected IMyIGCMessage MakeMessage(object[] messageData)
        {
            if(messageData == null)
                return null;

            var content = (string)messageData[0];
            var authorAddress = (long)messageData[1];
            var authorEndpoint = new MyIGCEndpoint(this.Context, authorAddress);
            return new MyIGCMessage(authorEndpoint, content);
        }

        public IMyIGCMessage AcceptMessage()
            => MakeMessage(AcceptMessageData());
    }

    public class MyIGCBroadcastListener : MyIGCMessageProvider, IMyIGCBroadcastListener
    {
        public string Tag { get; }
        public bool IsActive => base.Context.IsBroadcastListenerActive(this.Tag);
        public override bool IsMessageWaiting => base.Context.IsBroadcastMessageWaiting(this.Tag);

        public MyIGCBroadcastListener(MyIGCContext context, string tag) : 
            base(context)
        {
            this.Tag = tag;
        }

        protected override object[] AcceptMessageData()
            => base.Context.AcceptBroadcastMessage(this.Tag);

        public override void DisableMessageCallback() 
            => this.Context.SetBroadcastListenerCallback(this.Tag, null);

        public override void SetMessageCallback(string argument = "")
        {
            if(argument == null)
                throw new ArgumentNullException(nameof(argument));

            this.Context.SetBroadcastListenerCallback(this.Tag, argument);
        }
    }

    public class MyUnicastListener : MyIGCMessageProvider, IMyUnicastListener
    {
        public override bool IsMessageWaiting => base.Context.IsUnicastMessageWaiting();

        public MyUnicastListener(MyIGCContext context) :
            base(context)
        { }

        protected override object[] AcceptMessageData() 
            => base.Context.AcceptUnicastMessage();

        public override void DisableMessageCallback()
            => this.Context.SetUnicastListenerCallback(null);

        public override void SetMessageCallback(string argument = "")
        {
            if(argument == null)
                throw new ArgumentNullException(nameof(argument));

            this.Context.SetUnicastListenerCallback(argument);
        }
    }


    //#script end
}