using System;
using System.Collections.Generic;
using System.Linq;

namespace UIOperateInterleaverFinalTest
{
    /// <summary>
    /// 扫描口候选（与 PortAssist 解耦，便于单测）。
    /// </summary>
    public sealed class ScanPortCandidate
    {
        public int ProductIndex { get; set; }
        public int PortIndex { get; set; }
        public int ScanIndex { get; set; }
        public double TestTmpt { get; set; }
        public bool IsTested { get; set; }
        /// <summary>切开关用的端口键（如 PORT1 / L3-4）。</summary>
        public string PortKey { get; set; }

        public ScanPortCandidate()
        {
            PortKey = "";
        }
    }

    /// <summary>
    /// 一次扫描应包含的端口计划。
    /// </summary>
    public sealed class DemuxScanPortPlan
    {
        public List<int> PortIndexes { get; private set; }
        /// <summary>切开关顺序：按 PortIndex 升序，保证可重复。</summary>
        public List<string> PortKeysInSwitchOrder { get; private set; }

        public DemuxScanPortPlan()
        {
            PortIndexes = new List<int>();
            PortKeysInSwitchOrder = new List<string>();
        }
    }

    /// <summary>
    /// Demux / 多口同扫端口合并纯逻辑（无设备 IO）。
    /// Demux：同产品同温未测口全部合并（Even+Odd 一次扫）。
    /// 非 Demux：同产品同 ScanIndex 同温合并（老行为）。
    /// </summary>
    public static class DemuxScanPortPlanner
    {
        public const int DefaultMaxOutputSwitchChannels = 32;

        /// <summary>
        /// Demux 双口出光：PORT1（Even）= productIndex+16；PORT2（Odd）= productIndex。
        /// </summary>
        public static int GetDemuxOutputChannelForPortIndex(
            int productIndex, int portIndex, int maxOutputChannels = DefaultMaxOutputSwitchChannels)
        {
            if (productIndex < 1)
                return -1;
            if (portIndex == 1)
            {
                int evenChannel = productIndex + 16;
                return (evenChannel >= 1 && evenChannel <= maxOutputChannels) ? evenChannel : -1;
            }
            if (portIndex == 2)
            {
                int oddChannel = productIndex;
                return (oddChannel >= 1 && oddChannel <= maxOutputChannels) ? oddChannel : -1;
            }
            return -1;
        }

        /// <summary>
        /// 收集同次扫描端口。assists 为空或无匹配时返回空计划。
        /// </summary>
        /// <param name="seedScanIndex">非 Demux 时用于匹配 ScanIndex（通常取排队首口）。</param>
        public static DemuxScanPortPlan CollectPortsForScan(
            IList<ScanPortCandidate> assists,
            int productId,
            double scanTmpt,
            int seedScanIndex,
            bool isDemuxDual)
        {
            var plan = new DemuxScanPortPlan();
            if (assists == null || assists.Count == 0)
                return plan;

            List<ScanPortCandidate> matched;
            if (isDemuxDual)
            {
                matched = assists
                    .Where(a => a != null
                        && a.ProductIndex == productId
                        && Math.Abs(a.TestTmpt - scanTmpt) < 0.001
                        && !a.IsTested)
                    .OrderBy(a => a.PortIndex)
                    .ToList();
            }
            else
            {
                matched = assists
                    .Where(a => a != null
                        && a.ProductIndex == productId
                        && a.ScanIndex == seedScanIndex
                        && Math.Abs(a.TestTmpt - scanTmpt) < 0.001)
                    .OrderBy(a => a.PortIndex)
                    .ToList();
            }

            var seenPorts = new HashSet<int>();
            foreach (ScanPortCandidate a in matched)
            {
                if (!seenPorts.Add(a.PortIndex))
                    continue;
                plan.PortIndexes.Add(a.PortIndex);
                plan.PortKeysInSwitchOrder.Add(a.PortKey ?? "");
            }
            return plan;
        }
    }
}
