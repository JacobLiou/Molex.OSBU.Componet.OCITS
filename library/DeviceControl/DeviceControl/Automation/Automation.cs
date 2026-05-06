using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceControl
{
    public class Automation:MolexUtility.Device.IAutomation
    {
        private string serverIP="";
        private int severPort=0;
        public Automation(string host, int port)
        {
            serverIP = host;
            severPort = port;
        }
        /// <summary>
        /// 获取与自动化通信的IP和port
        /// </summary>
        /// <param name="host">服务器IP地址</param>
        /// <param name="port">端口号</param>
        /// <returns></returns>
        public int GetIPAndPort(ref string host, ref int port)
        {
            if (serverIP == "" || severPort == 0)
            {
                return 1;
            }
            host = serverIP;
            port = severPort;          
            return 0;
        }
    }
}
