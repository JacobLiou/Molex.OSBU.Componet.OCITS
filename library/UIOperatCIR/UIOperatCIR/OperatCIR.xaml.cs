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
using System.Runtime.InteropServices;

using MolexUtility;
using MolexUtility.Command;
using MolexUtility.Protocol;
using MolexUtility.UIList;
using MolexUtility.Device;
using MolexUtility.Algorithm;
using ProtocolAggregator;
using System.Windows.Interop;
using UDL2_ServerLib;

namespace UIOperatCIR
{
    /// <summary>
    /// Interaction logic for OperatCIR.xaml
    /// </summary>
    /// 
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperatCIR")]
    public partial class OperatCIR : UserControl
    {
        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsUrl = "http://172.18.1.101/amts/";

        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsSaveUrl = "http://172.18.1.101/amts/Atd_UploadMessage.asmx";

        private string refPath = Environment.CurrentDirectory + "\\reference\\refdata.ini";

        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

        /// <summary>
        /// 测试但未保存
        /// </summary>
        private bool isTestedUnSave = false;

        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 所有产品测试信息
        /// </summary>
        private MESControl productControl=new MESControl();
        //private FusionControl productControl = new FusionControl();


        /// <summary>
        /// 与其他模块通信的事件集 
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        [Import(typeof(IAlgotithm))]
        private IAlgotithm algorithm;


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
        /// 模板是否正确打开，并完成
        /// </summary>
        private bool isOpenTemplateComplete = false;

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
        /// 注册快捷集合
        /// </summary>
        readonly Dictionary<string, short> hotKeyDic = new Dictionary<string, short>();

        private const int rerefHours = 6;

        /// <summary>
        /// 单项测试线程
        /// </summary>
        BackgroundWorker bkSingle;

        /// <summary>
        /// 一键测试线程
        /// </summary>
        BackgroundWorker bkOnekey;

        BackgroundWorker bkReference;

        /// <summary>
        /// 停止一键测试
        /// </summary>
        private bool isStopOnekey;

        /// <summary>
        /// 是否是一键测试
        /// </summary>
        private bool isOnekeyTest = false;

        /// <summary>
        /// 设备控制 使用UDL
        /// </summary>
        /*[Import(typeof(IDeviceHandle))]
        public IDeviceHandle DeviceControl { get; set; }*/
        static UDL2_Engine deviceEngine = new UDL2_Engine();
        static UDL2_OPM powermeterCtrl = new UDL2_OPM();
        static UDL2_OSW opticalSWCtrl = new UDL2_OSW();

        /// <summary>
        /// 功率计GUID
        /// </summary>
        private const int OPMIL_GUID = 1;
        private const int OPMRL_GUID = 2;
        private const int OSW2X2_GUID = 1;
        private const int OSWINOUT_GUID = 2;
        private const int OSWA_GUID = 3;

        /// <summary>
        /// 端口定义，用于数组处理
        /// </summary>
        private const int IN_PORT = 0;
        private const int A_PORT = 1;
        private const int OUT_PORT = 2;

        /// <summary>
        /// src到三个端口的归零数据
        /// </summary>
        private double[][] srcToPortRef;

        /// <summary>
        /// pm到三个端口的归零数据
        /// </summary>
        private double[][] pmToPortRef;

        private double[][] systemRL;

        private double srcRefPower = 0;
        /// <summary>
        ///是否需要重新归零，24小时重新归两个1X64SW的第一路，如果与记录的归零数据差异
        ///超过0.05（配置），则删除归零文件，全部需要重新归零。否则其他通道无需重新归零
        /// </summary>
        private bool isRefAgain = true;

        public delegate void GetUDLMessageDelegate(ref string msg,ref bool isSuccess);
        

