using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.CompilerServices;
using System.Threading;

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;
using MolexUtility.UIList;
//using UDL2_ServerLib;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace UIOperateITLCD
{
    /// <summary>
    /// Interaction logic for OperateITLCD.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateITLCD")]
    public partial class OperateITLCD : UserControl
    {
        public enum BakeStatus
        {
            UnBake = 0,
            Baking,
            BakeComplete
        }

        /// <summary>
        /// 是否正在烤温
        /// </summary>
        private BakeStatus curBakeStatus = BakeStatus.UnBake;

        /// <summary>
        /// 与其他模块通信的事件集 
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// 记录计算的过程数据，用于port参数计算
        /// </summary>
        private List<SamePortParamData> curPortRecords = new List<SamePortParamData>();

        /// <summary>
        /// 设备控制
        /// </summary>
        [Import(typeof(IDeviceHandle))]
        public IDeviceHandle DeviceControl { get; set; }


        [Import(typeof(IInterleaverAlgorithm))]
        private IInterleaverAlgorithm algorithm;

        private const double lightSpeed = 2.99792458E8;

        /// <summary>
        /// 参数计算处理类
        /// </summary>
        private ParamCal paramCal = null;

        /// <summary>
        /// 由主程序传递的工位信息等
        /// </summary>
        private MainInitInfo mainInfo = null;

        private List<string> savePathList = new List<string>();

        /// <summary>
        /// 模板的测试工序
        /// </summary>
        private MESTestProcess testProcess;

        /// <summary>
        /// 模板获取到的最小扫描频率
        /// </summary>
        private double minScanFre = 2000000.0;

        /// <summary>
        /// 模板获取到的最大扫描频率
        /// </summary>
        private double maxScanFre = -2000000.0;

        /// <summary>
        ///扫描时需要设置的参数，从模板里获取 
        /// </summary>
        private double scanSetFreMax = 0;
        private double scanSetFreMin = 0;
        private double scanSetRFModulationFre = 0;
        private double scanSetStep = 0;
        private int scanSetIFBandwidth = 0;

        private double refSetFreMax = 0;
        private double refSetFreMin = 0;
        private bool refIsPDL = false;


        /// <summary>
        /// 扫描得到的数据路径
        /// </summary>
        private string scanRawdataPath = "";

        /// <summary>
        /// 模板类型
        /// </summary>
        private MESTemplateType templateType;

        public UIVariable UIControl = new UIVariable();

        /// <summary>
        /// 测试通过图片
        /// </summary>
        private const string passImage = "\\image\\Pass.ico";

        /// <summary>
        /// 所有产品测试信息
        /// </summary>
        private List<FusionControl> allProductControl;

        /// <summary>
        /// 扫描数据记录，归零时port传实际端口，
        /// </summary>
        private ScanDetail scanDetailInfo = new ScanDetail();

        /// <summary>
        /// 模板里最大的端口号，用来开关切换配置
        /// </summary>
        private int SWMaxPortFlag = 0;

        /// <summary>
        /// 需要更新的参数项在所有测试下中的index，减少后续处理循环需要
        /// </summary>
        private List<int> updateParamIndex = new List<int>();

        /// <summary>
        /// port 和名称对应关系
        /// </summary>
        private Dictionary<string, string> portAndNameDic = new Dictionary<string, string>();
        private List<PortAssist> portAssistant = new List<PortAssist>();

        /// <summary>
        /// 测试失败图片
        /// </summary>
        private const string failImage = "\\image\\Fail.ico";

        /// <summary>
        /// 扫描是否结束
        /// </summary>
        private bool isScanFinished = true;

        /// <summary>
        /// 测试通过图片加载存储对象
        /// </summary>
        private BitmapImage passBitmapImage = null;

        /// <summary>
        /// 测试失败图片加载存储对象
        /// </summary>
        private BitmapImage failBitmapImage = null;

        /// <summary>
        /// 用于显示的模板处理类
        /// </summary>
        private FusionControl testItemShow = null;

        /// <summary>
        /// 归零时间确认后台线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        private string templateName = "";

        /// <summary>
        /// 最老的归零时间，用于4小时归零倒计时
        /// </summary>
        private DateTime oldestRefTime = new DateTime();

        private const int rerefHours = 6;

        /// <summary>
        /// 烤温时间确认后台线程
        /// </summary>
        private BackgroundWorker bakeTimeCheckBK;

        /// <summary>
        /// 模板是否正确打开，并完成
        /// </summary>
        private bool isOpenTemplateComplete = false;

        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 当前测试温度
        /// </summary>
        private double curTestTmpt = -1;

        /// <summary>
        /// 产品有效带宽
        /// </summary>
        private double passBand = 10;

        /// <summary>
        /// 产品两相邻通道频率
        /// </summary>
        private double productFre = 50;

        /// <summary>
        /// 是否带PDL扫描
        /// </summary>
        private bool isPDL = false;

        /// <summary>
        /// 一起扫描的端口号
        /// </summary>
        private List<List<int>> _scanList = new List<List<int>>();

        private List<FusionControl> testShowControl = new List<FusionControl>();

        /// <summary>
        /// 
        /// </summary>
        private bool isAlreadRef = false;  //rjf test

        /// <summary>
        /// 当前是否是一键测试
        /// </summary>
        private bool isOnekeyScan = false;

        double[][] fre1ScanRawdata = null;
        double[][] fre2ScanRawdata = null;

        public ObservableCollection<TestProductInfo> AllProducts { get; set; }
        public OperateITLCD()
        {
            InitializeComponent();
            allProductControl = new List<FusionControl>();
            AllProducts = new ObservableCollection<TestProductInfo>();
            listSNs.ItemsSource = AllProducts;
            UIControl.IsClearSNVisiable = Visibility.Visible;
            txtBoxSN.DataContext = UIControl;
            txtSpec.DataContext = UIControl;
            txtPN.DataContext = UIControl;
            btnSaveToAMTS.DataContext = UIControl;
            btnScanRef.DataContext = UIControl;
            btnOnekeyScan.DataContext = UIControl;
            btnSingleScan.DataContext = UIControl;
            btnClearBakeSN.DataContext = UIControl;

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;

            bakeTimeCheckBK = new BackgroundWorker();
            bakeTimeCheckBK.DoWork += BakeTimeCheck_DoWork;
            bakeTimeCheckBK.ProgressChanged += BakeTimeCheck_Progress;
            bakeTimeCheckBK.WorkerSupportsCancellation = true;
            bakeTimeCheckBK.WorkerReportsProgress = true;

            UIControl.IsReferenceEnable = false;
            UIControl.IsScanEnable = false;
        }

        private void BakeTimeCheck_Progress(object sender, ProgressChangedEventArgs e)
        {
            int time = e.ProgressPercentage;
           
            if (time == 0)
            {
                TemptRemainTime.Text = "烤温完成";
                UIControl.IsClearSNVisiable = Visibility.Visible;
                DoScanOnBK();
            }
            else
            {
                time = time / 1000;
                string timeShow = string.Format("{0}:{1:D2}:{2:D2}", "00", Convert.ToInt32(time / 60), time % 60);
                TemptRemainTime.Text = timeShow;
            }
        }

        private void BakeTimeCheck_DoWork(object sender, DoWorkEventArgs e)
        {
            double totalBakeTime = (double)e.Argument;
            totalBakeTime = totalBakeTime * 1000;
            int beginTick = System.Environment.TickCount;
            while (!bakeTimeCheckBK.CancellationPending)
            {
                int preTickCount = System.Environment.TickCount;
                int EndTickCount = System.Environment.TickCount;
                if ((EndTickCount - beginTick) > totalBakeTime)
                {
                    curBakeStatus = BakeStatus.BakeComplete;
                    bakeTimeCheckBK.ReportProgress(0);
                    return;
                }
                while (EndTickCount - preTickCount < 1000)
                {
                    EndTickCount = System.Environment.TickCount;
                    Thread.Sleep(50);
                }
                double percent = totalBakeTime - (EndTickCount - beginTick);
               
                bakeTimeCheckBK.ReportProgress(Convert.ToInt32(percent));
            }
        }

        private void RefTimeCheck_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!refTimeCheckBK.CancellationPending)
            {
                int preTickCount = System.Environment.TickCount;
                int EndTickCount = System.Environment.TickCount;
                while (EndTickCount - preTickCount < 1000)
                {
                    EndTickCount = System.Environment.TickCount;
                    Thread.Sleep(50);
                }
                refTimeCheckBK.ReportProgress(1);
            }
        }

        private void ClearListData()
        {
            testItemShow = new FusionControl();
            // 更新测试信息
            if (EventAggregator != null)
            {
                List<FusionControl> shows = new List<FusionControl>();
                shows.Add(testItemShow);
                EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
            }
        }

        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool GetOpenTemplateComplete()
        {
            return isOpenTemplateComplete;
        }

        /// <summary>
        /// 过来时间是否超期（未打开模板6小时，打开模板6.5小时）
        /// </summary>
        /// <param name="refSpan"></param>
        /// <returns></returns>
        private bool IsRefTimePassdue(TimeSpan refSpan)
        {
            if (((refSpan.TotalMinutes > rerefHours * 60) && !GetOpenTemplateComplete())
                        || ((refSpan.TotalMinutes > (rerefHours + 0.5) * 60) && GetOpenTemplateComplete()))
                return true;
            return false;
        }

        private void RefTimeCheck_Progress(object sender, ProgressChangedEventArgs e)
        {
            DateTime curTime = DateTime.Now;
            DateTime defaultTime = new DateTime();
            if (oldestRefTime.Equals(defaultTime))
                return;
            //查看系统归零数据是否过期            
            TimeSpan span = curTime - oldestRefTime;
            string timeShow = string.Format("{0}:{1}:{2}", span.Days * 24 + span.Hours, span.Minutes, span.Seconds);
            txtRefTime.Text = timeShow;
            if (span.TotalMinutes > (rerefHours - 0.5) * 60)
                txtRefTime.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            else
                txtRefTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));

            if (IsRefTimePassdue(span))
            {
                /*for (int i = 0; i < portAssistant.Count; i++)
                {
                    string pdlRefPath = string.Format("{0}-product{1}-port{2}.csv", refWithPDLFile, portAssistant[i].ProductIndex, portAssistant[i].PortIndex);
                    if (File.Exists(pdlRefPath))
                        File.Delete(pdlRefPath);
                    //清除内存归零数据 
                    InterleaverScanResult.InitRawdataBuffer(portPDLRef[portAssistant[i].OperateIndex]);

                    portAssistant[i].IsRef = false;
                    UpdateReferenceStatus(portAssistant[i].ProductIndex - 1, portAssistant[i]);
                }*/
                oldestRefTime = new DateTime();
                isAlreadRef = false;
            }
        }

        /// <summary>
        /// 与插件通信，将传进的模板信息进行显示
        /// </summary>
        private void InitRegerster()
        {
            EventAggregator.GetEvent<EventMainInit>().Subscribe
                (
                    info =>
                    {
                        Init(info);
                    }
                );
        }

        /// <summary>
        /// 警告提示
        /// </summary>
        /// <param name="warning">提示信息</param>
        private void WarningBox(string warning)
        {
            MessageBox.Show(warning, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 错误提示
        /// </summary>
        /// <param name="error">错误信息</param>
        private void ErrorBox(string error)
        {
            MessageBox.Show(error, "出错", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 实时状态列表信息显示
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private void RealtimeMsg(string message, StatusType type = StatusType.Normal)
        {
            RealtimeStatusInfo status = new RealtimeStatusInfo();
            status.Status = message;
            status.StatusTime = DateTime.Now.ToString();
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventRealTimeStatus>().Publish(status);
            }
            CommonFunction.WriteLog(message);
        }

        /// <summary>
        /// 接收到主程序已经初始化完成，再进行的初始化动作
        /// </summary>
        /// <param name="info">主程序初始化信息</param>
        public void Init(MainInitInfo info)
        {
            mainInfo = info;
            //testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), mainInfo.TestProcess, true);
            //templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), mainInfo.TemplateType, true);
            string errMsg = "";
            if (mainInfo.MESMode.ToUpper().Contains("MESLESS") || mainInfo.MESMode.ToUpper().Contains("OFFLINE"))
            {
                if (!FusionControl.SetToSpecMode(mainInfo.CheckUser, mainInfo.CheckPSW, "MESLESS", ref errMsg))
                {
                    ErrorBox(errMsg);
                    RealtimeMsg(errMsg);
                    return;
                }
            }

            paramCal = new ParamCal(algorithm);

            string curDir = System.Environment.CurrentDirectory;
            //refWithPDLFile = curDir + refWithPDLFile;
            //scanWithPDLFile = curDir + scanWithPDLFile;

            //IInterleaverScan scan = null;
            //errMsg = "";
            //DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            //if (scan != null)
            //{
            // scanPowermeterCount = scan.PowermeterCount();
            //}
            refTimeCheckBK.RunWorkerAsync();
            //曲线显示初始化
            //curveShow.InitAllCurve();
        }

        /// <summary>
        /// 注册接收选中行变化信息
        /// </summary>
        private void SelectedItemChangeRegister()
        {
            EventAggregator.GetEvent<EventListSelectChanged>().Subscribe
                (
                    info =>
                    {
                        SelectedItemUpdate(info);
                    }
                );
        }

        private void SelectedItemUpdate(IndexMap map)
        {
            selectItem = map.Clone();
            if (selectItem != null && selectItem.ParamIndex.Count > 0)
            {
                int selectIndex = selectItem.ParamIndex[0];
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();
                if (selectIndex >= showInfos.Count)
                    return;
                MESTestInfo selectTestItem = showInfos[selectIndex];

            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
        }

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        private void OperateITLCD_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                if (txtBoxSN.IsFocused)
                {
                    btnOpenTemplate.Focus();
                    btnOpenTemplate_Click(sender, e);

                    e.Handled = true;
                }
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ICDScan cdScan = null;
            string errMsg = "";
            DeviceControl.GetCDScanByIndex(1, ref cdScan, ref errMsg);
            if (cdScan != null)
            {
                cdScan.DisConnect();
            }
            refTimeCheckBK.CancelAsync();
        }

        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            bool isSaveError = false;
            for (int i = 0; i < allProductControl.Count; i++)
            {
                List<AMTSRawdata> rawdatas = new List<AMTSRawdata>();
                for (int j = 0; j < portAssistant.Count; j++)
                {
                    if (portAssistant[j].ProductIndex == (i + 1))
                    {
                        AMTSRawdata data = new AMTSRawdata();
                        data.PortName = portAssistant[j].Name;
                        data.Temperature = portAssistant[j].TestTmpt;
                        data.Rawdata = portAssistant[j].Rawdata;
                        rawdatas.Add(data);
                    }
                }

                string snPath = allProductControl[i].GetSNDir("", ref errMsg);

                for (int j = 0; j < savePathList.Count; j++)
                {
                    if (!savePathList[j].Contains(allProductControl[i].ProductSN))
                        continue;
                    string serverPath = snPath + "\\" + savePathList[j];
                    string strLocalPath = Environment.CurrentDirectory + "\\rawdata\\" + savePathList[j];

                    if (File.Exists(strLocalPath))
                    {
                        File.Copy(strLocalPath, serverPath, true);
                        File.Delete(strLocalPath);
                    }
                }

                string saveDir = snPath + "\\upload";
                Directory.CreateDirectory(saveDir);

                string savePath = saveDir + "\\" + allProductControl[i].ProductSN + ".xml";
                allProductControl[i].SaveTestType("0");
                if (mainInfo.LoginMode.ToUpper().Contains("DEBUG"))
                {
                    allProductControl[i].SavePermsLevel("1");
                }
                else if (mainInfo.LoginMode.ToUpper().Contains("RD"))
                {
                    allProductControl[i].SavePermsLevel("2");
                }
                else
                {
                    allProductControl[i].SavePermsLevel("0");
                }
                allProductControl[i].SaveSoftwareInfo("SOFTWARE2673_ITL_CDFTS", "V1.0.0.0", "Jinfang Ruan", "2023-7-13");
                if (!allProductControl[i].UploadTestData(savePath, out errMsg))
                {
                    ErrorBox(errMsg);
                    isSaveError = true;
                }
            }
            if (!isSaveError)
            {
                AllProducts.Clear();
                TemptRemainTime.Text = "00:00:00";
                allProductControl.Clear();
                testShowControl.Clear();
                ClearListData();
                UIControl.SN = "";
                UIControl.IsSaveEnable = false;
                UIControl.IsScanEnable = false;
                templateName = "";
                ShowTmpltPath();
               
            }
        }

        private void ScanRef_DoWork(object sender, DoWorkEventArgs e)
        {
            ICDScan cdScan = null;
            string errMsg = "";
            DeviceControl.GetCDScanByIndex(1, ref cdScan, ref errMsg);
            if (cdScan != null)
            {
                if(!cdScan.GetIsConnect())
                {
                    errMsg = "CD服务器连接失败";
                }
               
                scanSetRFModulationFre = 2000;
                scanSetIFBandwidth = 300;
                cdScan.SetScanParam(scanSetFreMin, scanSetFreMax, scanSetRFModulationFre, scanSetStep, scanSetIFBandwidth);
                string dataPath = "";
                if (cdScan.Scan(isPDL, true, ref dataPath, ref errMsg) != 0)
                {
                    //把错误信息显示出来 //rjf test
                    //RealtimeMsg("归零出错");
                    //RealtimeMsg(errMsg);
                    isAlreadRef = false;
                    e.Result = 1;
                    return;
                }
                CommonFunction.WriteLog("cdScan.Scan success");
                while(cdScan.GetScanCompleted()<0)
                {
                    Thread.Sleep(100);
                }
                isAlreadRef = true;
                e.Result = 0;
                CommonFunction.WriteLog("GetScanCompleted true");
                //RealtimeMsg("归零完成");
                
            }
            else
            {
                errMsg = "未连接CD服务器";
                e.Result = 1;
            }
        }

        private void ScanRef_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetIsScanFinished(true);
            if (e.Result == null || Convert.ToInt32(e.Result) != 0)
            {
                UIControl.IsScanEnable = false;
                UIControl.IsReferenceEnable = true;
                if(e.Result == null)
                    RealtimeMsg("归零出错");
                else if(Convert.ToInt32(e.Result)==3)
                {
                    RealtimeMsg(string.Format("归零出错:扫描告警，请查看服务器"));
                }
                else
                {
                    RealtimeMsg(string.Format("归零出错:扫描出错，请查看服务器"));
                }
                UpdateReferenceStatus(false);
            }
            else
            {
                UIControl.IsScanEnable = true;
                UIControl.IsReferenceEnable = true;
                oldestRefTime = DateTime.Now;
                refSetFreMin = scanSetFreMin;
                refSetFreMax = scanSetFreMax;
                refIsPDL = isPDL;
                RealtimeMsg("归零完成");
                UpdateReferenceStatus(true);
            }
        }

        private void btnScanRef_Click(object sender, RoutedEventArgs e)
        {
            if (allProductControl.Count == 0)
            {
                WarningBox("请先输入SN打开模板！");
                return;
            }

            if (GetIsScanFinished())
            {
                //切换开关
                if (!SetSwitch(0, "", true))
                    return;
                BackgroundWorker bkRefScan = new BackgroundWorker();
                SetIsScanFinished(false);
                bkRefScan.DoWork += ScanRef_DoWork;
                bkRefScan.RunWorkerCompleted += ScanRef_RunWorkerCompleted;
                //scanDetailInfo.ScanType = scanType;
                bkRefScan.RunWorkerAsync();
                UIControl.IsScanEnable = false;
                UIControl.IsReferenceEnable = false;
                RealtimeMsg("正在归零，请等待...");
            }
            return ;
            
        }

        private bool DoTestBySelectItem(double testTmpt)
        {
            if (!isAlreadRef)
            {
                WarningBox("请先归零再测试!");
                UIControl.IsScanEnable = true;
                return false;
            }
            if (refSetFreMax < scanSetFreMax || refSetFreMin > scanSetFreMin)
            {
                string msg = string.Format("归零波长范围:{0}~{1},测试波长范围:{2}~{3},请重新归零", refSetFreMin, refSetFreMax, scanSetFreMin, scanSetFreMax);
                WarningBox(msg);
                RealtimeMsg(msg);
                UIControl.IsScanEnable = true;
                UpdateReferenceStatus(false);
                return false;
            }
            if(refIsPDL!=isPDL)
            {
                string msg = string.Format("归零是否PDL:{0},与测试要求:{1} 不一致,请重新归零", refIsPDL, isPDL);
                WarningBox(msg);
                RealtimeMsg(msg);
                UIControl.IsScanEnable = false;
                UpdateReferenceStatus(false);
                return false;
            }
            

            
            UIControl.IsScanEnable = false;

            //切换开关
            //SetSwitch(true);
            //判断是否需要烤温
            bool isNeedHeat = false;

            if (curTestTmpt.CompareTo(-300) == 0)
            {
                if (testTmpt > 20 && curTestTmpt < 30)
                {
                    curTestTmpt = testTmpt;
                    isNeedHeat = false;
                }
                else
                    isNeedHeat = true;
            }
            else if (curTestTmpt.CompareTo(testTmpt) != 0)
            {
                isNeedHeat = true;
            }
            

           int nProductIdx=selectItem.ProductIndex + 1;
            
            UIControl.IsScanEnable = false;
            //需要知道选择的是产品几
            
            SetSwitch(scanDetailInfo.ProductIndex, scanDetailInfo.PortFlagName);
            curTestTmpt = testTmpt;

            if (isNeedHeat)
            {
                string prompt = "";
               
                //烤温是否需要增加提示
                prompt = string.Format("是否进行{0}度烤温", testTmpt);
                RealtimeMsg(prompt);
                MessageBoxResult res = MessageBox.Show(prompt, "询问", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    
                    //tmptChangeTimes=
                    foreach (PortAssist assist in portAssistant)
                    {
                        //string[] splits = assist.Name.Split('-');
                        if (scanDetailInfo.ProductIndex == assist.ProductIndex
                            && curTestTmpt == assist.TestTmpt && scanDetailInfo.Ports == assist.Name)
                        {
                            RealtimeMsg(string.Format("开始烤温，时间:{0}", assist.TmptChangeTimes));
                            bakeTimeCheckBK.RunWorkerAsync(assist.TmptChangeTimes * 60);                            
                            break;
                        }
                    }
                    
                }
                else
                {
                    UIControl.IsScanEnable = true;
                    return true;
                }

            }
            else
            {
                /*scanRawdataPath = string.Format("{0}\\rawdata\\CDScanRawdata.csv", Environment.CurrentDirectory);
                 Scan_RunWorkerCompleted(null, null);*/
                DoScanOnBK();
                return true;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetIsScanFinished(bool isFinished)
        {
            isScanFinished = isFinished;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool GetIsScanFinished()
        {
            return isScanFinished;
        }

        private void DoScanOnBK()
        {

            if (GetIsScanFinished())
            {
                SetIsScanFinished(false);
                RealtimeMsg("开始扫描。。。");
                BackgroundWorker bkScan = new BackgroundWorker();
                bkScan.DoWork += Scan_DoWork;
                bkScan.RunWorkerCompleted += Scan_RunWorkerCompleted;
                //scanDetailInfo.ScanType = scanType;

                bkScan.RunWorkerAsync(scanDetailInfo);
            }
            else
            {
                RealtimeMsg("已处于扫描状态");
            }
            
        }
        private void Scan_DoWork(object sender, DoWorkEventArgs e)
        {
            ICDScan cdScan = null;
            string errMsg = "";
            

            DeviceControl.GetCDScanByIndex(1, ref cdScan, ref errMsg);
            if (cdScan != null)
            {
                if (!cdScan.GetIsConnect())
                {
                    errMsg = "CD服务器连接失败";
                }

                scanSetRFModulationFre = 2000;
                scanSetIFBandwidth = 300;
                cdScan.SetScanParam(scanSetFreMin, scanSetFreMax, scanSetRFModulationFre, scanSetStep, scanSetIFBandwidth);
                if (cdScan.Scan(isPDL, false, ref scanRawdataPath, ref errMsg) != 0)
                {
                    //把错误信息显示出来 //rjf test
                    e.Result = 1;
                    return;
                }
                CommonFunction.WriteLog("cdScan.Scan success");
                int nRes = cdScan.GetScanCompleted();
                while (nRes<0)
                {
                    Thread.Sleep(100);
                    nRes = cdScan.GetScanCompleted();
                }
                //RealtimeMsg("CD扫描完成");
                CommonFunction.WriteLog("CD扫描完成");
                e.Result = nRes;
            }
        }

        private void AddStrToList(ref List<string> destList, string src)
        {
            foreach (string str in destList)
            {
                if (str == src)
                {
                    return;
                }
            }
            destList.Add(src);
        }


        /// <summary>
        /// CD扫描ackground dowork执行结束后函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
           
            if (e.Result==null||Convert.ToInt32(e.Result) == 1|| Convert.ToInt32(e.Result) == 2)
            {
                //把错误信息显示出来 //rjf test
                RealtimeMsg(string.Format("CD扫描出错"));
                UIControl.IsScanEnable = true;
                SetIsScanFinished(true);
                return;
            }
            else
            {
                if(Convert.ToInt32(e.Result) == 0)
                    RealtimeMsg(string.Format("CD扫描完成"));
                else
                {
                    RealtimeMsg(string.Format("CD扫描完成:存在告警，请查看服务器"));
                }
                //从数据路径读到数据，并计算结果
                string errMsg = "";
                if (CDScanRes.ReadScanResFromFile(scanRawdataPath, ref fre1ScanRawdata, ref fre2ScanRawdata, ref errMsg) != 0)
                {
                    //出错
                    RealtimeMsg(string.Format("读取扫描数据出错:{0}", errMsg));
                    SetIsScanFinished(true);
                    return;
                }

                foreach (PortAssist assist in portAssistant)
                {
                    //string[] splits = assist.Name.Split('-');
                    if (scanDetailInfo.ProductIndex == assist.ProductIndex 
                        && curTestTmpt == assist.TestTmpt&& scanDetailInfo.Ports== assist.Name)
                    {
                        string fileName = allProductControl[scanDetailInfo.ProductIndex - 1].ProductSN + "_CD_SCAN_" + assist.Name + "_" + mainInfo.TestProcess + "_" + assist.TmptID + ".csv";
                        string strLocalPath = Environment.CurrentDirectory + "\\rawdata\\" + fileName;
                        if (File.Exists(strLocalPath))
                            File.Delete(strLocalPath);
                        File.Copy(scanRawdataPath, strLocalPath);
                        AddStrToList(ref savePathList, fileName);

                        assist.IsTested = true;
                        RealtimeMsg(string.Format("产品:{0},prot:{1},tempr:{2} IsTested", scanDetailInfo.ProductIndex, curTestTmpt, scanDetailInfo.Ports));
                    }
                }


                CommonFunction.WriteLog("ReadScanResFromFile");
                //计算参数结果
                curPortRecords.Clear();
                CommonFunction.WriteLog("curPortRecords");
                CalChannelRes(ref errMsg);
                CommonFunction.WriteLog("CalChannelRes");
                CalPortRes(ref errMsg);
                CalBPParamRes(ref errMsg);
                CommonFunction.WriteLog("CalPortRes");
                ParamItemUpdate(scanDetailInfo.ProductIndex - 1);
                if (isOnekeyScan)
                {
                    //自动开始下一项测试
                    SetIsScanFinished(true);
                    OnekeyScan();
                }
                else
                {
                    UIControl.IsScanEnable = true;
                    CommonFunction.WriteLog("UIControl.IsScanEnable = true");
                    SetIsScanFinished(true);
                }
                
            }
        }

        /// <summary>
        /// 把测试结果放到记录的list中
        /// </summary>
        /// <param name="records">用于记录所有数据的列表</param>
        /// <param name="param">参数名称</param>
        /// <param name="res">参数计算结果</param>
        /// <param name="errMsg">出错信息</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void AddResultToRecord(List<SamePortParamData> records, string param, string port, string tempreture, double res, ref string errMsg)
        {
            try
            {
                if (records == null)
                {
                    errMsg = "记录数据List未初始化！";
                }
                bool isFind = false;
                //找到对应的参数记录，并增加
                foreach (SamePortParamData data in records)
                {
                    if (data.ParamName.Length > 0 && data.ParamName.ToUpper() == param.ToUpper() &&
                        data.Tempreture == tempreture && data.Port == port)
                    {
                        data.Results.Add(res);
                        isFind = true;
                        break;
                    }
                }

                //如果为找到，则创建一个新的放入列表中
                if (!isFind)
                {
                    SamePortParamData newParam = new SamePortParamData();
                    newParam.ParamName = param;
                    newParam.Tempreture = tempreture;
                    newParam.Port = port;
                    newParam.Results.Add(res);
                    records.Add(newParam);
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        private bool SetSwitch(int productIndex, string portName,bool isDoRef=false)
        {
            string flag = "";
            if (isDoRef)
            {
                flag = "CDREFROUTING";
            }
            else
            {
                string flagPort = portName.Replace(" ", "");
                //flag最后一位用模板里port最大index，兼容产品两个port都是odd或者even的情况
                flag = productIndex.ToString() + "::" + flagPort.ToUpper() + ":" + SWMaxPortFlag.ToString();
            }
            //RealtimeMsg("开始切换开关");
            string errMsg = "";
            IOpticalSwitch opticalSwitch = null;
            //if (DeviceControl.GetSwitchByType("InterleaverFinalTestSwitch", ref opticalSwitch, ref errMsg) == 0)
            if (DeviceControl.GetSwitchByIndex(1, ref opticalSwitch, ref errMsg) == 0)
            {
                if (opticalSwitch != null)
                {
                    if (opticalSwitch.SetSwitch(flag, ref errMsg) == 0)
                    {
                        RealtimeMsg("切换开关成功！");
                    }
                }
            }
            if (errMsg.Length > 0)
            {
                RealtimeMsg("切换开关失败:" + errMsg);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算port参数
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void CalPortRes( ref string errMsg)
        {
            try
            {
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();

                MESTestInfo selectTestItem = showInfos[selectItem.ParamIndex[0]];
                int nProductIdx = selectItem.ProductIndex;
                string testPortName = selectTestItem.PortNameForUser;
                List<MESTestInfo> allTestParam = allProductControl[nProductIdx].GetAllTestInfo();
                /*for (int i = 0; i < allTestParam.Count; i++)
                {
                    if (!allTestParam[i].Tested)
                        continue;
                    string param = allTestParam[i].ExParamName;
                    string[] paramSplits = param.Split('@');
                    //string maxILParam = "MAXIL@PB=" + passBand.ToString();
                    if (paramSplits[0].ToUpper() != "MAXIL")
                        continue;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');
                    if (portSplits.Length >= 2)
                    {
                        double paramResult = allTestParam[i].CurValue;
                        AddResultToRecord(curPortRecords, param, portSplits[0], allTestParam[i].Temperature.ToString(), paramResult, ref errMsg);
                    }
                }*/
                for (int i = 0; i < allTestParam.Count; i++)
                {
                    string param = allTestParam[i].ExParamName;
                    if (allTestParam[i].ExParamName.Contains("_BP"))
                        continue;
                    if (allTestParam[i].Temperature.CompareTo(curTestTmpt) != 0)
                    {
                        string[] paramSplits = param.Split('@');
                        if (paramSplits[0].ToUpper() != "TDL")
                            continue;
                    }

                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');
                    //判断是总的端口，然后使用之前的计算结果，进行计算。
                    if (portSplits.Length == 1)
                    {                      
                        //判断是不是需要计算的port
                        bool bTestPort = false;
                       
                            if (testPortName == portSplits[0])
                            {
                                bTestPort = true;
                            }
                        
                        if (!bTestPort)
                            continue;

                        bool isPass = true;
                        //计算参数结果
                        double paramResult = paramCal.CalPortParam(param, allTestParam[i].Temperature.ToString(), portSplits[0], curPortRecords, allProductControl[nProductIdx].TmptArray(), ref errMsg);

                        if (errMsg.Length == 0 && (!CommonFunction.IsDefault(paramResult)))
                        {
                            paramResult = Math.Round(paramResult, 3);
                            allProductControl[nProductIdx].UpdateTestData(i, paramResult, ref isPass);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 计算参数函数
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void CalChannelRes(ref string errMsg)
        {
            try
            {
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();

                MESTestInfo selectTestItem = showInfos[selectItem.ParamIndex[0]];
                int nProductIdx = selectItem.ProductIndex;
                string testPortName = selectTestItem.PortNameForUser;
                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[nProductIdx].GetAllTestInfo();

                var typeName = algorithm.GetType();
                IInterleaverAlgorithm interleaverAlgorithm = (IInterleaverAlgorithm)Activator.CreateInstance(typeName);
                ParamCal calFuntion = new ParamCal(interleaverAlgorithm);
                int paramCount = allTestParam.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    if (allTestParam[i].Temperature.CompareTo(curTestTmpt) != 0)
                    {
                        continue;
                    }
                    string param = allTestParam[i].ExParamName;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');

                    if (portSplits.Length > 2)
                    {
                        double fre = Convert.ToDouble(portSplits[portSplits.Length - 2]);
                        string portIndex = portSplits[portSplits.Length - 1];
                        //解析出端口号，从最后开始往前找数字
                        int numBeginIndex = portIndex.Length;
                        for (int j = portIndex.Length - 1; j >= 0; j--)
                        {
                            if (CommonFunction.IsNumber(portIndex[j]))
                            {
                                numBeginIndex = j;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        //不是扫描的温度，则返回
                        portName = portSplits[0];
                        //不是当前扫描的通道
                        if (testPortName != portName)
                        {
                            continue;
                        }
                        bool isPass = true;
                        //计算参数结果
                        double paramResult = CommonFunction.GetDefaultValue();
                                            
                        paramResult = calFuntion.CalChannelTestParam(param, fre1ScanRawdata, fre2ScanRawdata, fre, productFre, ref errMsg);                 
                        paramResult = Math.Round(paramResult, 3);
                        AddResultToRecord(curPortRecords, param, portSplits[0], allTestParam[i].Temperature.ToString(), paramResult, ref errMsg);
                        if (errMsg.Length == 0)
                        {
                            allProductControl[nProductIdx].UpdateTestData(i, paramResult, ref isPass);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }


        }

        private void btnOnekeyScan_Click(object sender, RoutedEventArgs e)
        {
            OnekeyScan();
        }

        private void btnSingleScan_Click(object sender, RoutedEventArgs e)
        {
            isOnekeyScan = false;
            if (selectItem != null && selectItem.ParamIndex.Count > 0)
            {
                int selectIndex = selectItem.ParamIndex[0];
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();
                if (selectIndex >= showInfos.Count)
                    return;
                MESTestInfo selectTestItem = showInfos[selectIndex];

                //string[] portNames = selectTestItem.PortNameForUser.Split('-')

                scanDetailInfo.Ports = "";
                scanDetailInfo.PortFlagName = "";
                //获取同时扫描的端口号。
                //进光端一样，则可以一起扫描，比如in-to,in-te,in-moni同时扫描
                int scanIndex = -1;
                double tmptChangeTimes = 0;
                foreach (PortAssist assist in portAssistant)
                {
                    if (scanIndex == -1)
                    {
                        if (selectTestItem.PortNameForUser == assist.Name)
                        {
                            scanIndex = assist.PortIndex;
                        }
                    }
                    //string[] splits = assist.Name.Split('-');
                    if ((selectItem.ProductIndex + 1) == assist.ProductIndex && scanIndex == assist.PortIndex
                        && selectTestItem.Temperature == assist.TestTmpt)
                    {
                        tmptChangeTimes = assist.TmptChangeTimes;
                        scanDetailInfo.PortFlagName = assist.Port;
                        scanDetailInfo.Ports = assist.Name;
                    }
                }

                string errMsg = "";

                scanDetailInfo.ProductIndex = selectItem.ProductIndex + 1;

                DoTestBySelectItem(selectTestItem.Temperature);
            }
           
            
        }

        private void btnClearBakeSN_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("正在测试，是否要清空列表！", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                AllProducts.Clear();
                allProductControl.Clear();
                ClearListData();
                TemptRemainTime.Text = "00:00:00";
                UIControl.SN = "";
            }
        }

        /// <summary>
        /// 解析高频、低频、中频的左右频率，有效带宽，相邻隔离频率
        /// </summary>
        /// <param name="source">待解析字符串</param>
        /// <param name="leftFre">左频率</param>
        /// <param name="rightFre">右频率</param>
        private void ParserRange(string source, ref double leftFre, ref double rightFre)
        {
            string[] splits = source.Split('~');
            if (splits.Length > 1)
            {
                leftFre = Convert.ToDouble(splits[0]);
                rightFre = Convert.ToDouble(splits[1]);
            }
        }

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            //rjf test
            /*string strGoldsampleSN = mainInfo.Goldsample;
            errMsg = string.Format("goldsampleSN:{0},userID:{1}", strGoldsampleSN, mainInfo.UserID);
            RealtimeMsg(errMsg);
            errMsg = "";
            if (!FusionControl.GoldsampleCheck(strGoldsampleSN, mainInfo.UserID, "", ref errMsg))
            {
                string errPrmpt = string.Format("Goldsample验证失败{0}:{1}", strGoldsampleSN, errMsg);
                MessageBox.Show(errPrmpt, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }*/
            if (UIControl.SN != null && UIControl.SN.Length == 0)
            {
                WarningBox("请输入产品号！！");
                return;
            }
            if (mainInfo == null)
            {
                ErrorBox("无工位信息，请检查配置！");
                return;
            }

            if (allProductControl.Count >= 4)
            {
                ErrorBox("该工位最多支持测试4个3端口产品！");
                return;
            }
            RealtimeMsg("正在打开模板...");
            curTestTmpt = -300;
            UIControl.IsSaveEnable = false;
            UIControl.IsScanEnable = false;
            BackgroundWorker templateBK = new BackgroundWorker();
            templateBK.DoWork += OpenTemplateBK_DoWork;
            templateBK.RunWorkerCompleted += OpenTemplateBK_RunWorkerCompleted;
            templateBK.RunWorkerAsync();
        }

        private void OpenTemplateBK_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (allProductControl.Count == 1)
            {
                _scanList.Clear();
                //打开注意事项网页
                string strUrl = string.Format("\\\\zh-mfs-srv\\Public\\TestTemplate\\{0}\\{1}@{2}.HTML",
                    allProductControl[0].GetProductInfo().ProductPN, allProductControl[0].GetProductInfo().ProductPN, allProductControl[0].GetProductInfo().SpecNum);
                ProcessStartInfo info = new ProcessStartInfo();
                info.WindowStyle = ProcessWindowStyle.Normal;
                info.FileName = strUrl;//需要启动的程序
                if (File.Exists(strUrl))
                {
                    Process.Start(info);
                }

            }
            string errMsg = (string)e.Result;
            if (errMsg.Length == 0)
            {
                RealtimeMsg(UIControl.SN + "：打开模板成功！");
                SetOpenTemplateComplete(true);

                TestProductInfo curInfo = new TestProductInfo();
                curInfo.Index = AllProducts.Count + 1;
                curInfo.SN = UIControl.SN;
                AllProducts.Add(curInfo);
                //列表显示  
                //曲线显示处理
                IndexMap nextSeleted = new IndexMap();
                List<MESTestInfo> testInfos = allProductControl[allProductControl.Count - 1].AllTestInfo;
                if (allProductControl.Count == 1)
                {
                    updateParamIndex.Clear();
                    portAndNameDic.Clear();
                    portAssistant.Clear();

                    int rangCount = 0;
                    CFGRecordInfo[] cfgInfo = allProductControl[allProductControl.Count - 1].CFGInfo.ToArray();
                    for (int i = 0; i < cfgInfo.Length; i++)
                    {
                        string param = cfgInfo[i].Name.ToUpper();
                        if (param == "PASSBAND")
                        {
                            passBand = Convert.ToDouble(cfgInfo[i].Value);
                        }
                        else if (param == ("ProductFrequency").ToUpper())
                        {
                            productFre = Convert.ToDouble(cfgInfo[i].Value);
                        }
                        else if (param == ("PDL").ToUpper())
                        {
                            if (Convert.ToInt32(cfgInfo[i].Value) == 0)
                                isPDL = false;
                            else
                                isPDL = true;
                        }
                        else if (param == "LFRANGE")
                        {
                            double lowFreLeft = 0;
                            double lowFreRight = 0;

                            ParserRange(cfgInfo[i].Value, ref lowFreLeft, ref lowFreRight);
                            if (lowFreLeft.CompareTo(0) != 0 && lowFreRight.CompareTo(0) != 0)
                            {
                                scanSetFreMin = lightSpeed / lowFreRight;
                                scanSetFreMax = lightSpeed / lowFreLeft;
                            }
                            
                        }
                        else if (param == "STEP")
                        {
                            scanSetStep = Convert.ToDouble(cfgInfo[i].Value) / 1000;
                        }

                    }
                    maxScanFre = -2000000.0;
                    minScanFre = 2000000.0;
                }


                Dictionary<string, int> inportDic = new Dictionary<string, int>();
                for (int i = 0; i < testInfos.Count; i++)
                {
                    //转Fusion之后按照之前规则重组EX规则的参数名称
                    if (testInfos[i].TestParam.GetMESTemplateKeywords().Contains("_BP"))
                    {
                        testInfos[i].ExParamName = testInfos[i].TestParam + "@";
                        testInfos[i].ExParamName += testInfos[i].ConditionID;
                    }
                    else
                    {
                        testInfos[i].ExParamName = testInfos[i].TestParam + "@";
                        if (testInfos[i].Passband.ToUpper() == "ITU")
                            testInfos[i].ExParamName += "ITU";
                        else
                        {
                            testInfos[i].ExParamName += "PB=" + testInfos[i].Passband;
                        }
                        if (testInfos[i].Deepth != null && testInfos[i].Deepth != "")
                        {
                            testInfos[i].ExParamName += ";DH=" + testInfos[i].Deepth;
                        }
                    }
                    testInfos[i].ParamType = MESParamRule.EX;
                    testInfos[i].ParamColumnName = testInfos[i].ExParamName;

                    string param = testInfos[i].ExParamName.ToUpper();
                    //通道名_频率_porti
                    string[] splits = testInfos[i].PortNameForUser.Split('_');
                    double tmpt = testInfos[i].Temperature;
                    if (splits.Length > 2)
                    { 
                        if (allProductControl.Count == 1)
                        {
                            double fre = Convert.ToDouble(splits[1]);
                            if (minScanFre > fre)
                                minScanFre = fre;
                            if (maxScanFre < fre)
                                maxScanFre = fre;
                        }

                        if (!IsContainPortAssist(allProductControl.Count, splits[splits.Length - 3], tmpt))
                        {
                            PortAssist assist = new PortAssist();
                            assist.Name = splits[splits.Length - 3];
                            assist.Port = splits[splits.Length - 1];
                            assist.ProductIndex = allProductControl.Count;
                            assist.PortIndex = Convert.ToInt32(assist.Port.Remove(0, 4));
                            assist.TestTmpt = tmpt;
                            assist.TmptChangeTimes = testInfos[i].TmptChangeTimes;
                            if (assist.ProductIndex == 1)
                                assist.OperateIndex = assist.PortIndex - 1;
                            else
                                assist.OperateIndex = assist.ProductIndex * portAndNameDic.Count + assist.PortIndex - 1;

                            //决定了一起扫描的端口
                            
                            
                            
                              
                                    if (!inportDic.ContainsKey(assist.Name))
                                    {
                                        inportDic.Add(assist.Name, inportDic.Count + 1);
                                    }
                                    assist.ScanIndex = inportDic[assist.Name];
                                
                            
                            //else
                            //assist.PMIndex = assist.PortIndex;
                            assist.TmptID = testInfos[i].EnvironmentID;
                            portAssistant.Add(assist);

                            if (SWMaxPortFlag < Convert.ToInt32(assist.Port.Remove(0, 4)))
                                SWMaxPortFlag = Convert.ToInt32(assist.Port.Remove(0, 4));
                        }
                    }
                    else
                    {
                        
                        nextSeleted.ProductIndex = 0;
                        nextSeleted.ParamIndex.Clear();
                        nextSeleted.ParamIndex.Add(i);
                    }
                }

                if (allProductControl.Count == 1)
                {
                    UIControl.PN = allProductControl[0].GetProductInfo().ProductPN;
                    UIControl.Spec = allProductControl[0].GetProductInfo().SpecNum;
                    string[] portNames = portAndNameDic.Keys.ToArray();
                    //曲线显示初始化
                    minScanFre = minScanFre - productFre;
                    maxScanFre = maxScanFre + productFre;

                }

                ReadRefData(allProductControl.Count - 1, portAssistant, ref errMsg);
                ParamItemUpdate(allProductControl.Count - 1, true);
                UIControl.IsReferenceEnable = true;
                if (errMsg.Length > 0)
                {
                    WarningBox(errMsg);
                }

                ShowTmpltPath();

                if ((refSetFreMax < scanSetFreMax || refSetFreMin > scanSetFreMin||refIsPDL!=isPDL)&&isAlreadRef)
                {
                    string msg = string.Format("归零设置与测试设置不一致,请重新归零");
                    MessageBox.Show(msg);
                    RealtimeMsg(msg);
                    UIControl.IsScanEnable = false;
                    UpdateReferenceStatus(false);
                    return;
                }
                else
                {
                    UIControl.IsScanEnable = true;
                }

                /*if (allProductControl.Count == 1)
                {
                    UpdateItem(allProductControl[0].AllTestInfo[allProductControl[0].AllTestInfo.Count - 1], 0, 0, nextSeleted);
                    BackgroundWorker bk = new BackgroundWorker();
                    bk.DoWork += SelctToItemBegin_DoWork;
                    bk.RunWorkerCompleted += SelctToItemBegin_RunWorkerCompleted;
                    bk.RunWorkerAsync();
                }*/

            }
            else
            {
                RealtimeMsg(errMsg, StatusType.Error);
                ErrorBox(errMsg);
                return;
            }
        }

        private void CalBPParamRes(ref string errMsg)
        {
            List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();

            MESTestInfo selectTestItem = showInfos[selectItem.ParamIndex[0]];
            int nProductIdx = selectItem.ProductIndex;

            FusionControl loadDatacontrol = new FusionControl();
            List<MESTestInfo> allTestParam = allProductControl[nProductIdx].GetAllTestInfo();
            string bpPN = "";
            string bpProcess = "";
            for (int i = 0; i < allTestParam.Count; i++)
            {
                string param = allTestParam[i].ExParamName;
                if (!param.Contains("_BP"))
                {
                    continue;
                }
                string[] bpSetSplits = allTestParam[i].BPParamSet.Split('@');
                string[] curSetSplits = allTestParam[i].BPCurrentSet.Split('@');
                if (bpSetSplits.Length < 7 || curSetSplits.Length < 5)
                    continue;
                if (bpProcess != bpSetSplits[1])
                {
                    bpProcess = bpSetSplits[1];
                    string strErr = "";
                    loadDatacontrol.LoadTestData(allProductControl[nProductIdx].ProductSN, bpProcess, mainInfo.UserID, out strErr);
                    if (strErr.Length > 0)
                    {
                        errMsg = strErr;
                        CommonFunction.WriteLog(errMsg);
                        return;
                    }
                }

                double bpValue = CommonFunction.GetDefaultValue();
                double curValue = CommonFunction.GetDefaultValue();
                List<MESTestInfo> allBpParams = loadDatacontrol.GetAllTestInfo();

                //CommonFunction.WriteLog(allTestParam[i].BPParamSet);
                //CommonFunction.WriteLog(allTestParam[i].BPCurrentSet);

                foreach (MESTestInfo testInfo in allBpParams)
                {
                    if (testInfo.EnvironmentID.ToUpper() == bpSetSplits[2].ToUpper()
                        && testInfo.ObjectID.ToUpper() == bpSetSplits[3].ToUpper()
                        && testInfo.PortID.ToUpper() == bpSetSplits[4].ToUpper()
                        && testInfo.ConditionID.ToUpper() == bpSetSplits[5].ToUpper()
                        && testInfo.TestParam.GetMESTemplateKeywords().ToUpper() == bpSetSplits[6].ToUpper())
                    {
                        bpValue = Convert.ToDouble(testInfo.TestedValue);
                        break;
                    }
                }
                // CommonFunction.WriteLog(bpValue.ToString());
                foreach (MESTestInfo testInfo in allTestParam)
                {
                    if (testInfo.EnvironmentID.ToUpper() == curSetSplits[0].ToUpper()
                        && testInfo.ObjectID.ToUpper() == curSetSplits[1].ToUpper()
                        && testInfo.PortID.ToUpper() == curSetSplits[2].ToUpper()
                        && testInfo.ConditionID.ToUpper() == curSetSplits[3].ToUpper()
                        && testInfo.TestParam.GetMESTemplateKeywords().ToUpper() == curSetSplits[4].ToUpper())
                    {
                        curValue = testInfo.CurValue;
                        break;
                    }
                }
                //CommonFunction.WriteLog(curValue.ToString());
                if (curValue.CompareTo(CommonFunction.GetDefaultValue()) == 0
                    || bpValue.CompareTo(CommonFunction.GetDefaultValue()) == 0)
                {
                    continue;
                }
                double paramResult = curValue - bpValue;
                // CommonFunction.WriteLog(paramResult.ToString());
                if (errMsg.Length == 0 && (!CommonFunction.IsDefault(paramResult)))
                {
                    paramResult = Math.Round(paramResult, 3);
                    bool isPass = false;
                    allProductControl[nProductIdx].UpdateTestData(i, paramResult, ref isPass);
                }
            }

        }

        public void SelctToItemBegin_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(2000);
        }
        public void SelctToItemBegin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IndexMap nextSeleted = new IndexMap();
            nextSeleted.ProductIndex = 0;
            nextSeleted.ParamIndex.Add(0);
            UpdateItem(allProductControl[0].AllTestInfo[allProductControl[0].AllTestInfo.Count - 1], 0, 0, nextSeleted);
        }

        /// <summary>
        /// 更新测试结果ICON
        /// </summary>
        private void UpdateResIcon()
        {
            string errMsg = "";
            passOrFailImg.Source = passBitmapImage;
            for (int i = 0; i < allProductControl.Count; i++)
            {
                if (!allProductControl[i].GetAllTestedPassed(ref errMsg))
                {
                    passOrFailImg.Source = failBitmapImage;
                    break;
                }
            }
        }

        private void OnekeyScan()
        {
            //获取未测试的端口
            isOnekeyScan = true;
            scanDetailInfo.Ports="";
            List<int> scanPorts = new List<int>();
            int i = 0;
            double scanTmpt = -300;
            if (curTestTmpt == -300)
            {
                scanTmpt = portAssistant[0].TestTmpt;
            }
            else
            {
                scanTmpt = curTestTmpt;
            }
            //查找当前一键测试该测试哪项
            RealtimeMsg(string.Format("测试通道总数:{0}", portAssistant.Count));
            for (i = 0; i < portAssistant.Count; i++)
            {
                if ((!portAssistant[i].IsTested) && scanTmpt == portAssistant[i].TestTmpt)
                {
                    break;
                }
            }
            
            //一个温度测试完成，查找下一个测试的温度
            if (i == portAssistant.Count)
            {
                for (i = 0; i < portAssistant.Count; i++)
                {
                    if (!portAssistant[i].IsTested)
                    {
                        scanTmpt = portAssistant[i].TestTmpt;
                        RealtimeMsg(string.Format("下一个测试温度:{0}", scanTmpt));
                        break;
                    }
                }
            }
      
            if (i == portAssistant.Count)
            {
                //测试完成，恢复按钮状态
                //常温测试完后，需要继续，知道最后一个温度测试完成才结束
                //
                //
                MessageBox.Show("一键测试完成！");
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                SetIsScanFinished(true);
                return;
            }
            RealtimeMsg(string.Format("当前测试温度:{0}", scanTmpt));
            RealtimeMsg(string.Format("当前测试通道:{0}", portAssistant[i].Name));
            int productID = portAssistant[i].ProductIndex;
            int scanIndex = portAssistant[i].PortIndex;
            if (scanIndex == 0)
            {
                MessageBox.Show("一键测试扫描出错，扫描序列号不能为0！");
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                return;
            }
            //获取同时扫描的端口号。
            //进光端一样，则可以一起扫描，比如in-to,in-te,in-moni同时扫描
            //string testPortName = "";
            foreach (PortAssist assist in portAssistant)
            {
                //string[] splits = assist.Name.Split('-');
                if (productID == assist.ProductIndex && scanIndex == assist.PortIndex
                    && scanTmpt == assist.TestTmpt)
                {
                    scanDetailInfo.PortFlagName = assist.Port;
                    scanDetailInfo.Ports= assist.Name;
                }
            }
            scanDetailInfo.ProductIndex = productID;
            string errMsg = "";
            
            //切换开关
            SetSwitch(scanDetailInfo.ProductIndex, scanDetailInfo.PortFlagName);
            //CurProductIndex = productID;

            scanDetailInfo.ScanType = SCANTYPE.TestWithPDLOnekey;
            UIControl.IsScanEnable = false;

            //选中当前测试行
            List<MESTestInfo> shows = testShowControl[productID - 1].GetAllTestInfo();
            IndexMap nextSeleted = new IndexMap();
            nextSeleted.ProductIndex = productID - 1;
            for (int k = 0; k < shows.Count; k++)
            {
                if (portAssistant[i].Name == shows[k].PortNameForUser &&
                    portAssistant[i].TestTmpt == shows[k].Temperature)
                {
                    nextSeleted.ParamIndex.Add(k);
                    RealtimeMsg(string.Format("选中行温度:{0},port:{1}", scanTmpt, portAssistant[i].Name));
                    break;
                }
            }
            UpdateItem(testShowControl[productID - 1].GetAllTestInfo()[0], productID - 1, 0, nextSeleted);

            DoTestBySelectItem(scanTmpt);

        }

        private void ParamItemUpdate(int productID, bool isOpenTemplate = false)
        {
            if (!GetOpenTemplateComplete())
            {
                return;
            }
            UpdateResIcon();
            if (isOpenTemplate)
            {
                List<int> deleteItems = new List<int>();
                List<MESTestInfo> testInfos = allProductControl[productID].GetAllTestInfo();
                testItemShow = allProductControl[productID].Clone();
                for (int i = 0; i < testInfos.Count; i++)
                {
                    string param = testInfos[i].ExParamName.ToUpper();
                    //通道名_频率_porti
                    string[] splits = testInfos[i].PortNameForUser.Split('_');

                    //筛选出需要显示的参数项，只显示总通道,子通道都是 通道名_中心频率_PORTi
                    bool isNeedShow = false;
                    if (splits.Length == 1)
                    {
                        if (testInfos[i].PortNameForUser.ToUpper() != "Frequency Range".ToUpper())
                        {
                            isNeedShow = true;
                        }
                    }
                    else
                    {
                        /*if (!testInfos[i].Pass && testInfos[i].Tested)
                        {
                            isNeedShow = true;
                        }*/
                    }
                    if (!isNeedShow)
                    {
                        //需要删除行
                        deleteItems.Add(i);
                    }
                    else
                    {
                        //需要显示的行序号
                        updateParamIndex.Add(i);
                    }

                }
                testItemShow.DeleteParams(deleteItems);
                string sourceStr = "@PB=" + passBand.ToString();
                testItemShow.ColumnReplaceStr(sourceStr, "");
                // 更新测试信息
                if (EventAggregator != null)
                {
                    if (productID == 0)
                        testShowControl.Clear();

                    testShowControl.Add(testItemShow);
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(testShowControl);
                }

                if (EventAggregator != null)
                {
                    List<MESTestInfo> shows = testShowControl[productID].GetAllTestInfo();
                    for (int i = 0; i < shows.Count; i++)
                    {
                        MESTestInfo info = shows[i];
                        //UpdateItem(info, productID, i);
                        for (int j = 0; j < portAssistant.Count; j++)
                        {
                            if (shows[i].PortNameForUser == portAssistant[j].Name &&
                                productID == portAssistant[j].ProductIndex - 1)
                            {
                                testShowControl[productID].UpdateScanRefStatus(i, portAssistant[j].IsRef);
                                UpdateItem(testShowControl[productID].GetAllTestInfo()[i], productID, i);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                List<MESTestInfo> testInfos = allProductControl[productID].GetAllTestInfo();
                List<MESTestInfo> shows = testShowControl[productID].GetAllTestInfo();
                for (int i = 0; i < testInfos.Count; i++)
                {
                    string param = testInfos[i].ExParamName.ToUpper();
                    //通道名_频率_porti
                    string[] splits = testInfos[i].PortNameForUser.Split('_');
                    if (splits.Length == 1)
                    {
                        if (testInfos[i].PortNameForUser.ToUpper() != "Frequency Range".ToUpper())
                        {
                            for (int j = 0; j < shows.Count; j++)
                            {
                                if (testInfos[i].PortNameForUser == shows[j].PortNameForUser
                                    && testInfos[i].Temperature == shows[j].Temperature &&
                                    testInfos[i].ExParamName == shows[j].ExParamName)
                                {
                                    bool isPass = false;
                                    testShowControl[productID].UpdateTestData(j, testInfos[i].CurValue, ref isPass);
                                    UpdateItem(testShowControl[productID].GetAllTestInfo()[j], productID, j);
                                }
                            }
                        }
                    }

                }
                // 更新测试信息
            }
        }

        private void UpdateReferenceStatus(bool isRef)
        {
            for (int i = 0; i < allProductControl.Count; i++)
            {
                List<MESTestInfo> showInfos = testShowControl[i].GetAllTestInfo();
                for (int j = 0; j < showInfos.Count; j++)
                {
                    testShowControl[i].UpdateScanRefStatus(j, isRef);
                    UpdateItem(testShowControl[i].GetAllTestInfo()[j], i, j);

                }
            }
        }

        /// <summary>
        /// 更新测试项List显示
        /// </summary>
        /// <param name="info">需要更新的测试项</param>
        /// <param name="prodoctIndex">第几个产品</param>
        /// <param name="paramIndex">测试项对应index</param>
        /// <param name="nextSelect">自动跳转到下一行信息</param>
        private void UpdateItem(MESTestInfo info, int prodoctIndex, int paramIndex, IndexMap nextSelect = null)
        {
            ItemContent item = new ItemContent();
            item.TestInfo = info;
            item.UpdateItemMap = new IndexMap();
            item.UpdateItemMap.ProductIndex = prodoctIndex;
            item.UpdateItemMap.ParamIndex = new List<int>();
            item.UpdateItemMap.ParamIndex.Add(paramIndex);
            item.NextSelectMap = nextSelect;
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventListItemUpdate>().Publish(item);
            }
        }

        /// <summary>
        /// 读取归零数据
        /// </summary>
        /// <param name="errMsg"></param>
        private void ReadRefData(int productIndex, List<PortAssist> assists, ref string errMsg)
        {

        }

        /// <summary>
        /// 通知主界面显示模板名称
        /// </summary>
        private void ShowTmpltPath()
        {
            Dictionary<string, string> ackNodes = new Dictionary<string, string>();
            ackNodes.Add("Path", templateName);
            XmlStr ackXml = new XmlStr();
            MsgBaseInfo info = new MsgBaseInfo();
            info.MsgTarget = "MainWindow";
            info.MsgType = "Template";
            info.Operate = "ShowTemplatePath";
            info.MsgSource = "OperateInteleaverFinalTest";
            MsgXmlParser.MakeMsg(info, ackNodes, ref ackXml);
            EventAggregator.GetEvent<EventXml>().Publish(ackXml);
        }

        /// <summary>
        /// 根据端口号，判断扫描index
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private int GetScanIndex(int port)
        {
            for (int i = 0; i < _scanList.Count; i++)
            {
                for (int j = 0; j < _scanList[i].Count; j++)
                {
                    if (_scanList[i][j] == port)
                        return i + 1;
                }
            }
            return 0;
        }

        private bool IsContainPortAssist(int productIndex, string keyName, double testTmpt)
        {
            foreach (PortAssist assist in portAssistant)
            {
                if (assist.ProductIndex == productIndex && assist.Name == keyName
                    && Math.Abs(testTmpt - assist.TestTmpt) < 0.001)
                {
                    return true;
                }
            }
            return false;
        }

        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetOpenTemplateComplete(bool isComplete)
        {
            isOpenTemplateComplete = isComplete;
        }

        /// <summary>
        /// 打开模板处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTemplateBK_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (TestProductInfo info in AllProducts)
            {
                if (info.SN == UIControl.SN)
                {
                    e.Result = "该SN号已存在测试列表！";
                    return;
                }
            }
            FusionControl control = new FusionControl();
            string errMsg = "";
            //string templateName = "";
            //allProductControl.Clear();
            List<string> sptProcess = new List<string>();
            string tmpltContent = control.OpenTemplate(UIControl.SN, mainInfo.TestProcess, mainInfo.UserID, "", false, Environment.MachineName, sptProcess, out templateName, out errMsg);
            if (tmpltContent.Length > 0)
            {
                if (allProductControl.Count > 0)
                {
                    if (allProductControl[0].GetProductInfo().Spec == control.GetProductInfo().Spec)
                    {
                        allProductControl.Add(control);
                    }
                    else
                    {
                        e.Result = "该产品Spec与测试列表Spen不一致！";
                        return;
                    }
                }
                else
                    allProductControl.Add(control);
            }
            e.Result = errMsg;
        }

        private void PassOrFail_Load(object sender, RoutedEventArgs e)
        {
            InitPassFailImage();
            //设置图片显示大小，将图片放大1.5倍
            passOrFailImg.Height = passBitmapImage.Width * 1.5;
            passOrFailImg.Width = passBitmapImage.Width * 1.5;

            passOrFailImg.Source = passBitmapImage;
        }

        /// <summary>
        /// 初始化测试结果通过、失败图片
        /// </summary>
        private void InitPassFailImage()
        {
            if (failBitmapImage != null && passBitmapImage != null)
                return;

            passBitmapImage = new BitmapImage();
            BinaryReader binReader = new BinaryReader(File.Open(Environment.CurrentDirectory + passImage, FileMode.Open));
            FileInfo fileInfo = new FileInfo(Environment.CurrentDirectory + passImage);
            byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);

            // Init bitmap
            passBitmapImage.BeginInit();
            passBitmapImage.StreamSource = new MemoryStream(bytes);
            passBitmapImage.EndInit();
            binReader.Close();

            failBitmapImage = new BitmapImage();
            BinaryReader failReader = new BinaryReader(File.Open(Environment.CurrentDirectory + failImage, FileMode.Open));
            FileInfo failfileInfo = new FileInfo(Environment.CurrentDirectory + failImage);
            byte[] failbytes = failReader.ReadBytes((int)failfileInfo.Length);

            // Init bitmap
            failBitmapImage.BeginInit();
            failBitmapImage.StreamSource = new MemoryStream(failbytes);
            failBitmapImage.EndInit();
            failReader.Close();
        }
    }

    public class UIVariable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 与界面产品类型绑定
        /// </summary>
        private string spec;
        public string Spec
        {
            get
            {
                return spec;
            }
            set
            {
                spec = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Spec"));
            }
        }

        private string pn;
        public string PN
        {
            get
            {
                return pn;
            }
            set
            {
                pn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PN"));
            }
        }

        /// <summary>
        /// 与界面SN绑定
        /// </summary>
        private string sn;
        public string SN
        {
            get
            {
                return sn;
            }
            set
            {
                sn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SN"));
            }
        }



        private Visibility isClearSNVisiable;
        public Visibility IsClearSNVisiable
        {
            get
            {
                return isClearSNVisiable;
            }
            set
            {
                isClearSNVisiable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsClearSNVisiable"));
            }
        }




        private bool isReferenceEnable;
        public bool IsReferenceEnable
        {
            get
            {
                return isReferenceEnable;
            }
            set
            {
                isReferenceEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsReferenceEnable"));
            }
        }


        private bool isScanEnable;
        public bool IsScanEnable
        {
            get
            {
                return isScanEnable;
            }
            set
            {
                isScanEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsScanEnable"));
            }
        }


        private bool isSaveEnable;
        public bool IsSaveEnable
        {
            get
            {
                return isSaveEnable;
            }
            set
            {
                isSaveEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSaveEnable"));
            }
        }

    }

    public class TestProductInfo
    {
        public string SN { get; set; }
        public int Index { get; set; }
    }

    public class PortAssist
    {
        public string Name { get; set; }

        public string Port { get; set; }

        public int OperateIndex { get; set; }

        public int ProductIndex { get; set; }

        public int PortIndex { get; set; }

        public int PMIndex { get; set; }

        public bool IsTested { get; set; }

        public double TestTmpt { get; set; }

        public double TmptChangeTimes { get; set; }

        public bool IsRef { get; set; }

        public string Rawdata { get; set; }

        public int ScanIndex { get; set; }

        public string RawdataPath { get; set; }

        public string TmptID { get; set; }
        public PortAssist()
        {
            Name = "";
            Port = "";
            OperateIndex = -1;
            ProductIndex = -1;
            PortIndex = -1;
            PMIndex = -1;
            IsTested = false;
            TestTmpt = -300;
            TmptChangeTimes = 0;
            IsRef = false;
            Rawdata = "";
            ScanIndex = 0;
            RawdataPath = "";
            TmptID = "";
        }
    }
}
