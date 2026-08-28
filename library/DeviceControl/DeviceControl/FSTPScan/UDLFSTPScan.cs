using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility;
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

        private static readonly object SweepWaitLock = new object();

        private int scanResult = -1;
        private bool hasExecutedSweepInProcess = false;

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
            int waitCode;
            lock (SweepWaitLock)
            {
                waitCode = WaitForSweepCompletion(requestDoPDL, requestWLStart, requestWLStop, requestStep, ref requestErrMsg);
            }
            lock (scanStatusLock)
            {
                scanResult = waitCode;
                if (waitCode != 0)
                    return;
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

        private static bool TryGetUdlError(ref string errMsg)
        {
            if (!DeviceHandle.GetUDLMessage(ref errMsg))
                return false;
            errMsg = "";
            return true;
        }

        /// <summary>配置 FSTP 波长与功率计参数。</summary>
        /// <returns>0 成功，2 失败</returns>
        private int SetupScanParameters(double dWLStart, double dWLStop, double dStep, ref string errMsg)
        {
            DeviceHandle.fstpCtrl.SetFSTPParameters(deviceGUID, dWLStart, dWLStop, dStep);
            if (!TryGetUdlError(ref errMsg))
                return 2;

            double[] wl = new double[1];
            double[] rang = new double[pwmIdxs.Length];
            for (int i = 0; i < pwmIdxs.Length; i++)
                rang[i] = 0;

            DeviceHandle.fstpCtrl.SetAllPMParameters(deviceGUID, 0, ref wl[0], ref rang[0], 0, pwmIdxs.Length, ref pwmIdxs[0]);
            if (!TryGetUdlError(ref errMsg))
                return 2;
            return 0;
        }

        /// <summary>若上次扫描仍在进行，等待其结束。</summary>
        /// <returns>0 可继续，2 失败</returns>
        private int DrainInProgressSweep(ref string errMsg)
        {
            FstpScanWaitSettings settings = FstpScanWaitSettings.Current;
            DateTime deadline = DateTime.UtcNow.AddSeconds(settings.PreExecuteDrainSec);
            while (DateTime.UtcNow < deadline)
            {
                int plSweepStatus;
                int plEstWaitingTime;
                DeviceHandle.fstpCtrl.GetSweepStatus(deviceGUID, out plSweepStatus, out plEstWaitingTime);
                if (!TryGetUdlError(ref errMsg))
                    return 2;
                if (plSweepStatus == 0)
                {
                    Thread.Sleep(settings.GetPollIntervalMs(plEstWaitingTime));
                    continue;
                }
                if (plSweepStatus == -1)
                {
                    errMsg = "scan error";
                    return 2;
                }
                return 0;
            }
            CommonFunction.WriteLog(string.Format(
                "FSTP pre-check timeout, continue execute directly: guid={0}",
                deviceGUID));
            errMsg = "";
            return 0;
        }

        /// <summary>下发单次 IL/PDL 扫描。</summary>
        /// <returns>0 成功，2 失败</returns>
        private int ExecuteSweep(bool doPDL, ref string errMsg)
        {
            if (doPDL)
                DeviceHandle.fstpCtrl.ExecutePDLSingleSweep(deviceGUID);
            else
                DeviceHandle.fstpCtrl.ExecuteILSingleSweep(deviceGUID);
            if (!TryGetUdlError(ref errMsg))
                return 2;
            hasExecutedSweepInProcess = true;
            return 0;
        }

        /// <summary>轮询扫描完成；结合点数估算与服务端 plEstWaitingTime 动态延长 deadline。</summary>
        /// <returns>0 成功，1 超时，2 失败</returns>
        private int WaitForSweepCompletion(bool doPDL, double dWLStart, double dWLStop, double dStep, ref string errMsg)
        {
            FstpScanWaitSettings settings = FstpScanWaitSettings.Current;
            DateTime deadlineUtc = settings.ComputeDeadlineUtc(doPDL, dWLStart, dWLStop, dStep);
            DateTime startUtc = DateTime.UtcNow;
            DateTime lastLogUtc = startUtc;
            int pollCount = 0;

            CommonFunction.WriteLog(string.Format(
                "FSTP wait begin: guid={0} PDL={1} WL={2:F3}-{3:F3} step={4:F4} timeout~{5:F0}s",
                deviceGUID, doPDL, dWLStart, dWLStop, dStep, (deadlineUtc - startUtc).TotalSeconds));

            while (DateTime.UtcNow < deadlineUtc)
            {
                int plSweepStatus;
                int plEstWaitingTime;
                DeviceHandle.fstpCtrl.GetSweepStatus(deviceGUID, out plSweepStatus, out plEstWaitingTime);
                if (!TryGetUdlError(ref errMsg))
                    return 2;

                settings.ExtendDeadlineFromEstimate(ref deadlineUtc, plEstWaitingTime);

                if (plSweepStatus == 1)
                {
                    CommonFunction.WriteLog(string.Format(
                        "FSTP wait done: guid={0} polls={1} elapsed={2:F1}s",
                        deviceGUID, pollCount, (DateTime.UtcNow - startUtc).TotalSeconds));
                    return 0;
                }
                if (plSweepStatus == -1)
                {
                    errMsg = "scan error";
                    return 2;
                }
                if (plSweepStatus != 0)
                {
                    CommonFunction.WriteLog(string.Format(
                        "FSTP unexpected sweep status={0} guid={1}", plSweepStatus, deviceGUID));
                    errMsg = string.Format("unexpected sweep status: {0}", plSweepStatus);
                    return 2;
                }

                pollCount++;
                Thread.Sleep(settings.GetPollIntervalMs(plEstWaitingTime));

                if (settings.LogPollIntervalSec > 0
                    && (DateTime.UtcNow - lastLogUtc).TotalSeconds >= settings.LogPollIntervalSec)
                {
                    lastLogUtc = DateTime.UtcNow;
                    CommonFunction.WriteLog(string.Format(
                        "FSTP waiting: guid={0} est={1}s left={2:F0}s polls={3}",
                        deviceGUID, plEstWaitingTime,
                        Math.Max(0, (deadlineUtc - DateTime.UtcNow).TotalSeconds), pollCount));
                }
            }

            errMsg = "scan time out.";
            CommonFunction.WriteLog(string.Format(
                "FSTP wait timeout: guid={0} polls={1} elapsed={2:F1}s WL={3:F3}-{4:F3} step={5:F4}",
                deviceGUID, pollCount, (DateTime.UtcNow - startUtc).TotalSeconds,
                dWLStart, dWLStop, dStep));
            return 1;
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
                return 2;
            }
            if (pwmIdxs == null || pwmIdxs.Length == 0)
            {
                errMsg = "FSTP PM indexes not initialized.";
                return 2;
            }

            lock (SweepWaitLock)
            {
                int setupRes = SetupScanParameters(dWLStart, dWLStop, dStep, ref errMsg);
                if (setupRes != 0)
                    return setupRes;

                // 首次启动时服务端状态可能尚未稳定，避免因 pre-check 误判阻断首扫。
                // 本进程至少执行过一次后，再启用上次扫描排空保护。
                if (hasExecutedSweepInProcess)
                {
                    int drainRes = DrainInProgressSweep(ref errMsg);
                    if (drainRes != 0)
                        return drainRes;
                }

                int execRes = ExecuteSweep(doPDL, ref errMsg);
                if (execRes != 0)
                    return execRes;

                FstpScanWaitSettings settings = FstpScanWaitSettings.Current;
                int waitRes = WaitForSweepCompletion(doPDL, dWLStart, dWLStop, dStep, ref errMsg);
                if (waitRes == 0)
                    return 0;

                for (int retry = 0; retry < settings.TimeoutRetryCount && waitRes == 1; retry++)
                {
                    CommonFunction.WriteLog(string.Format(
                        "FSTP timeout retry {0}/{1} guid={2}", retry + 1, settings.TimeoutRetryCount, deviceGUID));
                    execRes = ExecuteSweep(doPDL, ref errMsg);
                    if (execRes != 0)
                        return execRes;
                    waitRes = WaitForSweepCompletion(doPDL, dWLStart, dWLStop, dStep, ref errMsg);
                }
                return waitRes;
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
            int scanCode = Scan(doPDL, doRef, dWLStart, dWLStop, dStep, ref errMsg);
            if (scanCode != 0)
                return scanCode;
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
                        return 2;
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
                        return 2;
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
