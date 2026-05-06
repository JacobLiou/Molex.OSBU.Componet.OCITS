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

namespace UIDemuxTest
{
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIDemuxTest")]
    public partial class DemuxTest : UserControl
    {
        private const string refDataFile = "\\reference\\RefData.csv";
        private const string pwmResetFile = "\\temple\\PWMReset.csv";
        private const string templateConfig = "\\set\\template.ini";
        private const string passImage = "image/Pass.ico";
        private const string failImage = "image/Fail.ico";
        private DemuxCurve curveShow;

        /// <summary>
        /// 无纸化加载模板和保存数据服务器地址
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
        IPowermeter[] powermeters = new IPowermeter[4];

        /// <summary>
        /// 功率值实时显示后台线程
        /// </summary>
        private Thread powermeterRealtimeThread;
        private delegate void PowermeterRealtimeDelegate();
        PowermeterRealtimeDelegate powermeterRealtimeDelegate;

        /// <summary>
        /// 功率值复位后台线程
        /// </summary>
        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 工位信息
        /// </summary>
        MESTestProcess testProcess;
        MESTemplateType templateType;
        string userID = "";

        /// <summary>
        /// 功率计值
        /// </summary>
        private List<double>[] realtimePowers = new List<double>[4];
        private List<double>[] oldPowers = new List<double>[4];

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

        List<ILRefValue> ILRefs = new List<ILRefValue>();

        public DemuxTest()
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
                //读取四个功率计的值
                int channel = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (powermeters[i] != null)
                        powermeters[i].ReadPowerAvg(ref errMsg, out realtimePowers[i]);
                    else
                    {
                        DeviceControl.GetPowermeterByIndex(i + 1, ref channel, ref powermeters[i], ref errMsg);
                        if (powermeters[i] != null)
                            powermeters[i].ReadPowerAvg(ref errMsg, out realtimePowers[i]);
                    }
                }

