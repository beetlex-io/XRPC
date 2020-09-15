using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
namespace BeetleX.XRPC
{
    public class DelegatePublisher
    {

        public DelegatePublisher(string name)
        {
            Name = name;
        }

        private ConcurrentDictionary<long, ISession> mSessions = new ConcurrentDictionary<long, ISession>();

        public string Name { get; set; }

        public Delegate Delegate { get; set; }

        public void Invoke<T>(T p1)
        {
            OnVoidExecute(p1);
        }

        public void Invoke<T, T1>(T p1, T1 p2)
        {
            OnVoidExecute(p1, p2);
        }

        public void Invoke<T, T1, T2>(T p1, T1 p2, T2 p3)
        {
            OnVoidExecute(p1, p2, p3);
        }

        public void Invoke<T, T1, T2, T3>(T p1, T1 p2, T2 p3, T3 p4)
        {
            OnVoidExecute(p1, p2, p3, p4);
        }

        public void Invoke<T, T1, T2, T3, T4>(T p1, T1 p2, T2 p3, T3 p4, T4 p5)
        {
            OnVoidExecute(p1, p2, p3, p4, p5);
        }

        public void Invoke<T, T1, T2, T3, T4, T5>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6)
        {
            OnVoidExecute(p1, p2, p3, p4, p5, p6);
        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7)
        {
            OnVoidExecute(p1, p2, p3, p4, p5, p6, p7);
        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6, T7>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8)
        {
            OnVoidExecute(p1, p2, p3, p4, p5, p6, p7, p8);
        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6, T7, T8>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9)
        {
            OnVoidExecute(p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }

        public void Invoke<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9, T9 p10)
        {
            OnVoidExecute(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        }

        public System.Reflection.MethodInfo GetMethod(string method, int parameters)
        {
            var methods = this.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var item in methods)
            {
                if (item.GetParameters().Length == parameters && item.Name.IndexOf(method) == 0)
                    return item;
            }
            throw new XRPCException("Delegate proxy method not support!");
        }

        private ISession[] Sessions =new ISession[0];

        public void Add(ISession session)
        {
            mSessions[session.ID] = session;
            Sessions = mSessions.Values.ToArray();
        }

        public void Remove(ISession session)
        {
            mSessions.TryRemove(session.ID, out ISession delete);
            Sessions = mSessions.Values.ToArray();
        }

        public Delegate CreateDelegate(Type type)
        {
            if (this.Delegate == null)
            {
                var invokeMethod = type.GetMethod("Invoke");
                var parameters = (from a in invokeMethod.GetParameters() select a.ParameterType).ToList();
                var method = GetMethod("Invoke", parameters.Count);
                var methodimpl = parameters.Count > 0 ? method.MakeGenericMethod(parameters.ToArray()) : method;
                Delegate = Delegate.CreateDelegate(type, this, methodimpl);
            }
            return this.Delegate;
        }

        protected virtual void OnVoidExecute(params object[] data)
        {
            RPCPacket packet = new RPCPacket();
            packet.NeedReply = false;
            packet.Url = Clients.XRPCClient.DELEGATE_TAG + Name;
            packet.Data = data;
            var sessions = Sessions;
            foreach(var item in sessions)
            {
                if(item.IsDisposed)
                {
                    Remove(item);
                }
                else
                {
                    item.Send(packet);
                }
            }
        }
    }
}
