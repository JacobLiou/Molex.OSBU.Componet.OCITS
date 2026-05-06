using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Device
{
    public interface IInterleaverScan
    {
        /// <summary>
        /// 与产品的哪个通道相关，字段为PMn
        /// </summary>
        //string Flag { get; set; }
        
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="serverIP">服务器IP地址</param>
        /// <param name="powmeterIndex">服务器端功率计对应index</param>
        /// <param name="flag">获取操作对象时flag</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        bool InitAndConnectServer(ref string errMsg, string serverIP, int powmeterIndex = -1, string flag = "");

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        int Scan(bool doPDL, bool doRef,ref string dataPath,ref string errMsg);

        /// <summary>
        /// 重连服务器
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        bool Reconnect(ref string errMsg);

        /// <summary>
        /// 获取同时扫描功率计个数
        /// </summary>
        /// <returns>功率计数量</returns>
        int PowermeterCount();
    }
}
