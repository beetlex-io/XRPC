using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public enum ResponseCode : short
    {
        SUCCESS = 200,

        INNER_ERROR = 500,

        ACTION_NOT_FOUND = 404,

        REQUEST_TIMEOUT = 408,

        NOT_SUPPORT = 403
    }
}