        public OperatCIR()
        {
            InitializeComponent();
            
            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            amtsSaveUrl = xmlSet.readStringData(CommonFunction.GetSaveWebservicSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");

            uiVariable.IsEnable = true;
            uiVariable.IsSaveEnable = false;
            txtBoxSN.DataContext = uiVariable;

            btnOpenTemplate.DataContext = uiVariable;
            btnSaveToAMTS.DataContext = uiVariable;
            btnPMReset.DataContext = uiVariable;
            btnOnekey.DataContext = uiVariable;

            btnScanRef.DataContext = uiVariable;
            btnSingleTest.DataContext = uiVariable;
            btnStopOnekey.DataContext = uiVariable;
            txtSpec.DataContext = uiVariable;
            txtPN.DataContext = uiVariable;
            uiVariable.IsEnable = false;
            uiVariable.IsStopScanVisible = Visibility.Hidden;
            uiVariable.IsOnekeyVisible = Visibility.Visible;
            srcToPortRef = new double[3][];
            pmToPortRef = new double[3][];
            systemRL = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                srcToPortRef[i] = new double[64];
                pmToPortRef[i] = new double[64];
                systemRL[i] = new double[64];
            }

            string errMsg = "";
            if (!UdlRuntimeConfig.IsUdlEngineLoadDisabled())
            {
                deviceEngine.SetDebugLogFile(Environment.CurrentDirectory + "\\log.txt");
                string udlCfg = Environment.CurrentDirectory + "\\set\\UDLConfig.xml";
                deviceEngine.LoadConfiguration(udlCfg);
                if(!GetMessage(ref errMsg))
                {
                    RealtimeMsg("加载UDL配置出错：" + errMsg);
                    return;
                }
                deviceEngine.OpenEngine();
                if (!GetMessage(ref errMsg))
                {
                    RealtimeMsg("UDL Open出错：" + errMsg);
                    return;
                }
            }
            

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

        /// <summary>
        /// 更新测试结果ICON
        /// </summary>
        private void UpdateResIcon()
        {
            string errMsg = "";
            passOrFailImg.Source = passBitmapImage;            
            if (!productControl.GetAllTestedPassed(ref errMsg))
            {
                passOrFailImg.Source = failBitmapImage;       
            }            
        }

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
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

        public static bool GetMessage(ref string msg)
        {
            try
            {
                string result = "";
                sbyte[] sbMsg = new sbyte[1024];
                byte[] bMsg = new byte[1024];
                
                deviceEngine.GetLastErrorMessage(out sbMsg[0], 1024);
                for (int i = 0; i < 1024; i++)
                {                    
                    bMsg[i] = (byte)sbMsg[i];
                }
                result = System.Text.Encoding.Default.GetString(bMsg);
                result = result.Substring(0, result.IndexOf('\0'));
                if (result.Length>7&& result.Substring(0, 8) == "NO ERROR")
                    return true;
                else
                {
                    msg = result;
                    return false;
                }
            }
            catch(Exception e)
            {
                msg = e.Message;
                return false;
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
        }

        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool GetOpenTemplateComplete()
        {
            return isOpenTemplateComplete;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool GetIsStopOnekey()
        {
            return isStopOnekey;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetIsStopOnekey(bool isStop)
        {
            isStopOnekey = isStop;
        }

        enum REFPORT
        {
            SRC_IN,
            SRC_A,
            PM_A,
            PM_OUT,
            RL_IN,
            RL_A,
            REF_TIME,
            REF_SRC
        }
        private bool SaveRefResult(REFPORT port, int chan)
        {
            IniParser paser;
            string section = "REFTIME";
            string key = "TIME";
            if (!File.Exists(refPath))
            {
                File.Create(refPath).Close();
                paser = new IniParser(refPath);
                paser.writeData(section, key, DateTime.Now.ToString());

            }
            else
            {
                paser = new IniParser(refPath);
            }

            switch (port)
            {
                case REFPORT.REF_SRC:
                    {
                        section = "REF_SRC";
                        key = "SRC";
                        paser.writeData(section, key, srcRefPower.ToString());
                    }
                    break;
                case REFPORT.REF_TIME:
                    {
                        section = "REFTIME";
                        key = "TIME";
                        paser.writeData(section, key, DateTime.Now.ToString());
                    }
                    break;
                case REFPORT.SRC_IN:
                    {
                        section = "SRC_TO_IN";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "SRC_IN" + (chan + 1).ToString();
                            paser.writeData(section, key, srcToPortRef[IN_PORT][chan].ToString());
                        }
                    }
                    break;
                case REFPORT.SRC_A:
                    {
                        section = "SRC_TO_A";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "SRC_A" + (chan + 1).ToString();
                            paser.writeData(section, key, srcToPortRef[A_PORT][chan].ToString());
                        }
                    }
                    break;
                case REFPORT.PM_A:
                    {
                        section = "PM_TO_A";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "PM_A" + (chan + 1).ToString();
                            paser.writeData(section, key, pmToPortRef[A_PORT][chan].ToString());
                        }
                    }
                    break;
                case REFPORT.PM_OUT:
                    {
                        section = "PM_TO_OUT";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "PM_OUT" + (chan + 1).ToString();
                            paser.writeData(section, key, pmToPortRef[OUT_PORT][chan].ToString());
                        }
                    }
                    break;
                case REFPORT.RL_IN:
                    {
                        section = "SYS_RL_IN";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "RL_IN" + (chan + 1).ToString();
                            paser.writeData(section, key, systemRL[IN_PORT][chan].ToString());
                        }
                    }
                    break;
                case REFPORT.RL_A:
                    {
                        section = "SYS_RL_A";
                        key = "";
                        //for (int i = 0; i < 32; i++)
                        {
                            key = "RL_A" + (chan + 1).ToString();
                            paser.writeData(section, key, systemRL[A_PORT][chan].ToString());
                        }
                    }
                    break;
            }

            return true;
        }

