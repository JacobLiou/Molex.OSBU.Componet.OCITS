using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UIOperateInterleaverFinalTest;

namespace UIOperateInterleaverFinalTest.Tests
{
    [TestClass]
    public class DemuxScanPortPlannerTests
    {
        private static ScanPortCandidate Port(
            int product, int portIndex, int scanIndex, double tmpt, bool tested, string key)
        {
            return new ScanPortCandidate
            {
                ProductIndex = product,
                PortIndex = portIndex,
                ScanIndex = scanIndex,
                TestTmpt = tmpt,
                IsTested = tested,
                PortKey = key
            };
        }

        [TestMethod]
        public void Demux_SameSnSameTemp_BothUntested_MergesEvenOdd()
        {
            var assists = new List<ScanPortCandidate>
            {
                Port(1, 1, 1, 25, false, "PORT1"),
                Port(1, 2, 1, 25, false, "PORT2"),
            };

            DemuxScanPortPlan plan = DemuxScanPortPlanner.CollectPortsForScan(
                assists, 1, 25, seedScanIndex: 1, isDemuxDual: true);

            CollectionAssert.AreEqual(new[] { 1, 2 }, plan.PortIndexes);
            CollectionAssert.AreEqual(new[] { "PORT1", "PORT2" }, plan.PortKeysInSwitchOrder);
        }

        [TestMethod]
        public void Demux_OneAlreadyTested_OnlyUntestedRemains()
        {
            var assists = new List<ScanPortCandidate>
            {
                Port(1, 1, 1, 25, true, "PORT1"),
                Port(1, 2, 1, 25, false, "PORT2"),
            };

            DemuxScanPortPlan plan = DemuxScanPortPlanner.CollectPortsForScan(
                assists, 1, 25, 1, true);

            CollectionAssert.AreEqual(new[] { 2 }, plan.PortIndexes);
            CollectionAssert.AreEqual(new[] { "PORT2" }, plan.PortKeysInSwitchOrder);
        }

        [TestMethod]
        public void Demux_DifferentTempOrSn_DoesNotMerge()
        {
            var assists = new List<ScanPortCandidate>
            {
                Port(1, 1, 1, 25, false, "PORT1"),
                Port(1, 2, 1, -5, false, "PORT2"),
                Port(2, 1, 1, 25, false, "PORT1"),
            };

            DemuxScanPortPlan at25 = DemuxScanPortPlanner.CollectPortsForScan(assists, 1, 25, 1, true);
            CollectionAssert.AreEqual(new[] { 1 }, at25.PortIndexes);

            DemuxScanPortPlan sn2 = DemuxScanPortPlanner.CollectPortsForScan(assists, 2, 25, 1, true);
            CollectionAssert.AreEqual(new[] { 1 }, sn2.PortIndexes);
        }

        [TestMethod]
        public void NonDemux_MergesByScanIndex_IgnoresIsTested()
        {
            var assists = new List<ScanPortCandidate>
            {
                Port(1, 1, 3, 25, true, "P1"),
                Port(1, 2, 3, 25, false, "P2"),
                Port(1, 3, 4, 25, false, "P3"),
            };

            DemuxScanPortPlan plan = DemuxScanPortPlanner.CollectPortsForScan(
                assists, 1, 25, seedScanIndex: 3, isDemuxDual: false);

            CollectionAssert.AreEqual(new[] { 1, 2 }, plan.PortIndexes);
            CollectionAssert.AreEqual(new[] { "P1", "P2" }, plan.PortKeysInSwitchOrder);
        }

        [TestMethod]
        public void SwitchOrder_IsStableAscendingPortIndex()
        {
            var assists = new List<ScanPortCandidate>
            {
                Port(1, 2, 1, 25, false, "PORT2"),
                Port(1, 1, 1, 25, false, "PORT1"),
            };

            DemuxScanPortPlan plan = DemuxScanPortPlanner.CollectPortsForScan(assists, 1, 25, 1, true);
            CollectionAssert.AreEqual(new[] { 1, 2 }, plan.PortIndexes);
            CollectionAssert.AreEqual(new[] { "PORT1", "PORT2" }, plan.PortKeysInSwitchOrder);
        }

        [TestMethod]
        public void DemuxOutputChannel_Product1_Even17_Odd1()
        {
            Assert.AreEqual(17, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(1, 1));
            Assert.AreEqual(1, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(1, 2));
        }

        [TestMethod]
        public void DemuxOutputChannel_Product2_Even18_Odd2()
        {
            Assert.AreEqual(18, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(2, 1));
            Assert.AreEqual(2, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(2, 2));
        }

        [TestMethod]
        public void DemuxOutputChannel_Invalid_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(0, 1));
            Assert.AreEqual(-1, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(1, 3));
            Assert.AreEqual(-1, DemuxScanPortPlanner.GetDemuxOutputChannelForPortIndex(20, 1)); // 20+16=36 > 32
        }
    }
}
