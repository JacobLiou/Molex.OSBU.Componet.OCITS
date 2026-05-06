using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Protocol
{
    public class MsgBaseInfo
    {
        public string MsgType { get; set; }
        public string MsgSource { get; set; }
        public string MsgTarget { get; set; }
        public string Operate { get; set; }
    }
}
