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

namespace UIOperateInterleaverMaterialTest
{
    /// <summary>
    /// Interaction logic for UIOperateInterleaverMaterialTest.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateInterleaverMaterialTest")]
    public partial class OperateInterleaverMaterialTest : UserControl
    {
        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

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
        /// 归零时间确认后台线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;
        
        /// <summary>
        /// 1830归零值
        /// </summary>
       // private double port1830Ref = CommonFunction.GetDefaultValue();

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
        /// 扫描、归零文件路径
        /// </summary>
        private string refWithNoPDLFile = "\\reference\\referenceWithNoPDLPort";
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
        /// 扫描是否结束
        /// </summary>
        private bool isScanFinished = true;

        /// <summary>
        /// 存放特殊显示中的MinISO值
        /// </summary>
        private Dictionary<string, double> portMinISODic = new Dictionary<string, double>();
        
        /// <summary>
        /// 功率计实时功率值
        /// </summary>
        private List<double> realtimePowers = new List<double>();
        
        /// <summary>
        /// 端口数量
        /// </summary>
        //private const int cstPortCount = 4;

        /// <summary>
        /// PDL数量
        /// </summary>
        private const int cstPDLCount = 4;

        /// <summary>
        /// 功率计1 对应端口ISO 有效带宽内所有通道的结果，用于界面显示 0--结果 1--波长
        /// </summary>
        private List<List<double>> pm1ISOResult = null;

        /// <summary>
        /// 功率计2 对应端口ISO 有效带宽内所有通道的结果，用于界面显示 0--结果 1--波长
        /// </summary>
        private List<List<double>> pm2ISOResult = null;

        private const int rerefHours = 6;

        /// <summary>
        /// rawdata保存数据路径
        /// </summary>
        //private string rawdataNetPath = "\\\\zh-mfs-srv.oplink.com.cn\\share\\WS8DataEtalon\\Interleaver\\关单前数据\\50G Interleaver\\Alignment_Data\\";
        /// <summary>
        /// 四个通道归零时间
        /// </summary>
       // private DateTime refTimes;
        
        private DateTime ref1830Times;
        
        /// <summary>
        /// 最老的归零时间，用于4小时归零倒计时
        /// </summary>
        //private DateTime oldestRefTime = new DateTime();

        //private string convertAlgorithm = ConvertAlgorithm.Mueller.GetAdditional();

        /// <summary>
        /// ISO曲线对应的提取数据的参数名称
        /// </summary>
        //private string curveAdjParamName = "";
        
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
        /// 是否PDL扫描
        /// </summary>
        private bool isBeginPDLScan = false;

        /// <summary>
        /// 特殊显示shift值和中心频率对应保存dic
        /// </summary>
        private Dictionary<double, double> specialShiftsDic = new Dictionary<double, double>();
        
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
        
        private Dictionary<string, string> portAndNameDic = new Dictionary<string, string>();

        private double LIGHT_REPORT = 299792458;

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

        public OperateInterleaverMaterialTest()
        {
            InitializeComponent();

            uiVariable.IsEnable = true;
            uiVariable.IsSaveEnable = false;
            txtBoxSN.DataContext = uiVariable;

            btnScanRef.DataContext = uiVariable;
            btnAdjustScan.DataContext = uiVariable;
            uiVariable.IsAdjustScanEnable = true;
            uiVariable.IsStopScanVisible = Visibility.Hidden;

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;

            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());

            portNoPDLRef = new List<double[][]>(1);
            portResData = new List<double[][]>(1);
            pdlRawData = new List<double[][]>(cstPDLCount);

            portNoPDLRef.Add(new double[3][]);
            portResData.Add(new double[6][]);

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

            uiVariable.SN = "";
            uiVariable.Path = "";
            txtBoxSN.Focus();
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
            if (refSpan.TotalMinutes > rerefHours * 60)
                return true;
            return false;
        }

