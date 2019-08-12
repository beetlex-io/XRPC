using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public class XRPCException : Exception
    {
        public XRPCException()
        {
        }

        public short ErrorCode { get; set; }

        public XRPCException(string message) : base(message) { }

        public XRPCException(string message, params object[] parameters) : base(string.Format(message, parameters)) { }

        public XRPCException(string message, Exception baseError) : base(message, baseError) { }

        public XRPCException(Exception baseError, string message, params object[] parameters) : base(string.Format(message, parameters), baseError) { }
    }
}
