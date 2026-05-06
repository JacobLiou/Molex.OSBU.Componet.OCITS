using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using MolexUtility.Device;
using System.IO;

namespace DeviceControl
{
    public class InterleaverScan: IInterleaverScan
    {
        /// <summary>
        /// 路径需要重新规划，看是否放在程序所在目录下的指定文件夹中，暂时放nas-srv4服务器上。
        /// </summary>
        public int ClientPortIndex { get; set; }

        /// <summary>
        /// 功率计数量
        /// </summary>
        private int powmerterCount=0;

        /// <summary>
        /// 记录服务器地址
        /// </summary>
        private string serverIPAddress = "";

        /// <summary>
        /// 与产品的哪个通道相关
        /// </summary>
        private string recordPM { get; set; }
        public InterleaverScan()
        {
            ClientPortIndex = -1;
            recordPM = "";
        }

        /// <summary>
        /// 获取同时扫描功率计个数
        /// </summary>
        /// <returns>功率计数量</returns>
        public int PowermeterCount()
        {
            return powmerterCount;
        }

        public bool Reconnect(ref string errMsg)
        {
            return InitAndConnectServer(ref errMsg, serverIPAddress, ClientPortIndex, recordPM);
        }
        public bool InitAndConnectServer(ref string errMsg,string serverIP,int sourceIndex,string pm)
        {
            if (sourceIndex == -1)
            {
                errMsg = "未指定光源端口，连接服务器失败！";
                return false;
            }
            recordPM = pm;
            serverIPAddress = serverIP;
            //Flag = flag;
            ///opticalSwitch = new SwitchInterleaver(txtCom.Text, "115200", "interleaverSwitch", ref errMsg);
            IntPtr config = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ClientTestingConfig)));
            ClientTestingConfig clientConfig = (ClientTestingConfig)Marshal.PtrToStructure(config, typeof(ClientTestingConfig));

            clientConfig.Port = 8888;
            string curPath = System.Environment.CurrentDirectory;
            //string curPath = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver";
            char[] clientDataPath = (curPath + "\\rawdata").ToArray();
            clientDataPath.CopyTo(clientConfig.ClientDatapath, 0);
            for (int i = clientDataPath.Length; i < clientConfig.ClientDatapath.Length; i++)
            {
                clientConfig.ClientDatapath[i] = '\0';
            }

            string refPath = curPath + "\\Reference";
            Array.Copy(refPath.ToArray(), clientConfig.ClientRefDatapath, refPath.Length);
            for (int i = refPath.Length; i < clientConfig.ClientRefDatapath.Length; i++)
            {
                clientConfig.ClientRefDatapath[i] = '\0';
            }
            Array.Copy(GetIPAdress().ToArray(), clientConfig.ClientIP, GetIPAdress().Length);
            for (int i = GetIPAdress().Length; i < clientConfig.ClientIP.Length; i++)
            {
                clientConfig.ClientIP[i] = '\0';
            }
            Array.Copy(Dns.GetHostName().ToArray(), clientConfig.ClientName, Dns.GetHostName().Length);
            for (int i = Dns.GetHostName().Length; i < clientConfig.ClientName.Length; i++)
            {
                clientConfig.ClientName[i] = '\0';
            }
            Array.Copy(serverIP.ToArray(), clientConfig.ServerIP, serverIP.Length);
            for (int i = serverIP.Length; i < clientConfig.ServerIP.Length; i++)
            {
                clientConfig.ServerIP[i] = '\0';
            }
            string serverPath = "\\\\" + serverIP + "\\data";

            //clientConfig.ServerDatapath = serverPath.ToArray();
            Array.Copy(serverPath.ToArray(), clientConfig.ServerDatapath, serverPath.Length);
            for (int i = serverPath.Length; i < clientConfig.ServerDatapath.Length; i++)
            {
                clientConfig.ServerDatapath[i] = '\0';
            }

            string[] pmSplits = pm.Split(';');
            clientConfig.PowermeterCount = pmSplits.Length;
            int[] pmPorts = new int[pmSplits.Length];
            powmerterCount = pmSplits.Length;
            for (int i=0;i< pmSplits.Length;i++)
            {
                pmPorts[i] = Convert.ToInt32(pmSplits[i]);
            }
            Array.Copy(pmPorts, clientConfig.PowermeterPorts, pmPorts.Length);
            for (int i = pmPorts.Length; i < clientConfig.PowermeterPorts.Length; i++)
            {
                clientConfig.PowermeterPorts[i] = 0;
            }

            if (sourceIndex != -1)
            {
                clientConfig.ClientPortIndex = sourceIndex;
                clientConfig.ClientTestPort = clientConfig.ClientPortIndex;
                ClientPortIndex = clientConfig.ClientPortIndex;

                if (ScanDll.ConnectServer(clientConfig)==0)
                {
                    IntPtr err = ScanDll.GetMsg(clientConfig.ClientTestPort);
                    errMsg = Marshal.PtrToStringAnsi(err);
                    Marshal.FreeHGlobal(config);                    
                    return false;
                }
            }
            
            Marshal.FreeHGlobal(config);
            return true;
        }

        private string GetIPAdress()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry localhost = Dns.GetHostEntry(hostName);
            IPAddress localAddr;
            if (localhost.AddressList.Length > 1)
                localAddr = localhost.AddressList[1];
            else
                localAddr = localhost.AddressList[0];
            return localAddr.ToString();
        }

        public int Scan(bool doPDL,bool doRef,ref string dataPath,ref string errMsg)
        {
            if(ClientPortIndex == -1)
            {
                errMsg = "未指定功率计端口！";
                return 2;
            }
            //dataPath = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\data\\ScanResult" + ClientPortIndex.ToString() + ".csv";
            //删除数据文件
            if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
            IntPtr path = Marshal.StringToHGlobalAnsi(dataPath);
            //listStatus.Items.Add("端口" + port + ":开始测试！");
            if (!ScanDll.TLSScan(doPDL, doRef, ClientPortIndex, path))
            {
                IntPtr err = ScanDll.GetMsg(ClientPortIndex);
                errMsg = Marshal.PtrToStringAnsi(err);
                return 1;
                //listStatus.Items.Add(errMsg);
            }
            return 0;
        }
    }
}