        private void RefTimeCheck_Progress(object sender, ProgressChangedEventArgs e)
        {
            DateTime curTime = DateTime.Now;
            DateTime defaultTime = new DateTime();
            string prompt1830 = "端口1";
            bool bPastDue = false;

            if (!ref1830Times.Equals(defaultTime))
            {
                TimeSpan refSpan = curTime - ref1830Times;
                //归零数据超过六个小时，删除
                if (IsRefTimePassdue(refSpan))
                {
                    //port1830Ref = CommonFunction.GetDefaultValue();
                   //ref1830Times = new DateTime();
                    bPastDue = true;
                }
            }
            else
                bPastDue = true;

            //prompt1830 += "功率计归零数据过期，需重新归零！";
            if (bPastDue)
            {
                //WarningBox(prompt1830);
                uiVariable.IsAdjustScanEnable  = false;
            }////////////////////////

            TimeSpan span = curTime - ref1830Times;
            string timeShow = string.Format("{0}:{1}:{2}", span.Days * 24 + span.Hours, span.Minutes, span.Seconds);
            txtRefTime.Text = timeShow;
            if (span.TotalMinutes > (rerefHours - 0.5) * 60)
                txtRefTime.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            else
                txtRefTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
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
           
            curveShow = new InterleaverCurve(EventAggregator);
            paramCal = new InterleaverParamCal(algorithm);

            string curDir = System.Environment.CurrentDirectory;
            refWithNoPDLFile = curDir + refWithNoPDLFile;
            scanWithNoPDLFile = curDir + scanWithNoPDLFile;
            ref1830File = curDir + ref1830File;

            //曲线显示初始化
            curveShow.InitAllCurve();
            
            IInterleaverScan scan = null;
            string errMsg = "";
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                scanPowermeterCount = scan.PowermeterCount();
            }
            
