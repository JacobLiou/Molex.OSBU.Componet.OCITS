using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Algorithm
{
    public interface IAlgotithm
    {
        /// <summary>
        /// IL算法
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="reference">归零数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double MaxIL(double[] rawdatas, double reference, ref string errMsg);

        /// <summary>
        /// RL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="ilRef">IL归零值</param>
        /// <param name="rlRef">RL归零值</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double RL(double[] rawdatas, double ilRef, double rlRef, ref string errMsg);

        /// <summary>
        /// WDL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double WDL(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// TDL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double TDL(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// WDR计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double WDR(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// WDRM计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double WDRM(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// TDR计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double TDR(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// TDRM计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double TDRM(double[] rawdatas, ref string errMsg);

        /// <summary>
        /// Resiｎ、Resout计算
        /// </summary>
        /// <param name="current">电流值（nA）</param>
        /// <param name="refPower">找功率值</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double Res(double current, double refPower,ref string errMsg);

        

        /// <summary>
        /// PDL算法
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="reference">归零数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        double PDL(double[] rawdatas, ref string errMsg);
    }
}
