using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using ProtocolAggregator;
using MolexUtility;
using MolexUtility.Protocol;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

///<summary>
///文件名：UICurveChart.xaml.cs
///作用：曲线显示处理类
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
    /// <summary>
    /// Interaction logic for UICurve.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UICurve")]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class UICurveChart : UserControl
    {
        private string TargetName { get; set; }
        /// <summary>
        /// 将触发者容器注入
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// chart对象
        /// </summary>
        private Chart dataChart=null;

        /// <summary>
        /// 曲线显示基类对象
        /// </summary>
        private CurveChart dataChartControl = null;

        /// <summary>
        /// 曲线的具体信息
        /// </summary>
        private List<ChartDetail> chartDetail = null;

        /// <summary>
        /// 曲线的具体信息
        /// </summary>
        private List<ChartDetail> curDetail = new List<ChartDetail>();

        /// <summary>
        /// 曲线是否显示的选择框
        /// </summary>
        private List<CheckBox> chartShowCheck = null;

        /// <summary>
        /// x轴标题
        /// </summary>
        private string xAixsTitle = "X";

        /// <summary>
        /// y轴标题
        /// </summary>
        private string yAxisTitle = "Y";

        /// <summary>
        /// x轴最大值
        /// </summary>
        private double xMax = -1;

        /// <summary>
        /// x轴原点值
        /// </summary>
        private double xBegin = 0;

        /// <summary>
        /// 曲线颜色
        /// </summary>
        private List<System.Drawing.Color> curveColor=null;

        /// <summary>
        /// 曲线类型
        /// </summary>
        private SeriesChartType dataShowType = SeriesChartType.Line;
        public UICurveChart()
        {
            InitializeComponent();
            TargetName = "";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dataChart = new Chart();
            chartHost.Child = dataChart;
            dataChart.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataChart_MouseDown);
            dataChart.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dataChart_MouseMove);
            dataChart.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataChart_MouseUp);
            dataChart.DoubleClick += DataChart_DoubleClick;
            dataChartControl = new CurveChart(dataChart, "area1");

            chartDetail = new List<ChartDetail>();
            curveColor = new List<System.Drawing.Color>();
            
            curveColor.Add(System.Drawing.Color.Aqua);
            curveColor.Add(System.Drawing.Color.Brown);
            curveColor.Add(System.Drawing.Color.BlueViolet);
            curveColor.Add(System.Drawing.Color.BurlyWood);
            curveColor.Add(System.Drawing.Color.CadetBlue);
            curveColor.Add(System.Drawing.Color.Coral);
            curveColor.Add(System.Drawing.Color.CornflowerBlue);
            curveColor.Add(System.Drawing.Color.Crimson);
            curveColor.Add(System.Drawing.Color.DarkCyan);
            curveColor.Add(System.Drawing.Color.YellowGreen);
            curveColor.Add(System.Drawing.Color.Fuchsia);

            chartShowCheck = new List<CheckBox>();
            CheckBox allShow = new CheckBox();
            allShow.Content = "全部显示";
            allShow.IsChecked = true;
            allShow.Click += AllShowCheckBox_Click;
            allShow.Margin = new Thickness(5, 2, 5, 0);
            allShow.FontSize = 15;
            chartShowCheck.Add(allShow);
            //curveSelect.Children.Add(allShow);
            Compose();
            CurveUpdateRegerster();

        }

        private void DataChart_DoubleClick(object sender, EventArgs e)
        {
             foreach(ChartDetail detal in chartDetail)
            {
                dataChartControl.UpdateChartXSet(detal.SerieName, xBegin, xMax);
                dataChartControl.UpdateChart(detal.SerieName, detal.XArray.ToArray(), detal.YArray.ToArray(),yMin,yMax, xMax);
            }
        }

        private double xZoomMin = 100000000;
        private double xZoomMax = -100000000;
        private double yZoomMin = 100000000;
        private double yZoomMax = -100000000;

        private bool IsSelected = false;
        private bool IsMouseMoved = false;
        private void dataChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!IsSelected)
                return;
            IsMouseMoved = true;
            
        }

        private void dataChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            IsSelected = true;
            var area = dataChart.ChartAreas[0];
            xZoomMin = area.AxisX.PixelPositionToValue(e.X);
            yZoomMin = area.AxisY.PixelPositionToValue(e.Y);
            
        }

        

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        /// <summary>
        /// 与插件通信，将传进的模板信息进行显示
        /// </summary>
        private void CurveUpdateRegerster()
        {
            EventAggregator.GetEvent<EventCurveUpdate>().Subscribe
                (
                    info =>
                    {
                        UpdateCurve(info);
                    }
                );
        }

        /// <summary>
        /// 根据信息更新曲线
        /// </summary>
        /// <param name="info">曲线具体信息</param>
        private void UpdateCurve(CurveUpdateDetail info)
        {
            if (Name != info.TargetName&& Name.Length!=0)
                return;
            if(info.UpdateType==CurveUpdate.Init)
            {                
                SetChartDetail(info.SeriesName, info.XAixsTitle, info.YAxisTitle, info.CurveColor, ConverToSeriesType(info.Type), info.XAixsBegin, info.XAxisEnd, info.XScaleCount);
            }
            else if(info.UpdateType==CurveUpdate.FirstPoint)
            {
                if ((info.XAxisStep.Count > 0) && (info.YAxisValue.Count > 0))
                {
                    UpdateChart(info.SeriesName, info.XAxisStep[0], info.YAxisValue[0], true);
                }
            }
            else if (info.UpdateType == CurveUpdate.AddPoint)
            {
                if (info.XAxisStep.Count > 0 && info.YAxisValue.Count > 0)
                    UpdateChart(info.SeriesName, info.XAxisStep[0], info.YAxisValue[0]);
            }
            else if(info.UpdateType==CurveUpdate.AllPoint)
            {
                UpdateChart(info.SeriesName, info.XAxisStep, info.YAxisValue);
            }
        }

        

        /// <summary>
        /// 从CurveType枚举类型转为SeriesChartType类型
        /// </summary>
        /// <param name="type">CurveType枚举的曲线类型</param>
        /// <returns></returns>
        private SeriesChartType ConverToSeriesType(CurveType type)
        {
            if (type == CurveType.Line)
            {
                return SeriesChartType.Line;
            }
            else
                return SeriesChartType.Point;
        }

        /// <summary>
        /// 是否全部曲线显示复选框响应按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllShowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();

            if (chartShowCheck[0].IsChecked.Value == true)
            {
                foreach (CheckBox box in chartShowCheck)
                {
                    box.IsChecked = true;
                }
                foreach(ChartDetail detal in chartDetail)
                {          
                     dataChartControl.UpdateChart(detal.SerieName, detal.XArray.ToArray(), detal.YArray.ToArray(),yMin,yMax, detal.XMax);
                }
            }
        }

        /// <summary>
        /// 是否显示曲线复选框响应按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
            foreach (CheckBox box in chartShowCheck)
            {
                if (box.IsChecked.Value == false)
                {
                    chartShowCheck[0].IsChecked = false;
                    break;
                }
            }

            
            for(int i=0;i<chartDetail.Count;i++)
            {
                if(chartShowCheck[i+1].IsChecked.Value==true)
                    dataChartControl.UpdateChart(chartDetail[i].SerieName, chartDetail[i].XArray.ToArray(), chartDetail[i].YArray.ToArray(), yMin,yMax,chartDetail[i].XMax);
                else
                    dataChartControl.UpdateChart(chartDetail[i].SerieName, null, null, yMin, yMax, chartDetail[i].XMax);
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
        public void SetChartDetail(string seriesName,string xTitle,string yTitle, System.Drawing.Color clr, SeriesChartType seriesType = SeriesChartType.Line, double begin=0,double dMax=-1,int xScaleCount=-1)
        {
            xAixsTitle = xTitle;
            yAxisTitle = yTitle;
            xMax = dMax;
            xBegin = begin;

            //更新曲线记录信息
            /*ChartDetail cur = null;
            foreach (ChartDetail detail in chartDetail)
            {
                if (detail.SerieName == seriesName)
                {
                    cur = detail;
                    break;
                }
            }

            if (cur == null)
            {
                cur = new ChartDetail();
                cur.SerieName = seriesName;
                chartDetail.Add(cur);
                if (xMax != -1)
                {
                    cur.XMax = xMax;
                }
            }
            else
            {
                if (xMax != -1)
                {
                    cur.XMax = xMax;
                }
            }*/

            dataShowType = seriesType;
            dataChartControl.SetDetail(xTitle, yTitle, begin, dMax,xScaleCount);
            dataChartControl.AddSeries(seriesName, dataShowType, clr, xBegin, xMax, xAixsTitle, yAxisTitle);
        }

        private double yMin;
        private double yMax;

        private void UpdateChart(string serName, List<double> xArray, List<double> yArray)
        {
            ChartDetail cur = null;
            CheckBox curbox = null;
            foreach (ChartDetail detail in chartDetail)
            {
                if (detail.SerieName == serName)
                {
                    cur = detail;
                    break;
                }
            }
            foreach (CheckBox check in chartShowCheck)
            {
                if (check.Content.ToString() == serName)
                {
                    curbox = check;
                    break;
                }
            }
            if (cur == null)
            {
                cur = new ChartDetail();
                cur.SerieName = serName;
                cur.XArray = xArray;
                cur.YArray = yArray;
                chartDetail.Add(cur);
                double dMax = 10;
                if (xMax != -1)
                {
                    dMax = xMax;
                    cur.XMax = xMax;
                }
                System.Drawing.Color lineColor;
                if (chartDetail.Count <= curveColor.Count)
                    lineColor = curveColor[chartDetail.Count - 1];
                else
                    lineColor = System.Drawing.Color.Red;
                dataChartControl.AddSeries(serName, dataShowType, lineColor, xBegin, dMax, xAixsTitle, yAxisTitle);
                if (curbox == null)
                {
                    curbox = new CheckBox();
                    curbox.Content = serName;
                    curbox.Foreground = new SolidColorBrush(Color.FromRgb(lineColor.R, lineColor.G, lineColor.B));
                    curbox.IsChecked = true;
                    curbox.Click += CheckBox_Click;
                    curbox.Margin = new Thickness(5, 2, 5, 0);
                    curbox.FontSize = 20;
                    chartShowCheck.Add(curbox);
                    //curveSelect.Children.Add(curbox);
                }

            }
            
            cur.XArray = xArray;
            cur.YArray = yArray;
            double dXMax = cur.XMax;
            /*if (dXMax < cur.XArray[cur.XArray.Count - 1])
                cur.XMax = cur.XArray[cur.XArray.Count - 1];*/
            if (curbox != null)
            {
                curbox.IsChecked = true;
                yMin = 10000;
                yMax = -10000;
                foreach (ChartDetail detail in chartDetail)
                {
                    foreach(double y in detail.YArray)
                    {
                        if (yMin > y)
                            yMin = y;
                        if (yMax < y)
                            yMax = y;
                    }
                }
                dataChartControl.UpdateChart(serName, cur.XArray.ToArray(), cur.YArray.ToArray(), yMin, yMax, cur.XMax);
            }
            
        }

        private void dataChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!IsMouseMoved)
                return;

            /*double dRatioX = (xZoomMax - xZoomMin) / (SecX - firstX);
            double dRatioY = Math.Abs((yZoomMax - yZoomMin) / (SecY - firstY));

            double xLeft = xZoomMin + (mouseDownX - firstX) * dRatioX;
            double xRight = xZoomMin + (e.X - firstX) * dRatioX;
            double yLeft = yZoomMin - (mouseDownY - firstY) * dRatioY;
            double yRight = yZoomMin - (e.Y - firstY) * dRatioY;*/
            var area = dataChart.ChartAreas[0];
            double xZoomMax = area.AxisX.PixelPositionToValue(e.X);
            double yZoomMax = area.AxisY.PixelPositionToValue(e.Y);
            IsMouseMoved = false;
            IsSelected = false;
            if (Math.Abs(xZoomMin - xZoomMax)<0.01|| Math.Abs(yZoomMin - yZoomMax) < 0.01)
                return;
            if(xZoomMin > xZoomMax)
            {
                double xtemp = xZoomMin;
                xZoomMin = xZoomMax;
                xZoomMax = xtemp;
            }
            if(yZoomMin > yZoomMax)
            {
                double ytemp = yZoomMin;
                yZoomMin = yZoomMax;
                yZoomMax = ytemp;
            }
            
            foreach (ChartDetail detail in chartDetail)
            {
                xZoomMin = (int)xZoomMin;
                xZoomMax = ((int)((xZoomMax - xZoomMin)/10+1))*10+ xZoomMin;
                dataChartControl.UpdateChartXSet(detail.SerieName, xZoomMin, xZoomMax);
                dataChartControl.UpdateChart(detail.SerieName, detail.XArray.ToArray(), detail.YArray.ToArray(), yZoomMin, yZoomMax, xZoomMax,true);
            }
            
            /*if(curDetail.Count==0)
            {
                foreach(ChartDetail detail in chartDetail)
                {
                    ChartDetail cloneDetail = detail.Clone();
                    curDetail.Add(cloneDetail);
                }
            }

            foreach(ChartDetail detail in curDetail)
            {
                List<double> xZoom = new List<double>();
                List<double> yZoom = new List<double>();
                for(int i=0;i<detail.XArray.Count;i++)
                {
                    if(detail.XArray[i]<xZoomMax&&detail.XArray[i]>xZoomMin
                        &&detail.YArray[i]<yZoomMax&&detail)
                }
            }*/
        }

        /// <summary>
        /// 更新曲线
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="xAdd">x轴步长</param>
        /// <param name="yValue">y轴值</param>
        /// <param name="bFisrtPoint">是否是第一个点</param>
        public void UpdateChart(string serName, double xAdd, double yValue,bool bFisrtPoint=false)
        {
            ChartDetail cur=null;
            CheckBox curbox = null;
            foreach (ChartDetail detail in chartDetail)
            {
                if(detail.SerieName==serName)
                {
                    cur = detail;
                    break;
                }
            }
            foreach (CheckBox check in chartShowCheck)
            {
                if (check.Content.ToString() == serName)
                {
                    curbox = check;
                    break;
                }
            }
            if (cur==null)
            {
                cur = new ChartDetail();
                cur.SerieName = serName;              
                cur.XArray.Add(xBegin);                
                cur.YArray.Add(yValue);
                chartDetail.Add(cur);
                double dMax = 10;
                if (xMax != -1)
                {
                    dMax = xMax;
                    cur.XMax = xMax;
                }
                System.Drawing.Color lineColor;
                if (chartDetail.Count <= curveColor.Count)
                    lineColor = curveColor[chartDetail.Count - 1];
                else
                    lineColor = System.Drawing.Color.Red;
                dataChartControl.AddSeries(serName, dataShowType, lineColor, xBegin, dMax, xAixsTitle, yAxisTitle);
                if (curbox == null)
                {
                    curbox = new CheckBox();
                    curbox.Content = serName;
                    curbox.Foreground = new SolidColorBrush(Color.FromRgb(lineColor.R, lineColor.G, lineColor.B));
                    curbox.IsChecked = true;
                    curbox.Click += CheckBox_Click;
                    curbox.Margin = new Thickness(5, 2, 5, 0);
                    curbox.FontSize = 20;
                    chartShowCheck.Add(curbox);
                    //curveSelect.Children.Add(curbox);
                }

            }
            else
            {
                if(bFisrtPoint)
                {
                    cur.XArray.Clear();
                    cur.XArray.Add(xBegin + xAdd);
                    cur.YArray.Clear();
                    cur.YArray.Add(yValue);
                }
                else
                {
                    double xLast = cur.XArray[cur.XArray.Count - 1];
                    cur.XArray.Add(xLast+xAdd);                  
                    cur.YArray.Add(yValue);
                }
                double dXMax = cur.XMax;
                if (dXMax < cur.XArray[cur.XArray.Count - 1])
                    cur.XMax = cur.XArray[cur.XArray.Count - 1];
                
                
                if(curbox!=null)
                {
                    yMin = 10000;
                    yMax = -10000;
                    foreach (ChartDetail detail in chartDetail)
                    {
                        foreach (double y in detail.YArray)
                        {
                            if (yMin > y)
                                yMin = y;
                            if (yMax < y)
                                yMax = y;
                        }
                    }
                    if ((curbox.IsChecked.Value==false)&& (chartShowCheck[0].IsChecked.Value==false))
                    {
                        dataChartControl.UpdateChart(serName, null, null, yMin, yMax);
                    }
                    else
                        dataChartControl.UpdateChart(serName, cur.XArray.ToArray(), cur.YArray.ToArray(), yMin, yMax, cur.XMax);
                }
            }
        }

    }

    public class ChartDetail
    {
        /// <summary>
        /// 曲线名称
        /// </summary>
        public string SerieName { get; set; }

        /// <summary>
        /// x轴值
        /// </summary>
        public List<double> XArray { get; set; }

        /// <summary>
        /// y轴值
        /// </summary>
        public List<double> YArray { get; set; }

        /// <summary>
        /// x轴最大值
        /// </summary>
        public double XMax { get; set; }
        public ChartDetail()
        {
            SerieName = "";
            XMax = -1;
            XArray = new List<double>();
            YArray = new List<double>();
        }

        public ChartDetail Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as ChartDetail;
        }
    }
}