        private bool ReadRefResult()
        {
            IniParser paser = new IniParser(refPath);

            string section = "REFTIME";
            string key = "TIME";
            //paser.writeData(section, key, DateTime.Now.ToString());
            section = "REF_SRC";
            key = "SRC";
            string readRes = paser.readStringData(section, key, "9999");
            srcRefPower = Convert.ToDouble(readRes);
            if (readRes != "9999")
            {
                string msg = key + "归零值：" + readRes;
                RealtimeMsg(msg);
            }

            section = "SRC_TO_IN";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "SRC_IN" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                srcToPortRef[IN_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            section = "SRC_TO_A";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "SRC_A" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                srcToPortRef[A_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            section = "PM_TO_A";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "PM_A" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                pmToPortRef[A_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            section = "PM_TO_OUT";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "PM_OUT" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                pmToPortRef[OUT_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            section = "SYS_RL_IN";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "RL_IN" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                systemRL[IN_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            section = "SYS_RL_A";
            key = "";
            for (int i = 0; i < 32; i++)
            {
                key = "RL_A" + (i + 1).ToString();
                readRes = paser.readStringData(section, key, "9999");
                systemRL[A_PORT][i] = Convert.ToDouble(readRes);
                if (readRes != "9999")
                {
                    string msg = key + "归零值：" + readRes;
                    RealtimeMsg(msg);
                }
            }

            return true;
        }

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            //SaveRefResult();
            if (GetOpenTemplateComplete() && isTestedUnSave)
            {
                if (MessageBox.Show("有未保存测试项，是否要打开新的模板！", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                {
                    //uiVariable.SN = allProductControl[0].ProductSN;
                    return;
                }
            }
            if (mainInfo == null)
            {
                ErrorBox("无工位信息，请检查配置！");
                RealtimeMsg("无工位信息，请检查配置！");
                return;
            }

            string errMsg = "";
            if (productControl.OpenTemplate(amtsUrl, templateType, uiVariable.SN, testProcess, MESTestType.Normal, mainInfo.UserID, mainInfo.Goldsample, true, false, ref errMsg))
            {
                // 更新测试信息
                if (EventAggregator != null)
                {
                    List<MESControl> shows = new List<MESControl>();
                    shows.Add(productControl);
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
                }
                MESProductInfo productInfo = productControl.GetProductInfo();
                uiVariable.PN = productInfo.ProductPN;
                uiVariable.Spec = productInfo.Spec;
                GetReference();
                UpdateResIcon();
                uiVariable.IsEnable = true;
            }
            else
            {
                RealtimeMsg("打开模板出错："+errMsg);
            }
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

        
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
            InitCurve("X", "Power", 1, 50, "serPower", System.Drawing.Color.Black, CurveType.Line, "PMSHOW");
            bkSingle = new BackgroundWorker();
            bkSingle.DoWork += SingleTest_DoWork;
            bkSingle.RunWorkerCompleted += SingleTest_Completed;
            bkSingle.WorkerReportsProgress = true;
            bkSingle.WorkerSupportsCancellation = true;
            bkSingle.ProgressChanged += CurveShow_Progress;

            bkOnekey = new BackgroundWorker();
            bkOnekey.DoWork += SingleTest_DoWork;
            bkOnekey.RunWorkerCompleted += Onekey_Completed;
            bkOnekey.WorkerReportsProgress = true;
            bkOnekey.WorkerSupportsCancellation = true;
            bkOnekey.ProgressChanged += CurveShow_Progress;

            bkReference = new BackgroundWorker();
            bkReference.DoWork += Reference_DoWork;
            bkReference.RunWorkerCompleted += Reference_Completed;
            bkReference.WorkerReportsProgress = true;
            bkReference.WorkerSupportsCancellation = true;
            bkReference.ProgressChanged += CurveShow_Progress;
            Win32API.CoInitialize((System.IntPtr)null);
            //InitCurve("X", "Power", 0, 50, "serRL", System.Drawing.Color.Blue, CurveType.Line, "PMSHOW");
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

            /*hotKeyDic.Add("Ctrl-B", Win32API.GlobalAddAtom("Ctrl-B"));
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
            Win32API.RegisterHotKey(wpfHwnd, hotKeyDic["Ctrl-T"], Win32API.KeyModifiers.Ctrl, (int)System.Windows.Forms.Keys.T);*/
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
  /*                      if (sid == hotKeyDic["Ctrl-B"])
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
                       /* else if (sid == hotKeyDic["Ctrl-X"])
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
                        }*/


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

        public delegate void UpdateItemDelegate(MESTestInfo info, int prodoctIndex, int paramIndex, IndexMap nextSelect);

        public void UpdateItemFun(MESTestInfo info, int prodoctIndex, int paramIndex, IndexMap nextSelect)
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
        /// 更新测试项List显示
        /// </summary>
        /// <param name="info">需要更新的测试项</param>
        /// <param name="prodoctIndex">第几个产品</param>
        /// <param name="paramIndex">测试项对应index</param>
        /// <param name="nextSelect">自动跳转到下一行信息</param>
        private void UpdateItem(MESTestInfo info, int prodoctIndex, int paramIndex, IndexMap nextSelect = null)
        {
            object[] param = new object[4];
            param[0] = info;
            param[1] = prodoctIndex;
            param[2] = paramIndex;
            param[3] = nextSelect;
            this.Dispatcher.Invoke(new UpdateItemDelegate(UpdateItemFun), param);
        }

        private bool isAllPortRef = false;
        /// <summary>
        /// 计算每个端口实际归零值
        /// </summary>
        private void CalReference()
        {
            isAllPortRef = true;
            List<MESTestInfo> testInfo = productControl.GetAllTestInfo();
            for (int i=0;i< testInfo.Count;i++)
            {
                string port = testInfo[i].PortNameForUser;
                if(port.Contains("IN")&&port.Contains("A"))
                {
                    string[] idxSplits = port.Split('A');
                    if(idxSplits.Length==2)
                    {
                        int nIndex = Convert.ToInt32(idxSplits[1]);
                        nIndex -= 1;
                        if (testInfo[i].TestParam==MESParam.MaxIL)
                        {
                            if (Math.Abs(srcToPortRef[IN_PORT][nIndex] - 9999) > 0.0001
                                && Math.Abs(pmToPortRef[A_PORT][nIndex] - 9999) > 0.0001)
                            {
                                double dILRef = srcToPortRef[IN_PORT][nIndex] + pmToPortRef[A_PORT][nIndex] + srcRefPower;

                                MESTestInfo info = productControl.UpdateILRefData(i, dILRef);
                                UpdateItem(info, 0, i);
                            }
                            else
                            {
                                isAllPortRef = false;
                            }
                        }
                        else if(testInfo[i].TestParam == MESParam.ReturnLoss)
                        {
                            if (Math.Abs(srcToPortRef[IN_PORT][nIndex] - 9999) > 0.0001
                                && Math.Abs(systemRL[IN_PORT][nIndex] - 9999) > 0.0001)
                            {
                                double dILRef = srcToPortRef[IN_PORT][nIndex] + srcRefPower;
                                productControl.UpdateRLRefData(i, systemRL[IN_PORT][nIndex] + srcRefPower);
                                MESTestInfo info = productControl.UpdateILRefData(i, dILRef);
                                UpdateItem(info, 0, i);
                            }
                            else
                            {
                                isAllPortRef = false;
                            }
                        }
                    }
                }
                else if (port.Contains("A") && port.Contains("OUT"))
                {
                    string[] idxSplits = port.Split('T');
                    if (idxSplits.Length == 2)
                    {
                        int nIndex = Convert.ToInt32(idxSplits[1]);
                        nIndex -= 1;
                        if (testInfo[i].TestParam == MESParam.MaxIL)
                        {
                            if (Math.Abs(srcToPortRef[A_PORT][nIndex] - 9999) > 0.0001
                                && Math.Abs(pmToPortRef[OUT_PORT][nIndex] - 9999) > 0.0001)
                            {
                                double dILRef = srcToPortRef[A_PORT][nIndex] + pmToPortRef[OUT_PORT][nIndex] + srcRefPower;
                                MESTestInfo info = productControl.UpdateILRefData(i, dILRef);
                                UpdateItem(info, 0, i);
                            }
                            else
                            {
                                isAllPortRef = false;
                            }
                        }
                        else if (testInfo[i].TestParam == MESParam.ReturnLoss)
                        {
                            if (Math.Abs(srcToPortRef[A_PORT][nIndex] - 9999) > 0.0001
                                && Math.Abs(systemRL[A_PORT][nIndex] - 9999) > 0.0001)
                            {
                                double dILRef = srcToPortRef[A_PORT][nIndex] + srcRefPower;
                                productControl.UpdateRLRefData(i, systemRL[A_PORT][nIndex] + srcRefPower);
                                MESTestInfo info = productControl.UpdateILRefData(i, dILRef);
                                UpdateItem(info, 0, i);
                            }
                            else
                            {
                                isAllPortRef = false;
                            }
                        }
                    }
                }
                //productControl.UpdateILRefData
            }
            
        }

        private void GetReference()
        {
            if (!File.Exists(refPath))
            {
                string prompt = "归零文件不存在，请重新归零！";
                RealtimeMsg(prompt);
                MessageBoxResult res = MessageBox.Show(prompt, "归零", MessageBoxButton.OK);
                return;   
            }
            IniParser paser = new IniParser(refPath);
            string section = "REFTIME";
            string key = "TIME";
            string refTime=paser.readStringData(section, key);
            if (refTime.Length == 0)
                return;
            //如果超过24小时
            DateTime time = Convert.ToDateTime(refTime);
            TimeSpan span = DateTime.Now - time;
            if(span.Hours>24)
            {
                string prompt = "归零时间超过24小时，请重新归零！";
                RealtimeMsg(prompt);
                MessageBoxResult res = MessageBox.Show(prompt, "归零", MessageBoxButton.OK);
                return;
            }
            ReadRefResult();
            //计算实际归零值，并显示
            CalReference();
        }

        const double referenceLimit = -25;
        /// <summary>
        /// 归零归src->IN src->A  PM->A PM->OUT RL_A,RL_OUT，先归PM进光的，再归其他
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScanRef_Click(object sender, RoutedEventArgs e)
        {
            bool isAllRef = false;

            string prompt = "";
            MessageBoxResult boxRes;
            prompt = string.Format("请将光源线接到IL对应的功率计！");
            RealtimeMsg(prompt);
            boxRes = MessageBox.Show(prompt, "归零", MessageBoxButton.OKCancel);

            if (boxRes == MessageBoxResult.Cancel)
            {
                RealtimeMsg("取消");
                return;
            }
            else if (boxRes == MessageBoxResult.OK)
            {
                if (!GetPower(OPMIL_GUID, ref srcRefPower))
                {
                    RealtimeMsg("读取光源功率出错！");
                    return;
                }
                else
                {
                    prompt = string.Format("光源功率为:{0}", srcRefPower);
                    RealtimeMsg(prompt);
                    SaveRefResult(REFPORT.REF_SRC, 0);
                }
            }

            if (File.Exists(refPath) && isAllPortRef)
            {
                prompt = "是否调用归零数据";
                RealtimeMsg(prompt);
                boxRes = MessageBox.Show("是否调用归零数据", "归零", MessageBoxButton.YesNoCancel);
                if (MessageBoxResult.Cancel == boxRes)
                {
                    RealtimeMsg("不调用归零数据！");
                    isAllRef = true;
                }
                else
                {
                    IniParser paser = new IniParser(refPath);

                    string section = "";
                    string key = "";
                    /*string refTime=paser.readStringData(section, key);
                    //如果超过24小时*/
                    if (!SWSrcToIn(1))
                    {
                        return;
                    }
                    double dSrcToIn1Ref = 0;
                    double dSrcToA1Ref = 0;

                    MessageBoxResult res;
                    while (true)
                    {
                        prompt = "请将光源接到SRC端，IN1接到功率计，确认有光后开始归零！";
                        res = MessageBox.Show(prompt, "归零", MessageBoxButton.OKCancel);
                        RealtimeMsg(prompt);
                        if (res == MessageBoxResult.Cancel)
                        {
                            RealtimeMsg("取消归零！");
                            return;
                        }

                        if (!GetSrcToInRef(1, ref dSrcToIn1Ref))
                        {
                            return;
                        }

                        if (!GetSrcToARef(1, ref dSrcToA1Ref))
                        {
                            return;
                        }
                        if (dSrcToIn1Ref < referenceLimit || dSrcToA1Ref < referenceLimit)
                        {
                            prompt = "归零光太弱，请确认光路！";
                            res = MessageBox.Show(prompt, "归零", MessageBoxButton.OKCancel);
                            RealtimeMsg(prompt);
                            if (res == MessageBoxResult.Cancel)
                            {
                                RealtimeMsg("取消归零！");
                                return;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    section = "SRC_TO_IN";
                    key = "SCR_IN1";
                    string strScrToIn1 = paser.readStringData(section, key, "0");
                    double dRecSrcToIn1Ref = Convert.ToDouble(strScrToIn1);

                    section = "SRC_TO_A";
                    key = "A_IN1";
                    string strScrToA1 = paser.readStringData(section, key, "0");
                    double dRecSrcToA1Ref = Convert.ToDouble(strScrToA1);

                    if (Math.Abs(dSrcToIn1Ref - dRecSrcToIn1Ref) > 0.05 ||
                        Math.Abs(dSrcToA1Ref - dRecSrcToA1Ref) > 0.05)
                    {
                        RealtimeMsg("记录归零值与当前IN1、A1归零值差异超过0.05，需要重新归零！");
                        isAllRef = true;
                    }
                    else
                    {

                        {
                            //读取归零数据
                            RealtimeMsg("读取归零数据！");
                            ReadRefResult();
                            //重新保存，更新归零时间
                            //SaveRefResult();
                            CalReference();
                        }
                    }
                }
            }
            else
            {
                isAllRef = true;
            }
            
            if(isAllRef)
            {
                prompt = string.Format("请将光源接到SRC端，确认A1-A32全部绕死后开始RL归零！");
                RealtimeMsg(prompt);
                boxRes = MessageBox.Show(prompt, "归零", MessageBoxButton.YesNoCancel);

                if (boxRes == MessageBoxResult.Cancel)
                {
                    RealtimeMsg("取消");
                    return;
                }
                else if (boxRes == MessageBoxResult.No)
                {
                    RunWorkerCompletedEventArgs resArgs = new RunWorkerCompletedEventArgs(A_PORT,null,false);
                    Reference_Completed(this, resArgs);
                }
                else
                {
                    bkReference.RunWorkerAsync(A_PORT);
                }
            }

        }

        private void Reference_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = -1;
            Win32API.CoInitialize((System.IntPtr)null);
            int nPort = Convert.ToInt32(e.Argument);
            if(nPort==A_PORT)
            {
                //提示归回损
                for (int i = 0; i < 32; i++)
                {
                    double dPower = 0;
                    if (!GetSystemRLARef(i + 1, ref dPower))
                    {
                        return;
                    }
                    systemRL[A_PORT][i] = dPower;
                    SaveRefResult(REFPORT.RL_A,i);
                }
            }
            else if(nPort==IN_PORT)
            {
                for (int i = 0; i < 32; i++)
                {
                    double dPower = 0;
                    if (!GetSystemRLInRef(i + 1, ref dPower))
                    {
                        return;
                    }
                    systemRL[IN_PORT][i] = dPower;
                    SaveRefResult(REFPORT.RL_IN,i);
                }
            }
            e.Result = e.Argument;
        }

        private void Reference_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            int nPort = Convert.ToInt32(e.Result);
            if(nPort==-1)
            {
                RealtimeMsg("归零出错！");
                return;
            }
            if (nPort == A_PORT)
            {
                string prompt = string.Format("请将光源接到SRC端，确认IN1-IN32全部绕死后开始RL归零！");
                RealtimeMsg(prompt);
                MessageBoxResult boxRes = MessageBox.Show(prompt, "归零", MessageBoxButton.YesNoCancel);
                if (boxRes == MessageBoxResult.Cancel)
                {
                    RealtimeMsg("取消");
                    return;
                }
                else if (boxRes == MessageBoxResult.No)
                {
                    nPort = IN_PORT;
                }
                else
                {
                    bkReference.RunWorkerAsync(IN_PORT);
                }
            }

            if(nPort==IN_PORT)
            {
                for(int i = 0; i < 32; i++)
                {
                    if (!SWPMToA(i + 1))
                    {
                        return;
                    }
                    double dPower = 0;
                    while (true)
                    {
                        string msg = string.Format("请将光源接到PM端，A{0}接到功率计，确认有光后开始归零！", i + 1);
                        RealtimeMsg(msg);
                        MessageBoxResult res = MessageBox.Show(msg, "归零", MessageBoxButton.YesNoCancel);
                        if (res == MessageBoxResult.No)
                        {
                            RealtimeMsg("取消");
                            break;
                        }
                        if (res == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        if (!GetPMToARef(i + 1, ref dPower))
                        {
                            return;
                        }
                        if (dPower < referenceLimit)
                        {
                            string promptMsg = "归零光太弱，请确认光路！";
                            res = MessageBox.Show(promptMsg, "归零", MessageBoxButton.YesNoCancel);
                            RealtimeMsg(promptMsg);
                            if (res == MessageBoxResult.No)
                            {
                                RealtimeMsg("取消");
                                break;
                            }
                            if (res == MessageBoxResult.Cancel)
                            {
                                return;
                            }
                        }
                        else
                        {
                            pmToPortRef[A_PORT][i] = dPower;
                            SaveRefResult(REFPORT.PM_A, i);
                            break;
                        }

                    }


                }

                for (int i = 0; i < 32; i++)
                {
                    if (!SWPMToOut(i + 1))
                    {
                        return;
                    }
                    double dPower = 0;
                    while (true)
                    {
                        string msg = string.Format("请将光源接到PM端，OUT{0}接到功率计，确认有光后开始归零！", i + 1);
                        RealtimeMsg(msg);

                        MessageBoxResult res = MessageBox.Show(msg, "归零", MessageBoxButton.YesNoCancel);
                        if (res == MessageBoxResult.No)
                        {
                            RealtimeMsg("取消");
                            break;
                        }
                        if (res == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        if (!GetPMToOutRef(i + 1, ref dPower))
                        {
                            return;
                        }
                        if (dPower < referenceLimit)
                        {
                            string promptMsg = "归零光太弱，请确认光路！";
                            res = MessageBox.Show(promptMsg, "归零", MessageBoxButton.YesNoCancel);
                            RealtimeMsg(promptMsg);
                            if (res == MessageBoxResult.No)
                            {
                                RealtimeMsg("取消");
                                break;
                            }
                            if (res == MessageBoxResult.Cancel)
                            {
                                return;
                            }
                        }
                        else
                        {
                            pmToPortRef[OUT_PORT][i] = dPower;
                            SaveRefResult(REFPORT.PM_OUT, i);
                            break;
                        }
                    }

                }

                for (int i = 0; i < 32; i++)
                {
                    if (!SWSrcToIn(i + 1))
                    {
                        return;
                    }
                    double dPower = 0;
                    while (true)
                    {
                        string msg = string.Format("请将光源接到SRC端，IN{0}接到功率计，确认有光后开始归零！", i + 1);
                        RealtimeMsg(msg);
                        MessageBoxResult res = MessageBox.Show(msg, "归零", MessageBoxButton.YesNoCancel);
                        if (res == MessageBoxResult.No)
                        {
                            RealtimeMsg("取消");
                            break;
                        }
                        if (res == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        if (!GetSrcToInRef(i + 1, ref dPower))
                        {
                            return;
                        }
                        if (dPower < referenceLimit)
                        {
                            string promptMsg = "归零光太弱，请确认光路！";
                            res = MessageBox.Show(promptMsg, "归零", MessageBoxButton.YesNoCancel);
                            RealtimeMsg(promptMsg);
                            if (res == MessageBoxResult.No)
                            {
                                RealtimeMsg("取消");
                                break;
                            }
                            if (res == MessageBoxResult.Cancel)
                            {
                                return;
                            }
                        }
                        else
                        {
                            srcToPortRef[IN_PORT][i] = dPower;
                            SaveRefResult(REFPORT.SRC_IN, i);
                            break;
                        }
                    }

                }

                for (int i = 0; i < 32; i++)
                {
                    if (!SWSrcToA(i + 1))
                    {
                        return;
                    }
                    double dPower = 0;
                    while (true)
                    {
                        string msg = string.Format("请将光源接到SRC端，A{0}接到功率计，确认有光后开始归零！", i + 1);
                        RealtimeMsg(msg);
                        MessageBoxResult res = MessageBox.Show(msg, "归零", MessageBoxButton.YesNoCancel);
                        if (res == MessageBoxResult.No)
                        {
                            RealtimeMsg("取消");
                            break;
                        }
                        if (res == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        if (!GetSrcToARef(i + 1, ref dPower))
                        {
                            return;
                        }
                        if (dPower < referenceLimit)
                        {
                            string promptMsg = "归零光太弱，请确认光路！";
                            res = MessageBox.Show(promptMsg, "归零", MessageBoxButton.YesNoCancel);
                            RealtimeMsg(promptMsg);
                            if (res == MessageBoxResult.No)
                            {
                                RealtimeMsg("取消");
                                break;
                            }
                            if (res == MessageBoxResult.Cancel)
                            {
                                return;
                            }
                        }
                        else
                        {
                            srcToPortRef[A_PORT][i] = dPower;
                            SaveRefResult(REFPORT.SRC_A, i);
                            break;
                        }
                    }
                }
                SaveRefResult(REFPORT.REF_TIME, 0);
                //计算实际的归零值，并更新到界面
                CalReference();
            }
        }

        private bool SWSrcToIn(int chan)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 4);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWINOUT_GUID, 1, chan);
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            //Thread.Sleep(1200);
            return true;
        }

        private bool SWSrcToA(int chan)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 2);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWA_GUID, 1, chan);
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            //Thread.Sleep(1200);
            return true;
        }

        private bool SWPMToOut(int chan)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 2);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWINOUT_GUID, 1, 32 + chan);
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            //Thread.Sleep(1200);
            return true;
        }

        private bool SWPMToA(int chan)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 4);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWA_GUID, 1, chan);
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            //Thread.Sleep(1200);
            return true;
        }

