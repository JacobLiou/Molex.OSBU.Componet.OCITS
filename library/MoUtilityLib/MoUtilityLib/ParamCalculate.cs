using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoUtilityLib
{
    public class ParamCalculate
    {
        //获取最大值
        private static double GetMaxValue(double[] dSourceArray,int nLeftIdx,int nRightIdx)
        {
            double dMax = CommonFunction.GetDefaultValue();
            if (dSourceArray.Length < nLeftIdx)
                return CommonFunction.GetDefaultValue();
            if (dSourceArray.Length <= nRightIdx)
                nRightIdx = dSourceArray.Length - 1;
            for (int i = nLeftIdx; i < nRightIdx;i++)
            {
                if (dMax < dSourceArray[i])
                    dMax = dSourceArray[i];
            }
            return dMax;
        }

        //获取最小值
        private static double GetMinValue(double[] dSourceArr, int nLeftIdx, int nRightIdx)
        {
            double dMin = Math.Abs(CommonFunction.GetDefaultValue());
            if (dSourceArr.Length < nLeftIdx)
                return CommonFunction.GetDefaultValue();
            if (dSourceArr.Length <= nRightIdx)
                nRightIdx = dSourceArr.Length - 1;
            for (int i = nLeftIdx; i < nRightIdx; i++)
            {
                if (dMin > dSourceArr[i])
                    dMin = dSourceArr[i];
            }
            return dMin;
        }

        //差损MaxIL最大值为取反的最大值，带符号时为最小
        public static double CalculateMaxIL(double[] dSourceArray)
        {
            return GetMinValue(dSourceArray, 0, dSourceArray.Length);
        }
        public static double CalculateMaxIL(double[] dSourceArr, int nLeftIdx, int nRightIdx)
        {
            return GetMinValue(dSourceArr, nLeftIdx, nRightIdx);
        }

        //差损PeakIL最小值为取反的最小值，带符号时为最大
        public static double CalculatePeakIL(double[] dSourceArr)
        {
            return GetMaxValue(dSourceArr, 0, dSourceArr.Length);
        }
        public static double CalculatePeakIL(double[] dSourceArr, int nLeftIdx, int nRightIdx)
        {
            return GetMaxValue(dSourceArr, nLeftIdx, nRightIdx);
        }

        public static double CalculatePDL(double[] dSourceArr)
        {
            return Math.Abs(CalculateMaxIL(dSourceArr) - CalculatePeakIL(dSourceArr));
        }

        //CT取带符号时的最大值
        public static double CalculateCT(double[] dSourceArr)
        {
            return GetMaxValue(dSourceArr, 0, dSourceArr.Length);
        }

        public static double CalculateRL(double dBsd, double dBs)
        {
            double Wsd = Math.Pow(10.0, dBsd / 10.0);
            double Ws = Math.Pow(10.0, dBs / 10.0);
            double dRL = 80;
            if (Wsd > Ws)
            {
                dRL = -3.01 - 10.0 * Math.Log10(Wsd - Ws);
                if (dRL > 80.0)
                    dRL = 80;
            }
            return dRL;
        }
    }
}
