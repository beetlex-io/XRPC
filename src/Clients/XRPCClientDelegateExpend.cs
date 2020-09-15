using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BeetleX.Clients;
using System.Linq;

namespace BeetleX.XRPC.Clients
{
    public partial class XRPCClient
    {
        public const string DELEGATE_TAG = "/__delegate/";

        public const string SUBSCRIBE_TAG = "/__subscribe/";

        public const string LOGIN_TAG = "/__login/";

        public string UserName { get; set; }

        public string PassWord { get; set; }

        protected virtual async Task OnLogin(AsyncTcpClient client)
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                RPCPacket packet = new RPCPacket();
                packet.Url = LOGIN_TAG;
                packet.Data = new object[] { UserName, PassWord };
                packet.NeedReply = true;
                var response = await SendWait(packet, client);
                if (response.Status != (short)StatusCode.SUCCESS)
                {
                    client.DisConnect();
                    throw new XRPCException((string)response.Data[0]);
                }
            }
            if (Connected != null)
            {
                await Connected(client);
            }
        }

        public Func<AsyncTcpClient, Task> Connected { get; set; }

        private async void InvokeDelegate(RPCPacket packet)
        {
            var path = packet.Url.SubRightWith('/', out string action);
            if (mDelegateHandlers.TryGetValue(action, out DelegateHandler handler))
            {
                try
                {
                    packet.LoadParameters(handler.Parameters);
                    object result = handler.Delegate.DynamicInvoke(packet.Data);
                    if (!handler.IsVoid)
                    {
                        await (Task)result;
                        if (handler.TargetReturnType != null)
                        {
                            var data = handler.GetValue(result);
                            packet.Reply(data);
                        }
                        else
                        {
                            packet.ReplySuccess();
                        }
                    }
                }
                catch (Exception e_)
                {
                    if (!handler.IsVoid)
                    {
                        packet.ReplyError((short)StatusCode.INNER_ERROR, $"{action} delegate invoke error {e_.Message}!");
                    }
                    else
                    {
                        OnError(packet.Client, new ClientErrorArgs { Error = e_, Message = $"{action} delegate invoke error {e_.Message}" });
                    }
                }
            }
            else
            {
                if (packet.NeedReply)
                {
                    packet.ReplyError((short)StatusCode.ACTION_NOT_FOUND, $"{action} delegate not found!");
                }
            }
        }

        private ConcurrentDictionary<string, DelegateHandler> mDelegateHandlers = new ConcurrentDictionary<string, DelegateHandler>(StringComparer.OrdinalIgnoreCase);

        private ConcurrentDictionary<string, DelegateHandler> mServerDelegates = new ConcurrentDictionary<string, DelegateHandler>(StringComparer.OrdinalIgnoreCase);

        public XRPCClient AddDelegate<T>(T handler) where T : Delegate
        {
            DelegateHandler item = new DelegateHandler(typeof(T));
            item.Delegate = handler;
            item.Init();
            mDelegateHandlers[item.Name] = item;
            return this;
        }

        public T Delegate<T>() where T : Delegate
        {
            DelegateHandler handler;
            var name = DelegateHandler.GetDelegateName(typeof(T));
            if (!mServerDelegates.TryGetValue(name, out handler))
            {
                handler = new DelegateHandler(typeof(T));
                handler.Init();
                handler.Bind(this);
                mServerDelegates[handler.Name] = handler;
            }
            return (T)handler.ClientDelegateProxy;
        }

        public async Task Subscribe<T>(T handler, AsyncTcpClient client = null) where T : Delegate
        {
            DelegateHandler item = new DelegateHandler(typeof(T));
            item.Delegate = handler;
            item.Init();
            if (!item.IsVoid)
                throw new XRPCException($"The subscribe delegate return value must be void!");
            if (item.Parameters == null || item.Parameters.Length == 0)
                throw new XRPCException($"The subscribe delegate parameters can't be empty!");
            RPCPacket packet = new RPCPacket();
            packet.Url = SUBSCRIBE_TAG + item.Name;
            packet.NeedReply = true;
            await SendWait(packet, client);
            mDelegateHandlers[item.Name] = item;
        }
    }

    class DelegateHandler
    {
        public DelegateHandler(Type type)
        {
            Type = type;
            Method = type.GetMethod("Invoke");
        }

        public static string GetDelegateName(Type type)
        {
            string name = type.ToString();
            int start = name.IndexOf(type.Namespace);
            if (start >= 0)
                name = name.Substring(start + type.Namespace.Length + 1);
            return name;
        }

        private PropertyInfo mResultProperty;

        private XRPCClient xRPCClient;

        public Type Type { get; set; }

        public string Name { get; set; }

        public bool IsVoid { get; set; }

        public Type TargetReturnType { get; set; }

        public Type[] GetReturnTypes()
        {
            if (TargetReturnType == null)
                return new Type[0];
            return new Type[] { TargetReturnType };

        }

        public Type[] Parameters { get; set; }

        public MethodInfo Method { get; set; }

        public Delegate Delegate { get; set; }

        public Delegate ClientDelegateProxy { get; set; }

        public object GetValue(object result)
        {
            if (mResultProperty != null)
                return mResultProperty.GetValue(result);
            return null;
        }

        public void Init()
        {
            Name = GetDelegateName(Type);
            Parameters = (from a in Method.GetParameters() select a.ParameterType).ToArray();
            foreach (var item in Method.GetParameters())
            {
                if (item.ParameterType == typeof(object))
                {
                    throw new XRPCException($"{item.Name} parameters cannot be object type");
                }
            }
            if (Method.ReturnType == typeof(void))
            {
                IsVoid = true;
            }
            else
            {
                mResultProperty = Method.ReturnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);

                if (Method.ReturnType == typeof(Task) || Method.ReturnType.BaseType == typeof(Task))
                {
                    if (Method.ReturnType.IsGenericType)
                    {
                        TargetReturnType = Method.ReturnType.GetGenericArguments()[0];
                    }
                }
                else
                {
                    throw new XRPCException("The method return value must be void or task!");
                }
            }
        }

        public void Invoke()
        {
            OnVoidExecute();
        }

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


        public async Task TaskInvoke()
        {
            await OnTaskExecute<RESULT_NULL>();
        }

        public async Task TaskInvoke<T>(T p1)
        {
            await OnTaskExecute<RESULT_NULL>(p1);
        }

        public async Task TaskInvoke<T, T1>(T p1, T1 p2)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2);
        }

        public async Task TaskInvoke<T, T1, T2>(T p1, T1 p2, T2 p3)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3);
        }

        public async Task TaskInvoke<T, T1, T2, T3>(T p1, T1 p2, T2 p3, T3 p4)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4>(T p1, T1 p2, T2 p3, T3 p4, T4 p5)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4, T5>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5, p6);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4, T5, T6>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5, p6, p7);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4, T5, T6, T7>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5, p6, p7, p8);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4, T5, T6, T7, T8>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }

        public async Task TaskInvoke<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9, T9 p10)
        {
            await OnTaskExecute<RESULT_NULL>(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        }


        public Task<Result> TaskResultInvoke<Result>()
        {
            return OnTaskExecute<Result>();
        }

        public Task<Result> TaskResultInvoke<Result, T>(T p1)
        {
            return OnTaskExecute<Result>(p1);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1>(T p1, T1 p2)
        {
            return OnTaskExecute<Result>(p1, p2);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2>(T p1, T1 p2, T2 p3)
        {
            return OnTaskExecute<Result>(p1, p2, p3);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3>(T p1, T1 p2, T2 p3, T3 p4)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4>(T p1, T1 p2, T2 p3, T3 p4, T4 p5)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4, T5>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5, p6);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4, T5, T6>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5, p6, p7);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4, T5, T6, T7>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5, p6, p7, p8);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4, T5, T6, T7, T8>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }

        public Task<Result> TaskResultInvoke<Result, T, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T p1, T1 p2, T2 p3, T3 p4, T4 p5, T5 p6, T6 p7, T7 p8, T8 p9, T9 p10)
        {
            return OnTaskExecute<Result>(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
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

        protected Delegate CreateDelegate()
        {
            Delegate result;
            var parameters = (from a in Method.GetParameters() select a.ParameterType).ToList();
            if (IsVoid)
            {
                var method = GetMethod("Invoke", parameters.Count);
                var methodimpl = parameters.Count > 0 ? method.MakeGenericMethod(parameters.ToArray()) : method;
                result = Delegate.CreateDelegate(Type, this, methodimpl);
            }
            else
            {
                if (TargetReturnType == null)
                {
                    var method = GetMethod("TaskInvoke", parameters.Count);
                    var methodimpl = parameters.Count > 0 ? method.MakeGenericMethod(parameters.ToArray()) : method;
                    result = Delegate.CreateDelegate(Type, this, methodimpl);
                }
                else
                {
                    var method = GetMethod("TaskResultInvoke", parameters.Count);
                    parameters.Insert(0, TargetReturnType);
                    var methodimpl = method.MakeGenericMethod(parameters.ToArray());
                    result = Delegate.CreateDelegate(Type, this, methodimpl);
                }
            }
            return result;
        }

        protected virtual Task OnVoidExecute(params object[] data)
        {
            RPCPacket packet = new RPCPacket();
            packet.NeedReply = false;
            packet.Url = Clients.XRPCClient.DELEGATE_TAG + Name;
            packet.Data = data;
            return xRPCClient.Send(packet, null);
        }

        protected virtual async Task<T> OnTaskExecute<T>(params object[] data)
        {
            bool istask = typeof(T) == typeof(RESULT_NULL);
            RPCPacket packet = new RPCPacket();
            packet.NeedReply = true;
            packet.Url = Clients.XRPCClient.DELEGATE_TAG + Name;
            packet.Data = data;
            var returltype = GetReturnTypes();
            var result = await xRPCClient.SendWait(packet, null, returltype);
            if (result.Status != (short)StatusCode.SUCCESS)
            {
                throw new XRPCException((string)result.Data[0]);
            }
            if (istask)
                return (T)(object)new RESULT_NULL();
            return (T)result.Data[0];
        }

        public void Bind(XRPCClient client)
        {
            ClientDelegateProxy = CreateDelegate();
            xRPCClient = client;
        }
    }

    struct RESULT_NULL
    {

    }
}
