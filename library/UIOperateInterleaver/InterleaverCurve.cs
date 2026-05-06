using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Protocol;
using ProtocolAggregator;

namespace UIOperateInterleaver
{
    public class InterleaverCurve
    {
        public IEventAggregator EventAggregator { get; set; }
        /// <summary>
        /// 曲线全部显示区域
        /// </summary>
        private string entireArea = "EntireArea";

        /// <summary>
        /// 低频曲线区域
        /// </summary>
        private string lowFreArea = "LowFreArea";

        /// <summary>
        /// 中频曲线区域
        /// </summary>
        private string midFreArea = "MidFreArea";

        /// <summary>
        /// 高频曲线区域
        /// </summary>
        private string highFreArea = "HighFreArea";

        /// <summary>
        /// iso曲线区域
        /// </summary>
        private string isoArea = "ISOArea";

        /// <summary>
        /// PM2曲线名称
        /// </summary>
        private string pm2CurveName = "PM2";

        /// <summary>
        /// PM1曲线名称
        /// </summary>
        private string pm1CurveName = "PM1";

        /// <summary>
        /// ISO合格线曲线名称
        /// </summary>
        private string isoCriterionCurve = "Criterion";

        /// <summary>
        /// 低频起始波长
        /// </summary>
        private double lowFreLeft = 193500.0;

        /// <summary>
        /// 低频终止波长
        /// </summary>
        private double lowFreRight = 194000.0;

        /// <summary>
        /// 中频起始波长
        /// </summary>
        private double midFreLeft = 194500.0;

        /// <summary>
        /// 中频终止波长
        /// </summary>
        private double midFreRight = 195000.0;

        /// <summary>
        /// 高频起始波长
        /// </summary>
        private double highFreLeft = 196000.0;

        /// <summary>
        /// 高频终止波长
        /// </summary>
        private double highFreRight = 196500.0;

        /// <summary>
        /// 整体显示的起始频率
        /// </summary>
        private double entireFreLeft = 191000.0;

        /// <summary>
        /// 整体显示的终止频率
        /// </summary>
        private double entireFreRight = 196500.0;
        public InterleaverCurve(IEventAggregator aggregator)
        {
            EventAggregator = null;
            EventAggregator = aggregator;
        }

