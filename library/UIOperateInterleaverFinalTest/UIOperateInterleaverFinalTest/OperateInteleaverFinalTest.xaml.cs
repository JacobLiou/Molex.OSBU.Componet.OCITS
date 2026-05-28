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

using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;
using MolexUtility.UIList;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Path = System.IO.Path;
//172.16.143.20

namespace UIOperateInterleaverFinalTest
{
    /// <summary>
    /// Interaction logic for OperateInteleaverFinalTest.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateInterleaverFinalTest")]
    public partial class OperateInteleaverFinalTest : UserControl
    {
        private const double lightSpeed = 2.99792458E8;

        private double fstpScanStep = 0.003;
        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsUrl = "http://172.18.1.101/amts/";

        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsSaveUrl = "http://172.18.1.101/amts/Atd_UploadMessage.asmx";

        /// <summary>
        /// 测试通过图片
        /// </summary>
        private const string passImage = "\\image\\Pass.ico";

        /// <summary>
        /// 测试失败图片
        /// </summary>
        private const string failImage = "\\image\\Fail.ico";

        /// <summary>
        /// 测试通过图片加载存储对象
        /// </summary>
        private BitmapImage passBitmapImage = null;

        /// <summary>
        /// 模板里最大的端口号，用来开关切换配置
        /// </summary>
        private int SWMaxPortFlag = 0;

        /// <summary>
        /// 测试失败图片加载存储对象
        /// </summary>
        private BitmapImage failBitmapImage = null;

        /// <summary>
        /// 设备控制 使用UDL
        /// </summary>
        //static UDL2_Engine deviceEngine = null;
        //static UDL2_TCC tccCtrl = null;

        private const int TCC_GUID = 1;

        /// <summary>循环箱实测温度与模板要求温度的允许偏差（°C）</summary>
        private const double TccTempToleranceCelsius = 2.0;

        /// <summary>TAS 打开模板 STA 线程最长等待（毫秒）</summary>
        private const int OpenTemplateStaTimeoutMs = 180000;

        /// <summary>
        /// 是否正在烤温
        /// </summary>
        private BakeStatus curBakeStatus = BakeStatus.UnBake;

        /// <summary>
        /// 当前烤温的温度，0-常温 1-低温 2-高温，或者后续用实际温度来标记？
        /// </summary>
        private int curBakeTempt = -1;

        private const int rerefHours = 6;

        /// <summary>
        /// 最老的归零时间，用于4小时归零倒计时
        /// </summary>
        private DateTime oldestRefTime = new DateTime();

        /// <summary>
        /// 模板是否正确打开，并完成
        /// </summary>
        private bool isOpenTemplateComplete = false;

        /// <summary>
        /// 正在打开模板（防重复点击/批量链式打开重叠）
        /// </summary>
        private bool templateOpenInProgress = false;

        /// <summary>
        /// 归零时间确认后台线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 烤温时间确认后台线程
        /// </summary>
        private BackgroundWorker bakeTimeCheckBK;

        /// <summary>
        /// 记录计算的过程数据，用于port参数计算
        /// </summary>
        private List<SamePortParamData> curPortRecords = new List<SamePortParamData>();

        /// <summary>
        /// 与其他模块通信的事件集 
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// 设备控制
        /// </summary>
        [Import(typeof(IDeviceHandle))]
        public IDeviceHandle DeviceControl { get; set; }


        [Import(typeof(IInterleaverAlgorithm))]
        private IInterleaverAlgorithm algorithm;

        private InterleaverFinalTestCurve curveShow = null;


        public ObservableCollection<TestProductInfo> AllProducts { get; set; }

        public UIVariable UIControl = new UIVariable();


        /// <summary>
        /// 利用四个偏振态下数据计算所得四个端口结果数据double[6][] 0:WL 1:ave 2:PDL 3:MaxIL 4:MinIL 5:Fre
        /// </summary>
        private List<double[][]> portResData = null;

        /// <summary>
        /// 四个偏振态下数据double[3][] 0:WL 1:IL 2:fre
        /// </summary>
        private List<double[][]> pdlRawData = null;

        /// <summary>
        /// 带PDL归零数据数据double[7][]  0:WL 1:ave 2:PDL1 IL 3:PDL2 IL 4:PDL3 IL 5:PDL4 IL 6:fre
        /// </summary>
        private List<double[][]> portPDLRef = null;

        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 扫描、归零文件路径
        /// </summary>
        private string refWithPDLFile = "\\reference\\referenceWithPDLPort";        
        private string scanWithPDLFile = "\\rawdata\\ScanWithPDLPort";

        /// <summary>
        /// 保存数据基础路径，到productfamily
        /// </summary>
        private string savePathBase = "";

        /// <summary>
        /// 由主程序传递的工位信息等
        /// </summary>
        private MainInitInfo mainInfo = null;

        /// <summary>
        /// 模板的测试工序
        /// </summary>
        private MESTestProcess testProcess;


        /// <summary>
        /// 模板类型
        /// </summary>
        private MESTemplateType templateType;

        private List<string> savePathList = new List<string>();

        /// <summary>
        /// 模板获取到的最小扫描频率
        /// </summary>
        private double minScanFre = 2000000.0;

        /// <summary>
        /// 模板获取到的最大扫描频率
        /// </summary>
        private double maxScanFre = -2000000.0;

        /// <summary>
        /// 模板获取到的归零最小扫描波长
        /// </summary>
        private double minRefScanWL = 0;
        private double maxRefScanFre = 0;

        /// <summary>
        /// 模板获取到的归零最大扫描波长
        /// </summary>
        private double maxRefScanWL = 0;
        private double minRefScanFre = 0;

        /// <summary>
        /// 产品有效带宽
        /// </summary>
        private double passBand = 10;

        /// <summary>
        /// 产品两相邻通道频率
        /// </summary>
        private double productFre = 50;

        /// <summary>
        /// 参数计算处理类
        /// </summary>
        private ParamCal paramCal = null;


        /// <summary>
        /// 扫描错误信息
        /// </summary>
        private string scanErrorMsg = "";

        /// <summary>
        /// 用于显示的模板处理类
        /// </summary>
        private FusionControl testItemShow = null;

        /// <summary>
        /// 所有产品测试信息
        /// </summary>
        private List<FusionControl> allProductControl;

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
        /// 功率计和port对应关系
        /// </summary>
        private Dictionary<string, int> portAndPMDic = new Dictionary<string, int>();

