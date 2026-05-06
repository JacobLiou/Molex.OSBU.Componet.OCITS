using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace LibTest
{
    class CurveChart
    {
        private Chart m_CurveChart;

        public CurveChart(Chart _chart)
        {
            m_CurveChart = _chart;
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
                ChartArea area = m_CurveChart.ChartAreas[0];
                area.CursorX.IsUserEnabled = true;
                area.CursorX.AutoScroll = false;
                area.CursorX.IsUserSelectionEnabled = true;
                area.AxisX.ScaleView.Zoomable = true;
                //area.AxisX.Interval = 1;
                //将X轴上格网取消
                area.AxisX.MajorGrid.Enabled = false;
                //X轴、Y轴标题
                area.AxisX.Title = xTitle;
                m_CurveChart.ChartAreas[0].AxisY.Title = yTitle;
                area.AxisX.Maximum = xMax;
                area.AxisX.Interval = xMax / 10;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        public void UpdateChart(string serName,double[] xArr,double[] yArr)
        {        
            //绑定数据源
            object[] invokeChartData = new object[3];
            invokeChartData[0] = serName;
            invokeChartData[1] = xArr;
            invokeChartData[2] = yArr;
            m_CurveChart.BeginInvoke(new UpdateChartDelegate(UpdateChartDelegateMethod), invokeChartData);
        }

        
        public void ClearChart(string serName)
        {
            Series ser = m_CurveChart.Series[serName];
            if (ser == null)
                return;
            ser.Points.Clear();
        }


        public void UpdateChartXSet(string serName, double nNumSum)
        {
            object[] invokeChartData = new object[2];
            invokeChartData[0] = serName;
            invokeChartData[1] = nNumSum;
            m_CurveChart.BeginInvoke(new SeriesDelegateXSet(ChartDelegateXSetMethod), invokeChartData);
        }

        public delegate void SeriesDelegateXSet(string serName, double nNumSum);//声明委托方法
        public void ChartDelegateXSetMethod(string serName, double nNumSum)
        {
            ChartArea area = m_CurveChart.ChartAreas[0];
            if (area != null)
            {
                area.AxisX.Maximum = nNumSum;
                area.AxisX.Interval = nNumSum / 10;
            }
        }

        public delegate void UpdateChartDelegate(string serName,double[] xl, double[] yl);//声明委托方法
        public void UpdateChartDelegateMethod(string serName, double[] xl, double[] yl)
        {
            ChartArea area = m_CurveChart.ChartAreas[0];
            Series ser = m_CurveChart.Series[serName];
            if (area == null||ser==null)
                return;
            double dmax = 0;
            double dmin = 0;
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
            area.AxisY.Maximum = dmax;
            area.AxisY.Minimum = dmin;
            area.AxisY.Interval = (dmax - dmin) / 5;
            
            ser.Points.DataBindXY(xl,yl);
            m_CurveChart.DataBind();
        }
        
        
    }
}