        /// <summary>
        /// 初始化曲线
        /// </summary>
        public void InitAllCurve()
        {
            InitCurve("GHz", "dB", entireFreLeft, entireFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, entireArea);
            InitCurve("GHz", "dB", entireFreLeft, entireFreRight, pm2CurveName, System.Drawing.Color.Green, CurveType.Line, entireArea);

            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Point, isoArea);
            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, pm2CurveName, System.Drawing.Color.Green, CurveType.Point, isoArea);
            
            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, isoCriterionCurve, System.Drawing.Color.Brown, CurveType.Point, isoArea);

            InitCurve("低频(GHz)", "dB", lowFreLeft, lowFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, lowFreArea, 4);
            InitCurve("低频(GHz)", "dB", lowFreLeft, lowFreRight, pm2CurveName, System.Drawing.Color.Green, CurveType.Line, lowFreArea, 4);

            InitCurve("中频(GHz)", "dB", midFreLeft, midFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, midFreArea, 4);
            InitCurve("中频(GHz)", "dB", midFreLeft, midFreRight, pm2CurveName, System.Drawing.Color.Green, CurveType.Line, midFreArea, 4);

            InitCurve("高频(GHz)", "dB", highFreLeft, highFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, highFreArea, 4);
            InitCurve("高频(GHz)", "dB", highFreLeft, highFreRight, pm2CurveName, System.Drawing.Color.Green, CurveType.Line, highFreArea, 4);
            

            
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
        /// 更新扫描曲线
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        public void UpdateScanCurve(int port,List<double> xValues, List<double> yValues)
        {
            //1、3PM1CurveName，2、4对应曲线为PM2CurveName
            if (port%3==1)
            {
                UpdateCurveShow(pm1CurveName, entireArea, xValues, yValues);
                UpdateCurveShow(pm1CurveName, lowFreArea, xValues, yValues);
                UpdateCurveShow(pm1CurveName, midFreArea, xValues, yValues);
                UpdateCurveShow(pm1CurveName, highFreArea, xValues, yValues);
            }
            else
            {
                UpdateCurveShow(pm2CurveName, entireArea, xValues, yValues);
                UpdateCurveShow(pm2CurveName, lowFreArea, xValues, yValues);
                UpdateCurveShow(pm2CurveName, midFreArea, xValues, yValues);
                UpdateCurveShow(pm2CurveName, highFreArea, xValues, yValues);
                
            }            
        }

        /// <summary>
        /// 更新ISO曲线
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        public void UpdateISOCurve(int port, List<double> xValues, List<double> yValues)
        {
            //1、3PM1CurveName，2、4对应曲线为PM2CurveName
            if (port % 3 == 1)
            {
                UpdateCurveShow(pm1CurveName, isoArea, xValues, yValues);
            }
            else
            {
                UpdateCurveShow(pm2CurveName, isoArea, xValues, yValues);
            }
        }

        /// <summary>
        /// 更新ISO合格范围曲线
        /// </summary>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        public void UpdateISOCriterionCurve(List<double> xValues, List<double> yValues)
        {
             UpdateCurveShow(isoCriterionCurve, isoArea, xValues, yValues);           
        }

        /// <summary>
        /// 整个曲线起始终止点变化
        /// </summary>
        /// <param name="left">起始频率</param>
        /// <param name="right">终止频率</param>
        public void UpdateEntireFre(double left,double right)
        {
            entireFreLeft = left;
            entireFreRight = right;
            InitCurve("GHz", "dB", entireFreLeft, entireFreRight, pm2CurveName, System.Drawing.Color.Red, CurveType.Line, entireArea);
            InitCurve("GHz", "dB", entireFreLeft, entireFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, entireArea);

            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, pm2CurveName, System.Drawing.Color.Red, CurveType.Point, isoArea);
            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Point, isoArea);
            InitCurve("ISO(GHz)", "dB", entireFreLeft, entireFreRight, isoCriterionCurve, System.Drawing.Color.DarkBlue, CurveType.Point, isoArea);
        }

        /// <summary>
        /// 更新中低高频曲线起始、终止频率，并更新显示
        /// </summary>
        /// <param name="lowL">低频起始频率</param>
        /// <param name="lowR">低频终止频率</param>
        /// <param name="midL">中频起始频率</param>
        /// <param name="midR">中频终止频率</param>
        /// <param name="highL">高频起始频率</param>
        /// <param name="highR">高频终止频</param>
        public void UpdateLowMidHighFre(double lowL,double lowR,double midL,double midR,double highL,double highR)
        {
            lowFreLeft = lowL;
            lowFreRight = lowR;
            midFreLeft = midL;
            midFreRight = midR;
            highFreLeft = highL;
            highFreRight = highR;
           
            InitCurve("低频(GHz)", "dB", lowFreLeft, lowFreRight, pm2CurveName, System.Drawing.Color.Red, CurveType.Line, lowFreArea, 4);
            InitCurve("低频(GHz)", "dB", lowFreLeft, lowFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, lowFreArea, 4);

            InitCurve("中频(GHz)", "dB", midFreLeft, midFreRight, pm2CurveName, System.Drawing.Color.Red, CurveType.Line, midFreArea, 4);
            InitCurve("中频(GHz)", "dB", midFreLeft, midFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, midFreArea, 4);

            InitCurve("高频(GHz)", "dB", highFreLeft, highFreRight, pm2CurveName, System.Drawing.Color.Red, CurveType.Line, highFreArea, 4);
            InitCurve("高频(GHz)", "dB", highFreLeft, highFreRight, pm1CurveName, System.Drawing.Color.DarkBlue, CurveType.Line, highFreArea, 4);
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
        private void InitCurve(string xTitle, string yTitle, double xBegin, double xEnd, string serName, System.Drawing.Color clr, CurveType curveType, string targetName, int xScaleCount = -1)
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.XAixsTitle = xTitle;
            curveDetail.YAxisTitle = yTitle;
            curveDetail.XAixsBegin = xBegin;
            curveDetail.UpdateType = CurveUpdate.Init;
            curveDetail.XAxisEnd = xEnd;
            curveDetail.SeriesName = serName;
            curveDetail.CurveColor = clr;
            curveDetail.TargetName = targetName;
            curveDetail.Type = curveType;
            if (xScaleCount != -1)
                curveDetail.XScaleCount = 4;
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
            }


        }
    }
}
