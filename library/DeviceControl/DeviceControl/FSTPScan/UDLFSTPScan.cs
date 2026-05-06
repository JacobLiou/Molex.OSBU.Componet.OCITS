using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility.Device;
using System.Threading;
using System.IO;

namespace DeviceControl
{
    public class UDLFSTPScan:IUDLFSTP
    {
        /// <summary>
        /// 设备GUID
        /// </summary>
        public int deviceGUID { get; set; }

        private int[] pwmIdxs;

        private static object scanStatusLock = new object();

        private int scanResult = -1;

        private bool requestDoPDL = false;
        private bool requestdoRef = false;
        private double requestWLStart = 0;
        private double requestWLStop = 0;
        private double requestStep = 0;
        private string requestErrMsg = "";
        private string requestDatapath = "";
        public void ScanFun()
        {
            if (DeviceHandle.fstpCtrl == null)
            {
                requestErrMsg = "FSTP object is null.";
                lock (scanStatusLock)
                {
                    scanResult = 1;
                    return;
                }
            }
            DeviceHandle.fstpCtrl.SetFSTPParameters(deviceGUID, requestWLStart, requestWLStop, requestStep);
            DeviceHandle.GetUDLMessage(ref requestErrMsg);
            if (requestErrMsg.Length > 0)
            {
                lock (scanStatusLock)
                {
                    scanResult = 1;
                    return;
                }
            }

            double[] wl = new double[1];
            double[] rang = new double[pwmIdxs.Length]; //0--一档  1--扫两档
            for (int i = 0; i < pwmIdxs.Length; i++)
            {
                rang[i] = 0;
            }
            DeviceHandle.fstpCtrl.SetAllPMParameters(deviceGUID, 0, ref wl[0], ref rang[0], 0, pwmIdxs.Length, ref pwmIdxs[0]);
            DeviceHandle.GetUDLMessage(ref requestErrMsg);
            if (requestErrMsg.Length > 0)
                scanResult = 1;
            if (requestDoPDL)
            {
                DeviceHandle.fstpCtrl.ExecutePDLSingleSweep(deviceGUID);
                DeviceHandle.GetUDLMessage(ref requestErrMsg);
                if (requestErrMsg.Length > 0)
                {
                    lock (scanStatusLock)
                    {
                        scanResult = 1;
                        return;
                    }
                }
            }
            else
            {
                DeviceHandle.fstpCtrl.ExecuteILSingleSweep(deviceGUID);
                DeviceHandle.GetUDLMessage(ref requestErrMsg);
                if (requestErrMsg.Length > 0)
                {
                    lock (scanStatusLock)
                    {
                        scanResult = 1;
                        return;
                    }
                }
            }
            int waitTimes = 180;
            bool isScanSuccess = false;
            while (waitTimes > 0)
            {
                int plSweepStatus;
                int plEstWaitingTime;
                DeviceHandle.fstpCtrl.GetSweepStatus(deviceGUID, out plSweepStatus, out plEstWaitingTime);
                DeviceHandle.GetUDLMessage(ref requestErrMsg);
                if (requestErrMsg.Length > 0)
                {
                    lock (scanStatusLock)
                    {
                        scanResult = 1;
                        return;
                    }
                }
                if (plSweepStatus == 0)
                {
                    waitTimes--;
                    Thread.Sleep(500);
                    continue;
                }
                else if (plSweepStatus == -1)
                {
                    requestErrMsg = "scan error";
                        lock (scanStatusLock)
                        {
                            scanResult = 1;
                        return;
                    }
                }
                else if (plSweepStatus == 1)
                {
                    isScanSuccess = true;
                    break;
                }

            }
            if (isScanSuccess)
            {
                lock (scanStatusLock)
                {
                    scanResult = 0;
                    return;
                }
            }
            else
            {
                requestErrMsg = "scan time out.";
                lock (scanStatusLock)
                {
                    scanResult = 1;
                    return;
                }
            }
            MolexUtility.CommonFunction.WriteLog(string.Format("DEVICE CONTROL Scan success"));
            int sampleCount = Convert.ToInt32((requestWLStop - requestWLStart) / requestStep) + 10;
            for (int i = 0; i < pwmIdxs.Length; i++)
            {

                MolexUtility.CommonFunction.WriteLog(string.Format("Begin to read data:{0}", sampleCount));
                double[] dWL = new double[sampleCount];
                double[] aveIL = new double[sampleCount];
                double[] pdl = new double[sampleCount];
                double[] te = new double[sampleCount];
                double[] tm = new double[sampleCount];
                double[] tapIL = new double[sampleCount];
                int realCount = sampleCount;
                string path = string.Format("{0}{1}.csv", requestDatapath, i + 1);
                MolexUtility.CommonFunction.WriteLog(path);
                if (requestDoPDL)
                {
                    MolexUtility.CommonFunction.WriteLog("Read data begin");
                    if (GetMeasureResultWithTETM(i, out dWL[0], out aveIL[0], out pdl[0], out te[0], out tm[0], out tapIL[0], out realCount, ref requestErrMsg) != 0)
                    {
                        lock (scanStatusLock)
                        {
                            scanResult = 1;
                            return;
                        }
                    }
                    MolexUtility.CommonFunction.WriteLog("Read data success");
                    FileStream stream = File.Open(path, FileMode.Create);
                    StreamWriter writer = new StreamWriter(stream);
                    string title = string.Format("WL,Power");
                    writer.WriteLine(title);
                    for (int j = 0; j < realCount; j++)
                    {
                        string line = string.Format("{0},{1},{2},{3},{4}", dWL[j], aveIL[j], pdl[j], te[j], tm[j]);
                        writer.WriteLine(line);
                    }
                    writer.Close();
                    MolexUtility.CommonFunction.WriteLog("Write data to file success");
                }
                else
                {
                    if (GetMeasureResult(i, out dWL[0], out aveIL[0], out pdl[0], out tapIL[0], out realCount, ref requestErrMsg) != 0)
                    {
                        lock (scanStatusLock)
                        {
                            scanResult = 1;
                            return;
                        }
                    }
                    FileStream stream = File.Open(path, FileMode.Create);
                    StreamWriter writer = new StreamWriter(stream);
                    string title = string.Format("WL,Power");
                    writer.WriteLine(title);
                    for (int j = 0; j < realCount; j++)
                    {
                        string line = string.Format("{0},{1}", dWL[j], aveIL[j]);
                        writer.WriteLine(line);
                    }
                    writer.Close();
                }
            }
            MolexUtility.CommonFunction.WriteLog("SCAN SUCCESS");
        }
        public int InitFSTP(int guid,string pm)
        {
            string[] splits = pm.Split(';');
            deviceGUID = guid;
            pwmIdxs = new int[splits.Length];
            for(int i=0;i< splits.Length;i++)
            {
                pwmIdxs[i] = Convert.ToInt32(splits[i]);
            }
            return 0;
        }

