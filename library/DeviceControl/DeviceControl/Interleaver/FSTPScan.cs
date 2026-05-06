using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility.Device;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;

namespace DeviceControl
{
    public class FSTPScan:IFSTPScan
    {
        /// <summary>
        /// 1--IL,2--PDL
        /// </summary>
        public int FSTPType { get; set; }
        /// 路径需要重新规划，看是否放在程序所在目录下的指定文件夹中，暂时放nas-srv4服务器上。
        /// </summary>
        public int ClientPortIndex { get; set; }

        /// <summary>
        /// 功率计数量
        /// </summary>
        private int powmerterCount = 0;


        /// <summary>
        /// 与产品的哪个通道相关
        /// </summary>
        private string recordPM { get; set; }
        public FSTPScan()
        {
            ClientPortIndex = -1;
            recordPM = "";
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="flag">功率计配置</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        public bool InitAndConnectFSTP(ref string errMsg , string pm, int flag)
        {
            MolexUtility.CommonFunction.WriteLog(string.Format("pm:{0},flag:{1}", pm,flag));
            recordPM = pm;
            //Flag = flag;
            ///opticalSwitch = new SwitchInterleaver(txtCom.Text, "115200", "interleaverSwitch", ref errMsg);
            IntPtr config = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ClientTestingConfig)));
            ClientTestingConfig clientConfig = (ClientTestingConfig)Marshal.PtrToStructure(config, typeof(ClientTestingConfig));
            MolexUtility.CommonFunction.WriteLog(string.Format("define ClientTestingConfig"));
            clientConfig.Port = 0;
            string curPath = System.Environment.CurrentDirectory;
            //string curPath = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver";
            //char[] clientDataPath = (curPath + "\\rawdata").ToArray();
            //clientDataPath.CopyTo(clientConfig.ClientDatapath, 0);
            //for (int i = clientDataPath.Length; i < clientConfig.ClientDatapath.Length; i++)
            for (int i = 0; i < clientConfig.ClientDatapath.Length; i++)
            {
                clientConfig.ClientDatapath[i] = '\0';
            }

            //string refPath = curPath + "\\Reference";
            //Array.Copy(refPath.ToArray(), clientConfig.ClientRefDatapath, refPath.Length);
            //for (int i = refPath.Length; i < clientConfig.ClientRefDatapath.Length; i++)
            for (int i = 0; i < clientConfig.ClientRefDatapath.Length; i++)
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

            for (int i = 0; i < clientConfig.ServerIP.Length; i++)
            {
                clientConfig.ServerIP[i] = '\0';
            }

            //string serverPath = curPath + "\\rawdata";

            //clientConfig.ServerDatapath = serverPath.ToArray();
            //Array.Copy(serverPath.ToArray(), clientConfig.ServerDatapath, serverPath.Length);
            //for (int i = serverPath.Length; i < clientConfig.ServerDatapath.Length; i++)
            for (int i = 0; i < clientConfig.ServerDatapath.Length; i++)
            {
                clientConfig.ServerDatapath[i] = '\0';
            }

            string[] pmSplits = pm.Split(';');
            clientConfig.PowermeterCount = pmSplits.Length;
            int[] pmPorts = new int[pmSplits.Length];
            powmerterCount = pmSplits.Length;
            MolexUtility.CommonFunction.WriteLog(string.Format("pm count:{0}", powmerterCount));
            for (int i = 0; i < pmSplits.Length; i++)
            {
                pmPorts[i] = Convert.ToInt32(pmSplits[i]);
            }
            Array.Copy(pmPorts, clientConfig.PowermeterPorts, pmPorts.Length);
            for (int i = pmPorts.Length; i < clientConfig.PowermeterPorts.Length; i++)
            {
                clientConfig.PowermeterPorts[i] = 0;
            }

            
                clientConfig.ClientPortIndex = 0;
                clientConfig.ClientTestPort = clientConfig.ClientPortIndex;
                ClientPortIndex = clientConfig.ClientPortIndex;
            clientConfig.ClientType = flag;
            FSTPType = flag;
            MolexUtility.CommonFunction.WriteLog(string.Format("begin ConnectServer"));
            int connectRes = ScanDll.ConnectServer(clientConfig);
            if (connectRes == 0)
            {
                IntPtr err = ScanDll.GetMsg(clientConfig.ClientTestPort);
                errMsg = Marshal.PtrToStringAnsi(err);
                Marshal.FreeHGlobal(config);
                MolexUtility.CommonFunction.WriteLog(string.Format(" ConnectServer error"));
                return false;
            }

            MolexUtility.CommonFunction.WriteLog(string.Format(" ConnectServer res:{0}", connectRes));
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
        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        public int Scan(bool doPDL, bool doRef, double dWLStart, double dWLStop, double dStep, ref string dataPath, ref string errMsg)
        {
            try
            {
                MolexUtility.CommonFunction.WriteLog(string.Format("dEVICE CONTROL Scan Begin"));
                if (File.Exists(dataPath))
                {
                    MolexUtility.CommonFunction.WriteLog(string.Format("delete file{0}", dataPath));
                    File.Delete(dataPath);
                }
                IntPtr path = Marshal.StringToHGlobalAnsi(dataPath);
                //listStatus.Items.Add("端口" + port + ":开始测试！");
                MolexUtility.CommonFunction.WriteLog(string.Format("TLSScanFSTP begin"));
                int scanRes = ScanDll.TLSScanFSTP(doPDL, doRef, dWLStart, dWLStop, dStep, path);
                MolexUtility.CommonFunction.WriteLog(string.Format("扫描结果:{0}", scanRes));
                if (scanRes == 0)
                {
                    IntPtr err = ScanDll.GetMsg(ClientPortIndex);
                    errMsg = Marshal.PtrToStringAnsi(err);

                    return 1;
                    //listStatus.Items.Add(errMsg);
                }

                return 0;
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
                MolexUtility.CommonFunction.WriteLog(string.Format("Scan Exception:{0}", errMsg));
                return 1;
            }
        }

        /// <summary>
        /// 重连服务器
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        public bool Reconnect(ref string errMsg)
        {
            return true;
        }

        /// <summary>
        /// 获取同时扫描功率计个数
        /// </summary>
        /// <returns>功率计数量</returns>
        public int PowermeterCount()
        {
            return powmerterCount;
        }
    }
}
