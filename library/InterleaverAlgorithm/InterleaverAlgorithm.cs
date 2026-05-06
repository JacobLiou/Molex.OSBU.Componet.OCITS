using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility;
using MolexUtility.Algorithm;
using System.ComponentModel.Composition;

namespace InterleaverAlgorithm
{
    [Export(typeof(IInterleaverAlgorithm))]
    [ExportMetadata("name", "InterleaverAlgorithm")]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class InterleaverAlgorithm:IInterleaverAlgorithm
    {
        private double preITU = 0;
        private double prePassband = 0;
        private int preLeftIndex = -1;
        private int preRightIndex = -1;
        /// <summary>
        /// 找最佳点index
        /// </summary>
        /// <param name="source">比较源数据</param>
        /// <param name="compareIndex"></param>
        /// <param name="criterion"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        private int FindBestIndex(List<double> source,int compareIndex,double criterion,ref string errMsg)
        {
            try
            {
                if ((source[compareIndex].CompareTo(criterion) == 0))
                    return compareIndex;
                else if ((compareIndex > 0) && (source[compareIndex].CompareTo(criterion) > 0 && source[compareIndex - 1].CompareTo(criterion) < 0))
                {
                    //取最接近的一个点
                    double leftDiff = Math.Abs(source[compareIndex - 1] - criterion);
                    double rightDiff = Math.Abs(source[compareIndex] - criterion);
                    if (leftDiff.CompareTo(rightDiff) >= 0)
                        return compareIndex;
                    else
                        return compareIndex-1;
                }
                return -1;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return -1;
            }
        }

        private int FindITUIndex(List<double> fres, double itu, ref string errMsg)
        {
            try
            {
                int ituIndex = -1;
                //如果当前的itu和passband与之前一直，则无需重新再找左右index

                for (int i = 0; i < fres.Count; i++)
                {
                    int findIndex = FindBestIndex(fres, i, itu, ref errMsg);
                    if (findIndex != -1)
                    {
                        ituIndex = findIndex;
                        return ituIndex;
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return -1;
            }
        }

        private double CalILByFre(List<double> fres, List<double> rawdatas, double fre, ref string errMsg)
        {
            try
            {
                double freIL = CommonFunction.GetDefaultValue();
                //CommonFunction.WriteLog(string.Format("fres length:{0},fres[0]:{1},fre:{2}", fres.Count, fres[0], fre));
                for (int i = 0; i < fres.Count - 1; i++)
                {
                    
                    if (fres[i].CompareTo(fre) == 0)
                    {
                        freIL = rawdatas[i];
                    }
                    else if ((fres[i].CompareTo(fre) > 0 && fres[i + 1].CompareTo(fre) < 0)
                        || (fres[i].CompareTo(fre) < 0 && fres[i + 1].CompareTo(fre) > 0))
                    {
                        double dblK = (rawdatas[i] - rawdatas[i + 1]) / (fres[i] - fres[i + 1]);
                        double dblC = rawdatas[i] - dblK * fres[i];
                        freIL = dblK * fre + dblC;
                    }

                }
                return freIL;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }
        /// <summary>
        /// 根据中心波长，有效带宽，找到左右两个点index
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="leftIndex">带宽左边点的index</param>
        /// <param name="rightIndex">带宽右边点的index</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0--正确 1--未找到 2--出错</returns>
        private int FindPassbandIndex(List<double> fres, double itu, double passband,ref int leftIndex,ref int rightIndex,ref string errMsg)
        {
            try
            {
                leftIndex = -1;
                rightIndex = -1;
                //如果当前的itu和passband与之前一直，则无需重新再找左右index
                if (preITU.CompareTo(itu) == 0 && prePassband.CompareTo(passband) == 0)
                {
                    leftIndex = preLeftIndex;
                    rightIndex = preRightIndex;
                }
                else
                {
                    double leftFre = itu - passband;
                    double rightFre = itu + passband;
                    for (int i = 0; i < fres.Count; i++)
                    {
                        int findIndex = FindBestIndex(fres, i, leftFre, ref errMsg);
                        if (findIndex != -1)
                            leftIndex = findIndex;

                        findIndex = -1;
                        findIndex = FindBestIndex(fres, i, rightFre, ref errMsg);
                        if (findIndex != -1)
                            rightIndex = findIndex;
                        preITU = itu;
                        prePassband = passband;
                        preLeftIndex = leftIndex;
                        preRightIndex = rightIndex;
                        //找到左右点后返回，无需继续循环
                        if (leftIndex != -1 && rightIndex != -1)
                            return 0;
                    }
                }
                if (leftIndex == -1 || rightIndex == -1)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }

        /// <summary>
        /// 找db down左右两边index值
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="leftIndex">找最大值左index</param>
        /// <param name="rightIndex">找最大值右index</param>
        /// <param name="down">db down值</param>
        /// <param name="dbDownLeft">db down后左index</param>
        /// <param name="dbDownRight">db down后右index</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--正确 1--未找到 2--出错</returns>
        private int FindDbDownIndex(List<double> rawdatas,int leftIndex,int rightIndex,double down,ref int dbDownLeft,ref int dbDownRight,ref string errMsg)
        {
            try
            {
                dbDownLeft = -1;
                dbDownRight = -1;
                if (rawdatas.Count< rightIndex)
                {
                    errMsg = "测试原始数据不正确！";
                    return 1;
                }
                
                //找到最大值
                double max = rawdatas[leftIndex];
                int maxIndex = leftIndex;
                for (int i=leftIndex;i< rightIndex; i++)
                {
                    if (max < rawdatas[i])
                    {
                        max = rawdatas[i];
                        maxIndex = i;
                    }
                }

                //找db down左边index
                double downValue = max - down;
                for (int i = maxIndex; i > 0; i--)
                {
                    if (rawdatas[i] <= downValue)
                    {
                        dbDownLeft = i;
                        break;
                    }
                }

                //找db down右边index
                for (int i = maxIndex; i < rawdatas.Count; i++)
                {
                    if (rawdatas[i] < downValue)
                    {
                        dbDownRight = i;
                        dbDownRight--;
                        break;
                    }
                    else if (rawdatas[i] == downValue)
                    {
                        dbDownRight = i;
                        break;
                    }
                }
                if (dbDownLeft == -1 || dbDownRight == -1)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }


        /// <summary>
        /// 找db down左右两边index值
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="fres">频率</param>
        /// <param name="leftIndex">找最大值左index</param>
        /// <param name="rightIndex">找最大值右index</param>
        /// <param name="down">db down值</param>
        /// <param name="dbDownLeftFre">db down后左边频率</param>
        /// <param name="dbDownRightFre">db down后右边频率</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--正确 1--未找到 2--出错</returns>
        private int FindDbDownFre(List<double> rawdatas, List<double> fres,int leftIndex, int rightIndex, double down, ref double dbDownLeftFre, ref double dbDownRightFre, ref string errMsg)
        {
            try
            {
                dbDownLeftFre = -1;
                dbDownRightFre = -1;
                if (rawdatas.Count < rightIndex)
                {
                    errMsg = "测试原始数据不正确！";
                    return 1;
                }

                //找到最大值
                double max = rawdatas[leftIndex];
                int maxIndex = leftIndex;
                for (int i = leftIndex; i < rightIndex; i++)
                {
                    if (max < rawdatas[i])
                    {
                        max = rawdatas[i];
                        maxIndex = i;
                    }
                }

                //找db down左边index
                double downValue = max - down;
                for (int i = maxIndex; i > 0; i--)
                {
                    if (rawdatas[i] == downValue)
                    {
                        dbDownLeftFre = fres[i];
                        break;
                    }
                    else if(rawdatas[i] < downValue)
                    {
                        double a = (rawdatas[i + 1] - rawdatas[i]) / (fres[i + 1] - fres[i]);
                        double b = rawdatas[i] - a * fres[i];
                        dbDownLeftFre = (downValue - b) / a;
                        break;
                    }
                }

                //找db down右边index
                for (int i = maxIndex; i < rawdatas.Count; i++)
                {
                    if (rawdatas[i] < downValue)
                    {
                        double a = (rawdatas[i - 1] - rawdatas[i]) / (fres[i - 1] - fres[i]);
                        double b = rawdatas[i] - a * fres[i];
                        dbDownRightFre = (downValue - b) / a;
                        break;
                    }
                    else if (rawdatas[i] == downValue)
                    {
                        dbDownRightFre = fres[i];
                        break;
                    }
                }
                if (dbDownLeftFre == -1 || dbDownRightFre == -1)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }

        /// <summary>
        /// 找db down左右两边index值
        /// </summary>
        /// <param name="rawdatas">原始数据</param>
        /// <param name="fres">频率</param>
        /// <param name="il">需要下降的IL</param>
        /// <param name="curIdx">当前index</param>
        /// <param name="down">db down值</param>
        /// <param name="dbDownLeftFre">db down后左边频率</param>
        /// <param name="dbDownRightFre">db down后右边频率</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--正确 1--未找到 2--出错</returns>
        private int FindDbDownFreByIL(List<double> rawdatas, List<double> fres, double il,int curIdx, double down, ref double dbDownLeftFre, ref double dbDownRightFre, ref string errMsg)
        {
            try
            {
                dbDownLeftFre = -1;
                dbDownRightFre = -1;
                
                //找到最大值
                double max = il;
                int maxIndex = curIdx;
                if (CommonFunction.IsDefault(max))
                    return 1;

                //找db down左边index
                double downValue = max - down;
                for (int i = maxIndex; i > 0; i--)
                {
                    if (rawdatas[i] == downValue)
                    {
                        dbDownLeftFre = fres[i];
                        break;
                    }
                    else if (rawdatas[i] < downValue)
                    {
                        double a = (rawdatas[i + 1] - rawdatas[i]) / (fres[i + 1] - fres[i]);
                        double b = rawdatas[i] - a * fres[i];
                        dbDownLeftFre = (downValue - b) / a;
                        break;
                    }
                }

                //找db down右边index
                for (int i = maxIndex; i < rawdatas.Count; i++)
                {
                    if (rawdatas[i] < downValue)
                    {
                        double a = (rawdatas[i - 1] - rawdatas[i]) / (fres[i - 1] - fres[i]);
                        double b = rawdatas[i] - a * fres[i];
                        dbDownRightFre = (downValue - b) / a;
                        break;
                    }
                    else if (rawdatas[i] == downValue)
                    {
                        dbDownRightFre = fres[i];
                        break;
                    }
                }
                if (dbDownLeftFre == -1 || dbDownRightFre == -1)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }

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
        public double CCF(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if(FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg)!=0)
                {
                    return CommonFunction.GetDefaultValue();
                }
                
                double downLeftFre = -1;
                double downRightFre = -1;
                if(0!= FindDbDownFre(avgRawdatas,fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                return (downLeftFre + downRightFre) / 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算有效带宽内实际中心波长
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算中心波长频率</returns>
        public double CCFItu(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                int passbangRight = passbandLeft;
                if(passbandLeft==-1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbandLeft, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                return (downLeftFre + downRightFre) / 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double Shift(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                //计算中心频率
                double ccf = CCF(fres, avgRawdatas, itu, passband, down, ref errMsg);

                //计算漂移值
                if(ccf!=CommonFunction.GetDefaultValue())
                {
                    return ccf - itu;
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
        /// 计算有效带宽内漂移
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param> 
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>漂移值</returns>
        public double ShiftITU(List<double> fres, List<double> avgRawdatas, double itu,  double down, ref string errMsg)
        {
            try
            {
                //计算中心频率
                double ccf = CCFItu(fres, avgRawdatas, itu, down, ref errMsg);

                //计算漂移值
                if (ccf != CommonFunction.GetDefaultValue())
                {
                    return ccf - itu;
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
        /// 所有通道shitf最小值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double MinShift(List<double> shifts, ref string errMsg)
        {
            try
            {
                double min = CommonFunction.GetDefaultValue();
                CalMin(shifts, 0, shifts.Count-1, ref min, ref errMsg);
                return min;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 所有通道shitf最大值-最小值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double FSR(List<double> shifts, ref string errMsg)
        {
            try
            {
                double max;
                double min;
                CommonFunction.GetMaxMin(shifts.ToArray(), out max, out min);
                /*CalMax(shifts, 0, shifts.Count - 1, ref max, ref errMsg);

                
                CalMin(shifts, 0, shifts.Count - 1, ref min, ref errMsg);*/
                if (max == CommonFunction.GetDefaultValue() || min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();
                else
                    return max - min;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 所有通道shitf最大值
        /// </summary>
        /// <param name="shifts">所有通道shift值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double MaxShift(List<double> shifts, ref string errMsg)
        {
            try
            {
                double max = CommonFunction.GetDefaultValue();
                CalMax(shifts, 0, shifts.Count - 1, ref max, ref errMsg);
                return max;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算有效带宽内MAXIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取Minrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MaxIL值</returns>
        public double MaxIL(List<double> fres, List<double> minRawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if(0!=FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                
                //计算最小值，max为带符号的min值
                double min = minRawdatas[passbandLeft];
                CalMin(minRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                
                return min;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算有效带宽内CD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">CD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>CD值</returns>
        public double CD(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                //计算最小值，max为带符号的min值
                double min = rawdatas[passbandLeft];
                CalMin(rawdatas, passbandLeft, passbangRight, ref min, ref errMsg);

                //计算最大值，mixIL为带符号的max值
                double max = rawdatas[passbandLeft];
                CalMax(rawdatas, passbandLeft, passbangRight, ref max, ref errMsg);

                if (Math.Abs(min) > Math.Abs(max))
                {
                    return min;
                }
                else
                {
                    return max;
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
        /// 计算有效带宽内PMD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">PMD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>PMD值</returns>
        public double PMD(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                //计算最小值，max为带符号的min值
                double min = rawdatas[passbandLeft];
                CalMin(rawdatas, passbandLeft, passbangRight, ref min, ref errMsg);

                //计算最大值，mixIL为带符号的max值
                double max = rawdatas[passbandLeft];
                CalMax(rawdatas, passbandLeft, passbangRight, ref max, ref errMsg);

                if (Math.Abs(min) > Math.Abs(max))
                {
                    return Math.Abs(min);
                }
                else
                {
                    return Math.Abs(max);
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
        /// 计算有效带宽内GD
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">GD的原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>GD值</returns>
        public double GDResult(List<double> fres, List<double> rawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                //计算最小值，max为带符号的min值
                double min = rawdatas[passbandLeft];
                CalMin(rawdatas, passbandLeft, passbangRight, ref min, ref errMsg);

                //计算最大值，mixIL为带符号的max值
                double max = rawdatas[passbandLeft];
                CalMax(rawdatas, passbandLeft, passbangRight, ref max, ref errMsg);

                if (Math.Abs(min) > Math.Abs(max))
                {
                    return min;
                }
                else
                {
                    return max;
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
        /// 计算有效带宽内MAXIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取Minrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MaxIL值</returns>
        public double MaxILITU(List<double> fres, List<double> minRawdatas, double itu, ref string errMsg)
        {
            try
            {
                double calIL = CalILByFre(fres, minRawdatas, itu,ref errMsg);
                if(CommonFunction.IsDefault(calIL))
                {
                    return CommonFunction.GetDefaultValue();
                }
                else
                {
                    return calIL;
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
        /// 计算有效带宽内MinIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxRawdatas">全部插损，取maxrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MinIL值</returns>
        public double MinIL(List<double> fres, List<double> maxRawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if(0!=FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                
                //计算最大值，mixIL为带符号的max值
                double max = maxRawdatas[passbandLeft];
                CalMax(maxRawdatas, passbandLeft, passbangRight, ref max, ref errMsg);
                
                return max;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算有效带宽内MinIL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxRawdatas">全部插损，取maxrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>MinIL值</returns>
        public double MinILITU(List<double> fres, List<double> maxRawdatas, double itu, ref string errMsg)
        {
            try
            {
                double calIL = CalILByFre(fres, maxRawdatas, itu, ref errMsg);
                if (CommonFunction.IsDefault(calIL))
                {
                    return CommonFunction.GetDefaultValue();
                }
                else
                {
                    return calIL;
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
        /// 计算有效带宽内ripple值,最大IL-最小IL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxRawdatas">全部插损，取Maxrawdata进行计算</param>
        /// <param name="minRawdatas">全部插损，取Minrawdata进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>ripple值</returns>
        public double Ripple(List<double> fres, List<double> maxRawdatas, List<double> minRawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if(0!=FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                
                //计算最大值 最小值
                double max = maxRawdatas[passbandLeft];
                double max2 = maxRawdatas[passbandLeft];
                double min = minRawdatas[passbandLeft];
                double min2 = minRawdatas[passbandLeft];
                CalMax(maxRawdatas, passbandLeft, passbangRight, ref max, ref errMsg);
                CalMax(minRawdatas, passbandLeft, passbangRight, ref max2, ref errMsg);
                CalMin(maxRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                CalMin(minRawdatas, passbandLeft, passbangRight, ref min2, ref errMsg);

                if (max == CommonFunction.GetDefaultValue() || min == CommonFunction.GetDefaultValue()
                    || max2 == CommonFunction.GetDefaultValue() || min2 == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();
                else
                {
                    if (max < max2)
                        max = max2;
                    if (min > min2)
                        min = min2;
                    return max - min;
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
        /// 该通道xGHz Clear band内寻找PDL最大点，即为该通道PDL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="pdlRawdatas">全部PDL值</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算PDL结果</returns>
        public double PDL(List<double> fres, List<double> pdlRawdatas, double itu, double passband, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if(0!=FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                
                //计算最大值
                double max = pdlRawdatas[passbandLeft];
                CalMax(pdlRawdatas, passbandLeft, passbangRight, ref max, ref errMsg);
                return max;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 该通道xGHz Clear band内寻找PDL最大点，即为该通道PDL
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="pdlRawdatas">全部PDL值</param>
        /// <param name="itu">中心频率</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算PDL结果</returns>
        public double PDLItu(List<double> fres, List<double> pdlRawdatas, double itu, ref string errMsg)
        {
            try
            {
                int idx = FindITUIndex(fres, itu, ref errMsg);
                if (idx != -1)
                {
                    return pdlRawdatas[idx];
                }
                else
                {
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
        /// 同一port下IL UNI计算，某port所有通道在xGHz Clear band内Max_IL，所有Max_IL中的最大值-最小值
        /// </summary>
        /// <param name="maxRawdatas">全部插损，取Maxrawdata进行计算</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double UniformityPDL(List<double> maxRawdatas, ref string errMsg)
        {
            try
            {
                //计算最大值
                /*double max = maxRawdatas[0];
                //计算最小值
                double min = minRawdatas[0];
                CalMax(maxRawdatas, 0, maxRawdatas.Count-1, ref max, ref errMsg);
                CalMin(minRawdatas, 0, minRawdatas.Count-1, ref min, ref errMsg);*/
                double max;
                double min;
                CommonFunction.GetMaxMin(maxRawdatas.ToArray(), out max, out min);

                if (max == CommonFunction.GetDefaultValue() || min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();
                else
                    return max - min;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        private void CalMax(List<double> rawdata,int leftIndex,int rightIndex,ref double max,ref string errMsg)
        {
            try
            {
                max = rawdata[leftIndex];
                for (int i = leftIndex; i <= rightIndex; i++)
                {
                    if (max < rawdata[i])
                        max = rawdata[i];
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                max = CommonFunction.GetDefaultValue();
                //return CommonFunction.GetDefaultValue();
            }
        }

        
        private void CalMin(List<double> rawdata, int leftIndex, int rightIndex, ref double min, ref string errMsg)
        {
            try
            {
                min = rawdata[leftIndex];
                for (int i = leftIndex; i <= rightIndex; i++)
                {
                    if (min > rawdata[i])
                        min = rawdata[i];
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                min = CommonFunction.GetDefaultValue();
                //return CommonFunction.GetDefaultValue();
            }
        }

        private void CalMaxMin(List<double> rawdata, int leftIndex, int rightIndex, ref double max,ref double min, ref string errMsg)
        {
            try
            {
                max = rawdata[leftIndex];
                min = rawdata[leftIndex];
                for (int i = leftIndex; i <= rightIndex; i++)
                {
                    if (max < rawdata[i])
                        max = rawdata[i];
                    if (min > rawdata[i])
                        min = rawdata[i];
                }
                
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                max = CommonFunction.GetDefaultValue();
                min = CommonFunction.GetDefaultValue();
            }
        }


        /// <summary>
        /// 同一port下IL UNI计算，某port所有通道在xGHz Clear band内Max_IL，(Max_IL中的最大值+最小值)/2,再取最大值-最小值
        /// </summary>
        /// <param name="maxILRawdatas">全部插损，取MaxIL rawdata进行计算</param>
        /// <param name="minILRawdatas">全部插损，取MinIL rawdata进行计算</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double UniformityNoPDL(List<double> maxILRawdatas, List<double> minILRawdatas, ref string errMsg)
        {
            try
            {
                List<double> aveILs = new List<double>();

                for (int i = 0; i < maxILRawdatas.Count; i++)
                {
                    aveILs.Add((maxILRawdatas[i] + minILRawdatas[i]) / 2);
                }
                double max = CommonFunction.GetDefaultValue();
                double min = CommonFunction.GetDefaultValue();
                CalMaxMin(aveILs, 0, aveILs.Count-1, ref max, ref min, ref errMsg);
                if (max == CommonFunction.GetDefaultValue() || min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();
                else
                    return max - min;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double AdjItu(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double ituStep, ref string errMsg)
        {
            try
            {
                double adjISOLeft = CommonFunction.GetDefaultValue();
                double adjISORight = CommonFunction.GetDefaultValue();
                if (TwoAdjItu(fres, minRawdatas, maxRawdatas, itu, ituStep, ref adjISOLeft, ref adjISORight, ref errMsg))
                {
                    //左右取小的ISOADJ
                    if (adjISOLeft > adjISORight)
                        return adjISORight;
                    else
                        return adjISOLeft;
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
        /// 当前通道导通，相邻通道Block，在当前通道ITU里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        public double SpecalAdj(List<double> fres, List<double> avgRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double adjISOLeft = CommonFunction.GetDefaultValue();
                double adjISORight = CommonFunction.GetDefaultValue();
                //int idx = FindITUIndex(fres, itu, ref errMsg);
          
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double min = CommonFunction.GetDefaultValue();
                CalMin(avgRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                if (min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();

                //double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg); //avgRawdatas[idx];
                int adjLeft = -1;
                int adjRight = -1;
                if (0 == FindPassbandIndex(fres, itu - ituStep, passband, ref adjLeft, ref adjRight, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(avgRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        adjISOLeft = min - max;
                    }
                }
                else
                    return CommonFunction.GetDefaultValue();

                int adjLeft1 = -1;
                int adjRight1 = -1;
                if (0 == FindPassbandIndex(fres, itu + ituStep, passband, ref adjLeft1, ref adjRight1, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(avgRawdatas, adjLeft1, adjRight1, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        adjISORight = min - max;
                    }
                }
                else
                    return CommonFunction.GetDefaultValue();
                //左右取小的ISOADJ
                if (adjISOLeft > adjISORight)
                    return adjISORight;
                else
                    return adjISOLeft;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道找到ITU的IL，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="avgRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        public double SpecalAdjITU(List<double> fres, List<double> avgRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double adjISOLeft = CommonFunction.GetDefaultValue();
                double adjISORight = CommonFunction.GetDefaultValue();
                //int idx = FindITUIndex(fres, itu, ref errMsg);

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg); //avgRawdatas[idx];
                int adjLeft = -1;
                int adjRight = -1;
                if (0 == FindPassbandIndex(fres, itu - ituStep, passband, ref adjLeft, ref adjRight, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(avgRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        adjISOLeft = ituIL - max;
                    }
                }
                else
                    return CommonFunction.GetDefaultValue();

                int adjLeft1 = -1;
                int adjRight1 = -1;
                if (0 == FindPassbandIndex(fres, itu + ituStep, passband, ref adjLeft1, ref adjRight1, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(avgRawdatas, adjLeft1, adjRight1, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        adjISORight = ituIL - max;
                    }
                }
                else
                    return CommonFunction.GetDefaultValue();
                //左右取小的ISOADJ
                if (adjISOLeft > adjISORight)
                    return adjISORight;
                else
                    return adjISOLeft;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 相邻通道ITU点寻找IL=XdB与产品扫描曲线相交两点（需要做拟合，找到IL=XdB对应的波长点）的中心点即为产品实际中心波长，
        /// Adj_Shift =产品实际中心波长-ITU，MaxAdj_Shift= Max（Adj_Shift1，Adj_Shift2），MinAdj_Shift= Min（Adj_Shift1，Adj_Shift2）单位GHz
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="aveRawdatas">用平均IL曲线计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得Adj_Shift值</returns>
        public double Adj_Shift(List<double> fres, List<double> aveRawdatas, double itu, double passband, double ituStep,double dPower, ref string errMsg)
        {
            try
            {
                double adjShiftLeft = CommonFunction.GetDefaultValue();
                double adjShiftRight = CommonFunction.GetDefaultValue();
                if (TwoAdjShift(fres, aveRawdatas, itu, passband, ituStep, dPower, ref adjShiftLeft, ref adjShiftRight, ref errMsg))
                {
                    //左右取大的Adj_Shift
                    if (Math.Abs(adjShiftLeft) > Math.Abs(adjShiftRight))
                        return adjShiftLeft;
                    else
                        return adjShiftRight;
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
        /// 当前通道导通，相邻通道Block，在当前通道clear band里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">相邻通道插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得adj_iso值</returns>
        public double Adj(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double adjISOLeft = CommonFunction.GetDefaultValue();
                double adjISORight = CommonFunction.GetDefaultValue();
                if (TwoAdj(fres, minRawdatas, maxRawdatas, itu, passband, ituStep, ref adjISOLeft, ref adjISORight, ref errMsg))
                {
                    //左右取小的ISOADJ
                    if (adjISOLeft > adjISORight)
                        return adjISORight;
                    else
                        return adjISOLeft;
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
        public double NonAdj(List<double> fres,double minFre,double maxFre, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double nonAdj= CommonFunction.GetDefaultValue();
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double min = CommonFunction.GetDefaultValue();
                CalMin(minRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                if (min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();

                for(int i=2; ;i++)
                {
                    double curFre = itu + i * ituStep;
                    if (curFre > maxFre)
                        break;
                    int adjLeft = -1;
                    int adjRight = -1;
                    if (0 == FindPassbandIndex(fres, curFre, passband, ref adjLeft, ref adjRight, ref errMsg))
                    {
                        double max = CommonFunction.GetDefaultValue();
                        CalMax(maxRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                        if (max != CommonFunction.GetDefaultValue())
                        {
                            double curNonAdj = min - max;
                            if (Math.Abs(nonAdj) > curNonAdj)
                                nonAdj = curNonAdj;
                        }
                    }
                }

                for (int i = 2; ; i++)
                {
                    double curFre = itu - i * ituStep;
                    if (curFre < minFre)
                        break;
                    int adjLeft = -1;
                    int adjRight = -1;
                    if (0 == FindPassbandIndex(fres, curFre, passband, ref adjLeft, ref adjRight, ref errMsg))
                    {
                        double max = CommonFunction.GetDefaultValue();
                        CalMax(maxRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                        if (max != CommonFunction.GetDefaultValue())
                        {
                            double curNonAdj = min - max;
                            if (Math.Abs(nonAdj) > curNonAdj)
                                nonAdj = curNonAdj;
                        }
                    }
                }

                return nonAdj;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double NonAdjITU(List<double> fres, double minFre, double maxFre, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double ituIL = CalILByFre(fres, minRawdatas, itu, ref errMsg);
                double nonAdj = CommonFunction.GetDefaultValue();
                
                for (int i = 2; ; i++)
                {
                    double curFre = itu + i * ituStep;
                    if (curFre > maxFre)
                        break;
                    double rightIL = CalILByFre(fres, maxRawdatas, itu + ituStep, ref errMsg);
                    double curNonAdj = ituIL - rightIL;
                    if (Math.Abs(nonAdj) > curNonAdj)
                        nonAdj = curNonAdj;
                }

                for (int i = 2; ; i++)
                {
                    double curFre = itu - i * ituStep;
                    if (curFre < minFre)
                        break;
                    double rightIL = CalILByFre(fres, maxRawdatas, itu + ituStep, ref errMsg);
                    double curNonAdj = ituIL - rightIL;
                    if (Math.Abs(nonAdj) > curNonAdj)
                        nonAdj = curNonAdj;
                }

                return nonAdj;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double NonAdjIso(List<double> fres, double minFre, double maxFre, List<double> aveRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double nonAdj = CommonFunction.GetDefaultValue();
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double min = CommonFunction.GetDefaultValue();
                CalMin(aveRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                if (min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();

                for (int i = 2; ; i++)
                {
                    double curFre = itu + i * ituStep;
                    if (curFre > maxFre)
                        break;
                    int adjLeft = -1;
                    int adjRight = -1;
                    if (0 == FindPassbandIndex(fres, curFre, passband, ref adjLeft, ref adjRight, ref errMsg))
                    {
                        double max = CommonFunction.GetDefaultValue();
                        CalMax(aveRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                        if (max != CommonFunction.GetDefaultValue())
                        {
                            double curNonAdj = min - max;
                            if (Math.Abs(nonAdj) > curNonAdj)
                                nonAdj = curNonAdj;
                        }
                    }
                }

                for (int i = 2; ; i++)
                {
                    double curFre = itu - i * ituStep;
                    if (curFre < minFre)
                        break;
                    int adjLeft = -1;
                    int adjRight = -1;
                    if (0 == FindPassbandIndex(fres, curFre, passband, ref adjLeft, ref adjRight, ref errMsg))
                    {
                        double max = CommonFunction.GetDefaultValue();
                        CalMax(aveRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                        if (max != CommonFunction.GetDefaultValue())
                        {
                            double curNonAdj = min - max;
                            if (Math.Abs(nonAdj) > curNonAdj)
                                nonAdj = curNonAdj;
                        }
                    }
                }

                return nonAdj;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double NonAdjIsoITU(List<double> fres, double minFre, double maxFre, List<double> aveRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double ituIL = CalILByFre(fres, aveRawdatas, itu, ref errMsg);
                double nonAdj = CommonFunction.GetDefaultValue();

                for (int i = 2; ; i++)
                {
                    double curFre = itu + i * ituStep;
                    if (curFre > maxFre)
                        break;
                    double rightIL = CalILByFre(fres, aveRawdatas, itu + ituStep, ref errMsg);
                    double curNonAdj = ituIL - rightIL;
                    if (Math.Abs(nonAdj) > curNonAdj)
                        nonAdj = curNonAdj;
                }

                for (int i = 2; ; i++)
                {
                    double curFre = itu - i * ituStep;
                    if (curFre < minFre)
                        break;
                    double rightIL = CalILByFre(fres, aveRawdatas, itu + ituStep, ref errMsg);
                    double curNonAdj = ituIL - rightIL;
                    if (Math.Abs(nonAdj) > curNonAdj)
                        nonAdj = curNonAdj;
                }

                return nonAdj;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 所有通道取最大值
        /// </summary>
        /// <param name="adjs">所有通道adj</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns></returns>
        public double MaxAdj(List<double> adjs, ref string errMsg)
        {
            try
            {
                double max = CommonFunction.GetDefaultValue();
                CalMax(adjs, 0, adjs.Count - 1, ref max, ref errMsg);
                return max;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 所有通道取最小值
        /// </summary>
        /// <param name="adjs">所有通道adj</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns></returns>
        public double MinAdj(List<double> adjs,ref string errMsg)
        {
            try
            {
                double min = CommonFunction.GetDefaultValue();
                CalMin(adjs, 0, adjs.Count - 1, ref min, ref errMsg);
                return min;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道clear band里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="aveRawdatas">全部插损ave rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="leftAdj">左相邻隔离度</param>
        /// <param name="rightAdj">右相邻隔离度</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>true--正确，false--出错</returns>
        private bool TwoAdjShift(List<double> fres, List<double> aveRawdatas, double itu, double passband, double ituStep, double dPower ,ref double leftAdjShift, ref double rightAdjShift, ref string errMsg)
        {
            try
            {
                leftAdjShift = CommonFunction.GetDefaultValue();
                rightAdjShift = CommonFunction.GetDefaultValue();
                //由于原始数据的Power都是负的，需要转换一下
                dPower = -1 * dPower;
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu - ituStep, ituStep, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return false;
                }
                double leftFres = CommonFunction.GetDefaultValue();
                double rightFres = CommonFunction.GetDefaultValue();
                if (passbandLeft != -1 && passbangRight != -1)
                {
                    for (int i = passbandLeft; i <= passbangRight; i++)
                    {
                        if (Math.Abs(aveRawdatas[i] - dPower) < 0.000001)
                        {
                            leftFres = fres[i];
                        }
                        if (i - passbandLeft > 0)
                        {
                            if (((aveRawdatas[i] - dPower) > 0 && (aveRawdatas[i - 1] - dPower) < 0) || ((aveRawdatas[i] - dPower) < 0 && (aveRawdatas[i - 1] - dPower) > 0))
                            {
                                double dSlope = (aveRawdatas[i] - aveRawdatas[i - 1]) / (fres[i] - fres[i - 1]);
                                double dInterecpt = aveRawdatas[i] - dSlope * fres[i];
                                leftFres = (dPower - dInterecpt) / dSlope;
                                break;
                            }
                        }
                    }

                    for (int i = passbangRight; i >= passbandLeft; i--)
                    {
                        if (Math.Abs(aveRawdatas[i] - dPower) < 0.000001)
                        {
                            rightFres = fres[i];
                        }
                        if (passbangRight - i > 0)
                        {
                            if (((aveRawdatas[i] - dPower) > 0 && (aveRawdatas[i + 1] - dPower) < 0) || ((aveRawdatas[i] - dPower) < 0 && (aveRawdatas[i + 1] - dPower) > 0))
                            {
                                double dSlope = (aveRawdatas[i] - aveRawdatas[i + 1]) / (fres[i] - fres[i + 1]);
                                double dInterecpt = aveRawdatas[i] - dSlope * fres[i];
                                rightFres = (dPower - dInterecpt) / dSlope;
                                break;
                            }
                        }
                    }
                    if(CommonFunction.IsDefault(rightFres)==false&& CommonFunction.IsDefault(leftFres)==false)
                        leftAdjShift = (rightFres + leftFres) / 2 - (itu - ituStep);
                }

                passbandLeft = -1;
                passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu + ituStep, ituStep, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return false;
                }
                leftFres = CommonFunction.GetDefaultValue();
                rightFres = CommonFunction.GetDefaultValue();
                if (passbandLeft != -1 && passbangRight != -1)
                {
                    for (int i = passbandLeft; i <= passbangRight; i++)
                    {
                        if (Math.Abs(aveRawdatas[i] - dPower) < 0.000001)
                        {
                            leftFres = fres[i];
                        }
                        if (i - passbandLeft > 0)
                        {
                            if (((aveRawdatas[i] - dPower) > 0 && (aveRawdatas[i - 1] - dPower) < 0) || ((aveRawdatas[i] - dPower) < 0 && (aveRawdatas[i - 1] - dPower) > 0))
                            {
                                double dSlope = (aveRawdatas[i] - aveRawdatas[i - 1]) / (fres[i] - fres[i - 1]);
                                double dInterecpt = aveRawdatas[i] - dSlope * fres[i];
                                leftFres = (dPower - dInterecpt) / dSlope;
                                break;
                            }
                        }
                    }


                    for (int i = passbangRight; i >= passbandLeft; i--)
                    {
                        if (Math.Abs(aveRawdatas[i] - dPower) < 0.000001)
                        {
                            rightFres = fres[i];
                        }
                        if (passbangRight - i > 0)
                        {
                            if (((aveRawdatas[i] - dPower) > 0 && (aveRawdatas[i + 1] - dPower) < 0) || ((aveRawdatas[i] - dPower) < 0 && (aveRawdatas[i + 1] - dPower) > 0))
                            {
                                double dSlope = (aveRawdatas[i] - aveRawdatas[i + 1]) / (fres[i] - fres[i + 1]);
                                double dInterecpt = aveRawdatas[i] - dSlope * fres[i];
                                rightFres = (dPower - dInterecpt) / dSlope;
                                break;
                            }
                        }
                    }
                    if (CommonFunction.IsDefault(rightFres) == false && CommonFunction.IsDefault(leftFres) == false)
                        rightAdjShift = (rightFres + leftFres) / 2 - (itu + ituStep);
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道clear band里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="leftAdj">左相邻隔离度</param>
        /// <param name="rightAdj">右相邻隔离度</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>true--正确，false--出错</returns>
        private bool TwoAdj(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref double leftAdj,ref double rightAdj, ref string errMsg)
        {
            try
            {
                leftAdj = CommonFunction.GetDefaultValue();
                rightAdj = CommonFunction.GetDefaultValue();
                int passbandLeft = -1;
                int passbangRight = -1;
                if (0 != FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg))
                {
                    return false;
                }
                double min = CommonFunction.GetDefaultValue();
                CalMin(minRawdatas, passbandLeft, passbangRight, ref min, ref errMsg);
                if (min == CommonFunction.GetDefaultValue())
                    return false;

                int adjLeft = -1;
                int adjRight = -1;
                if (0 == FindPassbandIndex(fres, itu - ituStep, passband, ref adjLeft, ref adjRight, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(maxRawdatas, adjLeft, adjRight, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        leftAdj = min-max;
                    }
                }

                int adjLeft1 = -1;
                int adjRight1 = -1;
                if (0 == FindPassbandIndex(fres, itu + ituStep, passband, ref adjLeft1, ref adjRight1, ref errMsg))
                {
                    double max = CommonFunction.GetDefaultValue();
                    CalMax(maxRawdatas, adjLeft1, adjRight1, ref max, ref errMsg);
                    if (max != CommonFunction.GetDefaultValue())
                    {
                        rightAdj = min - max;
                    }
                }                
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        /// <summary>
        /// 当前通道导通，相邻通道Block，在当前通道clear band里面寻找Min点，在相邻通道里面寻找Max点，两者差值即为Adj ISO
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">全部插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="leftAdj">左相邻隔离度</param>
        /// <param name="rightAdj">右相邻隔离度</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>true--正确，false--出错</returns>
        private bool TwoAdjItu(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double ituStep, ref double leftAdj, ref double rightAdj, ref string errMsg)
        {
            try
            {
                double ituIL = CalILByFre(fres, minRawdatas, itu, ref errMsg);
                double rightIL = CalILByFre(fres, maxRawdatas, itu + ituStep, ref errMsg);
                double leftIL = CalILByFre(fres, maxRawdatas, itu - ituStep, ref errMsg);
                
                leftAdj = CommonFunction.GetDefaultValue();
                rightAdj = CommonFunction.GetDefaultValue();
                if (CommonFunction.IsDefault(ituIL)==false&&CommonFunction.IsDefault(leftIL)== false)
                {
                    leftAdj = ituIL - leftIL;
                }
                if (CommonFunction.IsDefault(ituIL) == false && CommonFunction.IsDefault(rightIL) == false)
                {
                    rightAdj = ituIL - rightIL;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        /// <summary>
        /// 为首先计算出相邻两个通道的Adj ISO值Adj ISO_1和Adj ISO_2，再按照以下公式可计算出总串扰值
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="minRawdatas">全部插损MinIL rawdata</param>
        /// <param name="maxRawdatas">相邻通道插损MaxIL rawdata</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="ituStep">ituStep,两个端口相邻通道之间通道频率差值，从高低中频测试项中取</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算所得ct值</returns>
        public double Crosstalk(List<double> fres, List<double> minRawdatas, List<double> maxRawdatas, double itu, double passband, double ituStep, ref string errMsg)
        {
            try
            {
                double adjISOLeft = CommonFunction.GetDefaultValue();
                double adjISORight = CommonFunction.GetDefaultValue();
                if(TwoAdj(fres,minRawdatas,maxRawdatas,itu,passband,ituStep,ref adjISOLeft,ref adjISORight,ref errMsg))
                {
                    if (adjISOLeft == CommonFunction.GetDefaultValue() || adjISORight == CommonFunction.GetDefaultValue())
                        return CommonFunction.GetDefaultValue();
                    double ct = 10 * Math.Log10(Math.Pow(10, -adjISOLeft / 10) + Math.Pow(10, -adjISORight / 10));
                    return -ct;
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
        /// 计算左半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算信息</returns>
        public double HBWLeft(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFre(avgRawdatas, fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWLeft = itu - downLeftFre;
                return hBWLeft;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算左半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算信息</returns>
        public double HBWLeftITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                if(passbandLeft==-1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                int passbangRight = passbandLeft;

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWLeft = itu - downLeftFre;
                return hBWLeft;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double HBWRight(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFre(avgRawdatas, fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                return hBWRight;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算右半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>右半径带宽结果</returns>
        public double HBWRightITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                if (passbandLeft == -1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                int passbangRight = passbandLeft;

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                return hBWRight;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double HBWMin(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFre(avgRawdatas, fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                double hBWLeft = itu - downLeftFre;
                if (hBWLeft > hBWRight)
                    return hBWRight;
                else
                    return hBWLeft;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算小隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>小隔离度半径带宽结果</returns>
        public double HBWMinITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                if (passbandLeft == -1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                int passbangRight = passbandLeft;

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                double hBWLeft = itu - downLeftFre;
                if (hBWLeft > hBWRight)
                    return hBWRight;
                else
                    return hBWLeft;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double HBWMax(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFre(avgRawdatas, fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                double hBWLeft = itu - downLeftFre;
                if (hBWLeft > hBWRight)
                    return hBWLeft;
                else
                    return hBWRight;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算大隔离度半径带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>大隔离度半径带宽结果</returns>
        public double HBWMaxITU(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                if (passbandLeft == -1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                int passbangRight = passbandLeft;

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                double hBWRight = downRightFre - itu;
                double hBWLeft = itu - downLeftFre;
                if (hBWLeft > hBWRight)
                    return hBWLeft;
                else
                    return hBWRight;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

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
        public double BW(List<double> fres, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFre(avgRawdatas, fres, passbandLeft, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                double bw = downRightFre - downLeftFre;               
                return bw;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 计算带宽
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="rawdatas">全部插损，取平均IL进行计算</param>
        /// <param name="itu">中心频率</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>shift值</returns>
        public double BWItu(List<double> fres, List<double> avgRawdatas, double itu, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = FindITUIndex(fres, itu, ref errMsg);
                if (passbandLeft == -1)
                {
                    return CommonFunction.GetDefaultValue();
                }
                int passbangRight = passbandLeft;

                double ituIL = CalILByFre(fres, avgRawdatas, itu, ref errMsg);

                double downLeftFre = -1;
                double downRightFre = -1;
                if (0 != FindDbDownFreByIL(avgRawdatas, fres, ituIL, passbangRight, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }

                double bw = downRightFre - downLeftFre;
                return bw;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }


        /// <summary>
        /// 计算stopband
        /// </summary>
        /// <param name="fres">全部扫描频率</param>
        /// <param name="maxRawdatas">MAX原始数据</param>
        /// <param name="avgRawdatas">另一通道平均原始数据</param>
        /// <param name="itu">中心频率</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="down">多少db down</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>stopband值</returns>
        public double StopBand(List<double> fres, List<double> maxRawdatas, List<double> avgRawdatas, double itu, double passband, double down, ref string errMsg)
        {
            try
            {
                int passbandLeft = -1;
                int passbangRight = -1;
                if (FindPassbandIndex(fres, itu, passband, ref passbandLeft, ref passbangRight, ref errMsg) != 0)
                {
                    return CommonFunction.GetDefaultValue();
                }
                double downLeftFre = CommonFunction.GetDefaultValue();
                double downRightFre = CommonFunction.GetDefaultValue();

                double max = maxRawdatas[passbandLeft];
                //找到最大值
                int maxIndex = passbandLeft;
                for (int i = passbandLeft; i <= passbangRight; i++)
                {
                    if (max < maxRawdatas[i])
                    {
                        max = maxRawdatas[i];
                        maxIndex = i;
                    }
                }

                if (0 != FindDbDownFreByIL(avgRawdatas, fres, max, maxIndex, down, ref downLeftFre, ref downRightFre, ref errMsg))
                {
                    return CommonFunction.GetDefaultValue();
                }
                if (CommonFunction.IsDefault(downLeftFre) == false && CommonFunction.IsDefault(downRightFre) == false)
                {
                    return Math.Abs(downLeftFre - downRightFre);
                }
                else
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
        /// 计算WDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double WDL(List<double> maxILs, List<double> minILs, ref string errMsg)
        {
            try
            {
                List<double> aveILs = new List<double>();
                
                for(int i=0;i<maxILs.Count;i++)
                {
                    aveILs.Add((maxILs[i] + minILs[i]) / 2);
                }
                double max = CommonFunction.GetDefaultValue();
                double min = CommonFunction.GetDefaultValue();
                CalMaxMin(aveILs, 0, aveILs.Count-1, ref max, ref min, ref errMsg);
                if (max == CommonFunction.GetDefaultValue() || min == CommonFunction.GetDefaultValue())
                    return CommonFunction.GetDefaultValue();
                else
                    return max - min;
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
        /// <param name="highMaxILs">高温下的maxil</param>
        /// <param name="roomMaxILs">室温下的maxil</param>
        /// <param name="lowMaxILs">低温下的maxil</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>计算结果</returns>
        public double TDL(List<double> highMaxILs, List<double> roomMaxILs, List<double> lowMaxILs, ref string errMsg)
        {
            try
            {
                int totalCount = 0;
                if (highMaxILs != null)
                    totalCount = highMaxILs.Count;
                if (roomMaxILs != null)
                    totalCount = roomMaxILs.Count;
                if (lowMaxILs != null)
                    totalCount = lowMaxILs.Count;
                
                List<double> allTDLs = new List<double>();
                for(int i=0;i< totalCount; i++)
                {
                    double max = -Math.Abs(CommonFunction.GetDefaultValue());
                    double min = Math.Abs(CommonFunction.GetDefaultValue());
                    if (highMaxILs != null&&!CommonFunction.IsDefault(highMaxILs[i]))
                    {
                        if (max < highMaxILs[i])
                            max = highMaxILs[i];
                        if (min > highMaxILs[i])
                            min = highMaxILs[i];
                    }

                    if (lowMaxILs != null && !CommonFunction.IsDefault(lowMaxILs[i]))
                    {
                        if (max < lowMaxILs[i])
                            max = lowMaxILs[i];
                        if (min > lowMaxILs[i])
                            min = lowMaxILs[i];
                    }

                    if (roomMaxILs != null && !CommonFunction.IsDefault(roomMaxILs[i]))
                    {
                        if (max < roomMaxILs[i])
                            max = roomMaxILs[i];
                        if (min > roomMaxILs[i])
                            min = roomMaxILs[i];
                    }
                    allTDLs.Add(max - min);
                }

                double maxTDL = CommonFunction.GetDefaultValue();
                double minTDL = CommonFunction.GetDefaultValue();
                CalMaxMin(allTDLs, 0, allTDLs.Count - 1, ref maxTDL, ref minTDL, ref errMsg);
                return maxTDL;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 全温TDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double TDLAll(List<double> maxILs, List<double> minILs, double passband, ref string errMsg)
        {
            try
            {
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// 相对于常温TDL
        /// </summary>
        /// <param name="maxILs">所有通道的MaxIL值</param>
        /// <param name="minILs">所有通道的MinIL值</param>
        /// <param name="passband">有效带宽</param>
        /// <param name="errMsg">出错信息，如果不为空，则计算结果无效</param> 
        /// <returns>计算结果</returns>
        public double TDLRoom(List<double> maxILs, List<double> minILs, double passband, ref string errMsg)
        {
            try
            {
                return 0;
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
