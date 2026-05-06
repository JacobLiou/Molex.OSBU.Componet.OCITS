using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility;
using MolexUtility.Algorithm;
using System.ComponentModel.Composition;

namespace CommonAlgorithm
{
    [Export(typeof(IAlgotithm))]
    public class Algorithm:IAlgotithm
    {
        /// <summary>
        /// IL算法
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="reference">归零数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double MaxIL(double[] rawdatas, double reference, ref string errMsg)
        {
            try
            {
                double ilMax;
                double ilMin;
                CommonFunction.GetMaxMin(rawdatas, out ilMax, out ilMin);

                if (CommonFunction.IsDefault(ilMin))
                {
                    errMsg = "IL无数据";
                    return ilMin;
                }
                double il = ilMin - reference;
                return il;
            }
            catch(Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// WDL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double WDL(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double ilMax;
                double ilMin;
                CommonFunction.GetMaxMin(rawdatas, out ilMax, out ilMin);

                if (CommonFunction.IsDefault(Math.Abs(ilMin)) ||  CommonFunction.IsDefault(Math.Abs(ilMax)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double wdl = ilMax - ilMin;
                return wdl;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// WDR计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double WDR(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double resinMax;
                double resinMin;
                CommonFunction.GetMaxMin(rawdatas, out resinMax, out resinMin);
                if (CommonFunction.IsDefault(Math.Abs(resinMin)) || CommonFunction.IsDefault(Math.Abs(resinMax)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double wdr = 10.0 * Math.Log10(resinMax / resinMin);
                return wdr;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// WDRM计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double WDRM(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double resinMax;
                double resinMin;
                CommonFunction.GetMaxMin(rawdatas, out resinMax, out resinMin);

                if (CommonFunction.IsDefault(Math.Abs(resinMin)) || CommonFunction.IsDefault(Math.Abs(resinMax)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double wdrm = resinMax - resinMin;
                return wdrm;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// TDR计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double TDR(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double resinMax;
                double resinMin;
                CommonFunction.GetMaxMin(rawdatas, out resinMax, out resinMin);

                if (CommonFunction.IsDefault(Math.Abs(resinMin)) || CommonFunction.IsDefault(Math.Abs(resinMax)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double wdr = 10.0 * Math.Log10(resinMax / resinMin);
                return wdr;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// TDRM计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double TDRM(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double resinMax;
                double resinMin;
                CommonFunction.GetMaxMin(rawdatas, out resinMax, out resinMin);

                if (CommonFunction.IsDefault(Math.Abs(resinMin)) || CommonFunction.IsDefault(Math.Abs(resinMax)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double tdrm = resinMax - resinMin;
                return tdrm;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }


        /// <summary>
        /// TDL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double TDL(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double ilMax;
                double ilMin;
                CommonFunction.GetMaxMin(rawdatas, out ilMax, out ilMin);

                if (CommonFunction.IsDefault(Math.Abs(ilMax)) || CommonFunction.IsDefault(Math.Abs(ilMin)))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double tdl = ilMax - ilMin;
                return tdl;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// Resiｎ、Resout计算
        /// </summary>
        /// <param name="current">电流值（nA）</param>
        /// <param name="refPower">找功率值</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double Res(double current, double refPower, ref string errMsg)
        {
            try
            {
                if (refPower.CompareTo(0.0) == 0)
                {
                    errMsg = "找功率值异常，不可以为0！";
                    return CommonFunction.GetDefaultValue();
                }
                double res = Math.Abs(current / refPower);
                return res;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }


        /// <summary>
        /// RL计算
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="ilRef">IL归零值</param>
        /// <param name="rlRef">RL归零值</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double RL(double[] rawdatas, double ilRef, double rlRef, ref string errMsg)
        {
            try
            {
                double ilMax;
                double ilMin;
                CommonFunction.GetMaxMin(rawdatas, out ilMax, out ilMin);

                if (CommonFunction.IsDefault(ilMax))
                {
                    return ilMax;
                }
                double dBsd = ilMax - ilRef;
                double dBs = rlRef - ilRef;
                double Wsd = Math.Pow(10, dBsd / 10.0);
                double Ws = Math.Pow(10, dBs / 10.0);
                if(Wsd>Ws)
                {
                    double rl = -3.01 - 10.0 * Math.Log10(Wsd - Ws);
                    return rl;
                }
                else
                {
                    errMsg = "RL值比系统回损大！";
                    return CommonFunction.GetDefaultValue();
                }
                
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// PDL算法
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="reference">归零数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>计算结果</returns>
        public double PDL(double[] rawdatas, ref string errMsg)
        {
            try
            {
                double max;
                double min;
                CommonFunction.GetMaxMin(rawdatas, out max, out min);

                if (CommonFunction.IsDefault(min) || CommonFunction.IsDefault(max))
                {
                    errMsg = "PDL无数据";
                    return CommonFunction.GetDefaultValue();
                }
                double result = max - min;
                return result;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }
    }
}
