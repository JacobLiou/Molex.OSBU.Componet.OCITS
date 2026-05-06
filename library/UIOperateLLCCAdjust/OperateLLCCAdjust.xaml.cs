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

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.UIList;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;

using System.Diagnostics;
using System.IO;

namespace UIOperateLLCCAdjust
{
    /// <summary>
    /// Interaction logic for OperateLLCCAdjust.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperateLLCCAdjust")]
    public partial class OperateLLCCAdjust : UserControl
    {

        private const string refDataFile = "\\reference\\RefData.csv";
        private const string pwmResetFile = "\\temple\\PWMReset.csv";
        private const string passImage = "\\image/Pass.ico";
        private const string failImage = "\\image/Fail.ico";
        private const string dataServerPath = "\\\\zh-mfs-srv2.oplink.com.cn\\Data\\TestData\\Pilot\\LLCC\\";
        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 产品测试信息
        /// </summary>
        private FusionControl templateControl = new FusionControl();

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

        /// <summary>
        /// 参数
        /// </summary>
        [Import(typeof(IAlgotithm))]
        private IAlgotithm algorithm;

        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

        /// <summary>
        /// 功率计对象
        /// </summary>
        IPowermeter powermeter;

        /// <summary>
        /// 功率计实时显示后台线程
        /// </summary>
        private Thread powermeterRealtimeThread;
        private delegate void PowermeterRealtimeDelegate();
        PowermeterRealtimeDelegate powermeterRealtimeDelegate;

        /// <summary>
        /// 测试通过图片加载存储对象
        /// </summary>
        private BitmapImage passBitmapImage = null;

        /// <summary>
        /// 测试失败图片加载存储对象
        /// </summary>
        private BitmapImage failBitmapImage = null;

        /// <summary>
        /// 功率计复位线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 由主程序传递的工位信息等
        /// </summary>
        private MainInitInfo mainInfo = null;

        /// <summary>
        /// 模板名称
        /// </summary>
        private string templateName = "";

        /// <summary>
        /// 功率计值
        /// </summary>
        private List<double> realtimePowers = new List<double>();

        private Dictionary<string, int> dicUVRecord = new Dictionary<string, int>();

        private IOpticalSource srcDevice = null;

        private IOpticalSource srcDevice2 = null;

        private IPDLController pdlCtrl = null;

        private BackgroundWorker bkTest = new BackgroundWorker();

        //0--IL 归零，1\2自动化左右通道归零，3--RL归零
        private BackgroundWorker bkReferece = new BackgroundWorker();

        /// <summary>
        /// 端口分组信息，只有自动化项目需要分组
        /// </summary>
        private Dictionary<int, GroupPorts> proGroups = new Dictionary<int, GroupPorts>();

        private List<AutoTestInfo> allTestItems = new List<AutoTestInfo>();
        private int curTestItemIdx = -1;

        private int powerCount = 200;

        /// <summary>
        /// 是否正在测试，能否切换波长等
        /// </summary>
        private bool isOnTesting = false;

        private string OMSProcess = "";

        /// <summary>
        /// 更新曲线委托
        /// </summary>
        /// <param name="xArr"></param>
        /// <param name="yArr"></param>
        private delegate void UpdateCurveDelegate(List<double> xArr, List<double> yArr);
        UpdateCurveDelegate myUpdateCurveDelegate;

