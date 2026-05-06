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
using System.Threading;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.UIList;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;
using System.Windows.Interop;

namespace UIOperateInterleaver
{
    /// <summary>
    /// Interaction logic for OperateInterleaver.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateInterleaver")]
    public partial class OperateInterleaver : UserControl
    {
        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsUrl = "http://172.18.1.101/amts/";

        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsSaveUrl = "http://172.18.1.101/amts/Atd_UploadMessage.asmx";

        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 所有产品测试信息
        /// </summary>
        private List<MESControl> allProductControl;

        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

        /// <summary>
        /// 测试但未保存
        /// </summary>
        private bool isTestedUnSave = false;

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

        /// <summary>
        /// 功率计实时显示后台线程
        /// </summary>
        private BackgroundWorker powermeterRealtimeBK;

        /// <summary>
        /// 归零时间确认后台线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 功率计实时显示后台线程
        /// </summary>
        private BackgroundWorker noPDLContinueScanBK;

        /// <summary>
        /// 1830归零值
        /// </summary>
        private double[] port1830Ref = null;

        /// <summary>
        /// 利用四个偏振态下数据计算所得四个端口结果数据double[6][] 0:WL 1:ave 2:PDL 3:MaxIL 4:MinIL 5:Fre
        /// </summary>
        private List<double[][]> portResData = null;

        /// <summary>
        /// 四个偏振态下数据double[3][] 0:WL 1:IL 2:fre
        /// </summary>
        private List<double[][]> pdlRawData = null;

        /// <summary>
        /// 不带PDL归零数据数据double[3][]  0:WL 1:IL 2:fre
        /// </summary>
        private List<double[][]> portNoPDLRef = null;

        /// <summary>
        /// 带PDL归零数据数据double[7][]  0:WL 1:ave 2:PDL1 IL 3:PDL2 IL 4:PDL3 IL 5:PDL4 IL 6:fre
        /// </summary>
        private List<double[][]> portPDLRef = null;

        /// <summary>
        /// 扫描、归零文件路径
        /// </summary>
        private string refWithPDLFile = "\\reference\\referenceWithPDLPort";
        private string refWithNoPDLFile = "\\reference\\referenceWithNoPDLPort";
        private string scanWithPDLFile = "\\rawdata\\ScanWithPDLPort";
        private string scanWithNoPDLFile = "\\rawdata\\ScanWithNoPDLPort";
        private string ref1830File = "\\reference\\1830Ref.csv";

        /// <summary>
        /// 曲线显示对象
        /// </summary>
        private InterleaverCurve curveShow = null;

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
        private InterleaverParamCal paramCal = null;


        /// <summary>
        /// 扫描错误信息
        /// </summary>
        private string scanErrorMsg = "";

        /// <summary>
        /// 扫描数据记录，归零时port传实际端口，扫描时1、2一起扫描，port赋值1，3、4一起扫描，port赋值3
        /// </summary>
        private ScanDetail scanDetailInfo = new ScanDetail();


        /// <summary>
        /// 扫描是否结束
        /// </summary>
        private bool isScanFinished = true;

        /// <summary>
        /// 存放特殊显示中的MinISO值
        /// </summary>
        private Dictionary<string, double> portMinISODic = new Dictionary<string, double>();

        /// <summary>
        /// 用于显示的模板处理类
        /// </summary>
        private MESControl showControl = null;

        /// <summary>
        /// 需要在列表中显示出来的参数
        /// </summary>
        private List<string> showParam = new List<string>();

        /// <summary>
        /// 需要更新的参数项在所有测试下中的index，减少后续处理循环需要
        /// </summary>
        private List<int> updateParamIndex = new List<int>();

        /// <summary>
        /// 功率计实时功率值
        /// </summary>
        private List<double> realtimePowers = new List<double>();

        /// <summary>
        /// 模板获取到的最小扫描频率
        /// </summary>
        private double minScanFre = 2000000.0;

        /// <summary>
        /// 模板获取到的最大扫描频率
        /// </summary>
        private double maxScanFre = -2000000.0;

        /// <summary>
        /// 是否选择12端口
        /// </summary>
        private bool isPort12Select = true;

        /// <summary>
        /// 端口数量
        /// </summary>
        private const int cstPortCount = 4;

        /// <summary>
        /// PDL数量
        /// </summary>
        private const int cstPDLCount = 4;

        /// <summary>
        /// 模板是否正确打开，并完成
        /// </summary>
        private bool isOpenTemplateComplete = false;

        /// <summary>
        /// 功率计1 对应端口ISO 有效带宽内所有通道的结果，用于界面显示 0--结果 1--波长
        /// </summary>
        private List<List<double>> pm1ISOResult = null;

        /// <summary>
        /// 功率计2 对应端口ISO 有效带宽内所有通道的结果，用于界面显示 0--结果 1--波长
        /// </summary>
        private List<List<double>> pm2ISOResult = null;

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
        /// 测试失败图片加载存储对象
        /// </summary>
        private BitmapImage failBitmapImage = null;

        private const int rerefHours = 6;

        /// <summary>
        /// rawdata保存数据路径
        /// </summary>
        private string rawdataNetPath = "\\\\zh-mfs-srv.oplink.com.cn\\share\\WS8DataEtalon\\Interleaver\\关单前数据\\50G Interleaver\\Alignment_Data\\";
        /// <summary>
        /// 四个通道归零时间
        /// </summary>
        private DateTime[] refTimes = new DateTime[cstPortCount];

        private Ellipse[] ref1830LEDs = new Ellipse[cstPortCount];

        private DateTime[] ref1830Times = new DateTime[cstPortCount];

        private Ellipse[] refSysLEDs = new Ellipse[cstPortCount];

        /// <summary>
        /// 最老的归零时间，用于4小时归零倒计时
        /// </summary>
        private DateTime oldestRefTime = new DateTime();

        private string convertAlgorithm = ConvertAlgorithm.Mueller.GetAdditional();

        /// <summary>
        /// ISO曲线对应的提取数据的参数名称
        /// </summary>
        private string curveAdjParamName = "";

        /// <summary>
        /// 模板中包含端口，归零数据判断
        /// </summary>
        private List<int> templatePorts = new List<int>();

        /// <summary>
        /// 几个线程一起计算结果
        /// </summary>
        private int splitCalCount = 2;

        /// <summary>
        /// 几个线程计算是否结束
        /// </summary>
        private bool[] isSplitCalFinished = new bool[2];

        /// <summary>
        /// 记录计算的过程数据，用于port参数计算
        /// </summary>
        private List<SamePortParamData> curPortRecords = new List<SamePortParamData>();

        /// <summary>
        /// 扫描是否结束
        /// </summary>
        private bool isStopScan = false;

        /// <summary>
        /// 是否开始照光
        /// </summary>
        private bool isBeginLight = false;

        /// <summary>
        /// 是否PDL扫描
        /// </summary>
        private bool isBeginPDLScan = false;

        //照光文件路径，在设置当前所在路径后，在前面加上当前文件夹
        private string lightDataDir = "\\lightdata\\";

        /// <summary>
        /// 注册快捷集合
        /// </summary>
        readonly Dictionary<string, short> hotKeyDic = new Dictionary<string, short>();

        /// <summary>
        /// 特殊显示shift值和中心频率对应保存dic
        /// </summary>
        private Dictionary<double, double> specialShiftsDic = new Dictionary<double, double>();

        /// <summary>
        /// 1、2最小ISO值，用于显示
        /// </summary>
        //private double pm1MinISOValue = CommonFunction.GetDefaultValue();
        //private double pm2MinISOValue = CommonFunction.GetDefaultValue();

        /// <summary>
        /// 扫描功率计个数
        /// </summary>
        private int scanPowermeterCount = 2;


        /// <summary>
        /// 特殊频率shift监控，PM1需要监控
        /// </summary>
        private List<double> PM1SpecialFre = new List<double>();

        /// <summary>
        /// 特殊频率shift监控，PM2需要监控
        /// </summary>
        private List<double> PM2SpecialFre = new List<double>();

        /// <summary>
        /// 为了界面显示处理方便，界面上shift显示相关按钮
        /// </summary>
        private List<Label> shiftLabels = new List<Label>();

        private List<TextBox> shitfValueTextBox = new List<TextBox>();

        private Dictionary<string, string> portRawdatas = new Dictionary<string, string>();

        private Dictionary<string, string> portAndNameDic = new Dictionary<string, string>();

        private List<PortAssist> portAssistant = new List<PortAssist>();

        /// <summary>
        /// 功率计和port对应关系
        /// </summary>
        private Dictionary<string, int> portAndPMDic = new Dictionary<string, int>();
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

