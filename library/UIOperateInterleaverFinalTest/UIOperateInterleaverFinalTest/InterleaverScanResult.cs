using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MolexUtility;

namespace UIOperateInterleaverFinalTest
{
    public class InterleaverScanResult
    {
        /// <summary>
        /// 光速
        /// </summary>
        private const double lightSpeed = 2.99792458E8;

        /// <summary>
        /// 归零与测试波长匹配容差（nm），避免 double 绝对相等误伤。
        /// </summary>
        private const double WlMatchToleranceNm = 1e-6;


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
                    errMsg = "归零文件不存在！";
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

        /// <summary>
        /// 读取扫描数据
        /// </summary>
        /// <param name="path">归零数据路径</param>
        /// <param name="spec">归零数据对应Spec</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功 1--文件不存在  2--其他错误，具体看错误信息</returns>
        public static int ReadRefPortCount(string path, ref int portCount, ref string errMsg)
        {
            try
            {
                if (!File.Exists(path))
                {
                    errMsg = "归零文件不存在！";
                    return 1;
                }

                StreamReader sR = new StreamReader(path);
                string content = sR.ReadLine();
                string[] splits = content.Split(',');
                sR.Close();
                if (splits.Length >= 5)
                {
                    portCount = Convert.ToInt32(splits[4]);
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

        /// <summary>
        /// 读取扫描数据
        /// </summary>
        /// <param name="path">归零数据路径</param>
        /// <param name="spec">归零数据对应Spec</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功 1--文件不存在  2--其他错误，具体看错误信息</returns>
        public static int ReadRefSpec(string path, ref string spec, ref string errMsg)
        {
            try
            {
                if (!File.Exists(path))
                {
                    errMsg = "归零文件不存在！";
                    return 1;
                }

                StreamReader sR = new StreamReader(path);
                string content = sR.ReadLine();
                string[] splits = content.Split(',');
                sR.Close();
                if (splits.Length >= 4)
                {
                    spec = splits[3];
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
                    errMsg = "数据文件不存在！";
                    CommonFunction.WriteLog(string.Format("[ScanCalDiag] ReadScanData FAIL fileMissing path={0}", path));
                    return 1;
                }

                FileInfo fi = new FileInfo(path);
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
                CommonFunction.WriteLog(string.Format(
                    "[ScanCalDiag] ReadScanData OK path={0} size={1}B pts={2} {3}",
                    path, fi.Length, dataCount, DescribeRawBuffer("buf", rawdata)));
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(string.Format("[ScanCalDiag] ReadScanData EXCEPTION path={0} ex={1}", path, ex.Message));
                return 2;
            }
        }

        /// <summary>
        /// 诊断用：摘要 rawdata 点数与首末波长/功率。
        /// </summary>
        public static string DescribeRawBuffer(string name, double[][] rawdata)
        {
            if (rawdata == null)
                return name + "=null";
            if (rawdata.Length == 0)
                return name + ".len=0";
            if (rawdata[0] == null)
                return name + "[0]=null";
            int n = rawdata[0].Length;
            if (n == 0)
                return name + ": pts=0";
            string wl0 = rawdata[0][0].ToString("F6");
            string wlN = rawdata[0][n - 1].ToString("F6");
            string p0 = (rawdata.Length > 1 && rawdata[1] != null && rawdata[1].Length == n)
                ? rawdata[1][0].ToString("F3") : "n/a";
            string pMid = (rawdata.Length > 1 && rawdata[1] != null && rawdata[1].Length == n)
                ? rawdata[1][n / 2].ToString("F3") : "n/a";
            return string.Format("{0}: pts={1} WL[{2}..{3}] P[0]={4} P[mid]={5}", name, n, wl0, wlN, p0, pMid);
        }

        /// <summary>
        /// 归零缓冲是否有可用波长点（内存有效，非仅磁盘文件存在）。
        /// </summary>
        public static bool HasValidRefBuffer(double[][] pdlRef)
        {
            return pdlRef != null
                && pdlRef.Length > 1
                && pdlRef[0] != null
                && pdlRef[0].Length > 0
                && pdlRef[1] != null
                && pdlRef[1].Length == pdlRef[0].Length;
        }

        /// <summary>
        /// 根据错误文案写一条产品/程序定性提示，便于事后 grep CLASSIFY。
        /// </summary>
        public static void LogScanCalClassify(string stage, string errMsg, bool isRefFlag, bool refBufferOk)
        {
            string kind;
            string action;
            if (string.IsNullOrEmpty(errMsg) && refBufferOk)
            {
                kind = "ok";
                action = "continue";
            }
            else if (!refBufferOk || (errMsg != null && (errMsg.Contains("无归零") || errMsg.Contains("归零内存"))))
            {
                kind = "product_rule";
                action = "重新系统归零（勿跳过扣归零）";
            }
            else if (errMsg != null && (errMsg.Contains("波长不一致") || errMsg.Contains("ptsMismatch") || errMsg.Contains("扫描范围")))
            {
                kind = "product_or_range";
                action = "同扫描范围重归零后再测；若点数相同且波长差极小则查程序容差";
            }
            else if (!isRefFlag)
            {
                kind = "product_rule";
                action = "未标已归零，请先系统归零";
            }
            else
            {
                kind = "program_or_config";
                action = "查 CalResByPort/模板温度/端口名与 [ScanCalDiag] 明细";
            }
            CommonFunction.WriteLog(string.Format(
                "[ScanCalDiag] CLASSIFY stage={0} kind={1} IsRef={2} refBufOk={3} action={4} err={5}",
                stage, kind, isRefFlag, refBufferOk, action, errMsg ?? ""));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="resultData">rawdata</param>
        /// <param name="errMsg">出错信息</param>
        public static void WritePDLRefData(string path, double[][] resultData, string spec, int portCount, ref string errMsg)
        {
            WritePDLRefData(path, resultData, spec, portCount, DateTime.Now, ref errMsg);
        }

        /// <summary>
        /// 写入归零文件；refTime 由同一次系统归零会话统一传入，避免各口时间戳不一致。
        /// </summary>
        public static void WritePDLRefData(string path, double[][] resultData, string spec, int portCount, DateTime refTime, ref string errMsg)
        {
            try
            {
                double[][] res = resultData;
                FileStream stream = File.Open(path, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                string title = string.Format("WL,Power,{0},{1},{2}", refTime.ToString(), spec, portCount);
                writer.WriteLine(title);
                for (int i = res[0].Length - 1; i >= 0; i--)
                {
                    string line = string.Format("{0},{1},{2},{3},{4},{5}", res[0][i], res[1][i], res[2][i], res[3][i], res[4][i], res[5][i]);
                    writer.WriteLine(line);
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 将四个偏振态数据计算出AVE PDL MAX MIN值，使用mueller矩阵来计算
        /// </summary>
        /// <param name="pdlRef">归零数据</param>
        /// <param name="resultData">扫描的原始数据and计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalFSTPRawdata(double[][] pdlRef, double[][] resultData, ref string errMsg)
        {
            try
            {
                CommonFunction.WriteLog(string.Format(
                    "[ScanCalDiag] CalFSTPRawdata enter {0} | {1}",
                    DescribeRawBuffer("ref", pdlRef), DescribeRawBuffer("test", resultData)));
                if (pdlRef == null)
                {
                    errMsg = "无归零数据！";
                    CommonFunction.WriteLog("[ScanCalDiag] CalFSTPRawdata FAIL pdlRef=null");
                    LogScanCalClassify("CalFSTPRawdata", errMsg, false, false);
                    return;
                }
                if (pdlRef[0] == null || resultData == null || resultData[0] == null)
                {
                    errMsg = "无归零数据！";
                    CommonFunction.WriteLog("[ScanCalDiag] CalFSTPRawdata FAIL ref/test WL buffer null");
                    LogScanCalClassify("CalFSTPRawdata", errMsg, false, false);
                    return;
                }
                double[][] res = resultData;
                //波长
                double[] testWL = resultData[0];


                //波长
                double[] refWL = pdlRef[0];
                //归零平均值
                double[] refAve = pdlRef[1];

                double[] refFre = pdlRef[6];

                if (refWL == null || refAve == null || refFre == null)
                {
                    errMsg = "无归零数据！";
                    CommonFunction.WriteLog("[ScanCalDiag] CalFSTPRawdata FAIL refWL/refAve/refFre null");
                    LogScanCalClassify("CalFSTPRawdata", errMsg, false, false);
                    return;
                }
                if (refWL.Length == 0 || testWL.Length == 0)
                {
                    errMsg = "无归零数据！";
                    CommonFunction.WriteLog(string.Format(
                        "[ScanCalDiag] CalFSTPRawdata FAIL empty pts refLen={0} testLen={1}",
                        refWL.Length, testWL.Length));
                    LogScanCalClassify("CalFSTPRawdata", errMsg, false, false);
                    return;
                }
                if (refWL.Length != testWL.Length)
                {
                    errMsg = string.Format(
                        "归零波长与测试波长不一致，请重新归零！(点数 ref={0} test={1})",
                        refWL.Length, testWL.Length);
                    CommonFunction.WriteLog(string.Format(
                        "[ScanCalDiag] CalFSTPRawdata FAIL ptsMismatch refLen={0} testLen={1}",
                        refWL.Length, testWL.Length));
                    LogScanCalClassify("CalFSTPRawdata", errMsg, true, true);
                    return;
                }

                int Count = resultData[0].Length;
                // 先整段校验波长，任一点超差立即返回，避免半截扣减
                for (int i = 0; i < Count; i++)
                {
                    double dWl = Math.Abs(testWL[i] - refWL[i]);
                    if (dWl > WlMatchToleranceNm)
                    {
                        errMsg = string.Format(
                            "归零波长与测试波长不一致，请重新归零！(idx={0} test={1:F6} ref={2:F6} d={3:E2})",
                            i, testWL[i], refWL[i], dWl);
                        CommonFunction.WriteLog(string.Format(
                            "[ScanCalDiag] CalFSTPRawdata FAIL wlMismatch firstIdx={0}/{1} testWL={2:F6} refWL={3:F6} dWl={4:E2} tol={5:E2}",
                            i, Count, testWL[i], refWL[i], dWl, WlMatchToleranceNm));
                        LogScanCalClassify("CalFSTPRawdata", errMsg, true, true);
                        return;
                    }
                }

                for (int i = 0; i < Count; i++)
                {
                    //计算ave IL
                    res[1][i] = res[1][i] - refAve[i];
                    res[3][i] = res[3][i] - refAve[i];
                    res[4][i] = res[4][i] - refAve[i];

                    //频率
                    res[5][i] = refFre[i];
                }
                CommonFunction.WriteLog(string.Format(
                    "[ScanCalDiag] CalFSTPRawdata OK afterSubtract tol={0:E2} {1}",
                    WlMatchToleranceNm, DescribeRawBuffer("IL", res)));
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(string.Format("[ScanCalDiag] CalFSTPRawdata EXCEPTION {0}", ex.Message));
                return;
            }
        }

        /// <summary>
        /// 计算出最大、最小、PDL值后，将最终用于计算的rawdata写文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="resultData">rawdata</param>
        /// <param name="errMsg">出错信息</param>
        public static void WriteCalData(string path, double[][] resultData,ref string errMsg)
        {
            try
            {
                double[][] res = resultData;               
                FileStream stream = File.Open(path, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine("WL,AVG,PDL,MAX,MIN,FRE");
                for (int i = 0; i < res[0].Length; i++)
                {
                    //判断归零和测试波长是否一致                    
                    string line = string.Format("{0},{1},{2},{3},{4},{5}", res[0][i], res[1][i], res[2][i], res[3][i], res[4][i], res[5][i]);
                    writer.WriteLine(line);                    
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 计算出最大、最小、PDL值后，将最终用于计算的rawdata写文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="resultData">rawdata</param>
        /// <param name="errMsg">出错信息</param>
        public static void WriteFusionCalData(string path,string userID,string stationID,string tempr, double[][] resultData, ref string errMsg)
        {
            try
            {
                double[][] res = resultData;
                FileStream stream = File.Open(path, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine("CRC:");
                writer.WriteLine(string.Format("Operator:{0}",userID));
                writer.WriteLine(string.Format("StationInfo:{0}",stationID));
                writer.WriteLine("Device:FSTP");
                writer.WriteLine("DeviceSetting:8164B and N774X");
                writer.WriteLine(string.Format("Temperature:{0}",tempr));
                DateTime dt = System.DateTime.Now;
                writer.WriteLine(string.Format("Time:{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}",dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
                writer.WriteLine("WL,AVG,PDL,TE,TM,FRE");
                for (int i = 0; i < res[0].Length; i++)
                {
                    //判断归零和测试波长是否一致                    
                    string line = string.Format("{0},{1},{2},{3},{4},{5}", res[0][i], res[1][i], res[2][i], res[3][i], res[4][i], res[5][i]);
                    writer.WriteLine(line);
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 计算出最大、最小、PDL值后，将最终用于计算的rawdata写文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="resultData">rawdata</param>
        /// <param name="errMsg">出错信息</param>
        public static void WriteFusionData(string path, double[][] resultData, string userID, string stationID, string tmpr, ref string errMsg)
        {
            try
            {
                double[][] res = resultData;
                FileStream stream = File.Open(path, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine("CRC:");
                writer.WriteLine(string.Format("Operator:{0}", userID));
                writer.WriteLine(string.Format("StationInfo:{0}", stationID));
                writer.WriteLine(string.Format("Device:FSTP"));
                writer.WriteLine(string.Format("DeviceSetting:8164B and N774X"));
                writer.WriteLine(string.Format("Temperature:{0}", tmpr));
                DateTime dt = System.DateTime.Now;
                writer.WriteLine(string.Format("Time:{0}-{1}-{2} {3}:{4}:{5}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
                int dataLen = resultData.Length;
                int scanPoint = res[0].Length;
                writer.WriteLine("Frequency:{0}~{1},Step:{2}", resultData[dataLen - 1][0], resultData[dataLen - 1][scanPoint - 1], Math.Abs(resultData[dataLen - 1][0] - resultData[dataLen - 1][1]));
                writer.WriteLine("WL,AVG,PDL,TE,TM,FRE");
                for (int i = 0; i < res[0].Length; i++)
                {
                    //判断归零和测试波长是否一致                    
                    string line = string.Format("{0},{1},{2},{3},{4},{5}", res[0][i], res[1][i], res[2][i], res[3][i], res[4][i], res[5][i]);
                    writer.WriteLine(line);
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
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
        /// 将四个偏振态数据计算出AVE PDL AVE AVE值，直接去同一波长点四个偏振态下最大最小值来计算
        /// </summary>
        /// <param name="pdlRawData">扫描的原始数据</param>
        /// <param name="pdlRef">归零数据</param>
        /// <param name="resultData">计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalPDLRefData(List<double[][]> pdlRawData,  double[][] resultData, ref string errMsg)
        {
            try
            {
                double[][] res = resultData;
                //波长
                double[] testWL = pdlRawData[0][0];
                double[] refFre = pdlRawData[0][2];
                //四个偏振态下原始数据
                double[] pdlRawdata1 = pdlRawData[0][1];
                double[] pdlRawdata2 = pdlRawData[1][1];
                double[] pdlRawdata3 = pdlRawData[2][1];
                double[] pdlRawdata4 = pdlRawData[3][1];

                //清零
                InitRawdataBuffer(res);
                int Count = pdlRawdata1.Length;
                //看数组长度是否合适，如不合适，重新分配空间
                InitRawdataBuffer(res, Count);
                for (int i = 0; i < Count; i++)
                {
                    double sum = pdlRawdata1[i] + pdlRawdata2[i] + pdlRawdata3[i] + pdlRawdata4[i];
                    //波长
                    res[0][i] = testWL[i];
                    //计算ave IL
                    res[1][i] = sum / 4;

                    if(res[1][i]<-25)
                    {
                        errMsg = "带PDL归零光太弱（<-25db），请检查光路";
                        InitRawdataBuffer(res);
                        return;
                    }

                    res[2][i] = pdlRawdata1[i];
                    res[3][i] = pdlRawdata2[i];
                    res[4][i] = pdlRawdata3[i];
                    res[5][i] = pdlRawdata4[i];
                    //频率
                    res[6][i] = refFre[i];

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
        /// 将四个偏振态数据计算出AVE PDL AVE AVE值，直接去同一波长点四个偏振态下最大最小值来计算
        /// </summary>
        /// <param name="pdlRawData">扫描的原始数据</param>
        /// <param name="pdlRef">归零数据</param>
        /// <param name="resultData">计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalRawdataByAve(List<double[][]> pdlRawData, double[][] pdlRef, double[][] resultData, ref string errMsg)
        {
            try
            {
                if (pdlRef == null)
                {
                    errMsg = "无归零数据！";
                    return;
                }
                double[][] res = resultData;
                //波长
                double[] testWL = pdlRawData[0][0];
                //四个偏振态下原始数据
                double[] pdlRawdata1 = pdlRawData[0][1];
                double[] pdlRawdata2 = pdlRawData[1][1];
                double[] pdlRawdata3 = pdlRawData[2][1];
                double[] pdlRawdata4 = pdlRawData[3][1];

                //波长
                double[] refWL = pdlRef[0];
                //归零平均值
                double[] refAve = pdlRef[1];
                //四个偏振态下归零数据
                double[] refData1 = pdlRef[2];
                double[] refData2 = pdlRef[3];
                double[] refData3 = pdlRef[4];
                double[] refData4 = pdlRef[5];
                double[] refFre = pdlRef[6];

                //清零
                InitRawdataBuffer(res);
                int Count = pdlRawdata1.Length;
                //看数组长度是否合适，如不合适，重新分配空间
                InitRawdataBuffer(res, Count);
                for (int i = 0; i < Count; i++)
                {
                    //判断归零和测试波长是否一致
                    if (testWL[i] == refWL[i])
                    {
                        double[] iLs = new double[4];
                        iLs[0] = pdlRawdata1[i] - refData1[i];
                        iLs[1] = pdlRawdata2[i] - refData2[i];
                        iLs[2] = pdlRawdata3[i] - refData3[i];
                        iLs[3] = pdlRawdata4[i] - refData4[i];
                        double sum = 0;
                        double max = iLs[0];
                        double min = iLs[0];
                        for (int j = 0; j < 4; j++)
                        {
                            sum += iLs[j];
                            if (max < iLs[j])
                            {
                                max = iLs[j];
                            }
                            if (min > iLs[j])
                            {
                                min = iLs[j];
                            }
                        }
                        //波长
                        res[0][i] = testWL[i];
                        //计算ave IL
                        res[1][i] = sum / 4;
                        res[2][i] = max - min;
                        res[3][i] = sum / 4;
                        res[4][i] = sum / 4;
                        //频率
                        res[5][i] = refFre[i];

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
        /// 将四个偏振态数据计算出AVE PDL MAX MIN值，直接去同一波长点四个偏振态下最大最小值来计算
        /// </summary>
        /// <param name="pdlRawData">扫描的原始数据</param>
        /// <param name="pdlRef">归零数据</param>
        /// <param name="resultData">计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalRawdataByMaxMin(List<double[][]> pdlRawData, double[][] pdlRef, double[][] resultData, ref string errMsg)
        {
            try
            {
                if (pdlRef == null)
                {
                    errMsg = "无归零数据！";
                    return;
                }
                double[][] res = resultData;
                //波长
                double[] testWL = pdlRawData[0][0];
                //四个偏振态下原始数据
                double[] pdlRawdata1 = pdlRawData[0][1];
                double[] pdlRawdata2 = pdlRawData[1][1];
                double[] pdlRawdata3 = pdlRawData[2][1];
                double[] pdlRawdata4 = pdlRawData[3][1];

                //波长
                double[] refWL = pdlRef[0];
                //归零平均值
                double[] refAve = pdlRef[1];
                //四个偏振态下归零数据
                double[] refData1 = pdlRef[2];
                double[] refData2 = pdlRef[3];
                double[] refData3 = pdlRef[4];
                double[] refData4 = pdlRef[5];
                double[] refFre = pdlRef[6];

                //清零
                InitRawdataBuffer(res);
                int Count = pdlRawdata1.Length;
                //看数组长度是否合适，如不合适，重新分配空间
                InitRawdataBuffer(res, Count);
                for (int i = 0; i < Count; i++)
                {
                    //判断归零和测试波长是否一致
                    if (testWL[i] == refWL[i])
                    {
                        double[] iLs = new double[4];
                        iLs[0] = pdlRawdata1[i] - refData1[i];
                        iLs[1] = pdlRawdata2[i] - refData2[i];
                        iLs[2] = pdlRawdata3[i] - refData3[i];
                        iLs[3] = pdlRawdata4[i] - refData4[i];
                        double sum = 0;
                        double max = iLs[0];
                        double min = iLs[0];
                        for (int j = 0; j < 4; j++)
                        {
                            sum += iLs[j];
                            if (max < iLs[j])
                            {
                                max = iLs[j];
                            }
                            if (min > iLs[j])
                            {
                                min = iLs[j];
                            }
                        }
                        //波长
                        res[0][i] = testWL[i];
                        //计算ave IL
                        res[1][i] = sum / 4;
                        res[2][i] = max - min;
                        res[3][i] = max;
                        res[4][i] = min;
                        //频率
                        res[5][i] = refFre[i];

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
        /// 将四个偏振态数据计算出AVE PDL MAX MIN值，使用mueller矩阵来计算
        /// </summary>
        /// <param name="pdlRawData">扫描的原始数据</param>
        /// <param name="pdlRef">归零数据</param>
        /// <param name="resultData">计算结果</param>
        /// <param name="errMsg">出错信息</param>
        public static void CalRawdataByMueller(List<double[][]> pdlRawData, double[][] pdlRef, double[][] resultData, ref string errMsg)
        {
            try
            {
                if(pdlRef == null)
                {
                    errMsg = "无归零数据！";
                    return;
                }
                double[][] res = resultData;
                //波长
                double[] testWL = pdlRawData[0][0];
                //四个偏振态下原始数据
                double[] pdlRawdata1 = pdlRawData[0][1];
                double[] pdlRawdata2 = pdlRawData[1][1];
                double[] pdlRawdata3 = pdlRawData[2][1];
                double[] pdlRawdata4 = pdlRawData[3][1];

                //波长
                double[] refWL = pdlRef[0];
                //归零平均值
                double[] refAve = pdlRef[1];
                //四个偏振态下归零数据
                double[] refData1 = pdlRef[2];
                double[] refData2 = pdlRef[3];
                double[] refData3 = pdlRef[4];
                double[] refData4 = pdlRef[5];
                double[] refFre = pdlRef[6];
                //清零
                InitRawdataBuffer(res);
                int Count = pdlRawdata1.Length;
                //看数组长度是否合适，如不合适，重新分配空间
                InitRawdataBuffer(res, Count);
                for (int i = 0; i < Count; i++)
                {
                    //判断归零和测试波长是否一致
                    if (testWL[i] == refWL[i])
                    {
                        double[] iLs = new double[4];
                        iLs[0] = pdlRawdata1[i] - refData1[i];
                        iLs[1] = pdlRawdata2[i] - refData2[i];
                        iLs[2] = pdlRawdata3[i] - refData3[i];
                        iLs[3] = pdlRawdata4[i] - refData4[i];
                        double sum = 0;
                        for (int j = 0; j < 4; j++)
                        {
                            sum += iLs[j];
                        }

                        //mueller算法，来自mindong提供的库
                        double pa = Math.Pow(10, (refData1[i])/10);
                        double pb = Math.Pow(10, (refData2[i])/10);
                        double pc = Math.Pow(10, (refData3[i])/10);
                        double pd = Math.Pow(10, (refData4[i])/10);

                        double p1 = Math.Pow(10, (pdlRawdata1[i])/10);
                        double p2 = Math.Pow(10, (pdlRawdata2[i])/10);
                        double p3 = Math.Pow(10, (pdlRawdata3[i])/10);
                        double p4 = Math.Pow(10, (pdlRawdata4[i])/10);

                        double t1 = p1 / pa;
                        double t2 = p2 / pb;
                        double t3 = p3 / pc;
                        double t4 = p4 / pd;

                        double m11 = (t1 + t2) / 2.0;
                        double m12 = (t1 - t2) / 2.0;
                        double m13 = t3 - m11;
                        double m14 = t4 - m11;

                        double tempSqrt = Math.Sqrt(m12 * m12 + m13 * m13 + m14 * m14);
                        double tMax = m11 + tempSqrt;
                        double tMin = m11 - tempSqrt;


                        //波长
                        res[0][i] = testWL[i];
                        //计算ave IL
                        res[1][i] = sum / 4;
                        //当光弱于-10dBm时，采用四个偏正态最大最小值，主要影响参数ADJ和CT
                        //开会PLM/PM决定的ADJ和CT不采用穆勒矩阵算法。2022-8-5
                        if ((10 * Math.Log10(tMax)) < -10)
                        {
                            double dmax = iLs[0];
                            double dmin = iLs[0];
                            for (int nCal = 0; nCal < 4; nCal++)
                            {
                                if (dmax < iLs[nCal])
                                    dmax = iLs[nCal];
                                if (dmin > iLs[nCal])
                                    dmin = iLs[nCal];
                            }
                            res[2][i] = dmax - dmin;
                            res[3][i] = dmax;
                            res[4][i] = dmin;
                        }
                        else
                        {
                            res[2][i] = 10*Math.Log10(tMax / tMin);
                            res[3][i] = 10*Math.Log10(tMax);
                            res[4][i] = 10*Math.Log10(tMin);
                        }
                        //频率
                        res[5][i] = refFre[i];
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
