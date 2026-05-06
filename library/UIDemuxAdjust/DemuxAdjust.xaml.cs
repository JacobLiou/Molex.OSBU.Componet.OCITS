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
using System.Threading;
using System.IO;

namespace UIDemuxAdjust
{
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIDemuxAdjust")]
    public partial class DemuxAdjust : UserControl
    {
        private const string refDataFile = "\\reference\\RefData.csv";
        private const string pwmResetFile = "\\temple\\PWMReset.csv";
        private const string templateConfig = "\\set\\template.ini";
        private const string passImage = "image/Pass.ico";
        private const string failImage = "image/Fail.ico";

        /// <summary>
        /// 曲线显示对象
        /// </summary>
        private DemuxCurve curveShow;

        /// <summary>
        /// 无纸化加载模板
        /// </summary>
        private string amtsUrl = "http://172.18.1.101/amts/";

        /// <summary>
        /// 保存数据服务器地址
        /// </summary>
        private string amtsSaveUrl = "http://172.18.1.101/amts/Atd_UploadMessage.asmx";

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
        /// 功率计复位线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 由主程序传递的工位信息等
        /// </summary>
        private MainInitInfo mainInfo = null;

        /// <summary>
        /// 工位信息
        /// </summary>
        //MESTestProcess testProcess;
        //MESTemplateType templateType;
        //string userID = "";

        /// <summary>
        /// 模板名称
        /// </summary>
        private string templateName = "";

        /// <summary>
        /// 功率计值
        /// </summary>
        private List<double> realtimePowers = new List<double>();

        /// <summary>
        /// 更新曲线委托
        /// </summary>
        /// <param name="xArr"></param>
        /// <param name="yArr"></param>
        private delegate void UpdateCurveDelegate(List<double> xArr, List<double> yArr);
        UpdateCurveDelegate myUpdateCurveDelegate;

        /// <summary>
        /// 更新按钮状态委托
        /// </summary>
        /// <param name="isEnabled"></param>
        private delegate void UpdateBtnDelegate(bool isEnabled);
        UpdateBtnDelegate myUpdateBtnDelegate;

        bool isRetest = false;

        public DemuxAdjust()
        {
            InitializeComponent();

            txtSN.DataContext = uiVariable;
            btnOpenTemplate.DataContext = uiVariable;
            btnSaveToAMTS.DataContext = uiVariable;
            btnILRef.DataContext = uiVariable;
            btnILTest.DataContext = uiVariable;
            chkRetest.DataContext = uiVariable;
            chkLoadData.DataContext = uiVariable;
            btnPMReset.DataContext = uiVariable;
            lblPMResetTime.DataContext = uiVariable;
            lblMessage.DataContext = uiVariable;

            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            
            powermeterRealtimeDelegate = new PowermeterRealtimeDelegate(UpdatemeterDelegate);
            powermeterRealtimeThread = new Thread(UpdatePowermeter);

            refTimeCheckBK = new BackgroundWorker();
            refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            refTimeCheckBK.WorkerSupportsCancellation = true;
            refTimeCheckBK.WorkerReportsProgress = true;
        }

