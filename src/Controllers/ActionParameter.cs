using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeetleX.XRPC.Controllers
{
    public class ActionParameter
    {
        public ActionParameter(ParameterInfo p)
        {
            ParameterInfo = p;
            Type = p.ParameterType;
        }

        public ParameterInfo ParameterInfo { get; private set; }

        public Type Type { get; internal set; }
    }
}
