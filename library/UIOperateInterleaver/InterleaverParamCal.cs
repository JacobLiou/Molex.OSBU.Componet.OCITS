using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility;
using MolexUtility.Algorithm;

namespace UIOperateInterleaver
{
    public class InterleaverParamCal
    {
        private IInterleaverAlgorithm algorithm = null;
        public InterleaverParamCal(IInterleaverAlgorithm alg)
        {
            algorithm = alg;
        }
        /// <summary>
        /// 计算相关参数
        /// </summary>
        /// <param name="param">参数名称</param>
        /// <param name="resData">扫描计算后得到的原始数据</param>
        /// <param name="borderData">相邻端口的原始数据</param>
        /// <param name="errMsg">出错信息</param>
        public double CalChannelTestParam(string param,double minFre,double maxFre, double[][] resData, double[][] borderData, double ituFre, double productFre,ref string errMsg)
        {
            try
            {
                //CommonFunction.WriteLog(string.Format("CalChannelTestParam begin"));
                for (int i = 0; i < resData.Length; i++)
                {
                    if (resData[i] == null)
                    {
                        errMsg = "无测试数据！";
                        CommonFunction.WriteLog(string.Format("{0}:{1}", errMsg,i));
                        return CommonFunction.GetDefaultValue();
                    }
                }
                string[] paramSplits = param.Split('@');
                double testPassband = CommonFunction.GetDefaultValue();
                double deepth = CommonFunction.GetDefaultValue();
                bool isITU = false;
                if (paramSplits.Length >= 2)
                {
                    string[] setSplits = paramSplits[1].Split(';');
                    //解析参与计算的passband
                    if (setSplits.Length >= 1)
                    {
                        string[] passbandSplits = setSplits[0].Split('=');
                        if(passbandSplits.Length == 1)
                        {
                            if (passbandSplits[0].ToUpper() == "ITU")
                            {
                                isITU = true;
                            }
                        }
                        if (passbandSplits.Length >= 2 /*&& CommonFunction.IsNumber(passbandSplits[1])*/)
                        {
                            testPassband = Convert.ToDouble(passbandSplits[1]);
                        }

                    }
                    //解析下降多少db
                    if (setSplits.Length >= 2)
                    {
                        string[] deepthSplits = setSplits[1].Split('=');
                        if (deepthSplits.Length >= 2 /*&& CommonFunction.IsNumber(deepthSplits[1])*/)
                        {
                            deepth = Convert.ToDouble(deepthSplits[1]);
                        }
                    }
                }

                if (paramSplits[0].ToUpper() == "MAXIL")
                {
                    if(isITU)
                    {
                        //CommonFunction.WriteLog(string.Format("MAXIL,ITU"));
                        double resultValueE= -algorithm.MaxILITU(resData[5].ToList(), resData[3].ToList(), ituFre, ref errMsg);
                        double resultValueM = -algorithm.MaxILITU(resData[5].ToList(), resData[4].ToList(), ituFre, ref errMsg);
                        if (Math.Abs(resultValueE) > Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        double resultValueE = -algorithm.MaxIL(resData[5].ToList(), resData[3].ToList(), ituFre, testPassband, ref errMsg);
                        double resultValueM = -algorithm.MaxIL(resData[5].ToList(), resData[4].ToList(), ituFre, testPassband, ref errMsg);
                        if (Math.Abs(resultValueE) > Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                    
                }
                if (paramSplits[0].ToUpper() == "MINIL")
                {
                    if (isITU)
                    {
                        //CommonFunction.WriteLog(string.Format("MAXIL,ITU"));
                        double resultValueE = -algorithm.MinILITU(resData[5].ToList(), resData[3].ToList(), ituFre, ref errMsg);
                        double resultValueM = -algorithm.MinILITU(resData[5].ToList(), resData[4].ToList(), ituFre, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        double resultValueE = -algorithm.MinIL(resData[5].ToList(), resData[3].ToList(), ituFre, testPassband, ref errMsg);
                        double resultValueM = -algorithm.MinIL(resData[5].ToList(), resData[4].ToList(), ituFre, testPassband, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                }
                /*else if (paramSplits[0].ToUpper() == "UNI")
                {
                    
                }*/
                else if (paramSplits[0].ToUpper() == "PDL")
                {
                    if (isITU)
                    {
                        return algorithm.PDLItu(resData[5].ToList(), resData[2].ToList(), ituFre, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.PDL(resData[5].ToList(), resData[2].ToList(), ituFre, testPassband, ref errMsg);
                    }
                    
                }
                else if (paramSplits[0].ToUpper() == "RIPPLE")
                {
                    if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.Ripple(resData[5].ToList(), resData[3].ToList(), resData[4].ToList(), ituFre, testPassband, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "SHIFT")
                {
                    if (isITU)
                    {
                        return algorithm.ShiftITU(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.Shift(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                    
                }
                /*else if (paramSplits[0].ToUpper() == "MAXSHIFT")
                {

                }
                else if (paramSplits[0].ToUpper() == "MINSHIFT")
                {

                }*/
                else if (paramSplits[0].ToUpper() == "ADJ")
                {
                    if (isITU)
                    {
                        double resultValueE = algorithm.AdjItu(resData[5].ToList(), resData[3].ToList(), resData[3].ToList(), ituFre, productFre, ref errMsg);
                        double resultValueM = algorithm.AdjItu(resData[5].ToList(), resData[4].ToList(), resData[4].ToList(), ituFre, productFre, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            double resultValueE = algorithm.Adj(resData[5].ToList(), resData[3].ToList(), resData[3].ToList(), ituFre, testPassband, productFre, ref errMsg);
                            double resultValueM = algorithm.Adj(resData[5].ToList(), resData[4].ToList(), resData[4].ToList(), ituFre, testPassband, productFre, ref errMsg);
                            if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                                return resultValueE;
                            else
                                return resultValueM;
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "ADJ_AVG")
                {
                    if (isITU)
                    {
                        return algorithm.AdjItu(resData[5].ToList(), resData[1].ToList(), resData[1].ToList(),ituFre, productFre, ref errMsg);
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            return algorithm.Adj(resData[5].ToList(), resData[1].ToList(), resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "NONADJ")
                {
                    if (isITU)
                    {
                        double resultValueE = algorithm.NonAdjITU(resData[5].ToList(), minFre, maxFre, resData[3].ToList(), resData[3].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        double resultValueM = algorithm.NonAdjITU(resData[5].ToList(), minFre, maxFre, resData[4].ToList(), resData[4].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            double resultValueE = algorithm.NonAdj(resData[5].ToList(), minFre, maxFre, resData[3].ToList(), resData[3].ToList(), ituFre, testPassband, productFre, ref errMsg);
                            double resultValueM = algorithm.NonAdj(resData[5].ToList(), minFre, maxFre, resData[4].ToList(), resData[4].ToList(), ituFre, testPassband, productFre, ref errMsg);
                            if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                                return resultValueE;
                            else
                                return resultValueM;
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "NONADJ_ISO")
                {
                    if (isITU)
                    {
                        return algorithm.NonAdjIsoITU(resData[5].ToList(), minFre, maxFre, resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            return algorithm.NonAdjIso(resData[5].ToList(), minFre, maxFre, resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "ADJ_AVG")
                {
                    if (isITU)
                    {
                        return algorithm.AdjItu(resData[5].ToList(), resData[1].ToList(), resData[1].ToList(), ituFre, productFre, ref errMsg);
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            return algorithm.Adj(resData[5].ToList(), resData[1].ToList(), resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "ADJ_ISO")
                {
                    if (isITU)
                    {
                        return algorithm.SpecalAdjITU(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                    }
                    else
                    {
                        if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                        {
                            return algorithm.SpecalAdj(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        }
                    }
                }
                else if (paramSplits[0].ToUpper() == "ADJ_SHIFT")
                {
                    if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        double resultValueE = algorithm.Adj_Shift(resData[5].ToList(), resData[3].ToList(), ituFre, testPassband, productFre, deepth, ref errMsg);
                        double resultValueM = algorithm.Adj_Shift(resData[5].ToList(), resData[4].ToList(), ituFre, testPassband, productFre, deepth, ref errMsg);
                        if (Math.Abs(resultValueE) > Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                        
                    }
                }
                else if (paramSplits[0].ToUpper() == "CT")
                {
                    if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        double resultValueE = algorithm.Crosstalk(resData[5].ToList(), resData[3].ToList(), resData[3].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        double resultValueM = algorithm.Crosstalk(resData[5].ToList(), resData[4].ToList(), resData[4].ToList(), ituFre, testPassband, productFre, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                }
                else if (paramSplits[0].ToUpper() == "STOPBAND")
                {
                    if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        double resultValueE = algorithm.StopBand(resData[5].ToList(), resData[3].ToList(), borderData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                        double resultValueM = algorithm.StopBand(resData[5].ToList(), resData[4].ToList(), borderData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                        if (Math.Abs(resultValueE) < Math.Abs(resultValueM))
                            return resultValueE;
                        else
                            return resultValueM;
                    }
                }
                /*else if (paramSplits[0].ToUpper() == "WDL")
                {
                    
                }
                else if (paramSplits[0].ToUpper() == "TDL_ALL")
                {
                    
                }
                else if (paramSplits[0].ToUpper() == "UNIPDL")
                {

                }
                else if (paramSplits[0].ToUpper() == "TDL_ROOM")
                {

                }*/
                else if (paramSplits[0].ToUpper() == "HBW_MAX")
                {
                    if (isITU)
                    {
                        return algorithm.HBWMaxITU(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.HBWMax(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                    
                }
                else if (paramSplits[0].ToUpper() == "HBW_MIN")
                {
                    if (isITU)
                    {
                        return algorithm.HBWMinITU(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.HBWMin(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                    
                }
                else if (paramSplits[0].ToUpper() == "HBW_L")
                {
                    if (isITU)
                    {
                        return algorithm.HBWLeftITU(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.HBWLeft(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                    
                }
                else if (paramSplits[0].ToUpper() == "HBW_R")
                {
                    if (isITU)
                    {
                        return algorithm.HBWRightITU(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.HBWRight(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                    
                }
                else if (paramSplits[0].ToUpper() == "BW")
                {
                    if (isITU)
                    {
                        return algorithm.BWItu(resData[5].ToList(), resData[1].ToList(), ituFre, deepth, ref errMsg);
                    }
                    else if (testPassband.CompareTo(CommonFunction.GetDefaultValue()) != 0 && deepth.CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        return algorithm.BW(resData[5].ToList(), resData[1].ToList(), ituFre, testPassband, deepth, ref errMsg);
                    }
                }

                return CommonFunction.GetDefaultValue();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算总通道测试项
        /// </summary>
        /// <param name="param">测试项名称</param>
        /// <param name="temperature">测试项温度</param>
        /// <param name="port">测试项端口名称</param>
        /// <param name="records">记录的测试结果</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns></returns>
        public double CalPortParam(string param,string temperature,string port, List<SamePortParamData> records, ref string errMsg)
        {
            try
            {
                //CommonFunction.WriteLog(string.Format("cal param:{0}", param));
                string[] paramSplits = param.Split('@');
                if (paramSplits.Length == 0)
                    return CommonFunction.GetDefaultValue();
                if (paramSplits[0].ToUpper() == "MAXSHIFT")
                {
                    string compParam = "SHIFT@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", compParam));
                    List<double> rawdatas = GetRecordResultByParamName(compParam, temperature, port,records, ref errMsg);
                    if (rawdatas != null)
                    {
                        //计算max值
                        return algorithm.MaxShift(rawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINSHIFT")
                {
                    string compParam = "SHIFT@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", compParam));
                    List<double> rawdatas = GetRecordResultByParamName(compParam, temperature, port, records, ref errMsg);
                    if (rawdatas != null)
                    {
                        //计算min值
                        return algorithm.MinShift(rawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "UNI")
                {
                    string maxParam = "MAXIL@" + paramSplits[1];
                    string minParam = "MINIL@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0},{1}", maxParam, minParam));
                    List<double> maxRawdatas = GetRecordResultByParamName(maxParam, temperature, port, records, ref errMsg);
                    List<double> minRawdatas = GetRecordResultByParamName(minParam, temperature, port, records, ref errMsg);
                    if (maxRawdatas != null && minRawdatas != null)
                    {
                        return algorithm.UniformityNoPDL(maxRawdatas, minRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "UNIPDL")
                {
                    string maxParam = "MAXIL@" + paramSplits[1];
                    string minParam = "MINIL@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0},{1}", maxParam, minParam));
                    List<double> maxRawdatas = GetRecordResultByParamName(maxParam, temperature, port, records, ref errMsg);
                    List<double> minRawdatas = GetRecordResultByParamName(minParam, temperature, port, records, ref errMsg);
                    if (maxRawdatas != null && minRawdatas != null)
                    {
                        return algorithm.UniformityPDL(maxRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "WDL")
                {
                    string maxParam = "MAXIL@" + paramSplits[1];
                    string minParam = "MINIL@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0},{1}", maxParam, minParam));
                    List<double> maxRawdatas = GetRecordResultByParamName(maxParam, temperature, port, records, ref errMsg);
                    List<double> minRawdatas = GetRecordResultByParamName(minParam, temperature, port, records, ref errMsg);
                    if (maxRawdatas != null && minRawdatas != null)
                    {
                        return algorithm.WDL(maxRawdatas, minRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXISO")
                {
                    string adjParam = "ADJ@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MaxAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINISO")
                {
                    string adjParam = "ADJ@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MinAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXADJ_ISO")
                {
                    string adjParam = "ADJ_ISO@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MaxAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINADJ_ISO")
                {
                    string adjParam = "ADJ_ISO@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MinAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXNONISO")
                {
                    string adjParam = "NONADJ@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MaxAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINNONISO")
                {
                    string adjParam = "NONADJ@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MinAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXNONADJ_ISO")
                {
                    string adjParam = "NONADJ_ISO@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MaxAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINNONADJ_ISO")
                {
                    string adjParam = "NONADJ_ISO@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MinAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MINADJ_SHIFT")
                {
                    string adjParam = "ADJ_SHIFT@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MinAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXADJ_SHIFT")
                {
                    string adjParam = "ADJ_SHIFT@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", adjParam));
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {
                        return algorithm.MaxAdj(adjRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "TDL_ALL")
                {
                    /*string adjParam = "ADJ@" + paramSplits[1];
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {

                    }*/
                }
                else if (paramSplits[0].ToUpper() == "TDL_ROOM")
                {
                    /*string adjParam = "ADJ@" + paramSplits[1];
                    List<double> adjRawdatas = GetRecordResultByParamName(adjParam, temperature, port, records, ref errMsg);
                    if (adjRawdatas != null)
                    {

                    }*/
                }
                else if (paramSplits[0].ToUpper() == "FSR")
                {
                    string shiftParam = "SHIFT@" + paramSplits[1];
                   // CommonFunction.WriteLog(string.Format("channel param:{0}", shiftParam));
                    List<double> shiftRawdatas = GetRecordResultByParamName(shiftParam, temperature, port, records, ref errMsg);
                    if (shiftRawdatas != null)
                    {
                        return algorithm.FSR(shiftRawdatas, ref errMsg);
                    }
                }
                else if (paramSplits[0].ToUpper() == "MAXBW")
                {
                    string bwParam = "BW@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", bwParam));
                    List<double> rawdatas = GetRecordResultByParamName(bwParam, temperature, port, records, ref errMsg);
                    if (rawdatas != null)
                    {
                        // 计算最大值
                        double max;
                        double min;
                        CommonFunction.GetMaxMin(rawdatas.ToArray(), out max, out min);
                        return max;
                    }
                }
                else if(paramSplits[0].ToUpper() == "MINPEAKIL")
                {
                    string minParam = "MINIL@" + paramSplits[1];
                    //CommonFunction.WriteLog(string.Format("channel param:{0}", minParam));
                    List<double> rawdatas = GetRecordResultByParamName(minParam, temperature, port, records, ref errMsg);
                    if (rawdatas != null)
                    {
                        // 计算最大值
                        double max;
                        double min;
                        CommonFunction.GetMaxMin(rawdatas.ToArray(), out max, out min);
                        return max;
                    }
                }
                else
                {
                    List<double> rawdatas = GetRecordResultByParamName(param, temperature, port, records, ref errMsg);
                    if (rawdatas != null)
                    {
                        if (paramSplits[0].ToUpper() == "STOPBAND" || paramSplits[0].ToUpper() == "MAXIL" || paramSplits[0].ToUpper() == "PDL" ||
                            paramSplits[0].ToUpper() == "RIPPLE" || paramSplits[0].ToUpper() == "UNI" || paramSplits[0].ToUpper().Contains("MAX"))
                        {
                            // 计算最大值
                            double max;
                            double min;
                            CommonFunction.GetMaxMin(rawdatas.ToArray(), out max, out min);
                            return max;
                        }
                        else
                        {
                            // 计算最小值
                            double max;
                            double min;
                            CommonFunction.GetMaxMin(rawdatas.ToArray(), out max, out min);
                            return min;
                        }
                    }
                }
                //CommonFunction.WriteLog(string.Format("DefaultValue"));
                return CommonFunction.GetDefaultValue();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 根据测试项名称将存储的数据
        /// </summary>
        /// <param name="param">测试项名称</param>
        /// <param name="temperature">测试项温度</param>
        /// <param name="port">测试项端口名称</param>
        /// <param name="records">记录的数据</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>根据测试项名称取出的数据</returns>
        public List<double> GetRecordResultByParamName(string param,string temperature,string port, List<SamePortParamData> records, ref string errMsg)
        {
            try
            {
                if (records != null)
                {
                    foreach (SamePortParamData data in records)
                    {
                        //CommonFunction.WriteLog(string.Format("channel param:{0},{1},{2},{3},{4},{5}", data.ParamName, param, data.Tempreture, temperature, data.Port, port));
                        if (data.ParamName.ToUpper() == param.ToUpper() && data.Tempreture==temperature&&data.Port==port)
                        {
                            return data.Results;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return null;
            }
        }
    }

    /// <summary>
    /// 保存统一通道统一测试项不同中心频率计算结果，用于总端口参数计算
    /// </summary>
    public class SamePortParamData
    {
        /// <summary>
        /// 温度
        /// </summary>
        public string Tempreture { get; set; }
        /// <summary>
        /// 端口名称
        /// </summary>
        public string Port { get; set; }
        /// <summary>
        /// 测试项名称
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        /// 不同中心频率结果
        /// </summary>
        public List<double> Results { get; set; }
        public SamePortParamData()
        {
            Tempreture = "";
            Port = "";
            ParamName = "";
            Results = new List<double>();
        }
    }

    public enum SCANTYPE
    {
        RefWithNoPDL = 0,
        RefWithPDL,
        TestWithNoPDL,
        TestWithPDL
    }

    public class ScanDetail
    {
        /// <summary>
        /// 扫描类型
        /// </summary>
        public SCANTYPE ScanType;

        /// <summary>
        /// 扫描端口
        /// </summary>
        public int Port;
    }
}
