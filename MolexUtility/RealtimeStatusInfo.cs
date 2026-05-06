using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility
{
    public class RealtimeStatusInfo
    {
        /// <summary>
        /// 状态具体信息
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 产生该状态的时间
        /// </summary>
        public string StatusTime { get; set; }

        /// <summary>
        /// 状态在提示框的序列号，由显示库决定
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 状态类型
        /// </summary>
        public StatusType Type { get; set; }

        public RealtimeStatusInfo()
        {
            Status = "";
            Type = StatusType.Normal;
        }
    }

    /// <summary>
    /// 状态信息类型
    /// </summary>
    public enum StatusType
    {
        /// <summary>
        /// 普通信息
        /// </summary>
        Normal=0,
        /// <summary>
        /// 警告信息
        /// </summary>
        Warning,
        /// <summary>
        /// 错误信息
        /// </summary>
        Error
    }
}