        /// <summary>
        /// 获取同时扫描功率计个数
        /// </summary>
        /// <returns>功率计数量</returns>
        public int PowermeterCount()
        {
            return pwmIdxs.Length;
        }

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        public int Scan(bool doPDL, bool doRef, double dWLStart, double dWLStop, double dStep, ref string errMsg)
        {
            
            if (DeviceHandle.fstpCtrl == null)
            {
                errMsg = "FSTP object is null.";
                return 1;
            }
            DeviceHandle.fstpCtrl.SetFSTPParameters(deviceGUID, dWLStart, dWLStop, dStep);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;

            double[] wl = new double[1];
            double[] rang = new double[pwmIdxs.Length]; //0--一档  1--扫两档
            for(int i=0;i< pwmIdxs.Length;i++)
            {
                rang[i] = 0;
            }
            DeviceHandle.fstpCtrl.SetAllPMParameters(deviceGUID, 0, ref wl[0], ref rang[0], 0, pwmIdxs.Length, ref pwmIdxs[0]);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            if(doPDL)
            {
                DeviceHandle.fstpCtrl.ExecutePDLSingleSweep(deviceGUID);
                DeviceHandle.GetUDLMessage(ref errMsg);
                if (errMsg.Length > 0)
                    return 1;
            }
            else
            {
                DeviceHandle.fstpCtrl.ExecuteILSingleSweep(deviceGUID);
                DeviceHandle.GetUDLMessage(ref errMsg);
                if (errMsg.Length > 0)
                    return 1;
            }
            int waitTimes = 180;
            bool isScanSuccess = false;
            while(waitTimes>0)
            {
                int plSweepStatus;
                int plEstWaitingTime;
                DeviceHandle.fstpCtrl.GetSweepStatus(deviceGUID, out plSweepStatus, out plEstWaitingTime);
                DeviceHandle.GetUDLMessage(ref errMsg);
                if (errMsg.Length > 0)
                    return 1;
                if(plSweepStatus==0)
                {
                    waitTimes--;
                    Thread.Sleep(500);
                    continue;
                }
                else if(plSweepStatus == -1)
                {
                    errMsg = "scan error";
                    return 1;
                }
                else if(plSweepStatus==1)
                {
                    isScanSuccess = true;
                    break;
                }
               
            }
            if(isScanSuccess)
            {
                return 0;
            }
            else
            {
                errMsg = "scan time out.";
                return 1;
            }
        }

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        public int Scan(bool doPDL, bool doRef, double dWLStart, double dWLStop, double dStep, ref string dataPath, ref string errMsg)
        {
            /*MolexUtility.CommonFunction.WriteLog(string.Format("DEVICE CONTROL Scan Begin"));
            requestDoPDL = doPDL;
            requestdoRef = doRef;
            requestWLStart = dWLStart;
            requestWLStop = dWLStop;
            requestStep = dStep;
            requestDatapath = dataPath;
            scanResult = -1;
            requestErrMsg = "";
            Thread scanThread = new Thread(new ThreadStart(ScanFun));
            scanThread.SetApartmentState(ApartmentState.STA);
            scanThread.Start();
            while (true)
            {
                lock(scanStatusLock)
                {
                    if(scanResult!=-1)
                    {
                        break;
                    }
                }
            }
            errMsg = requestErrMsg;
            if (scanResult == 1)
                return scanResult;*/
            if (Scan(doPDL, doRef, dWLStart, dWLStop, dStep, ref errMsg) != 0)
                return 1;
            MolexUtility.CommonFunction.WriteLog(string.Format("DEVICE CONTROL Scan success"));
            int sampleCount = Convert.ToInt32((dWLStop - dWLStart) / dStep) + 10;
            for (int i = 0; i < pwmIdxs.Length; i++)
            {

                MolexUtility.CommonFunction.WriteLog(string.Format("Begin to read data:{0}", sampleCount));
                double[] dWL = new double[sampleCount];
                double[] aveIL = new double[sampleCount];
                double[] pdl = new double[sampleCount];
                double[] te = new double[sampleCount];
                double[] tm = new double[sampleCount];
                double[] tapIL = new double[sampleCount];
                int realCount = sampleCount;
                string path = string.Format("{0}{1}.csv", dataPath, i + 1);
                MolexUtility.CommonFunction.WriteLog(path);
                if (doPDL)
                {
                    MolexUtility.CommonFunction.WriteLog("Read data begin");
                    if (GetMeasureResultWithTETM(i, out dWL[0], out aveIL[0], out pdl[0], out te[0], out tm[0], out tapIL[0], out realCount, ref errMsg) != 0)
                        return 1;
                    MolexUtility.CommonFunction.WriteLog("Read data success");
                    FileStream stream = File.Open(path, FileMode.Create);
                    StreamWriter writer = new StreamWriter(stream);
                    string title = string.Format("WL,Power,IL,TE,TM");
                    writer.WriteLine(title);
                    for (int j = 0; j < realCount; j++)
                    {
                        string line = string.Format("{0},{1},{2},{3},{4}", dWL[j], aveIL[j], pdl[j], te[j], tm[j]);
                        writer.WriteLine(line);
                    }
                    writer.Close();
                    MolexUtility.CommonFunction.WriteLog("Write data to file success");
                }
                else
                {
                    if (GetMeasureResult(i, out dWL[0], out aveIL[0], out pdl[0], out tapIL[0], out realCount, ref errMsg) != 0)
                        return 1;
                    FileStream stream = File.Open(path, FileMode.Create);
                    StreamWriter writer = new StreamWriter(stream);
                    string title = string.Format("WL,Power");
                    writer.WriteLine(title);
                    for (int j = 0; j < realCount; j++)
                    {
                        string line = string.Format("{0},{1}", dWL[j], aveIL[j]);
                        writer.WriteLine(line);
                    }
                    writer.Close();
                }
            }
            MolexUtility.CommonFunction.WriteLog("SCAN SUCCESS");
            return 0;
        }


