using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace MoUtilityLib
{
    public class CurveChart
    {
        private Chart m_CurveChart;
        private string m_AreaName;

        public CurveChart(Chart _chart,string areaName)
        {
            m_CurveChart = _chart;
            m_AreaName = areaName;
            m_CurveChart.ChartAreas.Add(m_AreaName);
            
        }

        public void AddSeries(string serName, SeriesChartType serType, System.Drawing.Color clr, double xMax, string xTitle, string yTitle)
        {
            try
            {
                Series newSeries;
                //判断是否已存在
                if (!m_CurveChart.Series.IsUniqueName(serName))
                    newSeries = m_CurveChart.Series[serName];
                else
                    newSeries=m_CurveChart.Series.Add(serName);
                newSeries.ChartType = serType;
                newSeries.Color = clr;
                newSeries.BorderWidth = 2;
                newSeries.IsValueShownAsLabel = false;
                ChartArea area = m_CurveChart.ChartAreas[m_AreaName];
                area.CursorX.IsUserEnabled = true;
                area.CursorX.AutoScroll = false;
                area.CursorX.IsUserSelectionEnabled = true;
                area.AxisX.ScaleView.Zoomable = true;
                //area.AxisX.Interval = 1;
                //将X轴上格网取消
                area.AxisX.MajorGrid.Enabled = false;
                //area.AxisY.MajorGrid.Enabled = false;
                //X轴、Y轴标题
                area.AxisX.Title = xTitle;
                m_CurveChart.ChartAreas[m_AreaName].AxisY.Title = yTitle;
                area.AxisX.Maximum = xMax;
                area.AxisX.Interval = xMax / 10;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        public void UpdateChart(string serName,double[] xArr,double[] yArr,double xTotal)
        {
            UpdateChartXSet(serName, xTotal);
            //绑定数据源
            object[] invokeChartData = new object[3];
            invokeChartData[0] = serName;
            invokeChartData[1] = xArr;
            invokeChartData[2] = yArr;
            m_CurveChart.BeginInvoke(new UpdateChartDelegate(UpdateChartDelegateMethod), invokeChartData);
        }

        public void ClearAllShow()
        {
            foreach (Series ser in m_CurveChart.Series)
            {
                ser.Points.Clear();
            }
        }
        
        public void ClearChart(string serName)
        {
            Series ser = null;
            if (!m_CurveChart.Series.IsUniqueName(serName))
                ser = m_CurveChart.Series[serName];
            if (ser == null)
                return;
            ser.Points.Clear();
        }


        public void UpdateChartXSet(string serName, double nNumSum)
        {
            object[] invokeChartData = new object[2];
            invokeChartData[0] = serName;
            invokeChartData[1] = nNumSum;
            m_CurveChart.BeginInvoke(new SeriesXSetDelegate(ChartDelegateXSetMethod), invokeChartData);
        }

        private delegate void SeriesXSetDelegate(string serName, double nNumSum);//声明委托方法
        private void ChartDelegateXSetMethod(string serName, double nNumSum)
        {
            ChartArea area = m_CurveChart.ChartAreas[m_AreaName];
            if (area != null)
            {
                area.AxisX.Maximum = nNumSum;
                area.AxisX.Interval = nNumSum / 10;
            }
        }

        private delegate void UpdateChartDelegate(string serName, double[] xl, double[] yl);//声明委托方法
        private void UpdateChartDelegateMethod(string serName, double[] xl, double[] yl)
        {
            ChartArea area = m_CurveChart.ChartAreas[m_AreaName];
            Series ser = m_CurveChart.Series[serName];
            if (area == null||ser==null)
                return;
            double dmax = 0;
            double dmin = 0;
            if (yl.Length < 2)
                return;
            if (yl.Length > 0)
            {
                dmax = yl[0];
                dmin = yl[0];
                foreach (double dvalue in yl)
                {
                    if (dmax < dvalue)
                        dmax = dvalue;
                    if (dmin > dvalue)
                        dmin = dvalue;
                }
            }
            //area.AxisY.Maximum = dmax+0.2;

            area.AxisY.Minimum = Math.Round(dmin - 0.3, 1);
            double dinter = (dmax - dmin) / 5 + 0.1;
            area.AxisY.Interval = Math.Round(dinter, 1);
            area.AxisY.Maximum = Math.Round(area.AxisY.Minimum + area.AxisY.Interval * 5, 1);
            ser.Points.DataBindXY(xl,yl);
            m_CurveChart.DataBind();
        }
        
        
    }
}