        public OperateInterleaver()
        {
            InitializeComponent();

            allProductControl = new List<MESControl>();
            uiVariable.IsEnable = true;
            uiVariable.IsSaveEnable = false;
            txtBoxSN.DataContext = uiVariable;

            btnOpenTemplate.DataContext = uiVariable;
            btnSaveToAMTS.DataContext = uiVariable;
            btnUVata.DataContext = uiVariable;

            btnScanRef.DataContext = uiVariable;
            btnPDLScan.DataContext = uiVariable;
            btnAdjustScan.DataContext = uiVariable;
            txtSpec.DataContext = uiVariable;
            txtPN.DataContext = uiVariable;
            chkPort12.DataContext = uiVariable;
            chkPort34.DataContext = uiVariable;
            btnStopScan.DataContext = uiVariable;
            btnUVata.DataContext = uiVariable;
            uiVariable.IsStopScanVisible = Visibility.Hidden;

            //btnSingleScan.DataContext = uiVariable;

            powermeterRealtimeBK = new BackgroundWorker();
            powermeterRealtimeBK.DoWork += PowermeterRealtime_DoWork;
            powermeterRealtimeBK.ProgressChanged += PowermeterRealtimeShow_Progress;
            powermeterRealtimeBK.WorkerSupportsCancellation = true;
            powermeterRealtimeBK.WorkerReportsProgress = true;

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;


            noPDLContinueScanBK = new BackgroundWorker();
            noPDLContinueScanBK.DoWork += NoPDLContinueScanBK_DoWork;
            noPDLContinueScanBK.RunWorkerCompleted += NoPDLContinueScanBK_RunWorkerCompleted;
            showParam.Add("MAXIL");
            showParam.Add("PDL");
            showParam.Add("MAXSHIFT");
            showParam.Add("MINSHIFT");
            showParam.Add("MAXISO");
            showParam.Add("MINISO");
            showParam.Add("UNI");
            showParam.Add("FSR");
            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            amtsSaveUrl = xmlSet.readStringData(CommonFunction.GetSaveWebservicSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");

            portNoPDLRef = new List<double[][]>(cstPortCount);
            portPDLRef = new List<double[][]>(cstPortCount);
            portResData = new List<double[][]>(cstPortCount);
            pdlRawData = new List<double[][]>(cstPDLCount);
            port1830Ref = new double[cstPortCount];
            for (int i = 0; i < cstPortCount; i++)
            {
                portNoPDLRef.Add(new double[3][]);
                portPDLRef.Add(new double[3][]);
                portResData.Add(new double[6][]);
                port1830Ref[i] = CommonFunction.GetDefaultValue();
            }

            for (int i = 0; i < cstPDLCount; i++)
            {
                pdlRawData.Add(new double[3][]);
            }

            pm1ISOResult = new List<List<double>>();
            pm1ISOResult.Add(new List<double>());
            pm1ISOResult.Add(new List<double>());
            pm2ISOResult = new List<List<double>>();
            pm2ISOResult.Add(new List<double>());
            pm2ISOResult.Add(new List<double>());

            portAndPMDic.Add("1", 1);
            portAndPMDic.Add("2", 2);
            portAndPMDic.Add("3", 1);
            portAndPMDic.Add("4", 2);

            ref1830LEDs[0] = ref18301;
            ref1830LEDs[1] = ref18302;
            ref1830LEDs[2] = ref18303;
            ref1830LEDs[3] = ref18304;

            refSysLEDs[0] = refSys1;
            refSysLEDs[1] = refSys2;
            refSysLEDs[2] = refSys3;
            refSysLEDs[3] = refSys4;

            shiftLabels.Add(PM1LowFre);
            shiftLabels.Add(PM1MidFre);
            shiftLabels.Add(PM1HighFre);
            shiftLabels.Add(PM2LowFre);
            shiftLabels.Add(PM2MidFre);
            shiftLabels.Add(PM2HighFre);

            shitfValueTextBox.Add(PM1LowFreShift);
            shitfValueTextBox.Add(PM1MidFreShift);
            shitfValueTextBox.Add(PM1HighFreShift);
            shitfValueTextBox.Add(PM2LowFreShift);
            shitfValueTextBox.Add(PM2MidFreShift);
            shitfValueTextBox.Add(PM2HighFreShift);
            uiVariable.SN = "";
            txtBoxSN.Focus();

        }

        private void NoPDLContinueScanBK_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SCANTYPE scanType = (SCANTYPE)e.Result;
            DoScanOnBK(scanType);
        }

        private void NoPDLContinueScanBK_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(10);
            e.Result = e.Argument;
        }

        private void GetPowers()
        {
            realtimePowers.Clear();
            double pm21830Ref = CommonFunction.GetDefaultValue();
            double pm11830Ref = CommonFunction.GetDefaultValue();
            int curPM1 = GetCurPort(true);
            //先确定归零数据

            pm21830Ref = port1830Ref[curPM1];
            pm11830Ref = port1830Ref[curPM1 - 1];

            /*if (pm21830Ref == CommonFunction.GetDefaultValue() || pm11830Ref == CommonFunction.GetDefaultValue())
                return;*/
            //读功率计的值
            IPowermeter pm1 = null;
            IPowermeter pm2 = null;
            int channel = 0;
            int channel2 = 0;
            string errMsg = "";
            DeviceControl.GetPowermeterByIndex(1, ref channel, ref pm1, ref errMsg);
            DeviceControl.GetPowermeterByIndex(2, ref channel2, ref pm2, ref errMsg);

            if (pm1 != null)
            {
                List<double> powerAvgs = null;
                pm1.ReadPowerAvg(ref errMsg, out powerAvgs, 1, false, channel.ToString());
                if (powerAvgs != null && powerAvgs.Count > 0 && pm11830Ref != CommonFunction.GetDefaultValue())
                {
                    realtimePowers.Add(powerAvgs[0] - pm11830Ref);
                }
            }
            if (pm2 != null)
            {
                List<double> powerAvgs = null;
                pm2.ReadPowerAvg(ref errMsg, out powerAvgs, 1, false, channel2.ToString());
                if (powerAvgs != null && powerAvgs.Count > 0 && pm21830Ref != CommonFunction.GetDefaultValue())
                {
                    realtimePowers.Add(powerAvgs[0] - pm21830Ref);
                }
            }
        }

