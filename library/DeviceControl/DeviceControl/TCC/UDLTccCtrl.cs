using System;
using System.Diagnostics;
using System.Threading;
using MolexUtility;
using MolexUtility.Device;

namespace DeviceControl
{
    /// <summary>
    /// UDL 循环箱控制。稳定性策略集中在此（探针 TccTempTest 结论）：
    /// 单次读温约 0.9～1s；与 FSTP 共享 Engine 时必须串行（DeviceHandle.UdlEngineLock）；
    /// 失败重试间隔须 ≥1s；硬失败后可冷却重连 Engine；优先在 UI/STA 线程调用。
    /// </summary>
    public class UDLTccCtrl : IUDLTCC
    {
        /// <summary>产线验证：单次 GetCurrentTemp 约 1s，重试间隔过短无效。</summary>
        private const int MaxAttempts = 5;
        private const int RetryDelayMs = 1200;
        /// <summary>设温后稍等再读回设定值，降低“已下发但读回尚未更新”的误判。</summary>
        private const int SetpointReadbackDelayMs = 400;
        private const double PlausibleTempMinCelsius = -90.0;
        private const double PlausibleTempMaxCelsius = 200.0;
        private const double SetpointMatchToleranceCelsius = 0.1;

        /// <summary>
        /// 设备GUID
        /// </summary>
        public int deviceGUID { get; set; }

        /// <summary>
        /// 读取循环箱温度。以实测是否落在合理温区为准；GetUDLMessage 仅软告警，不单独判失败。
        /// </summary>
        public int GetCurrentTemp(out double getTempr, ref string errMsg)
        {
            double localTemp = CommonFunction.GetDefaultValue();
            string localErr = "";
            int result = UdlUiSync.Send(() =>
            {
                lock (DeviceHandle.UdlEngineLock)
                {
                    int r = GetCurrentTempCore(out localTemp, ref localErr);
                    if (r != 0 && LooksLikeHardUdlFailure(localErr) && DeviceHandle.CanTryReopenUdlEngine())
                    {
                        string reopenErr = "";
                        CommonFunction.WriteLog(
                            "TCC GetCurrentTemp hard fail → TryReopenUdlEngine: " + localErr);
                        if (DeviceHandle.TryReopenUdlEngine(ref reopenErr))
                        {
                            r = GetCurrentTempCore(out localTemp, ref localErr);
                            CommonFunction.WriteLog(string.Format(
                                "TCC GetCurrentTemp after reopen: res={0} err={1}", r, localErr));
                        }
                        else
                        {
                            CommonFunction.WriteLog(
                                "TCC GetCurrentTemp reopen skipped/failed: " + reopenErr);
                        }
                    }
                    return r;
                }
            });
            getTempr = localTemp;
            errMsg = localErr ?? "";
            return result;
        }

        /// <summary>
        /// 设置循环箱温度。以读回设定值是否匹配为准；GetUDLMessage 仅软告警，不单独判失败。
        /// </summary>
        public int SetTempSetpoint(double setTempr, ref string errMsg)
        {
            string localErr = "";
            int result = UdlUiSync.Send(() =>
            {
                lock (DeviceHandle.UdlEngineLock)
                {
                    int r = SetTempSetpointCore(setTempr, ref localErr);
                    if (r != 0 && LooksLikeHardUdlFailure(localErr) && DeviceHandle.CanTryReopenUdlEngine())
                    {
                        string reopenErr = "";
                        CommonFunction.WriteLog(
                            "TCC SetTempSetpoint hard fail → TryReopenUdlEngine: " + localErr);
                        if (DeviceHandle.TryReopenUdlEngine(ref reopenErr))
                        {
                            r = SetTempSetpointCore(setTempr, ref localErr);
                            CommonFunction.WriteLog(string.Format(
                                "TCC SetTempSetpoint after reopen: res={0} err={1}", r, localErr));
                        }
                        else
                        {
                            CommonFunction.WriteLog(
                                "TCC SetTempSetpoint reopen skipped/failed: " + reopenErr);
                        }
                    }
                    return r;
                }
            });
            errMsg = localErr ?? "";
            return result;
        }

        private int GetCurrentTempCore(out double getTempr, ref string errMsg)
        {
            getTempr = CommonFunction.GetDefaultValue();
            if (DeviceHandle.tccCtrl == null)
            {
                errMsg = "TCC object is null.";
                return 1;
            }

            string lastUdlMsg = "";
            Stopwatch swTotal = Stopwatch.StartNew();
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                double reading = CommonFunction.GetDefaultValue();
                string udlMsg = "";
                try
                {
                    DeviceHandle.tccCtrl.GetCurrentTemp(deviceGUID, out reading);
                    DeviceHandle.GetUDLMessage(ref udlMsg);
                }
                catch (Exception ex)
                {
                    lastUdlMsg = ex.Message;
                    CommonFunction.WriteLog(string.Format(
                        "TCC GetCurrentTemp exception attempt={0}/{1} elapsedMs={2}: {3}",
                        attempt, MaxAttempts, sw.ElapsedMilliseconds, ex.Message));
                    if (attempt < MaxAttempts)
                        Thread.Sleep(RetryDelayMs);
                    continue;
                }

                lastUdlMsg = udlMsg ?? "";
                sw.Stop();

                if (IsPlausibleChamberTemperature(reading))
                {
                    getTempr = reading;
                    if (!string.IsNullOrEmpty(lastUdlMsg))
                    {
                        CommonFunction.WriteLog(string.Format(
                            "TCC GetCurrentTemp soft UDL warning (ignored, temp={0:F3}, attempt={1}, ms={2}): {3}",
                            reading, attempt, sw.ElapsedMilliseconds, lastUdlMsg));
                    }
                    else if (attempt > 1)
                    {
                        CommonFunction.WriteLog(string.Format(
                            "TCC GetCurrentTemp OK after retry attempt={0}/{1} temp={2:F3} totalMs={3}",
                            attempt, MaxAttempts, reading, swTotal.ElapsedMilliseconds));
                    }
                    errMsg = "";
                    return 0;
                }

                CommonFunction.WriteLog(string.Format(
                    "TCC GetCurrentTemp invalid attempt={0}/{1} reading={2} udl='{3}' ms={4}",
                    attempt, MaxAttempts, reading, lastUdlMsg, sw.ElapsedMilliseconds));

                if (attempt < MaxAttempts)
                    Thread.Sleep(RetryDelayMs);
            }

