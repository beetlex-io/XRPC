using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace BeetleX.XRPC.Controllers
{
    public class ActionHandler
    {
        public ActionHandler(Type controllerType, MethodInfo method, object controller)
        {
            ControllerType = controllerType;
            Controller = controller;
            Method = method;
            MethodHandler = new MethodHandler(method);
            ResultType = method.ReturnType;
            PropertyInfo pi = method.ReturnType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            if (pi != null)
                ResultProperty = new PropertyHandler(pi);
            foreach (var p in method.GetParameters())
            {
                Parameters.Add(new ActionParameter(p));
            }
        }

        public bool SingleInstance { get; set; } = true;

        public bool IsTaskResult => ResultType.BaseType == typeof(Task);

        public bool IsVoid => ResultType == typeof(void);

        public string Url { get; set; }

        public Type Interface { get; set; }

        public object Controller { get; set; }

        public Type ControllerType { get; private set; }

        public MethodInfo Method { get; private set; }

        internal MethodHandler MethodHandler { get; private set; }

        public List<ActionParameter> Parameters { get; private set; } = new List<ActionParameter>();

        public Type ResultType { get; set; }

        internal PropertyHandler ResultProperty { get; set; }

        public object GetResult(object result)
        {
            if (IsVoid)
                return null;
            if (IsTaskResult)
            {
                if (ResultProperty != null)
                    return ResultProperty.Get(result);
                return null;
            }
            else
            {
                return result;
            }
        }

    }
}
