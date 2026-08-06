using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MolexUtility;

namespace UIOperateInterleaver
{
    /// <summary>
    /// 光谱调节共享目录落盘（meta / spectrum / result），契约见 doc/光谱调节_自动化数据接口.md。
    /// </summary>
    internal static class SpectrumSharePublisher
    {
        public const string ApiVersion = "1.0";

        private static readonly object SeqLock = new object();
        private static string SeqDay = "";
        private static int SeqCounter;

        public sealed class PublishRequest
        {
            public string ShareRoot;
            public string TestProcess;
            public string Sn;
            public int Port;
            public string ScanType;
            public string DataKind;
            public List<double[][]> PortResData;
            public int PmCount;
            public bool UseAveAlgorithm;
            public double[] Shifts;
            public double[] MaxILs;
            public double[] Fsrs;
            public Dictionary<string, double> MinIsoByPort;
        }

        public sealed class PublishResult
        {
            public bool Ok;
            public string DatasetDir;
            public string ErrorMessage;
        }

        public static PublishResult Publish(PublishRequest req)
        {
            var result = new PublishResult();
            if (req == null)
            {
                result.ErrorMessage = "PublishRequest is null.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(req.Sn))
            {
                result.ErrorMessage = "SN is empty.";
                return result;
            }
            if (req.PortResData == null || req.PmCount <= 0)
            {
                result.ErrorMessage = "No spectrum buffer.";
                return result;
            }
            if (req.PortResData.Count < 1 || req.PortResData[0] == null
                || req.PortResData[0].Length < 6 || req.PortResData[0][5] == null || req.PortResData[0][1] == null)
            {
                result.ErrorMessage = "Spectrum buffer incomplete.";
                return result;
            }

            try
            {
                string root = string.IsNullOrWhiteSpace(req.ShareRoot)
                    ? TasRuntimeConfig.GetSpectrumShareRoot()
                    : req.ShareRoot.Trim();
                root = Path.GetFullPath(root);
                string process = SanitizePathSegment(string.IsNullOrWhiteSpace(req.TestProcess) ? "UnknownProcess" : req.TestProcess);
                string sn = SanitizePathSegment(req.Sn);
                string stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                int seq = NextSeq();
                string datasetDir = Path.GetFullPath(Path.Combine(root, process, sn, stamp + "_" + seq.ToString("D3", CultureInfo.InvariantCulture)));
                Directory.CreateDirectory(datasetDir);

                int pointCount = req.PortResData[0][5].Length;
                WriteSpectrumCsv(datasetDir, req.PortResData, req.PmCount, req.UseAveAlgorithm, pointCount);
                WriteMetaJson(datasetDir, req, pointCount);
                WriteResultJson(datasetDir, req);

                result.Ok = true;
                result.DatasetDir = datasetDir;
                return result;
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                CommonFunction.WriteLog("SpectrumSharePublisher.Publish fail: " + ex);
                return result;
            }
        }

        public static string BuildDataReadyLine(string sn, string dataKind, string scanType, string datasetDir)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "DATA;READY;{0};{1};{2};{3};{4}\r\n",
                ApiVersion,
                sn ?? "",
                dataKind ?? "",
                scanType ?? "",
                datasetDir ?? "");
        }

        private static int NextSeq()
        {
            lock (SeqLock)
            {
                string day = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                if (SeqDay != day)
                {
                    SeqDay = day;
                    SeqCounter = 0;
                }
                SeqCounter++;
                return SeqCounter;
            }
        }

