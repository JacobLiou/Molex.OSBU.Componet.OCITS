using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility.Protocol;
using ProtocolAggregator;

namespace UIDemuxAdjust
{
    public class DemuxCurve
    {
        private string seriesName = "series0";
        private string areaName = "area1";
        public IEventAggregator EventAggregator { get; set; }
        
        public DemuxCurve(IEventAggregator aggregator)
        {
            EventAggregator = null;
            EventAggregator = aggregator;
        }

        /// <summary>
        /// 更新曲线显示
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="areaName">显示曲线的区域名称</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        private void UpdateCurveShow(string serName, string areaName, List<double> xValues, List<double> yValues)
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.SeriesName = serName;
            curveDetail.UpdateType = CurveUpdate.AllPoint;
            curveDetail.TargetName = areaName;
            curveDetail.XAxisStep = xValues;
            curveDetail.YAxisValue = yValues;

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
            }
        }

        /// <summary>
        /// 更新曲线
        /// </summary>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        public void UpdateCurve( List<double> xValues, List<double> yValues)
        {
            UpdateCurveShow(seriesName, areaName, xValues, yValues);
        }

        /// <summary>
        /// 初始化曲线
        /// </summary>
        /// <param name="xTitle">x轴标题</param>
        /// <param name="yTitle">y轴标题</param>
        /// <param name="xBegin">x轴左侧坐标</param>
        /// <param name="xEnd">x轴最右侧坐标</param>
        /// <param name="serName">曲线名称</param>
        /// <param name="clr">颜色</param>
        /// <param name="targetName">区域名称</param>
        /// <param name="xScaleCount">x轴刻度个数</param>
        public void InitCurve()
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.XAixsTitle = "";
            curveDetail.YAxisTitle = "dB";
            curveDetail.XAixsBegin = 0;
            curveDetail.UpdateType = CurveUpdate.Init;
            curveDetail.XAxisEnd = 100;
            curveDetail.SeriesName = seriesName;
            curveDetail.CurveColor = System.Drawing.Color.Green;
            curveDetail.TargetName = areaName;
            curveDetail.Type = CurveType.Line;
            //if (xScaleCount != -1)
            //    curveDetail.XScaleCount = xScaleCount;
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
            }
        }
    }
}
