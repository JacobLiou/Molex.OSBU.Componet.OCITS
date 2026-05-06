using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility
{
    public class RealtimePowerInfo
    {
        /// <summary>
        /// 功率值
        /// </summary>
        public string Power { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; }

        public RealtimePowerInfo()
        {
            Power = "";
            Prefix = "";
        }
    }
}