        private bool GetSrcToInRef(int chan,ref double dRefPower)
        {
            string errMsg = "";
            
            if (!GetPower(OPMIL_GUID, ref dRefPower))
            {
                RealtimeMsg("读取功率出错：" + errMsg);
                return false;
            }
            string prompt = string.Format("src to IN{0} 归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            dRefPower -= srcRefPower;
            RealtimeMsg(prompt);
            return true;
        }

        private bool GetSrcToARef(int chan, ref double dRefPower)
        {
            
            if (!GetPower(OPMIL_GUID, ref dRefPower))
            {
                RealtimeMsg("读取功率出错");
                return false;
            }
            string prompt = string.Format("src to A{0} 归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            RealtimeMsg(prompt);
            dRefPower -= srcRefPower;
            return true;
        }

        private bool GetPMToOutRef(int chan, ref double dRefPower)
        {            
            if (!GetPower(OPMIL_GUID, ref dRefPower))
            {
                RealtimeMsg("读取功率出错");
                return false;
            }
            string prompt = string.Format("PM to OUT{0} 归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            RealtimeMsg(prompt);
            dRefPower -= srcRefPower;
            return true;
        }

        private bool GetPMToARef(int chan, ref double dRefPower)
        {
                        
            if (!GetPower(OPMIL_GUID, ref dRefPower))
            {
                RealtimeMsg("读取功率出错");
                return false;
            }
            string prompt = string.Format("PM to A{0} 归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            RealtimeMsg(prompt);
            dRefPower -= srcRefPower;
            return true;
        }

        private bool GetSystemRLARef(int chan, ref double dRefPower)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 2);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWA_GUID, 1, chan);
            
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            Thread.Sleep(1000);
            double[] dPower = null;
            dPower = new double[10];
            
            for(int i=0;i<10;i++)
            {
                
                if (!GetPower(OPMRL_GUID, ref dPower[i]))
                {
                    RealtimeMsg("读取功率出错");
                    return false;
                }
                bkReference.ReportProgress(i, dPower[i]);
                if(dRefPower < dPower[i]||i==0)
                {
                    dRefPower = dPower[i];
                }
                Thread.Sleep(5);
            }
            string prompt = string.Format("PORT A{0} RL 归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            RealtimeMsg(prompt);
            dRefPower -= srcRefPower;
            return true;
        }

        private bool GetSystemRLInRef(int chan, ref double dRefPower)
        {
            opticalSWCtrl.SetSwitchPosition(OSW2X2_GUID, 1, 4);
            string errMsg = "";
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换2X2开关出错：" + errMsg);
                return false;
            }
            opticalSWCtrl.SetSwitchPosition(OSWINOUT_GUID, 1, chan);
            if (!IsUDLSuccess(ref errMsg))
            {
                RealtimeMsg("切换1X64开关出错：" + errMsg);
                return false;
            }
            Thread.Sleep(1000);
            double[] dPower = null;
            dPower = new double[10];
            for (int i = 0; i < 10; i++)
            {
                if (!GetPower(OPMRL_GUID, ref dPower[i]))
                {
                    RealtimeMsg("读取功率出错" );
                    return false;
                }
                bkReference.ReportProgress(i, dPower[i]);
                if (dRefPower < dPower[i] || i == 0)
                {
                    dRefPower = dPower[i];
                }
                Thread.Sleep(5);
            }
            string prompt = string.Format("Port IN{0} RL归零值：{1}-{2}", chan, dRefPower, srcRefPower);
            RealtimeMsg(prompt);
            dRefPower -= srcRefPower;
            return true;
        }

        private bool GetPower(int guid,ref double dPower)
        {
            string errMsg = "";
            powermeterCtrl.GetPower(guid, out dPower);
            if (!IsUDLSuccess(ref errMsg))
            {
                powermeterCtrl.GetPower(guid, out dPower);
                if (!IsUDLSuccess(ref errMsg))
                {
                    return false;
                }
                dPower = Math.Round(dPower, 3);
            }
            return true;
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
            object[] param = new object[1];
            param[0] = message;
            this.Dispatcher.Invoke(new RealTimeMsgDelegate(RealtimeMsgShow), param);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ReleaseCom(deviceEngine);
            ReleaseCom(powermeterCtrl);
            ReleaseCom(opticalSWCtrl);
        }

        
        private void btnPMReset_Click(object sender, RoutedEventArgs e)
        {
            powermeterCtrl.ResetDevice(OPMIL_GUID);
            powermeterCtrl.ResetDevice(OPMRL_GUID);
            isRefAgain = false;
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
        /// 更新曲线显示
        /// </summary>
        /// <param name="serName">曲线名称</param>
        /// <param name="areaName">显示曲线的区域名称</param>
        /// <param name="xValues">x轴值</param>
        /// <param name="yValues">y轴值</param>
        private void UpdateCurveShow(string serName, CurveUpdate type, List<double> xValues, List<double> yValues)
        {
            CurveUpdateDetail curveDetail = new CurveUpdateDetail();
            curveDetail.SeriesName = serName;
            curveDetail.UpdateType = type;
            curveDetail.TargetName = "PMSHOW";
            curveDetail.XAxisStep = xValues;
            curveDetail.YAxisValue = yValues;

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(curveDetail);
            }
        }

        private void SingleTest_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = -1;
            if (selectItem == null|| selectItem.ParamIndex.Count==0)
                return;
            List<MESTestInfo> testInfo = productControl.GetAllTestInfo();
            int nIdx= Convert.ToInt32(e.Argument);
            
            int nTestIndex = selectItem.ParamIndex[nIdx];
            //nextSeleted.ParamIndex.Add(selectItem.ParamIndex[i] + 1);
            MESTestInfo info = testInfo[nTestIndex];
            if (isOnekeyTest)
            {
                if (info.Tested)
                {
                    e.Result = nIdx;
                    return;
                }
            }
            string port = testInfo[nTestIndex].PortNameForUser;
#if true
            if (port.Contains("IN") && port.Contains("A"))
            {
                string[] idxSplits = port.Split('A');
                if (idxSplits.Length == 2)
                {
                    int nIndex = Convert.ToInt32(idxSplits[1]);
                    if (!SWSrcToIn(nIndex) || !SWPMToA(nIndex))
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }
            else
            {
                string[] idxSplits = port.Split('T');
                if (idxSplits.Length == 2)
                {
                    int nIndex = Convert.ToInt32(idxSplits[1]);
                    if (!SWSrcToA(nIndex) || !SWPMToOut(nIndex))
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }
#endif
            string errMsg = "";
            if (info.TestParam == MESParam.MaxIL)
            {
                //double dPowr = 0.0;
                double[] dPowr = new double[10];

                for (int j = 0; j < 10; j++)
                {
#if true
                    if (!GetPower(OPMIL_GUID, ref dPowr[j]))
                    {
                        return;
                    }
#else
                    dPowr[j] = -50;
#endif
                    if (isOnekeyTest)
                    {
                        bkOnekey.ReportProgress(j, dPowr[j]);
                    }
                    else
                    {
                        bkSingle.ReportProgress(j, dPowr[j]);
                    }
                }
                double dIL = -algorithm.MaxIL(dPowr, info.ILRef, ref errMsg);
                dIL = Math.Round(dIL, 3);
                bool isPass = false;
                MESTestInfo testRes = productControl.UpdateTestData(nTestIndex, dIL, ref isPass);
                UpdateItem(testRes, 0, nTestIndex);
            }
            else if (info.TestParam == MESParam.ReturnLoss)
            {
                //读取功率值，计算回损
                double[] dPowr = new double[10];

                for (int j = 0; j < 10; j++)
                {
#if true
                    if (!GetPower(OPMRL_GUID, ref dPowr[j]))
                    {
                        return;
                    }
#else
                    dPowr[j] = -50;
#endif
                    if (isOnekeyTest)
                    {
                        bkOnekey.ReportProgress(j, dPowr[j]);
                    }
                    else
                    {
                        bkSingle.ReportProgress(j, dPowr[j]);
                    }
                    dPowr[j] = -dPowr[j];
                }
                errMsg = "";
                double dRL = -algorithm.RL(dPowr,Math.Abs(info.ILRef), Math.Abs(info.RLRef), ref errMsg);
                dRL = Math.Round(dRL, 3);
                if(errMsg.Length>0)
                {
                    string prompt = "";
                    prompt = string.Format("{0}{1}", info.PortNameForUser);
                    RealtimeMsg(prompt);
                }
                bool isPass = false;
                MESTestInfo testRes = productControl.UpdateTestData(nTestIndex, dRL, ref isPass);
                UpdateItem(testRes, 0, nTestIndex);
                
            }
            e.Result = nIdx;
        }

        public void CurveShow_Progress(object sender, ProgressChangedEventArgs e)
        {
            int nIdx = e.ProgressPercentage;
            double dPower = Convert.ToInt32(e.UserState);
            List<double> xArr = new List<double>();
            xArr.Add(nIdx + 1);
            List<double> yArr = new List<double>();
            yArr.Add(dPower);
            if (nIdx == 0)
            {
                UpdateCurveShow("serPower", CurveUpdate.FirstPoint, xArr, yArr);
            }
            else
            {
                UpdateCurveShow("serPower", CurveUpdate.AddPoint, xArr, yArr);
            }
        }

        private void Onekey_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateResIcon();
            int res = Convert.ToInt32(e.Result);
            if (res == -1)
            {
                uiVariable.IsEnable = true;
                uiVariable.IsStopScanVisible = Visibility.Hidden;
                uiVariable.IsOnekeyVisible = Visibility.Visible;
                return;
            }
            else
            {
                if (selectItem.ParamIndex[res] == productControl.GetAllTestInfo().Count - 1)
                {
                    string prompt = "一键测试结束";
                    RealtimeMsg(prompt);
                    MessageBox.Show(prompt);
                    uiVariable.IsEnable = true;
                    uiVariable.IsStopScanVisible = Visibility.Hidden;
                    uiVariable.IsOnekeyVisible = Visibility.Visible;
                    return;
                }
                if(GetIsStopOnekey())
                {
                    return;
                }
                if ((res + 1) < selectItem.ParamIndex.Count)
                {
                    bkOnekey.RunWorkerAsync(res + 1);
                }
                else
                {
                    IndexMap nextSeleted = new IndexMap();
                    nextSeleted.ProductIndex = 0;
                    nextSeleted.ParamIndex.Add(selectItem.ParamIndex[res] + 1);
                    List<MESTestInfo> testInfo = productControl.GetAllTestInfo();
                    UpdateItem(testInfo[0], 0, 0, nextSeleted);
                    bkOnekey.RunWorkerAsync(0);
                }
            }

        }
        

        private void SingleTest_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            uiVariable.IsEnable = true;
            UpdateResIcon();
            int res = Convert.ToInt32(e.Result);
            if (res == -1)
            {
                return;
            }
            else
            {
                if ((res + 1) < selectItem.ParamIndex.Count)
                {
                    uiVariable.IsEnable = false;
                    bkSingle.RunWorkerAsync(res + 1);
                }
                else
                {
                    IndexMap nextSeleted = new IndexMap();
                    nextSeleted.ProductIndex = 0;
                    nextSeleted.ParamIndex.Add(selectItem.ParamIndex[res] + 1);
                    List<MESTestInfo> testInfo = productControl.GetAllTestInfo();
                    UpdateItem(testInfo[0], 0, 0, nextSeleted);
                }
            }
            
        }
        
