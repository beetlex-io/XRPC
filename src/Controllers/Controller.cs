using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ControllerAttribute : Attribute
    {
        public ControllerAttribute(params Type[] types)
        {
            Types = types;
        }

        public Type[] Types { get; private set; }

        public string Name { get; set; }

        public bool SingleInstance { get; set; } = true;
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute
    {
        public ActionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
