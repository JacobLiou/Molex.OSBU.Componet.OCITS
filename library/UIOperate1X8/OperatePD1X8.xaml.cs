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



namespace UIOperatePD1X8
{
    /// <summary>
    /// Interaction logic for PD1X8Operate.xaml
    /// </summary>
    
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIOperate1X8")]
    public partial class OperatePD1X8: UserControl
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
        /// 所有产品测试信息
        /// </summary>
        private List<FusionControl> allProductControl; 

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
        
        /// <summary>
        /// 界面相关变量
        /// </summary>
        public UIVariable uiVariable = new UIVariable();

        /// <summary>
        /// 使用光源类型
        /// </summary>
        private Devices opticalSourceType = Devices.OpiticalSourceBank;

        /// <summary>
        /// 是否需要停止一键测试
        /// </summary>
        private bool isStopOnekeyTest = false;

        /// <summary>
        /// 一键测试lock对象
        /// </summary>
        private object onekeyObject = new object();

        //public string TemplateID { get; set; }
        public OperatePD1X8()
        {
            InitializeComponent();
            allProductControl = new List<FusionControl>();
            uiVariable.TemplateID = "";
            uiVariable.IsEnable = true;


            lblProductModel.DataContext = uiVariable;
            lblTemperatureCount.DataContext = uiVariable;
            txtBoxSN.DataContext = uiVariable;
            lblUnTestCount.DataContext = uiVariable;
            lblTestStatus.DataContext = uiVariable;
            lblPWMRestTime.DataContext = uiVariable;
            btnOpenTemplate.DataContext = uiVariable;
            btnSaveToAMTS.DataContext = uiVariable;
            btnRecoverData.DataContext = uiVariable;
            btnDeleteProduct.DataContext = uiVariable;
            btnILRef.DataContext = uiVariable;
            btnRLRef.DataContext = uiVariable;
            btnOnekeyRef.DataContext = uiVariable;
            btnFindPower.DataContext = uiVariable;
            btnILTest.DataContext = uiVariable;
            btUnqualifiedTest.DataContext = uiVariable;
            btnSingleTest.DataContext = uiVariable;
            btnOnekeyTest.DataContext = uiVariable;
            btnPWMReset.DataContext = uiVariable;
            btnOnekeyTest.DataContext = uiVariable;
           
            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");


            //CommandManager.RegisterClassCommandBinding(typeof(CustomCommand), new CommandBinding(CustomCommand.ILTest, ILTestCommand_Executed, ILTestCommand_CanExcute));
        }

