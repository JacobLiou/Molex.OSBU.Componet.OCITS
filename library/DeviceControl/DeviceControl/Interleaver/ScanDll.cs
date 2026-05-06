using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace DeviceControl
{
    public struct ClientTestingConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] ServerIP;             // 服务器IP地址

        public int Port;                       // 服务器网络连接端口 

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] ClientIP;             // 客户端IP地址

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] ClientName;           // 客户端用户名（电脑名）

        public int ClientPortIndex;            // 连接服务器的物理端口（光纤到客户端对于服务器开关的实际端口）

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public char[] ServerDatapath;      // 服务器临时数据存放路径

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public char[] ClientDatapath;       // 客户端保持数据存放路径

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public char[] ClientRefDatapath;    // 客户端归零数据存放路径

        public int ClientTestPort;             // 客户端自身测试端口号，存在文件名中，便于归零数据命名的存储和调用，服务器无实际对应关系

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public int[] PowermeterPorts;    // 使用的功率计index

        public int PowermeterCount;             // 使用的功率计数量

        public int ClientType;             //1--IL Scan,2--PDL Scan
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        //public byte[] reserv;
    }

    public class ScanDll
    {

        [DllImport("FastScanClentDLL.dll")]
        public static extern int ConnectServer(ClientTestingConfig testinfo);

        [DllImport("FastScanClentDLL.dll")]
        public static extern bool TLSScan(bool bDoPDL, bool bDoRef, int nPort, IntPtr strfilefullname);

        [DllImport("FastScanClentDLL.dll")]
        public static extern int TLSScanFSTP(bool bDoPDL, bool bDoRef, double dWLStart, double dWLStop, double dStep, IntPtr strfilefullname);

        [DllImport("FastScanClentDLL.dll")]
        public static extern IntPtr GetMsg(int nPort);

        [DllImport("FastScanClentDLL.dll")]
        public static extern void Release();
    }
    
}
    