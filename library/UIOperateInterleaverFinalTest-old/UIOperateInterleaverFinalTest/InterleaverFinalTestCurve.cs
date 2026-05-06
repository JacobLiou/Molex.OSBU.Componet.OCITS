using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Protocol;
using ProtocolAggregator;

namespace UIOperateInterleaverFinalTest
{
    public class InterleaverFinalTestCurve
    {
        public IEventAggregator EventAggregator { get; set; }
        /// <summary>
        /// 曲线全部显示区域
        /// </summary>
        private string entireArea = "EntireArea";

        /// <summary>
        /// 整体显示的起始频率
        /// </summary>
        private double entireFreLeft = 191000.0;

        /// <summary>
        /// 整体显示的终止频率
        /// </summary>
        private double entireFreRight = 196500.0;

        private string[] seriesNames = null;

        private System.Drawing.Color[] lineColors = new System.Drawing.Color[7];
        public InterleaverFinalTestCurve(IEventAggregator aggregator)
        {
            EventAggregator = null;
            EventAggregator = aggregator;
            lineColors[0] = System.Drawing.Color.DarkBlue;
            lineColors[1] = System.Drawing.Color.Green;
            lineColors[2] = System.Drawing.Color.Brown;
            lineColors[3] = System.Drawing.Color.BurlyWood;
            lineColors[4] = System.Drawing.Color.DarkGray;
            lineColors[5] = System.Drawing.Color.DarkOrange;
            lineColors[6] = System.Drawing.Color.DarkSeaGreen;
        }

        /// <summary>
        /// 初始化曲线
        /// </summary>
        public void InitAllCurve(string[] curveNames,bool isFreChanged=false)
        {
            bool isNeedInit = false;
            if (seriesNames == null || seriesNames.Length != curveNames.Length)
            {
                seriesNames = curveNames;
                isNeedInit = true;
            }
            else
            {
                for (int i = 0; i < curveNames.Length; i++)
                {
                    if (seriesNames[i] != curveNames[i])
                    {
                        seriesNames = curveNames;
                        isNeedInit = true;
                        break;
                    }
                }
            }
            if (isNeedInit || isFreChanged)
            {
                for (int i = 0; i < curveNames.Length; i++)
                {
                    if (i < lineColors.Length)
                        InitCurve("GHz", "dB", entireFreLeft, entireFreRight, curveNames[i], lineColors[i], CurveType.Line, entireArea);
                    else
                        InitCurve("GHz", "dB", entireFreLeft, entireFreRight, curveNames[i], System.Drawing.Color.Red, CurveType.Line, entireArea);
                }
            }
        }

        public void ClearAllCurve()
        {
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            for (int i = 0; i < seriesNames.Length; i++)
            {
                UpdateCurveShow(seriesNames[i], x, y);
            }
        }

        /// <summary>
        /// 更新曲线显示
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        public void UpdateCurveShow(string serName, List<double> xValues, List<double> yValues)
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.SeriesName = serName;
            curveDetail.UpdateType = CurveUpdate.AllPoint;
            curveDetail.TargetName = entireArea;
            curveDetail.XAxisStep = xValues;
            curveDetail.YAxisValue = yValues;

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
            }
        }

        

        /// <summary>
        /// 整个曲线起始终止点变化
        /// </summary>
        /// <param name="left">起始频率</param>
        /// <param name="right">终止频率</param>
        public void UpdateFre(double left,double right)
        {
            entireFreLeft = left;
            entireFreRight = right;
            if(seriesNames!=null)
            {
                InitAllCurve(seriesNames,true);
            }
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