        private void btnSingleTest_Click(object sender, RoutedEventArgs e)
        {
            isOnekeyTest = false;
            string errMsg = "";
            if(!productControl.GetAllRef(ref errMsg))
            {
                RealtimeMsg(errMsg);
                MessageBox.Show(errMsg);
#if true
                return;
#endif
            }
            uiVariable.IsEnable = false;
            bkSingle.RunWorkerAsync(0);
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
        }

        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            int nUntestIndex = -1;
            string errMsg = "";
            if(!productControl.GetAllTested(out nUntestIndex,ref errMsg))
            {
                MessageBoxResult res =  MessageBox.Show(errMsg+"是否要上传数据！", "数据保存", MessageBoxButton.OKCancel);
                if(MessageBoxResult.Cancel==res)
                {
                    return;
                }
            }

            if(!productControl.GetAllTestedPassed(ref errMsg))
            {
                MessageBoxResult res = MessageBox.Show(errMsg + "是否要上传数据！", "数据保存", MessageBoxButton.OKCancel);
                if (MessageBoxResult.Cancel == res)
                {
                    return;
                }
            }

            if(0!=productControl.SaveDataToAMTS(productControl.ProductSN, amtsSaveUrl,ref errMsg))
            {
                RealtimeMsg(errMsg);
                return;
            }