            getTempr = CommonFunction.GetDefaultValue();
            errMsg = string.IsNullOrEmpty(lastUdlMsg)
                ? string.Format("循环箱读温失败：未获得有效温度（已重试 {0} 次，间隔 {1}ms）。", MaxAttempts, RetryDelayMs)
                : lastUdlMsg;
            CommonFunction.WriteLog("TCC GetCurrentTemp FAIL: " + errMsg);
            return 1;
        }

        private int SetTempSetpointCore(double setTempr, ref string errMsg)
        {
            if (DeviceHandle.tccCtrl == null)
            {
                errMsg = "TCC object is null.";
                return 1;
            }

            string lastUdlMsg = "";
            Stopwatch swTotal = Stopwatch.StartNew();
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    string udlMsg = "";
                    DeviceHandle.tccCtrl.SetTempSetpoint(deviceGUID, setTempr);
                    DeviceHandle.GetUDLMessage(ref udlMsg);
                    lastUdlMsg = udlMsg ?? "";

                    Thread.Sleep(SetpointReadbackDelayMs);

                    double readback = CommonFunction.GetDefaultValue();
                    string readbackMsg = "";
                    DeviceHandle.tccCtrl.GetTempSetpoint(deviceGUID, out readback);
                    DeviceHandle.GetUDLMessage(ref readbackMsg);
                    if (!string.IsNullOrEmpty(readbackMsg))
                        lastUdlMsg = readbackMsg;

                    sw.Stop();

                    if (Math.Abs(readback - setTempr) <= SetpointMatchToleranceCelsius)
                    {
                        if (!string.IsNullOrEmpty(udlMsg) || !string.IsNullOrEmpty(readbackMsg))
                        {
                            CommonFunction.WriteLog(string.Format(
                                "TCC SetTempSetpoint soft UDL warning (ignored, set={0:F3}, readback={1:F3}, attempt={2}, ms={3}): {4}",
                                setTempr, readback, attempt, sw.ElapsedMilliseconds,
                                string.IsNullOrEmpty(udlMsg) ? readbackMsg : udlMsg));
                        }
                        else if (attempt > 1)
                        {
                            CommonFunction.WriteLog(string.Format(
                                "TCC SetTempSetpoint OK after retry attempt={0}/{1} set={2:F1} totalMs={3}",
                                attempt, MaxAttempts, setTempr, swTotal.ElapsedMilliseconds));
                        }
                        errMsg = "";
                        return 0;
                    }

                    CommonFunction.WriteLog(string.Format(
                        "TCC SetTempSetpoint mismatch attempt={0}/{1} set={2:F1} readback={3} udl='{4}' ms={5}",
                        attempt, MaxAttempts, setTempr, readback, lastUdlMsg, sw.ElapsedMilliseconds));
                }
                catch (Exception ex)
                {
                    lastUdlMsg = ex.Message;
                    CommonFunction.WriteLog(string.Format(
                        "TCC SetTempSetpoint exception attempt={0}/{1}: {2}",
                        attempt, MaxAttempts, ex.Message));
                }

                if (attempt < MaxAttempts)
                    Thread.Sleep(RetryDelayMs);
            }

            errMsg = string.IsNullOrEmpty(lastUdlMsg)
                ? string.Format("循环箱设温失败：读回设定值与目标不一致（目标 {0:F1}°C，已重试 {1} 次）。", setTempr, MaxAttempts)
                : lastUdlMsg;
            CommonFunction.WriteLog("TCC SetTempSetpoint FAIL: " + errMsg);
            return 1;
        }

        private static bool IsPlausibleChamberTemperature(double tempCelsius)
        {
            if (CommonFunction.IsDefault(tempCelsius) ||
                CommonFunction.IsDefault(-tempCelsius))
                return false;
            if (double.IsNaN(tempCelsius) || double.IsInfinity(tempCelsius))
                return false;
            return tempCelsius >= PlausibleTempMinCelsius && tempCelsius <= PlausibleTempMaxCelsius;
        }

        /// <summary>通讯硬失败（含 0x00120091 / 通讯库 / COM 丢失），值得尝试 Engine 重连。</summary>
        private static bool LooksLikeHardUdlFailure(string err)
        {
            if (string.IsNullOrEmpty(err))
                return true;
            string s = err;
            if (s.IndexOf("0x00120091", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (s.IndexOf("通讯库", StringComparison.Ordinal) >= 0)
                return true;
            if (s.IndexOf("TCC object is null", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (s.IndexOf("UDL Engine is null", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (s.IndexOf("未获得有效温度", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }
    }
}
