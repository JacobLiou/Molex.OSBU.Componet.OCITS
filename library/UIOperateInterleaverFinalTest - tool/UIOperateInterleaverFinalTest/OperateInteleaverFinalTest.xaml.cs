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

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;
using MolexUtility.UIList;
using Microsoft.Office.Interop.Excel;


namespace UIOperateInterleaverFinalTest
{
    /// <summary>
    /// Interaction logic for OperateInteleaverFinalTest.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateInterleaverFinalTest")]
    public partial class OperateInteleaverFinalTest : UserControl
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
        /// 模板获取到的最小扫描频率
        /// </summary>
        private double minScanFre = 2000000.0;

        /// <summary>
        /// 模板获取到的最大扫描频率
        /// </summary>
        private double maxScanFre = -2000000.0;

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
        private MESControl testItemShow = null;

        /// <summary>
        /// 所有产品测试信息
        /// </summary>
        private List<MESControl> allProductControl;

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
            allProductControl = new List<MESControl>();
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
            testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), mainInfo.TestProcess, true);
            templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), mainInfo.TemplateType, true);

            curveShow = new InterleaverFinalTestCurve(EventAggregator);
            paramCal = new ParamCal(algorithm);

            string curDir = System.Environment.CurrentDirectory;
            refWithPDLFile = curDir + refWithPDLFile;            
            scanWithPDLFile = curDir + scanWithPDLFile;

            IInterleaverScan scan = null;
            string errMsg = "";
            DeviceControl.GetInterleaverScanByFlag(1, ref scan, ref errMsg);
            if (scan != null)
            {
                scanPowermeterCount = scan.PowermeterCount();
            }
            refTimeCheckBK.RunWorkerAsync();
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
            //MessageBox.Show(warning, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 错误提示
        /// </summary>
        /// <param name="error">错误信息</param>
        private void ErrorBox(string error)
        {
            //MessageBox.Show(error, "出错", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private List<string> snList = new List<string>();
        private int snIndex = -1;
        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            if(snIndex==-1)
            {
                Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
                Workbooks wbks = app.Workbooks;
                string path = System.Environment.CurrentDirectory + "\\sn.csv";
                Workbook wbk = wbks.Add(path);
                Worksheet wsh = (Worksheet)wbk.Sheets[1];
                long rowCount = wsh.UsedRange.Rows.Count;
                object[,] dataContents = new object[rowCount+1, 1];
                dataContents = wsh.Range[wsh.Cells[1, 1], wsh.Cells[rowCount+1, 1]].Value2;
                for(int i=1;i<= rowCount;i++)
                {
                    snList.Add(dataContents[i, 1].ToString());
                }
                wbk.Close();
                wbks.Close();
                app.Quit();
                //释放掉多余的excel进程
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
                app = null;
                snIndex = 0;
                UIControl.SN = snList[snIndex];
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
            if(allProductControl.Count>=2&&portAndNameDic.Count==7)
            {
                ErrorBox("该工位最多支持测试2个7端口产品！");
                return;
            }

            if (allProductControl.Count >= 8 && portAndNameDic.Count == 2)
            {
                ErrorBox("该工位最多支持测试8个3端口产品！");
                return;
            }
            portRawdatas.Clear();
            RealtimeMsg("正在打开模板...");
            curTestTmpt = -300;
            UIControl.IsSaveEnable = false;
            UIControl.IsScanEnable = false;
            BackgroundWorker templateBK = new BackgroundWorker();
            templateBK.DoWork += OpenTemplateBK_DoWork;
            templateBK.RunWorkerCompleted += OpenTemplateBK_RunWorkerCompleted;
            templateBK.RunWorkerAsync();
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
                    e.Result= "该SN号已存在测试列表！";
                    return;
                }
            }
            MESControl control = new MESControl();
            string errMsg = "";
            //allProductControl.Clear();
            if (control.OpenTemplate(amtsUrl, templateType, UIControl.SN, testProcess, MESTestType.Normal, mainInfo.UserID, mainInfo.Goldsample, true, false, ref errMsg))
            {
                if(allProductControl.Count>0)
                {
                    if(allProductControl[0].GetProductInfo().Spec==control.GetProductInfo().Spec)
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

        private void ClearListData()
        {
            testItemShow = new MESControl();
            // 更新测试信息
            if (EventAggregator != null)
            {
                List<MESControl> shows = new List<MESControl>();
                shows.Add(testItemShow);
                EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
            }
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

        private List<MESControl> testShowControl = new List<MESControl>();
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
                            for(int j=0;j< shows.Count;j++)
                            {
                                if(testInfos[i].PortNameForUser== shows[j].PortNameForUser
                                    &&testInfos[i].Temperature==shows[j].Temperature&&
                                    testInfos[i].ExParamName==shows[j].ExParamName)
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
        
        private void OpenTemplateBK_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (allProductControl.Count == 1)
                _scanList.Clear();
            string errMsg = (string)e.Result;
            if (errMsg.Length == 0)
            {
                RealtimeMsg(UIControl.SN + "：打开模板成功！");
                SetOpenTemplateComplete(true);
                TestProductInfo curInfo = new TestProductInfo();
                curInfo.Index = AllProducts.Count+1;
                curInfo.SN = UIControl.SN;
                AllProducts.Add(curInfo);
                //列表显示  
                //曲线显示处理
                List<MESTestInfo> testInfos = allProductControl[allProductControl.Count-1].GetAllTestInfo();
                if (allProductControl.Count == 1)
                {
                    updateParamIndex.Clear();
                    portAndNameDic.Clear();
                    portAssistant.Clear();

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
                        else if(param.ToUpper().Contains("CONFIG"))
                        {
                            //从无纸化上获取端口和功率计对应关系
                            string[] confSplit0 = param.Split('@');
                            if(confSplit0.Length==2)
                            {
                                string[] confSplit1 = confSplit0[1].Split(';');
                                List<int> scanPorts = new List<int>();
                                foreach (string conf in confSplit1)
                                {
                                    string[] confSplit2 = conf.Split(':');
                                    if(confSplit2.Length==2)
                                    {
                                        string splitPort = confSplit2[0].Substring(4);
                                        int splitPM = Convert.ToInt32(confSplit2[1].Substring(2));
                                        if(portAndPMDic.ContainsKey(splitPort))
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
                        /*if (rangCount == 3)
                            break;*/
                    }

                    maxScanFre = -2000000.0;
                    minScanFre = 2000000.0;
                    
                }


                Dictionary<string, int> inportDic = new Dictionary<string, int>();
                for (int i = 0; i < testInfos.Count; i++)
                {
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

                            //if (assist.PortIndex > 2)
                                assist.PMIndex = portAndPMDic[assist.PortIndex.ToString()];

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
                            portAssistant.Add(assist);
                        }
                    }
                }

                if (allProductControl.Count == 1)
                {
                    UIControl.PN = allProductControl[0].GetProductInfo().ProductPN;
                    UIControl.Spec = allProductControl[0].GetProductInfo().SpecNO;
                    string[] portNames = portAndNameDic.Keys.ToArray();
                    //曲线显示初始化
                    minScanFre = minScanFre - productFre;
                    maxScanFre = maxScanFre + productFre;
                    curveShow.InitAllCurve(portNames);
                    curveShow.UpdateFre(minScanFre, maxScanFre);
                }

                ReadRefData(allProductControl.Count-1, portAssistant, ref errMsg);
                ParamItemUpdate(allProductControl.Count-1,true);
                UIControl.IsScanEnable = true;
                if (errMsg.Length>0)
                {
                    WarningBox(errMsg);
                }
                btnOnekeyScan_Click(sender, null);
            }
            else
            {
                RealtimeMsg(errMsg, StatusType.Error);
                ErrorBox(errMsg);
                return;
            }
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
                        SetSwitch(portAssistant[referenceIndex].ProductIndex, portAssistant[referenceIndex].Port);
                        int portIndex = portAssistant[referenceIndex].PortIndex;
                        SetIsScanFinished(false);
                        RealtimeMsg(prompt);
                        BackgroundWorker bkPM = new BackgroundWorker();
                        bkPM.DoWork += Scan_DoWork;
                        bkPM.RunWorkerCompleted += Scan_RunWorkerCompleted;
                        scanDetailInfo.ScanType = SCANTYPE.RefWithPDL;
                        scanDetailInfo.Ports.Clear();
                        scanDetailInfo.Ports.Add(portIndex);
                        scanDetailInfo.ProductIndex = portAssistant[referenceIndex].ProductIndex;
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

        private void SetSwitch(int productIndex,string portName)
        {
            string flagPort=portName.Replace(" ", "");
            string flag = productIndex.ToString()+"::" + flagPort.ToUpper() + ":"+portAndNameDic.Count.ToString();
            //RealtimeMsg("开始切换开关");
            string errMsg = "";
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType("InterleaverFinalTestSwitch", ref opticalSwitch, ref errMsg) == 0)
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
                    int pmIndex = portAndPMDic[scanInfo.Ports[0].ToString()];
                    
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
                            int pmIndex = portAndPMDic[scanInfo.Ports[j].ToString()];
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
            return 2;
        }

        /// <summary>
        /// 开启扫描background线程
        /// </summary>
        /// <param name="scanType">扫描类型</param>
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
                CurProductIndex = scanDetailInfo.ProductIndex-1;
                scanErrorMsg = "";
                int res = 0;
                //ScanAndCalResult(scanDetailInfo, ref scanErrorMsg);
                SetIsScanFinished(true);
                if (scanErrorMsg.Length > 0 || res != 0)
                {
                    string errMsg = "";
                    //清除测试结果
                    ClearResult(scanDetailInfo.Ports, ref errMsg);
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
                            paramResult = calFuntion.CalChannelTestParam(param, portResData[dataIndex], portResData[adjDataIndex], fre, productFre, ref errMsg);
                        }
                        else
                        {
                            paramResult = calFuntion.CalChannelTestParam(param, portResData[dataIndex], null, fre, productFre, ref errMsg);
                        }
                        paramResult = Math.Round(paramResult, 3);
                        string[] paramSplits = param.Split('@');
                        //string maxILParam = "MAXIL@PB=" + passBand.ToString();
                        if (paramSplits[0].ToUpper()!= "MAXIL")
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
                    errMsg = "";
                    string param = allTestParam[i].ExParamName;
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
                        double paramResult = paramCal.CalPortParam(param, allTestParam[i].Temperature.ToString(), portSplits[0], curPortRecords, allProductControl[CurProductIndex].GetGlobalSetting().TmptArray, ref errMsg);

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
                                int dataLen = scanRes[i].Length;
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
                                }
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
                    ParamItemUpdate(CurProductIndex);
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;              
            }

            if(scanInfo.ScanType==SCANTYPE.TestWithPDLOnekey)
            {
                if (scanErrorMsg.Length == 0)
                {
                    ParamItemUpdate(CurProductIndex);
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
                if (showInfos[j].PortNameForUser == assist.Name)
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
            //if (MessageBox.Show("正在测试，是否要清空列表！", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                AllProducts.Clear();
                allProductControl.Clear();
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
            /*foreach(PortAssist assist in portAssistant)
            {                
                assist.IsTested = false;
            }
            curTestTmpt = -300;*/
            OnekeyScan();
        }

        
        private void OnekeyScan()
        {
            //获取未测试的端口
            scanDetailInfo.Ports.Clear();
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
            for (i = 0; i < portAssistant.Count; i++)
            {
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
                    if (!portAssistant[i].IsTested)
                    {
                        scanTmpt = portAssistant[i].TestTmpt;
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
                //MessageBox.Show("一键测试完成！");
                UIControl.IsScanEnable = true;
                UIControl.IsSaveEnable = true;
                btnSaveToAMTS_Click(null, null);
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
            string testPortName = "";
            foreach (PortAssist assist in portAssistant)
            {
                //string[] splits = assist.Name.Split('-');
                if (productID==assist.ProductIndex&& scanIndex==assist.ScanIndex
                    && scanTmpt==assist.TestTmpt)
                {
                    testPortName = assist.Port;
                    scanDetailInfo.Ports.Add(assist.PortIndex);
                }
            }
            scanDetailInfo.ProductIndex = productID;
            string errMsg = "";
            /*if (!IsScanRef(scanDetailInfo.Ports, ref errMsg))
            {
                WarningBox(errMsg);
                return;
            }
            //切换开关
            SetSwitch(productID,testPortName);*/
            //CurProductIndex = productID;

            
            Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
            Workbooks wbks = app.Workbooks;
            string snDataPath = Environment.CurrentDirectory + "\\15275\\";
            snDataPath += allProductControl[0].ProductSN;
            snDataPath += ".xlsx";
            Workbook wbk = wbks.Add(snDataPath);
            Worksheet wsh = (Worksheet)wbk.Sheets[1];
            long rowCount = wsh.UsedRange.Rows.Count;
            long columnCount = wsh.UsedRange.Columns.Count;
            object[,] dataContents = new object[rowCount, columnCount];
            dataContents = wsh.Range[wsh.Cells[1, 1], wsh.Cells[rowCount, columnCount]].Value2;
            foreach (int port in scanDetailInfo.Ports)
            {
                int dataIndex = (scanDetailInfo.ProductIndex - 1) * portAndNameDic.Count + port - 1;
                InterleaverScanResult.InitRawdataBuffer(portResData[dataIndex],Convert.ToInt32(rowCount - 3));
            }
            //判断是否需要烤温
            bool isNeedHeat = false;
            curTestTmpt = scanTmpt;
            //if(curTestTmpt.CompareTo(-300)==0)
            {
                if (curTestTmpt > 20 && curTestTmpt < 30)
                {
                    //常温 even B-E列  odd F-I列
                    for (int nIdx = 3; nIdx < rowCount; nIdx++)
                    {
                        portResData[0][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[0][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 4]);
                        portResData[0][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 5]);
                        portResData[0][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 2]);
                        portResData[0][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 3]);
                        portResData[0][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx - 3];

                        portResData[1][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[1][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 8]);
                        portResData[1][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 9]);
                        portResData[1][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 6]);
                        portResData[1][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 7]);
                        portResData[1][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx - 3];
                    }
                }
                else if (curTestTmpt < 10)
                {
                    //低温 even J-M列  odd N-Q列
                    for (int nIdx = 3; nIdx < rowCount; nIdx++)
                    {
                        portResData[0][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[0][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 12]);
                        portResData[0][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 13]);
                        portResData[0][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 10]);
                        portResData[0][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 11]);
                        portResData[0][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx - 3];

                        portResData[1][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[1][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 16]);
                        portResData[1][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 17]);
                        portResData[1][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 14]);
                        portResData[1][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 15]);
                        portResData[1][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx - 3];
                    }
                }
                else if (curTestTmpt > 30)
                {
                    //高温 even R-U列  odd V-Y列
                    for (int nIdx = 3; nIdx < rowCount; nIdx++)
                    {
                        portResData[0][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[0][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 20]);
                        portResData[0][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 21]);
                        portResData[0][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 18]);
                        portResData[0][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 19]);
                        portResData[0][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx-3];

                        portResData[1][5][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 1]);
                        portResData[1][1][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 24]);
                        portResData[1][2][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 25]);
                        portResData[1][3][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 22]);
                        portResData[1][4][nIdx - 3] = Convert.ToDouble(dataContents[nIdx, 23]);
                        portResData[1][0][nIdx - 3] = 2.99792458E8 / portResData[0][0][nIdx - 3];


                    }
                }

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

            DoScanOnBK();
            wbk.Close();
            wbks.Close();
            app.Quit();
            //释放掉多余的excel进程
            System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            app = null;
            /*if (isNeedHeat)
            {
                //烤温是否需要增加提示
                string prompt = string.Format("是否进行{0}度烤温", portAssistant[i].TestTmpt);
                if (MessageBox.Show(prompt, "温馨提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    prompt= string.Format("开始进行{0}度烤温", portAssistant[i].TestTmpt);
                    RealtimeMsg(prompt);
                    double tmptChangeTimes = portAssistant[i].TmptChangeTimes;
                    bakeTimeCheckBK.RunWorkerAsync(tmptChangeTimes * 60);
                    curTestTmpt = scanTmpt;
                }
                else
                {
                    RealtimeMsg("一键测试结束");
                    UIControl.IsReferenceEnable = true;
                    UIControl.IsScanEnable = true;
                    UIControl.IsSaveEnable = true;
                    return;
                }
            }
            else
            {
                curTestTmpt = scanTmpt;
                DoScanOnBK();
            }*/
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
            if(selectItem!=null&& selectItem.ParamIndex.Count>0)
            {
                int selectIndex = selectItem.ParamIndex[0];
                List<MESTestInfo> showInfos = testShowControl[selectItem.ProductIndex].GetAllTestInfo();
                if (selectIndex >= showInfos.Count)
                    return;
                MESTestInfo selectTestItem = showInfos[selectIndex];

                //string[] portNames = selectTestItem.PortNameForUser.Split('-');
                scanDetailInfo.Ports.Clear();
                //获取同时扫描的端口号。
                //进光端一样，则可以一起扫描，比如in-to,in-te,in-moni同时扫描
                string testPortName = "";
                int scanIndex = -1;
                double tmptChangeTimes = 0;
                foreach (PortAssist assist in portAssistant)
                {
                    if(scanIndex==-1)
                    {
                        if(selectTestItem.PortNameForUser == assist.Name)
                        {
                            scanIndex = assist.ScanIndex;
                        }
                    }
                    //string[] splits = assist.Name.Split('-');
                    if ((selectItem.ProductIndex+1) == assist.ProductIndex && scanIndex == assist.ScanIndex
                        && selectTestItem.Temperature == assist.TestTmpt)
                    {
                        tmptChangeTimes = assist.TmptChangeTimes;
                        testPortName = assist.Port;
                        scanDetailInfo.Ports.Add(assist.PortIndex);
                    }
                }

                string errMsg = "";
                if (!IsScanRef(scanDetailInfo.Ports, ref errMsg))
                {
                    WarningBox(errMsg);
                    return;
                }
                //切换开关
                //SetSwitch(true);
                double testTmpt = selectTestItem.Temperature;
                //判断是否需要烤温
                bool isNeedHeat = false;
                if (curTestTmpt.CompareTo(-300) == 0)
                {
                    if (testTmpt > 20 && curTestTmpt < 30)
                    {
                        isNeedHeat = false;
                    }
                    else
                        isNeedHeat = true;
                }
                else if (curTestTmpt.CompareTo(testTmpt) != 0)
                {
                    isNeedHeat = true;
                }

                scanDetailInfo.ScanType = SCANTYPE.TestWithPDL;
                scanDetailInfo.ProductIndex = selectItem.ProductIndex + 1;
                UIControl.IsScanEnable = false;
                //需要知道选择的是产品几
                SetSwitch(scanDetailInfo.ProductIndex, testPortName);
                curTestTmpt = testTmpt;
                if (isNeedHeat)
                {
                    //烤温是否需要增加提示
                    string prompt = string.Format("开始进行{0}度烤温", testTmpt);
                    RealtimeMsg(prompt);
                    bakeTimeCheckBK.RunWorkerAsync(tmptChangeTimes * 60);
                    //curTestTmpt = testTmpt;
                }
                else
                    DoScanOnBK();
            }
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
                if (allProductControl[i].SaveDataToAMTS(allProductControl[i].ProductSN, amtsSaveUrl, ref errMsg, false, MESRawdataType.MemsVOA, rawdatas) != 0)
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
                ClearListData();
                UIControl.SN = "";
                UIControl.IsSaveEnable = false;
                UIControl.IsScanEnable = false;
                //下个SN，调Open
                snIndex++;
                if (snIndex >= snList.Count)
                {
                    snIndex = -1;
                    snList.Clear();
                    return;
                }
                btnClearBakeSN_Click(null, null);
                UIControl.SN = snList[snIndex];
                btnOpenTemplate_Click(null, null);
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
        }
    }
}