            errMsg = "";
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
        
        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\..\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }
        
        //声明整个方法为线程同步
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
        /// 切换光开关
        /// </summary>
        /// <param name="isScan">是否是扫描</param>
        private void SetSwitch(bool isScan)
        {
            string flag = GetSwitchFlag(isScan);
            lst_Msg.Items.Add ("开始切换开关");
            lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
            string errMsg = "";
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType("InterleaverSwitch", ref opticalSwitch, ref errMsg) == 0)
            {
                if (opticalSwitch != null)
                {
                    if (opticalSwitch.SetSwitch(flag, ref errMsg) == 0)
                    {
                        lst_Msg.Items.Add("切换开关成功！");
                    }
                }
            }
            if (errMsg.Length > 0)
            {
                lst_Msg.Items.Add("切换开关失败:" + errMsg);
                lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
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
        private int DoScan(SCANTYPE scanType, ref string resPath, ref string errMsg)
        {
            IInterleaverScan scan = null;
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                if (scanType == SCANTYPE.RefWithNoPDL)
                {
                    resPath = scanWithNoPDLFile;
                    return scan.Scan(false, true, ref resPath, ref errMsg);
                }
                else if (scanType == SCANTYPE.TestWithNoPDL)
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
                lst_Msg.Items.Add("开始扫描。。。");
                lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                BackgroundWorker bkScan = new BackgroundWorker();
                bkScan.DoWork += Scan_DoWork;
                bkScan.RunWorkerCompleted += Scan_RunWorkerCompleted;
                ScanType = scanType;
                bkScan.RunWorkerAsync(scanType);
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
                bool isRefSuccess;
                string noPDLRefPath = refWithNoPDLFile + "1.csv";
                //读取NoPDL的归零数据
                int noPDLRef = InterleaverScanResult.ReadScanData(noPDLRefPath, portNoPDLRef[0], ref errMsg);
                if (noPDLRef == 0)
                {
                    isRefSuccess = true;
                }
                else
                {
                    isRefSuccess = false;
                }

                ReadRefTime();

                bool bReadSuccess = false;
                string strPrompt = "端口1";
                if (isRefSuccess)
                {
                    strPrompt += "  ";
                    bReadSuccess = true;
                    //显示归零数据
                    double[][] scanRes = null;
                    scanRes = portNoPDLRef[0];
                    if (scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
                    {
                        curveShow.UpdateScanCurve(1, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                    }
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
        private bool IsScanRef(ref string errMsg)
        {
            errMsg = "端口";
            if (portNoPDLRef[0][0] == null || portNoPDLRef[0][0].Length <= 0 || portNoPDLRef[0][0][0].CompareTo(0) <= 0)
            {
                errMsg = errMsg + " " + 1.ToString();
                errMsg += "未归零!";
                return false;
            }

            return true;
        }

        private void btnAdjustScan_Click(object sender, RoutedEventArgs e)
        {
            if (txtBoxSN.Text == "" && txt_path.Text == "")
            {
                MessageBox.Show("请完善SN和文件路径信息，谢谢！");
                return;
            }

            uiVariable.Path = txt_path.Text;
            if (!Directory.Exists(uiVariable.Path))
                Directory.CreateDirectory(uiVariable.Path);
            string errMsg = "";

            if (!IsScanRef(ref errMsg))
            {
                WarningBox(errMsg);
                return;
            }

            isStopScan = false;
            isBeginPDLScan = false;
            uiVariable.IsEnable = false;
            uiVariable.IsAdjustScanEnable = false;
            uiVariable.IsSaveEnable = false;
            SetSwitch(true);
            ScanType = SCANTYPE.TestWithNoPDL;
            DoScanOnBK(ScanType);
        }

        /// <summary>
        /// 扫描结束后处理
        /// </summary>
        /// <param name="scanInfo">扫描类型等信息</param>
        private void ScanFinish(SCANTYPE scanType)
        {
            double[][] scanRes = null;

            if (scanType == SCANTYPE.RefWithNoPDL )
            {
                scanRes = portNoPDLRef[0];
            }
            else
            {
                scanRes = portResData[0];
            }
           
            if (scanRes != null && scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
            {
                curveShow.UpdateScanCurve(1, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
               
                ChangeData(scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                txt_MaxIL.Text = scanRes[1].Max().ToString();
                txt_MinIL.Text = scanRes[1].Min().ToString();
                txt_Differ.Text = (scanRes[1].Max() - scanRes[1].Min()).ToString();
                MessageBox.Show("扫描完成！");
            }
            
            SetSwitch(false);

            uiVariable.IsEnable = true;
        }

        /// <summary>
        /// 扫描并读取返回的结果
        /// </summary>
        /// <param name="scanInfo">扫描信息，是否带PDL，归零还是测试，归零通道等具体信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        private int ScanAndCalResult(SCANTYPE scanType, ref string errMsg)
        {
            try
            {
                string resPath = "";
                int res = 0;
                try
                {
                    res = DoScan(scanType, ref resPath, ref errMsg);
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

                if (scanType == SCANTYPE.TestWithNoPDL)
                {
                    InterleaverScanResult.InitRawdataBuffer(portResData[0]);
                    if (scanType == SCANTYPE.TestWithNoPDL)
                    {
                        resPath = scanWithNoPDLFile + "1.csv";
                        InterleaverScanResult.ReadScanData(resPath, pdlRawData[0], ref errMsg);
                        InterleaverScanResult.CalRawdataByNoPDL(pdlRawData, portNoPDLRef[0], portResData[0], ref errMsg);
                    }
                    return 0;
                }

                if (ScanType == SCANTYPE.RefWithNoPDL)
                {
                    string pLocalFilePath = scanWithNoPDLFile + "1.csv";
                    string pSaveFilePath = refWithNoPDLFile + "1.csv";
                    if (File.Exists(pLocalFilePath))//必须判断要复制的文件是否存在
                    {
                        File.Copy(pLocalFilePath, pSaveFilePath, true);//三个参数分别是源文件路径，存储路径，若存储路径有相同文件是否替换
                    }

                    //读取NoPDL的归零数据
                    InterleaverScanResult.ReadScanData(pSaveFilePath, portNoPDLRef[0], ref errMsg);
                    if (InterleaverScanResult.CheckRefRight(portNoPDLRef[0], ref errMsg) != 0)
                        return 2;
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
                var typeName = algorithm.GetType();
                IInterleaverAlgorithm interleaverAlgorithm = (IInterleaverAlgorithm)Activator.CreateInstance(typeName);
                InterleaverParamCal calFuntion = new InterleaverParamCal(interleaverAlgorithm);
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }
        
        /// <summary>
        /// 计算port参数
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void CalPortRes(ref string errMsg)
        {
            try
            {
                portMinISODic.Clear();
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        private SCANTYPE ScanType = SCANTYPE.RefWithNoPDL;
        /// <summary>
        /// PM1扫描ackground dowork执行结束后函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //开始计算，处理PM2数据
            if (scanErrorMsg.Length > 0)
            {
                lst_Msg.Items.Add("扫描出错:" + scanErrorMsg);
                lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                if (ScanType == SCANTYPE.TestWithNoPDL)
                {
                    uiVariable.IsAdjustScanEnable = true;
                }
            }
            else
            {
                lst_Msg.Items.Add("扫描结束！");
                lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                if (GetIsScanFinished())
                {
                    txt_MaxIL.Text = uiVariable.Max;
                    txt_MinIL.Text = uiVariable.Min;
                    txt_Differ.Text = uiVariable.Differ;
                    //先进行pm1 PDL归零，再进行noPDL归零，接下来才是pm2 PDL和noPDL归零
                    if (ScanType == SCANTYPE.RefWithNoPDL)
                    {
                        //显示归零曲线
                        double[][] scanRes = null;
                        scanRes = portNoPDLRef[0];
                        List<int> scanPorts = new List<int>();
                        scanPorts.Add(1);
                        
                        //处理数据
                        if (scanRes[scanRes.Length - 1] != null && scanRes[1] != null)
                        {
                            curveShow.UpdateScanCurve(1, scanRes[scanRes.Length - 1].ToList(), scanRes[1].ToList());
                        }
                        
                        SaveRefTime();
                        ReadRefTime();
                    }
                    uiVariable.IsAdjustScanEnable = true;
                }
            }

            ScanFinish(ScanType);
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
                SCANTYPE scanType = (SCANTYPE)e.Argument;
                scanErrorMsg = "";
                int res = ScanAndCalResult(scanType, ref scanErrorMsg);
                SetIsScanFinished(true);
                if (scanErrorMsg.Length > 0 || res != 0)
                {
                    string errMsg = "";
                    //清除测试结果
                    ClearResult(1, ref errMsg);
                    if (res == 1)
                    {
                        ReconnectServer(ref errMsg);
                    }
                    return;
                }
                else
                {
                    CalAllResultInThread();
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
            refTimeCheckBK.CancelAsync();
        }
        
        /// <summary>
        /// 获取光开关切换flag
        /// </summary>
        /// <param name="isScan">是否是扫描</param>
        /// <returns>切换开关flag，与指令配置文件匹配</returns>
        private string GetSwitchFlag(bool isScan)
        {
            string switchFlag = "";
            int port =1;

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
            int port = 1;

            SetSwitch(false);
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
                    lst_Msg.Items.Add(prompt);
                    lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                    List<double> powerAvgs = null;
                    pm.ReadPowerAvg(ref errMsg, out powerAvgs, 1, false, channel.ToString());
                    if (powerAvgs.Count > 0)
                    {
                        if (powerAvgs[0] < -25)
                        {
                            //报光太弱错误，
                            lst_Msg.Items.Add("功率计归零光太弱（<-25db），请检查光路");
                            lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                           // port1830Ref = CommonFunction.GetDefaultValue();
                        }
                        else
                        {
                            //port1830Ref = powerAvgs[0];
                        }
                        SaveRefTime();
                    }
                }
            }
        }

        /// <summary>
        /// 扫描归零函数
        /// </summary>
        /// <param name="pwmIndex">功率计index，1或者2</param>
        private bool ScanRef(int pwmIndex)
        {
            int port = 1;
            if (pwmIndex % 2 == 0)
                port = 2;

            string prompt = string.Format("进行PORT{0}系统归零，请确认COM->PORT{1}对接!", port, port);
            if (MessageBox.Show(prompt, "系统归零", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                string noPDLRefPath = refWithNoPDLFile + port.ToString() + ".csv";

                if (File.Exists(noPDLRefPath))
                    File.Delete(noPDLRefPath);
                
                SetSwitch(true);
                if (GetIsScanFinished())
                {
                    SetIsScanFinished(false);
                    lst_Msg.Items.Add(prompt);
                    lst_Msg.ScrollIntoView(lst_Msg.Items[lst_Msg.Items.Count - 1]);
                    BackgroundWorker bkPM = new BackgroundWorker();
                    bkPM.DoWork += Scan_DoWork;
                    bkPM.RunWorkerCompleted += Scan_RunWorkerCompleted;
                    ScanType = SCANTYPE.RefWithNoPDL;
                    bkPM.RunWorkerAsync(ScanType);
                }
            }
            else
                return false;
            return true;
        }
        
        /// <summary>
        /// 扫描按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScanRef_Click(object sender, RoutedEventArgs e)
        {
            if (txtBoxSN.Text == "" && txt_path.Text == "")
            {
                MessageBox.Show("请完善SN和文件路径信息，谢谢！");
                return;
            }
            uiVariable.Path = txt_path.Text;
            if (!Directory.Exists(uiVariable.Path))
                Directory.CreateDirectory(uiVariable.Path);
            uiVariable.IsEnable = false;
            uiVariable.IsAdjustScanEnable = false;
            uiVariable.IsSaveEnable = false;

            PowermeterRef(1);
            ScanRef(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void ReadRefTime()
        {
            try
            {
                using (StreamReader reader = new StreamReader(ref1830File))
                {
                    string line = reader.ReadLine();
                    DateTime time = Convert.ToDateTime(line);
                    ref1830Times = time;
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                string errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                System.Windows.Forms.MessageBox.Show(errMsg);
            }
        }

        /// <summary>
        /// 保存1830归零数据
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        private void SaveRefTime()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(ref1830File, false, Encoding.Default))
                {
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
               string errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                System.Windows.Forms.MessageBox.Show(errMsg);
            }
        }
        
        private void btn_Scan_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    txt_path.Text = dlg.SelectedPath;
                    uiVariable.Path = txt_path.Text;
                    if (!Directory.Exists(uiVariable.Path))
                        Directory.CreateDirectory(uiVariable.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("创建文件夹" + uiVariable.Path + "失败");
                    return;
                }
            }
        }
        
        private void ChangeData(List<double> wl,List <double >power)
        {
            try
            {
                if (wl.Count == power.Count)
                {
                    wl.Reverse();
                    power.Reverse();
                    string strDist = uiVariable.Path + "\\" + uiVariable.SN + ".txt";
                    ChangeSrcData( wl, power, strDist);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void ChangeSrcData( List<double > wl,List <double >power,string strDst)//string strSrc, string strDst)
        {
            List<string> strTemplateList = new List<string>();
          
            FileInfo fi = new FileInfo(strDst);
            if (fi.Exists)
            {
                fi.Delete();
            }

            using (FileStream fs = new FileStream(strDst, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default))
                {
                    for (int i = 0; i < wl.Count; i++)
                    {
                        if (wl[i] <= 190000)
                        {
                            break;
                        }
                        sw.WriteLine(wl[i]+ "\t" + power[i]);
                    }
                }
            }
        }
    }


    public class UIVariable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
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

        private string path;
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Path"));
            }
        }

        private string max;
        public string Max
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Max"));
            }
        }
        private string min;
        public string Min
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Min"));
            }
        }

        private string differ;
        public string Differ
        {
            get
            {
                return differ;
            }
            set
            {
                differ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Differ"));
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
    }
}
