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
namespace UIDemuxTest
{
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIDemuxTest")]
    public partial class DemuxTest : UserControl
    {
        private const string saveWebservicSection = "Webservice Set";
        private const string refDataFile = "\\temple\\RefData.csv";
        private const string pwmResetFile = "\\temple\\PWMReset.csv";
        private const string templateConfig = "\\set\\template.ini";
        private const string cfgProcessSection = "TestProcess";
        private const string cfgProcessUserKey = "UserProcess";
        private const string cfgProcessAMTSKey = "AMTSProcess";
        private const string cfgCurProcessKey = "CurProcess";
        private const string cfgTemplateTypeSection = "TemplateType";
        private const string cfgCurTemplateKey = "CurType";
        private const string cfgDevicePath = "\\set\\config.ini";
        private const string cfgDeviceSection = "DEVICE";
        private const string cfgDeviceTypeKey = "DeviceType";
        private const string cfgDevicePort = "Port";
        private const string passImage = "image/Pass.ico";
        private const string failImage = "image/Fail.ico";
        private int curveTest = 0;

        /// <summary>
        /// 无纸化加载模板和保存数据服务器地址
        /// </summary>
        private string amtsUrl = "http://172.18.1.101/amts/";

        /// <summary>
        /// 选中测试列表index
        /// </summary>
        private IndexMap selectItem = null;

        /// <summary>
        /// 产品测试信息
        /// </summary>
        private MESControl templateControl = new MESControl();

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


        [Import(typeof(IAlgotithm))]
        private IAlgotithm algorithm;

        [Import(typeof(IDemuxAlgorithm))]
        private IDemuxAlgorithm demuxAlgorithm;

        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

        /// <summary>
        /// 是否可以终止当前测试项的测试
        /// </summary>
        private bool isStopCurrTest = true;
        private bool StopCurrTest = false;

        /// <summary>
        /// 一键测试lock对象
        /// </summary>
        private object onekeyObject = new object();

        IPowermeter[] powers = new IPowermeter[4];
        private int powerCount = 1;

        /// <summary>
        /// 功率计实时显示后台线程
        /// </summary>
        private BackgroundWorker powermeterRealtimeBK;

        private BackgroundWorker refTimeCheckBK;

        /// <summary>
        /// 功率计值
        /// </summary>
        private List<double>[] realtimePowers = new List<double>[4];
        public DemuxTest()
        {
            InitializeComponent();

            uiVariable.IsEnable = true;

            txtSN.DataContext = uiVariable;
            btnOpenTemplate.DataContext = uiVariable;
            btnSave.DataContext = uiVariable;
            btnILRef.DataContext = uiVariable;
            btnILTest.DataContext = uiVariable;
            chkRetest.DataContext = uiVariable;
            chkLoadData.DataContext = uiVariable;
            btnPMReset.DataContext = uiVariable;
            btnPMUnLock.DataContext = uiVariable;
            lblPMResetTime.DataContext = uiVariable;
            lblMessage.DataContext = uiVariable;

            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");

            //获取四个功率计的对象
            string errMsg = "";
            int channel = 0;
            DeviceControl.GetPowermeterByIndex(1, ref channel, ref powers[0], ref errMsg);
            DeviceControl.GetPowermeterByIndex(2, ref channel, ref powers[1], ref errMsg);
            DeviceControl.GetPowermeterByIndex(3, ref channel, ref powers[2], ref errMsg);
            DeviceControl.GetPowermeterByIndex(4, ref channel, ref powers[3], ref errMsg);
            if (powers[0] != null && powers[1] != null && powers[2] != null && powers[3] != null)
                powerCount = 4;

            imgResult.Source = new BitmapImage(new Uri("/image/" + "Fail.ico", UriKind.Absolute));

            powermeterRealtimeBK = new BackgroundWorker();
            powermeterRealtimeBK.DoWork += PowermeterRealtime_DoWork;
            powermeterRealtimeBK.ProgressChanged += PowermeterRealtimeShow_Progress;
            powermeterRealtimeBK.WorkerSupportsCancellation = true;
            powermeterRealtimeBK.WorkerReportsProgress = true;

            //refTimeCheckBK = new BackgroundWorker();
            //refTimeCheckBK.DoWork += RefTimeCheck_DoWork;
            //refTimeCheckBK.ProgressChanged += RefTimeCheck_Progress;
            //refTimeCheckBK.WorkerSupportsCancellation = true;
            //refTimeCheckBK.WorkerReportsProgress = true;
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

                string errMsg = "";
                //读取四个功率计的值
                for (int i = 0; i < 4; i++)
                {
                    if (powers[i] != null)
                        powers[i].ReadPowerAvg(ref errMsg, out realtimePowers[i]);
                }

                powermeterRealtimeBK.ReportProgress(1);
            }
        }