        private void ILTestCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
        }

        private void ILTestCommand_CanExcute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (allProductControl.Count > 0)
                e.CanExecute = true;
            else
                e.CanExecute = false;
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

        private void btnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            selectItem = null;
            if (allProductControl.Count >= 8)
            {
                WarningBox("已达到最大的产品测试个数，不能再添加！！");
                return;
            }

            if (uiVariable.SN.Length==0)
            {
                WarningBox("请输入产品号！！");
                return;
            }
            
            if (SnIsExist(uiVariable.SN))
            {
                WarningBox("该SN已存在测试列表中！！");
                return;
            }

            
            
            //早7点、晚7点清理功率文件 

            //

            
            FusionControl control = new FusionControl();
            string errMsg = "";
            if (control.OpenTemplate(uiVariable.SN, MESTestProcess.Test.ToString(), "11091", "", false, Environment.MachineName, new List<string>(), out var _, out errMsg) != "")
            {
                if(allProductControl.Count==0)
                {
                    uiVariable.TemplateID = (control.GetProductInfo()).TemplateID;
                }
                else
                {
                    //判断是否是同一类型条码
                    if (uiVariable.TemplateID != (control.GetProductInfo()).TemplateID)
                    {
                        WarningBox("请输入相同类型的条码！！");
                        return;
                    }
                }
                //读取归零数据处理
                if (checkGetRefData.IsChecked.Value)
                {
                    if (!control.ReadRefData(System.Environment.CurrentDirectory + refDataFile, ref errMsg))
                    {
                        ErrorBox(errMsg);
                        return;
                    }
                }
                //

                allProductControl.Insert(allProductControl.Count, control);
                uiVariable.SN = "";
                uiVariable.UnTestCount = allProductControl.Count.ToString();
                uiVariable.TestStatus = "模板打开成功！";

                //更新测试信息
                if (EventAggregator != null)
                {                   
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(allProductControl);
                }
            }
            else
            {
                ErrorBox(errMsg);
                return;
            }
        }

        /// <summary>
        /// 查看该SN号是否已经在打开的模板中
        /// </summary>
        /// <param name="sn">需要判断的产品sn号</param>
        /// <returns></returns>
        private bool SnIsExist(string sn)
        {
            foreach(FusionControl mes in allProductControl)
            {
                if (sn == mes.ProductSN)
                    return true;
            }
            return false;
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
            SelectedItemChangeRegister();
        }

        private void SetStatus(string status)
        {
            this.Dispatcher.BeginInvoke(new Action(()=>{ uiVariable.TestStatus = status; }));
        }

        private void BtnILTest_Click(object sender, RoutedEventArgs e)
        {
            uiVariable.TestStatus = "开始IL测试";
            if (selectItem == null)
            {
                uiVariable.TestStatus = "测试失败：未选择行";
                return;
            }
            List<MESTestInfo> testInfo = allProductControl[selectItem.ProductIndex].GetAllTestInfo();
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

            if (ilTest == null)
            {
                uiVariable.TestStatus = "测试失败：选中行没有IL";
                return;
            }
           
            DoTest(allProductControl[selectItem.ProductIndex].GetAllTestInfo(), selectItem.ProductIndex, ilIndex);

            

            //老的软件还有测试PDISO、WDR、WDL、WDRM

            //判断lps300，何作用
            //

            //MessageBox.Show("IL Test");
            /*CurveUpdateDetail detail = new CurveUpdateDetail();
            if (curveTest == 0)
            {
                detail.XAixsTitle = "X";
                detail.YAxisTitle = "Y";
                detail.Type = CurveType.Line;
                detail.XAixsBegin = 10;
                detail.XAxisEnd = 20;
                detail.UpdateType = CurveUpdate.Init;
            }
            else if (curveTest % 40 == 0)
            {
                detail.SeriesName = "test1";
                detail.UpdateType = CurveUpdate.FirstPoint;
                detail.XAxisStep = 1;
                detail.YAxisValue = curveTest;
            }
            else
            {
                detail.SeriesName = "test1";
                detail.UpdateType = CurveUpdate.AddPoint;
                detail.XAxisStep = 1;
                detail.YAxisValue = curveTest;
            }
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventCurveUpdate>().Publish(detail);
            }
            curveTest++;*/
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

            //设置波长
            if (!SetWavelength(ilTest.WLLeft, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //切换光源盒
            if (!SetSwitch(ilTest, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //读取功率
            List<double> rawdatas = null;
            GetPower(ref rawdatas, ref errMsg);
            if (errMsg.Length > 0)
                return CommonFunction.GetDefaultValue();

            //计算IL
            double il = algorithm.MaxIL(rawdatas.ToArray(), ilTest.ILRef, ref errMsg);
            if(errMsg.Length>0)
            {
                return CommonFunction.GetDefaultValue();
            }
            return il;    
        }

        /// <summary>
        /// PDISO测试
        /// </summary>
        /// <param name="curInfos">当前产品所有测试信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        private double DoPDISOTest(List<MESTestInfo> curInfos,MESTestInfo curTest,ref string errMsg)
        {
            return 0;
        }

        /// <summary>
        /// WDL测试
        /// </summary>
        /// <param name="curInfos">当前产品所有测试信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>WDL结果</returns>
        private double DoWDLTest(List<MESTestInfo> curInfos, MESTestInfo curTest, ref string errMsg)
        {
            try
            {
                List<double> ils = new List<double>();
                foreach (MESTestInfo info in curInfos)
                {
                    //找出所有符合该WDL计算的IL值,相同端口相同温度
                    if(info.TestParam==MESParam.MaxIL&&info.PortNameForAMTS==curTest.PortNameForAMTS && info.Temperature == curTest.Temperature)
                    {
                        if(curTest.WLLeft<info.WLLeft&&curTest.WLRight>info.WLLeft)
                        {
                            //如果为默认值，未测试，则继续
                            if (CommonFunction.IsDefault(info.TestedValue))
                                continue;
                            ils.Add(info.TestedValue);
                        }
                    }
                }
                return algorithm.WDL(ils.ToArray(), ref errMsg);
            }
            catch(Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// WDR或者WDRM测试
        /// </summary>
        /// <param name="curInfos">当前产品所有测试信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>WDR或者WDRM结果</returns>
        private double DoWDROrWDRMTest(List<MESTestInfo> curInfos, MESTestInfo curTest, ref string errMsg)
        {
            try
            {
                List<double> resins = new List<double>();
                foreach (MESTestInfo info in curInfos)
                {
                    //找出所有符合该WDR计算的Resin值,相同端口相同温度
                    if (info.TestParam == MESParam.RESIN && info.PortNameForAMTS == curTest.PortNameForAMTS&&info.Temperature==curTest.Temperature)
                    {
                        if (curTest.WLLeft < info.WLLeft && curTest.WLRight > info.WLLeft)
                        {
                            //如果为默认值，未测试，则继续
                            if (CommonFunction.IsDefault(info.TestedValue))
                                continue;
                            resins.Add(info.TestedValue);
                        }
                    }
                }
                if (curTest.TestParam == MESParam.WDR)
                    return algorithm.WDR(resins.ToArray(), ref errMsg);
                else
                    return algorithm.WDRM(resins.ToArray(), ref errMsg);
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// TDR测试
        /// </summary>
        /// <param name="curInfos">当前产品所有测试信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>TDR结果</returns>
        private double DoTDRTest(List<MESTestInfo> curInfos, MESTestInfo curTest, ref string errMsg)
        {
            try
            {
                List<double> resins = new List<double>();
                foreach (MESTestInfo info in curInfos)
                {
                    //找出所有符合该TDR计算的Resin值
                    if (info.TestParam == MESParam.RESIN && info.PortNameForAMTS == curTest.PortNameForAMTS&&info.WLLeft==curTest.WLLeft)
                    {
                        if (curTest.TestParam==MESParam.TDRL)
                        {
                            if (info.Temperature > 30)
                                continue;
                        }
                        else if(curTest.TestParam==MESParam.TDRH)
                        {
                            if (info.Temperature < 10)
                                continue;
                        }
                        //如果为默认值，未测试，则继续
                        if (CommonFunction.IsDefault(info.TestedValue))
                            continue;
                        resins.Add(info.TestedValue);
                    }
                }
                if (curTest.TestParam == MESParam.TDRM)
                    return algorithm.TDRM(resins.ToArray(), ref errMsg);
                else
                    return algorithm.TDR(resins.ToArray(), ref errMsg);
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        /// <summary>
        /// TDL测试
        /// </summary>
        /// <param name="curInfos">当前产品所有测试信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>TDL结果</returns>
        private double DoTDLTest(List<MESTestInfo> curInfos, MESTestInfo curTest, ref string errMsg)
        {
            try
            {
                List<double> ils = new List<double>();
                foreach (MESTestInfo info in curInfos)
                {
                    //找出所有符合该TDR计算的Resin值
                    if (info.TestParam == MESParam.MaxIL && info.PortNameForAMTS == curTest.PortNameForAMTS && info.WLLeft == curTest.WLLeft)
                    {
                        //如果为默认值，未测试，则继续
                        if (CommonFunction.IsDefault(info.TestedValue))
                            continue;
                        ils.Add(info.TestedValue);
                    }
                }
                
                return algorithm.TDL(ils.ToArray(), ref errMsg);
                
            }
            catch (Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return CommonFunction.GetDefaultValue();
            }
        }

        private double DoDKTest()
        {
            return 0;
        }

        /// <summary>
        /// Resin、Resout测试
        /// </summary>
        /// <param name="curTest">当前测试信息</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>返回结果</returns>
        private double DoResTest(MESTestInfo curTest, ref string errMsg)
        {
            if(curTest.TestParam!=MESParam.RESIN && curTest.TestParam != MESParam.RESOUT)
            {
                errMsg = "当前行没有Resin、Resout测试项";
                return CommonFunction.GetDefaultValue();
            }
            if(CommonFunction.IsDefault(curTest.InPower))
            {
                errMsg = "未找功率！";
                return CommonFunction.GetDefaultValue();
            }
            //设置波长
            if (!SetWavelength(curTest.WLLeft, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //切换光源盒
            if (!SetSwitch(curTest, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //读取功率
            ICurrent current=null;
            if(DeviceControl.GetCurrentByIndex(0,ref current,ref errMsg)!=0)
            {
                return CommonFunction.GetDefaultValue();
            }
            double value = 0;
            if (current.GetCurrent("0.00001", -5, ref value, ref errMsg) != 0)
                return CommonFunction.GetDefaultValue();
            //用什么单位来计算resin

            return algorithm.Res(value, curTest.InPower, ref errMsg);
        }

        /// <summary>
        /// RL测试
        /// </summary>
        /// <param name="curTest">当前测试信息</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>返回结果</returns>
        private double DoRLTest(MESTestInfo curTest, ref string errMsg)
        {
            if(curTest.TestParam!=MESParam.ReturnLoss)
            {
                errMsg = "该行无RL测试项！";
                return CommonFunction.GetDefaultValue();
            }
            if(CommonFunction.IsDefault(curTest.ILRef)||CommonFunction.IsDefault(curTest.RLRef))
            {
                errMsg = "RL归零数据不完整！";
                return CommonFunction.GetDefaultValue();
            }

            //设置波长
            if (!SetWavelength(curTest.WLLeft, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //切换光源盒
            if (!SetSwitch(curTest, ref errMsg))
            {
                return CommonFunction.GetDefaultValue();
            }

            //读取功率
            List<double> rawdatas = null;
            GetPower(ref rawdatas, ref errMsg);
            if (errMsg.Length > 0)
                return CommonFunction.GetDefaultValue();

            //计算IL
            double rl = algorithm.RL(rawdatas.ToArray(), curTest.ILRef,curTest.RLRef, ref errMsg);
            if (errMsg.Length > 0)
            {
                return CommonFunction.GetDefaultValue();
            }
            return rl;
        }

        /// <summary>
        /// 保存数据按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = "";
            foreach(FusionControl product in allProductControl.ToList())
            {
                string savePath = Environment.CurrentDirectory + "\\data\\" + product.ProductSN + ".xml";
                if(!product.UploadTestData(savePath, out errMsg))
                {
                    ErrorBox(errMsg);
                    continue;
                }
                //清除保存成功的数据
                allProductControl.Remove(product);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(allProductControl);
                }
            }
        }

        /// <summary>
        /// 删除产品按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (selectItem == null)
                return;
            if(selectItem.ProductIndex<allProductControl.Count)
            {
                allProductControl.RemoveAt(selectItem.ProductIndex);
                //更新测试信息
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<EventTemplateUpdate>().Publish(allProductControl);
                }
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
        /// 设置波长 
        /// </summary>
        /// <param name="wavelength">需要设置的波长</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true--正确，false--出错</returns>
        private bool SetWavelength(double wavelength,ref string errMsg)
        {
            return true;
            IOpticalSource opticalSource = null;
            if (0 != DeviceControl.GetOpticalSourceByWaveAndType(1, ref opticalSource, ref errMsg))
            {
                //ErrorBox(errMsg);
                errMsg = "设置波长出错：" + errMsg;
                return false;
            }

            if (opticalSource.SetWavelength(wavelength, ref errMsg) != 0)
            {
                errMsg = "设置波长出错：" + errMsg;
                return false;
            }
            opticalSourceType = opticalSource.GetDeviceType();
            return true;
        }

        /// <summary>
        /// 切换光源盒
        /// </summary>
        /// <param name="testInfo">当前测试项信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true--正确，false--出错</returns>
        private bool SetSwitch(MESTestInfo testInfo,ref string errMsg)
        {
            return true;
            IOpticalSwitch opticalSwitch = null;
            if (DeviceControl.GetSwitchByType("", ref opticalSwitch, ref errMsg) != 0)
            {
                errMsg = "切换光源盒出错：" + errMsg;
                return false;
            }
            //光源类型：产品序号:波长:端口:参数
            string flag = opticalSourceType.GetAdditional() + ":" + "1" + ":" + testInfo.WLLeft.ToString() + ":" + testInfo.PortNameForUser + ":" + testInfo.TestParam.GetMESTemplateKeywords();
            if (opticalSwitch.SetSwitch(flag, ref errMsg) != 0)
            {
                errMsg = "切换光源盒出错：" + errMsg;
                return false;
            }
            return true;
        }

        private void btnSingleTest_Click(object sender, RoutedEventArgs e)
        {
            if (selectItem == null)
            {
                uiVariable.TestStatus = "未选择行";
                return;
            }
            List<MESTestInfo> testInfos = allProductControl[selectItem.ProductIndex].GetAllTestInfo();

            //int curIndex = 0;
            foreach (int index in selectItem.ParamIndex)
            {          
                DoTest(testInfos,selectItem.ProductIndex, index);
            }
        }


        /// <summary>
        /// IL归零按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnILRef_Click(object sender, RoutedEventArgs e)
        {
            uiVariable.TestStatus = "开始IL归零";
            if (selectItem == null)
            {
                uiVariable.TestStatus = "归零失败：未选择行";
                return;
            }
            List<MESTestInfo> testInfo = allProductControl[selectItem.ProductIndex].GetAllTestInfo();
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
            ILRef(testInfo, selectItem.ProductIndex, ilIndex, ref errMsg);
            if (errMsg.Length > 0)
            {
                ErrorBox("IL归零出错：" + errMsg);
            }


        }

        /// <summary>
        /// 读取功率最大最小值，取五个点，点和点之间间隔30ms
        /// </summary>
        /// <param name="powerMax">返回功率最大值</param>
        /// <param name="powerMin">返回功率最小值</param>
        /// <param name="errMsg">错误信息</param>
        private void GetPower(ref List<double> rawdatas,ref string errMsg)
        {
            if(rawdatas==null)
            {
                rawdatas = new List<double>();
            }
            rawdatas.Add(-0.1);
            rawdatas.Add(-0.2);
            rawdatas.Add(-0.3);
            rawdatas.Add(-0.3);
            //读取功率
            /*IPowermeter powermeter = null;
            int channel = 0;
            if (DeviceControl.GetPowermeterByIndex(1, ref channel, ref powermeter, ref errMsg) != 0)
            {
                return;
            }

            List<List<double>> powers = null;
            if (powermeter.GetMultiPowers(ref errMsg, out powers, 30, 5, false, channel.ToString()) != 0)
            {
                return;
            }
            rawdatas = powers[0];*/

        }

        /// <summary>
        /// IL归零
        /// </summary>
        /// <param name="testInfo">所有测试项信息</param>
        /// <param name="rlTest">当前归零行</param>
        /// <param name="rlIndex">归零行的序列号</param>
        private void ILRef(List<MESTestInfo> testInfo, int prodeuctIndex, int ilIndex,ref string errMsg)
        {
            MESTestInfo ilTest = testInfo[ilIndex];
            errMsg = "";
            if (ilTest == null)
            {
                errMsg = "选中行没有IL";
                return;
            }
            //设置波长
            if (!SetWavelength(ilTest.WLLeft, ref errMsg))
            {
                return;
            }

            //切换光源盒
            if (!SetSwitch(ilTest, ref errMsg))
            {
                return;
            }

            //读取功率
            double ilMin ;
            double ilMax ;
            List<double> rawdatas = null;
            GetPower(ref rawdatas, ref errMsg);
            if (errMsg.Length > 0)
                return;

            CommonFunction.GetMaxMin(rawdatas.ToArray(), out ilMax, out ilMin);

            //相同波长，相同端口的归零值一致
            for (int i = 0; i < testInfo.Count(); i++)
            {
                if (testInfo[i].WLLeft == ilTest.WLLeft && testInfo[i].WLRight == ilTest.WLRight && testInfo[i].PortNameForAMTS == ilTest.PortNameForAMTS)
                {
                    MESTestInfo info = allProductControl[prodeuctIndex].UpdateILRefData(i, ilMin);
                    UpdateItem(info, prodeuctIndex, i);
                }
            }
        }


        private void UpdateItem(MESTestInfo info,int prodoctIndex,int paramIndex,IndexMap nextSelect=null)
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
        /// RL归零按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRLRef_Click(object sender, RoutedEventArgs e)
        {
            uiVariable.TestStatus = "开始RL归零";
            if (selectItem == null)
            {
                uiVariable.TestStatus = "归零失败：未选择行";
                return;
            }
            List<MESTestInfo> testInfo = allProductControl[selectItem.ProductIndex].GetAllTestInfo();
            MESTestInfo rlTest = null;
            //选中行是否存在RL测试项
            int rlIndex = 0;
            foreach (int index in selectItem.ParamIndex)
            {
                if (testInfo[index].TestParam == MESParam.ReturnLoss)
                {
                    rlTest = testInfo[index];
                    rlIndex = index;
                    break;
                }
            }
            string errMsg = "";
            RLRef(testInfo, selectItem.ProductIndex, rlIndex,ref errMsg);
            if(errMsg.Length>0)
            {
                ErrorBox("RL归零出错：" + errMsg);
            }
        }

        /// <summary>
        /// RL归零
        /// </summary>
        /// <param name="testInfo">所有测试项信息</param>
        /// <param name="rlTest">当前归零行</param>
        /// <param name="rlIndex">归零行的序列号</param>
        private void RLRef(List<MESTestInfo> testInfo, int productIndex,int rlIndex, ref string errMsg)
        {
            MESTestInfo rlTest = testInfo[rlIndex];
            errMsg = "";
            if (rlTest == null)
            {
                errMsg = "选中行没有RL";
                return;
            }

            
            //设置波长
            if (!SetWavelength(rlTest.WLLeft, ref errMsg))
            {                
                return;
            }

            //切换光源盒
            if (!SetSwitch(rlTest, ref errMsg))
            {          
                return;
            }

            //读取功率
            double ilMin;
            double ilMax;
            List<double> rawdatas = null;
            GetPower(ref rawdatas, ref errMsg);
            if (errMsg.Length > 0)
                return;
            CommonFunction.GetMaxMin(rawdatas.ToArray(), out ilMax, out ilMin);

            //相同波长，相同端口的归零值一致
            for (int i = 0; i < testInfo.Count(); i++)
            {
                if (testInfo[i].TestParam == MESParam.ReturnLoss && testInfo[i].WLLeft == rlTest.WLLeft && testInfo[i].PortNameForAMTS == rlTest.PortNameForAMTS)
                {
                    MESTestInfo info = allProductControl[productIndex].UpdateRLRefData(i, ilMax);
                    UpdateItem(info, productIndex, i);
                }
            }
        }

        /// <summary>
        /// 一键归零按钮响应函数,先清除所有归零数据，再全部重新归零
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOnekeyRef_Click(object sender, RoutedEventArgs e)
        {
            //清除所有归零数据
            foreach (FusionControl product in allProductControl)
            {
                int total = product.GetAllTestInfo().Count;
                for (int i=0;i< total;i++)
                {
                    product.UpdateILRefData(i, CommonFunction.GetDefaultValue());
                    product.UpdateRLRefData(i, CommonFunction.GetDefaultValue());
                }
            }

            Thread refThread = new Thread(new ThreadStart(OnekeyRefThread));
            refThread.Start();
        }

        private void OnekeyRefThread()
        {
            //一键归零是否需要放在线程中完成
            //全部重新归零
            int prodeuctIndex = 0;
            foreach (FusionControl product in allProductControl)
            {
                List<MESTestInfo> testInfos = product.GetAllTestInfo();
                int total = testInfos.Count;
                string errMsg = "";
                for (int i = 0; i < total; i++)
                {
                    if (testInfos[i].TestParam == MESParam.MaxIL)
                    {
                        if (testInfos[i].ILRef == CommonFunction.GetDefaultValue()
                            || testInfos[i].ILRef == CommonFunction.GetFormatDefaultValue())
                        {
                            ILRef(testInfos, prodeuctIndex, i, ref errMsg);
                            if (errMsg.Length > 0)
                            {
                                //ErrorBox(errMsg);
                                SetStatus("归零失败：" + errMsg);
                            }
                            testInfos = product.GetAllTestInfo();
                        }
                    }
                    else if (testInfos[i].TestParam == MESParam.ReturnLoss)
                    {
                        if (testInfos[i].RLRef == CommonFunction.GetDefaultValue()
                            || testInfos[i].RLRef == CommonFunction.GetFormatDefaultValue())
                        {
                            RLRef(testInfos, prodeuctIndex, i, ref errMsg);
                            if (errMsg.Length > 0)
                            {
                                ErrorBox(errMsg);
                            }
                            testInfos = product.GetAllTestInfo();
                        }
                    }
                }
                prodeuctIndex++;
            }
        }

        /// <summary>
        /// 找功率按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFindPower_Click(object sender, RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// 不合格项测试按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btUnqualifiedTest_Click(object sender, RoutedEventArgs e)
        {
            if (selectItem == null)
            {
                uiVariable.TestStatus = "未选择行";
                return;
            }
            List<MESTestInfo> testInfos = allProductControl[selectItem.ProductIndex].GetAllTestInfo();

            //选中行是否存在IL测试项            
            int curIndex = 0;
            foreach (int index in selectItem.ParamIndex)
            {
                if (!testInfos[index].Pass)
                {
                    DoTest(testInfos, selectItem.ProductIndex, curIndex);
                }
            }
        }


        /// <summary>
        /// 根据测试项，调不同的处理函数
        /// </summary>
        /// <param name="testInfos">当前测试产品信息</param>
        /// <param name="curTest">当前测试项</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>测试结果</returns>
        private void DoTest(List<MESTestInfo> testInfos,int productIndex,int curIndex, IndexMap selectMap=null)
        {
            string errMsg = "";
            try
            {
                if(testInfos==null||testInfos.Count<curIndex)
                {
                    errMsg = "测试信息出错";
                    return;
                }
                MESTestInfo curTest = testInfos[curIndex];
                double result = CommonFunction.GetDefaultValue();
                switch(curTest.TestParam)
                {
                    case MESParam.TDL:
                        result=DoTDLTest(testInfos, curTest, ref errMsg);
                        break;
                    case MESParam.TDRL:
                    case MESParam.TDRH:
                    case MESParam.TDRM:
                    case MESParam.TDR:
                        result = DoTDRTest(testInfos, curTest, ref errMsg);
                        break;
                    case MESParam.WDL:
                        result = DoWDLTest(testInfos, curTest, ref errMsg);
                        break;
                    case MESParam.WDR:
                    case MESParam.WDRM:
                        result = DoWDROrWDRMTest(testInfos, curTest, ref errMsg);
                        break;
                    case MESParam.ReturnLoss:
                        result = DoRLTest(curTest, ref errMsg);
                        break;
                    case MESParam.RESOUT:
                    case MESParam.RESIN:
                        result = DoResTest(curTest, ref errMsg);
                        break;
                    case MESParam.DK:
                        result = DoDKTest();
                        break;
                    case MESParam.MaxIL:
                        result = DoILTest(curTest, ref errMsg);
                        break;
                    case MESParam.PDISO:
                        result = DoPDISOTest(testInfos, curTest, ref errMsg);
                        break;
         
                }
                //是否要增加多线程处理，后续解决 
                if (errMsg.Length > 0)
                {
                    //ErrorBox(errMsg);
                    SetStatus("测试失败：" + errMsg);
                    //uiVariable.TestStatus = "测试失败：" + errMsg;
                }
                bool isPass = true;

                MESTestInfo info = allProductControl[productIndex].UpdateTestData(curIndex, result, ref isPass);
                UpdateItem(info, productIndex, curIndex, selectMap);
                
               
                //通知界面更新合格结果

                //
                return;
            }
            catch(Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return;
            }
        }

        /// <summary>
        /// 一键按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOnekeyTest_Click(object sender, RoutedEventArgs e)
        {
            lock (onekeyObject)
            {
                isStopOnekeyTest = false;
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
            int productIndex = -1;
            foreach (FusionControl control in allProductControl)
            {
                productIndex++;
                int count = control.GetAllTestInfo().Count;
                for (int i = 0; i < count; i++)
                {
                    //是否按下停止测试按钮
                    bool isExit = false;
                    lock(onekeyObject)
                    {
                        isExit = isStopOnekeyTest;
                    }
                    if (isExit)
                        return;
                    IndexMap selectMap = new IndexMap();
                    selectMap.ParamIndex = new List<int>();
                    //当前产品测试完，且下一产品存在时，则跳转到下一个产品
                    if(i==count -1&& (productIndex+1)<allProductControl.Count)
                    {
                        selectMap.ParamIndex.Add(0);
                        selectMap.ProductIndex = productIndex+1;
                    } 
                    else if(i<count -1)
                    {
                        //调整到下一个测试项
                        selectMap.ParamIndex.Add(i + 1);
                        selectMap.ProductIndex = productIndex;
                    }
                    DoTest(control.GetAllTestInfo(), productIndex, i,selectMap);
                    Thread.Sleep(1000);
                }
            }
            lock (onekeyObject)
            {
                //将界面灰掉的电脑点亮
                uiVariable.IsEnable = true;
            }
        }

        private void btnStopTest_Click(object sender, RoutedEventArgs e)
        {
            lock (onekeyObject)
            {
                isStopOnekeyTest = true;
                //将界面灰掉的电脑点亮
                uiVariable.IsEnable = true;
            }
            
        }

    }

    public class UIVariable:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 与界面产品类型绑定
        /// </summary>
        private string templateID;
        public string TemplateID
        {
            get
            {
                return templateID;
            }
            set
            {
                templateID = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TemplateID"));
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

        /// <summary>
        /// 与界面烤温时间倒计时绑定
        /// </summary>
        private string heatTime;
        public string HeatTime
        {
            get
            {
                return heatTime;
            }
            set
            {
                heatTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeatTime"));
            }

        }

        /// <summary>
        /// 与界面功率计复位时间倒计时绑定
        /// </summary>
        private string powermeterResetTime;
        public string PowermeterResetTime
        {
            get
            {
                return powermeterResetTime;
            }
            set
            {
                powermeterResetTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PowermeterResetTime"));
            }
        }

        /// <summary>
        /// 未测试个数
        /// </summary>
        private string unTestCount;
        public string UnTestCount
        {
            get
            {
                return unTestCount;
            }
            set
            {
                unTestCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnTestCount"));
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
