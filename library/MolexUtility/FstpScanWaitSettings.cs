using System;
using System.Globalization;
using System.IO;

namespace MolexUtility
{
    /// <summary>
    /// UDL FSTP 扫描等待策略（运行目录 set\FstpScanTimeout.txt，可选）。
    /// 用于按波长范围/步进/服务端预估时间动态计算客户端超时。
    /// </summary>
    public sealed class FstpScanWaitSettings
    {
        private static FstpScanWaitSettings _current;
        private static DateTime _loadedUtc = DateTime.MinValue;
        private static readonly object LoadLock = new object();
        private static readonly TimeSpan ReloadInterval = TimeSpan.FromMinutes(1);

        /// <summary>最小等待秒数（默认 120）。</summary>
        public double MinTimeoutSec { get; private set; }

        /// <summary>最大等待秒数（默认 3600）。</summary>
        public double MaxTimeoutSec { get; private set; }

        /// <summary>按点数估算后的超时倍数（默认 2.0）。</summary>
        public double TimeoutMargin { get; private set; }

        /// <summary>PDL 单点估算秒数（默认 0.05，现场可标定）。</summary>
        public double SecondsPerPointPdl { get; private set; }

        /// <summary>IL 单点估算秒数（默认 0.02）。</summary>
        public double SecondsPerPointIl { get; private set; }

        /// <summary>服务端 plEstWaitingTime 扩展倍数（默认 1.5）。</summary>
        public double EstWaitMultiplier { get; private set; }

        /// <summary>服务端预估之外的固定裕量秒（默认 30）。</summary>
        public double EstWaitMarginSec { get; private set; }

        /// <summary>轮询间隔（毫秒）：服务端预估较短时用（默认 300）。</summary>
        public int PollIntervalFastMs { get; private set; }

        /// <summary>轮询间隔（毫秒）：服务端预估较长时用（默认 1000）。</summary>
        public int PollIntervalSlowMs { get; private set; }

        /// <summary>plEstWaitingTime 超过该秒数时改用慢轮询（默认 30）。</summary>
        public int PollSlowThresholdSec { get; private set; }

        /// <summary>等待中写日志的间隔秒（默认 15）。</summary>
        public int LogPollIntervalSec { get; private set; }

        /// <summary>超时后重新 Execute 扫描的次数（默认 1）。</summary>
        public int TimeoutRetryCount { get; private set; }

        /// <summary>启动新扫描前，若上次仍在进行则最多等待秒数（默认 60）。</summary>
        public double PreExecuteDrainSec { get; private set; }

        public static FstpScanWaitSettings Current
        {
            get
            {
                lock (LoadLock)
                {
                    if (_current == null || DateTime.UtcNow - _loadedUtc > ReloadInterval)
                    {
                        _current = Load();
                        _loadedUtc = DateTime.UtcNow;
                    }
                    return _current;
                }
            }
        }

        public FstpScanWaitSettings()
        {
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            MinTimeoutSec = 120;
            MaxTimeoutSec = 3600;
            TimeoutMargin = 2.0;
            SecondsPerPointPdl = 0.05;
            SecondsPerPointIl = 0.02;
            EstWaitMultiplier = 1.5;
            EstWaitMarginSec = 30;
            PollIntervalFastMs = 300;
            PollIntervalSlowMs = 1000;
            PollSlowThresholdSec = 30;
            LogPollIntervalSec = 15;
            TimeoutRetryCount = 1;
            PreExecuteDrainSec = 60;
        }

        public static FstpScanWaitSettings Load()
        {
            var settings = new FstpScanWaitSettings();
            string path = Path.Combine(Environment.CurrentDirectory, "set", "FstpScanTimeout.txt");
            if (!File.Exists(path))
                return settings;

            try
            {
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    settings.ApplyKey(key, value);
                }
            }
            catch (Exception ex)
            {
                CommonFunction.WriteLog("FstpScanTimeout.txt load failed: " + ex.Message);
            }
            return settings;
        }

        private void ApplyKey(string key, string value)
        {
            switch (key.ToUpperInvariant())
            {
                case "MINTIMEOUTSEC": MinTimeoutSec = ParseDouble(value, MinTimeoutSec); break;
                case "MAXTIMEOUTSEC": MaxTimeoutSec = ParseDouble(value, MaxTimeoutSec); break;
                case "TIMEOUTMARGIN": TimeoutMargin = ParseDouble(value, TimeoutMargin); break;
                case "SECONDSPERPOINTPDL": SecondsPerPointPdl = ParseDouble(value, SecondsPerPointPdl); break;
                case "SECONDSPERPOINTIL": SecondsPerPointIl = ParseDouble(value, SecondsPerPointIl); break;
                case "ESTWAITMULTIPLIER": EstWaitMultiplier = ParseDouble(value, EstWaitMultiplier); break;
                case "ESTWAITMARGINSEC": EstWaitMarginSec = ParseDouble(value, EstWaitMarginSec); break;
                case "POLLINTERVALFASTMS": PollIntervalFastMs = ParseInt(value, PollIntervalFastMs); break;
                case "POLLINTERVALSLOWMS": PollIntervalSlowMs = ParseInt(value, PollIntervalSlowMs); break;
                case "POLLSLOWTHRESHOLDSEC": PollSlowThresholdSec = ParseInt(value, PollSlowThresholdSec); break;
                case "LOGPOLLINTERVALSEC": LogPollIntervalSec = ParseInt(value, LogPollIntervalSec); break;
                case "TIMEOUTRETRYCOUNT": TimeoutRetryCount = ParseInt(value, TimeoutRetryCount); break;
                case "PREEXECUTEDRAINSEC": PreExecuteDrainSec = ParseDouble(value, PreExecuteDrainSec); break;
            }
        }

        private static double ParseDouble(string value, double fallback)
        {
            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                return parsed;
            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                || int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
                return parsed;
            return fallback;
        }

        public DateTime ComputeDeadlineUtc(bool doPDL, double wlStart, double wlStop, double step)
        {
            double span = Math.Abs(wlStop - wlStart);
            double safeStep = step > 1e-9 ? step : 1e-9;
            int points = (int)Math.Ceiling(span / safeStep) + 1;
            double secPerPoint = doPDL ? SecondsPerPointPdl : SecondsPerPointIl;
            double estimatedSec = points * secPerPoint * TimeoutMargin;
            double totalSec = Math.Max(MinTimeoutSec, estimatedSec);
            if (MaxTimeoutSec > 0)
                totalSec = Math.Min(MaxTimeoutSec, totalSec);
            return DateTime.UtcNow.AddSeconds(totalSec);
        }

        public void ExtendDeadlineFromEstimate(ref DateTime deadlineUtc, int plEstWaitingTimeSec)
        {
            if (plEstWaitingTimeSec <= 0)
                return;
            DateTime fromEst = DateTime.UtcNow.AddSeconds(plEstWaitingTimeSec * EstWaitMultiplier + EstWaitMarginSec);
            if (fromEst > deadlineUtc)
                deadlineUtc = fromEst;
            if (MaxTimeoutSec > 0)
            {
                DateTime maxDeadline = DateTime.UtcNow.AddSeconds(MaxTimeoutSec);
                if (deadlineUtc > maxDeadline)
                    deadlineUtc = maxDeadline;
            }
        }

        public int GetPollIntervalMs(int plEstWaitingTimeSec)
        {
            if (plEstWaitingTimeSec >= PollSlowThresholdSec)
                return Math.Max(50, PollIntervalSlowMs);
            return Math.Max(50, PollIntervalFastMs);
        }
    }
}