        private void PowermeterRealtimeShow_Progress(object sender, ProgressChangedEventArgs e)
        {
            List<RealtimePowerInfo> powers = new List<RealtimePowerInfo>();

            RealtimePowerInfo ch1 = new RealtimePowerInfo();
            ch1.Prefix = "";
            ch1.Power = realtimePowers[0][0].ToString("#0.000") + "dB";
            powers.Add(ch1);

            if (powerCount == 4)
            {
                RealtimePowerInfo ch2 = new RealtimePowerInfo();
                ch2.Prefix = "";
                ch2.Power = realtimePowers[1][0].ToString("#0.000") + "dB";
                powers.Add(ch2);

                RealtimePowerInfo ch3 = new RealtimePowerInfo();
                ch3.Prefix = "";
                ch3.Power = realtimePowers[2][0].ToString("#0.000") + "dB";
                powers.Add(ch3);

                RealtimePowerInfo ch4 = new RealtimePowerInfo();
                ch4.Prefix = "";
                ch4.Power = realtimePowers[3][0].ToString("#0.000") + "dB";
                powers.Add(ch4);
            }

            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventRealtimePowerUpdate>().Publish(powers);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            powermeterRealtimeBK.CancelAsync();
            //refTimeCheckBK.CancelAsync();

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

        public void Init(MainInitInfo info)
        {
            //mainInfo = info;
            //testProcess = (MESTestProcess)Enum.Parse(typeof(MESTestProcess), mainInfo.TestProcess, true);
            //templateType = (MESTemplateType)Enum.Parse(typeof(MESTemplateType), mainInfo.TemplateType, true);

            //curveShow = new InterleaverCurve(EventAggregator);
            //paramCal = new InterleaverParamCal(algorithm);

            ////曲线显示初始化
            //curveShow.InitAllCurve();

            //powermeterRealtimeBK.RunWorkerAsync();
            //uiVariable.IsNoPDL = true;
            //uiVariable.IsPort12 = true;
            //uiVariable.IsPDL = false;
            //uiVariable.IsPort34 = false;
            //string errMsg = "";
            //ReadRefTime(ref errMsg);
            //ReadRefData(ref errMsg);
            //refTimeCheckBK.RunWorkerAsync();
        }

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
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
            if (uiVariable.SN.Length == 0)
            {
                WarningBox("请输入产品号！！");
                return;
            }

            if (templateControl.OpenTemplate(amtsUrl, MESTemplateType.DC, uiVariable.SN, MESTestProcess.Test, MESTestType.Normal, "11091", "", true, false, ref errMsg))
            {
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

                if (!templateControl.ReadRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg))
                {
                    ErrorBox(errMsg);
                    return;
                }

                uiVariable.SN = "";
                uiVariable.Message = "模板打开成功！";

                List<MESControl> controls = new List<MESControl>();
                controls.Add(templateControl);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
                }
            }
            else
            {
                ErrorBox(errMsg);
                txtSN.Text = "";
                txtSN.Focus();
                return;
            }

            ////提取出来，统一显示
            //List<TemplateListBoxShow> proDataShow = new List<TemplateListBoxShow>();
            //proDataShow.Add(new TemplateListBoxShow("SpecNO:", curProInfo.SpecNO));
            //proDataShow.Add(new TemplateListBoxShow("TemplateID:", curProInfo.TemplateID));
            //proDataShow.Add(new TemplateListBoxShow("PN:", curProInfo.ProductPN));
            //proDataShow.Add(new TemplateListBoxShow("SO:", curProInfo.SO));
            //ProductInfoList.ItemsSource = proDataShow;

            //OPTestInfo[] a = templateControl.GetAllTestInfo();
            //testDataShow.InitView(ref dataView, templateControl.GetAllTestInfo(), true);

            //UpdateResIcon();

            chkRetest.IsEnabled = false;
            chkLoadData.IsEnabled = false;
        }

        /// <summary>
        /// 功率计复位
        /// </summary>
        private void ResetPWM()
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
                DateTime dt = Convert.ToDateTime(strTime);
                TimeSpan ts = DateTime.Now - dt;
                TimeSpan dtarget = new TimeSpan(6, 0, 0);
                TimeSpan tsRemainder = dtarget - ts;
                if (tsRemainder.Hours == 0 && tsRemainder.Minutes < 30)
                {
                    lblPMResetTime.Foreground = Brushes.Red;
                }
                else
                    lblPMResetTime.Foreground = Brushes.Black;
                lblPMResetTime.Content = string.Format("{0}小时{1}分{2}秒", tsRemainder.Hours, tsRemainder.Minutes, tsRemainder.Seconds);
                if (ts.Days > 0 || (ts.Hours >= 6 && ts.Minutes >= 30))
                {
                    string errMsg = "";
                    if (powerCount == 4)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (powers[i] != null)
                                powers[i].ResetPowermeter(ref errMsg);
                            if (errMsg != "")
                            {
                                lblMessage.Content = errMsg;
                                errMsg = "";
                            }
                        }
                    }
                    else
                    {
                        if (powers[0] != null)
                            powers[0].ResetPowermeter(ref errMsg);
                        if (errMsg != "")
                        {
                            lblMessage.Content = errMsg;
                            errMsg = "";
                        }
                    }

                    //记录新的归零时间
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

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\..\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            SelectedItemChangeRegister();
        }

        /// <summary>
        /// 保存数据按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";

            if (templateControl.SaveDataToAMTS(templateControl.ProductSN, amtsUrl, ref errMsg) != 0)
            {
                ErrorBox(errMsg);
            }

            List<MESControl> controls = new List<MESControl>();
            controls.Add(templateControl);
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventTemplateUpdate>().Publish(controls);
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
            return true;
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType("OplinkSwitch", ref opticalSwitch, ref errMsg) != 0)
            {
                errMsg = "切换光源盒出错：" + errMsg;
                return false;
            }
            Devices device = Devices.OplinkSwitch;
            //光源类型：产品序号:波长:端口:参数
            string flag = device.GetAdditional() + "::" + testInfo.WLLeft.ToString() + ":" + testInfo.PortNameForUser + ":" + testInfo.TestParam.GetMESTemplateKeywords();
            if (opticalSwitch.SetSwitch(flag, ref errMsg) != 0)
            {
                errMsg = "切换光源盒出错：" + errMsg;
                return false;
            }
            return true;
        }

        /// <summary>
        /// IL归零
        /// </summary>
        /// <param name="testInfo">所有测试项信息</param>
        /// <param name="rlTest">当前归零行</param>
        /// <param name="rlIndex">归零行的序列号</param>
        private void ILRef(List<MESTestInfo> testInfo, int prodeuctIndex, int ilIndex, ref string errMsg)
        {
            MESTestInfo ilTest = testInfo[ilIndex];
            errMsg = "";
            if (ilTest == null)
            {
                errMsg = "选中行没有IL";
                return;
            }

            //根据波长切换光开关
            if (!SetSwitch(ilTest, ref errMsg))
            {
                return;
            }

            List<List<double>> rawdatas = null;

            if (powerCount == 4)
            {
                //读取四个功率计的值，判断连接的功率计是哪个
                List<double>[] powerArray = new List<double>[4];
                for (int i = 0; i < 4; i++)
                {
                    if (powers[i] != null)
                        powers[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (powerArray[i][0] > 0 && powerArray[i][0] < 20)
                        powers[i].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);
                }
            }
            else
                powers[0].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);

            double ilMin;
            double ilMax;

            CommonFunction.GetMaxMin(rawdatas[0].ToArray(), out ilMax, out ilMin);

            //相同波长，相同端口的归零值一致
            for (int i = 0; i < testInfo.Count(); i++)
            {
                if (testInfo[i].WLLeft == ilTest.WLLeft && testInfo[i].WLRight == ilTest.WLRight && testInfo[i].PortNameForAMTS == ilTest.PortNameForAMTS)
                {
                    MESTestInfo info = templateControl.UpdateILRefData(i, ilMin, i);
                    UpdateItem(info, prodeuctIndex, i);
                }
            }
        }

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

        ///// <summary>
        ///// Demux--四个功率计同时测试ILRef
        ///// </summary>
        ///// <param name="index"></param>
        //private void AutoILRefBackWork(int index)
        //{
        //    string errMsg = "";
        //    MESTestInfo curTestInfo = templateControl.GetTestInfoByIndex(index,ref errMsg);
        //    double[] dPowerArr = null;
        //    double dILRef = CommonFunction.GetDefaultValue();

        //    if (curTestInfo.WLLeft == 1271.0 && curTestInfo.WLRight == 1271.0)
        //    {
        //        ligthSwitch.SetSwitch(0, 0);
        //        pwmControl[0].GetPower_PDL(0, 20, 0, out errMsg);
        //        pwmControl[0].GetRecordPower(0, out dPowerArr);
        //    }
        //    else if (curTestInfo.WLLeft == 1291.0 && curTestInfo.WLRight == 1291.0)
        //    {
        //        ligthSwitch.SetSwitch(0, 1);
        //        pwmControl[1].GetPower_PDL(0, 20, 0, out errMsg);
        //        pwmControl[1].GetRecordPower(0, out dPowerArr);
        //    }
        //    else if (curTestInfo.WLLeft == 1311.0 && curTestInfo.WLRight == 1311.0)
        //    {
        //        ligthSwitch.SetSwitch(0, 2);
        //        pwmControl[2].GetPower_PDL(0, 20, 0, out errMsg);
        //        pwmControl[2].GetRecordPower(0, out dPowerArr);
        //    }
        //    else if (curTestInfo.WLLeft == 1331.0 && curTestInfo.WLRight == 1331.0)
        //    {
        //        ligthSwitch.SetSwitch(0, 1);
        //        pwmControl[3].GetPower_PDL(0, 20, 0, out errMsg);
        //        pwmControl[3].GetRecordPower(0, out dPowerArr);
        //    }
        //    else
        //    { }

        //    if (dPowerArr == null)
        //        return;
        //    dILRef = ParamCalculate.CalculatePeakIL(dPowerArr, 0, dPowerArr.Length);
        //    OPTestInfo[] allTestInfo = templateControl.GetAllTestInfo();
        //    for (int i = 0; i < allTestInfo.Length; i++)
        //    {
        //        if ((allTestInfo[i].WLLeft == curTestInfo.WLLeft) && (allTestInfo[i].WLRight == curTestInfo.WLRight))
        //        {
        //            templateControl.UpdateILRefData(i, dILRef);
        //            testDataShow.UpdateRefView(ref dataView, i, templateControl.GetTestInfoByIndex(i));
        //        }
        //    }
        //    templateControl.RecordRefData(System.Environment.CurrentDirectory + refDataFile);

        //    for (int i = 0; i < dataView.RowCount; i++)
        //    {
        //        if (dataView.Rows[i].Cells["ILREF"].Value.ToString() == "")
        //        {
        //            AutoILRefBackWork(i);
        //        }
        //    }
        //}

        /// <summary>
        /// 根据测试项，调不同的处理函数
        /// </summary>
        /// <param name="testInfos">当前测试产品信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>测试结果</returns>
        private void DoTest(List<MESTestInfo> testInfos, int productIndex, int curIndex, IndexMap selectMap = null)
        {
            string errMsg = "";
            try
            {
                if (testInfos == null || testInfos.Count < curIndex)
                {
                    errMsg = "测试信息出错";
                    return;
                }
                MESTestInfo curTest = testInfos[curIndex];
                double result = CommonFunction.GetDefaultValue();

                switch (curTest.TestParam)
                {
                    case MESParam.MaxIL:
                        result = DoILTest(curTest, ref errMsg);
                        break;
                    case MESParam.PDL:
                        result = DoPDLTest(curTest, ref errMsg);
                        break;
                }

                bool isPass = true;
                MESTestInfo info = templateControl.UpdateTestData(curIndex, result, ref isPass);
                UpdateItem(info, productIndex, curIndex, selectMap);

                if (errMsg.Length > 0)
                {
                    uiVariable.Message = "测试失败：" + errMsg;
                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri("/image/" + "Fail.ico", UriKind.Absolute))); }));
                    //imgResult.Source = new BitmapImage(new Uri("/image/" + "Fail.ico", UriKind.Absolute));
                }
                else
                    this.Dispatcher.Invoke(new Action(() => { imgResult.Source = (new BitmapImage(new Uri("/image/" + "Pass.ico", UriKind.Absolute))); }));
                //imgResult.Source = new BitmapImage(new Uri("/image/" + "Pass.ico", UriKind.Absolute));

                return;
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        private void btnILRef_Click(object sender, RoutedEventArgs e)
        {
            if (selectItem == null)
            {
                return;
            }
            List<MESTestInfo> testInfo = templateControl.GetAllTestInfo();
            MESTestInfo ilTest = null;
            //选中行是否存在IL测试项
            int ilIndex = 0;
            foreach (int index in selectItem.ParamIndex)
            {
                if (testInfo[index].TestParam == MESParam.MaxIL)
                {
                    ilTest = testInfo[index];
                    ilIndex = index;
                    break;
                }
            }
            string errMsg = "";
            powermeterRealtimeBK.CancelAsync();
            ILRef(testInfo, selectItem.ProductIndex, ilIndex, ref errMsg);
            powermeterRealtimeBK.RunWorkerAsync();
            if (errMsg.Length > 0)
            {
                ErrorBox("IL归零出错：" + errMsg);
            }
        }

        private void btnILTest_Click(object sender, RoutedEventArgs e)
        {
            lock (onekeyObject)
            {
                uiVariable.IsEnable = false;
            }
            Thread oneKeyThread = new Thread(new ThreadStart(OnekeyThread));
            oneKeyThread.Start();
        }

        /// <summary>
        /// 一键测试线程函数
        /// </summary>
        private void OnekeyThread()
        {
            powermeterRealtimeBK.CancelAsync();
            int count = templateControl.GetAllTestInfo().Count;
            for (int i = 0; i < count; i++)
            {
                IndexMap selectMap = new IndexMap();
                selectMap.ParamIndex = new List<int>();
                if (i < count - 1)
                {
                    //调整到下一个测试项
                    selectMap.ParamIndex.Add(i + 1);
                }
                DoTest(templateControl.GetAllTestInfo(), 0, i, selectMap);
                Thread.Sleep(500);
            }
            powermeterRealtimeBK.RunWorkerAsync();
            lock (onekeyObject)
            {
                //将界面灰掉的电脑点亮
                uiVariable.IsEnable = true;
            }
        }

        /// <summary>
        /// IL测试
        /// </summary>
        /// <param name="ilTest">当前测试项信息</param>
        /// <param name="ilIndex">当前测试项index</param>
        /// <param name="errMsg">错误信息</param>
        private double DoILTest(MESTestInfo ilTest, ref string errMsg)
        {
            if (ilTest.ILRef == CommonFunction.GetDefaultValue() || ilTest.ILRef == CommonFunction.GetFormatDefaultValue())
            {
                errMsg = "选择行IL未归零";
                return CommonFunction.GetDefaultValue();
            }

            //切换光源盒
            if (!SetSwitch(ilTest, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //读取功率
            List<List<double>> rawdatas = null;

            if (powerCount == 4)
            {
                //读取四个功率计的值，判断连接的功率计是哪个
                List<double>[] powerArray = new List<double>[4];
                for (int i = 0; i < 4; i++)
                {
                    if (powers[i] != null)
                        powers[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (powerArray[i][0] > 0 && powerArray[i][0] < 20)
                        powers[i].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);
                }
            }
            else
            {
                powers[0].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);
            }

            //计算IL
            double il = algorithm.MaxIL(rawdatas[0].ToArray(), ilTest.ILRef, ref errMsg);
            if (errMsg.Length > 0)
            {
                return CommonFunction.GetDefaultValue();
            }
            return il;
        }

        /// <summary>
        /// PDL测试
        /// </summary>
        /// <param name="ilTest">当前测试项信息</param>
        /// <param name="ilIndex">当前测试项index</param>
        /// <param name="errMsg">错误信息</param>
        private double DoPDLTest(MESTestInfo ilTest, ref string errMsg)
        {
            if (ilTest.ILRef == CommonFunction.GetDefaultValue() || ilTest.ILRef == CommonFunction.GetFormatDefaultValue())
            {
                errMsg = "选择行IL未归零";
                return CommonFunction.GetDefaultValue();
            }

            //切换光源盒
            if (!SetSwitch(ilTest, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            uiVariable.Message = "请手摇偏振片！";

            //读取功率
            List<List<double>> rawdatas = null;

            if (powerCount == 4)
            {
                //读取四个功率计的值，判断连接的功率计是哪个
                List<double>[] powerArray = new List<double>[4];
                for (int i = 0; i < 4; i++)
                {
                    if (powers[i] != null)
                        powers[i].ReadPowerAvg(ref errMsg, out powerArray[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (powerArray[i][0] > 0 && powerArray[i][0] < 20)
                        powers[i].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);
                }
            }
            else
                powers[0].GetMultiPowers(ref errMsg, out rawdatas, 50, 200);

            uiVariable.Message = "";

            //计算PDL
            double result = demuxAlgorithm.PDL(rawdatas[0].ToArray(), ref errMsg);
            if (errMsg.Length > 0)
            {
                return CommonFunction.GetDefaultValue();
            }
            return result;
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
    }
}