        /// <summary>
        /// 1×16 MPLUS 光开关：端口显示名 → 输出通道（与光路图一致）
        /// </summary>
        private static readonly Dictionary<string, int> SwitchPortChannelMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "L3-4", 1 }, { "L3-3", 2 }, { "L3-2", 3 }, { "L3-1", 4 },
            { "L2-4", 5 }, { "L2-3", 6 }, { "L2-2", 7 }, { "L2-1", 8 },
            { "L1-4", 9 }, { "L1-3", 10 }, { "L1-2", 11 }, { "L1-1", 12 },
            { "L4-4", 13 }, { "L4-3", 14 }, { "L4-2", 15 }, { "L4-1", 16 },
        };

        /// <summary>
        /// 扫描功率计个数
        /// </summary>
        private int scanPowermeterCount = 2;

        /// <summary>
        /// 扫描数据记录，归零时port传实际端口，
        /// </summary>
        private ScanDetail scanDetailInfo = new ScanDetail();


        /// <summary>
        /// 扫描是否结束
        /// </summary>
        private bool isScanFinished = true;


        private string convertAlgorithm = ConvertAlgorithm.Mueller.GetAdditional();

        /// <summary>
        /// 端口数量
        /// </summary>
        private const int cstMaxPortCount = 32;

        /// <summary>
        /// PDL数量
        /// </summary>
        private const int cstPDLCount = 4;

        /// <summary>
        /// 同时计算多个端口参数，一个端口开启一个线程，用于判断是否每条线程是否结束
        /// </summary>
        private List<bool> isPortCalFinished = new List<bool>();

        /// <summary>
        /// 保存到无纸化的rawdata数据
        /// </summary>
        private Dictionary<string, string> portRawdatas = new Dictionary<string, string>();

        /// <summary>
        /// 一起扫描的端口号
        /// </summary>
        private List<List<int>> _scanList = new List<List<int>>();

        /// <summary>
        /// 批量打开模板时待处理的 SN 队列
        /// </summary>
        private Queue<string> batchSnQueue;

        /// <summary>
        /// 1×16 批次是否已因常温 MAXIL/MINIL 超限终止
        /// </summary>
        private bool batchTestAborted = false;

        public delegate void GetUDLMessageDelegate(ref string msg, ref bool isSuccess);

        /// <summary>
        /// 算法枚举，平均值法，Muellex矩阵，最大最小值法
        /// </summary>
        public enum ConvertAlgorithm
        {
            [Additional("PZ-Averagevalue")]
            Ave = 0,
            [Additional("PZ-MAX")]
            MaxMin,
            [Additional("Muellermatrix")]
            Mueller
        }

        public enum BakeStatus
        {
            UnBake = 0,
            Baking,
            BakeComplete
        }
        public OperateInteleaverFinalTest()
        {
            InitializeComponent();
            AllProducts = new ObservableCollection<TestProductInfo>();
            allProductControl = new List<FusionControl>();
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
            
            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            amtsSaveUrl = xmlSet.readStringData(CommonFunction.GetSaveWebservicSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/Atd_UploadMessage.asmx");
            savePathBase = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetBasePathKey(), "\\\\zh-mfs-srv2\\Data\\TestData\\Pilot\\Interleaver");
            portAndPMDic.Add("1", 1);
            portAndPMDic.Add("2", 2);
            portAndPMDic.Add("3", 3);
            portAndPMDic.Add("4", 2);
            portAndPMDic.Add("5", 2);
            portAndPMDic.Add("6", 1);
            portAndPMDic.Add("7", 1);
            portAndPMDic.Add("8", 3);
            portAndPMDic.Add("9", 3);

            bakeTimeCheckBK = new BackgroundWorker();
            bakeTimeCheckBK.DoWork += BakeTimeCheck_DoWork;
            bakeTimeCheckBK.ProgressChanged += BakeTimeCheck_Progress;
            bakeTimeCheckBK.WorkerSupportsCancellation = true;
            bakeTimeCheckBK.WorkerReportsProgress = true;

            portPDLRef = new List<double[][]>(cstMaxPortCount);
            portResData = new List<double[][]>(cstMaxPortCount);
            pdlRawData = new List<double[][]>(cstPDLCount);
            for (int i = 0; i < cstMaxPortCount; i++)
            {          
                portPDLRef.Add(new double[7][]);
                portResData.Add(new double[6][]);               
            }

            for (int i = 0; i < cstPDLCount; i++)
            {
                pdlRawData.Add(new double[3][]);
            }

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;
        }

        public static bool GetMessage(ref string msg)
        {
            try
            {
                
                return true;
            }
            catch (Exception e)
            {
                msg = e.Message;
                return false;
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
                for(int i=0;i<portAssistant.Count;i++)
                {                   
                    string pdlRefPath = string.Format("{0}-product{1}-port{2}.csv", refWithPDLFile, portAssistant[i].ProductIndex, portAssistant[i].PortIndex);
                    if (File.Exists(pdlRefPath))
                        File.Delete(pdlRefPath);
                    //清除内存归零数据 
                    InterleaverScanResult.InitRawdataBuffer(portPDLRef[portAssistant[i].OperateIndex]);

                    portAssistant[i].IsRef = false;
                    UpdateReferenceStatus(portAssistant[i].ProductIndex-1,portAssistant[i]);                                       
                }
                oldestRefTime = new DateTime();
            }
        }

        private void BakeTimeCheck_DoWork(object sender, DoWorkEventArgs e)
        {
            double totalBakeTime = (double)e.Argument;
            totalBakeTime = totalBakeTime * 1000;
            int beginTick= System.Environment.TickCount;
            while (!bakeTimeCheckBK.CancellationPending)
            {
                int preTickCount = System.Environment.TickCount;
                int EndTickCount = System.Environment.TickCount;
                if((EndTickCount-beginTick)> totalBakeTime)
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

        private void BakeTimeCheck_Progress(object sender, ProgressChangedEventArgs e)
        {
            int time = e.ProgressPercentage;
            time = time / 1000;
            if(time==0)
            {
                TemptRemainTime.Text = "烤温完成";
                UIControl.IsClearSNVisiable = Visibility.Visible;             
                DoScanOnBK();
            }
            else
            {               
                string timeShow = string.Format("{0}:{1:D2}:{2:D2}", "00",Convert.ToInt32(time/60),time%60);
                TemptRemainTime.Text = timeShow;
            }
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

        private string GetExeDir()
        {
            string curPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string[] dirs = curPath.Split('\\');
            if (dirs.Length == 0)
                return "";
            //去除应用程序名，得到路径
            int count = dirs[dirs.Length - 1].Length + 1;
            string path = curPath.Remove(curPath.Length - count, count);
            return path;
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

            curveShow = new InterleaverFinalTestCurve(EventAggregator);
            paramCal = new ParamCal(algorithm);

            string curDir = System.Environment.CurrentDirectory;
            refWithPDLFile = curDir + refWithPDLFile;            
            scanWithPDLFile = curDir + scanWithPDLFile;
            if (mainInfo.DeviceInitRes != true)
            {
                errMsg = "设备初始化失败。";
                ErrorBox(errMsg);
                RealtimeMsg(errMsg);
            }
            else
            {

                IUDLFSTP scan = null;
                DeviceControl.GetUDLFstpByGUID(2, ref scan, ref errMsg);
                errMsg = "";
                /*IInterleaverScan scan = null;               
                DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);*/
                if (scan != null)
                {
                    scanPowermeterCount = scan.PowermeterCount() ;
                }
                refTimeCheckBK.RunWorkerAsync();
            }
            //曲线显示初始化
            //curveShow.InitAllCurve();
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


        private string bakeSpecRec = "";
        private MESGlobalSetting bakeGlobalSet = null;
        

        

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
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

        private void btnBatchOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "批量打开 SN（每行一个，最多16个）",
                Width = 380,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock
            {
                Text = "请输入 SN，每行一个。须与已加载产品同 Spec。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
            var input = new TextBox
            {
                AcceptsReturn = true,
                Height = 180,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            panel.Children.Add(input);
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            bool confirmed = false;
            var okBtn = new Button { Content = "确定", Width = 72, Margin = new Thickness(4, 0, 0, 0) };
            var cancelBtn = new Button { Content = "取消", Width = 72, Margin = new Thickness(4, 0, 0, 0) };
            okBtn.Click += (s, args) => { confirmed = true; dialog.Close(); };
            cancelBtn.Click += (s, args) => dialog.Close();
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            panel.Children.Add(buttons);
            dialog.Content = panel;
            dialog.ShowDialog();
            if (!confirmed)
                return;

            string[] lines = input.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            batchSnQueue = new Queue<string>();
            foreach (string line in lines)
            {
                string sn = (line ?? "").Trim().ToUpperInvariant();
                if (sn.Length == 0)
                    continue;
                if (batchSnQueue.Count >= OpticalSwitchConfigNames.MaxProductsSinglePort)
                {
                    WarningBox(string.Format("最多批量打开 {0} 个 SN，超出部分已忽略。",
                        OpticalSwitchConfigNames.MaxProductsSinglePort));
                    break;
                }
                batchSnQueue.Enqueue(sn);
            }
            if (batchSnQueue.Count == 0)
            {
                WarningBox("未输入有效 SN。");
                batchSnQueue = null;
                return;
            }
            TryOpenNextBatchSn();
        }

        private void TryOpenNextBatchSn()
        {
            if (batchSnQueue == null || batchSnQueue.Count == 0)
            {
                batchSnQueue = null;
                RealtimeMsg("批量打开 SN 完成。");
                return;
            }
            UIControl.SN = batchSnQueue.Dequeue();
            Dispatcher.BeginInvoke(new Action(() => btnOpenTemplate_Click(this, new RoutedEventArgs())),
                DispatcherPriority.ApplicationIdle);
        }

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (templateOpenInProgress)
            {
                RealtimeMsg("正在打开模板，请稍候...");
                return;
            }

            if (allProductControl.Count == 0)
            {
                batchTestAborted = false;
            }

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
            if (portAndNameDic.Count > 0)
            {
                int maxProducts = OpticalSwitchConfigNames.GetMaxProductsForPortCount(portAndNameDic.Count);
                if (maxProducts > 0 && allProductControl.Count >= maxProducts)
                {
                    ErrorBox(string.Format("该工位最多支持测试{0}个{1}端口产品！",
                        maxProducts, portAndNameDic.Count));
                    return;
                }
            }

            if (portAndNameDic.Count > 0 &&
                (allProductControl.Count + 1) * portAndNameDic.Count > OpticalSwitchConfigNames.MaxOutputSwitchChannels)
            {
                ErrorBox(string.Format("产品数×端口数不能超过{0}（当前将超出出光开关通道容量）！",
                    OpticalSwitchConfigNames.MaxOutputSwitchChannels));
                return;
            }
            portRawdatas.Clear();
            SetOpenTemplateComplete(false);
            templateOpenInProgress = true;
            RealtimeMsg("正在打开模板...");
            curTestTmpt = -300;
            UIControl.IsSaveEnable = false;
            UIControl.IsScanEnable = false;
            var workArgs = new OpenTemplateWorkArgs
            {
                Sn = UIControl.SN,
                TestProcess = mainInfo.TestProcess,
                UserId = mainInfo.UserID,
                MachineName = Environment.MachineName,
                ExistingSns = AllProducts.Select(p => p.SN).ToList(),
                ExistingProductCount = allProductControl.Count
            };
            BackgroundWorker templateBK = new BackgroundWorker();
            templateBK.DoWork += OpenTemplateBK_DoWork;
            templateBK.RunWorkerCompleted += OpenTemplateBK_RunWorkerCompleted;
            templateBK.RunWorkerAsync(workArgs);
        }

        private sealed class OpenTemplateWorkArgs
        {
            public string Sn;
            public string TestProcess;
            public string UserId;
            public string MachineName;
            public List<string> ExistingSns;
            public int ExistingProductCount;
        }

        private sealed class OpenTemplateWorkResult
        {
            public string ErrorMessage = "";
            public string TemplateName = "";
            public FusionControl FusionControl;
        }

        private string templateName = "";
        /// <summary>
        /// 打开模板处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        /// 在 STA 线程调用 TAS（USL.TAS.dll），避免 BackgroundWorker 默认 MTA 导致本机崩溃。
        /// </summary>
        private OpenTemplateWorkResult DoOpenTemplateOnStaThread(OpenTemplateWorkArgs args)
        {
            var result = new OpenTemplateWorkResult();
            if (args == null || string.IsNullOrEmpty(args.Sn))
            {
                result.ErrorMessage = "打开模板参数无效。";
                return result;
            }
            if (args.ExistingSns != null)
            {
                foreach (string existingSn in args.ExistingSns)
                {
                    if (string.Equals(existingSn, args.Sn, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ErrorMessage = "该SN号已存在测试列表！";
                        return result;
                    }
                }
            }
            FusionControl control = new FusionControl();
            string errMsg = "";
            string tmplName = "";
            List<string> sptProcess = new List<string>();
            string tmpltContent = control.OpenTemplate(args.Sn, args.TestProcess, args.UserId, "", false,
                args.MachineName, sptProcess, out tmplName, out errMsg);
            result.TemplateName = tmplName ?? "";
            if (tmpltContent.Length > 0)
            {
                result.FusionControl = control;
                result.ErrorMessage = "";
            }
            else
            {
                result.ErrorMessage = errMsg ?? "";
            }
            return result;
        }

        private void OpenTemplateBK_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as OpenTemplateWorkArgs;
            OpenTemplateWorkResult result = null;
            Exception threadEx = null;
            bool timedOut = false;
            var staThread = new Thread(() =>
            {
                try
                {
                    result = DoOpenTemplateOnStaThread(args);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();
            if (!staThread.Join(OpenTemplateStaTimeoutMs))
                timedOut = true;

            if (result == null)
                result = new OpenTemplateWorkResult();
            if (timedOut)
                result.ErrorMessage = "打开模板超时（TAS/MES 无响应），请检查网络、工序与 data\\open_template.log。";
            else if (threadEx != null)
            {
                result.ErrorMessage = "打开模板异常：" + threadEx.Message;
                CommonFunction.WriteLog("OpenTemplateBK_DoWork STA: " + threadEx);
            }
            e.Result = result;
        }

        private void FinishOpenTemplateFailed(string errMsg)
        {
            templateOpenInProgress = false;
            if (allProductControl.Count > 0)
            {
                UIControl.IsScanEnable = true;
            }
            batchSnQueue = null;
            RealtimeMsg(errMsg, StatusType.Error);
            ErrorBox(errMsg);
        }

        private static void TryOpenTemplateNoticeHtmlAsync(FusionControl fusionControl)
        {
            if (fusionControl == null)
                return;
            Task.Run(() =>
            {
                try
                {
                    var productInfo = fusionControl.GetProductInfo();
                    string strUrl = string.Format("\\\\zh-mfs-srv\\Public\\TestTemplate\\{0}\\{1}@{2}.HTML",
                        productInfo.ProductPN, productInfo.ProductPN, productInfo.SpecNum);
                    if (File.Exists(strUrl))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = strUrl,
                            WindowStyle = ProcessWindowStyle.Normal
                        });
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.WriteLog("TryOpenTemplateNoticeHtmlAsync: " + ex.Message);
                }
            });
        }

        private void ClearListData()
        {
            batchTestAborted = false;
            testItemShow = new FusionControl();
            // 更新测试信息
            if (EventAggregator != null)
            {
                List<FusionControl> shows = new List<FusionControl>();
                shows.Add(testItemShow);
                EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
            }
        }

        private bool IsAllPass()
        {
            bool isPass = true;
            string errMsg = "";
            for (int i = 0; i < allProductControl.Count; i++)
            {
                if (!allProductControl[i].GetAllTestedPassed(ref errMsg))
                {
                    isPass = false;
                    break;
                }
            }
            return isPass;
        }

        /// <summary>
        /// 更新测试结果ICON
        /// </summary>
        private void UpdateResIcon()
        {
            passOrFailImg.Source = passBitmapImage;
            if (!GetOpenTemplateComplete())
                return;

            string errMsg = "";
            for (int i = 0; i < allProductControl.Count; i++)
            {
                if (!allProductControl[i].GetHasTested())
                    continue;
                if (!allProductControl[i].GetAllTestedPassed(ref errMsg))
                {
                    passOrFailImg.Source = failBitmapImage;
                    return;
                }
            }
        }

        /// <summary>
        /// 根据 FusionControl 与扫描状态解析单个产品的列表状态。
        /// </summary>
        private ProductTestStatus ResolveProductStatus(int index)
        {
            if (index < 0 || index >= AllProducts.Count || index >= allProductControl.Count)
            {
                return ProductTestStatus.NotStarted;
            }
            if (AllProducts[index].HasScanError)
            {
                return ProductTestStatus.Error;
            }

            bool hasData = allProductControl[index].GetHasTested();
            bool scanningThis = !GetIsScanFinished() && CurProductIndex == index;
            // 仅打开模板、尚未扫描写入数据：列表状态置灰，不按 MES 历史不合格标红
            if (!hasData && !scanningThis)
            {
                return ProductTestStatus.NotStarted;
            }

            string errMsg = "";
            if (!allProductControl[index].GetAllTestedPassed(ref errMsg))
            {
                return ProductTestStatus.Error;
            }
            return ProductTestStatus.Ok;
        }

        private void UpdateProductStatuses()
        {
            for (int i = 0; i < AllProducts.Count; i++)
            {
                if (i >= allProductControl.Count)
                {
                    break;
                }
                ProductTestStatus status = ResolveProductStatus(i);
                Brush newBrush = TestProductInfo.BrushFor(status);
                TestProductInfo p = AllProducts[i];
                if (p.Status != status || !ReferenceEquals(p.StatusBrush, newBrush))
                {
                    p.Status = status;
                    p.StatusBrush = newBrush;
                }
            }
        }

        private List<FusionControl> testShowControl = new List<FusionControl>();
        private void ParamItemUpdate(int productID, bool isOpenTemplate = false)
        {
            if (!isOpenTemplate && !GetOpenTemplateComplete())
            {
                return;
            }
            UpdateResIcon();
            UpdateProductStatuses();
            if (isOpenTemplate)
            {
                updateParamIndex.Clear();
                List<int> deleteItems = new List<int>();
                List<MESTestInfo> testInfos = allProductControl[productID].GetAllTestInfo();
                try
                {
                    testItemShow = allProductControl[productID].Clone();
                }
                catch (Exception ex)
                {
                    CommonFunction.WriteLog("ParamItemUpdate Clone failed: " + ex.Message);
                    testItemShow = allProductControl[productID];
                }
                for (int i = 0; i < testInfos.Count; i++)
                {
                    // 列表只显示总通道行（如 Demux-Even / Demux-Odd）；通道_频率_PORTn 子项仅用于扫描计算
                    if (!ShouldShowTestItemInList(testInfos[i]))
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

                    // 打开模板时只发布一次列表；归零状态写入 testItemShow，勿对每行 UpdateItem（ITL 模板行数多会卡死 UI）
                    SyncScanRefStatusToShowControl(testItemShow, productID);
                    testShowControl.Add(testItemShow);
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(testShowControl);
                }
            }
            else
            {
                List<MESTestInfo> testInfos = allProductControl[productID].GetAllTestInfo();
                List<MESTestInfo> shows = testShowControl[productID].GetAllTestInfo();
                for (int i = 0; i < testInfos.Count; i++)
                {
                    if (!ShouldShowTestItemInList(testInfos[i]))
                        continue;
                    for (int j = 0; j < shows.Count; j++)
                    {
                        if (testInfos[i].PortNameForUser == shows[j].PortNameForUser
                            && testInfos[i].Temperature == shows[j].Temperature
                            && testInfos[i].ExParamName == shows[j].ExParamName)
                        {
                            bool isPass = false;
                            testShowControl[productID].UpdateTestData(j, testInfos[i].CurValue, ref isPass);
                            UpdateItem(testShowControl[productID].GetAllTestInfo()[j], productID, j);
                        }
                    }
                }
                // 更新测试信息
            }
        }

        

        private void OpenTemplateBK_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    FinishOpenTemplateFailed("打开模板异常：" + e.Error.Message);
                    return;
                }
                var workResult = e.Result as OpenTemplateWorkResult;
                if (workResult == null)
                {
                    FinishOpenTemplateFailed("打开模板返回无效。");
                    return;
                }
                if (!string.IsNullOrEmpty(workResult.ErrorMessage))
                {
                    FinishOpenTemplateFailed(workResult.ErrorMessage);
                    return;
                }
                if (workResult.FusionControl == null)
                {
                    FinishOpenTemplateFailed("未获取到模板内容。");
                    return;
                }
                if (allProductControl.Count > 0 &&
                    allProductControl[0].GetProductInfo().Spec != workResult.FusionControl.GetProductInfo().Spec)
                {
                    FinishOpenTemplateFailed("该产品Spec与测试列表Spen不一致！");
                    return;
                }
                allProductControl.Add(workResult.FusionControl);
                templateName = workResult.TemplateName ?? "";

                TestProductInfo curInfo = new TestProductInfo();
                curInfo.Index = AllProducts.Count + 1;
                curInfo.SN = UIControl.SN;
                AllProducts.Add(curInfo);
                UpdateProductStatuses();
                int productIdx = allProductControl.Count - 1;
                RealtimeMsg("正在处理模板数据...");
                Dispatcher.BeginInvoke(new Action(() => CompleteOpenTemplateUi(productIdx)), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                FinishOpenTemplateFailed("处理模板结果异常：" + ex.Message);
                CommonFunction.WriteLog("OpenTemplateBK_RunWorkerCompleted: " + ex);
            }
        }

        private void CompleteOpenTemplateUi(int productIdx)
        {
            try
            {
                if (productIdx < 0 || productIdx >= allProductControl.Count)
                {
                    FinishOpenTemplateFailed("打开模板后产品索引无效。");
                    return;
                }
                string errMsg = "";
                List<MESTestInfo> testInfos = allProductControl[productIdx].AllTestInfo;
                if (allProductControl.Count == 1)
                {
                    _scanList.Clear();
                    TryOpenTemplateNoticeHtmlAsync(allProductControl[0]);
                    updateParamIndex.Clear();
                    portAndNameDic.Clear();
                    portAssistant.Clear();

                    double lowFreLeft = 193500.0;
                    double lowFreRight = 194000.0;
                    double midFreLeft = 194500.0;
                    double midFreRight = 195000.0;
                    double highFreLeft = 196000.0;
                    double highFreRight = 196500.0;
                    double entireFreLeft = 191000.0;
                    double entireFreRight = 196500.0;

                    int rangCount = 0;
                    CFGRecordInfo[] cfgInfo = allProductControl[allProductControl.Count - 1].CFGInfo.ToArray();
                    for (int i = 0; i < cfgInfo.Length; i++)
                    {

                        string param = cfgInfo[i].Name.ToUpper();
                        if (param == "LFRANGE")
                        {
                            ParserRange(cfgInfo[i].Value, ref lowFreLeft, ref lowFreRight);
                            rangCount++;
                        }
                        else if (param == "MFRANGE")
                        {
                            ParserRange(cfgInfo[i].Value, ref midFreLeft, ref midFreRight);
                            rangCount++;
                        }
                        else if (param == "HFRANGE")
                        {
                            ParserRange(cfgInfo[i].Value, ref highFreLeft, ref highFreRight);
                            rangCount++;
                        }
                        else if (param.Contains("ENTIREFRANGE"))
                        {
                            ParserRange(cfgInfo[i].Value, ref entireFreLeft, ref entireFreRight);
                            rangCount++;
                        }
                        else if (param == "PASSBAND")
                        {
                            passBand = Convert.ToDouble(cfgInfo[i].Value);
                        }
                        else if (param == ("ProductFrequency").ToUpper())
                        {
                            productFre = Convert.ToDouble(cfgInfo[i].Value);
                        }
                        else if (param == ("Algorithm").ToUpper())
                        {
                            convertAlgorithm = cfgInfo[i].Value;
                        }
                        else if (param == ("REFStartWL").ToUpper())
                        {
                            minRefScanWL = Convert.ToDouble(cfgInfo[i].Value);
                            if (minRefScanWL > 0)
                                maxRefScanFre = lightSpeed / minRefScanWL;
                        }
                        else if (param == ("REFStopWL").ToUpper())
                        {
                            maxRefScanWL = Convert.ToDouble(cfgInfo[i].Value);
                            if (maxRefScanWL > 0)
                                minRefScanFre = lightSpeed / maxRefScanWL;
                        }
                        else if (param == ("PDLScanStep").ToUpper())
                        {
                            fstpScanStep = Convert.ToDouble(cfgInfo[i].Value);

                        }
                        else if (param.Contains("GROUP"))
                        {
                            //从无纸化上获取端口和功率计对应关系
                            string[] confSplit1 = cfgInfo[i].Value.Split(';');
                            List<int> scanPorts = new List<int>();
                            foreach (string conf in confSplit1)
                            {
                                string[] confSplit2 = conf.Split(':');
                                if (confSplit2.Length == 2)
                                {
                                    string splitPort = confSplit2[0].Substring(4);
                                    int splitPM = Convert.ToInt32(confSplit2[1].Substring(2));
                                    if (portAndPMDic.ContainsKey(splitPort))
                                    {
                                        portAndPMDic[splitPort] = splitPM;
                                    }
                                    else
                                    {
                                        portAndPMDic.Add(splitPort, splitPM);
                                    }
                                    scanPorts.Add(Convert.ToInt32(splitPort));
                                }
                            }
                            _scanList.Add(scanPorts);
                        }
                    }

                    maxScanFre = -2000000.0;
                    minScanFre = 2000000.0;
                    
                }


                Dictionary<string, int> inportDic = new Dictionary<string, int>();
                for (int i = 0; i < testInfos.Count; i++)
                {
                    //转Fusion之后按照之前规则重组EX规则的参数名称
                    if (testInfos[i].TestParam != null &&
                        testInfos[i].TestParam.GetMESTemplateKeywords().Contains("_BP"))
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
                            //通道名称对应关系
                            if (!portAndNameDic.ContainsKey(splits[splits.Length - 3]))
                            {
                                portAndNameDic.Add(splits[splits.Length - 3], splits[splits.Length - 1]);
                            }
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

                            int pmIndex;
                            string pmMapErr = "";
                            if (!TryGetPmIndexForPort(assist.PortIndex, out pmIndex, ref pmMapErr))
                            {
                                errMsg += pmMapErr + "\r";
                                continue;
                            }
                            assist.PMIndex = pmIndex;

                            //决定了一起扫描的端口
                            if (_scanList.Count>0)
                            {
                                assist.ScanIndex = GetScanIndex(assist.PortIndex);
                            }
                            else
                            {
                                string[] insplits = assist.Name.Split('-');
                                if(insplits.Length>0)
                                {
                                    if(!inportDic.ContainsKey(insplits[0]))
                                    {
                                        inportDic.Add(insplits[0], inportDic.Count + 1);
                                    }
                                    assist.ScanIndex = inportDic[insplits[0]];
                                }
                            }
                            //else
                            //assist.PMIndex = assist.PortIndex;
                            assist.TmptID = testInfos[i].EnvironmentID;
                            assist.SwitchChannel = ResolveOutputSwitchChannelAtTemplateLoad(assist);
                            portAssistant.Add(assist);

                            if (SWMaxPortFlag < Convert.ToInt32(assist.Port.Remove(0, 4)))
                                SWMaxPortFlag = Convert.ToInt32(assist.Port.Remove(0, 4));
                        }
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
                    curveShow.InitAllCurve(portNames);
                    curveShow.UpdateFre(minScanFre, maxScanFre);
                }

                SWMaxPortFlag = Math.Max(SWMaxPortFlag, OpticalSwitchConfigNames.MaxOutputSwitchChannels);

                if (IsDemuxDualPortTemplate())
                {
                    int openedProductIndex = productIdx + 1;
                    foreach (PortAssist assist in portAssistant)
                    {
                        if (assist.ProductIndex != openedProductIndex)
                            continue;
                        int demuxCh = GetDemuxOutputChannelForPortIndex(assist.PortIndex);
                        if (demuxCh > 0)
                            assist.SwitchChannel = demuxCh;
                    }
                }

                ReadRefData(productIdx, portAssistant, ref errMsg);
                SetOpenTemplateComplete(true);
                ParamItemUpdate(productIdx, true);
                UIControl.IsScanEnable = true;
                RealtimeMsg(UIControl.SN + "：打开模板成功！");
                if (errMsg.Length > 0)
                    WarningBox(errMsg);

                ShowTmpltPath();

                templateOpenInProgress = false;

                if (batchSnQueue != null && batchSnQueue.Count > 0)
                    TryOpenNextBatchSn();
            }
            catch (Exception ex)
            {
                FinishOpenTemplateFailed("处理模板数据异常：" + ex.Message);
                CommonFunction.WriteLog("CompleteOpenTemplateUi: " + ex);
            }
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
            for(int i=0;i<_scanList.Count;i++)
            {
                for(int j=0;j<_scanList[i].Count;j++)
                {
                    if (_scanList[i][j] == port)
                        return i+1;
                }
            }
            return 0;
        }
        private bool IsContainPortAssist(int productIndex,string keyName,double testTmpt)
        {
            foreach(PortAssist assist in portAssistant)
            {
                if(assist.ProductIndex==productIndex&&assist.Name==keyName
                    &&Math.Abs(testTmpt-assist.TestTmpt)<0.001)
                {
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// 实时状态列表信息显示
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private void RealtimeMsg(string message, StatusType type = StatusType.Normal)
        {
            //CommonFunction.WriteLog(message);
            RealtimeStatusInfo status = new RealtimeStatusInfo();
            status.Status = message;
            status.StatusTime = DateTime.Now.ToString();
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventRealTimeStatus>().Publish(status);
            }
            
        }


        private int referenceIndex = 0;
        private void btnScanRef_Click(object sender, RoutedEventArgs e)
        {
            //判断为7端口产品，则按七端口产品光路进行归零。
            referenceIndex = 0;
            ScanRef();
        }


        private void ScanRef()
        {
            
            string[] keys = portAndNameDic.Keys.ToArray();
            while (true)
            {
                if (referenceIndex >= portAssistant.Count)
                { 
                    UIControl.IsScanEnable = true;
                    UIControl.IsReferenceEnable = true;
                    string errMsg = "";
                    MessageBox.Show("归零完成！");
                    ReadRefData(allProductControl.Count - 1, portAssistant, ref errMsg);
                    if (errMsg.Length > 0)
                    {
                        WarningBox(errMsg);
                        break;
                    }
                    string uploadRefErr = "";
                    if (!FusionControl.UploadRefCalibrationTime(mainInfo.UserID, ref uploadRefErr))
                    {
                        string uploadFailMsg = "上传归零时间到 TMS 失败：" + uploadRefErr;
                        CommonFunction.WriteLog(uploadFailMsg);
                        WarningBox(uploadFailMsg + "\r\n本地归零文件已保存，可继续测试。请检查 GDS/TMS 服务网络，或确认 USL.TAS.dll 与 USL.SYS 配置与产线一致。");
                        RealtimeMsg("归零完成（TMS 上传失败，见日志）。");
                    }
                    else
                    {
                        RealtimeMsg("归零时间已上传 TMS。");
                    }
                    UIControl.IsScanEnable = true;
                    UIControl.IsReferenceEnable = true;
                    break;
                }
                //只需要对一个温度进行归零即可
                if (Math.Abs(portAssistant[0].TestTmpt - portAssistant[referenceIndex].TestTmpt) > 0.001)
                {
                    referenceIndex++;
                    continue;
                }
                string prompt = string.Format("进行系统归零，请确认产品{0}的{1}对接!", portAssistant[referenceIndex].ProductIndex, portAssistant[referenceIndex].Name);
                if (MessageBox.Show(prompt, "系统归零", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    //删除已有的归零文件
                    //切换光源盒
                    if (GetIsScanFinished())
                    {
                        PortAssist refAssist = portAssistant[referenceIndex];
                        if (TasRuntimeConfig.IsRefAutoSwitchDisabled())
                        {
                            if (!ShowManualRefSwitchPrompt(refAssist))
                            {
                                UIControl.IsScanEnable = true;
                                UIControl.IsReferenceEnable = true;
                                break;
                            }
                            RealtimeMsg("手动光路模式：已跳过自动光开关，请确认入光/出光盒 MSW 与弹窗 flag 一致。");
                        }
                        else
                        {
                            string switchErr = "";
                            if (!SetSwitch(refAssist.ProductIndex, refAssist.Port, ref switchErr))
                            {
                                ErrorBox(string.IsNullOrEmpty(switchErr) ? "切换光开关失败。" : switchErr);
                                UIControl.IsScanEnable = true;
                                UIControl.IsReferenceEnable = true;
                                break;
                            }
                        }

                        ReadRefPowerSnapshot(refAssist);
                        int portIndex = refAssist.PortIndex;
                        SetIsScanFinished(false);
                        RealtimeMsg(prompt);
                        BackgroundWorker bkPM = new BackgroundWorker();
                        bkPM.DoWork += Scan_DoWork;
                        bkPM.RunWorkerCompleted += Scan_RunWorkerCompleted;
                        scanDetailInfo.ScanType = SCANTYPE.RefWithPDL;
                        scanDetailInfo.Ports.Clear();
                        scanDetailInfo.Ports.Add(portIndex);
                        scanDetailInfo.ProductIndex = refAssist.ProductIndex;
                        bkPM.RunWorkerAsync(scanDetailInfo);
                        UIControl.IsScanEnable = false;
                        UIControl.IsReferenceEnable = false;
                    }
                    break;
                }
                else
                {
                    referenceIndex++;
                }
            }
        }

        /// <summary>
        /// 测试项是否应在下方列表显示（仅总通道行 Demux-Even/Odd，不含 通道_频率_PORTn 子项）。
        /// </summary>
        private static bool ShouldShowTestItemInList(MESTestInfo testInfo)
        {
            if (testInfo == null)
                return false;
            if (string.IsNullOrWhiteSpace(testInfo.PortNameForUser))
                return false;
            if (string.Equals(testInfo.PortNameForUser, "Frequency Range", StringComparison.OrdinalIgnoreCase))
                return false;
            if (testInfo.PortNameForUser.IndexOf('_') >= 0)
                return false;
            if (testInfo.TestParam == MESParam.Default)
                return false;
            return true;
        }

        /// <summary>
        /// 将 portAssistant 的归零状态写入待显示的 FusionControl（按通道名匹配，含 L3-4_频率_PORTn）。
        /// </summary>
        private void SyncScanRefStatusToShowControl(FusionControl showControl, int productIndex)
        {
            if (showControl == null || portAssistant == null || portAssistant.Count == 0)
                return;
            int rowCount = showControl.AllTestInfo != null ? showControl.AllTestInfo.Count : 0;
            for (int i = 0; i < rowCount; i++)
            {
                MESTestInfo row = showControl.AllTestInfo[i];
                if (row == null)
                    continue;
                for (int j = 0; j < portAssistant.Count; j++)
                {
                    PortAssist assist = portAssistant[j];
                    if (productIndex == assist.ProductIndex - 1 &&
                        PortNameMatchesChannel(row.PortNameForUser, assist.Name))
                    {
                        showControl.UpdateScanRefStatus(i, assist.IsRef);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 列表行 PortNameForUser 与 portAssistant 通道名匹配（支持 L3-4 或 L3-4_频率_PORT1）。
        /// </summary>
        private static bool PortNameMatchesChannel(string portNameForUser, string channelName)
        {
            if (string.IsNullOrWhiteSpace(portNameForUser) || string.IsNullOrWhiteSpace(channelName))
                return false;
            if (string.Equals(portNameForUser, channelName, StringComparison.OrdinalIgnoreCase))
                return true;
            return portNameForUser.StartsWith(channelName + "_", StringComparison.OrdinalIgnoreCase);
        }

        private int ResolveSwitchChannel(string portDisplayName, string portKey)
        {
            string nameKey = (portDisplayName ?? "").Replace(" ", "");
            if (nameKey.Length > 0 && SwitchPortChannelMap.TryGetValue(nameKey, out int mapped))
                return mapped;

            string port = (portKey ?? "").Replace(" ", "").ToUpperInvariant();
            if (port.StartsWith("PORT") && port.Length > 4 &&
                int.TryParse(port.Substring(4), out int portNum) &&
                portNum >= 1 && portNum <= OpticalSwitchConfigNames.MaxOutputSwitchChannels)
                return portNum;

            return -1;
        }

        /// <summary>
        /// 16 SN×1 路：多产品且每 SN 仅 1 逻辑端口时，PORT1 映射到工位槽位（ProductIndex）。
        /// </summary>
        private int ApplySinglePortSlotChannelMapping(int productIndex, int channelFromPort)
        {
            if (productIndex < 1 || allProductControl.Count <= 1 || portAndNameDic.Count != 1)
                return channelFromPort;

            if (channelFromPort == 1)
                return productIndex;
            if (channelFromPort >= 1 && channelFromPort <= OpticalSwitchConfigNames.MaxInputSwitchChannels)
                return channelFromPort;

            return productIndex;
        }

        private int GetInputChannelForProduct(int productIndex)
        {
            return ApplySinglePortSlotChannelMapping(productIndex, 1);
        }

        /// <summary>
        /// 单 SN + 多 PORT（如 Demux Even/Odd）：一口入、两口出，入光固定 SN 槽位。
        /// </summary>
        private bool IsSingleProductMultiPortMode()
        {
            return allProductControl.Count == 1 && portAndNameDic.Count >= 2;
        }

        /// <summary>
        /// 模板含 Demux-Even/Odd 等双口（单 SN 或逐个打开模板累加多 SN 均适用）。
        /// </summary>
        private bool IsDemuxDualPortTemplate()
        {
            if (portAndNameDic.Count < 2)
                return false;
            foreach (string key in portAndNameDic.Keys)
            {
                if (key.IndexOf("Demux", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static int TryParsePortIndex(string portKey)
        {
            string port = (portKey ?? "").Replace(" ", "").ToUpperInvariant();
            if (port.StartsWith("PORT") && port.Length > 4 &&
                int.TryParse(port.Substring(4), out int portIndex))
                return portIndex;
            return 0;
        }

        /// <summary>
        /// Demux 双口：PORT1 Even→SW2 模块11(ch17)，PORT2 Odd→SW1 模块9(ch1)（见 doc/工位接线图.png）。
        /// </summary>
        private static int GetDemuxOutputChannelForPortIndex(int portIndex)
        {
            if (portIndex == 1)
                return OpticalSwitchConfigNames.DemuxEvenOutputChannel;
            if (portIndex == 2)
                return OpticalSwitchConfigNames.DemuxOddOutputChannel;
            return -1;
        }

        /// <summary>
        /// Demux 最简规则：入光 flag 产品序号位固定为 1（1::k:16）。
        /// </summary>
        private int GetInputFlagProductSerial(int productIndex)
        {
            if (IsDemuxDualPortTemplate())
                return 1;
            return productIndex;
        }

        /// <summary>
        /// 入光通道：Demux 双口每 SN 占 1 入光槽（Even/Odd 共用 productIndex）；多口产品按 PORT/L 名解析；否则回退 SN 槽位映射。
        /// </summary>
        private int GetInputChannelForProductPort(int productIndex, string portKey)
        {
            if (IsDemuxDualPortTemplate())
            {
                if (productIndex >= 1 && productIndex <= OpticalSwitchConfigNames.MaxInputSwitchChannels)
                    return productIndex;
                return GetInputChannelForProduct(productIndex);
            }

            int ch = ResolvePortChannelWithoutDemuxOverride(productIndex, portKey);
            if (ch >= 1 && ch <= OpticalSwitchConfigNames.MaxInputSwitchChannels)
                return ch;
            return GetInputChannelForProduct(productIndex);
        }

        /// <summary>
        /// 按 PORT/L 解析通道（不含 Demux 出光 9/11 映射）。
        /// </summary>
        private int ResolvePortChannelWithoutDemuxOverride(int productIndex, string portKey)
        {
            foreach (PortAssist assist in portAssistant)
            {
                if (assist.ProductIndex == productIndex &&
                    string.Equals(assist.Port, portKey, StringComparison.OrdinalIgnoreCase))
                {
                    int channel = ResolveSwitchChannel(assist.Name, assist.Port);
                    return ApplySinglePortSlotChannelMapping(productIndex, channel);
                }
            }
            int fallback = ResolveSwitchChannel(null, portKey);
            return ApplySinglePortSlotChannelMapping(productIndex, fallback);
        }

        /// <summary>
        /// 打开模板时写入 PortAssist.SwitchChannel（与运行时出光通道一致）。
        /// </summary>
        private int ResolveOutputSwitchChannelAtTemplateLoad(PortAssist assist)
        {
            if (IsDemuxDualPortTemplate())
            {
                int demuxCh = GetDemuxOutputChannelForPortIndex(assist.PortIndex);
                if (demuxCh > 0)
                    return demuxCh;
            }
            int switchChannel = ResolveSwitchChannel(assist.Name, assist.Port);
            return ApplySinglePortSlotChannelMapping(assist.ProductIndex, switchChannel);
        }

        /// <summary>
        /// 出光通道：Demux 映射模块9/11；否则按 L 名 / PORTn / 槽位解析。
        /// </summary>
        private int GetOutputChannelForProductPort(int productIndex, string portKey)
        {
            if (IsDemuxDualPortTemplate())
            {
                int demuxCh = GetDemuxOutputChannelForPortIndex(TryParsePortIndex(portKey));
                if (demuxCh > 0)
                    return demuxCh;
            }

            foreach (PortAssist assist in portAssistant)
            {
                if (assist.ProductIndex == productIndex &&
                    string.Equals(assist.Port, portKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (assist.SwitchChannel > 0)
                        return assist.SwitchChannel;
                    int channel = ResolveSwitchChannel(assist.Name, assist.Port);
                    return ApplySinglePortSlotChannelMapping(productIndex, channel);
                }
            }
            int fallback = ResolveSwitchChannel(null, portKey);
            return ApplySinglePortSlotChannelMapping(productIndex, fallback);
        }

        private int GetPmIndexForProductPort(int productIndex, string portKey, ref string errMsg)
        {
            foreach (PortAssist assist in portAssistant)
            {
                if (assist.ProductIndex == productIndex &&
                    string.Equals(assist.Port, portKey, StringComparison.OrdinalIgnoreCase) &&
                    assist.PMIndex > 0)
                    return assist.PMIndex;
            }

            string port = (portKey ?? "").Replace(" ", "").ToUpperInvariant();
            if (port.StartsWith("PORT") && port.Length > 4 &&
                int.TryParse(port.Substring(4), out int portIndex))
            {
                int pmIndex;
                if (TryGetPmIndexForPort(portIndex, out pmIndex, ref errMsg))
                    return pmIndex;
            }

            return 0;
        }

        private bool IsDualSwitchConfigured()
        {
            IOpticalSwitch sw = null;
            string err = "";
            return DeviceControl.GetSwitchByType(OpticalSwitchConfigNames.InterleaverMplus1X16In, ref sw, ref err) == 0 &&
                   DeviceControl.GetSwitchByType(OpticalSwitchConfigNames.InterleaverMplus1X32Out, ref sw, ref err) == 0;
        }

        /// <summary>
        /// 入光盒 MSW：SW1 级联选路（1,1,2=绿灯上路→模块9；1,1,1=红灯下路→模块10）+ 1×8 选通道。
        /// </summary>
        private static string FormatInputMplusMswForChannel(int inChannel)
        {
            if (inChannel < 1 || inChannel > OpticalSwitchConfigNames.MaxInputSwitchChannels)
                return "";
            if (inChannel <= 8)
                return string.Format("MSW 1,1,2;9,1,{0};", inChannel);
            return string.Format("MSW 1,1,1;10,1,{0};", inChannel - 8);
        }

        /// <summary>
        /// 出光盒 MSW：SW1/SW2 级联（1,1,2/1,1,1→模块9/10；2,1,2/2,1,1→模块11/12）+ 1×8 选通道。
        /// </summary>
        private static string FormatOutputMplusMswForChannel(int outChannel)
        {
            if (outChannel < 1 || outChannel > OpticalSwitchConfigNames.MaxOutputSwitchChannels)
                return "";
            if (outChannel <= 8)
                return string.Format("MSW 1,1,2;9,1,{0};", outChannel);
            if (outChannel <= 16)
                return string.Format("MSW 1,1,1;10,1,{0};", outChannel - 8);
            if (outChannel <= 24)
                return string.Format("MSW 2,1,2;11,1,{0};", outChannel - 16);
            return string.Format("MSW 2,1,1;12,1,{0};", outChannel - 24);
        }

        /// <summary>
        /// 手动光路对比实验：弹窗显示程序本应下发的 flag，操作员确认后返回 true。
        /// </summary>
        private bool ShowManualRefSwitchPrompt(PortAssist assist)
        {
            if (assist == null)
                return false;

            int productIndex = assist.ProductIndex;
            string portKey = assist.Port;
            int outChannel = GetOutputChannelForProductPort(productIndex, portKey);
            string pmErr = "";
            int pmIndex = GetPmIndexForProductPort(productIndex, portKey, ref pmErr);
            int inChannel = GetInputChannelForProductPort(productIndex, portKey);

            string switchDir = Path.Combine(Environment.CurrentDirectory, "switch");
            StringBuilder body = new StringBuilder();
            body.AppendLine("【手动光路模式】已跳过自动光开关。");
            body.AppendLine(string.Format("产品 {0}，通道 {1}，端口 {2}", productIndex, assist.Name, portKey));
            body.AppendLine();
            body.AppendLine("请在外部串口/工具对入光盒、出光盒手动下发 MSW 后点「确定」。");
            body.AppendLine("下表为程序自动模式将使用的 flag（可在 switch 指令表中查找对应 MSW 行）：");
            body.AppendLine();

            if (IsDualSwitchConfigured())
            {
                int inProductSerial = GetInputFlagProductSerial(productIndex);
                string inFlag = inProductSerial.ToString() + "::" + inChannel.ToString() + ":" +
                                OpticalSwitchConfigNames.MaxInputSwitchChannels.ToString();
                body.AppendLine("入光盒（1×16 级联：ch1~8 → MSW 1,1,2;9,1,n；ch9~16 → MSW 1,1,1;10,1,n）：");
                body.AppendLine("  flag = " + inFlag);
                body.AppendLine("  MSW = " + FormatInputMplusMswForChannel(inChannel));
                body.AppendLine("  文件 = " + Path.Combine(switchDir, OpticalSwitchConfigNames.InterleaverMplus1X16In));
                if (pmIndex >= 1)
                {
                    string outFlag = pmIndex.ToString() + "::" + outChannel.ToString() + ":" +
                                     OpticalSwitchConfigNames.MaxOutputSwitchChannels.ToString();
                    body.AppendLine("出光盒（1×32 级联：ch1~8→1,1,2;9 / 9~16→1,1,1;10 / 17~24→2,1,2;11 / 25~32→2,1,1;12）：");
                    body.AppendLine("  flag = " + outFlag);
                    body.AppendLine("  MSW = " + FormatOutputMplusMswForChannel(outChannel));
                    body.AppendLine("  文件 = " + Path.Combine(switchDir, OpticalSwitchConfigNames.InterleaverMplus1X32Out));
                }
                else
                {
                    body.AppendLine("出光盒：未配置 GROUP 功率计映射 — " + pmErr);
                }
                body.AppendLine();
                body.AppendLine(string.Format("（参考）出光通道 outChannel={0}，入光通道 inChannel={1}", outChannel, inChannel));
            }
            else
            {
                string legacyFlag = productIndex.ToString() + "::" + outChannel.ToString() + ":" + SWMaxPortFlag.ToString();
                body.AppendLine("单盒/旧版 MPLUS：");
                body.AppendLine("  flag = " + legacyFlag);
                body.AppendLine("  文件 = " + Path.Combine(switchDir, OpticalSwitchConfigNames.InterleaverMplus1X16));
            }

            RealtimeMsg(string.Format("手动光路：产品{0} {1} 预期入通道{2} 出通道{3} PM{4}",
                productIndex, assist.Name, inChannel, outChannel, pmIndex > 0 ? pmIndex.ToString() : "?"));

            return MessageBox.Show(body.ToString(), "手动光路 — 请切换开关", MessageBoxButton.OKCancel,
                MessageBoxImage.Information) == MessageBoxResult.OK;
        }

        /// <summary>
        /// 归零扫描前读取功率计瞬时功率（用于手动光路对比实验）。
        /// </summary>
        private void ReadRefPowerSnapshot(PortAssist assist)
        {
            if (assist == null || DeviceControl == null)
                return;

            HashSet<int> pmIndices = new HashSet<int>();
            string pmErr = "";
            int curPm = GetPmIndexForProductPort(assist.ProductIndex, assist.Port, ref pmErr);
            if (curPm > 0)
                pmIndices.Add(curPm);
            if (assist.PMIndex > 0)
                pmIndices.Add(assist.PMIndex);

            foreach (int pm in portAndPMDic.Values)
            {
                if (pm > 0)
                    pmIndices.Add(pm);
            }

            if (pmIndices.Count == 0)
            {
                RealtimeMsg("手动光路功率：未配置 GROUP 功率计映射，请用扫描结果判断是否有光。");
                return;
            }

            List<string> powerLines = new List<string>();
            List<RealtimePowerInfo> realtimePowers = new List<RealtimePowerInfo>();
            foreach (int pmIndex in pmIndices.OrderBy(i => i))
            {
                IPowermeter pm = null;
                int channel = 0;
                string errMsg = "";
                if (DeviceControl.GetPowermeterByIndex(pmIndex, ref channel, ref pm, ref errMsg) != 0 || pm == null)
                {
                    powerLines.Add(string.Format("PM{0}=（设备未配置）", pmIndex));
                    continue;
                }

                List<double> powerAvgs;
                if (pm.ReadPowerAvg(ref errMsg, out powerAvgs, 3, false, channel.ToString()) != 0 ||
                    powerAvgs == null || powerAvgs.Count == 0)
                {
                    powerLines.Add(string.Format("PM{0}=读数失败", pmIndex));
                    continue;
                }

                double dbm = powerAvgs[0];
                powerLines.Add(string.Format("PM{0}={1:F2} dBm", pmIndex, dbm));
                RealtimePowerInfo info = new RealtimePowerInfo();
                info.Prefix = "PM" + pmIndex;
                info.Power = dbm.ToString("F2") + " dBm";
                realtimePowers.Add(info);
            }

            string summary = "手动光路功率：" + string.Join(", ", powerLines);
            RealtimeMsg(summary);
            CommonFunction.WriteLog(summary);

            if (EventAggregator != null && realtimePowers.Count > 0)
                EventAggregator.GetEvent<EventRealtimePowerUpdate>().Publish(realtimePowers);
        }

        private bool TryExecuteSwitchOnNamedDevice(string switchShowName, string flag, ref string errMsg)
        {
            IOpticalSwitch opticalSwitch = null;
            errMsg = "";
            if (DeviceControl.GetSwitchByType(switchShowName, ref opticalSwitch, ref errMsg) != 0 || opticalSwitch == null)
                return false;

            return opticalSwitch.SetSwitch(flag, ref errMsg) == 0;
        }

        private bool TryExecuteSwitchCommand(string switchShowName, string flag, string roleLabel, ref string errMsg)
        {
            errMsg = "";
            if (TryExecuteSwitchOnNamedDevice(switchShowName, flag, ref errMsg))
            {
                RealtimeMsg(string.Format("切换{0}成功！(flag={1})", roleLabel, flag));
                return true;
            }

            // 产线仅部署旧版 switch\interleaverSwitch-MPLUS 时，入光 flag（1::n:16）仍可匹配
            if (string.Equals(switchShowName, OpticalSwitchConfigNames.InterleaverMplus1X16In, StringComparison.OrdinalIgnoreCase))
            {
                string legacyErr = "";
                if (TryExecuteSwitchOnNamedDevice(OpticalSwitchConfigNames.InterleaverMplus1X16, flag, ref legacyErr))
                {
                    RealtimeMsg(string.Format("切换{0}成功（使用旧版 switch\\{1}，建议部署 switch\\{2}）。(flag={3})",
                        roleLabel, OpticalSwitchConfigNames.InterleaverMplus1X16,
                        OpticalSwitchConfigNames.InterleaverMplus1X16In, flag));
                    return true;
                }
                if (string.IsNullOrEmpty(errMsg))
                    errMsg = legacyErr;
            }

            string switchFile = System.IO.Path.Combine(Environment.CurrentDirectory, "switch", switchShowName);
            if (string.IsNullOrEmpty(errMsg))
                errMsg = string.Format("未找到指令配置文件或 flag={0}。请确认存在: {1}", flag, switchFile);
            else if (errMsg.IndexOf("未找到", StringComparison.OrdinalIgnoreCase) < 0)
                errMsg += string.Format("（指令表: {0}, flag={1}）", switchFile, flag);

            return false;
        }

        private bool SetInputSwitch(int inputProductSerial, int inChannel, ref string errMsg)
        {
            errMsg = "";
            if (inChannel < 1 || inChannel > OpticalSwitchConfigNames.MaxInputSwitchChannels)
            {
                errMsg = string.Format("切换入光开关失败:入通道 {0} 无效(1-{1})",
                    inChannel, OpticalSwitchConfigNames.MaxInputSwitchChannels);
                return false;
            }

            string flag = inputProductSerial.ToString() + "::" + inChannel.ToString() + ":" +
                            OpticalSwitchConfigNames.MaxInputSwitchChannels.ToString();
            if (TryExecuteSwitchCommand(
                OpticalSwitchConfigNames.InterleaverMplus1X16In, flag, "入光开关", ref errMsg))
                return true;

            return false;
        }

        private bool SetOutputSwitch(int pmIndex, int outChannel, ref string errMsg)
        {
            errMsg = "";
            if (outChannel < 1 || outChannel > OpticalSwitchConfigNames.MaxOutputSwitchChannels)
            {
                errMsg = string.Format("切换出光开关失败:出通道 {0} 无效(1-{1})",
                    outChannel, OpticalSwitchConfigNames.MaxOutputSwitchChannels);
                return false;
            }
            if (pmIndex < 1)
            {
                errMsg = "切换出光开关失败:未配置功率计映射(GROUP)";
                return false;
            }

            string flag = pmIndex.ToString() + "::" + outChannel.ToString() + ":" +
                            OpticalSwitchConfigNames.MaxOutputSwitchChannels.ToString();
            if (TryExecuteSwitchCommand(OpticalSwitchConfigNames.InterleaverMplus1X32Out, flag, "出光开关", ref errMsg))
                return true;

            return false;
        }

        private int GetSwitchChannelForPort(int productIndex, string portKey)
        {
            return GetOutputChannelForProductPort(productIndex, portKey);
        }

        private bool TryGetPmIndexForPort(int portIndex, out int pmIndex, ref string errMsg)
        {
            pmIndex = 0;
            string key = portIndex.ToString();
            if (portAndPMDic.TryGetValue(key, out pmIndex))
                return true;

            errMsg = string.Format("端口 PORT{0} 未配置功率计映射，请在模板 CFG 的 GROUP 中设置（例如 PORT{0}:PM1）。", portIndex, portIndex);
            return false;
        }

        private bool SetSwitch(int productIndex, string portKey, ref string errMsg)
        {
            errMsg = "";
            int outChannel = GetSwitchChannelForPort(productIndex, portKey);
            if (outChannel < 1 || outChannel > OpticalSwitchConfigNames.MaxOutputSwitchChannels)
            {
                errMsg = string.Format("切换开关失败:无法解析端口 {0} 对应的出光通道(1-{1})",
                    portKey, OpticalSwitchConfigNames.MaxOutputSwitchChannels);
                return false;
            }

            if (IsDualSwitchConfigured())
            {
                int inChannel = GetInputChannelForProductPort(productIndex, portKey);
                int inProductSerial = GetInputFlagProductSerial(productIndex);
                string pmErr = "";
                int pmIndex = GetPmIndexForProductPort(productIndex, portKey, ref pmErr);
                if (pmIndex < 1)
                {
                    errMsg = pmErr;
                    return false;
                }
                if (!SetInputSwitch(inProductSerial, inChannel, ref errMsg))
                    return false;
                if (!SetOutputSwitch(pmIndex, outChannel, ref errMsg))
                    return false;
                return true;
            }

            // 兼容旧版单 MPLUS 指令表 interleaverSwitch-MPLUS
            RealtimeMsg("提示:使用旧版单光开关配置，建议升级为 IN/OUT 双设备。");
            string legacyFlag = productIndex.ToString() + "::" + outChannel.ToString() + ":" + SWMaxPortFlag.ToString();
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType(OpticalSwitchConfigNames.InterleaverMplus1X16, ref opticalSwitch, ref errMsg) != 0)
            {
                errMsg = "";
                DeviceControl.GetSwitchByIndex(1, ref opticalSwitch, ref errMsg);
            }
            if (opticalSwitch == null)
            {
                if (string.IsNullOrEmpty(errMsg))
                    errMsg = "未找到可用的 MPLUS 光开关设备，请检查 set\\Deviceconfig.xml 与设备初始化。";
                return false;
            }
            if (opticalSwitch.SetSwitch(legacyFlag, ref errMsg) == 0)
            {
                RealtimeMsg(string.Format("切换开关成功！(产品{0} 通道{1} flag={2})", productIndex, outChannel, legacyFlag));
                return true;
            }
            return false;
        }

        private void SetSwitch(int productIndex, string portKey)
        {
            string errMsg = "";
            if (!SetSwitch(productIndex, portKey, ref errMsg))
            {
                if (errMsg.Length > 0)
                    RealtimeMsg("切换开关失败:" + errMsg);
            }
        }

        /// <summary>
        /// 扫描/测试前切换光开关；失败时提示并返回 false（与系统归零同一套 Demux 入出光解析）。
        /// </summary>
        private bool TrySetSwitchBeforeScan(int productIndex, string portKey)
        {
            if (TasRuntimeConfig.IsRefAutoSwitchDisabled())
                return true;

            string switchErr = "";
            if (SetSwitch(productIndex, portKey, ref switchErr))
                return true;

            ErrorBox(string.IsNullOrEmpty(switchErr) ? "切换光开关失败。" : switchErr);
            return false;
        }

        /// <summary>
        /// 读取归零数据
        /// </summary>
        /// <param name="errMsg"></param>
        private void ReadRefData(int productIndex,List<PortAssist> assists, ref string errMsg)
        {
            try
            {
                string productSpec = "";
                for(int i=0;i<assists.Count;i++)
                {
                    //productIndex = assists[i].ProductIndex - 1;
                    productSpec = allProductControl[productIndex].GetProductInfo().Spec;
                    if (assists[i].ProductIndex != productIndex+1)
                        continue;
                    string pdlRefPath = string.Format("{0}-product{1}-port{2}.csv", refWithPDLFile, productIndex + 1, assists[i].PortIndex);

                    int refCount = 0;
                    if (InterleaverScanResult.ReadRefPortCount(pdlRefPath, ref refCount, ref errMsg) != 0)
                    {
                        assists[i].IsRef = false;
                        return;
                    }
                    DateTime refTime = new DateTime();
                    DateTime curTime = DateTime.Now;
                    DateTime defaultTime = new DateTime();
                    if (InterleaverScanResult.ReadRefTime(pdlRefPath,ref refTime,ref errMsg)!=0)
                    {
                        assists[i].IsRef = false;
                        return;
                    }
                    TimeSpan refSpan = curTime - refTime;
                    int refIndex = productIndex * portAndNameDic.Count  + assists[i].PortIndex - 1;
                    if (IsRefTimePassdue(refSpan))
                    {
                        assists[i].IsRef = false;
                        errMsg = "归零数据超过6小时，需要重新归零！";
                        //清除内存归零数据 
                        InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                        return;
                    }

                    if (portPDLRef[refIndex] !=null)
                    {
                        if (portAndNameDic.Count != refCount)
                        {
                            //提示归零数据不正确，需要重新归零？
                            assists[i].IsRef = false;
                            errMsg = "归零数据与模板不一致，需要重新归零！";
                            return;
                        }                       

                        //读取PDL的归零数据
                        int pdlRef = InterleaverScanResult.ReadScanData(pdlRefPath, portPDLRef[refIndex], ref errMsg);
                        double[] fres = portPDLRef[refIndex][6];
                        if(fres[fres.Length - 1] <maxScanFre||fres[0]>minScanFre)
                        {
                            errMsg = "测试频率超出归零频率，请确认服务器扫描范围!";
                            //清除内存归零数据 
                            InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                            return;
                        }
                        //四个归零时间中，最老的时间用来做倒计时
                        if (oldestRefTime.Equals(defaultTime) || oldestRefTime.CompareTo(refTime) > 0)
                        {
                            oldestRefTime = refTime;
                        }
                        if (pdlRef==0)
                        {
                            assists[i].IsRef = true;
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

        private int ScanAndCalResultFSTP(ScanDetail scanInfo, ref string errMsg)
        {
            try
            {
                CommonFunction.WriteLog("ScanAndCalResultFSTP begin");
                int refIndex = (scanInfo.ProductIndex - 1) * portAndNameDic.Count + scanInfo.Ports[0] - 1;
                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                    CommonFunction.WriteLog(string.Format("clear refdata:{0}", refIndex));
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                {
                    foreach (int port in scanInfo.Ports)
                    {
                        int dataIndex = (scanInfo.ProductIndex - 1) * portAndNameDic.Count + port - 1;
                        InterleaverScanResult.InitRawdataBuffer(portResData[dataIndex]);
                        CommonFunction.WriteLog(string.Format("clear data:{0}", dataIndex));
                    }
                }
                string resPath = "";
                int res = 0;
                try
                {
                    CommonFunction.WriteLog(string.Format("DoScan begin"));
                    res = DoScan(scanInfo, ref resPath, ref errMsg);
                    CommonFunction.WriteLog(string.Format("DoScan res:{0}", res));
                }
                catch (Exception ex)
                {
                    errMsg = ex.InnerException.Message;
                    CommonFunction.WriteLog(string.Format("DoScan Exception:{0}", errMsg));
                    return 2;
                }
                if (errMsg.Length > 0 || res != 0)
                {
                    return res;
                }

                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    int pmIndex;
                    if (!TryGetPmIndexForPort(scanInfo.Ports[0], out pmIndex, ref errMsg))
                        return 2;
                    string pdlRefPath = string.Format("{0}-product{1}-port{2}.csv", refWithPDLFile, CurProductIndex + 1, scanInfo.Ports[0]);


                    string path = scanWithPDLFile + pmIndex.ToString() + ".csv";
                    if (File.Exists(path))
                    {
                        InterleaverScanResult.ReadScanData(path, portPDLRef[refIndex], ref errMsg);
                        if (errMsg.Length > 0)
                            return 2;
                        double[] fres = portPDLRef[refIndex][6];
                        if (fres[fres.Length - 1] < maxScanFre || fres[0] > minScanFre)
                        {
                            errMsg = "测试频率超出归零频率，请确认归零扫描范围";
                            InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                            return 2;
                        }
                        InterleaverScanResult.WritePDLRefData(pdlRefPath, portPDLRef[refIndex], allProductControl[CurProductIndex].GetProductInfo().Spec, portAndNameDic.Count, ref errMsg);
                    }

                }
                if (errMsg.Length == 0)
                {
                    //InitRawdataBuffer(ref rawdata);
                    if (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                    {
                        for (int j = 0; j < scanInfo.Ports.Count; j++)
                        {
                            int pmIndex;
                            if (!TryGetPmIndexForPort(scanInfo.Ports[j], out pmIndex, ref errMsg))
                                return 2;
                            int dataIndex = (scanInfo.ProductIndex - 1) * portAndNameDic.Count + scanInfo.Ports[j] - 1;

                            string path = scanWithPDLFile + pmIndex.ToString() + ".csv";
                            CommonFunction.WriteLog(string.Format("path:{0}", path));
                            InterleaverScanResult.ReadScanData(path, portResData[dataIndex], ref errMsg);
                            CommonFunction.WriteLog(string.Format("ReadScanData finished"));
                            InterleaverScanResult.CalFSTPRawdata(portPDLRef[dataIndex], portResData[dataIndex], ref errMsg);
                            CommonFunction.WriteLog(string.Format("CalFSTPRawdata finished"));

                        }
                    }
                    return 0;
                }
                return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }


        /// <summary>
        /// 扫描并读取返回的结果
        /// </summary>
        /// <param name="scanInfo">扫描信息，是否带PDL，归零还是测试，归零通道等具体信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        private int ScanAndCalResult(ScanDetail scanInfo, ref string errMsg)
        {
            try
            {
                int refIndex = (scanInfo.ProductIndex-1) * portAndNameDic.Count + scanInfo.Ports[0] - 1;
                //清除上一次测试数据
                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {                    
                    InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithPDL||scanInfo.ScanType==SCANTYPE.TestWithPDLOnekey)
                {
                    foreach(int port in scanInfo.Ports)
                    {
                        int dataIndex = (scanInfo.ProductIndex - 1) * portAndNameDic.Count + port - 1;
                        InterleaverScanResult.InitRawdataBuffer(portResData[dataIndex]);
                    }
                }
               
                string resPath = "";
                int res = 0;
                try
                {
                    
                    res = DoScan(scanInfo, ref resPath, ref errMsg);
                }
                catch (Exception ex)
                {
                    errMsg = ex.InnerException.Message;
                    return 2;
                }
                
                if (errMsg.Length > 0 || res != 0)
                {
                    return res;
                }
                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    int pmIndex;
                    if (!TryGetPmIndexForPort(scanInfo.Ports[0], out pmIndex, ref errMsg))
                        return 2;
                    
                    //读取四个偏振态下原始数据
                    for (int i = 0; i < 4; i++)
                    {
                        string path = scanWithPDLFile + pmIndex.ToString() + (i + 1).ToString() + ".csv";
                        InterleaverScanResult.ReadScanData(path, pdlRawData[i], ref errMsg);
                    }

                    InterleaverScanResult.CalPDLRefData(pdlRawData, portPDLRef[refIndex], ref errMsg);
                    if (errMsg.Length > 0)
                        return 2;
                    double[] fres = portPDLRef[refIndex][6];
                    if (fres[fres.Length - 1] < maxScanFre || fres[0] > minScanFre)
                    {
                        errMsg = "测试频率超出归零频率，请确认服务器扫描范围!";
                        //清除内存归零数据 
                        InterleaverScanResult.InitRawdataBuffer(portPDLRef[refIndex]);
                        return 2;
                    }

                    string pdlRefPath = string.Format("{0}-product{1}-port{2}.csv", refWithPDLFile, CurProductIndex + 1, scanInfo.Ports[0]);
                    InterleaverScanResult.WritePDLRefData(pdlRefPath, portPDLRef[refIndex], allProductControl[CurProductIndex].GetProductInfo().Spec, portAndNameDic.Count, ref errMsg);

                }
               
                if (errMsg.Length == 0)
                {
                    //先将数据清零
                    //InitRawdataBuffer(ref rawdata);
                    if (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                    {
                        for (int j = 0; j < scanInfo.Ports.Count; j++)
                        {
                            int pmIndex;
                            if (!TryGetPmIndexForPort(scanInfo.Ports[j], out pmIndex, ref errMsg))
                                return 2;
                            int dataIndex = (scanInfo.ProductIndex - 1) * portAndNameDic.Count + scanInfo.Ports[j] - 1;
                            //读取四个偏振态下原始数据
                            for (int i = 0; i < 4; i++)
                            {
                                string path = scanWithPDLFile + pmIndex.ToString() + (i + 1).ToString() + ".csv";
                                InterleaverScanResult.ReadScanData(path, pdlRawData[i], ref errMsg);
                                double[] fres = pdlRawData[i][2];
                                if (fres[fres.Length - 1] < maxScanFre || fres[0] > minScanFre)
                                {
                                    errMsg = "测试频率超出扫描数据频率，请确认服务器扫描范围!"; 
                                    InterleaverScanResult.InitRawdataBuffer(pdlRawData[i]);
                                    return 2;
                                }
                            }
                            if (convertAlgorithm.ToUpper() == ConvertAlgorithm.Ave.GetAdditional().ToUpper())
                            {
                                string recPath = scanWithPDLFile + scanInfo.Ports[j].ToString() + "Ave.CSV";
                                InterleaverScanResult.CalRawdataByAve(pdlRawData, portPDLRef[dataIndex], portResData[dataIndex], ref errMsg);
                                //InterleaverScanResult.WriteCalData(recPath, portResData[scanInfo.Port - 1], ref errMsg);
                            }
                            else if (convertAlgorithm.ToUpper() == ConvertAlgorithm.MaxMin.GetAdditional().ToUpper())  //将四个偏振态数据转为ave PDL max min数据
                            {
                                string recPath = scanWithPDLFile + scanInfo.Ports[j].ToString() + "MaxMin.CSV";
                                InterleaverScanResult.CalRawdataByMaxMin(pdlRawData, portPDLRef[dataIndex], portResData[dataIndex], ref errMsg);
                                //InterleaverScanResult.WriteCalData(recPath, portResData[scanInfo.Port - 1], ref errMsg);
                            }
                            else if (convertAlgorithm.ToUpper() == ConvertAlgorithm.Mueller.GetAdditional().ToUpper())
                            {
                                string recPath = scanWithPDLFile + scanInfo.Ports[j].ToString() + "Mueller.CSV";
                                InterleaverScanResult.CalRawdataByMueller(pdlRawData, portPDLRef[dataIndex], portResData[dataIndex], ref errMsg);
                                
                                InterleaverScanResult.WriteCalData(recPath, portResData[dataIndex], ref errMsg);
                            }
                        }
                    }
                    return 0;
                }
                return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 2;
            }
        }

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="scanInfo">扫描类型，是否带PDL，归零还是测试</param>
        /// <param name="resPath">保存扫描结果文件路径</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        private int DoScan(ScanDetail scanInfo, ref string resPath, ref string errMsg)
        {
            bool isFSTP = true;
            if (isFSTP)
            {
                //为兼容C Lband产品，改为模板配置，如果未配置，默认为C Band产品
                double dStopWL = maxRefScanWL;
                double dStartWL = minRefScanWL;

                if (Math.Abs(minRefScanWL) < 0.1 || Math.Abs(maxRefScanWL) < 0.1)
                {
                    dStartWL = 1520;
                    dStopWL = 1580;
                    minRefScanFre = lightSpeed / dStopWL;
                    maxRefScanFre = lightSpeed / dStartWL;
                }


                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    IUDLFSTP scan = null;
                    DeviceControl.GetUDLFstpByGUID(2, ref scan, ref errMsg);
                    if(scan!=null)
                    {
                        resPath = scanWithPDLFile;
                        int scanRes = 0;
                        string savePath = scanWithPDLFile;
                        string scanErrMsg = "";
                        this.Dispatcher.Invoke(() =>
                        {
                            scanRes = scan.Scan(true, true, dStartWL, dStopWL, fstpScanStep, ref savePath, ref scanErrMsg);
                        });
                        CommonFunction.WriteLog(string.Format("fstp scan result:{0}", scanRes));
                        if (scanRes != 0)
                        {
                            errMsg = scanErrMsg;
                            return 2;
                        }
                        else
                            return 0;
                    }
                    
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithPDL|| scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                {
                    CommonFunction.WriteLog(string.Format("fstp scan TestWithPDL"));
                    IUDLFSTP scan = null;
                    DeviceControl.GetUDLFstpByGUID(2, ref scan, ref errMsg);
                    if (scan != null)
                    {
                        CommonFunction.WriteLog(string.Format("Get scan object"));
                        int scanRes = 0;
                        string savePath = scanWithPDLFile;
                        string scanErrMsg = "";
                        this.Dispatcher.Invoke(() =>
                        {
                            scanRes = scan.Scan(true, true, dStartWL, dStopWL, fstpScanStep, ref savePath, ref scanErrMsg);
                        });
                        CommonFunction.WriteLog(string.Format("fstp scan result:{0}", scanRes));
                        if (scanRes != 0)
                        {
                            errMsg = scanErrMsg;
                            return 2;
                        }
                        else
                            return 0;
                    }
                }
               
            }
            else
            {
                IInterleaverScan scan = null;
                DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
                if (scan != null)
                {
                    if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                    {
                        resPath = scanWithPDLFile;
                        return scan.Scan(true, true, ref resPath, ref errMsg);
                    }
                    else if (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                    {
                        resPath = scanWithPDLFile;
                        return scan.Scan(true, false, ref resPath, ref errMsg);
                    }

                }
            }
            return 2;
        }

        /// <summary>
        /// 开启扫描background线程
        /// </summary>
        /// <param name="scanType">扫描类型</param>
        private void DoScanOnBK()
        {
            if (curTestTmpt > -299 && !TasRuntimeConfig.IsTccChamberCheckDisabled())
            {
                string chamberMsg;
                if (!TryValidateChamberTemperature(curTestTmpt, out chamberMsg))
                {
                    RealtimeMsg(chamberMsg, StatusType.Error);
                    ErrorBox(chamberMsg);
                    UIControl.IsScanEnable = true;
                    UIControl.IsSaveEnable = true;
                    return;
                }
            }
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
        }

        /// <summary>
        /// PM1扫描background函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                CommonFunction.WriteLog("Scan_DoWork begin");
                scanDetailInfo = (ScanDetail)e.Argument;
                CurProductIndex = scanDetailInfo.ProductIndex-1;
                this.Dispatcher.Invoke(new Action(UpdateProductStatuses));
                scanErrorMsg = "";
                bool isFstpScan = true;
                int res = 0;
                if (isFstpScan)
                {
                    CommonFunction.WriteLog("ScanAndCalResultFSTP begin");
                    res = ScanAndCalResultFSTP(scanDetailInfo, ref scanErrorMsg);
                }
                else
                    res = ScanAndCalResult(scanDetailInfo, ref scanErrorMsg);
                SetIsScanFinished(true);
                if (scanErrorMsg.Length > 0 || res != 0)
                {
                    string errMsg = "";
                    //清除测试结果
                    //ClearResult(scanDetailInfo.Ports, ref errMsg);
                    if (res == 1)
                    {
                        if (scanErrorMsg.Length == 0)
                        {
                            scanErrorMsg = "扫描出错";
                        }
                        //ReconnectServer(ref errMsg);
                    }
                    return;
                }
                else
                {
                    if (GetOpenTemplateComplete())
                    {
                        if (scanDetailInfo.ScanType == SCANTYPE.TestWithNoPDL || scanDetailInfo.ScanType == SCANTYPE.TestWithPDL
                            || scanDetailInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
                        {
                            string errMsg = "";
                            //计算当前通道的参数结果。
                            CalAllResultInThread();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                SetIsScanFinished(true);
            }

        }

        private int FindAdjPortIndex(string portName)
        {
            string[] portKeys = portAndNameDic.Keys.ToArray();
            string[] ports = portName.Split('-');
            foreach(string adjPort in portKeys)
            {
                string[] adjPorts = adjPort.Split('-');
                if(ports[0]==adjPorts[0])
                {
                    if(adjPort != portName)
                    {                  
                        string portIndex = portAndNameDic[adjPort];
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
                        string portNum = portIndex.Substring(numBeginIndex, portIndex.Length - numBeginIndex);
                        int adjPortIndex = Convert.ToInt32(portNum);
                        return adjPortIndex;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 计算参数函数
        /// </summary>
        /// <param name="calPort">计算的端口号</param>
        /// <param name="errMsg">出错信息</param>
        private void CalResByPort(int calPort, ref string errMsg)
        {
            try
            {
                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[CurProductIndex].GetAllTestInfo();

                var typeName = algorithm.GetType();
                IInterleaverAlgorithm interleaverAlgorithm = (IInterleaverAlgorithm)Activator.CreateInstance(typeName);
                ParamCal calFuntion = new ParamCal(interleaverAlgorithm);
                int paramCount = allTestParam.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    if (allTestParam[i].Temperature.CompareTo(curTestTmpt)!=0)
                    {
                        continue;
                    }
                    string param = allTestParam[i].ExParamName;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');

                    if (portSplits.Length >= 2)
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
                        string portNum = portIndex.Substring(numBeginIndex, portIndex.Length - numBeginIndex);
                        int port = Convert.ToInt32(portNum);
                        //不是当前扫描的通道
                        if (calPort != port)
                        {
                            continue;
                        }
                        //不是扫描的温度，则返回
                        portName = portSplits[0];
                        bool isPass = true;
                        //计算参数结果
                        double paramResult = CommonFunction.GetDefaultValue();
                        int dataIndex = CurProductIndex * portAndNameDic.Count + port - 1;
                        int adjPortIndex = FindAdjPortIndex(portName);
                        if (adjPortIndex != -1)
                        {
                            int adjDataIndex = CurProductIndex * portAndNameDic.Count + adjPortIndex - 1;
                            paramResult = calFuntion.CalChannelTestParam(param,minScanFre,maxScanFre, portResData[dataIndex], portResData[adjDataIndex], fre, productFre, ref errMsg);
                        }
                        else
                        {
                            paramResult = calFuntion.CalChannelTestParam(param, minScanFre, maxScanFre, portResData[dataIndex], null, fre, productFre, ref errMsg);
                        }
                        paramResult = Math.Round(paramResult, 3);
                        string[] paramSplits = param.Split('@');
                        //string maxILParam = "MAXIL@PB=" + passBand.ToString();
                        if (paramSplits[0].ToUpper() != "MAXIL")
                            AddResultToRecord(curPortRecords, param, portSplits[0], allTestParam[i].Temperature.ToString(), paramResult, ref errMsg);
                        if (errMsg.Length == 0)
                        {             
                            allProductControl[CurProductIndex].UpdateTestData(i, paramResult, ref isPass);
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

        private int GetPortIndexByName(string name)
        {
            foreach(PortAssist assist in portAssistant)
            {
                if (assist.Name == name)
                    return assist.PortIndex;
            }
            return -1;
        }

        private void CalBPParamRes(ref string errMsg)
        {
            FusionControl loadDatacontrol = new FusionControl();
            List<MESTestInfo> allTestParam = allProductControl[CurProductIndex].GetAllTestInfo();
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
                if (bpSetSplits.Length < 7|| curSetSplits.Length<5)
                    continue;
                if(bpProcess!= bpSetSplits[1])
                {
                    bpProcess = bpSetSplits[1];
                    string strErr = "";
                    loadDatacontrol.LoadTestData(allProductControl[CurProductIndex].ProductSN, bpProcess, mainInfo.UserID, out strErr);
                    if(strErr.Length>0)
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
                    if(testInfo.EnvironmentID.ToUpper()== bpSetSplits[2].ToUpper()
                        && testInfo.ObjectID.ToUpper() == bpSetSplits[3].ToUpper()
                        && testInfo.PortID.ToUpper() == bpSetSplits[4].ToUpper()
                        && testInfo.ConditionID.ToUpper() == bpSetSplits[5].ToUpper()
                        && testInfo.TestParam.GetMESTemplateKeywords().ToUpper() == bpSetSplits[6].ToUpper())
                    {
                        bpValue =Convert.ToDouble( testInfo.TestedValue);
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
                if (curValue.CompareTo(CommonFunction.GetDefaultValue())==0
                    || bpValue.CompareTo(CommonFunction.GetDefaultValue()) == 0)
                {
                    continue;
                }
                double paramResult = curValue-bpValue;
               // CommonFunction.WriteLog(paramResult.ToString());
                if (errMsg.Length == 0 && (!CommonFunction.IsDefault(paramResult)))
                {
                    paramResult = Math.Round(paramResult, 3);
                    bool isPass = false;
                    allProductControl[CurProductIndex].UpdateTestData(i, paramResult, ref isPass);
                }
            }

        }

        /// <summary>
        /// 计算port参数
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void CalPortRes(List<int> calPorts, ref string errMsg)
        {
            try
            {
                List<MESTestInfo> allTestParam = allProductControl[CurProductIndex].GetAllTestInfo();
                for(int i=0;i<allTestParam.Count;i++)
                {
                    if (!allTestParam[i].Tested)
                        continue;
                    string param = allTestParam[i].ExParamName;
                    string[] paramSplits = param.Split('@');
                    //string maxILParam = "MAXIL@PB=" + passBand.ToString();
                    if (paramSplits[0].ToUpper() != "MAXIL")
                        continue;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');
                    if(portSplits.Length>=2)
                    {
                        double paramResult = allTestParam[i].CurValue;
                        AddResultToRecord(curPortRecords, param, portSplits[0], allTestParam[i].Temperature.ToString(), paramResult, ref errMsg);
                    }
                }
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
                        int portIndex = GetPortIndexByName(portSplits[0]);
                        bool bTestPort = false;
                        foreach(int cal in calPorts)
                        {
                            if(portIndex==cal)
                            {
                                bTestPort = true;
                            }
                        }
                        if (!bTestPort)
                            continue;

                        bool isPass = true;
                        //计算参数结果
                        double paramResult = paramCal.CalPortParam(param, allTestParam[i].Temperature.ToString(), portSplits[0], curPortRecords, allProductControl[CurProductIndex].TmptArray(), ref errMsg);

                        if (errMsg.Length == 0 && (!CommonFunction.IsDefault(paramResult)))
                        {
                            paramResult = Math.Round(paramResult, 3);
                            allProductControl[CurProductIndex].UpdateTestData(i, paramResult, ref isPass);
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
        /// 计算线程函数
        /// </summary>
        /// <param name="param">第几个计算线程</param>
        private void ChannelCalThread(object param)
        {
            int portIndex = Convert.ToInt32(param);
            string errMsg = "";
            SetCalFinished(portIndex, false);
            CalResByPort(scanDetailInfo.Ports[portIndex], ref errMsg);
            SetCalFinished(portIndex, true);
        }


        /// <summary>
        /// 计算参数函数
        /// </summary>
        private void CalAllResultInThread()
        {
            curPortRecords.Clear();
            isPortCalFinished.Clear();
            for(int i=0;i<scanDetailInfo.Ports.Count;i++)
            {
                isPortCalFinished.Add(false);
                Thread calThread = new Thread(new ParameterizedThreadStart(ChannelCalThread));
                calThread.Start(i);
            }            

            while (!IsAllCalFinished())
            {
                Thread.Sleep(100);
            }
            string errMsg = "";
            CalPortRes(scanDetailInfo.Ports,ref errMsg);
            CalBPParamRes(ref errMsg);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetCalFinished(int n, bool isFinished)
        {
            if(isPortCalFinished.Count>n)
                isPortCalFinished[n] = isFinished;
        }

        /// <summary>
        /// 是否所有计算线程结束
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool IsAllCalFinished()
        {
            for (int i = 0; i < isPortCalFinished.Count; i++)
            {
                if (isPortCalFinished[i] == false)
                    return false;
            }
            return true;
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

        /// <summary>
        /// 清除所有数据
        /// </summary>
        /// <param name="clrPort">清除哪个端口数据</param>
        /// <param name="errMsg">出错信息</param>
        private void ClearResult(List<int> clrPorts, ref string errMsg)
        {
            try
            {
                if (GetIsScanFinished() == false)
                    return;

                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[CurProductIndex].GetAllTestInfo();
                
                for (int i = 0; i < allTestParam.Count; i++)
                {
                    //如果不是当前测试温度，则不清除测试结果
                    double tmpt = allTestParam[i].Temperature;
                    if (tmpt != curTestTmpt)
                        continue;

                    string param = allTestParam[i].ExParamName;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');

                    if (portSplits.Length >= 2)
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
                        string portNum = portIndex.Substring(numBeginIndex, portIndex.Length - numBeginIndex);
                        int port = Convert.ToInt32(portNum);
                        //不是当前扫描的通道
                        bool isClrPort = false;
                        foreach(int clr in clrPorts)
                        {
                            if(clr==port)
                            {
                                isClrPort = true;
                                break;
                            }
                        }
                        if (!isClrPort)
                            continue;

                        portName = portSplits[0];
                        bool isPass = false;
                        allProductControl[CurProductIndex].UpdateTestData(i, CommonFunction.GetDefaultValue(), ref isPass);
                    }
                }

                for (int i = 0; i < allTestParam.Count; i++)
                {
                    string param = allTestParam[i].ExParamName;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');
                    //判断是总的端口，然后使用之前的计算结果，进行计算。如何去取得计算结果，再增加循环判断一次？
                    if (portSplits.Length == 1)
                    {
                        if (portName != portSplits[0])
                            continue;
                        bool isPass = true;
                        //计算参数结果

                        if (errMsg.Length == 0)
                        {
                            allProductControl[CurProductIndex].UpdateTestData(i, CommonFunction.GetDefaultValue(), ref isPass);
                            List<MESTestInfo> showInfos = testShowControl[CurProductIndex].GetAllTestInfo();
                            for (int j = 0; j < showInfos.Count; j++)
                            {
                                if (showInfos[j].Temperature == allTestParam[i].Temperature && showInfos[j].PortNameForUser == allTestParam[i].PortNameForUser
                                    && showInfos[j].ExParamName == allTestParam[i].ExParamName)
                                {
                                    testShowControl[CurProductIndex].UpdateTestData(j, CommonFunction.GetDefaultValue(), ref isPass);
                                    break;
                                }
                            }
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
        /// PM1扫描ackground dowork执行结束后函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (CurProductIndex >= 0 && CurProductIndex < AllProducts.Count)
            {
                if (scanErrorMsg.Length > 0)
                {
                    AllProducts[CurProductIndex].HasScanError = true;
                }
                else
                {
                    AllProducts[CurProductIndex].HasScanError = false;
                }
            }

            //开始计算，处理PM2数据
            /*string errMsg = "";
            if(e.Result!=null)
                errMsg = e.Result.ToString();*/
            if (scanErrorMsg.Length > 0)
            {
                RealtimeMsg("扫描出错:" + scanErrorMsg);
                ErrorBox("扫描出错:" + scanErrorMsg);
            }
            else
            {
                RealtimeMsg("扫描结束！");

            }

            ScanFinish(scanDetailInfo);
            UpdateProductStatuses();
        }

        private bool IsMultiSnSinglePortBatch()
        {
            return allProductControl.Count > 1 && portAndNameDic.Count == 1;
        }

        private static bool IsRoomTemperature(double tmpt)
        {
            return tmpt >= 20 && tmpt <= 30;
        }

        /// <summary>
        /// RtOnlyTest 模式下仅允许常温；否则不限制。
        /// </summary>
        private static bool IsTestTemperatureAllowed(double tmpt)
        {
            if (!TasRuntimeConfig.IsRtOnlyTestMode())
                return true;
            return IsRoomTemperature(tmpt);
        }

        private static bool IsMaxMinIlParam(string exParamName)
        {
            if (string.IsNullOrEmpty(exParamName))
                return false;
            string key = exParamName.Split('@')[0].ToUpper();
            return key == "MAXIL" || key == "MINIL";
        }

        private bool TryGetRoomTempMaxMinIlFailure(out string message)
        {
            message = "";
            if (!IsRoomTemperature(curTestTmpt))
                return false;

            for (int p = 0; p < allProductControl.Count; p++)
            {
                List<MESTestInfo> infos = allProductControl[p].GetAllTestInfo();
                string sn = allProductControl[p].ProductSN;
                for (int i = 0; i < infos.Count; i++)
                {
                    MESTestInfo info = infos[i];
                    if (Math.Abs(info.Temperature - curTestTmpt) > 0.001)
                        continue;
                    if (!info.Tested || info.Pass)
                        continue;
                    if (!IsMaxMinIlParam(info.ExParamName))
                        continue;
                    string[] splits = info.PortNameForUser.Split('_');
                    if (splits.Length != 1)
                        continue;
                    if (info.PortNameForUser.Equals("Frequency Range", StringComparison.OrdinalIgnoreCase))
                        continue;

                    message = string.Format(
                        "SN:{0}\r\n端口:{1}\r\n参数:{2}\r\n实测:{3:F3}\r\n限值:[{4}, {5}]",
                        sn, info.PortNameForUser, info.ExParamName, info.CurValue, info.Criterion, info.Criterion1);
                    return true;
                }
            }
            return false;
        }

        private void AbortBatchTest(string detailMessage)
        {
            batchTestAborted = true;
            batchSnQueue = null;
            string prompt = "产品批次有问题：常温测试 MAXIL/MINIL 超限，已终止整个测试。";
            if (!string.IsNullOrEmpty(detailMessage))
                prompt += "\r\n\r\n" + detailMessage;
            CommonFunction.WriteLog(prompt);
            RealtimeMsg(prompt, StatusType.Error);
            ErrorBox(prompt);
            UIControl.IsScanEnable = true;
            UIControl.IsSaveEnable = true;
            UpdateResIcon();
            UpdateProductStatuses();
        }

        private bool TryAbortBatchForRoomTempIl()
        {
            if (!IsMultiSnSinglePortBatch() || batchTestAborted)
                return false;
            if (!IsRoomTemperature(curTestTmpt))
                return false;
            string msg;
            if (!TryGetRoomTempMaxMinIlFailure(out msg))
                return false;
            AbortBatchTest(msg);
            return true;
        }

        private bool IsBatchTestAbortedBlocked()
        {
            if (!batchTestAborted)
                return false;
            WarningBox("产品批次测试已终止（常温 MAXIL/MINIL 超限）。请清空列表或重新点击「一键测试」后再测。");
            return true;
        }

        private static int TryReadChamberTemperature(IUDLTCC tcc, out double actual, ref string errMsg)
        {
            actual = 0;
            if (tcc == null)
            {
                errMsg = "循环箱未配置或未连接。";
                return 1;
            }
            int res = tcc.GetCurrentTemp(out actual, ref errMsg);
            if (res != 0)
            {
                Thread.Sleep(100);
                res = tcc.GetCurrentTemp(out actual, ref errMsg);
            }
            return res;
        }

        private bool TryValidateChamberTemperature(double requiredTmpt, out string message)
        {
            message = "";
            string errMsg = "";
            IUDLTCC tccCtrl = null;
            DeviceControl.GetUDLTCCByGUID(TCC_GUID, ref tccCtrl, ref errMsg);
            if (tccCtrl == null)
            {
                message = string.IsNullOrEmpty(errMsg)
                    ? "循环箱未配置或未连接，无法校验温度。"
                    : "循环箱未配置或未连接：" + errMsg;
                return false;
            }
            double actual;
            if (TryReadChamberTemperature(tccCtrl, out actual, ref errMsg) != 0)
            {
                message = string.Format("读取循环箱温度失败:{0}", errMsg);
                return false;
            }
            if (Math.Abs(actual - requiredTmpt) > TccTempToleranceCelsius)
            {
                message = string.Format(
                    "循环箱温度不符合模板要求，不能测试。\r\n模板要求:{0:F1}°C\r\n当前实测:{1:F1}°C\r\n允许偏差:±{2:F1}°C",
                    requiredTmpt, actual, TccTempToleranceCelsius);
                return false;
            }
            return true;
        }

        private bool EnsureChamberReadyForTest(double requiredTmpt, bool restoreOnekeyUiOnFail = false)
        {
            if (TasRuntimeConfig.IsTccChamberCheckDisabled())
            {
                RealtimeMsg("已跳过循环箱温度校验（set\\DisableTccChamberCheck.txt）");
                return true;
            }

            string message;
            if (TryValidateChamberTemperature(requiredTmpt, out message))
            {
                RealtimeMsg(string.Format("循环箱温度校验通过，模板要求 {0:F1}°C", requiredTmpt));
                return true;
            }
            RealtimeMsg(message, StatusType.Error);
            ErrorBox(message);
            if (restoreOnekeyUiOnFail)
            {
                RealtimeMsg("一键测试结束");
                UIControl.IsReferenceEnable = true;
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
            }
            return false;
        }

        /// <summary>
        /// 扫描结束后处理
        /// </summary>
        /// <param name="scanInfo">扫描类型等信息</param>
        private void ScanFinish(ScanDetail scanInfo)
        {
            double[][][] scanRes = new double[scanInfo.Ports.Count][][];
            if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
            {
                int dataIndex = CurProductIndex * portAndNameDic.Count + scanInfo.Ports[0] - 1;
                scanRes[0] = portPDLRef[dataIndex];
            }
            else if (scanInfo.ScanType == SCANTYPE.TestWithPDL|| scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey)
            {
                for (int i = 0; i < scanInfo.Ports.Count; i++)
                {
                    int dataIndex = CurProductIndex * portAndNameDic.Count + scanInfo.Ports[i] - 1;
                    scanRes[i] = portResData[dataIndex];
                }
                //是否测试状态更新
                if(scanErrorMsg.Length==0)
                {
                    foreach (PortAssist assist in portAssistant)
                    {
                        foreach (int scanPort in scanInfo.Ports)
                        {
                            if (scanPort == assist.PortIndex&&CurProductIndex==assist.ProductIndex-1
                                &&curTestTmpt== assist.TestTmpt)
                            {                               
                                assist.IsTested = true;
                            }
                        }
                    }
                }
                curveShow.ClearAllCurve();
            }
            
            //扫描曲线显示
            for (int i = 0; i < scanInfo.Ports.Count; i++)
            {
                if (scanRes[i] != null && scanRes[i][scanRes[i].Length - 1] != null && scanRes[i][1] != null)
                {
                    string seriesName = "";
                    foreach (PortAssist assist in portAssistant)
                    {
                        if (assist.PortIndex == scanInfo.Ports[i])
                        {
                            seriesName = assist.Name;
                            if (CurProductIndex == assist.ProductIndex - 1
                                && curTestTmpt == assist.TestTmpt
                                && (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithPDLOnekey))
                            {
                                string errMsg = "";
                                string snPath = allProductControl[CurProductIndex].GetSNDir(savePathBase, ref scanErrorMsg);

                                string fileName = allProductControl[CurProductIndex].ProductSN + "_IL_SCAN_" + assist.Name + "_" + mainInfo.TestProcess + "_" + assist.TmptID + ".csv";
                                string strLocalPath = Environment.CurrentDirectory + "\\rawdata\\" + fileName;
                                if (File.Exists(strLocalPath))
                                    File.Delete(strLocalPath);

                                assist.RawdataPath = snPath + "\\" + allProductControl[CurProductIndex].ProductSN + "_IL_SCAN_" + assist.Name + "_"+mainInfo.TestProcess + "_" + assist.TmptID + ".csv";
                                InterleaverScanResult.WriteFusionData(strLocalPath, scanRes[i], mainInfo.UserID, mainInfo.StationID, assist.TestTmpt.ToString(), ref errMsg);
                                AddStrToList(ref savePathList, fileName);

                                for (int n = 0; n < allProductControl[CurProductIndex].AllTestInfo.Count; n++)
                                {
                                    if (PortNameMatchesChannel(allProductControl[CurProductIndex].AllTestInfo[n].PortNameForUser, assist.Name) && assist.RawdataPath != ""
                                        && assist.TestTmpt== allProductControl[CurProductIndex].AllTestInfo[n].Temperature&& allProductControl[CurProductIndex].AllTestInfo[n].Tested)
                                    {
                                        allProductControl[CurProductIndex].AllTestInfo[n].Filename = assist.RawdataPath;
                                    }
                                }
                                /*int dataLen = scanRes[i].Length;
                                int rawdataLen = scanRes[i][1].Length;
                                string maxRawdata = "VOLT-1DB,";
                                string minRawdata = "VOLT-2DB,";
                                string aveRawdata = "VOLT-3DB,";
                                string pdlRawdata = "VOLT-4DB,";
                                for (int k = 0; k < rawdataLen; k++)
                                {
                                    maxRawdata += string.Format("{0:F3}:{1:F3},", scanRes[i][dataLen - 1][k], scanRes[i][3][k]);
                                    minRawdata += string.Format("{0:F3}:{1:F3},", scanRes[i][dataLen - 1][k], scanRes[i][4][k]);
                                    aveRawdata += string.Format("{0:F3}:{1:F3},", scanRes[i][dataLen - 1][k], scanRes[i][1][k]);
                                    if (k != rawdataLen - 1)
                                    {                                  
                                        pdlRawdata += string.Format("{0:F3}:{1:F3},", scanRes[i][dataLen - 1][k], scanRes[i][2][k]);
                                    }
                                    else
                                    {
                                        pdlRawdata += string.Format("{0:F3}:{1:F3}", scanRes[i][dataLen - 1][k], scanRes[i][2][k]);
                                    }
                                }
                                if(rawdataLen>0)
                                {
                                    assist.Rawdata = maxRawdata + minRawdata + aveRawdata + pdlRawdata;
                                }*/
                                break;
                            }
                        }
                    }
                    curveShow.UpdateCurveShow(seriesName, scanRes[i][scanRes[i].Length - 1].ToList(), scanRes[i][1].ToList());
                }
            }

            if (scanInfo.ScanType==SCANTYPE.RefWithPDL)
            {           
                if (scanErrorMsg.Length == 0)
                    portAssistant[referenceIndex].IsRef = true;
                else
                    portAssistant[referenceIndex].IsRef = false;
                UpdateReferenceStatus(CurProductIndex, portAssistant[referenceIndex]);

                referenceIndex++;
                ScanRef();
            }
            if(scanInfo.ScanType==SCANTYPE.TestWithPDL)
            {
                if (scanErrorMsg.Length == 0)
                {
                    ParamItemUpdate(CurProductIndex);
                    if (TryAbortBatchForRoomTempIl())
                        return;
                }
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;              
            }

            if(scanInfo.ScanType==SCANTYPE.TestWithPDLOnekey)
            {
                if (scanErrorMsg.Length == 0)
                {
                    ParamItemUpdate(CurProductIndex);
                    if (TryAbortBatchForRoomTempIl())
                        return;
                    OnekeyScan();
                }
                else
                {
                    UIControl.IsScanEnable = true;
                }
            }  
        }

        private void UpdateReferenceStatus(int productID,PortAssist assist)
        {
            List<MESTestInfo> showInfos = testShowControl[productID].GetAllTestInfo();
            for (int j = 0; j < showInfos.Count; j++)
            {
                if (PortNameMatchesChannel(showInfos[j].PortNameForUser, assist.Name))
                {
                    testShowControl[productID].UpdateScanRefStatus(j, assist.IsRef);
                    UpdateItem(testShowControl[productID].GetAllTestInfo()[j], productID, j);
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
        /// 重连服务器
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功， 2--失败</returns>
        private int ReconnectServer(ref string errMsg)
        {
            IInterleaverScan scan = null;
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                if (scan.Reconnect(ref errMsg))
                {
                    return 0;
                }
            }
            return 2;
        }


        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetOpenTemplateComplete(bool isComplete)
        {
            isOpenTemplateComplete = isComplete;
        }

        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool GetOpenTemplateComplete()
        {
            return isOpenTemplateComplete;
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


        private void btnClearBakeSN_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("正在测试，是否要清空列表！", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                AllProducts.Clear();
                allProductControl.Clear();
                testShowControl.Clear();
                SetOpenTemplateComplete(false);
                ClearListData();
                TemptRemainTime.Text = "00:00:00";
                UIControl.SN = "";
            }
        }


        /// <summary>
        /// 当前测试温度
        /// </summary>
        private double curTestTmpt = -1;

       
        private int CurProductIndex = 0;
        private void btnOnekeyScan_Click(object sender, RoutedEventArgs e)
        {
            if(portAssistant.Count==0|| allProductControl.Count==0)
            {
                WarningBox("请检查模板是否出错!");
                return;
            }
            batchTestAborted = false;
            /*foreach(PortAssist assist in portAssistant)
            {                
                assist.IsTested = false;
            }
            curTestTmpt = -300;*/
            OnekeyScan();
        }

        
        private void OnekeyScan()
        {
            if (batchTestAborted)
            {
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                return;
            }
            //获取未测试的端口
            scanDetailInfo.Ports.Clear();
            List<int> scanPorts = new List<int>();
            int i = 0;
            double scanTmpt = -300;
            if (curTestTmpt == -300)
            {
                scanTmpt = portAssistant[0].TestTmpt;
                if (TasRuntimeConfig.IsRtOnlyTestMode())
                {
                    scanTmpt = -300;
                    for (int j = 0; j < portAssistant.Count; j++)
                    {
                        if (IsRoomTemperature(portAssistant[j].TestTmpt))
                        {
                            scanTmpt = portAssistant[j].TestTmpt;
                            break;
                        }
                    }
                    if (scanTmpt < -299)
                    {
                        WarningBox("RtOnlyTest：模板中未找到常温测试项。");
                        UIControl.IsScanEnable = true;
                        UIControl.IsSaveEnable = true;
                        return;
                    }
                }
            }
            else
            {
                scanTmpt = curTestTmpt;
            }
            //查找当前一键测试该测试哪项
            for (i = 0; i < portAssistant.Count; i++)
            {
                if (!IsTestTemperatureAllowed(portAssistant[i].TestTmpt))
                    continue;
                if ((!portAssistant[i].IsTested) && scanTmpt==portAssistant[i].TestTmpt)
                {
                    break;
                }
            }
            //一个温度测试完成，查找下一个测试的温度
            if(i== portAssistant.Count)
            {
                for (i = 0; i < portAssistant.Count; i++)
                {
                    if (!IsTestTemperatureAllowed(portAssistant[i].TestTmpt))
                        continue;
                    if (!portAssistant[i].IsTested)
                    {
                        scanTmpt = portAssistant[i].TestTmpt;
                        break;
                    }
                }
            }

            if (i == portAssistant.Count)
            {
                bool rtOnly = TasRuntimeConfig.IsRtOnlyTestMode();
                bool anyRtPending = false;
                bool anyNonRtUntested = false;
                for (int j = 0; j < portAssistant.Count; j++)
                {
                    if (portAssistant[j].IsTested)
                        continue;
                    if (IsRoomTemperature(portAssistant[j].TestTmpt))
                        anyRtPending = true;
                    else
                        anyNonRtUntested = true;
                }
                string completeMsg = "一键测试完成！";
                if (rtOnly && !anyRtPending && anyNonRtUntested)
                    completeMsg = "RtOnlyTest：无常温待测项，一键测试结束。（低温/高温项已跳过）";
                MessageBox.Show(completeMsg);
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                return;
            }
            
            int productID = portAssistant[i].ProductIndex;
            int scanIndex = portAssistant[i].ScanIndex;
            if(scanIndex==0)
            {
                MessageBox.Show("一键测试扫描出错，扫描序列号不能为0！");
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                return;
            }
            //获取同时扫描的端口号。
            //进光端一样，则可以一起扫描，比如in-to,in-te,in-moni同时扫描
            // Demux Even/Odd 出光路径不同，与归零一致：每次只测当前排队口，不按 ScanIndex 合并。
            string testPortName = portAssistant[i].Port;
            scanDetailInfo.Ports.Clear();
            if (IsDemuxDualPortTemplate())
            {
                scanDetailInfo.Ports.Add(portAssistant[i].PortIndex);
            }
            else
            {
                foreach (PortAssist assist in portAssistant)
                {
                    if (productID == assist.ProductIndex && scanIndex == assist.ScanIndex
                        && scanTmpt == assist.TestTmpt)
                    {
                        testPortName = assist.Port;
                        scanDetailInfo.Ports.Add(assist.PortIndex);
                    }
                }
            }
            scanDetailInfo.ProductIndex = productID;
            string errMsg = "";
            if (!IsScanRef(scanDetailInfo.Ports, ref errMsg))
            {
                WarningBox(errMsg);
                return;
            }
            if (!TrySetSwitchBeforeScan(productID, testPortName))
            {
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                return;
            }

            scanDetailInfo.ScanType = SCANTYPE.TestWithPDLOnekey;
            UIControl.IsScanEnable = false;

            //选中当前测试行
            List<MESTestInfo> shows = testShowControl[productID-1].GetAllTestInfo();
            IndexMap nextSeleted = new IndexMap();
            nextSeleted.ProductIndex = productID-1;
            for (int k=0;k< shows.Count;k++)
            {
                if(portAssistant[i].Name==shows[k].PortNameForUser&&
                    portAssistant[i].TestTmpt==shows[k].Temperature)
                {
                    nextSeleted.ParamIndex.Add(k);
                    break;
                }
            }
            UpdateItem(testShowControl[productID-1].GetAllTestInfo()[0], productID-1, 0, nextSeleted);

            if (!EnsureChamberReadyForTest(scanTmpt, restoreOnekeyUiOnFail: true))
                return;

            curTestTmpt = scanTmpt;
            DoScanOnBK();
        }

        public void GetUDLMessage(ref string msg, ref bool isSuccess)
        {
            isSuccess = GetMessage(ref msg);
        }

        public bool IsUDLSuccess(ref string msg)
        {
            object[] param = new object[2];
            bool res = false;
            param[0] = msg;
            param[1] = res;
            this.Dispatcher.Invoke(new GetUDLMessageDelegate(GetUDLMessage), param);
            res = Convert.ToBoolean(param[1]);
            msg = Convert.ToString(param[0]);
            return res;
        }

        private bool IsScanRef(List<int> scanPorts, ref string errMsg)
        {
            bool bAllRef = true;
            for (int i = 0; i < portAssistant.Count; i++)
            {
                if (portAssistant[0].TestTmpt != portAssistant[i].TestTmpt)
                    continue;
                if (!portAssistant[i].IsRef)
                {
                    bAllRef = false;
                    errMsg = "未归零，请先归零！";
                    break;
                }
            }
            return bAllRef;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            refTimeCheckBK.CancelAsync();
        }

        private void btnSingleScan_Click(object sender, RoutedEventArgs e)
        {
            if (IsBatchTestAbortedBlocked())
                return;
            RealtimeMsg(string.Format("开始singleScan"));
            if (selectItem!=null&& selectItem.ParamIndex.Count>0)
            {

                int selectIndex = selectItem.ParamIndex[0];
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();
                if (selectIndex >= showInfos.Count)
                    return;
                MESTestInfo selectTestItem = showInfos[selectIndex];
              
                scanDetailInfo.Ports.Clear();
                PortAssist switchAssist = null;
                foreach (PortAssist assist in portAssistant)
                {
                    if ((selectItem.ProductIndex + 1) == assist.ProductIndex
                        && PortNameMatchesChannel(selectTestItem.PortNameForUser, assist.Name)
                        && selectTestItem.Temperature == assist.TestTmpt)
                    {
                        switchAssist = assist;
                        break;
                    }
                }
                if (switchAssist == null)
                {
                    WarningBox("未找到与列表选中项对应的光路端口，请确认模板已打开。");
                    return;
                }

                string testPortName = switchAssist.Port;
                if (IsDemuxDualPortTemplate())
                {
                    scanDetailInfo.Ports.Add(switchAssist.PortIndex);
                }
                else
                {
                    int scanIndex = switchAssist.ScanIndex;
                    foreach (PortAssist assist in portAssistant)
                    {
                        if ((selectItem.ProductIndex + 1) == assist.ProductIndex && scanIndex == assist.ScanIndex
                            && selectTestItem.Temperature == assist.TestTmpt)
                        {
                            scanDetailInfo.Ports.Add(assist.PortIndex);
                        }
                    }
                }

                string errMsg = "";
                if (!IsScanRef(scanDetailInfo.Ports, ref errMsg))
                {
                    WarningBox(errMsg);
                    return;
                }

                double testTmpt = selectTestItem.Temperature;
                RealtimeMsg(string.Format("当前测试温度:{0}", testTmpt));
                if (!IsTestTemperatureAllowed(testTmpt))
                {
                    WarningBox(string.Format(
                        "RtOnlyTest 模式仅支持常温测试（约 20~30°C）。\r\n当前项温度:{0:F1}°C\r\n请删除 set\\RtOnlyTest.txt 或改选常温行。",
                        testTmpt));
                    return;
                }
                if (!EnsureChamberReadyForTest(testTmpt))
                    return;

                scanDetailInfo.ScanType = SCANTYPE.TestWithPDL;
                scanDetailInfo.ProductIndex = selectItem.ProductIndex + 1;
                UIControl.IsScanEnable = false;
                if (!TrySetSwitchBeforeScan(scanDetailInfo.ProductIndex, testPortName))
                {
                    UIControl.IsScanEnable = true;
                    return;
                }
                curTestTmpt = testTmpt;
                DoScanOnBK();
            }
            //ReleaseCom(deviceEngine);
            //ReleaseCom(tccCtrl);
        }

        private void ReleaseCom(object obj)
        {
            int result = 0;
            do
            {
                if (obj != null)
                    result = Marshal.ReleaseComObject(obj);
            } while (result > 0);
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

        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            bool isSaveError = false;
            for(int i=0;i<allProductControl.Count;i++)
            {
                List<AMTSRawdata> rawdatas = new List<AMTSRawdata>();
                for(int j=0;j<portAssistant.Count;j++)
                {
                    if(portAssistant[j].ProductIndex==(i+1))
                    {
                        AMTSRawdata data = new AMTSRawdata();
                        data.PortName = portAssistant[j].Name;
                        data.Temperature = portAssistant[j].TestTmpt;
                        data.Rawdata = portAssistant[j].Rawdata;
                        rawdatas.Add(data);
                    }
                }
                string snPath = allProductControl[i].GetSNDir(savePathBase, ref scanErrorMsg);

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
                allProductControl[i].SaveSoftwareInfo("SOFTWARE2219_ITL_FTS", "V1.1.0.1", "Jinfang Ruan", "2024-3-6");
                if (!allProductControl[i].UploadTestData(savePath,out errMsg))
                {
                    ErrorBox(errMsg);
                    isSaveError = true;
                }
            }
            if(!isSaveError)
            {
                AllProducts.Clear();
                TemptRemainTime.Text = "00:00:00";
                allProductControl.Clear();
                testShowControl.Clear();
                SetOpenTemplateComplete(false);
                ClearListData();
                UIControl.SN = "";
                UIControl.IsSaveEnable = false;
                UIControl.IsScanEnable = false;
                templateName = "";
                ShowTmpltPath();
                savePathList.Clear();

                IUDLTCC tccCtrl = null;
                DeviceControl.GetUDLTCCByGUID(TCC_GUID, ref tccCtrl, ref errMsg);
                if (tccCtrl != null)               
                {
                    tccCtrl.SetTempSetpoint(25, ref errMsg);
                }
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void OperateInteleaverFinalTest_PreviewKeyDown(object sender, KeyEventArgs e)
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

    public enum ProductTestStatus
    {
        NotStarted,
        Ok,
        Error
    }

    public class TestProductInfo : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush BrushNotStarted = FreezeBrush(176, 176, 176);
        private static readonly SolidColorBrush BrushOk = FreezeBrush(50, 205, 50);
        private static readonly SolidColorBrush BrushError = FreezeBrush(255, 0, 0);

        private static SolidColorBrush FreezeBrush(byte r, byte g, byte b)
        {
            var br = new SolidColorBrush(Color.FromRgb(r, g, b));
            br.Freeze();
            return br;
        }

        public static Brush BrushFor(ProductTestStatus status)
        {
            switch (status)
            {
                case ProductTestStatus.Ok:
                    return BrushOk;
                case ProductTestStatus.Error:
                    return BrushError;
                default:
                    return BrushNotStarted;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SN { get; set; }
        public int Index { get; set; }

        private bool hasScanError;
        public bool HasScanError
        {
            get { return hasScanError; }
            set
            {
                if (hasScanError == value)
                {
                    return;
                }
                hasScanError = value;
                NotifyPropertyChanged();
            }
        }

        private ProductTestStatus status = ProductTestStatus.NotStarted;
        public ProductTestStatus Status
        {
            get { return status; }
            set
            {
                if (status == value)
                {
                    return;
                }
                status = value;
                NotifyPropertyChanged();
            }
        }

        private Brush statusBrush = BrushNotStarted;
        public Brush StatusBrush
        {
            get { return statusBrush; }
            set
            {
                if (ReferenceEquals(statusBrush, value))
                {
                    return;
                }
                statusBrush = value;
                NotifyPropertyChanged();
            }
        }

        public TestProductInfo()
        {
            status = ProductTestStatus.NotStarted;
            statusBrush = BrushNotStarted;
        }
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

        /// <summary>
        /// 1×16 光开关输出通道号 (1-16)
        /// </summary>
        public int SwitchChannel { get; set; }

        public PortAssist()
        {
            Name = "";
            Port = "";
            SwitchChannel = -1;
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