        /// <summary>
        /// 功率值实时显示线程
        /// </summary>
        private void UpdatePowermeter()
        {
            while (true)
            {
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
                else
                {
                    int channel = 0;
                    DeviceControl.GetPowermeterByIndex(1, ref channel, ref powermeter, ref errMsg);
                    if (powermeter != null)
                        powermeter.ReadPowerAvg(ref errMsg, out realtimePowers);
                }
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
            if (selectItem != null&& templateControl.GetAllTestInfo().Count >0)
            {
                MESTestInfo ilTest = templateControl.GetAllTestInfo()[selectItem.RowIndex];
                if (ilTest.ILRef != CommonFunction.GetDefaultValue())
                    ch1.Power = (realtimePowers[0]-ilTest.ILRef).ToString("#0.000") + "dB";
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
                    if ((tsRemainder.Hours == 0 && tsRemainder.Minutes < 30)||(tsRemainder.Hours <= 0 && tsRemainder.Minutes<=0))
                    {
                        lblPMResetTime.Foreground = Brushes.Red;
                    }
                    else
                        lblPMResetTime.Foreground = Brushes.Black;
                    lblPMResetTime.Content = string.Format("{0}小时{1}分{2}秒", tsRemainder.Hours, tsRemainder.Minutes, tsRemainder.Seconds);
                }
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            powermeterRealtimeThread.Abort();
            refTimeCheckBK.CancelAsync();
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
            /*testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), info.TestProcess, true);
            templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), info.TemplateType, true);
            userID = info.UserID;*/
            curveShow = new DemuxCurve(EventAggregator);
            curveShow.InitCurve();

            powermeterRealtimeThread.Start();
            refTimeCheckBK.RunWorkerAsync();

            string errMsg = "";
            if (powermeter != null)
            {
                powermeter.SetPMWavelength(1271.0, ref errMsg);
                powermeter.SetPMUnits(ref errMsg, "dB");
            }
        }

        /// <summary>
        /// 打开模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                //if (templateControl.GetHasTested())
                //{
                //    MessageBoxResult msgRes = System.Windows.MessageBox.Show("有测试数据未保存，是否打开新条码？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                //    if (msgRes == MessageBoxResult.No)
                //    {
                //        MESProductInfo proInfo = templateControl.GetProductInfo();
                //        txtSN.Text = proInfo.SN;
                //        return;
                //    }
                //}
                templateControl.ClearAllData();

                //功率计复位
                ResetPWM();

                selectItem = null;
                if (txtSN.Text == "")
                {
                    WarningBox("请输入产品号！！");
                    return;
                }

                MESTestType testType = MESTestType.Normal;
                isRetest = false;
                if (true == chkRetest.IsChecked)
                {
                    testType = MESTestType.Retest;
                    isRetest = true;
                }
                    bool bShowData = (chkLoadData.IsChecked == true);
                string tmpltContent = templateControl.OpenTemplate(uiVariable.SN, mainInfo.TestProcess, mainInfo.UserID, "", false, Environment.MachineName, out templateName, out errMsg);
                if (tmpltContent.Length > 0)
                {
                    if (errMsg != "")
                    {
                        CommonFunction.WriteLog(errMsg);
                        ErrorBox(errMsg);
                        return;
                    }

                    /*MESProductInfo curProInfo = templateControl.GetProductInfo();
                    int nFinishMode = Convert.ToInt32(curProInfo.FinishModel);

                    if (Convert.ToBoolean(nFinishMode & 1))
                    {
                        MessageBoxResult msgRes = System.Windows.MessageBox.Show("该产品已经测试过，要重新测试吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (msgRes == MessageBoxResult.No)
                        {
                            txtSN.Text = "";
                            txtSN.Focus();
                            return;
                        }
                    }*/

                    errMsg = "";
                    //读取归零数据
                    templateControl.ReadRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);
                    if (errMsg != "")
                    {
                        CommonFunction.WriteLog(errMsg);
                        errMsg = "";
                    }

                    uiVariable.Message = "模板打开成功！";
                    imgResult.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "/image/Fail.ico", UriKind.Absolute));

                    List<FusionControl> controls = new List<FusionControl>();
                    controls.Add(templateControl);
                    //更新测试信息
                    if (EventAggregator != null)
                    {
                        EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                    }

