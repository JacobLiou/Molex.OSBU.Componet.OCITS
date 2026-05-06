using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolexUtility.Device
{
    public interface ICDScan
    {
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="serverAddr">服务器地址</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        bool InitAndConnectServer(ref string errMsg, string serverAddr);

        string GetNewStatusMsg();

        bool GetIsConnect();

        void DisConnect();

        int GetScanCompleted();

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        int Scan(bool doPDL, bool doRef, ref string dataPath, ref string errMsg);

        /// <summary>
        /// 重连服务器
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        bool Reconnect(ref string errMsg);

        int SetScanParam(double xStart, double xStop, double dRFModulFre, double dStep, int dIFBW);
    }
}