        public OperateLLCCAdjust()
        {
            InitializeComponent();

            btnOpenTemplate.DataContext = uiVariable;
            btnSaveToAMTS.DataContext = uiVariable;
            btnUVata.DataContext = uiVariable;
            btnTest.DataContext = uiVariable;
            btnILRef.DataContext = uiVariable;
            txtBoxSN.DataContext = uiVariable;
            txtPN.DataContext = uiVariable;
            txtSpec.DataContext = uiVariable;

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;

            powermeterRealtimeDelegate = new PowermeterRealtimeDelegate(UpdatemeterDelegate);
            powermeterRealtimeThread = new Thread(UpdatePowermeter);
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

        /// <summary>
        /// 功率值实时显示线程
        /// </summary>
        private void UpdatePowermeter()
        {
            while (true)
            {
                using (Mutex m = new Mutex(true, "powermeter"))
                {
                    if (!isRealShowPower)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                int preTickCount = System.Environment.TickCount;
                int EndTickCount = System.Environment.TickCount;
                while (EndTickCount - preTickCount < 200)
                {
                    EndTickCount = System.Environment.TickCount;
                    Thread.Sleep(50);
                }

                string errMsg = "";
                //读取功率计的值
                if (powermeter != null)
                    powermeter.ReadPowerAvg(ref errMsg, out realtimePowers);
                
                this.Dispatcher.Invoke(powermeterRealtimeDelegate);
            }
        }

        /// <summary>
        /// 功率值实时显示委托
        /// </summary>
        private void UpdatemeterDelegate()
        {
            if (realtimePowers.Count <= 0)
                return;
            if (realtimePowers[0] == CommonFunction.GetDefaultValue() || realtimePowers[0] == -10000)
                return;
            List<RealtimePowerInfo> powers = new List<RealtimePowerInfo>();

            RealtimePowerInfo ch1 = new RealtimePowerInfo();
            ch1.Prefix = "";
            if (selectItem != null && templateControl.GetAllTestInfo().Count > 0)
            {
                MESTestInfo ilTest = templateControl.GetAllTestInfo()[selectItem.RowIndex];
                if (ilTest.ILRef != CommonFunction.GetDefaultValue())
                    ch1.Power = (realtimePowers[0] - ilTest.ILRef).ToString("#0.000") + "dB";
                else
                    ch1.Power = realtimePowers[0].ToString("#0.000") + "dB";
            }
            else
                ch1.Power = realtimePowers[0].ToString("#0.000") + "dB";
            powers.Add(ch1);

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventRealtimePowerUpdate>().Publish(powers);
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
            System.IO.FileInfo info = new System.IO.FileInfo(System.Environment.CurrentDirectory + pwmResetFile);
            if (info.Exists)
            {
                System.IO.FileStream fs = new System.IO.FileStream(System.Environment.CurrentDirectory + pwmResetFile, System.IO.FileMode.Open);
                System.IO.StreamReader sr = new System.IO.StreamReader(fs);
                string strTime = sr.ReadToEnd();
                sr.Close();
                sr = null;
                fs.Close();
                fs = null;
                if (strTime != "")
                {
                    DateTime dt = Convert.ToDateTime(strTime);
                    TimeSpan ts = DateTime.Now - dt;
                    TimeSpan dtarget = new TimeSpan(6, 0, 0);
                    TimeSpan tsRemainder = dtarget - ts;
                    if ((tsRemainder.Hours == 0 && tsRemainder.Minutes < 30) || (tsRemainder.Hours <= 0 && tsRemainder.Minutes <= 0))
                    {
                        txtRefTime.Foreground = Brushes.Red;
                    }
                    else
                        txtRefTime.Foreground = Brushes.Black;
                    txtRefTime.Text = string.Format("{0}小时{1}分{2}秒", tsRemainder.Hours, tsRemainder.Minutes, tsRemainder.Seconds);
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
        /// 初始化
        /// </summary>
        /// <param name="info"></param>
        public void Init(MainInitInfo info)
        {
            mainInfo = info;
            
            string errMsg = "";
            if (mainInfo.MESMode.ToUpper().Contains("MESLESS")|| mainInfo.MESMode.ToUpper().Contains("OFFLINE"))
            {
                if(!FusionControl.SetToSpecMode(mainInfo.CheckUser, mainInfo.CheckPSW, "MESLESS", ref errMsg))
                {
                    ErrorBox(errMsg);
                    RealtimeMsg(errMsg);
                    return;
                }
            }
            /*curveShow = new DemuxCurve(EventAggregator);
            curveShow.InitCurve();*/
            int channel = 0;
            int nRes = DeviceControl.GetPowermeterByIndex(1, ref channel, ref powermeter, ref errMsg);
            nRes = DeviceControl.GetOpticalSourceByWaveAndType(1, ref srcDevice, ref errMsg);
            nRes = DeviceControl.GetOpticalSourceByWaveAndType(2, ref srcDevice2, ref errMsg);
            nRes = DeviceControl.GetPDLControllerByIdx(1, ref pdlCtrl, ref errMsg);
            bkTest.DoWork += Test_DoWork;
            bkTest.ProgressChanged += Test_ProgressChanged;
            bkTest.RunWorkerCompleted += Test_Completed;
            bkTest.WorkerSupportsCancellation = true;
            bkTest.WorkerReportsProgress = true;
           

            bkReferece.DoWork += Reference_DoWork;
            bkReferece.ProgressChanged += Reference_ProgressChanged;
            bkReferece.RunWorkerCompleted += Reference_Completed;
            bkReferece.WorkerSupportsCancellation = true;
            bkReferece.WorkerReportsProgress = true;

            InitCurve("", "dB", 0, powerCount, "IL", System.Drawing.Color.DarkBlue, CurveType.Line, "");
            
            automationType = info.AutomationType;
            refTimeCheckBK.RunWorkerAsync();
            if(mainInfo.TestProcess.Contains("终测"))
            {
                isAdjustProcess = false;
            }
            if (automationType == 0)
            {
                uiVariable.IsEnable = true;
                uiVariable.IsSaveEnable = false;
                uiVariable.IsLightedEnable = true;
                if (powermeter == null)
                {
                    RealtimeMsg("功率计连接失败！");
                    ErrorBox("功率计连接失败！");
                }
                powermeterRealtimeThread.Start();

                btnReConnect.Content = "解锁";

                if (powermeter != null)
                {
                    List<double> testArray = new List<double>();
                    powermeter.ReadPowerAvg(ref errMsg,out testArray);
                    powermeter.SetPMWavelength(1550, ref errMsg);
                    powermeter.SetPMUnits(ref errMsg, "dB");
                }
                if (srcDevice == null)
                {
                    ErrorBox("光源连接失败！");
                    RealtimeMsg("光源连接失败！");
                    return;
                }
            }
            else if(automationType == 1)
            {
                uiVariable.IsEnable = false;
                uiVariable.IsSaveEnable = false;
                uiVariable.IsLightedEnable = false;
                IAutomation auto = null;
                DeviceControl.GetAutomationInIndex(1, ref auto, ref errMsg);
                if (auto != null)
                {
                    bkAutomationDeal = new BackgroundWorker();
                    bkAutomationDeal.DoWork += SeverDataDealDoWork;
                    bkAutomationDeal.ProgressChanged += SeverDataDeal_Progress;
                    bkAutomationDeal.WorkerSupportsCancellation = true;
                    bkAutomationDeal.WorkerReportsProgress = true;
                    bkAutomationDeal.RunWorkerAsync();
                    string host = "";
                    int port = 0;
                    if (auto.GetIPAndPort(ref host, ref port) == 0)
                    {
                        cltSocket = new ClientSocket(host, port);
                        if (!cltSocket.ConnectSever(ref errMsg))
                        {
                            RealtimeMsg(errMsg);
                        }
                        else
                        {
                            //callBack = CallBackSeverDataDeal;
                            cltSocket.SeverDataDeal += SeverDataDeal;
                            RealtimeMsg("连接自动化服务器成功！");
                        }
                    }
                }

                if (srcDevice == null|| srcDevice2 == null)
                {
                    ErrorBox("集成光源功率计连接失败！");
                    RealtimeMsg("集成光源功率计连接失败！");
                    return;
                }

                if(pdlCtrl==null)
                {
                    ErrorBox("偏振控制器连接失败！");
                    RealtimeMsg("偏振控制器连接失败！");
                    return;
                }
            }           
        }

        /// <summary>
        /// 更新曲线显示
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="areaName">显示曲线的区域名称</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        private void UpdateCurveShow(string serName, CurveUpdate upType, double xValues, double yValues)
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.SeriesName = serName;
            curveDetail.UpdateType = upType;
            curveDetail.TargetName = "";
            List<double> xArr = new List<double>();
            xArr.Add(1);
            List<double> yArr = new List<double>();
            yArr.Add(yValues);
            curveDetail.XAxisStep = xArr;
            curveDetail.YAxisValue = yArr;

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
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
        /// <summary>
        /// 功率计复位
        /// </summary>
        private void ResetPWM()
        {
            try
            {
                System.IO.FileInfo info = new System.IO.FileInfo(System.Environment.CurrentDirectory + pwmResetFile);
                if (info.Exists)
                {
                    System.IO.FileStream fs = new System.IO.FileStream(System.Environment.CurrentDirectory + pwmResetFile, System.IO.FileMode.Open);
                    System.IO.StreamReader sr = new System.IO.StreamReader(fs);
                    string strTime = sr.ReadToEnd();
                    sr.Close();
                    sr = null;
                    fs.Close();
                    fs = null;
                    if (strTime != "")
                    {
                        DateTime dt = Convert.ToDateTime(strTime);
                        TimeSpan ts = DateTime.Now - dt;
                        TimeSpan dtarget = new TimeSpan(6, 0, 0);
                        TimeSpan tsRemainder = dtarget - ts;
                        if ((tsRemainder.Hours == 0 && tsRemainder.Minutes < 30) || (tsRemainder.Hours <= 0 && tsRemainder.Minutes <= 0))
                        {
                            txtRefTime.Foreground = Brushes.Red;
                        }
                        else
                            txtRefTime.Foreground = Brushes.Black;
                        txtRefTime.Text = string.Format("{0}小时{1}分{2}秒", tsRemainder.Hours, tsRemainder.Minutes, tsRemainder.Seconds);
                        if (tsRemainder.Days < 0 || (tsRemainder.Hours < 0))
                        {
                            ResetPowermeter(false);
                        }
                    }
                    else
                    {
                        ResetPowermeter(false);
                    }
                }
                else
                {
                    ResetPowermeter(true);
                }
            }
            catch (Exception ex)
            {
                string errMsg = "";
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }
        private void ResetPowermeter(bool isCreate)
        {
            string errMsg = "";
            if (powermeter != null)
            {
                powermeter.ResetPowermeter(ref errMsg);
                if (errMsg != "")
                {
                    txtRefTime.Text = errMsg;
                    errMsg = "";
                }
            }

            FileInfo file = new FileInfo(System.Environment.CurrentDirectory + refDataFile);
            if (file.Exists)
                file.Delete();
            if (isCreate)
            {
                File.Exists(System.Environment.CurrentDirectory + pwmResetFile);
                {
                    File.Delete(System.Environment.CurrentDirectory + pwmResetFile);
                }
                System.IO.FileStream fs = new System.IO.FileStream(System.Environment.CurrentDirectory + pwmResetFile, System.IO.FileMode.OpenOrCreate);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(fs, Encoding.Default);
                sw.WriteLine(DateTime.Now.ToString());
                sw.Close();
                sw = null;
                fs.Close();
                fs = null;
            }
            else
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(System.Environment.CurrentDirectory + pwmResetFile, false, Encoding.Default);
                sw.WriteLine(DateTime.Now.ToString());
                sw.Close();
                sw = null;
            }

        }

        /// <summary>
        /// 显示模板名称到任务栏
        /// </summary>
        private void ShowTemplatePath()
        {
            XmlStr updateTmpltPath = new XmlStr();
            updateTmpltPath.Content = "<OCITS><Msg Type=\"Template\" Target=\"MainWindow\" Source=\"UIOperateLLCCAdjust\"/><Operate>ShowTemplatePath</Operate><Path>";
            updateTmpltPath.Content += System.IO.Path.GetFileName(templateName);
            updateTmpltPath.Content += "</Path></OCITS>";
            EventAggregator.GetEvent<EventXml>().Publish(updateTmpltPath);
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
        /// 加载dll
        /// </summary>
        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
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
                        UpdateSelectItem(info);
                    }
                );
        }

        /// <summary>
        /// 选中行信息更新
        /// </summary>
        /// <param name="map">选中行信息</param>
        private void UpdateSelectItem(IndexMap map)
        {
            if (templateControl.ProductSN == "" || (selectItem!=null&&templateControl.AllTestInfo.Count < selectItem.RowIndex + 1))
                return;
            selectItem = map.Clone();
            MESTestInfo ilTest = templateControl.AllTestInfo[selectItem.RowIndex];
            //根据波长切换光开关
            string errMsg = "";
            int nTestIndex = selectItem.ParamIndex[0];
            //RealtimeMsg(string.Format("select item change:{0}", selectItem.ParamIndex[0]));
            if (automationType==1)
            {
                for (int j = 0; j < allTestItems.Count; j++)
                {
                    if (nTestIndex== allTestItems[j].TestIdx&&(!allTestItems[j].isTested))
                    {                      
                        btnTest_Click(this, null);
                        break;
                    }
                }
            }
            else
            {
                MESTestInfo info = templateControl.AllTestInfo[nTestIndex];
                if (info.ObjectID.Contains("_UV") && (!dicUVRecord.ContainsKey(uiVariable.SN)))
                {
                    uiVariable.IsEnable = false;
                }
                else if ((!info.ObjectID.Contains("_UV")) && dicUVRecord.ContainsKey(uiVariable.SN))
                {
                    uiVariable.IsEnable = false;
                }
                else
                {
                    uiVariable.IsEnable = true;
                }
                if (isAdjustProcess && uiVariable.IsEnable && (!isOnTesting))
                {
                    if (srcDevice != null )
                    {                        
                       srcDevice.SetWavelength(info.WLLeft, ref errMsg);                        
                    }
                    if (powermeter != null)
                    {
                        powermeter.SetPMWavelength(info.WLLeft, ref errMsg);
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        /// <summary>
        /// 注册接收参数列表快捷键信息
        /// </summary>
        private void KeyDownRegister()
        {
            EventAggregator.GetEvent<EventListKeyDown>().Subscribe
                (
                    info =>
                    {
                        UpdateKeyDown(info);
                    }
                );
        }

        private bool isRefOneChanel = false;
        private bool isKeyFinish = true;
        /// <summary>
        /// 参数列表快捷键响应函数
        /// </summary>
        /// <param name="info"></param>
        private void UpdateKeyDown(KeyDownInfo info)
        {
            using (Mutex m = new Mutex(true, "keyFinish"))
            {
                if (isKeyFinish == false)
                    return;
                isKeyFinish = false;
            }
            if (info.Key == Key.Insert)
            {
                if (uiVariable.IsSaveEnable)
                {
                    btnSaveToAMTS_Click(null, null);
                }
            }
            else if (info.Key == Key.Multiply) //RL reference
            {
                
                isRefOneChanel = true;              
                btnRLRef_Click(null, null);
                isRefOneChanel = false;
            }
            else if (info.Key == Key.Divide)
            {
               
                isRefOneChanel = true;
                btnILRef_Click(null, null);
                isRefOneChanel = false;
            }
            else if (info.Key == Key.Add|| info.Key == Key.Subtract)
            {
                if (uiVariable.IsEnable)
                {
                    btnTest_Click(null, null);
                }
            }
            else if(info.Key==Key.Return)
            {
                if(txtBoxSN.IsFocused)
                {
                    btnOpenTemplate_Click(null, null);
                }
            }
            using (Mutex m = new Mutex(true, "keyFinish"))
            {
                isKeyFinish = true;
            }
        }

        /// <summary>
        /// 快捷键响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                
            }
            else if (Keyboard.IsKeyDown(Key.Subtract))
            {
                
            }
            else if (Keyboard.IsKeyDown(Key.Multiply))
            {
                
            }
            else if (Keyboard.IsKeyDown(Key.Divide))
            {
                
            }
            else if (Keyboard.IsKeyDown(Key.Add))
            {
                
            }
        }

        /// <summary>
        /// 更新参数列表
        /// </summary>
        /// <param name="info"></param>
        /// <param name="paramIndex"></param>
        /// <param name="nextSelect"></param>
        private void UpdateItem(MESTestInfo info, int paramIndex, IndexMap nextSelect = null)
        {
            ItemContent item = new ItemContent();
            item.TestInfo = info;
            item.UpdateItemMap = new IndexMap();
            item.UpdateItemMap.ParamIndex = new List<int>();
            item.UpdateItemMap.ParamIndex.Add(paramIndex);
            item.NextSelectMap = nextSelect;
            item.UpdateItemMap.ProductIndex = 0;
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventListItemUpdate>().Publish(item);
            }
            UpdateResIcon();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
            KeyDownRegister();
        }

        private void btnPWMReset_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                FileInfo file = new FileInfo(System.Environment.CurrentDirectory + refDataFile);
                if (file.Exists)
                    file.Delete();

                ResetPowermeter(true);
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }


        public void Reference_DoWork(object sender, DoWorkEventArgs e)
        {
            string errMsg = "";
            int nType = (int)e.Argument;
            try
            {
                for (int i = 0; i < templateControl.AllTestInfo.Count; i++)
                {
                    if (Math.Abs(templateControl.AllTestInfo[i].ILRef - CommonFunction.GetDefaultValue()) < 0.01
                        || (Math.Abs(templateControl.AllTestInfo[i].RLRef - CommonFunction.GetDefaultValue()) < 0.01 && templateControl.AllTestInfo[i].TestParam == MESParam.RL)|| isRefOneChanel == true)
                    {
                        if (templateControl.AllTestInfo[i].TestParam == MESParam.WDL)
                            continue;
                        if (nType == 3 && templateControl.AllTestInfo[i].TestParam != MESParam.RL)
                            continue;
                        double refPower = 0;
                        if (automationType == 1)
                        {
                            int curChan = Convert.ToInt32(e.Argument);
                            //遍历看当前通道是左还是右
                            int[] allKeys = proGroups.Keys.ToArray();
                            int nLeftOrRight = 0;
                            foreach (int grpKey in allKeys)
                            {
                                if (templateControl.AllTestInfo[i].PortNameForUser.ToUpper().TrimEnd().TrimStart() == proGroups[grpKey].LeftPort.ToUpper().TrimEnd().TrimStart())
                                {
                                    nLeftOrRight = 1;
                                    break;
                                }
                                else if (templateControl.AllTestInfo[i].PortNameForUser.ToUpper().TrimEnd().TrimStart() == proGroups[grpKey].RightPort.ToUpper().TrimEnd().TrimStart())
                                {
                                    nLeftOrRight = 2;
                                    break;
                                }
                                else
                                {
                                    //报错模板分组有问题。
                                }

                            }
                            if (nLeftOrRight != curChan)
                                continue;
                            int nRes = 0;
                            if (srcDevice != null && nLeftOrRight == 1)
                            {
                                nRes = srcDevice2.SetWavelength(1310, ref errMsg);
                                nRes = srcDevice.SetWavelength(templateControl.AllTestInfo[i].WLLeft, ref errMsg);
                                
                            }
                            if (srcDevice2 != null && nLeftOrRight == 2)
                            {
                                nRes = srcDevice.SetWavelength(1310, ref errMsg);
                                nRes = srcDevice2.SetWavelength(templateControl.AllTestInfo[i].WLLeft, ref errMsg);
                            }
                            if (nRes != 0)
                            {
                                string errPrompt = string.Format("切换光源出错：{0}", errMsg);
                                e.Result = errPrompt;
                                //RealtimeMsg(errPrompt);
                                //MessageBox.Show(errPrompt);
                                return;
                            }


                            string askPower = string.Format("ASK;{0}\r\n", nLeftOrRight);
                            cltSocket.SendData(askPower, ref errMsg);
                            if (!powerEvent.WaitOne(5 * 1000))
                            {
                                string errPrompt = string.Format("请求读取功率超时，请确认自动化程序是否打开！");
                                e.Result = errPrompt;
                                return;
                            }
                            string res = AskPowerRes;
                            AskPowerRes = "";
                            if (res == "" || res.ToUpper().Contains("ERROR"))
                            {
                                //发送异常给自动化
                                string errPrompt = string.Format("请求读取功率出错：{0}！", res);
                                e.Result = errPrompt;
                                return;
                            }
                            else
                            {
                                refPower = Convert.ToDouble(res);
                            }
                        }
                        else
                        {
                            int nRes = 0;
                            if (srcDevice != null )
                            {
                                nRes = srcDevice.SetWavelength(templateControl.AllTestInfo[i].WLLeft, ref errMsg);
                                Thread.Sleep(400);
                            }
                            else
                            {
                                //没连接光源时，只归当前通道
                                if(i!= curReferenceIdx)
                                {
                                    continue;
                                }
                            }
                            //快捷键归零，只归当前通道
                            if (isRefOneChanel == true)
                            {
                                if (i != curReferenceIdx)
                                {
                                    continue;
                                }
                            }
                            if (nRes != 0)
                            {
                                string errPrompt = string.Format("切换光源出错：{0}", errMsg);
                                e.Result = errPrompt;
                                return;
                            }

                            List<double> readPower = new List<double>();
                            if (powermeter != null)
                            {
                                nRes = powermeter.SetPMWavelength(templateControl.AllTestInfo[i].WLLeft, ref errMsg);
                                Thread.Sleep(2000);
                                if (nRes != 0)
                                {
                                    nRes = powermeter.SetPMWavelength(templateControl.AllTestInfo[i].WLLeft, ref errMsg);
                                    if (nRes != 0)
                                    {
                                        string errPrompt = string.Format("功率计切换波长出错：{0}", errMsg);
                                        e.Result = errPrompt;
                                        return;
                                    }
                                }

                                Thread.Sleep(1000);
                                nRes = powermeter.ReadPowerAvg(ref errMsg, out readPower);
                                int nReadCount = 0;

                                while (nRes != 0)
                                {
                                    nRes = powermeter.ReadPowerAvg(ref errMsg, out readPower);
                                    nReadCount++;
                                    if(nReadCount>3)
                                    {
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                string errPrompt = string.Format("功率计未连接!");
                                e.Result = errPrompt;
                                return;
                            }
                            if (nRes != 0)
                            {
                                string errPrompt = string.Format("读取功率计出错：{0}", errMsg);
                                e.Result = errPrompt;
                                return;
                            }
                            refPower = readPower[0];
                        }


                        //所有波长相同的归零值一样
                        for (int j = i; j < templateControl.AllTestInfo.Count; j++)
                        {
                            if (Math.Abs(templateControl.AllTestInfo[j].WLLeft - templateControl.AllTestInfo[i].WLLeft) < 0.00001
                                && Math.Abs(templateControl.AllTestInfo[j].WLRight - templateControl.AllTestInfo[i].WLLeft) < 0.00001)
                            {
                                if (nType == 3 && templateControl.AllTestInfo[j].TestParam == MESParam.RL)
                                {
                                    templateControl.UpdateRLRefData(j, refPower);
                                    bkReferece.ReportProgress(j);
                                }
                                else if (nType != 3)
                                {
                                    templateControl.UpdateILRefData(j, refPower);
                                    bkReferece.ReportProgress(j);
                                }
                            }
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                string errPrompt = string.Format("归零DoWork(Exception):", ex.Message);
                return;
            }

        }

        private int curRefChan = 0;

        public void Reference_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            using (Mutex m = new Mutex(true, "powermeter"))
            {
                isRealShowPower = true;
            }
            string errMsg = "";
            if (e.Result != null && e.Result.ToString().Length > 0)
            {
                RealtimeMsg(e.Result.ToString());
                ErrorBox(e.Result.ToString());
                return;
            }
            templateControl.RecordRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);
            
            if (automationType == 1)
            {
                if (curRefChan == 1)
                {
                    curRefChan = 2;
                    string prompt = string.Format("请将右光源线接到功率计后再开始归零！");
                    RealtimeMsg(prompt);
                    MessageBox.Show (prompt);
                    bkReferece.RunWorkerAsync(2);
                }
                else if (curRefChan == 2)
                {
                    string automationRef = "REF";
                    foreach (MESTestInfo info in templateControl.AllTestInfo)
                    {
                        if (info.TestParam == MESParam.MaxIL)
                            automationRef += string.Format(";{0}:{1}", info.WLLeft, info.ILRef);
                    }
                    automationRef += "\r\n";
                    SendToAutomation(automationRef, ref errMsg);
                }
            }
        }

        public void Reference_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MESTestInfo info = templateControl.AllTestInfo[e.ProgressPercentage];
            UpdateItem(info, e.ProgressPercentage);
        }

        private ClientSocket cltSocket;

        public void SeverDataDealDoWork(object sender, DoWorkEventArgs e)
        {
            while (!bkAutomationDeal.CancellationPending)
            {
                Thread.Sleep(100);
            }
        }

        private void SeverDataDeal_Progress(object sender, ProgressChangedEventArgs e)
        {
            CallBackSeverDataDeal(e.UserState.ToString());
        }

        /// <summary>
        /// 回到主线程的回调函数
        /// </summary>
        /// <param name="message">信息</param>
        public delegate void CallBackDelegate(string message);

        public CallBackDelegate callBack;

        private string AskPowerRes = "";
        public void CallBackSeverDataDeal(string message)
        {
            string strRev = "Rec:" + message;
            RealtimeMsg(strRev);
            string[] splits = message.Split(';');
            if (splits.Length >= 2|| splits[0].Contains("SAVE"))
            {
                if (splits[0] == "SNNO")
                {
                    string[] snSplits = splits[1].Split('\r');
                    uiVariable.SN = snSplits[0];
                    //btnOpenTemplate.Focus();
                    btnOpenTemplate_Click(this, null);
                   
                }
                else if(splits[0] == "SWWL")
                {
                    if (splits.Length >= 3)
                    {
                        string errMsg = "";
                        double dWL = Convert.ToDouble(splits[2].Replace("\r\n",""));
                        if(splits[1]=="1")
                        {
                            if(srcDevice!=null&&0 != srcDevice.SetWavelength(dWL, ref errMsg))
                            {
                                string ackMsg = string.Format("SWWL;FAIL;{0}\r\n",errMsg);
                                SendToAutomation(ackMsg, ref errMsg);
                            }
                            else
                            {
                                string ackMsg = string.Format("SWWL;PASS;{0}\r\n", dWL);
                                SendToAutomation(ackMsg, ref errMsg);
                            }
                        }
                        else if(splits[1]=="2")
                        {
                            if (srcDevice2!=null&&0 != srcDevice2.SetWavelength(dWL, ref errMsg))
                            {
                                string ackMsg = string.Format("SWWL;FAIL;{0}\r\n", errMsg);
                                SendToAutomation(ackMsg, ref errMsg);
                            }
                            else
                            {
                                string ackMsg = string.Format("SWWL;PASS;{0}\r\n", dWL);
                                SendToAutomation(ackMsg, ref errMsg);
                            }
                        }
                    }
                }
                else if (splits[0].Contains("SAVE"))
                {
                    btnSaveToAMTS_Click(this, null);
                }
                else if (splits[0] == "TEST")
                {
                    string[] snSplits = splits[1].Split('\r');
                    int testGroup = Convert.ToInt32(snSplits[0]);
                    RealtimeMsg("进入测试");
                    AutomationTest(testGroup);
                }
                else if(splits[0] == "ASK")
                {
                    string[] snSplits = splits[2].Split('\r');                   
                    if (splits[1]=="PASS")
                    {
                        
                        AskPowerRes = snSplits[0];
                    }
                    else
                    {
                        AskPowerRes = string.Format("Error:{0}", snSplits[0]);
                    }
                    powerEvent.Set();
                }
            }
        }

        public void AutomationTest(int nGroup)
        {
            allTestItems.Clear();
            string errMsg = "";
            RealtimeMsg(string.Format("进入第{0}测试",nGroup));
            if (!proGroups.ContainsKey(nGroup))
            {
                //发送失败给自动化               
                string ackMsg = string.Format("TEST;FAIL;组号不存在！\r\n");
                SendToAutomation(ackMsg, ref errMsg);
                return;
            }
            if (srcDevice == null|| srcDevice2 == null)
            {
                string ackMsg = string.Format("TEST;FAIL;请先确认集成光源是否连接成功再进行测试！\r\n");
                SendToAutomation(ackMsg, ref errMsg);
                //发送失败给自动化
                return;
            }

            int nIdx = -1;
            GroupPorts testPort = proGroups[nGroup];
            foreach(MESTestInfo info in templateControl.AllTestInfo)
            {
                nIdx++;
                if (info.TestParam == MESParam.WDL || info.TestParam == MESParam.TDL)
                {
                    continue;
                }
                if (info.PortNameForUser.ToUpper().TrimEnd().TrimStart() == testPort.LeftPort.ToUpper().TrimEnd().TrimStart())
                {
                    AutoTestInfo autoInfo = new AutoTestInfo();
                    autoInfo.TestIdx = nIdx;
                    autoInfo.LeftOrRight = 1;
                    autoInfo.PortName = info.PortNameForUser;
                    allTestItems.Add(autoInfo);
                    
                }
                if (info.PortNameForUser.ToUpper().TrimEnd().TrimStart() == testPort.RightPort.ToUpper().TrimEnd().TrimStart())
                {
                    AutoTestInfo autoInfo = new AutoTestInfo();
                    autoInfo.TestIdx = nIdx;
                    autoInfo.LeftOrRight = 2;
                    autoInfo.PortName = info.PortNameForUser;
                    allTestItems.Add(autoInfo);
                }                
            }
            
            if (allTestItems.Count>0)
            {
                RealtimeMsg(string.Format("进入{0}测试", allTestItems[0].PortName));
                //选中当前测试行
                IndexMap nextSeleted = new IndexMap();
                nextSeleted.ProductIndex = 0;
                nextSeleted.ParamIndex.Add(allTestItems[0].TestIdx);
                UpdateItem(templateControl.AllTestInfo[allTestItems[0].TestIdx], allTestItems[0].TestIdx, nextSeleted);
                curTestItemIdx = allTestItems[0].TestIdx;
                /*if(selectItem==null)
                {
                    selectItem = nextSeleted;
                }*/
                if (CurItemIsSelected(0, allTestItems[0].TestIdx))
                    btnTest_Click(this, null);
            }
        }
        public void SeverDataDeal(string revData)
        {

            bkAutomationDeal.ReportProgress(1, revData);
        }

        private BackgroundWorker bkAutomationDeal;

        private int automationType = 0;
        private int curReferenceIdx = -1;
        private void btnILRef_Click(object sender, RoutedEventArgs e)
        {
            using (Mutex m = new Mutex(true, "powermeter"))
            {
                if (isRealShowPower == false)
                    return;
            }
            string prompt = "";
            MessageBoxResult boxRes;
            if(automationType==1)
            {
                prompt = string.Format("请将左光源线接到功率计后再开始归零！");
                curRefChan = 1;
            }
            else
                prompt = string.Format("请将光源线接到功率计后再开始归零！");
            RealtimeMsg(prompt);
            boxRes = MessageBox.Show(prompt, "归零", MessageBoxButton.OKCancel);
            if (boxRes == MessageBoxResult.Cancel)
            {
                RealtimeMsg("取消");
                return;
            }
            else if (boxRes == MessageBoxResult.OK)
            {
                if (automationType == 1)
                {
                    //清除所有归零数据
                    for (int i = 0; i < templateControl.AllTestInfo.Count; i++)
                    {
                        templateControl.UpdateILRefData(i, CommonFunction.GetDefaultValue());
                    }
                    if (srcDevice == null|| srcDevice2 == null)
                    {
                        MessageBox.Show("请先确认集成光源是否连接成功再进行归零！");
                        RealtimeMsg("请先确认集成光源是否连接成功再进行归零！");
                        //rjf test
                        return;
                    }
                    bkReferece.RunWorkerAsync(1);
                }
                else
                {
                    string errMsg = "";
                    int nRes = 0;
                    if (powermeter == null)
                    {
                        MessageBox.Show("请先确认功率计是否连接成功再进行归零！");
                        RealtimeMsg("请先确认功率计是否连接成功再进行归零！");
                        
                        return;
                    }


                    if (srcDevice == null)
                    {
                        //MessageBox.Show("请先确认光源是否连接成功再进行归零！");
                        RealtimeMsg("请先确认光源是否连接成功再进行归零！");
                        if (selectItem != null)
                        {
                            curReferenceIdx = selectItem.ParamIndex[0];
                            templateControl.UpdateILRefData(curReferenceIdx, CommonFunction.GetDefaultValue());
                        }
                        //rjf test
                        //return;
                    }
                    else
                    {
                        if (!isRefOneChanel)
                        {
                            //连接光源，则一键归零
                            //清除所有归零数据
                            for (int i = 0; i < templateControl.AllTestInfo.Count; i++)
                            {
                                templateControl.UpdateILRefData(i, CommonFunction.GetDefaultValue());
                            }
                        }
                        
                    }
                    using (Mutex m = new Mutex(true, "powermeter"))
                    {
                        isRealShowPower = false;
                    }
                    bkReferece.RunWorkerAsync(0);
                }
               
            }
        }

        public delegate void RealTimeMsgDelegate(string message);
        public void RealtimeMsgShow(string message)
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
        /// 实时状态列表信息显示
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private void RealtimeMsg(string message, StatusType type = StatusType.Normal)
        {
            try
            {
                string logPath = "";
                DataDicCheck(ref logPath);
                CommonFunction.WriteLog(message, logPath);
                object[] param = new object[1];
                param[0] = message;
                this.Dispatcher.Invoke(new RealTimeMsgDelegate(RealtimeMsgShow), param);
            }
            catch(Exception ex)
            {
                string prompt = "实时信息显示出错：" + ex.Message;
                MessageBox.Show(prompt);
            }
        }

        private void ParserPortGroup()
        {
            proGroups.Clear();
            for (int nIdx = 0; nIdx < templateControl.CFGInfo.Count; nIdx++)
            {
                if (templateControl.CFGInfo[nIdx].Name.ToUpper().Contains("GROUP"))
                {
                    int groupIdx = Convert.ToInt32(templateControl.CFGInfo[nIdx].Name.ToUpper().Replace("GROUP", ""));
                    if (proGroups.ContainsKey(groupIdx))
                    {
                        RealtimeMsg("Port分组组号重复，请检查！");
                        ErrorBox("Port分组组号重复，请检查！");
                        return;
                    }
                    string[] portsSplits = templateControl.CFGInfo[nIdx].Value.Split(';');
                    if (portsSplits.Length < 2)
                    {
                        RealtimeMsg("Port分组出错，请检查！");
                        ErrorBox("Port分组出错，请检查！");
                        return;
                    }
                    GroupPorts ports = new GroupPorts();
                    if (portsSplits[0].ToUpper().Contains("(L)"))
                    {
                        ports.LeftPort = portsSplits[0].ToUpper().Replace("(L)", "");
                    }
                    else if (portsSplits[0].ToUpper().Contains("(R)"))
                    {
                        ports.RightPort = portsSplits[0].ToUpper().Replace("(R)", "");
                    }

                    if (portsSplits[1].ToUpper().Contains("(L)"))
                    {
                        ports.LeftPort = portsSplits[1].ToUpper().Replace("(L)", "");
                    }
                    else if (portsSplits[1].ToUpper().Contains("(R)"))
                    {
                        ports.RightPort = portsSplits[1].ToUpper().Replace("(R)", "");
                    }
                    proGroups.Add(groupIdx, ports);
                }
            }
        }

        private void SendToAutomation(string sendData,ref string errMsg)
        {
            string sendRec = string.Format("Send:{0}", sendData);
            RealtimeMsg(sendRec);
            if(!cltSocket.SendData(sendData, ref errMsg))
            {
                IAutomation auto = null;
                DeviceControl.GetAutomationInIndex(1, ref auto, ref errMsg);
                if (auto != null)
                {                    
                    string host = "";
                    int port = 0;
                    if (auto.GetIPAndPort(ref host, ref port) == 0)
                    {
                        cltSocket = new ClientSocket(host, port);
                        if (!cltSocket.ConnectSever(ref errMsg))
                        {
                            RealtimeMsg(errMsg);
                        }
                        else
                        {
                            //callBack = CallBackSeverDataDeal;
                            cltSocket.SeverDataDeal += SeverDataDeal;
                            RealtimeMsg("连接自动化服务器成功！");
                            cltSocket.SendData(sendData, ref errMsg);
                        }
                    }
                }
            }
        }

        private bool isAdjustProcess = true;

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                templateControl.ClearAllData();
                proGroups.Clear();
                //功率计复位
                ResetPWM();

                selectItem = null;
                if (txtBoxSN.Text == "")
                {
                    WarningBox("请输入产品号！！");
                    return;
                }

                //if(omsProcess.ToUpper()!="PREADJUST"&& omsProcess.ToUpper() != "ADJUST"||omsProcess.ToUpper() != "TEST5")
                List<string> sptProcess = new List<string>();
                sptProcess.Add("PREADJUST");
                sptProcess.Add("ADJUST");
                sptProcess.Add("TEST5");
                sptProcess.Add("TEST6");
                string errMsg = "";
                string tmpltContent = templateControl.OpenTemplate(uiVariable.SN, mainInfo.TestProcess, mainInfo.UserID, "", false, Environment.MachineName, sptProcess, out templateName, out errMsg);
                if (tmpltContent.Length > 0)
                {
                    if (errMsg != "")
                    {
                        CommonFunction.WriteLog(errMsg);
                        if (automationType == 1)
                        {
                            string ackMsg = string.Format("SNNO;{0};FAIL;{1}\r\n", uiVariable.SN, errMsg);
                            SendToAutomation(ackMsg, ref errMsg);
                        }
                        else
                        {
                            string prompt = "打开模板出错：" + errMsg;
                            RealtimeMsg(prompt);
                            ErrorBox(prompt);
                        }
                        return;
                    }
                    
                    if (automationType == 1)
                    {
                        //Port分组解析
                        ParserPortGroup();
                        string ackMsg = string.Format("SNNO;{0};PASS", uiVariable.SN);
                        int[] groupKeys = proGroups.Keys.ToArray();
                        for (int nKey = 0; nKey < groupKeys.Length; nKey++)
                        {
                            MESTestInfo leftInfo = null;
                            MESTestInfo rightInfo = null;
                            foreach (MESTestInfo info in templateControl.AllTestInfo)
                            {
                                if (info.PortNameForUser.TrimEnd().TrimStart().ToUpper() == proGroups[groupKeys[nKey]].LeftPort.TrimEnd().TrimStart().ToUpper())
                                {
                                    if (leftInfo == null)
                                        leftInfo = info;
                                }
                                if (info.PortNameForUser.TrimEnd().TrimStart().ToUpper() == proGroups[groupKeys[nKey]].RightPort.TrimEnd().TrimStart().ToUpper())
                                {
                                    if (rightInfo == null)
                                        rightInfo = info;
                                }
                                if (leftInfo != null && rightInfo != null)
                                    break;
                            }
                            if (leftInfo != null&& rightInfo != null)
                                ackMsg += string.Format(";{0}:{1},{2}:{3}", leftInfo.WLLeft, leftInfo.Criterion1, rightInfo.WLLeft, rightInfo.Criterion1);
                        }
                        ackMsg += "\r\n";
                        SendToAutomation(ackMsg, ref errMsg);
                    }
                    uiVariable.Spec = templateControl.productInfo.SpecNum;
                    uiVariable.PN = templateControl.productInfo.ProductPN;

                    //OMSProcess = templateControl.GetOplinkProcess(templateControl.productInfo.ProductPN, mainInfo.TestProcess, ref errMsg);
                    errMsg = "";
                    //读取归零数据
                    templateControl.ReadRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);
                    if (errMsg != "")
                    {
                        CommonFunction.WriteLog(errMsg);
                        errMsg = "";
                    }

                    RealtimeMsg("模板打开成功！");
                    passOrFailImg.Source = passBitmapImage;

                    templateControl.SaveTestType("0");
                    if (mainInfo.LoginMode.ToUpper().Contains("DEBUG"))
                    {
                        templateControl.SavePermsLevel("1");
                    }
                    else if (mainInfo.LoginMode.ToUpper().Contains("RD"))
                    {
                        templateControl.SavePermsLevel("2");
                    }
                    else
                    {
                        templateControl.SavePermsLevel("0");
                    }
                    templateControl.SaveSoftwareInfo("SW2036_LLCC_ATSFTS", "V1.0.2.0", "Jinfang Ruan", "12/23/2021");

                    List<FusionControl> controls = new List<FusionControl>();
                    controls.Add(templateControl);
                    //更新测试信息
                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                    }

                    int i = 0;
                    foreach (MESTestInfo info in templateControl.AllTestInfo)
                    {
                        UpdateItem(info, i);
                        i++;
                    }
                    //选中当前测试行
                    IndexMap nextSeleted = new IndexMap();
                    nextSeleted.ProductIndex = 0;
                    nextSeleted.ParamIndex.Add(templateControl.AllTestInfo.Count - 1);
                    UpdateItem(templateControl.AllTestInfo[templateControl.AllTestInfo.Count - 1], templateControl.AllTestInfo.Count - 1, nextSeleted);

                    ShowTemplatePath();

                    if (isAdjustProcess)
                    {
                        if (IsLighted())
                        {
                            uiVariable.IsSaveEnable = true;
                        }
                    }
                    else
                    {
                        uiVariable.IsSaveEnable = true;
                        uiVariable.IsLightedEnable = false;
                    }
                    
                    BackgroundWorker bk = new BackgroundWorker();
                    bk.DoWork += SelctToItemBegin_DoWork;
                    bk.RunWorkerCompleted += SelctToItemBegin_RunWorkerCompleted;
                    bk.RunWorkerAsync();

                }
                else
                {
                    WarningBox(errMsg);
                    txtBoxSN.Text = "";
                    if (automationType == 1)
                    {
                        string ackMsg = string.Format("SNNO;{0};FAIL;{1}\r\n", uiVariable.SN, errMsg);
                        SendToAutomation(ackMsg, ref errMsg);
                    }
                    else
                    {
                        CommonFunction.WriteLog(errMsg);
                        txtBoxSN.Focus();
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                RealtimeMsg(string.Format("打开模板(Exception):", ex.Message));
            }
        }

        public void SelctToItemBegin_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }
        public void SelctToItemBegin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IndexMap nextSeleted = new IndexMap();
            nextSeleted.ProductIndex = 0;
            nextSeleted.ParamIndex.Add(0);
            UpdateItem(templateControl.AllTestInfo[0], 0, nextSeleted);
        }

        private EventWaitHandle powerEvent = new AutoResetEvent(false);
        public  void Test_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                int[] allTestArray = (int[])e.Argument;
                string errMsg = "";
                List<double> allPwer = new List<double>();
                if (automationType == 1)
                {
                    int nCurItem = 0;
                    for (int j = 0; j < allTestItems.Count; j++)
                    {
                        if (curTestItemIdx == allTestItems[j].TestIdx)
                        {
                            nCurItem = j;
                            break;
                        }
                    }

                    if (srcDevice != null && allTestItems[nCurItem].LeftOrRight == 1)
                    {
                        int nRes = srcDevice.SetWavelength(templateControl.AllTestInfo[curTestItemIdx].WLLeft, ref errMsg);

                    }
                    if (srcDevice2 != null && allTestItems[nCurItem].LeftOrRight == 2)
                    {
                        int nRes = srcDevice2.SetWavelength(templateControl.AllTestInfo[curTestItemIdx].WLLeft, ref errMsg);
                    }
                    if (pdlCtrl != null)
                    {
                        if (0 != pdlCtrl.DoPDL(0, ref errMsg))
                        {
                            //发送异常给自动化
                            string errPrompt = string.Format("控制偏振控制器出错：{0}", errMsg);
                            e.Result = errPrompt;
                            return;
                        }
                    }
                    int i = 0;
                    while (true)
                    {
                        //偏执控制器停止，则退出
                        if (pdlCtrl != null && pdlCtrl.IsPDLFinish(ref errMsg))
                        {
                            if (errMsg.Length > 0)
                            {
                                //发送异常给自动化
                                string errPrompt = string.Format("控制偏振控制器出错：{0}", errMsg);
                                e.Result = errPrompt;
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }

                        

                        string askPower = string.Format("ASK;{0}\r\n", allTestItems[nCurItem].LeftOrRight);
                        cltSocket.SendData(askPower, ref errMsg);
                        if (!powerEvent.WaitOne(5 * 1000))
                        {
                            //发送异常给自动化
                            string errPrompt = string.Format("读取功率计超时！");
                            e.Result = errPrompt;
                            return;
                        }
                        string res = AskPowerRes;
                        AskPowerRes = "";
                        if (res == "" || res.ToUpper().Contains("ERROR"))
                        {
                            //发送异常给自动化
                            string errPrompt = string.Format("读取功率计超时：{0}", errMsg);
                            e.Result = errPrompt;
                            return;
                        }
                        else
                        {
                            allPwer.Add(Convert.ToDouble(res));
                        }
                        bkTest.ReportProgress(i, res);                        
                        i++;
                    }
                }
                else
                {
                    for (int i = 0; i < powerCount; i++)
                    {
                        int nBeginTick = Environment.TickCount;
                        List<double> readPower = new List<double>();
                        int nRes = 0;
                        if (powermeter != null)
                        {
                            nRes = powermeter.ReadPowerAvg(ref errMsg, out readPower);
                            int nReadCount = 0;
                            while (nRes != 0)
                            {
                                nRes = powermeter.ReadPowerAvg(ref errMsg, out readPower);
                                nReadCount++;
                                if (nReadCount > 3)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            string errPrompt = string.Format("功率计未连接!");
                            e.Result = errPrompt;
                            return;
                        }

                        if (nRes != 0)
                        {
                            string errPrompt = string.Format("读取功率计出错：{0}", errMsg);
                            e.Result = errPrompt;
                            return;
                        }
                        allPwer.Add(readPower[0]);
                        bkTest.ReportProgress(i, readPower[0].ToString());                      
                    }
                }
                for (int i = 0; i < allTestArray.Length; i++)
                {
                    //RealtimeMsg(string.Format("当前测试idx:{0},productidx:{1}", i, allTestArray[i]));
                    MESTestInfo info = templateControl.AllTestInfo[allTestArray[i]];
                    if (info.TestParam == MESParam.MaxIL|| info.TestParam == MESParam.ISO || info.TestParam == MESParam.PeakIL|| info.TestParam == MESParam.IL)
                    {
                        bool isPass = true;
                        double dMaxIL = -algorithm.MaxIL(allPwer.ToArray(), info.ILRef, ref errMsg);
                        templateControl.UpdateTestData(allTestArray[i], dMaxIL, ref isPass);

                    }
                    else if (info.TestParam == MESParam.PDL)
                    {
                        bool isPass = true;
                        double dPDL = algorithm.PDL(allPwer.ToArray(), ref errMsg);
                        templateControl.UpdateTestData(allTestArray[i], dPDL, ref isPass);
                    }
                    else if (info.TestParam == MESParam.RL)
                    {
                        bool isPass = true;
                        double dRL = algorithm.RL(allPwer.ToArray(), info.ILRef, info.RLRef, ref errMsg);
                        templateControl.UpdateTestData(allTestArray[i], dRL, ref isPass);
                    }
                    for (int j = 0; j < allTestItems.Count; j++)
                    {
                        if (allTestItems[j].TestIdx == allTestArray[i])
                        {
                            allTestItems[j].isTested = true;
                            break;
                        }
                    }
                    //bkTest.ReportProgress(allTestArray[i]);
                }
            }
            catch(Exception ex)
            {
                e.Result = ex.Message;
            }
        }

        private bool CurItemIsSelected(int nProdIdx,int nCurIdx)
        {
            if(selectItem!=null)
            {
                foreach(int nIdx in selectItem.ParamIndex)
                {
                    if(nIdx==nCurIdx&&nProdIdx==selectItem.ProductIndex)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Test_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            using (Mutex m = new Mutex(true, "powermeter"))
            {
                isRealShowPower = true;
            }
            uiVariable.IsEnable = true;
            isOnTesting = false;
            string errMsg = "";
            if (e.Result!=null&&e.Result.ToString().Length>0)
            {               
                RealtimeMsg(e.Result.ToString());
                if (automationType == 1)
                {
                    allTestItems.Clear();
                    string ackMsg = string.Format("TEST;FAIL;{0}\r\n", e.Result.ToString());
                    SendToAutomation(ackMsg, ref errMsg);
                }
                else
                {
                    ErrorBox(e.Result.ToString());
                }                                 
                return;
            }

            int i = 0;
            foreach (MESTestInfo info in templateControl.AllTestInfo)
            {
                if(info.TestParam==MESParam.WDL)
                {
                    List<double> allIL = new List<double>();
                    foreach (MESTestInfo wdlInfo in templateControl.AllTestInfo)
                    {
                        if(info.EnvironmentID==wdlInfo.EnvironmentID&&info.ObjectID==wdlInfo.ObjectID&&info.PortID==wdlInfo.PortID
                            && wdlInfo.TestParam==MESParam.MaxIL)
                        {
                            if((wdlInfo.WLLeft>info.WLLeft&&wdlInfo.WLLeft<info.WLRight)||Math.Abs(wdlInfo.WLLeft - info.WLLeft)<0.0001|| Math.Abs(wdlInfo.WLLeft - info.WLRight)<0.001)
                            {
                                allIL.Add(wdlInfo.CurValue);
                            }
                        }
                    }
                    
                    double dWDL = algorithm.WDL(allIL.ToArray(), ref errMsg);
                    bool isPass = true;
                    templateControl.UpdateTestData(i, dWDL, ref isPass);
                    UpdateItem(templateControl.AllTestInfo[i], i);

                    for (int j = 0; j < allTestItems.Count; j++)
                    {
                        if (allTestItems[j].TestIdx == i)
                        {
                            allTestItems[j].isTested = true;
                            break;
                        }
                    }
                }
                else if (info.TestParam == MESParam.TDL)
                {
                    UpdateItem(templateControl.AllTestInfo[i], i);
                }
                else
                    UpdateItem(info, i);
                i++;               
            }
            UpdateResIcon();

            if (automationType == 1)
            {
                bool isFinished = true;
                for (int j = 0; j < allTestItems.Count; j++)
                {
                    if (!allTestItems[j].isTested)
                    {
                        //选中当前测试行
                        IndexMap nextSeleted = new IndexMap();
                        nextSeleted.ProductIndex = 0;
                        nextSeleted.ParamIndex.Add(allTestItems[j].TestIdx);                
                        curTestItemIdx = allTestItems[j].TestIdx;
                        //RealtimeMsg(string.Format("complet 查找下组idx{0}", curTestItemIdx));
                        UpdateItem(templateControl.AllTestInfo[allTestItems[j].TestIdx], allTestItems[j].TestIdx, nextSeleted);
                        /*if(CurItemIsSelected(0, allTestItems[j].TestIdx))
                            btnTest_Click(this, null);*/
                        isFinished = false;
                        break;
                    }
                }
                if (isFinished)
                {
                    allTestItems.Clear();
                    string ackMsg = string.Format("TEST;PASS\r\n");
                    SendToAutomation(ackMsg, ref errMsg);
                }
            }
        }

        public void Test_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //更新曲线
           if(e.ProgressPercentage==0)
            {
                UpdateCurveShow("IL", CurveUpdate.FirstPoint, e.ProgressPercentage + 1, Convert.ToDouble(e.UserState));
            }
            else
            {
                UpdateCurveShow("IL", CurveUpdate.AddPoint, e.ProgressPercentage + 1, Convert.ToDouble(e.UserState));
            }
            
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (bkTest.IsBusy)
                    return;

                
                if (selectItem == null || selectItem.ParamIndex.Count == 0)
                    return;
                
                int[] allTestIdx = selectItem.ParamIndex.ToArray();
                int nTestIndex = allTestIdx[0];

                MESTestInfo info = templateControl.AllTestInfo[nTestIndex];
                if (info.TestParam == MESParam.WDL || info.TestParam == MESParam.TDL)
                {
                    RealtimeMsg("WDL/TDL无需单独测试！");
                    ErrorBox("WDL/TDL无需单独测试！");
                    return;
                }
                string errMsg = "";
                

                if (automationType == 1)
                {
                    if (!templateControl.GetAllRef(ref errMsg))
                    {
                        RealtimeMsg("请先归零再测试！");
                        allTestItems.Clear();
                        string ackMsg = string.Format("TEST;FAIL;请先归零再测试！\r\n");
                        SendToAutomation(ackMsg, ref errMsg);
                        return;
                    }
                }
                else
                {
                    if (!templateControl.GetAllRef(ref errMsg))
                    {
                        RealtimeMsg("请先归零再测试！");
                        ErrorBox("请先归零再测试！");
                        return;
                    }
                    if (powermeter == null)
                    {
                        RealtimeMsg("请先确认功率计是否连接成功再进行测试！");
                        ErrorBox("请先确认功率计是否连接成功再进行测试！");
                        return;
                    }

                    if (srcDevice == null)
                    {
                        //ErrorBox("请先确认光源是否连接成功再进行测试！");
                        RealtimeMsg("请先确认光源是否连接成功再进行测试！");
                        //rjf test
                        //return;
                    }

                    if (info.ObjectID.Contains("_UV") && (!dicUVRecord.ContainsKey(uiVariable.SN)))
                    {
                        uiVariable.IsEnable = false;
                        ErrorBox("该通道未照光，无法进行照光后测试");
                        RealtimeMsg("该通道未照光，无法进行照光后测试");
                        return;
                    }


                    if ((!info.ObjectID.Contains("_UV")) && dicUVRecord.ContainsKey(uiVariable.SN))
                    {
                        uiVariable.IsEnable = false;
                        ErrorBox("该通道已照光，无法进行照光前测试！");
                        RealtimeMsg("该通道已照光，无法进行照光前测试！");
                        return;
                    }
                    

                    int nRes = 0;
                    if (srcDevice != null)
                        nRes = srcDevice.SetWavelength(info.WLLeft, ref errMsg);
                    if (nRes != 0)
                    {
                        string errPrompt = string.Format("切换光源出错：{0}", errMsg);
                        RealtimeMsg(errPrompt);
                        ErrorBox(errPrompt);
                        return;
                    }

                    if (powermeter != null)
                    {
                        using (Mutex m = new Mutex(true, "powermeter"))
                        {
                            isRealShowPower = false;
                        }
                        nRes = powermeter.SetPMWavelength(info.WLLeft, ref errMsg);
                        if(nRes!=0)
                        {
                            nRes = powermeter.SetPMWavelength(info.WLLeft, ref errMsg);
                        }
                    }
                    if (nRes != 0)
                    {
                        string errPrompt = string.Format("功率计切换波长出错：{0}", errMsg);
                        RealtimeMsg(errPrompt);
                        ErrorBox(errPrompt);
                        return;
                    }
                    Thread.Sleep(1000);
                }
                if (!bkTest.IsBusy)
                {
                    bkTest.RunWorkerAsync(allTestIdx);
                    isOnTesting = true;
                    uiVariable.IsEnable = false;
                }
            }
            catch(Exception ex)
            {
                string errPrompt = string.Format("{0}", ex.Message);
                RealtimeMsg(errPrompt);
                ErrorBox(errPrompt);
                return;
            } 
        }

        /// <summary>
        /// 更新测试结果ICON
        /// </summary>
        private void UpdateResIcon()
        {
            string errMsg = "";
            passOrFailImg.Source = passBitmapImage;
            if (!templateControl.GetAllTestedPassed(ref errMsg))
            {
                passOrFailImg.Source = failBitmapImage;
            }
        }

        
        private void btnUVata_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //检查是否有选中通道，并且测试过数据，如无，提示需要测试
                if (selectItem == null || selectItem.ParamIndex.Count == 0)
                {
                    string errPrompt = string.Format("请选择一个端口照光, 空行不能照光!");
                    RealtimeMsg(errPrompt);
                    ErrorBox(errPrompt);
                    return;
                }
                MESTestInfo selctInfo = templateControl.AllTestInfo[selectItem.ParamIndex[0]];
                if (dicUVRecord.ContainsKey(uiVariable.SN))
                {
                    string errPrompt = string.Format(" 当前通道已经完成照光！");
                    RealtimeMsg(errPrompt);
                    ErrorBox(errPrompt);
                    return;
                }
                foreach (MESTestInfo testInfo in templateControl.AllTestInfo)
                {
                    if ((!testInfo.ObjectID.Contains("_UV")) && testInfo.PortID == selctInfo.PortID)
                    {
                        if (!testInfo.Tested)
                        {
                            string errPrompt = string.Format(" 当前通道照光前数据未测试完, 不能照光");
                            RealtimeMsg(errPrompt);
                            ErrorBox(errPrompt);
                            return;
                        }
                    }
                }
                //如果有，保存数据后启动照光软件
                string errMsg = "";
                string savePath = "";// Environment.CurrentDirectory+"\\data\\";
                DataDicCheck(ref savePath);
                savePath += "\\";
                savePath += uiVariable.SN +"_" + selctInfo.PortNameForUser + ".xml";
                //string savePath = System.Environment.CurrentDirectory + "\\data\\" + uiVariable.SN + "_" + selctInfo.PortNameForUser + ".xml";
                if(!templateControl.UploadTestData(savePath, out errMsg))
                {
                    string prompt = string.Format("保存数据出错：{0}", errMsg);
                    RealtimeMsg(prompt);
                    ErrorBox(prompt);
                    //return;
                }              

                //写SN文件，启动照光程序
                string snPath = System.Environment.CurrentDirectory + "\\LightData\\"+ uiVariable.SN+"-CRC.dat";
                if (File.Exists(snPath))
                {
                    File.Delete(snPath);
                }

                string vuRecPath = System.Environment.CurrentDirectory + "\\sn.txt";

                StreamWriter sw = new StreamWriter(vuRecPath, false);
                string wrContent = string.Format("{0}", uiVariable.SN);
                sw.WriteLine(wrContent);
                sw.Close();

                Clipboard.Clear();
                Clipboard.SetText(templateControl.ProductSN);
                //启动照光程序
                ProcessStartInfo info = new ProcessStartInfo();
                info.WindowStyle = ProcessWindowStyle.Normal;
                info.FileName = System.Environment.CurrentDirectory + "\\light.exe";//需要启动的程序
                if(!File.Exists(info.FileName))
                {
                    errMsg = "文件不存在：" + info.FileName;
                    RealtimeMsg(errMsg);
                    ErrorBox(errMsg);
                    return;
                }

                int nIdx = 0;
                foreach (MESTestInfo testInfo in templateControl.AllTestInfo)
                {
                    if ((!testInfo.ObjectID.Contains("_UV")) && testInfo.PortID == selctInfo.PortID)
                    {
                        bool isPass = true;
                        templateControl.UpdateTestData(nIdx, CommonFunction.GetDefaultValue(), ref isPass);
                        templateControl.AllTestInfo[nIdx].Tested = false;
                        UpdateItem(templateControl.AllTestInfo[nIdx], nIdx);
                    }
                    nIdx++;
                }

                Process.Start(info);
                uiVariable.IsEnable = false;
                uiVariable.IsSaveEnable = false;
                uiVariable.IsLightedEnable = false;

                //启动线程检查是否照光完成
                BackgroundWorker bkUV = new BackgroundWorker();
                bkUV.DoWork += CheckLight_DoWork;
                bkUV.RunWorkerCompleted += CheckLight_Completed;
                bkUV.RunWorkerAsync();
            }
            catch(Exception ex)
            {
                RealtimeMsg(ex.Message);
                ErrorBox(ex.Message);
            }
        }