                this.Dispatcher.Invoke(powermeterRealtimeDelegate);
            }
        }

        /// <summary>
        /// 功率值实时显示委托
        /// </summary>
        private void UpdatemeterDelegate()
        {
            if (realtimePowers.Count() <= 0)
                return;
            List<RealtimePowerInfo> powers = new List<RealtimePowerInfo>();

            if (realtimePowers[0] != null && realtimePowers[0].Count > 0)
            {
                if ((realtimePowers[0][0] == CommonFunction.GetDefaultValue() || realtimePowers[0][0] == -10000) && oldPowers[0] != null)
                    realtimePowers[0][0] = oldPowers[0][0];
                RealtimePowerInfo ch1 = new RealtimePowerInfo();
                ch1.Prefix = "";
                ch1.Power = realtimePowers[0][0].ToString("#0.000") + "dB";
                powers.Add(ch1);
            }

            if (realtimePowers[1] != null && realtimePowers[1].Count > 0)
            {
                if ((realtimePowers[1][0] == CommonFunction.GetDefaultValue() || realtimePowers[1][0] == -10000) && oldPowers[1] != null)
                    realtimePowers[1][0] = oldPowers[1][0];
                RealtimePowerInfo ch2 = new RealtimePowerInfo();
                ch2.Prefix = "";
                ch2.Power = realtimePowers[1][0].ToString("#0.000") + "dB";
                powers.Add(ch2);
            }

            if (realtimePowers[2] != null && realtimePowers[2].Count > 0)
            {
                if ((realtimePowers[2][0] == CommonFunction.GetDefaultValue() || realtimePowers[2][0] == -10000) && oldPowers[2] != null)
                    realtimePowers[2][0] = oldPowers[2][0];
                RealtimePowerInfo ch3 = new RealtimePowerInfo();
                ch3.Prefix = "";
                ch3.Power = realtimePowers[2][0].ToString("#0.000") + "dB";
                powers.Add(ch3);
            }

            if (realtimePowers[3] != null && realtimePowers[3].Count > 0)
            {
                if ((realtimePowers[3][0] == CommonFunction.GetDefaultValue() || realtimePowers[3][0] == -10000) && oldPowers[3] != null)
                    realtimePowers[3][0] = oldPowers[3][0];
                RealtimePowerInfo ch4 = new RealtimePowerInfo();
                ch4.Prefix = "";
                ch4.Power = realtimePowers[3][0].ToString("#0.000") + "dB";
                powers.Add(ch4);
            }

            oldPowers = realtimePowers;
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
            testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), info.TestProcess, true);
            templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), info.TemplateType, true);
            userID = info.UserID;

            curveShow = new DemuxCurve(EventAggregator);
            curveShow.InitCurve();

            powermeterRealtimeThread.Start();
            refTimeCheckBK.RunWorkerAsync();

            string errMsg = "";
            for (int i = 0; i < 4; i++)
            {
                if (powermeters[i] != null)
                {
                    powermeters[i].SetPMWavelength(1271.0, ref errMsg);
                    powermeters[i].SetPMUnits(ref errMsg, "dB");
                }
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
                if (templateControl.GetHasTested())
                {
                    MessageBoxResult msgRes = System.Windows.MessageBox.Show("有测试数据未保存，是否打开新条码？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (msgRes == MessageBoxResult.No)
                    {
                        MESProductInfo proInfo = templateControl.GetProductInfo();
                        txtSN.Text = proInfo.SN;
                        return;
                    }
                }
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
                if ((bool)rbtnTest.IsChecked)
                    testProcess = MESTestProcess.Test;
                else
                    testProcess = MESTestProcess.Test6;

                if (templateControl.OpenTemplate(amtsUrl, templateType, txtSN.Text, testProcess, testType, userID, "", true, bShowData, ref errMsg))
                {
                    if (errMsg != "")
                    {
                        CommonFunction.WriteLog(errMsg);
                        ErrorBox(errMsg);
                        return;
                    }

                    MESProductInfo curProInfo = templateControl.GetProductInfo();
                    int nFinishMode = Convert.ToInt32(curProInfo.FinishModel);
                    //带头如何判断
                    if (Convert.ToBoolean(nFinishMode & 1))
                    {
                        MessageBoxResult msgRes = System.Windows.MessageBox.Show("该产品已经测试过，要重新测试吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (msgRes == MessageBoxResult.No)
                        {
                            txtSN.Text = "";
                            txtSN.Focus();
                            return;
                        }
                    }

                    ILRefs = new List<ILRefValue>();
                    errMsg = "";
                    ReadRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);
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
                        IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                        nextSelect.ProductIndex = 0;
                        nextSelect.ParamIndex = new List<int>();
                        if (i + 1 == templateControl.GetAllTestInfo().Count)
                            nextSelect.ParamIndex.Add(0);
                        else
                            nextSelect.ParamIndex.Add(i + 1);
                        UpdateItem(info, i, nextSelect);
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
                        if (ts.Days > 0 || ts.Hours >= 6)
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
            for (int i = 0; i < 4; i++)
            {
                if (powermeters[i] != null)
                    powermeters[i].ResetPowermeter(ref errMsg);
                if (errMsg != "")
                {
                    ErrorBox(errMsg);
                    errMsg = "";
                }
            }

            System.IO.FileInfo file = new System.IO.FileInfo(System.Environment.CurrentDirectory + refDataFile);
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
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            InitRegerster();
            SelectedItemChangeRegister();
            KeyDownRegister();
        }

        /// <summary>
        /// 保存数据按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            try
            {
                if (templateControl.SaveDataToAMTS(templateControl.ProductSN, amtsSaveUrl, ref errMsg) != 0)
                {
                    uiVariable.Message = "保存失败!";
                    ErrorBox(errMsg);
                    CommonFunction.WriteLog(errMsg);
                    return;
                }

                templateControl.ClearAllData();
                List<MESControl> controls = new List<MESControl>();
                controls.Add(templateControl);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                }
                uiVariable.Message = "保存成功！";
                uiVariable.SN = "";
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
                    errMsg = "切换光源盒出错：" + errMsg;
                    return false;
                }
                //Devices device = Devices.OMSSwitch;
                //产品序号:波长:端口:参数
                string flag = ":" + testInfo.WLLeft.ToString() + ":" + testInfo.PortNameForUser + ":" + testInfo.TestParam.GetMESTemplateKeywords();
                if (opticalSwitch.SetSwitch(flag, ref errMsg) != 0)
                {
                    errMsg = "切换光源盒出错：" + errMsg;
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

        private void UpdateItem(MESTestInfo info, int paramIndex, IndexMap nextSelect = null)
        {
            ItemDemuxContent item = new ItemDemuxContent();
            item.TestInfo = info;
            for (int i = 0; i < ILRefs.Count; i++)
                if (ILRefs[i].LeftWL == info.WLLeft && ILRefs[i].RightWL == info.WLRight && ILRefs[i].TestParam == info.TestParam)
                {
                    item.Offset1 = ILRefs[i].ILOffset1;
                    item.Offset2 = ILRefs[i].ILOffset2;
                    item.Offset3 = ILRefs[i].ILOffset3;
                    break;
                }
            item.UpdateItemMap = new IndexMap();
            item.UpdateItemMap.ProductIndex = 0;
            item.UpdateItemMap.ParamIndex = new List<int>();
            item.UpdateItemMap.ParamIndex.Add(paramIndex);
            item.NextSelectMap = nextSelect;
            item.UpdateItemMap.ProductIndex = 0;
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventListItemUpdateDemux>().Publish(item);
            }
        }

        /// <summary>
        /// IL归零响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnILRef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectItem == null)
                {
                    return;
                }
                ButtonState(false);
                powermeterRealtimeThread.Abort();

                myUpdateCurveDelegate = new UpdateCurveDelegate(UpdateCurve);
                myUpdateBtnDelegate = new UpdateBtnDelegate(ButtonState);
                Thread ILRefThread = new Thread(new ThreadStart(ILRefFun));
                ILRefThread.Start();
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
        /// IL归零线程
        /// </summary>
        private void ILRefFun()
        {
            string errMsg = "";
            List<MESTestInfo> testInfo = templateControl.GetAllTestInfo();
            int count = templateControl.GetAllTestInfo().Count;
            for (int i = 0; i < count; i = i + 2)
            {
                ILRef(testInfo, selectItem.ProductIndex, i, ref errMsg);
                uiVariable.Message = "波长" + (1271 + (i / 2) * 20) + "归零结束！";
            }
            powermeterRealtimeThread = new Thread(UpdatePowermeter);
            powermeterRealtimeThread.Start();

            this.Dispatcher.Invoke(myUpdateBtnDelegate, true);

            errMsg = "";
            if (!GetAllRef(ref errMsg))
                return;
            RecordRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg);
            if (errMsg.Length > 0)
            {
                CommonFunction.WriteLog(errMsg);
            }
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
                if (ilTest == null)
                {
                    ErrorBox("选中行没有IL");
                    return;
                }

                //根据波长切换光开关
                if (!SetSwitch(ilTest, ref errMsg))
                {
                    ErrorBox(errMsg);
                    return;
                }

                List<double> rawdatas = new List<double>();

                //读取四个功率计的值，判断连接的功率计是哪个
                int channel = 0;
                List<double>[] powerArray = new List<double>[4];
                for (int i = 0; i < 4; i++)
                {
                    if (powermeters[i] != null)
                        powermeters[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                }

                List<double> xArr = new List<double>();
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(powerArray[i][0]) >= 0 && Math.Abs(powerArray[i][0]) <= 20)
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            powermeters[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                            if (powerArray.Count() > 0)
                            {
                                rawdatas.Add(powerArray[i][0]);
                                xArr.Add(xArr.Count + 1);
                            }
                            this.Dispatcher.Invoke(myUpdateCurveDelegate, xArr, rawdatas);
                        }
                        channel = i;
                        break;
                    }
                }

                if (rawdatas == null)
                    uiVariable.Message = "归零失败，请确保其中一通道有光！";

                double ilMin;
                double ilMax;

                if (rawdatas.Count <= 0)
                {
                    uiVariable.Message = "归零失败，请重新归零!";
                    return;
                }
                CommonFunction.GetMaxMin(rawdatas.ToArray(), out ilMax, out ilMin);

                //相同波长，相同端口的归零值一致
                for (int i = 0; i < testInfo.Count(); i++)
                {
                    if (testInfo[i].WLLeft == ilTest.WLLeft && testInfo[i].WLRight == ilTest.WLRight && testInfo[i].PortNameForAMTS == ilTest.PortNameForAMTS)
                    {
                        MESTestInfo info = UpdateILRefData(i, ilMin, channel);
                        IndexMap nextSelect = new MolexUtility.UIList.IndexMap();
                        nextSelect.ProductIndex = 0;
                        nextSelect.ParamIndex = new List<int>();
                        if (i + 1 == testInfo.Count())
                            nextSelect.ParamIndex.Add(0);
                        else
                            nextSelect.ParamIndex.Add(i + 1);
                        UpdateItem(info, i, nextSelect);
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
        private void UpdateCurve(List<double> xArr, List<double> yArr)
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
                if (!GetAllRef(ref errMsg))
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

        private void TestFun()
        {
            if (isRetest)
            {
                IndexMap selectMap = new IndexMap();
                selectMap.ParamIndex = new List<int>();
                selectMap.ParamIndex.Add(selectItem.RowIndex + 1);
                DoTest(templateControl.GetAllTestInfo(), selectItem.RowIndex, selectMap);
            }
            else
            {
                IndexMap selectMap = new IndexMap();
                selectMap.ParamIndex = new List<int>();
                selectMap.ParamIndex.Add(selectItem.RowIndex + 1);
                DoTest(templateControl.GetAllTestInfo(), selectItem.RowIndex, selectMap);
            }
            powermeterRealtimeThread = new Thread(UpdatePowermeter);
            powermeterRealtimeThread.Start();

            this.Dispatcher.Invoke(myUpdateBtnDelegate, true);
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
            btnILTestOne.IsEnabled = isEnabled;
        }

        /// <summary>
        /// IL一键测试响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnILTestOne_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string errMsg = "";
                if (!GetAllRef(ref errMsg))
                {
                    uiVariable.Message = "归零数据不完整！";
                    return;
                }

                uiVariable.Message = "开始IL一键测试";

                ButtonState(false);

                powermeterRealtimeThread.Abort();

                myUpdateCurveDelegate = new UpdateCurveDelegate(UpdateCurve);
                myUpdateBtnDelegate = new UpdateBtnDelegate(ButtonState);
                Thread TestThread = new Thread(new ThreadStart(OneKeyTestFun));
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
        /// IL一键测试线程函数
        /// </summary>
        private void OneKeyTestFun()
        {
            int count = templateControl.GetAllTestInfo().Count;
            if (isRetest)
            {
                for (int i = 0; i < count; i++)
                {
                    IndexMap selectMap = new IndexMap();
                    selectMap.ParamIndex = new List<int>();
                    if (i < count - 1)
                    {
                        //调整到下一个测试项
                        selectMap.ParamIndex.Add(i + 1);
                    }
                    DoTest(templateControl.GetAllTestInfo(), i, selectMap);
                }
            }
            else
            {
                for (int i = 0; i < count; i = i + 2)
                {
                    IndexMap selectMap = new IndexMap();
                    selectMap.ParamIndex = new List<int>();
                    if (i < count - 1)
                    {
                        //调整到下一个测试项
                        selectMap.ParamIndex.Add(i + 1);
                    }
                    DoTest(templateControl.GetAllTestInfo(), i, selectMap);
                }
            }
            powermeterRealtimeThread = new Thread(UpdatePowermeter);
            powermeterRealtimeThread.Start();

            this.Dispatcher.Invoke(myUpdateBtnDelegate, true);
        }

        /// <summary>
        /// 根据测试项，调不同的处理函数
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

                Thread.Sleep(500);
                //读取功率
                List<double> rawdatas = new List<double>();

                //读取四个功率计的值，判断连接的功率计是哪个
                List<double>[] powerArray = new List<double>[4];
                for (int j = 0; j < 4; j++)
                {
                    if (powermeters[j] != null)
                        powermeters[j].ReadPowerAvg(ref errMsg, out powerArray[j]);
                }
                int i = 0;
                List<double> xArr = new List<double>();
                for (; i < 4; i++)
                {
                    if (Math.Abs(powerArray[i][0]) > 0 && Math.Abs(powerArray[i][0]) < 20)
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            powermeters[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                            if (powerArray.Count() > 0)
                            {
                                rawdatas.Add(powerArray[i][0]);
                                xArr.Add(xArr.Count + 1);
                            }
                            this.Dispatcher.Invoke(myUpdateCurveDelegate, xArr, rawdatas);
                        }
                        break;
                    }
                }

                if (i == 4)
                    return;
                double ilRef = CommonFunction.GetDefaultValue();
                if (i == 0)
                    ilRef = curTest.ILRef;
                else if (i == 1)
                {
                    for (int j = 0; j < ILRefs.Count; j++)
                    {
                        if (ILRefs[j].LeftWL == curTest.WLLeft && ILRefs[j].RightWL == curTest.WLRight && ILRefs[j].TestParam == curTest.TestParam)
                        {
                            ilRef = ILRefs[j].ILOffset1;
                            break;
                        }
                    }
                }
                else if (i == 2)
                {
                    for (int j = 0; j < ILRefs.Count; j++)
                    {
                        if (ILRefs[j].LeftWL == curTest.WLLeft && ILRefs[j].RightWL == curTest.WLRight && ILRefs[j].TestParam == curTest.TestParam)
                        {
                            ilRef = ILRefs[j].ILOffset2;
                            break;
                        }
                    }
                }
                else if (i == 3)
                {
                    for (int j = 0; j < ILRefs.Count; j++)
                    {
                        if (ILRefs[j].LeftWL == curTest.WLLeft && ILRefs[j].RightWL == curTest.WLRight && ILRefs[j].TestParam == curTest.TestParam)
                        {
                            ilRef = ILRefs[j].ILOffset3;
                            break;
                        }
                    }
                }

                if (curTest.TestParam == MESParam.MaxIL || curTest.TestParam == MESParam.PDL)
                {
                    for (int l = 0; l < testInfos.Count; l++)
                    {
                        //温度、波长、端口相同,PDL和IL同时测试
                        if ((testInfos[l].WLLeft == curTest.WLLeft) && (testInfos[l].WLRight == curTest.WLRight)
                            && (testInfos[l].PortNameForUser == curTest.PortNameForUser) && (testInfos[l].Temperature == curTest.Temperature))
                        {
                            if (testInfos[l].TestParam == MESParam.MaxIL)
                            {
                                curIndex = l;
                                curTest = templateControl.GetTestInfoByIndex(curIndex, ref errMsg);
                                double il = (-1) * algorithm.MaxIL(rawdatas.ToArray(), ilRef, ref errMsg);
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
                            else if (testInfos[l].TestParam == MESParam.PDL)
                            {
                                curIndex = l;
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

                for (int i = 0; i < 4; i++)
                {
                    if (powermeters[i] != null)
                        powermeters[i].ResetPowermeter(ref errMsg);
                    if (errMsg != "")
                    {
                        ErrorBox(errMsg);
                        return;
                    }
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
            if (info.Key == Key.Insert)
            {
                btnSaveToAMTS_Click(null, null);
            }
            else if (info.Key == Key.Divide)
            {
                btnILRef_Click(null, null);
            }
            else if (info.Key == Key.Add)
            {
                btnILTestOne_Click(null, null);
            }
            else if (info.Key == Key.Subtract)
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
            else if (Keyboard.IsKeyDown(Key.Insert))
            {
                btnSaveToAMTS_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Divide))
            {
                btnILRef_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Add))
            {
                btnILTestOne_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Subtract))
            {
                btnILTest_Click(null, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 更新归零数据
        /// </summary>
        /// <param name="nIndex">更新nIndex行的数据</param>
        /// <param name="dILRef">归零数据</param>
        public MESTestInfo UpdateILRefData(int nIndex, double dILRef, int channel)
        {
            MESTestInfo testInfo = null;
            List<MESTestInfo> allTestInfo = templateControl.GetAllTestInfo();

            if (allTestInfo.Count > nIndex)
            {
                testInfo = allTestInfo[nIndex].Clone();
                if (channel == 0)
                {
                    testInfo.ILRef = dILRef;
                    templateControl.UpdateILRefData(nIndex, dILRef);
                }
                else if (channel == 1)
                {
                    int i = 0;
                    for (; i < ILRefs.Count; i++)
                    {
                        if (ILRefs[i].LeftWL == testInfo.WLLeft && ILRefs[i].RightWL == testInfo.WLRight && ILRefs[i].TestParam == testInfo.TestParam)
                        {
                            ILRefs[i].ILOffset1 = dILRef;
                            break;
                        }
                    }
                    if (i >= ILRefs.Count)
                    {
                        ILRefValue il = new UIDemuxTest.ILRefValue();
                        il.LeftWL = testInfo.WLLeft;
                        il.RightWL = testInfo.WLRight;
                        il.TestParam = testInfo.TestParam;
                        il.ILOffset1 = dILRef;
                        ILRefs.Add(il);
                    }
                }
                else if (channel == 2)
                {
                    int i = 0;
                    for (; i < ILRefs.Count; i++)
                    {
                        if (ILRefs[i].LeftWL == testInfo.WLLeft && ILRefs[i].RightWL == testInfo.WLRight && ILRefs[i].TestParam == testInfo.TestParam)
                        {
                            ILRefs[i].ILOffset2 = dILRef;
                            break;
                        }
                    }
                    if (i >= ILRefs.Count)
                    {
                        ILRefValue il = new UIDemuxTest.ILRefValue();
                        il.LeftWL = testInfo.WLLeft;
                        il.RightWL = testInfo.WLRight;
                        il.TestParam = testInfo.TestParam;
                        il.ILOffset2 = dILRef;
                        ILRefs.Add(il);
                    }
                }
                else if (channel == 3)
                {
                    int i = 0;
                    for (; i < ILRefs.Count; i++)
                    {
                        if (ILRefs[i].LeftWL == testInfo.WLLeft && ILRefs[i].RightWL == testInfo.WLRight && ILRefs[i].TestParam == testInfo.TestParam)
                        {
                            ILRefs[i].ILOffset3 = dILRef;
                            break;
                        }
                    }
                    if (i >= ILRefs.Count)
                    {
                        ILRefValue il = new UIDemuxTest.ILRefValue();
                        il.LeftWL = testInfo.WLLeft;
                        il.RightWL = testInfo.WLRight;
                        il.TestParam = testInfo.TestParam;
                        il.ILOffset3 = dILRef;
                        ILRefs.Add(il);
                    }
                }
            }

            return testInfo;
        }

        /// <summary>
        /// 是否全部都归零
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>是否全部归零</returns>
        public bool GetAllRef(ref string errMsg)
        {
            foreach (MESTestInfo info in templateControl.GetAllTestInfo())
            {
                if (info.ILRef == CommonFunction.GetDefaultValue() || info.ILRef == CommonFunction.GetFormatDefaultValue() || info.ILRef == -10000)
                {
                    errMsg = "归零数据不完整！";
                    return false;
                }
                for (int i = 0; i < ILRefs.Count; i++)
                {
                    if (ILRefs[i].LeftWL == info.WLLeft && ILRefs[i].RightWL == info.WLRight && ILRefs[i].TestParam == info.TestParam)
                    {
                        if (ILRefs[i].ILOffset1 == CommonFunction.GetDefaultValue() || ILRefs[i].ILOffset1 == CommonFunction.GetFormatDefaultValue() || ILRefs[i].ILOffset1 == -10000
                            || ILRefs[i].ILOffset2 == CommonFunction.GetDefaultValue() || ILRefs[i].ILOffset2 == CommonFunction.GetFormatDefaultValue() || ILRefs[i].ILOffset2 == -10000
                            || ILRefs[i].ILOffset3 == CommonFunction.GetDefaultValue() || ILRefs[i].ILOffset3 == CommonFunction.GetFormatDefaultValue() || ILRefs[i].ILOffset3 == -10000)
                            return false;
                        break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 写归零数据
        /// </summary>
        /// <param name="strFilePath">归零数据文件路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>操作是否成功</returns>
        public bool RecordRefData(string strFilePath, ref string errMsg)
        {
            try
            {
                List<MESTestInfo> infoArr;
                infoArr = templateControl.GetAllTestInfo();
                string strWrite = "";
                strWrite += templateControl.GetProductInfo().TemplateID + "\n";
                foreach (MESTestInfo info in infoArr)
                {
                    for (int i = 0; i < ILRefs.Count; i++)
                    {
                        if (ILRefs[i].LeftWL == info.WLLeft && ILRefs[i].RightWL == info.WLRight && ILRefs[i].TestParam == info.TestParam)
                        {
                            strWrite += string.Format("{0:0.000},{1:0.000},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000}\n",
                                info.WLLeft, info.WLRight, info.PortNameForUser, info.TestParam.GetMESTemplateKeywords(), info.ILRef, info.RLRef,
                                ILRefs[i].ILOffset1, ILRefs[i].ILOffset2, ILRefs[i].ILOffset3);
                            break;
                        }
                    }
                }
                CommonFunction.WriteFile(strFilePath, strWrite);
                return true;
            }
            catch (Exception ex)
            {
                errMsg = "RecordRefData 出错：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 读取归零数据
        /// </summary>
        /// <param name="strFilePath">归零数据文件路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>操作是否成功</returns>
        public bool ReadRefData(string strFilePath, ref string errMsg)
        {
            try
            {
                if (!File.Exists(strFilePath))
                {
                    errMsg = "归零文件不存在！";
                    return false;
                }
                StreamReader sr = new StreamReader(strFilePath, Encoding.Default);
                string line;
                List<string> refList = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    refList.Add(line.ToString());
                }
                sr.Close();
                sr = null;
                if (refList.Count == 0)
                    return false;
                //模板不对应
                if (templateControl.GetProductInfo().TemplateID != refList[0])
                {
                    errMsg = "当前模板与前一模板不一致，请重新归零！";
                    return false;
                }
                List<MESTestInfo> allTestInfo = templateControl.GetAllTestInfo();
                //归零数据不完整
                if (allTestInfo.Count != (refList.Count - 1))
                {
                    errMsg = "归零数据不完整！";
                    return false;
                }
                for (int i = 0; i < refList.Count - 1; i++)
                {
                    string[] strRef = refList[i + 1].Split(',');
                    if (strRef.Length < 9)
                        return false;

                    templateControl.UpdateILRefData(i, Convert.ToDouble(strRef[4]));
                    int j = 0;
                    for (; j < ILRefs.Count; j++)
                    {
                        if (allTestInfo[i].WLLeft == ILRefs[j].LeftWL && allTestInfo[i].WLRight == ILRefs[j].RightWL &&
                           allTestInfo[i].TestParam == ILRefs[j].TestParam)
                        {
                            ILRefs[j].ILOffset1 = Convert.ToDouble(strRef[6]);
                            ILRefs[j].ILOffset2 = Convert.ToDouble(strRef[7]);
                            ILRefs[j].ILOffset3 = Convert.ToDouble(strRef[8]);
                            break;
                        }
                    }
                    if (j >= ILRefs.Count)
                    {
                        ILRefValue il = new ILRefValue();
                        il.LeftWL = allTestInfo[i].WLLeft;
                        il.RightWL = allTestInfo[i].WLRight;
                        il.TestParam = allTestInfo[i].TestParam;
                        il.ILOffset1 = Convert.ToDouble(strRef[6]);
                        il.ILOffset2 = Convert.ToDouble(strRef[7]);
                        il.ILOffset3 = Convert.ToDouble(strRef[8]);
                        ILRefs.Add(il);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = "ReadRefData 出错：" + ex.Message;
                return false;
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

    public class ILRefValue
    {
        public int Index { get; set; }
        public double ILOffset1 { get; set; }
        public double ILOffset2 { get; set; }
        public double ILOffset3 { get; set; }
        public double LeftWL { get; set; }
        public double RightWL { get; set; }
        public MESParam TestParam { get; set; }

        public ILRefValue()
        {
            Index = -1;
            ILOffset1 = CommonFunction.GetDefaultValue();
            ILOffset2 = CommonFunction.GetDefaultValue();
            ILOffset3 = CommonFunction.GetDefaultValue();
            LeftWL = CommonFunction.GetDefaultValue();
            RightWL = CommonFunction.GetDefaultValue();
            TestParam = MESParam.Default;
        }
    }
}