            ClearListData();
            uiVariable.SN = "";
        }

        private void ClearListData()
        {
            MESControl testItemShow = new MESControl();
            // 更新测试信息
            if (EventAggregator != null)
            {
                List<MESControl> shows = new List<MESControl>();
                shows.Add(testItemShow);
                EventAggregator.GetEvent<EventTemplateUpdate>().Publish(shows);
            }
        }

        
        private void btnOnekey_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            if (!productControl.GetAllRef(ref errMsg))
            {
                RealtimeMsg(errMsg);
                MessageBox.Show(errMsg);
                //return;
            }
            isOnekeyTest = true;
            SetIsStopOnekey(false);
            List<MESTestInfo> testInfos = productControl.GetAllTestInfo();
            for(int i=0;i<testInfos.Count;i++)
            {
                if (testInfos[i].Tested)
                    continue;
                IndexMap nextSeleted = new IndexMap();
                nextSeleted.ProductIndex = 0;
                nextSeleted.ParamIndex.Add(i);                
                UpdateItem(testInfos[0], 0, 0, nextSeleted);
                
                uiVariable.IsEnable = false;
                uiVariable.IsStopScanVisible = Visibility.Visible;
                uiVariable.IsOnekeyVisible = Visibility.Hidden;
                bkOnekey.RunWorkerAsync(0);
                return;
            }

        }

        
        private void btnStopOnekey_Click(object sender, RoutedEventArgs e)
        {
            SetIsStopOnekey(true);
            uiVariable.IsEnable = true;
            uiVariable.IsStopScanVisible = Visibility.Hidden;
            uiVariable.IsOnekeyVisible = Visibility.Visible;
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

        private Visibility isOnekeyVisible;
        public Visibility IsOnekeyVisible
        {
            get
            {
                return isOnekeyVisible;
            }
            set
            {
                isOnekeyVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsOnekeyVisible"));
            }
        }
    }
}
