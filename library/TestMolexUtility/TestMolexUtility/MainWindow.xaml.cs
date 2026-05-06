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
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using System.ComponentModel;
using System.IO;
using MolexUtility;

namespace TestMolexUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 
        /// </summary>
        private DataGridView testParamDataGrid = null;

        private UIParamShow testParamShow = null;

        private MESControl templateControl = null;

        private MESTemplateType templateType = MESTemplateType.DC;

        private MESTestProcess testProcess = MESTestProcess.Adjust;

        private string userID = "11091";

        private string goldSample = "";

        private List<MESTestProcess> m_ComboList = new List<MESTestProcess>();
        //private DynamicLoadDll molexUtilityLoad = new DynamicLoadDll();
        public MainWindow()
        {
            InitializeComponent();

            testParamDataGrid = new DataGridView();
            testParamShow = new UIParamShow(testParamDataGrid);
            templateControl = new MESControl(testParamShow);

            this.ResizeMode = System.Windows.ResizeMode.CanMinimize;
            this.Left = 0;
            this.Top = 0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            WinFormForDataGridView.Child = testParamDataGrid;
            //测试模板读取配置
            UpdateTemplateTypeList();
            //测试工序读取配置
            UpdateTestProcessList();

            LblUserID.Content = userID;
            //molexUtilityLoad.LoadDll("..\\common\\MolexUtility.dll");
        }

        private void UpdateTemplateTypeList()
        {
            IniParser templateSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetTemplateConfig());
            //测试模板读取配置
            string strCurTemplateType = templateSet.readStringData(CommonFunction.GetTemplateTypeSection(), CommonFunction.GetCurrentTemplateKey(), "");
            for (int tmplet = Convert.ToInt32(MESTemplateType.GFQC); tmplet < Convert.ToInt32(MESTemplateType.DEVICE) + 1; tmplet++)
            {
                MESTemplateType t = (MESTemplateType)tmplet;
                int nIdx = ComboTemplateType.Items.Add(t.GetMESSaveDataKeywords());
                if (strCurTemplateType == t.GetMESSaveDataKeywords())
                {
                    ComboTemplateType.SelectedIndex = nIdx;
                    templateType = t;
                }
            }
        }

        private void UpdateTestProcessList()
        {
            IniParser templateSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetTemplateConfig());
            //测试工序读取配置
            string strCurProcess = templateSet.readStringData(CommonFunction.GetProcessSection(), CommonFunction.GetCurrentProcessKey());
            for (int nProcess = Convert.ToInt32(MESTestProcess.PreAdjust); nProcess <= Convert.ToInt32(MESTestProcess.Test9); nProcess++)
            {
                string strUserKey = string.Format("{0}{1}", CommonFunction.GetProcessUserKey(), nProcess);
                string strAMTSKey = string.Format("{0}{1}", CommonFunction.GetProcessAMTSKey(), nProcess);
                //m_ComboList
                string strUserName = templateSet.readStringData(CommonFunction.GetProcessSection(), strUserKey);
                string strAMTSName = templateSet.readStringData(CommonFunction.GetProcessSection(), strAMTSKey);
                if (strUserName == "")
                    continue;
                int nidx = ComboTestProcess.Items.Add(strUserName);
                for (int n = Convert.ToInt32(MESTestProcess.PreAdjust); n < Convert.ToInt32(MESTestProcess.Test9); n++)
                {
                    MESTestProcess pe = (MESTestProcess)n;
                    if (pe.GetAdditional() == strAMTSName)
                    {
                        m_ComboList.Add(pe);
                        if (strCurProcess == strAMTSName)
                        {
                            testProcess = (MESTestProcess)n;
                            ComboTestProcess.SelectedIndex = nidx;
                        }
                        break;
                    }
                }

            }
        }

        

        private void BtnSaveToAMTS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnUnLockPWM_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnILRef_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRLRef_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnILTest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRLTest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPWMReset_Click(object sender, RoutedEventArgs e)
        {

        }


        private void ComboTestProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboTemplateType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void WinFormForDataGridView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        private void BtnOpenTemplate_Click(object sender, RoutedEventArgs e)
        {
            

            if (templateControl.GetHasTested())
            {
                MessageBoxResult msgRes = System.Windows.MessageBox.Show("有测试数据未保存，是否打开新条码？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (msgRes == MessageBoxResult.No)
                {
                    MESProductInfo proInfo = templateControl.GetProductInfo();
                    TxtBoxSN.Text = proInfo.SN;
                    return;
                }
            }
            templateControl.ClearAllData();


            string strSN = TxtBoxSN.Text;
            if (strSN.Length == 0)
            {
                System.Windows.MessageBox.Show("请输入产品号！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string errMsg = "";
            MESTestType testType = MESTestType.Normal;
            if (true == CheckRetest.IsChecked)
                testType = MESTestType.Retest;
            bool bShowData = (CheckShowData.IsChecked == true);
            IniParser xmlSet = new IniParser(System.Environment.CurrentDirectory + CommonFunction.GetXmlSetPath());
            string strAddr = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            if (!templateControl.OpenTemplate(strAddr, templateType, strSN, testProcess, testType, userID, "", false, bShowData, ref errMsg))
            {
                System.Windows.MessageBox.Show(errMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtBoxSN.Text = "";
                TxtBoxSN.Focus();
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
                    TxtBoxSN.Text = "";
                    TxtBoxSN.Focus();
                    return;
                }
            }
            //提取出来，统一显示
            /*List<TemplateListBoxShow> proDataShow = new List<TemplateListBoxShow>();
            proDataShow.Add(new TemplateListBoxShow("SpecNO:", curProInfo.SpecNO));
            proDataShow.Add(new TemplateListBoxShow("TemplateID:", curProInfo.TemplateID));
            proDataShow.Add(new TemplateListBoxShow("PN:", curProInfo.ProductPN));
            proDataShow.Add(new TemplateListBoxShow("SO:", curProInfo.SO));
            ProductInfoList.ItemsSource = proDataShow;*/

            if (!templateControl.OpenTemplate(strAddr, templateType, strSN, testProcess, testType, userID, "", true, bShowData, ref errMsg))
            {
                MessageBoxResult msgRes = System.Windows.MessageBox.Show("该产品已经测试过，要重新测试吗？", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //读取归零数据
            //templateControl.ReadRefData(System.Environment.CurrentDirectory + c_RefDataFile);


            //UpdateResIcon();

            ComboTestProcess.IsEnabled = false;
            ComboTemplateType.IsEnabled = false;
            //testParamShow.InitView(templateControl.GetAllTestInfo());
            testParamDataGrid.Focus();

        }
    }
    

    public class TemplateListBoxShow
    {
        public string ShowName { get; set; }
        public string ShowContent { get; set; }
        public TemplateListBoxShow()
        {
            ShowName = "";
            ShowContent = "";
        }
        public TemplateListBoxShow(string strName, string strContent)
        {
            ShowName = strName;
            ShowContent = strContent;
        }
    }
}
