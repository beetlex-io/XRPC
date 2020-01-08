using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Clients
{
    public class ClientActionHandler
    {
        public ClientActionHandler(MethodInfo method)
        {
            this.MethodInfo = method;
            MethodHandler = new MethodHandler(method);
            ResultType = method.ReturnType;
            PropertyInfo pi = method.ReturnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            if (pi != null)
            {
                ResultProperty = new PropertyHandler(pi);
                ResultPropertyInfo = pi;
            }

            IsTaskResult = ResultType.BaseType == typeof(Task) || ResultType == typeof(Task);
            IsVoid = ResultType == typeof(void);
            ResponseType = GetResponseType();
        }

        public bool IsTaskResult { get; private set; }

        public bool IsVoid { get; private set; }

        public string Url { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public PropertyInfo ResultPropertyInfo { get; set; }

        internal MethodHandler MethodHandler { get; set; }

        public Type ResultType { get; set; }

        internal PropertyHandler ResultProperty { get; set; }

        public Type[] ResponseType { get; set; }

        private Type[] GetResponseType()
        {
            if (IsVoid)
                return new Type[0];
            else
            {
                if (IsTaskResult)
                {
                    if (ResultProperty == null)
                    {
                        return new Type[0];
                    }
                    else
                    {
                        return new Type[] { ResultProperty.Property.PropertyType };
                    }
                }
                else
                {
                    return new Type[] { ResultType };
                }
            }
        }

        private Type mCompletionSourceType;

        public object Execute(object controller, params object[] parameters)
        {
            return MethodHandler.Execute(controller, parameters);
        }

        internal IAnyCompletionSource GetCompletionSource()
        {
            if (mCompletionSourceType == null)
            {
                Type gtype = typeof(AnyCompletionSource<>);
                if (ResultProperty != null)
                    mCompletionSourceType = gtype.MakeGenericType(ResultProperty.Property.PropertyType);
                else
                    mCompletionSourceType = gtype.MakeGenericType(typeof(object));
            }
            return (IAnyCompletionSource)Activator.CreateInstance(mCompletionSourceType);
        }


    }
}
