using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

///<summary>
///文件名：CurveChart
///作用：chart曲线显示基类
///作者：阮锦芳
///编写日期：2018-04-19
///修改记录
///R1：
///		修改作者：作者中文名
///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
///		修改内容：xxx
///</summary>

namespace UICurve
{
    public class CurveChart
    {
        /// <summary>
        /// chart对象
        /// </summary>
        private Chart curveChart;

        /// <summary>
        /// chart区域名称
        /// </summary>
        private string chartAreaName;

        public int XScaleCount { get; set; }

        /// <summary>
        /// 构造函数，获取chart对象并保存，创建area
        /// </summary>
        /// <param name="chart">chart对象</param>
        /// <param name="areaName">area名称</param>
        public CurveChart(Chart chart, string areaName)
        {
            curveChart = chart;
            chartAreaName = areaName;
            curveChart.ChartAreas.Add(chartAreaName);
            XScaleCount = 10;
            /* curveChart.ChartAreas[0].AxisX.ScaleView.Zoom(2, 3);

             // Enable range selection and zooming end user interface
             curveChart.ChartAreas[0].CursorX.IsUserEnabled = true;
             curveChart.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
             curveChart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
             curveChart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;

             //将滚动内嵌到坐标轴中
             curveChart.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;

             // 设置滚动条的大小
             curveChart.ChartAreas[0].AxisX.ScrollBar.Size = 10;*/
            curveChart.BackColor = System.Drawing.Color.AliceBlue;
            LegendCollection legends = curveChart.Legends;
            if (legends.Count == 0)
            {
                legends.Add(new Legend());
                legends[0].Docking = Docking.Top;
                legends[0].Alignment = System.Drawing.StringAlignment.Near;
            }


        }

        /// <summary>
        /// 设置曲线的具体信息
        /// </summary>
        /// <param name="xTitle">x轴标题</param>
        /// <param name="yTitle">y轴标题</param>
        /// <param name="seriesType">曲线类型</param>
        /// <param name="begin">x轴原点坐标</param>
        /// <param name="dMax">x轴最大的坐标</param>
        public void SetDetail(string xTitle, string yTitle, double begin = 0, double dMax = -1,int xScaleCount=-1)
        {
            if(xScaleCount!=-1)
                XScaleCount = xScaleCount;
            ChartArea area = curveChart.ChartAreas[chartAreaName];
            area.AlignmentOrientation = AreaAlignmentOrientations.All;
            area.AlignmentStyle = AreaAlignmentStyles.All;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.AutoScroll = false;
            area.CursorX.IsUserSelectionEnabled = true;
            area.AxisX.ScaleView.Zoomable = false;
            area.CursorY.IsUserEnabled = true;
            area.CursorY.AutoScroll = false;
            area.CursorY.IsUserSelectionEnabled = true;
            area.AxisY.ScaleView.Zoomable = false;
            //area.AxisX.Interval = 1;
            //将X轴上格网取消
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gray;
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gray;
            //area.AxisX.MajorGrid.Enabled = false;
            //area.AxisY.MajorGrid.Enabled = false;
            //X轴、Y轴标题
            area.AxisX.Title = xTitle;
            curveChart.ChartAreas[chartAreaName].AxisY.Title = yTitle;
            if (dMax != -1)
            {
                area.AxisX.Maximum = dMax;
                area.AxisX.Interval = (dMax - begin) / XScaleCount;
                
            }
            area.AxisX.Minimum = begin;
            area.BackColor = System.Drawing.Color.AliceBlue;

            //area.AxisX.Crossing = begin;
            //area.BackColor = System.Drawing.Color.Black;

        }

