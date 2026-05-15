using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDL2_ServerLib;

namespace MolexUtility.Device
{
    public interface IUDLFSTP
    {
        /// <summary>
        /// 设备GUID
        /// </summary>
        int deviceGUID { get; set; }

        /// <summary>
        /// 获取同时扫描功率计个数
        /// </summary>
        /// <returns>功率计数量</returns>
        int PowermeterCount();

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        int Scan(bool doPDL, bool doRef, double dWLStart, double dWLStop, double dStep, ref string errMsg);

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        int Scan(bool doPDL, bool doRef, double dWLStart, double dWLStop, double dStep, ref string dataPath, ref string errMsg);

        /// <summary>
        /// 读取扫描结果
        /// </summary>
        /// <param name="lPMIndex">要读取的功率计序号</param>
        /// <param name="pdblWL">波长</param>
        /// <param name="pdblIL">IL值</param>
        /// <param name="pdblPDL">PDL值--无效</param>
        /// <param name="pdblTapIL">tap值--无效</param>
        /// <param name="plDataCount">点数</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--出错</returns>
        int GetMeasureResult(int lPMIndex, out double pdblWL, out double pdblIL, out double pdblPDL, out double pdblTapIL, out int plDataCount, ref string errMsg);

        /// <summary>
        /// 获取带PDL扫描的结果
        /// </summary>
        /// <param name="lPMIndex">功率计序号</param>
        /// <param name="pdblWL">波长</param>
        /// <param name="pdblIL">IL值</param>
        /// <param name="pdblPDL">PDL值</param>
        /// <param name="pdblTE">TE值</param>
        /// <param name="pdblTM">TM值</param>
        /// <param name="pdblTapIL">tapIL值</param>
        /// <param name="plDataCount">点数</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--出错</returns>
        int GetMeasureResultWithTETM(int lPMIndex, out double pdblWL, out double pdblIL, out double pdblPDL, out double pdblTE, out double pdblTM, out double pdblTapIL, out int plDataCount, ref string errMsg);
    }
}
