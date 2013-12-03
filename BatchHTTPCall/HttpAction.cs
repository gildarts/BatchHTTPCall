using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchHTTPCall
{
    //代表一次的 HTTP  呼叫。
    internal class HttpAction
    {
        public int IdNumber { get; set; }

        public string Url { get; set; }
    }
}