        private void PowermeterRealtimeShow_Progress(object sender, ProgressChangedEventArgs e)
        {
            if (realtimePowers.Count == 0)
                return;
            List<RealtimePowerInfo> powers = new List<RealtimePowerInfo>();

            RealtimePowerInfo ch1 = new RealtimePowerInfo();
            ch1.Prefix = "";
            ch1.Power = realtimePowers[0].ToString("#0.000") + "dB";
            powers.Add(ch1);

            if (realtimePowers.Count > 1)
            {
                RealtimePowerInfo ch2 = new RealtimePowerInfo();
                ch2.Prefix = "";
                ch2.Power = realtimePowers[1].ToString("#0.000") + "dB";
                powers.Add(ch2);
            }
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventRealtimePowerUpdate>().Publish(powers);
            }

        }

        private void PowermeterRealtime_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!powermeterRealtimeBK.CancellationPending)
            {
                int preTickCount = System.Environment.TickCount;
                int EndTickCount = System.Environment.TickCount;
                while (EndTickCount - preTickCount < 200)
                {
                    EndTickCount = System.Environment.TickCount;
                    Thread.Sleep(50);
                }
                GetPowers();
                powermeterRealtimeBK.ReportProgress(1);
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
            //查看功率计归零数据是否过期
            string prompt1830 = "端口";
            bool bPastDue = false;
            for (int i = 0; i < cstPortCount; i++)
            {
                if (!ref1830Times[i].Equals(defaultTime))
                {
                    TimeSpan refSpan = curTime - ref1830Times[i];
                    //归零数据超过六个小时，删除
                    if (IsRefTimePassdue(refSpan))
                    {
                        port1830Ref[i] = CommonFunction.GetDefaultValue();
                        ref1830Times[i] = new DateTime();
                        prompt1830 += " " + (i + 1).ToString();
                        ref1830LEDs[i].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        bPastDue = true;
                    }
                }
            }
            prompt1830 += "功率计归零数据过期，需重新归零！";
            if (bPastDue)
                WarningBox(prompt1830);


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
                string prompt = "端口";
                for (int i = 0; i < cstPortCount; i++)
                {
                    string pdlRefPath = refWithPDLFile + (i + 1).ToString() + ".csv";
                    string nopdlRefPath = refWithNoPDLFile + (i + 1).ToString() + ".csv";
                    //读取PDL的归零数据
                    //处理归零超过4个小时，删除归零文件，清除内存数据

                    if (!refTimes[i].Equals(defaultTime))
                    {
                        TimeSpan refSpan = curTime - refTimes[i];

                        //归零数据超过四个小时，删除
                        if (IsRefTimePassdue(refSpan))
                        {
                            refTimes[i] = new DateTime();
                            if(File.Exists(pdlRefPath))
                                File.Delete(pdlRefPath);

                            if (File.Exists(nopdlRefPath))
                                File.Delete(nopdlRefPath);
                            //清除内存归零数据 
                            InterleaverScanResult.InitRawdataBuffer(portPDLRef[i]);
                            InterleaverScanResult.InitRawdataBuffer(portNoPDLRef[i]);
                            prompt += " " + (i + 1).ToString();
                            refSysLEDs[i].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        }
                        //四个归零时间中，最老的时间用来做倒计时
                        if (oldestRefTime.Equals(defaultTime) || oldestRefTime.CompareTo(refTimes[i]) > 0)
                        {
                            oldestRefTime = refTimes[i];
                        }
                    }
                }
                //是否要把1830归零数据也删除了？

                prompt += "系统归零数据过期，需重新归零！";
                WarningBox(prompt);
            }


        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            RegisterShotCut();
        }

        /// <summary>
        /// 注册快捷键，钩子热键
        /// </summary>
        private void RegisterShotCut()
        {
            Window parentWindow = Window.GetWindow(this);
            var wpfHwnd = new WindowInteropHelper(parentWindow).Handle;

            var hWndSource = HwndSource.FromHwnd(wpfHwnd);
            //添加处理程序
            if (hWndSource != null) hWndSource.AddHook(MainWindowProc);

            hotKeyDic.Add("Ctrl-B", Win32API.GlobalAddAtom("Ctrl-B"));
            hotKeyDic.Add("Ctrl-Y", Win32API.GlobalAddAtom("Ctrl-Y"));
            hotKeyDic.Add("Ctrl-G", Win32API.GlobalAddAtom("Ctrl-G"));
            hotKeyDic.Add("Ctrl-X", Win32API.GlobalAddAtom("Ctrl-X"));
            hotKeyDic.Add("Ctrl-S", Win32API.GlobalAddAtom("Ctrl-S"));
            hotKeyDic.Add("Ctrl-P", Win32API.GlobalAddAtom("Ctrl-P"));
            hotKeyDic.Add("Ctrl-L", Win32API.GlobalAddAtom("Ctrl-L"));
            hotKeyDic.Add("Ctrl-T", Win32API.GlobalAddAtom("Ctrl-T"));
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-B"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.B);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-Y"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.Y);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-G"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.G);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-X"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.X);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-S"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.S);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-P"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.P);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-L"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.L);
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-T"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.T);
        }

        /// <summary>
        /// 响应快捷键事件
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr MainWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            
            switch (msg)
            {
                case Win32API.WmHotkey:
                    {
                        int sid = wParam.ToInt32();
                        if (sid == hotKeyDic["Ctrl-B"])
                        {
                            if (btnSaveToAMTS.IsEnabled)
                            {
                                btnSaveToAMTS_Click(null, null);
                            }
                        }
                        else if (sid == hotKeyDic["Ctrl-Y"])
                        {
                            if (btnUVata.IsEnabled)
                            {
                                btnUVata_Click(null, null);
                            }
                        }
                        /*else if (sid == hotKeyDic["Ctrl-G"])
                        {
                            if (btn1830Ref.IsEnabled)
                            {
                                btn1830Ref_Click(null, null);
                            }
                        }*/
                        else if (sid == hotKeyDic["Ctrl-X"])
                        {
                            if (btnScanRef.IsEnabled)
                            {
                                btnScanRef_Click(null, null);
                            }
                        }
                        else if (sid == hotKeyDic["Ctrl-S"])
                        {
                            if (btnAdjustScan.IsEnabled && btnAdjustScan.IsVisible)
                            {
                                btnAdjustScan_Click(null, null);
                            }
                            else if (btnStopScan.IsEnabled && btnStopScan.IsVisible)
                            {
                                btnStopScan_Click(null, null);
                            }
                        }
                        else if (sid == hotKeyDic["Ctrl-T"])
                        {
                            if (btnPDLScan.IsEnabled && btnPDLScan.IsVisible)
                            {
                                btnPDLScan_Click(null, null);
                            }
                        }
                        else if (sid == hotKeyDic["Ctrl-P"])
                        {
                            chkPort12.IsChecked = !chkPort12.IsChecked.Value;
                            chkPort34.IsChecked = !chkPort12.IsChecked.Value;
                        }


                        handled = true;
                        break;
                    }
            }

            return IntPtr.Zero;
        }

        private void OperateInterleaver_KeyDown(object sender, KeyEventArgs e)
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
        /// 接收到主程序已经初始化完成，再进行的初始化动作
        /// </summary>
        /// <param name="info">主程序初始化信息</param>
        public void Init(MainInitInfo info)
        {
            mainInfo = info;
            testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), mainInfo.TestProcess, true);
            templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), mainInfo.TemplateType, true);

            curveShow = new InterleaverCurve(EventAggregator);
            paramCal = new InterleaverParamCal(algorithm);

            string curDir = System.Environment.CurrentDirectory;
            refWithPDLFile = curDir + refWithPDLFile;
            refWithNoPDLFile = curDir + refWithNoPDLFile;
            scanWithPDLFile = curDir + scanWithPDLFile;
            scanWithNoPDLFile = curDir + scanWithNoPDLFile;
            lightDataDir = curDir + lightDataDir;
            ref1830File = curDir + ref1830File;

            //曲线显示初始化
            curveShow.InitAllCurve();

            //只有调节工序才需要功率计实时显示，照光和调节按钮
            if (IsAdjust(testProcess))
            {
                uiVariable.IsAdjustScanEnable = true;
                powermeterRealtimeBK.RunWorkerAsync();
            }
            else
            {
                uiVariable.IsLightedEnable = false;
                uiVariable.IsStopScanVisible = Visibility.Hidden;
                uiVariable.IsAdjustScanVisible = Visibility.Visible;
                uiVariable.IsAdjustScanEnable = false;
            }

            IInterleaverScan scan = null;
            string errMsg = "";
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                scanPowermeterCount = scan.PowermeterCount();
            }

            uiVariable.IsPort12 = true;

            uiVariable.IsPort34 = false;
            errMsg = "";
            ReadRefTime(ref errMsg);
            ReadRefData(ref errMsg);
            refTimeCheckBK.RunWorkerAsync();
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

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\..\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            if(GetOpenTemplateComplete()&& isTestedUnSave)
            {
                if(MessageBox.Show("有未保存测试项，是否要打开新的模板！", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning)==MessageBoxResult.Cancel)
                {
                    uiVariable.SN = allProductControl[0].ProductSN;
                    return;
                }
            }
            portRawdatas.Clear();
            SetOpenTemplateComplete(false);
            isTestedUnSave = false;
            selectItem = null;

            if (uiVariable.SN!=null&&uiVariable.SN.Length == 0)
            {
                WarningBox("请输入产品号！！");
                return;
            }
            if (mainInfo == null)
            {
                ErrorBox("无工位信息，请检查配置！");
                return;
            }
            RealtimeMsg("正在打开模板...");
            isBeginPDLScan = false;
            isBeginLight = false;
            uiVariable.IsSaveEnable = false;
            uiVariable.IsLightedEnable = false;
            portAssistant.Clear();

            BackgroundWorker templateBK = new BackgroundWorker();
            templateBK.DoWork += OpenTemplateBK_DoWork;
            templateBK.RunWorkerCompleted += OpenTemplateBK_RunWorkerCompleted;
            templateBK.RunWorkerAsync();
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


        /// <summary>
        /// 根据模板更新中、低、高频
        /// </summary>
        /// <param name="lowFre">低频</param>
        /// <param name="midFre">中频</param>
        /// <param name="highFre">高频</param>
        private void UpdateSpecialFre(double lowFre, double midFre, double highFre)
        {
            specialShiftsDic.Clear();
            specialShiftsDic.Add(lowFre, CommonFunction.GetDefaultValue());
            specialShiftsDic.Add(midFre, CommonFunction.GetDefaultValue());
            specialShiftsDic.Add(highFre, CommonFunction.GetDefaultValue());

            specialShiftsDic.Add(lowFre + productFre, CommonFunction.GetDefaultValue());
            specialShiftsDic.Add(midFre + productFre, CommonFunction.GetDefaultValue());
            specialShiftsDic.Add(highFre + productFre, CommonFunction.GetDefaultValue());
            int i = 0;
            foreach (KeyValuePair<double, double> pair in specialShiftsDic)
            {
                string content = string.Format("{0:N1}-Shift:", pair.Key);
                shiftLabels[i].Content = content.Replace(",", "");
                i++;
            }

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

        private void OpenTemplateBK_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string errMsg = (string)e.Result;
            if (errMsg.Length == 0)
            {
                templatePorts.Clear();
                RealtimeMsg(uiVariable.SN + "：打开模板成功！");
                SetOpenTemplateComplete(true);
                //列表显示
                showControl = allProductControl[0].Clone();
                List<int> deleteItems = new List<int>();
                updateParamIndex.Clear();
                portAndNameDic.Clear();

                //曲线显示处理
                List<MESTestInfo> testInfos = allProductControl[0].GetAllTestInfo();

                double lowFreLeft = 193500.0;
                double lowFreRight = 194000.0;
                double midFreLeft = 194500.0;
                double midFreRight = 195000.0;
                double highFreLeft = 196000.0;
                double highFreRight = 196500.0;

                int rangCount = 0;
                for (int i = 0; i < testInfos.Count; i++)
                {
                    string param = testInfos[i].ExParamName.ToUpper();
                    if (param.Contains("LFRANGE"))
                    {
                        ParserRange(param, ref lowFreLeft, ref lowFreRight);
                        rangCount++;
                    }
                    else if (param.Contains("MFRANGE"))
                    {
                        ParserRange(param, ref midFreLeft, ref midFreRight);
                        rangCount++;
                    }
                    else if (param.Contains("HFRANGE"))
                    {
                        ParserRange(param, ref highFreLeft, ref highFreRight);
                        rangCount++;
                    }
                    if (rangCount == 3)
                        break;
                }
                UpdateSpecialFre((lowFreLeft + lowFreRight) / 2, (midFreLeft + midFreRight) / 2, (highFreLeft + highFreRight) / 2);
                //iso标准线显示
                List<double> fres = new List<double>();
                List<double> isoCriterions = new List<double>();
                curveAdjParamName = "ADJ@PB=" + passBand.ToString();
                List<string> retainParam = new List<string>();
                foreach (string str in showParam)
                {
                    string fullName = str.ToUpper() + "@PB=" + passBand.ToString();
                    retainParam.Add(fullName);
                }
                maxScanFre = -2000000.0;
                minScanFre = 2000000.0;
                for (int i = 0; i < testInfos.Count; i++)
                {
                    string param = testInfos[i].ExParamName.ToUpper();
                    //通道名_频率_porti
                    string[] splits = testInfos[i].PortNameForUser.Split('_');
                    double tmpt = testInfos[i].Temperature;
                    //adj合格要求值
                    if (param.ToUpper().Contains(curveAdjParamName))
                    {
                        if (splits.Length > 2)
                        {
                            isoCriterions.Add(Convert.ToDouble(testInfos[i].Criterion));
                            fres.Add(Convert.ToDouble(splits[splits.Length - 2]));

                            //通道名称对应关系
                            if(!portAndNameDic.ContainsKey(splits[splits.Length - 3]))
                            {
                                portAndNameDic.Add(splits[splits.Length - 3], splits[splits.Length - 1]);
                                //增加port assistant,只适合一个温度

                            }
                            double fre = Convert.ToDouble(splits[1]);
                            if (minScanFre > fre)
                                minScanFre = fre;
                            if (maxScanFre < fre)
                                maxScanFre = fre;

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

                                if (assist.PortIndex > 2)
                                    assist.PMIndex = portAndPMDic[assist.PortIndex.ToString()];
                                else
                                    assist.PMIndex = assist.PortIndex;
                                portAssistant.Add(assist);
                            }
                        }
                    }

                    //筛选出需要显示的参数项，只显示总通道,子通道都是 通道名_中心频率_PORTi
                    bool isNeedShow = false;
                    if (splits.Length == 1)
                    {
                        //查找是否是需要显示的参数
                        /*foreach (string str in retainParam)
                        {
                            if (param.Contains(str))
                            {*/
                        if (testInfos[i].PortNameForUser.ToUpper() != "Frequency Range".ToUpper())
                        {
                            isNeedShow = true;
                        }
                            /*}
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
                showControl.DeleteParams(deleteItems);  
                string sourceStr= "@PB=" + passBand.ToString();
                showControl.ColumnReplaceStr(sourceStr, "");

                minScanFre = minScanFre - productFre;
                maxScanFre = maxScanFre + productFre;

                uiVariable.PN = allProductControl[0].GetProductInfo().ProductPN;
                uiVariable.Spec = allProductControl[0].GetProductInfo().Spec;

                //更新曲线
                curveShow.UpdateISOCriterionCurve(fres, isoCriterions);
                curveShow.UpdateLowMidHighFre(lowFreLeft, lowFreRight, midFreLeft, midFreRight, highFreLeft, highFreRight);

                // 更新测试信息
                if (EventAggregator != null)
                {
                    List<MESControl> shows = new List<MESControl>();
                    shows.Add(showControl);
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
                }
            }
            else
            {
                RealtimeMsg(errMsg, StatusType.Error);

                ErrorBox(errMsg);
                return;
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
            string[] splits = source.Split('@');
            if (splits.Length > 1)
            {
                string[] rangs = splits[1].Split(',');
                if (rangs.Length > 1)
                {
                    string[] wls = rangs[0].Split('-');
                    leftFre = Convert.ToDouble(wls[0]);
                    rightFre = Convert.ToDouble(wls[1]);
                    productFre = Convert.ToDouble(rangs[1]);
                }
                if (rangs.Length > 2)
                {
                    string[] pbs = rangs[2].Split('=');
                    passBand = Convert.ToDouble(pbs[1]);
                }
                if (rangs.Length > 3)
                {
                    convertAlgorithm = rangs[3];
                }
            }
        }

        /// <summary>
        /// 打开模板处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTemplateBK_DoWork(object sender, DoWorkEventArgs e)
        {
            MESControl control = new MESControl();
            string errMsg = "";
            allProductControl.Clear();
            if (control.OpenTemplate(amtsUrl, templateType, uiVariable.SN, testProcess, MESTestType.Normal, mainInfo.UserID, mainInfo.Goldsample, true, false, ref errMsg))
            {
                allProductControl.Add(control);
            }
            e.Result = errMsg;
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
        }

        /// <summary>
        /// 切换光开关
        /// </summary>
        /// <param name="isScan">是否是扫描</param>
        private void SetSwitch(bool isScan)
        {
            string flag = GetSwitchFlag(isScan);
            //RealtimeMsg("开始切换开关");
            string errMsg = "";
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType("InterleaverSwitch", ref opticalSwitch, ref errMsg) == 0)
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
                return;
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

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="scanInfo">扫描类型，是否带PDL，归零还是测试</param>
        /// <param name="resPath">保存扫描结果文件路径</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        private int DoScan(ScanDetail scanInfo, ref string resPath, ref string errMsg)
        {
            IInterleaverScan scan = null;
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                if (scanInfo.ScanType == SCANTYPE.RefWithNoPDL)
                {
                    resPath = scanWithNoPDLFile;
                    return scan.Scan(false, true, ref resPath, ref errMsg);
                }
                else if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    resPath = scanWithPDLFile;
                    return scan.Scan(true, true, ref resPath, ref errMsg);
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithPDL)
                {
                    resPath = scanWithPDLFile;
                    return scan.Scan(true, false, ref resPath, ref errMsg);
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithNoPDL)
                {
                    resPath = scanWithNoPDLFile;
                    return scan.Scan(false, false, ref resPath, ref errMsg);
                }
            }
            return 2;
        }

        /// <summary>
        /// 开启扫描background线程
        /// </summary>
        /// <param name="scanType">扫描类型</param>
        private void DoScanOnBK(SCANTYPE scanType)
        {
            if (GetIsScanFinished())
            {
                SetIsScanFinished(false);
                RealtimeMsg("开始扫描。。。");
                BackgroundWorker bkScan = new BackgroundWorker();
                bkScan.DoWork += Scan_DoWork;
                bkScan.RunWorkerCompleted += Scan_RunWorkerCompleted;
                scanDetailInfo.ScanType = scanType;
                scanDetailInfo.Port = GetCurPort(true);
                bkScan.RunWorkerAsync(scanDetailInfo);
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
        /// 读取最久的系统归零时间
        /// </summary>
        /// <param name="errMsg"></param>
        private void ReadRefTime(ref string errMsg)
        {
            try
            {
                oldestRefTime = new DateTime();
                DateTime defaultTime = new DateTime();
                bool[] isRefSuccess = new bool[4];
                for (int i = 0; i < cstPortCount; i++)
                {
                    string pdlRefPath = refWithPDLFile + (i + 1).ToString() + ".csv";
                    string nopdlRefPath = refWithNoPDLFile + (i + 1).ToString() + ".csv";
                    //读取PDL的归零数据
                    if (InterleaverScanResult.ReadRefTime(pdlRefPath, ref refTimes[i], ref errMsg) == 0)
                    {
                        //处理归零超过4个小时，删除归零文件，清除内存数据
                        DateTime curTime = DateTime.Now;
                        if (!refTimes[i].Equals(defaultTime))
                        {
                            TimeSpan span = curTime - refTimes[i];

                            //归零数据超过四个小时，删除
                            if (IsRefTimePassdue(span))
                            {
                                refTimes[i] = new DateTime();
                                if (File.Exists(pdlRefPath))
                                    File.Delete(pdlRefPath);

                                if (File.Exists(nopdlRefPath))
                                    File.Delete(nopdlRefPath);
                                //清除内存归零数据 
                                InterleaverScanResult.InitRawdataBuffer(portPDLRef[i]);
                            }
                            //四个归零时间中，最老的时间用来做倒计时
                            if (oldestRefTime.Equals(defaultTime) || oldestRefTime.CompareTo(refTimes[i]) > 0)
                            {
                                oldestRefTime = refTimes[i];
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
        /// 读取归零数据，1830归零数据读取未加
        /// </summary>
        /// <param name="errMsg"></param>
        private void ReadRefData(ref string errMsg)
        {
            try
            {
                bool[] isRefSuccess = new bool[4];
                for (int i = 0; i < cstPortCount; i++)
                {
                    string pdlRefPath = refWithPDLFile + (i + 1).ToString() + ".csv";
                    string noPDLRefPath = refWithNoPDLFile + (i + 1).ToString() + ".csv";
                    //读取PDL的归零数据
                    int pdlRef = InterleaverScanResult.ReadScanData(pdlRefPath, portPDLRef[i], ref errMsg);
                    //读取NoPDL的归零数据
                    int noPDLRef = InterleaverScanResult.ReadScanData(noPDLRefPath, portNoPDLRef[i], ref errMsg);
                    
                    if (pdlRef == 0 && noPDLRef == 0)
                    {
                         isRefSuccess[i] = true;
                    }
                    else
                    {
                        isRefSuccess[i] = false;
                    }
                }

                Read1830Ref(ref errMsg);

                bool bReadSuccess = false;
                string strPrompt = "端口";
                for (int i = 0; i < cstPortCount; i++)
                {
                    if (isRefSuccess[i])
                    {
                        strPrompt += "  ";
                        strPrompt += (i + 1).ToString();
                        bReadSuccess = true;
                        //显示归零数据
                        double[][] scanRes = null;
                        scanRes = portNoPDLRef[i];
                        if (scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
                        {
                            curveShow.UpdateScanCurve(i + 1, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                        }
                        refSysLEDs[i].Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    }
                    else
                        refSysLEDs[i].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
                strPrompt += "读取系统归零数据成功！";
                if (bReadSuccess)
                    MessageBox.Show(strPrompt);

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 扫描的通道是否归零
        /// </summary>
        /// <param name="scanPorts">参与扫描的通道</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true--已归零 false--未归零</returns>
        private bool IsScanRef(List<int> scanPorts, ref string errMsg)
        {
            List<int> unRefPort = new List<int>();
            errMsg = "端口";
            foreach (int port in scanPorts)
            {
                if (portNoPDLRef[port - 1][0] != null && portNoPDLRef[port - 1][0].Length > 0 && portNoPDLRef[port - 1][0][0].CompareTo(0) > 0
                    && portPDLRef[port - 1][0] != null && portPDLRef[port - 1][0].Length > 0 && portPDLRef[port - 1][0][0].CompareTo(0) > 0)
                {
                    double[] fres = portPDLRef[port - 1][2];
                    if (fres[fres.Length - 1] < maxScanFre || fres[0] > minScanFre)
                    {
                        WarningBox("测试频率超出归零频率，请确认服务器扫描范围!");
                        //清除内存归零数据 
                        unRefPort.Add(port);
                        errMsg = errMsg + " " + port.ToString();
                    }
                    else
                        continue;
                }
                else
                {
                    unRefPort.Add(port);
                    errMsg = errMsg + " " + port.ToString();
                }
            }
            if (unRefPort.Count > 0)
            {
                errMsg += "未归零!";

                return false;
            }

            return true;
        }

        /// <summary>
        /// 扫描按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPDLScan_Click(object sender, RoutedEventArgs e)
        {
            isPort12Select = uiVariable.IsPort12;
            string errMsg = "";

            List<int> scanPorts = new List<int>();
            int port = GetCurPort(true);
            scanPorts.Add(port);
            scanPorts.Add(port + 1);
            if (!IsScanRef(scanPorts, ref errMsg))
            {
                WarningBox(errMsg);
                return;
            }

            SetSwitch(true);

            SCANTYPE scanType = SCANTYPE.TestWithPDL;

            isBeginPDLScan = true;

            uiVariable.IsEnable = false;
            uiVariable.IsSaveEnable = false;
            uiVariable.IsAdjustScanEnable = false;
            uiVariable.IsAdjustScanVisible = Visibility.Visible;
            uiVariable.IsStopScanVisible = Visibility.Hidden;
            DoScanOnBK(scanType);
        }


        private void btnAdjustScan_Click(object sender, RoutedEventArgs e)
        {
            isPort12Select = uiVariable.IsPort12;
            string errMsg = "";

            List<int> scanPorts = new List<int>();
            int port = GetCurPort(true);
            scanPorts.Add(port);
            scanPorts.Add(port + 1);
            if (!IsScanRef(scanPorts, ref errMsg))
            {
                WarningBox(errMsg);
                return;
            }
            uiVariable.IsAdjustScanVisible = Visibility.Hidden;
            uiVariable.IsStopScanVisible = Visibility.Visible;
            isStopScan = false;
            isBeginPDLScan = false;
            uiVariable.IsEnable = false;
            uiVariable.IsAdjustScanEnable = false;
            uiVariable.IsSaveEnable = false;
            SetSwitch(true);

            SCANTYPE scanType = SCANTYPE.TestWithNoPDL;
            
            DoScanOnBK(scanType);
        }



        /// <summary>
        /// 扫描结束后处理
        /// </summary>
        /// <param name="scanInfo">扫描类型等信息</param>
        private void ScanFinish(ScanDetail scanInfo)
        {
            double[][] scanRes = null;
            double[][] scanRes2 = null;

            if (scanInfo.ScanType == SCANTYPE.RefWithNoPDL || scanInfo.ScanType == SCANTYPE.RefWithPDL)
            {
                scanRes = portPDLRef[scanInfo.Port - 1];
                List<int> scanPorts = new List<int>();
                scanPorts.Add(scanInfo.Port);
                string errMsg = "";
                if (!IsScanRef(scanPorts, ref errMsg))
                {
                    refSysLEDs[scanInfo.Port - 1].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
                else
                    refSysLEDs[scanInfo.Port - 1].Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));

            }
            else
            {
                scanRes = portResData[scanInfo.Port - 1];
                scanRes2 = portResData[scanInfo.Port];
            }
            //归零时，更新一条曲线，测试时，两条曲线同时更新
            if (scanRes != null && scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
            {
                curveShow.UpdateScanCurve(scanInfo.Port, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                if (scanRes2 != null && scanRes2[scanRes.Length - 1] != null && scanRes2[1] != null)
                {
                    curveShow.UpdateScanCurve(scanInfo.Port + 1, scanRes2[scanRes.Length - 1].ToList(), scanRes2[1].ToList());
                }
            }

            if (GetIsScanFinished())
            {
                if (scanInfo.ScanType == SCANTYPE.TestWithNoPDL && isStopScan == false)
                {
                    if (!noPDLContinueScanBK.IsBusy)
                        noPDLContinueScanBK.RunWorkerAsync(scanDetailInfo.ScanType);
                }
                else
                {
                    if (scanInfo.ScanType == SCANTYPE.RefWithNoPDL || scanInfo.ScanType == SCANTYPE.RefWithPDL)
                    {
                        string errMsg = "";
                        ReadRefTime(ref errMsg);
                    }
                    else if (scanInfo.ScanType == SCANTYPE.TestWithPDL)
                    {
                        isTestedUnSave = true;
                        //增加保存到无纸化rawdata更新               
                        foreach (PortAssist assist in portAssistant)
                        {
                            if (assist.PortIndex == scanInfo.Port)
                            {
                                int dataLen = scanRes.Length;
                                string maxRawdata = "VOLT-1DB,";
                                string minRawdata = "VOLT-2DB,";
                                string aveRawdata = "VOLT-3DB,";
                                string pdlRawdata = "VOLT-4DB,";
                                int rawdataLen = scanRes[1].Length;
                                for (int k = 0; k < rawdataLen; k++)
                                {
                                    maxRawdata += string.Format("{0:F3}:{1:F3},", scanRes[dataLen - 1][k], scanRes[3][k]);
                                    minRawdata += string.Format("{0:F3}:{1:F3},", scanRes[dataLen - 1][k], scanRes[4][k]);
                                    aveRawdata += string.Format("{0:F3}:{1:F3},", scanRes[dataLen - 1][k], scanRes[1][k]);
                                    if (k != rawdataLen - 1)
                                    {
                                        pdlRawdata += string.Format("{0:F3}:{1:F3},", scanRes[dataLen - 1][k], scanRes[2][k]);
                                    }
                                    else
                                    {
                                        pdlRawdata += string.Format("{0:F3}:{1:F3}", scanRes[dataLen - 1][k], scanRes[2][k]);
                                    }
                                }
                                if (rawdataLen > 0)
                                {
                                    assist.Rawdata = maxRawdata + minRawdata + aveRawdata + pdlRawdata;
                                }
                            }
                            else if(assist.PortIndex == scanInfo.Port+1)
                            {
                                int dataLen = scanRes2.Length;
                                string maxRawdata = "VOLT-1DB,";
                                string minRawdata = "VOLT-2DB,";
                                string aveRawdata = "VOLT-3DB,";
                                string pdlRawdata = "VOLT-4DB,";
                                int rawdataLen = scanRes2[1].Length;
                                for (int k = 0; k < rawdataLen; k++)
                                {
                                    maxRawdata += string.Format("{0:F3}:{1:F3},", scanRes2[dataLen - 1][k], scanRes2[3][k]);
                                    minRawdata += string.Format("{0:F3}:{1:F3},", scanRes2[dataLen - 1][k], scanRes2[4][k]);
                                    aveRawdata += string.Format("{0:F3}:{1:F3},", scanRes2[dataLen - 1][k], scanRes2[1][k]);
                                    if (k != rawdataLen - 1)
                                    {
                                        pdlRawdata += string.Format("{0:F3}:{1:F3},", scanRes2[dataLen - 1][k], scanRes2[2][k]);
                                    }
                                    else
                                    {
                                        pdlRawdata += string.Format("{0:F3}:{1:F3}", scanRes2[dataLen - 1][k], scanRes2[2][k]);
                                    }
                                }
                                if (rawdataLen > 0)
                                {
                                    assist.Rawdata = maxRawdata + minRawdata + aveRawdata + pdlRawdata;
                                }
                            }
                        }
                        //如果开始照光，必须照完光后才可以点亮保存按钮
                        if (isBeginLight)
                        {
                            string errMsg = "";
                            if (LightedCRCExist(ref errMsg))
                            {
                                uiVariable.IsSaveEnable = true;
                            }
                        }
                        else
                        {
                            uiVariable.IsSaveEnable = true;
                        }
                    }

                    uiVariable.IsAdjustScanVisible = Visibility.Visible;
                    uiVariable.IsStopScanVisible = Visibility.Hidden;
                    uiVariable.IsEnable = true;
                    if (IsAdjust(testProcess))
                    {
                        uiVariable.IsAdjustScanEnable = true;
                    }
                    SetSwitch(false);
                }
                UpdateSpecialParam();
                UpdateParamList();
            }
        }



        private void UpdateSpecialParam()
        {
            if (portMinISODic.ContainsKey("PORT1")&& !CommonFunction.IsDefault(-portMinISODic["PORT1"]))
            {
                PM1MinIso.Text = string.Format("{0:N2}", portMinISODic["PORT1"]).Replace(",", "");
            }

            if (portMinISODic.ContainsKey("PORT2") && !CommonFunction.IsDefault(-portMinISODic["PORT2"]))
            {
                PM2MinIso.Text = string.Format("{0:N2}", portMinISODic["PORT2"]).Replace(",", "");
            }


            double fsr1 = CommonFunction.GetDefaultValue();
            double fsr2 = CommonFunction.GetDefaultValue();          

            double[] shifts = specialShiftsDic.Values.ToArray();
            
            for (int i=0;i<shifts.Length;i++)
            {
                if (CommonFunction.IsDefault(shifts[i]) || CommonFunction.IsDefault(-shifts[i]))
                {
                    shitfValueTextBox[i].Text = "";
                }
                else
                {                
                    shitfValueTextBox[i].Text = string.Format("{0:N2}", shifts[i]).Replace(",", "");
                }
                if(i==2)
                {
                    fsr1 = shifts[i] - shifts[i - 2];
                    PM1FSR.Text= string.Format("{0:N2}", fsr1).Replace(",", "");
                }
                if(i==5)
                {
                    fsr2 = shifts[i] - shifts[i - 2];
                    PM2FSR.Text = string.Format("{0:N2}", fsr2).Replace(",", "");
                }
            }

        }


        /// <summary>
        /// 更新参数列表
        /// </summary>
        private void UpdateParamList()
        {
            //计算后再显示
            if (GetOpenTemplateComplete())
            {
                // 更新测试信息
                if (EventAggregator != null && GetOpenTemplateComplete())
                {
                    List<MESTestInfo> testInfos = showControl.GetAllTestInfo();
                    for (int i = 0; i < testInfos.Count; i++)
                    {
                        MESTestInfo info = testInfos[i];
                        UpdateItem(info, 0, i);
                    }
                }
                curveShow.UpdateISOCurve(1, pm1ISOResult[1], pm1ISOResult[0]);
                curveShow.UpdateISOCurve(2, pm2ISOResult[1], pm2ISOResult[0]);
                UpdateResIcon();
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
                //清除上一次测试数据
                if (scanInfo.ScanType == SCANTYPE.RefWithPDL)
                {
                    InterleaverScanResult.InitRawdataBuffer(portPDLRef[scanInfo.Port - 1]);
                }
                else if (scanInfo.ScanType == SCANTYPE.TestWithPDL || scanInfo.ScanType == SCANTYPE.TestWithNoPDL)
                {
                    InterleaverScanResult.InitRawdataBuffer(portResData[scanInfo.Port - 1]);
                    InterleaverScanResult.InitRawdataBuffer(portResData[scanInfo.Port]);
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
                    int pmIndex = scanInfo.Port;
                    if (scanInfo.Port > scanPowermeterCount)
                        pmIndex = scanInfo.Port - scanPowermeterCount;
                    //读取一个偏振态下原始数据
                    //for (int i = 0; i < 4; i++)
                    {
                        string path = scanWithPDLFile + pmIndex.ToString()+ ".csv";
                        InterleaverScanResult.ReadScanData(path, pdlRawData[0], ref errMsg);
                    }

                    InterleaverScanResult.CalPDLRefData(pdlRawData, portPDLRef[scanInfo.Port - 1], ref errMsg);
                    if (errMsg.Length > 0)
                        return 2;



                    string pdlRefPath = refWithPDLFile + scanInfo.Port.ToString() + ".csv";
                    InterleaverScanResult.WritePDLRefData(pdlRefPath, portPDLRef[scanInfo.Port - 1], ref errMsg);
                    //读取PDL的归零数据
                    //InterleaverScanResult.ReadScanData(resPath, portPDLRef[scanInfo.Port - 1], ref errMsg);

                    //PDL归完零后，是NoPDL归零
                    scanInfo.ScanType = SCANTYPE.RefWithNoPDL;
                    res = DoScan(scanInfo, ref resPath, ref errMsg);
                    if (errMsg.Length > 0 || res != 0)
                    {
                        return res;
                    }
                    //拷贝归零数据

                    string pLocalFilePath = scanWithNoPDLFile + pmIndex.ToString() + ".csv";
                    string pSaveFilePath = refWithNoPDLFile + scanInfo.Port.ToString() + ".csv";
                    if (File.Exists(pLocalFilePath))//必须判断要复制的文件是否存在
                    {
                        File.Copy(pLocalFilePath, pSaveFilePath, true);//三个参数分别是源文件路径，存储路径，若存储路径有相同文件是否替换
                    }

                    //读取NoPDL的归零数据
                    InterleaverScanResult.ReadScanData(pSaveFilePath, portNoPDLRef[scanInfo.Port - 1], ref errMsg);
                    if (InterleaverScanResult.CheckRefRight(portNoPDLRef[scanInfo.Port - 1], ref errMsg) != 0)
                        return 2;
                }
                if (errMsg.Length == 0)
                {
                    //先将数据清零
                    //InitRawdataBuffer(ref rawdata);
                    if (scanInfo.ScanType == SCANTYPE.TestWithNoPDL)
                    {
                        for (int i = scanInfo.Port; i < scanInfo.Port + scanPowermeterCount; i++)
                        {
                            int pmIndex = i;
                            if (i > scanPowermeterCount)
                                pmIndex = i - scanPowermeterCount;
                            //原始数据文件都是1、2，但是端口可能是1、2、3、4，所以如果是3、4时，需要对2取模
                            resPath = scanWithNoPDLFile + pmIndex.ToString() + ".csv";
                            InterleaverScanResult.ReadScanData(resPath, pdlRawData[0], ref errMsg);
                            InterleaverScanResult.CalRawdataByNoPDL(pdlRawData, portNoPDLRef[i - 1], portResData[i - 1], ref errMsg);
                        }
                    }
                    else if (scanInfo.ScanType == SCANTYPE.TestWithPDL)
                    {
                        for (int j = scanInfo.Port; j < scanInfo.Port + scanPowermeterCount; j++)
                        {
                            int pmIndex = j;
                            if (j > scanPowermeterCount)
                                pmIndex = j - scanPowermeterCount;
                            //读取四个偏振态下原始数据
                            for (int i = 0; i < 4; i++)
                            {
                                string path = scanWithPDLFile + pmIndex.ToString() + (i + 1).ToString() + ".csv";
                                InterleaverScanResult.ReadScanData(path, pdlRawData[i], ref errMsg);
                            }
                            if (convertAlgorithm.ToUpper() == ConvertAlgorithm.Ave.GetAdditional().ToUpper())
                            {
                                string recPath = scanWithPDLFile + j.ToString() + "Ave.CSV";
                                InterleaverScanResult.CalRawdataByAve(pdlRawData, portPDLRef[j - 1], portResData[j - 1], ref errMsg);
                                //InterleaverScanResult.WriteCalData(recPath, portResData[scanInfo.Port - 1], ref errMsg);
                            }
                            else if (convertAlgorithm.ToUpper() == ConvertAlgorithm.MaxMin.GetAdditional().ToUpper())  //将四个偏振态数据转为ave PDL max min数据
                            {
                                string recPath = scanWithPDLFile + j.ToString() + "MaxMin.CSV";
                                InterleaverScanResult.CalRawdataByMaxMin(pdlRawData, portPDLRef[j - 1], portResData[j - 1], ref errMsg);
                                //InterleaverScanResult.WriteCalData(recPath, portResData[scanInfo.Port - 1], ref errMsg);
                            }
                            else if (convertAlgorithm.ToUpper() == ConvertAlgorithm.Mueller.GetAdditional().ToUpper())
                            {
                                string recPath = scanWithPDLFile + j.ToString() + "Mueller.CSV";
                                InterleaverScanResult.CalRawdataByMueller(pdlRawData, portPDLRef[j - 1], portResData[j - 1], ref errMsg);
                                //InterleaverScanResult.WriteCalData(recPath, portResData[scanInfo.Port - 1], ref errMsg);
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

        private void ClearAllResult(ref string errMsg)
        {
            try
            {
                if (GetIsScanFinished() == false)
                    return;

                foreach (List<double> res in pm1ISOResult)
                {
                    res.Clear();
                }

                foreach (List<double> res in pm2ISOResult)
                {
                    res.Clear();
                }


                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[0].GetAllTestInfo();
                bool isPass = false;
                for (int i = 0; i < allTestParam.Count; i++)
                {
                    allProductControl[0].UpdateTestData(i, CommonFunction.GetDefaultValue(), ref isPass);
                }
                List<MESTestInfo> showInfos = showControl.GetAllTestInfo();
                for (int j = 0; j < showInfos.Count; j++)
                {
                    showControl.UpdateTestData(j, CommonFunction.GetDefaultValue(), ref isPass);
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
        private void ClearResult(int clrPort, ref string errMsg)
        {
            try
            {
                if (GetIsScanFinished() == false)
                    return;

                foreach (List<double> res in pm1ISOResult)
                {
                    res.Clear();
                }

                foreach (List<double> res in pm2ISOResult)
                {
                    res.Clear();
                }


                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[0].GetAllTestInfo();
                for (int i = 0; i < allTestParam.Count; i++)
                {
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
                        if (clrPort != port)
                        {
                            continue;
                        }
                        portName = portSplits[0];
                        bool isPass = false;
                        allProductControl[0].UpdateTestData(i, CommonFunction.GetDefaultValue(), ref isPass);
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
                            allProductControl[0].UpdateTestData(i, CommonFunction.GetDefaultValue(), ref isPass);
                            List<MESTestInfo> showInfos = showControl.GetAllTestInfo();
                            for (int j = 0; j < showInfos.Count; j++)
                            {
                                if (showInfos[j].Temperature == allTestParam[i].Temperature && showInfos[j].PortNameForUser == allTestParam[i].PortNameForUser
                                    && showInfos[j].ExParamName == allTestParam[i].ExParamName)
                                {
                                    showControl.UpdateTestData(j, CommonFunction.GetDefaultValue(), ref isPass);
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
        /// 计算参数函数
        /// </summary>
        private void CalAllResultInThread()
        {
            ClearISO();
            double[] keys = specialShiftsDic.Keys.ToArray();
            foreach (double key in keys)
            {
                specialShiftsDic[key] = CommonFunction.GetDefaultValue();
            }
            SetCalFinished(0, false);
            SetCalFinished(1, false);
            Thread calThread1 = new Thread(new ParameterizedThreadStart(ChannelCalThread));
            Thread calThread2 = new Thread(new ParameterizedThreadStart(ChannelCalThread));

            calThread1.Start(0);
            calThread2.Start(1);


            while (!IsAllCalFinished())
            {
                Thread.Sleep(100);
            }
            string errMsg = "";
            CalPortRes(ref errMsg);
        }

        /// <summary>
        /// 清除ISO等数据
        /// </summary>
        private void ClearISO()
        {
            foreach (List<double> res in pm1ISOResult)
            {
                res.Clear();
            }
            foreach (List<double> res in pm2ISOResult)
            {
                res.Clear();
            }
            curPortRecords.Clear();
        }

        /// <summary>
        /// 计算线程函数
        /// </summary>
        /// <param name="param">第几个计算线程</param>
        private void ChannelCalThread(object param)
        {
            int splitIndex = Convert.ToInt32(param);
            string errMsg = "";
            SetCalFinished(splitIndex, false);
            CalChannelResByThread(splitIndex, ref errMsg);
            SetCalFinished(splitIndex, true);
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetCalFinished(int n, bool isFinished)
        {
            isSplitCalFinished[n] = isFinished;
        }

        /// <summary>
        /// 是否所有计算线程结束
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool IsAllCalFinished()
        {
            for (int i = 0; i < splitCalCount; i++)
            {
                if (isSplitCalFinished[i] == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 计算参数函数
        /// </summary>
        /// <param name="splitIndex">第几个线程参</param>
        /// <param name="errMsg">出错信息</param>
        private void CalChannelResByThread(int splitIndex, ref string errMsg)
        {
            try
            {
                
                string portName = "";
                List<MESTestInfo> allTestParam = allProductControl[0].GetAllTestInfo();
                //List<SamePortParamData> curPortRecords = new List<SamePortParamData>();
                int split = allTestParam.Count / splitCalCount;
                int end = allTestParam.Count;
                if (splitCalCount != splitIndex + 1)
                {
                    end = split * (splitIndex + 1);
                }
                var typeName = algorithm.GetType();
                IInterleaverAlgorithm interleaverAlgorithm = (IInterleaverAlgorithm)Activator.CreateInstance(typeName);
                InterleaverParamCal calFuntion = new InterleaverParamCal(interleaverAlgorithm);

                int scanPort1 = scanDetailInfo.Port;
                int scanPort2 = scanDetailInfo.Port + 1;
                for (int i = split * splitIndex; i < end; i++)
                {
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
                        if (scanPort1 != port && scanPort2 != port)
                        {
                            continue;
                        }
                        //不是扫描的温度，则返回,暂定只对常温做计算
                        if (allTestParam[i].Temperature > 30 || allTestParam[i].Temperature < 20)
                            continue;
                        portName = portSplits[0];
                        bool isPass = true;
                        //计算参数结果
                        double paramResult = CommonFunction.GetDefaultValue();
                        //if (calPort == port)
                        {
                            paramResult = calFuntion.CalChannelTestParam(param, portResData[port - 1], null, fre, productFre, ref errMsg);

                            //特殊中低高频shift值,未考虑深度，只考虑带宽
                            string specialShift = "SHIFT@PB=" + passBand.ToString();
                            if(param.ToUpper().Contains(specialShift))
                            {
                                double[] keys = specialShiftsDic.Keys.ToArray();
                                foreach (double key in keys)
                                {
                                    if (key.CompareTo(fre) == 0)
                                    {
                                        specialShiftsDic[key] = paramResult;
                                        break;
                                    }
                                }
                            }
                            
                            //ISO曲线显示值
                            if (scanPort1 == port)
                            {
                                if (param.ToUpper().Contains(curveAdjParamName))
                                {
                                    pm1ISOResult[0].Add(paramResult);
                                    pm1ISOResult[1].Add(fre);
                                }
                            }
                            else if (scanPort2 == port)
                            {
                                if (param.ToUpper().Contains(curveAdjParamName))
                                {
                                    pm2ISOResult[0].Add(paramResult);
                                    pm2ISOResult[1].Add(fre);
                                }
                            }
                        }

                        if (errMsg.Length == 0)
                        {
                            paramResult = Math.Round(paramResult, 3);
                            AddResultToRecord(curPortRecords, param, portSplits[0], allTestParam[i].Temperature.ToString(), paramResult, ref errMsg);
                            allProductControl[0].UpdateTestData(i, paramResult, ref isPass);
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

        private string GetNamePortIndex(int index)
        {
            foreach (PortAssist assist in portAssistant)
            {
                if (assist.PortIndex == index)
                    return assist.Name;
            }
            return "";
        }

        /// <summary>
        /// 计算port参数
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void CalPortRes(ref string errMsg)
        {
            try
            {
                int scanPort1 = scanDetailInfo.Port;
                int scanPort2 = scanDetailInfo.Port + 1;
                string testPort1 = GetNamePortIndex(scanDetailInfo.Port);
                string testPort2 = GetNamePortIndex(scanDetailInfo.Port+1);
                portMinISODic.Clear();
                List<MESTestInfo> allTestParam = allProductControl[0].GetAllTestInfo();
                for (int i = 0; i < allTestParam.Count; i++)
                {
                    string param = allTestParam[i].ExParamName;
                    string[] portSplits = allTestParam[i].PortNameForUser.Split('_');
                    //判断是总的端口，然后使用之前的计算结果，进行计算。
                    if (portSplits.Length == 1)
                    {
                        if (!(testPort1 == portSplits[0]|| testPort2 == portSplits[0]))
                            continue;
                        bool isPass = true;
                        //计算参数结果
                        double paramResult = paramCal.CalPortParam(param, allTestParam[i].Temperature.ToString(), portSplits[0], curPortRecords, ref errMsg);

                        if (errMsg.Length == 0 && (!CommonFunction.IsDefault(paramResult)))
                        {
                            if (param.ToUpper().Contains("MINISO"))
                            {
                                if(portAndNameDic.ContainsKey(portSplits[0]))
                                {
                                    string portName = portAndNameDic[portSplits[0]].ToUpper();
                                    if (portMinISODic.ContainsKey(portName))
                                    {
                                        portMinISODic[portName] = paramResult;
                                    }
                                    else
                                    {
                                        portMinISODic.Add(portName, paramResult);
                                    }
                                }                               
                            }
                            paramResult = Math.Round(paramResult, 3);
                            allProductControl[0].UpdateTestData(i, paramResult, ref isPass);
                            List<MESTestInfo> showInfos = showControl.GetAllTestInfo();
                            for (int j = 0; j < showInfos.Count; j++)
                            {
                                if (showInfos[j].Temperature == allTestParam[i].Temperature && showInfos[j].PortNameForUser == allTestParam[i].PortNameForUser
                                    && showInfos[j].ExParamName == allTestParam[i].ExParamName)
                                {
                                    showControl.UpdateTestData(j, paramResult, ref isPass);
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
        /// 更新测试结果ICON
        /// </summary>
        private void UpdateResIcon()
        {
            string errMsg = "";

            if (allProductControl[0].GetAllTestedPassed(ref errMsg))
                passOrFailImg.Source = passBitmapImage;
            else
                passOrFailImg.Source = failBitmapImage;
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

        private void PassOrFail_Load(object sender, RoutedEventArgs e)
        {
            InitPassFailImage();
            //设置图片显示大小，将图片放大1.5倍
            passOrFailImg.Height = passBitmapImage.Width * 1.5;
            passOrFailImg.Width = passBitmapImage.Width * 1.5;

            passOrFailImg.Source = passBitmapImage;

        }

        private bool IsAdjust(MESTestProcess process)
        {
            if (process == MESTestProcess.Adjust || process == MESTestProcess.PreAdjust)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// PM1扫描ackground dowork执行结束后函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            //开始计算，处理PM2数据
            /*string errMsg = "";
            if(e.Result!=null)
                errMsg = e.Result.ToString();*/
            if (scanErrorMsg.Length > 0)
                RealtimeMsg("扫描出错:" + scanErrorMsg);
            else
            {
                RealtimeMsg("扫描结束！");
                if (GetIsScanFinished())
                {
                    //先进行pm1 PDL归零，再进行noPDL归零，接下来才是pm2 PDL和noPDL归零
                    if (scanDetailInfo.ScanType == SCANTYPE.RefWithNoPDL && scanDetailInfo.Port % 2 != 0)
                    {
                        //显示归零曲线
                        double[][] scanRes = null;
                        scanRes = portNoPDLRef[scanDetailInfo.Port - 1];
                        List<int> scanPorts = new List<int>();
                        scanPorts.Add(scanDetailInfo.Port);
                        string errMsg = "";
                        if (!IsScanRef(scanPorts, ref errMsg))
                        {
                            refSysLEDs[scanDetailInfo.Port - 1].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        }
                        else
                            refSysLEDs[scanDetailInfo.Port - 1].Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));

                        //处理数据
                        if (scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
                        {
                            curveShow.UpdateScanCurve(scanDetailInfo.Port, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                        }
                        if (Port24Ref())
                            return;
                    }
                }
            }

            ScanFinish(scanDetailInfo);
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
                scanDetailInfo = (ScanDetail)e.Argument;
                scanErrorMsg = "";
                int res = ScanAndCalResult(scanDetailInfo, ref scanErrorMsg);
                SetIsScanFinished(true);
                if (scanErrorMsg.Length > 0 || res != 0)
                {
                    string errMsg = "";
                    //清除测试结果
                    ClearResult(scanDetailInfo.Port, ref errMsg);
                    ClearResult(scanDetailInfo.Port + 1, ref errMsg);
                    if (res == 1)
                    {
                        ReconnectServer(ref errMsg);
                    }
                    return;
                }
                else
                {
                    if (GetOpenTemplateComplete())
                    {
                        //int port1 = pm1ScanInfo.Port;
                        //int port2 = pm1ScanInfo.Port + 1;
                        string errMsg = "";
                        //计算当前通道的参数结果。
                        //CalAllTestResult(pm1ScanInfo.Port, true, ref errMsg);
                        CalAllResultInThread();
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

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            powermeterRealtimeBK.CancelAsync();
            refTimeCheckBK.CancelAsync();
        }

        /// <summary>
        /// 获取端口号，根据选择的1/2、3/4，是功率计1，还是2返回端口号。由于打开模板太慢，所以用界面选择来做
        /// </summary>
        /// <param name="isPM1">是否是功率计1</param>
        /// <returns>端口号</returns>
        private int GetCurPort(bool isPM1)
        {
            if (isPort12Select)
            {
                if (isPM1)
                    return 1;
                else
                    return 2;
            }
            else
            {
                if (isPM1)
                    return 3;
                else
                    return 4;
            }
        }

        /// <summary>
        /// 获取光开关切换flag
        /// </summary>
        /// <param name="isScan">是否是扫描</param>
        /// <returns>切换开关flag，与指令配置文件匹配</returns>
        private string GetSwitchFlag(bool isScan)
        {
            string switchFlag = "";
            int port = GetCurPort(true);

            if (isScan)
                switchFlag = "::PORT" + port.ToString() + ":SCAN";
            else
                switchFlag = "::PORT" + port.ToString() + ":ADJUST";

            return switchFlag;
        }


        /// <summary>
        /// 功率计归零函数
        /// </summary>
        /// <param name="pwmIndex">功率计index，1或者2</param>
        private void PowermeterRef(int pwmIndex)
        {
            //根据功率计index，和1/2、 3/4端口选择，获取端口号，是1、2、3还是4

            //只有调节工序才需要用到功率计
            if (!IsAdjust(testProcess))
                return;
            isPort12Select = uiVariable.IsPort12;
            int port = GetCurPort(true);
            if (pwmIndex % 2 == 0)
                port = GetCurPort(false);

            SetSwitch(false);
            powermeterRealtimeBK.CancelAsync();
            IPowermeter pm = null;
            int channel = 0;
            string errMsg = "";
            DeviceControl.GetPowermeterByIndex(pwmIndex, ref channel, ref pm, ref errMsg);
            string prompt = "";
            if (pm == null)
            {
                MessageBox.Show("未连接功率计，请重新配置设备！");
            }
            else
            {
                prompt = string.Format("进行PORT{0}功率计归零，请确认将光源线放入PORT{1}对应功率计!", port, port);
                if (MessageBox.Show(prompt, "功率计归零", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    RealtimeMsg(prompt);
                    List<double> powerAvgs = null;
                    pm.ReadPowerAvg(ref errMsg, out powerAvgs, 1, false, channel.ToString());
                    if (powerAvgs.Count > 0)
                    {
                        if (powerAvgs[0] < -25)
                        {
                            //报光太弱错误，
                            RealtimeMsg("功率计归零光太弱（<-25db），请检查光路");
                            ref1830LEDs[port - 1].Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                            port1830Ref[port - 1] = CommonFunction.GetDefaultValue();
                        }
                        else
                        {
                            port1830Ref[port - 1] = powerAvgs[0];
                            ref1830LEDs[port - 1].Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                            //记录1830归零数据

                        }
                        Save1830Ref(ref errMsg);
                    }

                }
                if (!powermeterRealtimeBK.IsBusy)
                    powermeterRealtimeBK.RunWorkerAsync();
            }
        }

        /// <summary>
        /// 扫描归零函数
        /// </summary>
        /// <param name="pwmIndex">功率计index，1或者2</param>
        private bool ScanRef(int pwmIndex)
        {
            //根据功率计index，和1/2、 3/4端口选择，获取端口号，是1、2、3还是4
            int port = GetCurPort(true);
            if (pwmIndex % 2 == 0)
                port = GetCurPort(false);

            string prompt = string.Format("进行PORT{0}系统归零，请确认COM->PORT{1}对接!", port, port);
            if (MessageBox.Show(prompt, "系统归零", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //重新归零，先删除归零数据
                string pdlRefPath = refWithPDLFile + port.ToString() + ".csv";
                string noPDLRefPath = refWithNoPDLFile + port.ToString() + ".csv";
                if (File.Exists(pdlRefPath))
                    File.Delete(pdlRefPath);

                if (File.Exists(noPDLRefPath))
                    File.Delete(noPDLRefPath);
                uiVariable.IsEnable = false;
                uiVariable.IsAdjustScanEnable = false;
                uiVariable.IsSaveEnable = false;
                SetSwitch(true);
                if (GetIsScanFinished())
                {
                    SetIsScanFinished(false);
                    RealtimeMsg(prompt);
                    BackgroundWorker bkPM = new BackgroundWorker();
                    bkPM.DoWork += Scan_DoWork;
                    bkPM.RunWorkerCompleted += Scan_RunWorkerCompleted;
                    scanDetailInfo.ScanType = SCANTYPE.RefWithPDL;
                    scanDetailInfo.Port = port;
                    bkPM.RunWorkerAsync(scanDetailInfo);
                }
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// 是否取消
        /// </summary>
        /// <returns>false--取消归零  true--开始归零</returns>
        private bool Port24Ref()
        {
            PowermeterRef(2);
            return ScanRef(2);
        }

        /// <summary>
        /// 是否取消
        /// </summary>
        /// <returns>false--取消归零  true--开始归零</returns>
        private bool Port13Ref()
        {
            
            PowermeterRef(1);
            return ScanRef(1);
        }

        /// <summary>
        /// 扫描按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScanRef_Click(object sender, RoutedEventArgs e)
        {            
            isPort12Select = uiVariable.IsPort12;
            if (!Port13Ref())
            {
                Port24Ref();
            }
        }


        /// <summary>
        /// 读取1830归零数据
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void Read1830Ref(ref string errMsg)
        {
            try
            {
                string path = ref1830File;
                FileStream stream = File.Open(path, FileMode.Open);
                StreamReader reader = new StreamReader(stream);
                string line = reader.ReadLine();
                while (line != null)
                {
                    string[] splits = line.Split(',');
                    if (splits.Length == 3)
                    {
                        int port = Convert.ToInt32(splits[0]) - 1;
                        DateTime time = DateTime.Parse(splits[2]);

                        TimeSpan span = DateTime.Now - time;
                        //小于4小时的归零数据才读
                        if (!IsRefTimePassdue(span))
                        {
                            port1830Ref[port] = Convert.ToDouble(splits[1]);
                            if (port1830Ref[port].CompareTo(CommonFunction.GetDefaultValue()) != 0)
                            {
                                ref1830Times[port] = time;
                                ref1830LEDs[port].Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                            }
                        }

                    }
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
            }
        }

        /// <summary>
        /// 保存1830归零数据
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void Save1830Ref(ref string errMsg)
        {
            try
            {
                string path = ref1830File;
                if (File.Exists(path))
                    File.Delete(path);

                FileStream stream = File.Open(path, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                for (int i = 0; i < cstPortCount; i++)
                {
                    if (port1830Ref[i].CompareTo(CommonFunction.GetDefaultValue()) != 0)
                    {
                        string refData = string.Format("{0},{1},{2}\n", i + 1, port1830Ref[i], DateTime.Now.ToString());
                        writer.WriteLine(refData);
                    }
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
            }
        }

        /// <summary>
        /// 停止扫描按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopScan_Click(object sender, RoutedEventArgs e)
        {
            isStopScan = true;
            //SetSwitch(false);
            uiVariable.IsStopScanVisible = Visibility.Hidden;
            uiVariable.IsAdjustScanVisible = Visibility.Visible;
            //uiVariable.IsEnable = true;
        }

        /// <summary>
        /// 12端口被选中响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkPort12_Checked(object sender, RoutedEventArgs e)
        {
            if (GetIsScanFinished())
            {
                isPort12Select = uiVariable.IsPort12;
                SetSwitch(false);
            }
        }

        /// <summary>
        /// 34端口被选中响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkPort34_Checked(object sender, RoutedEventArgs e)
        {
            if (GetIsScanFinished())
            {
                isPort12Select = uiVariable.IsPort12;
                SetSwitch(false);
            }
        }

        /// <summary>
        /// 检测照光CRC文件是否存在，存在说明照光完成
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true-文件存在，false-文件不存在</returns>
        private bool LightedCRCExist(ref string errMsg)
        {
            try
            {
                string crcPath = lightDataDir + uiVariable.SN + "-CRC.dat";
                FileInfo info = new FileInfo(crcPath);
                if (info.Exists)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                     + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                RealtimeMsg(errMsg);
                return false;
            }
        }



        /// <summary>
        /// 将rawdata数据写到给定的文件夹下
        /// </summary>
        /// <param name="bLighted">照光前还是后保存rawdata</param>
        /// <param name="errMsg">出粗信息</param>
        private void WriteRawdataToNet(bool bLighted, ref string errMsg)
        {
            try
            {
                if (scanPowermeterCount == 0)
                    return;
                string rawdataSavePath = rawdataNetPath + testProcess.GetAdditional();
                if (!Directory.Exists(rawdataSavePath))
                {
                    Directory.CreateDirectory(rawdataSavePath);
                }
                if(IsAdjust(testProcess))
                {
                    if (bLighted)
                    {
                        rawdataSavePath += "\\" + uiVariable.SN + "-UV后.csv";
                    }
                    else
                    {
                        rawdataSavePath += "\\" + uiVariable.SN + "-UV前.csv";
                    }
                }
                else
                {
                    rawdataSavePath += "\\" + uiVariable.SN + ".csv";
                }
                
                FileStream stream = File.Open(rawdataSavePath, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);

                string title = "GHZ";
                int dataLens = portResData[0][0].Length;

                if (convertAlgorithm.ToUpper() == ConvertAlgorithm.Ave.GetAdditional().ToUpper())
                {
                    //写title
                    for (int i = 0; i < scanPowermeterCount; i++)
                    {
                        title += ",AVE-" + (i + 1).ToString();
                    }
                    writer.WriteLine(title);

                    //写数据
                    for (int i = 0; i < dataLens; i++)
                    {
                        string line = string.Format("{0:N2}", portResData[0][5][i]).Replace(",", "");
                        for (int j = 0; j < scanPowermeterCount; j++)
                        {
                            line += ",";
                            line += string.Format("{0:N2}", portResData[j][1][i]).Replace(",", "");
                        }
                        writer.WriteLine(line);
                    }
                }
                else
                {
                    for (int i = 0; i < scanPowermeterCount; i++)
                    {
                        title += ",AVE-" + (i + 1).ToString();
                        title += ",MAX-" + (i + 1).ToString();
                        title += ",MIN-" + (i + 1).ToString();
                    }
                    writer.WriteLine(title);

                    for (int i = 0; i < dataLens; i++)
                    {
                        string line = string.Format("{0}", portResData[0][5][i]);
                        for (int j = 0; j < scanPowermeterCount; j++)
                        {
                            line += string.Format(",{0},{1},{2}", portResData[j][1][i], portResData[j][3][i], portResData[j][4][i]);
                        }
                        writer.WriteLine(line);
                    }
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                errMsg += "写rawdata文件出错:" + ex.Message + "\r";
                return;
            }
        }

        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            if (GetOpenTemplateComplete() && isBeginPDLScan)
            {
                List<AMTSRawdata> rawdatas = new List<AMTSRawdata>();
                for (int j = 0; j < portAssistant.Count; j++)
                {                   
                    AMTSRawdata data = new AMTSRawdata();
                    data.PortName = portAssistant[j].Name;
                    data.Temperature = portAssistant[j].TestTmpt;
                    data.Rawdata = portAssistant[j].Rawdata;
                    rawdatas.Add(data);                  
                }
                string errMsg = "";
                if (isBeginLight)
                {
                    if (LightedCRCExist(ref errMsg))
                    {
                        
                        if (allProductControl[0].SaveDataToAMTS(uiVariable.SN, amtsSaveUrl, ref errMsg, true,MESRawdataType.MemsVOA, rawdatas) != 0)
                        {
                            ErrorBox(errMsg);
                        }
                        else
                        {
                            isTestedUnSave = false;
                            ClearAllResult(ref errMsg);
                            isBeginPDLScan = false;
                            isBeginLight = false;
                            uiVariable.SN = "";
                            UpdateParamList();
                            uiVariable.IsSaveEnable = false;
                        }
                        //无需写数据到网盘
                        /*WriteRawdataToNet(true, ref errMsg);
                        if (errMsg.Length > 0)
                        {
                            WarningBox(errMsg);
                        }*/
                    }
                    else
                    {
                        WarningBox("请等照光完成后再保存数据！");
                    }
                }
                else
                {
                    if (allProductControl[0].SaveDataToAMTS(uiVariable.SN, amtsSaveUrl, ref errMsg,false, MESRawdataType.MemsVOA, rawdatas) != 0)
                    {
                        ErrorBox(errMsg);
                    }
                    else
                    {
                        isTestedUnSave = false;
                        ClearAllResult(ref errMsg);
                        isBeginPDLScan = false;
                        UpdateParamList();
                        if (IsAdjust(testProcess))
                        {
                            uiVariable.IsLightedEnable = true;
                        }
                        uiVariable.IsSaveEnable = false;
                    }

                    /*WriteRawdataToNet(false, ref errMsg);
                    if (errMsg.Length > 0)
                    {
                        WarningBox(errMsg);
                    }*/
                }
            }
        }

        private void btnUVata_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //写SN文件，启动照光程序
                string snPath = System.Environment.CurrentDirectory + "\\sn.txt";
                if(File.Exists(snPath))
                {
                    File.Delete(snPath);
                }
                
                string crcPath = lightDataDir + uiVariable.SN + "-CRC.dat";
                if (File.Exists(crcPath))
                {
                    File.Delete(crcPath);
                }
                CommonFunction.WriteFileASCII(snPath, uiVariable.SN);

                //启动照光程序
                ProcessStartInfo info = new ProcessStartInfo();
                info.WindowStyle = ProcessWindowStyle.Normal;
                info.FileName = System.Environment.CurrentDirectory + "\\light.exe";//需要启动的程序
                Process.Start(info);
                isBeginLight = true;
                isBeginPDLScan = false;
                uiVariable.IsSaveEnable = false;

            }
            catch (Exception ex)
            {

                string errMsg = "启动紫外照光 error:" + ex.Message + "\r";
                RealtimeMsg(errMsg);
                isBeginLight = false;
            }
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

        /// <summary>
        /// 与界面测试状态绑定
        /// </summary>
        private string testStatus;
        public string TestStatus
        {
            get
            {
                return testStatus;
            }
            set
            {
                testStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TestStatus"));
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

        private bool isEnable;
        public bool IsEnable
        {
            get
            {
                return isEnable;
            }
            set
            {
                isEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnable"));
            }
        }

        private bool isLightedEnable;
        public bool IsLightedEnable
        {
            get
            {
                return isLightedEnable;
            }
            set
            {
                isLightedEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsLightedEnable"));
            }
        }


        private Visibility isAdjustScanVisible;
        public Visibility IsAdjustScanVisible
        {
            get
            {
                return isAdjustScanVisible;
            }
            set
            {
                isAdjustScanVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsAdjustScanVisible"));
            }
        }

        private bool isAdjustScanEnable;
        public bool IsAdjustScanEnable
        {
            get
            {
                return isAdjustScanEnable;
            }
            set
            {
                isAdjustScanEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsAdjustScanEnable"));
            }
        }

        private Visibility isStopScanVisible;
        public Visibility IsStopScanVisible
        {
            get
            {
                return isStopScanVisible;
            }
            set
            {
                isStopScanVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsStopScanVisible"));
            }
        }

        private bool isPort12;
        public bool IsPort12
        {
            get
            {
                return isPort12;
            }
            set
            {
                isPort12 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPort12"));
            }
        }

        private bool isPort34;
        public bool IsPort34
        {
            get
            {
                return isPort34;
            }
            set
            {
                isPort34 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPort34"));
            }
        }


    }
}
