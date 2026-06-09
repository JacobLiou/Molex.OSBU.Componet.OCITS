using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Algorithm
{
    public interface IInterleaverAlgorithm
    {
        /// <summary>
        /// 计算有效带宽内实际中心波长
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算中心波长频率</returns>
        double CCF(List<double> fres, List<double> avgRawdatas, double itu, double passband,double down,ref string errMsg);

        /// <summary>
        /// 计算有效带宽内实际中心波长
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算中心波长频率</returns>
        double CCFItu(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内漂移
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>漂移值</returns>
        double ShiftITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内漂移
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>漂移值</returns>
        double Shift(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 所有通道shitf最小值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double MinShift(List<double> shifts, ref string errMsg);

        /// <summary>
        /// 所有通道shitf最大值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double MaxShift(List<double> shifts, ref string errMsg);

        /// <summary>
        /// 所有通道shitf最大值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double FSR(List<double> shifts, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内MAXIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取MinIL rawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MaxIL值</returns>
        double MaxIL(List<double> fres, List<double> minILRawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内MAXIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取Minrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MaxIL值</returns>
        double MaxILITU(List<double> fres, List<double> minRawdatas, double itu, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内MinIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxRawdatas">全部插损，取maxrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MinIL值</returns>
        double MinILITU(List<double> fres, List<double> maxRawdatas, double itu, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内MinIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取MaxIL rawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MinIL值</returns>
        double MinIL(List<double> fres, List<double> maxILRawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内ripple值,最大IL-最小IL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxILRawdatas">全部插损，取MaxIL rawdata进行计算</param>
        /// <param name="minILRawdatas">全部插损，取MinIL rawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>ripple值</returns>
        double Ripple(List<double> fres, List<double> maxILRawdatas, List<double> minILRawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 该通道xGHz Clear band内寻找PDL最大点，即为该通道PDL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部PDL值</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算PDL结果</returns>
        double PDL(List<double> fres, List<double> pdlRawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 该通道xGHz Clear band内寻找PDL最大点，即为该通道PDL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="pdlRawdatas">全部PDL值</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算PDL结果</returns>
        double PDLItu(List<double> fres, List<double> pdlRawdatas, double itu, ref string errMsg);

        /// <summary>
        /// 同一port下IL UNI计算，某port所有通道在xGHz Clear band内Max_IL，所有Max_IL中的最大值-最小值
        /// </summary>
        /// <param name="maxRawdatas">全部插损，取Maxrawdata进行计算</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double UniformityPDL(List<double> maxRawdatas, ref string errMsg);


        /// <summary>
        /// 同一port下IL UNI计算，某port所有通道在xGHz Clear band内Max_IL，(Max_IL中的最大值+最小值)/2,再取最大值-最小值
        /// </summary>
        /// <param name="maxILRawdatas">全部插损，取MaxIL rawdata进行计算</param>
        /// <param name="minILRawdatas">全部插损，取MinIL rawdata进行计算</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double UniformityNoPDL(List<double> maxILRawdatas, List<double> minILRawdatas, ref string errMsg);

        /// <summary>
        /// 相邻通道ITU点寻找IL=XdB与产品扫描曲线相交两点（需要做拟合，找到IL=XdB对应的波长点）的中心点即为产品实际中心波长，
        /// Adj_Shift =产品实际中心波长-ITU，MaxAdj_Shift= Max（Adj_Shift1，Adj_Shift2），MinAdj_Shift= Min（Adj_Shift1，Adj_Shift2）单位GHz
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="aveRawdatas">用平均IL曲线计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="dPower">功率点为dPower计算shift</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double Adj_Shift(List<double> fres, List<double> aveRawdatas, double itu, double passband, double ituStep, double dPower,ref string errMsg);

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道clear band里面寻找Max IL点，在相邻通道里面寻找Min IL点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxILRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="minILRawdatas">全部插损MinIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double Adj(List<double> fres, List<double> maxILRawdatas, List<double> minILRawdatas, double itu, double passband,double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道ITU里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double SpecalAdj(List<double> fres, List<double> avgRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道找到ITU的IL值，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double SpecalAdjITU(List<double> fres, List<double> avgRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，非相邻通道Block，在当前通道clear band里面寻找（最低）IL点（ave曲线），在所有非相邻通道里面寻找（最高）IL点（ave曲线），两者差值即为Adj ISO； 
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minFre">模板中通道的最小频率</param>
        /// <param name="maxFre">模板中通道的最大频率</param>
        /// <param name="aveRawdatas">全部插损ave rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double NonAdjIso(List<double> fres, double minFre, double maxFre, List<double> aveRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，非相邻通道Block，在当前通道ITUIL点（带符号的Min曲线），在所有非相邻通道里面寻找 ITU IL点（带符号的Max曲线），两者差值即为Adj ISO； 
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minFre">模板中通道的最小频率</param>
        /// <param name="maxFre">模板中通道的最大频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">相邻通道插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double NonAdjITU(List<double> fres, double minFre, double maxFre, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，非相邻通道Block，在当前通道clear band里面寻找（最低）IL点（带符号的Min曲线），在所有非相邻通道里面寻找（最高）IL点（带符号的Max曲线），两者差值即为Adj ISO； 
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minFre">模板中通道的最小频率</param>
        /// <param name="maxFre">模板中通道的最大频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">相邻通道插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double NonAdj(List<double> fres, double minFre, double maxFre, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，非相邻通道Block，在当前通道ITUIL点（ave曲线），在所有非相邻通道里面寻找 ITU IL点（ave曲线），两者差值即为Adj ISO； 
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minFre">模板中通道的最小频率</param>
        /// <param name="maxFre">模板中通道的最大频率</param>
        /// <param name="aveRawdatas">全部插损ave rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double NonAdjIsoITU(List<double> fres, double minFre, double maxFre, List<double> aveRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道ITU里面寻找Min点，在相邻通道里面寻找ITU Min点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        double AdjItu(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double ituStep, ref string errMsg);

        /// <summary>
        /// 所有通道取最大值
        /// </summary>
        /// <param name="adjs">所有通道adj</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns></returns>
        double MaxAdj(List<double> adjs, ref string errMsg);

        /// <summary>
        /// 所有通道取最小值
        /// </summary>
        /// <param name="adjs">所有通道adj</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns></returns>
        double MinAdj(List<double> adjs, ref string errMsg);

        /// <summary>
        /// 为首先计算出相邻两个通道的Adj ISO值Adj ISO_1和Adj ISO_2，再按照以下公式可计算出总串扰值
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxILRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="minILRawdatas">全部插损MinIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得ct值</returns>
        double Crosstalk(List<double> fres, List<double> maxILRawdatas, List<double> minILRawdatas, double itu, double passband, double ituStep, ref string errMsg);

        /// <summary>
        /// 计算左半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算信息</returns>
        double HBWLeft(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算左半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算信息</returns>
        double HBWLeftITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算右半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>右半径带宽结果</returns>
        double HBWRight(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算右半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>右半径带宽结果</returns>
        double HBWRightITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算小隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>小隔离度半径带宽结果</returns>
        double HBWMinITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算小隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>小隔离度半径带宽结果</returns>
        double HBWMin(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算大隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>大隔离度半径带宽结果</returns>
        double HBWMax(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算大隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>大隔离度半径带宽结果</returns>
        double HBWMaxITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);

        /// <summary>
        /// 计算带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>shift值</returns>
        double BW(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>shift值</returns>
        double BWItu(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg);


        /// <summary>
        /// 计算stopband
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxILRawdatas">MAXIL原始数据</param>
        /// <param name="avgRawdatas">另一通道平均原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>stopband值</returns>
        double StopBand(List<double> fres, List<double> maxILRawdatas,List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg);

        /// <summary>
        /// 计算WDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double WDL(List<double> maxILs, List<double> minILs, ref string errMsg);
        /// <summary>
        /// TDL计算
        /// </summary>
        /// <param name="highMaxILs">高温下的maxil</param>
        /// <param name="roomMaxILs">室温下的maxil</param>
        /// <param name="lowMaxILs">低温下的maxil</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>计算结果</returns>
        double TDL(List<double> highMaxILs, List<double> roomMaxILs, List<double> lowMaxILs, ref string errMsg);

        /// <summary>
        /// 全温TDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double TDLAll(List<double> maxILs, List<double> minILs, double passband, ref string errMsg);

        /// <summary>
        /// 相对于常温TDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        double TDLRoom(List<double> maxILs, List<double> minILs, double passband, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内CD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">CD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>CD值</returns>
        double CD(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内PMD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">PMD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>PMD值</returns>
        double PMD(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg);

        /// <summary>
        /// 计算有效带宽内GD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">GD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>GD值</returns>
        double GDResult(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg);

    }
}