        /// <summary>
        /// 获取扫描状态
        /// </summary>
        /// <param name="plSweepStatus">0--正在扫描，1--扫描结束，-1--扫描出错</param>
        /// <param name="plEstWaitingTime">等待时间</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败</returns>
        public int GetSweepStatus(out int plSweepStatus, out int plEstWaitingTime, ref string errMsg)
        {
            DeviceHandle.fstpCtrl.GetSweepStatus(deviceGUID,out plSweepStatus,out plEstWaitingTime);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }

        /// <summary>
        /// 读取扫描结果
        /// </summary>
        /// <param name="lPMIndex">要读取的功率计序号</param>
        /// <param name="pdblWL">波长</param>
        /// <param name="pdblIL">IL值</param>
        /// <param name="pdblPDL">PDL值</param>
        /// <param name="pdblTapIL">tap值</param>
        /// <param name="plDataCount">点数</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--出错</returns>
        public int GetMeasureResult(int lPMIndex, out double pdblWL, out double pdblIL, out double pdblPDL, out double pdblTapIL, out int plDataCount, ref string errMsg)
        {
            DeviceHandle.fstpCtrl.GetMeasureResult(deviceGUID, pwmIdxs[lPMIndex]-1, out pdblWL,out pdblIL,out pdblPDL,out pdblTapIL,out plDataCount);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }

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
        public int GetMeasureResultWithTETM(int lPMIndex, out double pdblWL, out double pdblIL, out double pdblPDL, out double pdblTE, out double pdblTM, out double pdblTapIL, out int plDataCount, ref string errMsg)
        {
            DeviceHandle.fstpCtrl.GetMeasureResultWithTETM(deviceGUID, pwmIdxs[lPMIndex]-1, out pdblWL, out pdblIL, out pdblPDL,out pdblTE, out pdblTM, out pdblTapIL, out plDataCount);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }
    }
}
