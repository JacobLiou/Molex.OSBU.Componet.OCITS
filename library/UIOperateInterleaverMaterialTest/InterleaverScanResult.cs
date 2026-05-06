using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MolexUtility;

namespace UIOperateInterleaverMaterialTest
{
    public class InterleaverScanResult
    {
        /// <summary>
        /// 光速
        /// </summary>
        private const double lightSpeed = 2.99792458E8;


        /// <summary>
        /// 读取扫描数据
        /// </summary>
        /// <param name="path">扫描数据路径</param>
        /// <param name="rawdata">保存数据的数组</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功 1--文件不存在  2--其他错误，具体看错误信息</returns>
        public static int ReadRefTime(string path,ref DateTime dt, ref string errMsg)
        {
            try
            {
                if (!File.Exists(path))
                {
                    errMsg = "文件不存在！";
                    return 1;
                }

                StreamReader sR = new StreamReader(path);
                string content = sR.ReadLine();
                string[] splits = content.Split(',');
                sR.Close();
                if (splits.Length >= 3)
                {
                    dt = Convert.ToDateTime(splits[2]);
                }
                else
                {
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }

        }

        public static int CheckRefRight(double[][] rawdata,ref string errMsg)
        {
            try
            {
                if(rawdata==null|| rawdata[0]==null)
                {
                    errMsg = "无归零数据！";
                    return 1;
                }
                for(int i=0; i<rawdata[0].Length;i++)
                {
                    if(rawdata[1][i]<-25)
                    {
                        errMsg = "No PDL 归零光太弱（<-25db），请检查光路";
                        InitRawdataBuffer(rawdata);
                        return 1;
                    }
                }
                
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
        /// 读取扫描数据
        /// </summary>
        /// <param name="path">扫描数据路径</param>
        /// <param name="rawdata">保存数据的数组</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功 1--文件不存在  2--其他错误，具体看错误信息</returns>
        public static int ReadScanData(string path, double[][] rawdata, ref string errMsg)
        {
            try
            {
                InitRawdataBuffer(rawdata);

                if (!File.Exists(path))
                {
                    errMsg = "文件不存在！";
                    return 1;
                }

                StreamReader sR = new StreamReader(path);
                int dataCount = 0;
                while (sR.ReadLine() != null)
                {
                    dataCount++;
                }
                //第一行不是数据
                dataCount--;

                //查看扫描点数是否和数组初始化一致，不一致的话就重新初始化
                InitRawdataBuffer(rawdata, dataCount);
                ParserRawdata(sR, dataCount, ref rawdata);
                sR.Close();
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
        /// 用nopdl rawdata来计算
        /// </summary>
        /// <param name="pdlRawData">扫描的原始数据</param>
        /// <param name="noPDLRef">归零数据</param>
        /// <param name="resultData">计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalRawdataByNoPDL(List<double[][]> pdlRawData, double[][] noPDLRef, double[][] resultData, ref string errMsg)
        {
            try
            {
                double[][] res = resultData;
                double[][] rawData = pdlRawData[0];
                double[][] refData = noPDLRef;
                //清零
                InitRawdataBuffer(res);
                int Count = rawData[0].Length;
                //看数组长度是否合适，如不合适，重新分配空间
                InitRawdataBuffer(res, Count);
                for (int i = 0; i < Count; i++)
                {
                    //判断归零和测试波长是否一致
                    if (rawData[0][i] == refData[0][i])
                    {
                        //波长
                        res[0][i] = rawData[0][i];
                        //计算IL
                        res[1][i] = rawData[1][i] - refData[1][i];
                        res[3][i] = res[1][i];
                        res[4][i] = res[1][i];
                        //频率
                        res[5][i] = rawData[2][i];

                    }
                    else
                    {
                        errMsg = "归零波长与测试波长不一致，请重新归零！";
                    }
                }
                
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }
        
        /// <summary>
        /// 初始化rawdata数组函数
        /// </summary>
        /// <param name="rawData">需要初始化的二维数组</param>
        /// <param name="pointCount">扫描点数</param>
        public static void InitRawdataBuffer(double[][] rawData, int pointCount = -1)
        {
            if (pointCount == -1)
            {
                for (int i = 0; i < rawData.Length; i++)
                {
                    if (rawData[i] != null)
                    {
                        Array.Clear(rawData[i], 0, rawData[i].Length);
                    }
                }
            }
            else if (rawData[0] == null || rawData[0].Length != pointCount)
            {
                for (int i = 0; i < rawData.Length; i++)
                {
                    rawData[i] = new double[pointCount];
                }
            }
        }


        /// <summary>
        /// 解析rawdata数据
        /// </summary>
        /// <param name="sR">文件流</param>
        /// <param name="rawdata">原始数据数组</param>
        private static void ParserRawdata(StreamReader sR, int pointCount, ref double[][] rawdata)
        {
            sR.BaseStream.Seek(0, SeekOrigin.Begin);
            sR.DiscardBufferedData();
            //第一行不是数据
            string line = sR.ReadLine();
            line = sR.ReadLine();
            int dataIndex = pointCount - 1;
            while (line != null)
            {
                string[] splitDatas = line.Split(',');
                if (splitDatas.Length <= rawdata.Length)
                {
                    for (int i = 0; i < splitDatas.Length; i++)
                    {
                        rawdata[i][dataIndex] = Convert.ToDouble(splitDatas[i]);
                        if (i == 0)
                        {
                            rawdata[rawdata.Length - 1][dataIndex] = lightSpeed / Convert.ToDouble(splitDatas[i]);
                        }
                    }
                    dataIndex--;
                }
                line = sR.ReadLine();
            }
        }
    }
}
