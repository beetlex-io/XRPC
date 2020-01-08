using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.XRPC
{
    public enum StatusCode : short
    {
        SUCCESS = 200,

        INNER_ERROR = 500,

        ACTION_NOT_FOUND = 404,

        REQUEST_TIMEOUT = 408,

        NOT_SUPPORT = 403,

        BAD_REQUEST = 400,

        ENEITY_TOO_LARGE = 413
    }
}