                    int i = 0;
                    foreach (MESTestInfo info in templateControl.GetAllTestInfo())
                    {
                        //IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                        //nextSelect.ProductIndex = 0;
                        //nextSelect.ParamIndex = new List<int>();
                        //if (i + 1 == templateControl.GetAllTestInfo().Count)
                        //    nextSelect.ParamIndex.Add(0);
                        //else
                        //    nextSelect.ParamIndex.Add(i + 1);
                        //UpdateItem(info, i, nextSelect);
                        UpdateItem(info, i);
                        i++;
                    }
                }
                else
                {
                    ErrorBox(errMsg);
                    CommonFunction.WriteLog(errMsg);
                    txtSN.Text = "";
                    txtSN.Focus();
                    return;
                }
                chkRetest.IsEnabled = false;
                chkLoadData.IsEnabled = false;
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
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
                            lblPMResetTime.Foreground = Brushes.Red;
                        }
                        else
                            lblPMResetTime.Foreground = Brushes.Black;
                        lblPMResetTime.Content = string.Format("{0}小时{1}分{2}秒", tsRemainder.Hours, tsRemainder.Minutes, tsRemainder.Seconds);
                        if (ts.Days > 0 || (ts.Hours >= 6))
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
                    lblMessage.Content = errMsg;
                    errMsg = "";
                }

                FileInfo file = new FileInfo(System.Environment.CurrentDirectory + refDataFile);
                if (file.Exists)
                    file.Delete();
                if (isCreate)
                {
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
        /// 加载注册函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
            KeyDownRegister();
        }

        /// <summary>
        /// 保存数据按钮响应函数（照光前）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                string savePath = Environment.CurrentDirectory + "\\data\\" + templateControl.ProductSN + ".xml";
                if (!templateControl.UploadTestData(savePath, out errMsg))
                {
                    ErrorBox(errMsg);
                    uiVariable.Message = "保存失败，请重新保存！";
                    return;
                }
                uiVariable.Message = "保存成功!";
                
                List<FusionControl> controls = new List<FusionControl>();
                controls.Add(templateControl);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                }
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// 保存数据按钮响应函数（照光后）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToAMTS2_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                string savePath = Environment.CurrentDirectory + "\\data\\" + templateControl.ProductSN + "_UV.xml";
                if (!templateControl.UploadTestData(savePath, out errMsg))
                {
                    ErrorBox(errMsg);
                    uiVariable.Message = "保存失败，请重新保存！";
                    return;
                }
                uiVariable.Message = "保存成功!";
                //uiVariable.SN = "";
                //templateControl.ClearAllData();
                List<FusionControl> controls = new List<FusionControl>();
                controls.Add(templateControl);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                }
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
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
            selectItem = map.Clone();
            MESTestInfo ilTest = templateControl.GetAllTestInfo ()[selectItem.RowIndex];
            //根据波长切换光开关
            string errMsg = "";
            SetSwitch(ilTest, ref errMsg);
        }

        /// <summary>
        /// 切换光开关
        /// </summary>
        /// <param name="testInfo">当前测试项信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true--正确，false--出错</returns>
        private bool SetSwitch(MESTestInfo testInfo, ref string errMsg)
        {
            try
            {
                IOpticalSwitch opticalSwitch = null;
                if (DeviceControl.GetSwitchByType("DemuxSwitch", ref opticalSwitch, ref errMsg) != 0)
                {
                    ErrorBox("切换光源盒出错：" + errMsg);
                    return false;
                }
                //Devices device = Devices.OMSSwitch;
                //产品序号:波长:端口:参数
                string flag = ":" + testInfo.WLLeft.ToString() + ":" + testInfo.PortNameForUser + ":" + testInfo.TestParam.GetMESTemplateKeywords();
                if (opticalSwitch.SetSwitch(flag, ref errMsg) != 0)
                {
                    ErrorBox("切换光源盒出错：" + errMsg);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return false;
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
        }
        
        /// <summary>
        /// IL归零响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnILRef_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                uiVariable.Message = "开始归零!";
                ButtonState(false);
                powermeterRealtimeThread.Abort();
                
                myUpdateCurveDelegate = new UpdateCurveDelegate(UpdateCurve);
                myUpdateBtnDelegate = new UpdateBtnDelegate(ButtonState);
                Thread ILRefThread = new Thread(new ThreadStart(ILRefFun));
                ILRefThread.Start();
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// IL归零线程
        /// </summary>
        private void ILRefFun()
        {
            List<MESTestInfo> testInfo = templateControl.GetAllTestInfo();
            string errMsg = "";
            int count = templateControl.GetAllTestInfo().Count;
            for (int i = 0; i < count; i = i + 2)
                ILRef(testInfo, 0, i, ref errMsg);

            powermeterRealtimeThread = new Thread(UpdatePowermeter);
            powermeterRealtimeThread.Start();

            errMsg = "";
            templateControl.RecordRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);

            this.Dispatcher.Invoke(myUpdateBtnDelegate, true);
        }

        /// <summary>
        /// IL归零
        /// </summary>
        /// <param name="testInfo">所有测试项信息</param>
        /// <param name="rlTest">当前归零行</param>
        /// <param name="rlIndex">归零行的序列号</param>
        private void ILRef(List<MESTestInfo> testInfo, int prodeuctIndex, int ilIndex, ref string errMsg)
        {
            errMsg = "";
            try
            {
                MESTestInfo ilTest = testInfo[ilIndex];

                //根据波长切换光开关
                if (!SetSwitch(ilTest, ref errMsg))
                {
                    ErrorBox(errMsg);
                    return;
                }

                List<double> rawdatas = new List<double>();

                //读取功率计的值
                List<double> xArr = new List<double>();
                int startTick = Environment.TickCount;
                for (int i = 0; i < 100; i++)
                {
                    List<double> power = new List<double>();
                    powermeter.ReadPowerAvg(ref errMsg, out power);
                    if (power.Count > 0 && (power[0] != -10000 && power[0] != CommonFunction.GetDefaultValue()))
                    {
                        rawdatas.Add(power[0]);
                        xArr.Add(xArr.Count + 1);
                    }
                    this.Dispatcher.Invoke(myUpdateCurveDelegate, xArr, rawdatas);
                }

                if (rawdatas.Count < 0)
                {
                    ErrorBox(errMsg);
                    return;
                }

                double ilMin;
                double ilMax;

                CommonFunction.GetMaxMin(rawdatas.ToArray(), out ilMax, out ilMin);

                //相同波长，相同端口的归零值一致
                for (int i = 0; i < testInfo.Count(); i++)
                {
                    if (testInfo[i].WLLeft == ilTest.WLLeft && testInfo[i].WLRight == ilTest.WLRight && testInfo[i].PortNameForAMTS == ilTest.PortNameForAMTS)
                    {
                        MESTestInfo info = templateControl.UpdateILRefData(i, ilMin);
                        //IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                        //nextSelect.ProductIndex = 0;
                        //nextSelect.ParamIndex = new List<int>();
                        //if (i + 1 == testInfo.Count())
                        //    nextSelect.ParamIndex.Add(0);
                        //else
                        //    nextSelect.ParamIndex.Add(i + 1);
                        //UpdateItem(info, i, nextSelect);
                        UpdateItem(info, i);
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// 更新曲线委托函数
        /// </summary>
        /// <param name="xArr"></param>
        /// <param name="yArr"></param>
        private void UpdateCurve(List <double > xArr,List <double >yArr)
        {
            curveShow.UpdateCurve(xArr, yArr);
        }

        /// <summary>
        /// IL测试响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnILTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectItem == null)
                {
                    uiVariable.Message = "未选择行";
                    return;
                }

                string errMsg = "";
                if (!templateControl.GetAllRef(ref errMsg))
                {
                    uiVariable.Message = "归零数据不完整！";
                    return;
                }

                uiVariable.Message = "开始IL测试";

                ButtonState(false);

                powermeterRealtimeThread.Abort();

                myUpdateCurveDelegate = new UpdateCurveDelegate(UpdateCurve);
                myUpdateBtnDelegate = new UpdateBtnDelegate(ButtonState);
                Thread TestThread = new Thread(new ThreadStart(TestFun));
                TestThread.Start();
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

        /// <summary>
        /// 更改按钮状态
        /// </summary>
        /// <param name="isEnabled"></param>
        private void ButtonState(bool isEnabled)
        {
            btnOpenTemplate.IsEnabled = isEnabled;
            btnILTest.IsEnabled = isEnabled;
            btnILRef.IsEnabled = isEnabled;
            btnPMReset.IsEnabled = isEnabled;
            btnSaveToAMTS.IsEnabled = isEnabled;
            btnSaveToAMTS2.IsEnabled = isEnabled;
        }

        /// <summary>
        /// IL测试线程
        /// </summary>
        private void TestFun()
        {
            List<MESTestInfo> testInfos = templateControl.GetAllTestInfo();
            if (isRetest)
            {
                foreach (int index in selectItem.ParamIndex)
                {
                    IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                    nextSelect.ProductIndex = 0;
                    nextSelect.ParamIndex = new List<int>();
                    nextSelect.ParamIndex.Add(index + 1);
                    DoTest(testInfos, index, nextSelect);
                }
            }
            else
            {
                foreach (int index in selectItem.ParamIndex)
                {
                    IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                    nextSelect.ProductIndex = 0;
                    nextSelect.ParamIndex = new List<int>();
                    nextSelect.ParamIndex.Add(index + 2);
                    DoTest(testInfos, index, nextSelect);
                }
            }
                
            powermeterRealtimeThread = new Thread(UpdatePowermeter);
            powermeterRealtimeThread.Start();
            this.Dispatcher.Invoke(myUpdateBtnDelegate, true);
        }

        /// <summary>
        /// IL测试
        /// </summary>
        /// <param name="testInfos">当前测试产品信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>测试结果</returns>
        private void DoTest(List<MESTestInfo> testInfos, int curIndex, IndexMap selectMap = null)
        {
            string errMsg = "";
            try
            {
                if (testInfos == null || testInfos.Count < curIndex)
                {
                    ErrorBox("测试信息出错");
                    return;
                }

                MESTestInfo curTest = testInfos[curIndex];

                //切换光源盒
                if (!SetSwitch(curTest, ref errMsg))
                {
                    ErrorBox(errMsg);
                    return;
                }

                //读取功率
                List<double> rawdatas = new List<double>();
                List<double> xArr = new List<double>();
                for (int i = 0; i < 100; i++)
                {
                    List<double> power = new List<double>();
                    powermeter.ReadPowerAvg(ref errMsg, out power);
                    if (power.Count > 0 && (power[0] != -10000 && power[0] != CommonFunction.GetDefaultValue()))
                    {
                        rawdatas.Add(power[0]);
                        xArr.Add(xArr.Count + 1);
                    }
                    this.Dispatcher.Invoke(myUpdateCurveDelegate, xArr, rawdatas);
                }

                if (curTest.TestParam == MESParam.MaxIL || curTest.TestParam == MESParam.PDL)
                {
                    for (int i = 0; i < testInfos.Count; i++)
                    {
                        //温度、波长、端口相同,PDL和IL同时测试
                        if ((testInfos[i].WLLeft == curTest.WLLeft) && (testInfos[i].WLRight == curTest.WLRight)
                            && (testInfos[i].PortNameForUser == curTest.PortNameForUser) && (testInfos[i].Temperature == curTest.Temperature))
                        {
                            if (testInfos[i].TestParam == MESParam.MaxIL)
                            {
                                curIndex = i;
                                curTest = templateControl.GetTestInfoByIndex(curIndex, ref errMsg);
                                double il =(-1)* algorithm.MaxIL(rawdatas.ToArray(), curTest.ILRef, ref errMsg);
                                il = Math.Round(il, 3);
                                bool isPass = true;
                                MESTestInfo info = templateControl.UpdateTestData(curIndex, il, ref isPass);
                                UpdateItem(info, curIndex, selectMap);
                                if (!templateControl.GetAllTestedPassed(ref errMsg))
                                {
                                    uiVariable.Message = "测试失败：" + errMsg;
                                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri(Environment.CurrentDirectory + "/image/Fail.ico", UriKind.Absolute))); }));
                                }
                                else
                                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri(Environment.CurrentDirectory + "/image/Pass.ico", UriKind.Absolute))); }));
                            }
                            else if (testInfos[i].TestParam == MESParam.PDL)
                            {
                                curIndex = i;
                                curTest = templateControl.GetTestInfoByIndex(curIndex, ref errMsg);
                                double il = algorithm.PDL(rawdatas.ToArray(), ref errMsg);
                                il = Math.Round(il, 3);
                                bool isPass = true;
                                MESTestInfo info = templateControl.UpdateTestData(curIndex, il, ref isPass);
                                UpdateItem(info, curIndex, selectMap);
                                if (!templateControl.GetAllTestedPassed(ref errMsg))
                                {
                                    uiVariable.Message = "测试失败：" + errMsg;
                                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri(Environment.CurrentDirectory + "/image/Fail.ico", UriKind.Absolute))); }));
                                }
                                else
                                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri(Environment.CurrentDirectory + "/image/Pass.ico", UriKind.Absolute))); }));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// 功率计复位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPMReset_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                FileInfo file = new FileInfo(System.Environment.CurrentDirectory + refDataFile);
                if (file.Exists)
                    file.Delete();
               
                if (powermeter != null)
                {
                    powermeter.ResetPowermeter(ref errMsg);
                    if (errMsg != "")
                    {
                        ErrorBox(errMsg);
                        return;
                    }

                    System.IO.FileInfo info = new System.IO.FileInfo(System.Environment.CurrentDirectory + pwmResetFile);
                    if (info.Exists)
                    {
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(System.Environment.CurrentDirectory + pwmResetFile, false, Encoding.Default);
                        sw.WriteLine(DateTime.Now.ToString());
                        sw.Close();
                        sw = null;
                    }
                    else
                    {
                        System.IO.FileStream fs = new System.IO.FileStream(System.Environment.CurrentDirectory + pwmResetFile, System.IO.FileMode.CreateNew);
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(fs, Encoding.Default);
                        sw.WriteLine(DateTime.Now.ToString());
                        sw.Close();
                        sw = null;
                        fs.Close();
                        fs = null;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
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

        /// <summary>
        /// 参数列表快捷键响应函数
        /// </summary>
        /// <param name="info"></param>
        private void UpdateKeyDown(KeyDownInfo info)
        {
            if (info.Key == Key.Subtract)
            {
                btnSaveToAMTS_Click(null, null);
            }
            else if (info.Key == Key.Multiply)
            {
                btnSaveToAMTS2_Click(null, null);
            }
            else if (info.Key == Key.Divide)
            {
                btnILRef_Click(null, null);
            }
            else if (info.Key == Key.Add)
            {
                btnILTest_Click(null, null);
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
                if (txtSN.IsFocused)
                {
                    btnOpenTemplate_Click(null, null);
                    e.Handled = true;
                }
            }
            else if (Keyboard.IsKeyDown(Key.Subtract))
            {
                btnSaveToAMTS_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Multiply))
            {
                btnSaveToAMTS2_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Divide))
            {
                btnILRef_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Add))
            {
                btnILTest_Click(null, null);
                e.Handled = true;
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

        /// <summary>
        /// 与界面功率计复位时间倒计时绑定
        /// </summary>
        private string pmResetTime;
        public string PMResetTime
        {
            get
            {
                return pmResetTime;
            }
            set
            {
                pmResetTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PMResetTime"));
            }
        }

        /// <summary>
        /// 与界面功率计复位时间倒计时绑定
        /// </summary>
        private string message;
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message"));
            }
        }
    }
}