        private bool IsLighted()
        {
            string snPath = System.Environment.CurrentDirectory + "\\LightData\\" + uiVariable.SN + "-CRC.dat";
            if (File.Exists(snPath))
            {
                /*StreamReader strRd = new StreamReader(snPath);
                string readLine = strRd.ReadLine();
                string[] splits = readLine.Split('_');
                if (!dicUVRecord.ContainsKey(splits[splits.Length - 1]))
                {
                    dicUVRecord.Add(splits[splits.Length - 1], 1);
                    break;
                }*/
                dicUVRecord.Add(uiVariable.SN, 1);
                return true;
            }
            return false;
        }

        public void CheckLight_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                //写SN文件，启动照光程序
                if(IsLighted())
                {
                    break;
                }
            }
        }

        public void CheckLight_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            uiVariable.IsEnable = true;
            uiVariable.IsSaveEnable = true;
            uiVariable.IsLightedEnable = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            powermeterRealtimeThread.Abort();
            refTimeCheckBK.CancelAsync();
        }

        private void PassOrFail_Load(object sender, RoutedEventArgs e)
        {
            InitPassFailImage();
            //设置图片显示大小，将图片放大1.5倍
            passOrFailImg.Height = passBitmapImage.Width * 1.5;
            passOrFailImg.Width = passBitmapImage.Width * 1.5;

            passOrFailImg.Source = passBitmapImage;
        }

        private void DataDicCheck(ref string savePath)
        {
           /*savePath = Environment.CurrentDirectory + "\\data";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }*/
            
            string specPath = templateControl.productInfo.SpecNum;
            specPath = specPath.Replace('/', '-');
            specPath = specPath.Replace('\\', '-');
            specPath = specPath.Replace('>', '-');
            specPath = specPath.Replace('<', '-');
            savePath = dataServerPath + specPath;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            savePath += "\\";
            savePath += templateControl.productInfo.ProductPN;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            savePath += "\\";
            savePath += templateControl.ProductSN;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }

        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RealtimeMsg("保存数据");
                //如果有，保存数据后启动照光软件
                string errMsg = "";
                string savePath = "";// Environment.CurrentDirectory+"\\data\\";
                DataDicCheck(ref savePath);
                savePath += "\\";
                savePath += uiVariable.SN + ".xml";

                if(automationType!=1)
                {
                    foreach (MESTestInfo testInfo in templateControl.AllTestInfo)
                    {
                        if (testInfo.ObjectID.Contains("_UV"))
                        {
                            if (!testInfo.Tested)
                            {
                                string errPrompt = string.Format(" 照光后数据未测试完, 是否需要保存？");

                                if (MessageBox.Show(errPrompt, "询问", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }

                

                if (!templateControl.UploadTestData(savePath, out errMsg))
                {
                    if (automationType == 1)
                    {
                        string ackMsg = string.Format("SAVE;FAIL;{0}\r\n", errMsg);
                        SendToAutomation(ackMsg, ref errMsg);
                    }
                    else
                    {
                        string prompt = string.Format("保存数据出错：{0}", errMsg);
                        RealtimeMsg(prompt);
                        ErrorBox(prompt);
                    }
                }
                else
                {
                    if (automationType == 1)
                    {
                        string ackMsg = string.Format("SAVE;PASS\r\n", errMsg);
                        SendToAutomation(ackMsg, ref errMsg);
                    }
                    else
                    {
                        string prompt = string.Format("保存数据完成！");
                        RealtimeMsg(prompt);
                    }

                    templateControl.ProductSN= "";
                    templateControl.AllTestInfo.Clear();
                    // 更新测试信息
                    if (EventAggregator != null)
                    {
                        List<FusionControl> shows = new List<FusionControl>();
                        shows.Add(templateControl);
                        EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
                    }
                    uiVariable.SN = "";
                }
            }
            catch(Exception ex)
            {
                string prompt = string.Format("保存数据出错：{0}", ex.Message);
                RealtimeMsg(prompt);
                ErrorBox(prompt);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
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

        private void btnRLRef_Click(object sender, RoutedEventArgs e)
        {
            using (Mutex m = new Mutex(true, "powermeter"))
            {
                if (isRealShowPower == false)
                    return;
            }
            string prompt = "";
            MessageBoxResult boxRes;
           
            prompt = string.Format("请将光源线绕死，RL头接到功率计后再开始归零！");
            RealtimeMsg(prompt);
            boxRes = MessageBox.Show(prompt, "归零", MessageBoxButton.OKCancel);
            if (boxRes == MessageBoxResult.Cancel)
            {
                RealtimeMsg("取消");
                return;
            }
            else if (boxRes == MessageBoxResult.OK)
            {
                
                
                string errMsg = "";
                int nRes = 0;
                if (powermeter == null)
                {
                    MessageBox.Show("请先确认功率计是否连接成功再进行归零！");
                    RealtimeMsg("请先确认功率计是否连接成功再进行归零！");
                        
                    return;
                }


                if (srcDevice == null)
                {
                    //MessageBox.Show("请先确认光源是否连接成功再进行归零！");
                    RealtimeMsg("请先确认光源是否连接成功再进行归零！");
                    if (selectItem != null)
                        curReferenceIdx = selectItem.ParamIndex[0];
                }
                else
                {
                    if (!isRefOneChanel)
                    {
                        //清除所有归零数据
                        for (int i = 0; i < templateControl.AllTestInfo.Count; i++)
                        {
                            templateControl.UpdateRLRefData(i, CommonFunction.GetDefaultValue());
                        }
                    }
                }
                using (Mutex m = new Mutex(true, "powermeter"))
                {
                    isRealShowPower = false;
                }

                bkReferece.RunWorkerAsync(3);
            }
        }

        private bool isRealShowPower = true;

        private void btnReConnect_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            if(automationType==1)
            {
                IAutomation auto = null;
                DeviceControl.GetAutomationInIndex(1, ref auto, ref errMsg);
                if (auto != null)
                {
                    string host = "";
                    int port = 0;
                    if (auto.GetIPAndPort(ref host, ref port) == 0)
                    {
                        cltSocket = new ClientSocket(host, port);
                        if (!cltSocket.ConnectSever(ref errMsg))
                        {
                            RealtimeMsg(errMsg);
                        }
                        else
                        {
                            //callBack = CallBackSeverDataDeal;
                            cltSocket.SeverDataDeal += SeverDataDeal;
                            RealtimeMsg("连接自动化服务器成功！");
                        }
                    }
                }
            }
            else
            {
                using (Mutex m = new Mutex(true, "powermeter"))
                {
                    isRealShowPower = !isRealShowPower;
                }
            }
        }
    }

    public class GroupPorts
    {
        public string LeftPort { get; set; }
        public string RightPort { get; set; }
        public GroupPorts()
        {
            LeftPort = "";
            RightPort = "";
        }
    }

    public class AutoTestInfo
    {
        public string PortName { get; set;}
        public int LeftOrRight { get; set; }
        public int TestIdx { get; set; }
        public bool isTested { get; set; }
        public AutoTestInfo()
        {
            PortName = "";
            LeftOrRight = -1;
            TestIdx = -1;
            isTested = false;
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
    }
}
