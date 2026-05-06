using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Device
{
    public interface IAutomation
    {
        /// <summary>
        /// 获取与自动化通信的IP和port
        /// </summary>
        /// <param name="host">服务器IP地址</param>
        /// <param name="port">端口号</param>
        /// <returns></returns>
        int GetIPAndPort(ref string host, ref int port);
    }
}