        private static string SanitizePathSegment(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_";
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                bool bad = false;
                for (int i = 0; i < invalid.Length; i++)
                {
                    if (c == invalid[i])
                    {
                        bad = true;
                        break;
                    }
                }
                sb.Append(bad ? '_' : c);
            }
            return sb.ToString();
        }

        private static void WriteSpectrumCsv(string dir, List<double[][]> portResData, int pmCount, bool useAve, int pointCount)
        {
            string path = Path.Combine(dir, "spectrum.csv");
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                if (useAve)
                {
                    var title = new StringBuilder("GHZ");
                    for (int i = 0; i < pmCount; i++)
                        title.Append(",AVE-").Append(i + 1);
                    writer.WriteLine(title.ToString());
                    for (int i = 0; i < pointCount; i++)
                    {
                        var line = new StringBuilder(FormatNum(portResData[0][5][i]));
                        for (int j = 0; j < pmCount; j++)
                        {
                            line.Append(',');
                            if (j < portResData.Count && portResData[j] != null && portResData[j][1] != null && i < portResData[j][1].Length)
                                line.Append(FormatNum(portResData[j][1][i]));
                        }
                        writer.WriteLine(line.ToString());
                    }
                }
                else
                {
                    var title = new StringBuilder("GHZ");
                    for (int i = 0; i < pmCount; i++)
                    {
                        title.Append(",AVE-").Append(i + 1);
                        title.Append(",MAX-").Append(i + 1);
                        title.Append(",MIN-").Append(i + 1);
                    }
                    writer.WriteLine(title.ToString());
                    for (int i = 0; i < pointCount; i++)
                    {
                        var line = new StringBuilder(portResData[0][5][i].ToString(CultureInfo.InvariantCulture));
                        for (int j = 0; j < pmCount; j++)
                        {
                            double ave = 0, max = 0, min = 0;
                            if (j < portResData.Count && portResData[j] != null)
                            {
                                if (portResData[j][1] != null && i < portResData[j][1].Length)
                                    ave = portResData[j][1][i];
                                if (portResData[j][3] != null && i < portResData[j][3].Length)
                                    max = portResData[j][3][i];
                                if (portResData[j][4] != null && i < portResData[j][4].Length)
                                    min = portResData[j][4][i];
                            }
                            line.Append(',').Append(ave.ToString(CultureInfo.InvariantCulture));
                            line.Append(',').Append(max.ToString(CultureInfo.InvariantCulture));
                            line.Append(',').Append(min.ToString(CultureInfo.InvariantCulture));
                        }
                        writer.WriteLine(line.ToString());
                    }
                }
            }
        }

        private static string FormatNum(double v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00}", v);
        }

        private static void WriteMetaJson(string dir, PublishRequest req, int pointCount)
        {
            var sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"apiVersion\": \"").Append(EscapeJson(ApiVersion)).Append("\",\r\n");
            sb.Append("  \"sn\": \"").Append(EscapeJson(req.Sn)).Append("\",\r\n");
            sb.Append("  \"testProcess\": \"").Append(EscapeJson(req.TestProcess ?? "")).Append("\",\r\n");
            sb.Append("  \"port\": ").Append(req.Port.ToString(CultureInfo.InvariantCulture)).Append(",\r\n");
            sb.Append("  \"scanType\": \"").Append(EscapeJson(req.ScanType)).Append("\",\r\n");
            sb.Append("  \"dataKind\": \"").Append(EscapeJson(req.DataKind)).Append("\",\r\n");
            sb.Append("  \"createdUtc\": \"").Append(EscapeJson(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))).Append("\",\r\n");
            sb.Append("  \"spectrumFile\": \"spectrum.csv\",\r\n");
            sb.Append("  \"resultFile\": \"result.json\",\r\n");
            sb.Append("  \"pointCount\": ").Append(pointCount.ToString(CultureInfo.InvariantCulture)).Append(",\r\n");
            sb.Append("  \"pmCount\": ").Append(req.PmCount.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("}\r\n");
            File.WriteAllText(Path.Combine(dir, "meta.json"), sb.ToString(), Encoding.UTF8);
        }

        private static void WriteResultJson(string dir, PublishRequest req)
        {
            var sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"apiVersion\": \"").Append(EscapeJson(ApiVersion)).Append("\",\r\n");
            sb.Append("  \"sn\": \"").Append(EscapeJson(req.Sn)).Append("\",\r\n");
            sb.Append("  \"dataKind\": \"").Append(EscapeJson(req.DataKind)).Append("\",\r\n");
            sb.Append("  \"scanType\": \"").Append(EscapeJson(req.ScanType)).Append("\",\r\n");
            sb.Append("  \"shifts\": ").Append(FormatNumberArray(req.Shifts)).Append(",\r\n");
            sb.Append("  \"maxIL\": ").Append(FormatNumberArray(req.MaxILs)).Append(",\r\n");
            sb.Append("  \"fsr\": ").Append(FormatNumberArray(req.Fsrs)).Append(",\r\n");
            sb.Append("  \"minIso\": ").Append(FormatMinIso(req.MinIsoByPort)).Append("\r\n");
            sb.Append("}\r\n");
            File.WriteAllText(Path.Combine(dir, "result.json"), sb.ToString(), Encoding.UTF8);
        }

        private static string FormatNumberArray(double[] values)
        {
            if (values == null || values.Length == 0)
                return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(values[i].ToString("G17", CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string FormatMinIso(Dictionary<string, double> map)
        {
            if (map == null || map.Count == 0)
                return "{}";
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (KeyValuePair<string, double> kv in map)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append('"').Append(EscapeJson(kv.Key)).Append("\": ");
                sb.Append(kv.Value.ToString("G17", CultureInfo.InvariantCulture));
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