        /// <summary>
        /// 增加曲线
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="serType">曲线类型，点、线等</param>
        /// <param name="clr">曲线颜色</param>
        /// <param name="xMax">x轴最大值</param>
        /// <param name="xTitle">x轴标题</param>
        /// <param name="yTitle">y轴标题</param>
        public void AddSeries(string serName, SeriesChartType serType, System.Drawing.Color clr,double xBegin, double xMax, string xTitle, string yTitle)
        {
            try
            {
                Series newSeries;
                //判断是否已存在
                if (!curveChart.Series.IsUniqueName(serName))
                    return;
                else
                {
                    newSeries = curveChart.Series.Add(serName);
                    //newSeries.Legend = m_CurveChart.Legends[0].Name;
                }
                newSeries.ChartType = serType;
                newSeries.Color = clr;
                //newSeries.BorderWidth = 2;
                newSeries.IsValueShownAsLabel = false;
                SetDetail(xTitle, yTitle, xBegin, xMax);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        /// <param name="serName">需要更新曲线名称</param>
        /// <param name="xArr">x轴数据</param>
        /// <param name="yArr">y轴数据</param>
        /// <param name="xTotal">x轴最大值</param>
        public void UpdateChart(string serName, double[] xArr, double[] yArr,double yMin,double yMax, double xTotal=-1,bool isZoom=false)
        {
            /*if(xTotal==-1)
            {
                if(xArr!=null)
                    xTotal = xArr[xArr.Length - 1];
            }
            if(xArr!=null&&yArr!=null&&(!isZoom))
                UpdateChartXSet(serName, xArr[0],xTotal);*/
            //绑定数据源
            object[] invokeChartData = new object[5];
            invokeChartData[0] = serName;
            invokeChartData[1] = xArr;
            invokeChartData[2] = yArr;
            invokeChartData[3] = yMin;
            invokeChartData[4] = yMax;
            curveChart.BeginInvoke(new UpdateChartDelegate(UpdateChartDelegateMethod), invokeChartData);
        }


        private delegate void UpdateChartDelegate(string serName, double[] xl, double[] yl, double yMin, double yMax);//声明委托方法
        private void UpdateChartDelegateMethod(string serName, double[] xl, double[] yl, double yMin, double yMax)
        {
            ChartArea area = curveChart.ChartAreas[chartAreaName];
            Series ser = curveChart.Series[serName];
            if (area == null || ser == null)
                return;
            if (xl == null || yl == null)
            {
                ser.Points.Clear();
                return;
            }
           
            if (yl.Length < 2)
            {
                ser.Points.Clear();
                return;
            }

            //area.AxisY.Maximum = dmax+0.2;
            double dinter = Math.Abs(yMax - yMin) / 4.5+0.1;
            //area.AxisY.Minimum = Math.Round(yMin - dinter*0.25, 1);
            area.AxisY.Minimum = Math.Floor((yMin - dinter * 0.25)*10)/10;
            //dinter = 1.1 * dinter;
            area.AxisY.Interval = Math.Round(dinter, 1);
            area.AxisY.Maximum = Math.Round(area.AxisY.Minimum + area.AxisY.Interval * 5, 1);
            ser.Points.DataBindXY(xl, yl);
            curveChart.DataBind();
        }

        /// <summary>
        /// 清楚所有显示
        /// </summary>
        public void ClearAllShow()
        {
            foreach (Series ser in curveChart.Series)
            {
                ser.Points.Clear();
            }
        }

        /// <summary>
        /// 清楚曲线
        /// </summary>
        /// <param name="serName">需要清楚的曲线名称</param>
        public void ClearChart(string serName)
        {
            Series ser = null;
            if (!curveChart.Series.IsUniqueName(serName))
                ser = curveChart.Series[serName];
            if (ser == null)
                return;
            ser.Points.Clear();
        }

        /// <summary>
        /// 更新X轴显示
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="xBegin">x轴其实值</param>
        /// <param name="nNumSum">x轴总共要显示的值</param>
        public void UpdateChartXSet(string serName, double xBegin,double nNumSum)
        {
            object[] invokeChartData = new object[3];
            invokeChartData[0] = serName;
            invokeChartData[1] = xBegin;
            invokeChartData[2] = nNumSum;
            curveChart.BeginInvoke(new SeriesXSetDelegate(ChartDelegateXSetMethod), invokeChartData);
        }

        private delegate void SeriesXSetDelegate(string serName, double xBegin, double nNumSum);//声明委托方法
        private void ChartDelegateXSetMethod(string serName, double xBegin, double nNumSum)
        {
            ChartArea area = curveChart.ChartAreas[chartAreaName];
            if (area != null)
            {
                //area.AxisX.IntervalOffset = 0.4;
                area.AxisX.Minimum = xBegin;
                //area.AxisX.Crossing = xBegin;
                area.AxisX.Maximum = nNumSum;
                area.AxisX.Interval = (nNumSum- xBegin) / XScaleCount;
                //area.AxisX.MinorGrid.Interval = area.AxisX.Interval / 10;
                area.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            }
        }

    }
}
