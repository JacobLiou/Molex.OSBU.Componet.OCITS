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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MolexUtility;
using System.Xml;

namespace OCITSAutoUpdate
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        string localPath = @"C:\Users\Public\software\OCITS";
        string serverPath = @"\\ZH-SOFT-SRV.OPLINK.COM.CN\public\passive\OCITS";


        string selectedLine = "";
        string softwareName = "";
        SingleStationConfig selectStation = new SingleStationConfig();
        string id = "1";
        string processArgs = "";

        /// <summary>
        /// 所有table页
        /// </summary>
        public static ObservableCollection<TabItem> tabItems = new ObservableCollection<TabItem>();

        /// <summary>
        /// 所有工位类型信息
        /// </summary>
        private List<StationShowConfig> allStations = new List<StationShowConfig>();

        SharedTool tool = null;
        public Login()
        {
            InitializeComponent();

            //用指定的账号访问soft服务器
            tool = new SharedTool("autotest", "China@123", "oplink");

            string errMsg = "";
            //增加工位类型文件解析
            string stationPath = serverPath + "\\OCITS\\CommonConfig\\set\\stations.xml";
            if (!System.IO.File.Exists(stationPath))
                stationPath = localPath + "\\CommonConfig\\set\\stations.xml";
            StationXMLParser.GetAllStations(stationPath, ref allStations, ref errMsg);
            
            //根据设备分类，界面根据设备分类进行分页显示
            bool isFind = false;
            for (int i = 0; i < allStations.Count; i++)
            {
                TabItem subItem = new TabItem();
                subItem.Header = allStations[i].ProdoctLine;
                subItem.Style = (Style)Application.Current.FindResource("TabItemStyle");
                subItem.Height = 38;
                subItem.Cursor = Cursors.Hand;
                Frame tabFrame = new Frame();
                tabFrame.Content = new SameProductLine(allStations[i]);
                subItem.Content = tabFrame;
                tabItems.Add(subItem);
            }
            tabStations.ItemsSource = tabItems;
            tabStations.SelectionChanged += TabStations_SelectionChanged;
            for (int j = 0; j < tabStations.Items.Count; j++)
            {
                TabItem subItem = tabStations.Items[j] as TabItem;
                for (int i = 0; i < allStations.Count; i++)
                {
                    if (subItem.Header.ToString () == allStations[i].ProdoctLine)
                    {
                        foreach (SingleStationConfig station in allStations[i].Stations)
                        {
                            if (station.IsSelected)
                            {
                                tabStations.SelectedIndex = j;
                                isFind = true;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            if (!isFind)
            {
                tabStations.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="args">产线;工位类型;ID;模板类型;工序;UserID;软件名</param>
        public Login(string args)
        {
            string[] argsArr = args.Split(';');
            selectedLine = argsArr[0];
            selectStation.Name = argsArr[1];
            id = argsArr[2];
            selectStation.TemplateType = argsArr[3];
            selectStation.TestProcess = argsArr[4];
            softwareName = argsArr[6];

            //更新OCITSSystem测试软件
            if (IsUpdateOCITSSystem())
            {
                CreatSoftwareEnvirment(true);
            }
            
            //记录当前选择的工位    
            string errMsg = "";
            StationXMLParser.RecordSelectedStation(localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + softwareName + "\\OCITestSystem\\set\\stations.xml",
                selectedLine, selectStation.Name, ref errMsg);

            //创建桌面快捷方式
            string dir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\OCITSAutoUpdate";
            string file = Common.FindEXEPath(dir);
            ShortCutCreator.CreateShortcutOnDesktop(selectedLine + "_" + selectStation.Name + "_" + id, file);

            //将set文件下载到本地
            string localSetPath = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem\\set";
            string serverSetPath = serverPath + "\\OCITS\\OCITSConfig\\" + System.Net.Dns.GetHostName().ToUpper() + "\\" + selectedLine + "\\" + selectStation.Name + "_" + id + "\\set";
            if (System.IO.Directory.Exists(serverPath))
                if (System.IO.Directory.Exists(serverSetPath))
                    Common.CopyFolder(serverSetPath, localSetPath, ref errMsg, false, false);

            //启动测试软件
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = argsArr[0] + ";" + argsArr[1] + ";" + argsArr[2] + ";" + argsArr[3]
                + ";" + argsArr[4] + ";" + argsArr[5];
            info.WindowStyle = ProcessWindowStyle.Normal;
            dir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem";
            file = Common.FindEXEPath(dir);
            info.FileName = file;//需要启动的程序
            Process.Start(info);

            this.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// tab页响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabStations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem currItem = tabStations.SelectedItem as TabItem;
            for (int i = 0; i < allStations.Count; i++)
            {
                if (currItem.Header.ToString () != allStations[i].ProdoctLine)
                {
                    for (int j = 0; j < allStations[i].Stations.Count; j++)
                    {
                        allStations[i].Stations[j].IsSelected = false;
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (tool != null)
            {
                tool.Dispose();
                tool = null;
            }
            this.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// 最小化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mini_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; //设置窗口最小化
            //App.DoEvents();
        }

        /// <summary>
        /// 最大化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal; //设置窗口还原
            }
            else
            {
                this.WindowState = WindowState.Maximized; //设置窗口最大化
            }
            //App.DoEvents();
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (tool != null)
            {
                tool.Dispose();
                tool = null;
            }
            this.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// 鼠标点在标题栏，拖动响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                //App.DoEvents();
            }
        }

        /// <summary>
        /// 登录响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string xmlSetPath = serverPath + "\\OCITS\\CommonConfig\\set\\xmlSet.xml";
                if (!System.IO.File.Exists(xmlSetPath))
                    xmlSetPath = localPath + "\\CommonConfig\\set\\xmlSet.xml";
                IniParser xmlSet = new IniParser(xmlSetPath);
                string amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
                string errMsg = "";
                List<List<string>> userAccounts = CommonFunction.GetUserData(amtsUrl, ref errMsg);
                if (errMsg.Length > 0 || userAccounts == null)
                {
                    MessageBox.Show(errMsg, "登陆错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                bool bSuccess = false;
                foreach (List<string> single in userAccounts)
                {
                    if (userName.Text == single[0] && password.Password == single[1])
                    {
                        bSuccess = true;
                        break;
                    }
                    else if (userName.Text == single[0] && password.Password != single[1])
                    {
                        MessageBox.Show("用户名或密码出错", "登陆错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        password.Password = "";
                        return;
                    }
                    else
                        continue;
                }
                if (!bSuccess)
                {
                    MessageBox.Show("用户名出错", "登陆错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    userName.Text = "";
                    password.Password = "";
                    return;
                }
                bool isSelected = false;
                foreach (StationShowConfig line in allStations)
                {
                    foreach (SingleStationConfig single in line.Stations)
                    {
                        if (single.IsSelected)
                        {
                            isSelected = true;
                            selectedLine = line.ProdoctLine;
                            selectStation = single;
                            break;
                        }
                    }
                    if (isSelected)
                        break;
                }
                if (isSelected)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    if (exePath.ToUpper().Contains("ZH-SOFT-SRV") || exePath.ToUpper().Contains("ZH-MFS-SRV"))
                    {//新搭环境
                        id = GetID(selectedLine, selectStation.Name);

                        string path = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id;
                        if (!System.IO.Directory.Exists(path))
                            System.IO.Directory.CreateDirectory(path);
                        string modulePath = serverPath + "\\OCITS\\CommonConfig\\set\\module_" + selectStation.Name + ".xml";
                        if (System.IO.File.Exists(modulePath))
                        {
                            softwareName = ModuleParser(modulePath);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("未找到文件" + modulePath + "，请检查网络是否连接良好！");
                            return;
                        }
                        
                        CreatSoftwareEnvirment(false);
                        string localDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\OCITSAutoUpdate";
                        string serverDir = serverPath + "\\OCITSAutoUpdate";
                        errMsg = "";
                        Common.CopyFolder(serverDir, localDir, ref errMsg, true, true);
                        if (errMsg != "")
                        {
                            System.Windows.Forms.MessageBox.Show(errMsg);
                            Common.WriteLog(errMsg);
                            return;
                        }

                        //创建服务器配置路径
                        string setPath = serverPath + "\\OCITS\\OCITSConfig\\" + System.Net.Dns.GetHostName().ToUpper() + "\\" + selectedLine + "\\" + selectStation.Name+"_"+id + "\\set";
                        if (System.IO.Directory.Exists(serverPath))
                            if (!System.IO.Directory.Exists(setPath))
                                System.IO.Directory.CreateDirectory(setPath);
                    }
                    else
                    {
                        string modulePath = localPath + "\\CommonConfig\\set\\module_" + selectStation.Name + ".xml";
                        if (System.IO.File.Exists(modulePath))
                        {
                            softwareName = ModuleParser(modulePath);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("未找到文件" + modulePath + "！");
                            return;
                        }
                        string[] exePathArr = exePath.Split('\\');
                        string[] stationInfo = exePathArr[exePathArr.Length - 3].Split('_');
                        if (stationInfo.Length >= 3)
                            id = stationInfo[2];

                        //产线;工位类型;ID;模板类型;工序;UserID
                        processArgs = selectedLine + ";";
                        processArgs += selectStation.Name + ";";
                        processArgs += id + ";";
                        processArgs += selectStation.TemplateType + ";";
                        processArgs += selectStation.TestProcess + ";";
                        processArgs += userName.Text + ";";
                        processArgs += softwareName;

                        UpdateByMyself(stationInfo);
                    }
                    //记录当前选择的工位                
                    StationXMLParser.RecordSelectedStation(localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + softwareName + "\\OCITestSystem\\set\\stations.xml",
                        selectedLine, selectStation.Name, ref errMsg);

                    //创建桌面快捷方式
                    string dir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\OCITSAutoUpdate";
                    string file = Common.FindEXEPath(dir);
                    ShortCutCreator.CreateShortcutOnDesktop(selectedLine + "_" + selectStation.Name + "_" + id, file);

                    //将set文件下载到本地
                    string localSetPath = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem\\set";
                    string serverSetPath = serverPath + "\\OCITS\\OCITSConfig\\" + System.Net.Dns.GetHostName().ToUpper() + "\\" + selectedLine + "\\" + selectStation.Name + "_" + id + "\\set";
                    if (System.IO.Directory.Exists(serverPath))
                        if (System.IO.Directory.Exists(serverSetPath))
                            Common.CopyFolder(serverSetPath, localSetPath, ref errMsg, false, false);
                    
                    //启动测试软件
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.Arguments = selectedLine + ";" + selectStation.Name + ";" + id + ";" + selectStation.TemplateType
                        + ";" + selectStation.TestProcess + ";" + userName.Text;
                    info.WindowStyle = ProcessWindowStyle.Normal;
                    dir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem";
                    file = Common.FindEXEPath(dir);
                    info.FileName = file;//需要启动的程序
                    Process.Start(info);

                    this.Close();
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show("请选择工位类型！", "登陆", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Common.WriteLog(ex.Message);
            }
        }

        /// <summary>
        /// 遍历本地OCITS文件夹中是否有产线_工位类型_ID文件夹，有则ID递增，无则ID=1
        /// </summary>
        /// <param name="line"></param>
        /// <param name="station"></param>
        /// <returns></returns>
        private string GetID(string line, string station)
        {
            try
            {
                if (System.IO.Directory.Exists(localPath))
                {
                    int id = 0;
                    foreach (string file in System.IO.Directory.GetDirectories(localPath))
                    {
                        System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(file);
                        if (info.Name.Contains(line + "_" + station))
                        {
                            string[] infoArr = info.Name.Split('_');
                            int temp = Convert.ToInt32(infoArr[2]);
                            if (id < temp)
                                id = temp;
                        }
                    }
                    return (id + 1).ToString();
                }
                return "1";
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                   + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(content);
                return "1";
            }
        }

        /// <summary>
        /// 解析module_产线工位类型.xml文件
        /// </summary>
        /// <param name="modulePath"></param>
        /// <returns>返回软件号</returns>
        private string ModuleParser(string modulePath)
        {
            try
            {
                if (System.IO.File.Exists(modulePath))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(modulePath);
                    XmlNode root = doc.SelectSingleNode("Grid");
                    XmlNode node = root.SelectSingleNode("Software");
                    return node.Attributes["ID"].InnerText;
                }
                return "";
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(content);
                return "";
            }
        }

        /// <summary>
        /// 创建软件环境
        /// </summary>
        private void CreatSoftwareEnvirment(bool isFilterSet)
        {
            try
            {
                string serverSoftwareDir = serverPath + "\\OCITS\\" + softwareName;
                string serverCommonConfigDir = serverPath + "\\OCITS\\CommonConfig\\set";
                string serverSwitchDir = serverPath + "\\OCITS\\CommonConfig\\switch";

                //获取软件环境
                string errMsg = "";
                if (System.IO.Directory.Exists(serverSoftwareDir))
                {
                    string localSoftwareDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName;
                    if (!System.IO.Directory.Exists(localSoftwareDir))
                        System.IO.Directory.CreateDirectory(localSoftwareDir);
                    Common.CopyFolder(serverSoftwareDir, localSoftwareDir, ref errMsg, true, isFilterSet);
                    if (errMsg != "")
                    {
                        System.Windows.Forms.MessageBox.Show(errMsg);
                        Common.WriteLog(errMsg);
                        return;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("未找到文件夹" + serverSoftwareDir + "，请检查网络是否连接良好！");
                    return;
                }

                //获取公共配置文件
                errMsg = "";
                if (System.IO.Directory.Exists(serverCommonConfigDir))
                {
                    string localConfigDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem\\set";
                    if (!System.IO.Directory.Exists(localConfigDir))
                        System.IO.Directory.CreateDirectory(localConfigDir);
                    Common.CopyFolder(serverCommonConfigDir, localConfigDir, ref errMsg, false, true);
                    if (errMsg != "")
                    {
                        System.Windows.Forms.MessageBox.Show(errMsg);
                        Common.WriteLog(errMsg);
                        return;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("未找到文件夹" + serverCommonConfigDir + "，请检查网络是否连接良好！");
                    return;
                }

                //获取公共开关配置文件
                errMsg = "";
                if (System.IO.Directory.Exists(serverSwitchDir))
                {
                    string localSwitchDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName + "\\OCITestSystem\\switch";
                    if (!System.IO.Directory.Exists(localSwitchDir))
                        System.IO.Directory.CreateDirectory(localSwitchDir);
                    Common.CopyFolder(serverSwitchDir, localSwitchDir, ref errMsg, false, true);
                    if (errMsg != "")
                    {
                        System.Windows.Forms.MessageBox.Show(errMsg);
                        Common.WriteLog(errMsg);
                        return;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("未找到文件夹" + serverSwitchDir + "，请检查网络是否连接良好！");
                    return;
                }

                string serverCommonConfig = serverPath + "\\OCITS\\CommonConfig";
                if (System.IO.Directory.Exists(serverCommonConfig))
                {
                    string localCommonConfig = localPath + "\\CommonConfig";
                    if (!System.IO.Directory.Exists(localCommonConfig))
                        System.IO.Directory.CreateDirectory(localCommonConfig);
                    Common.CopyFolder(serverCommonConfig, localCommonConfig, ref errMsg, true, false);
                    if (errMsg != "")
                    {
                        System.Windows.Forms.MessageBox.Show(errMsg);
                        Common.WriteLog(errMsg);
                        return;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("未找到文件夹" + serverSwitchDir + "，请检查网络是否连接良好！");
                    return;
                }
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(content);
                return;
            }
        }

        /// <summary>
        /// OCITSAutoUpdate自我升级
        /// </summary>
        /// <param name="stationInfo">快捷方式名称的解析</param>
        private void UpdateByMyself(string[] stationInfo)
        {
            try
            {
                if (stationInfo[0] == selectedLine && stationInfo[1] == selectStation.Name)
                {
                    string path = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\OCITSAutoUpdate_1";
                    string errMsg = "";
                    if (System.IO.Directory.Exists(path))
                    {//清除OCITSAutoUpdate_1
                        Common.DelectDir(path, ref errMsg);
                        if (errMsg != "")
                            Common.WriteLog(errMsg);
                    }

                    //OCITSAutoUpdate自我升级  
                    string localDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\OCITSAutoUpdate";
                    string serverDir = serverPath + "\\OCITSAutoUpdate";
                    if (Common.CompareFolderVersion(localDir, serverDir))
                    {//OCITSAutoUpdate有更新
                        errMsg = "";
                        Common.CopyFolder(serverDir, path, ref errMsg, true, true);
                        if (errMsg != "")
                        {
                            System.Windows.Forms.MessageBox.Show(errMsg);
                            Common.WriteLog(errMsg);
                            return;
                        }
                        string name = Common.FindEXEPath(path);
                        ProcessStartInfo p = new ProcessStartInfo();
                        p.Arguments = processArgs;
                        p.WindowStyle = ProcessWindowStyle.Normal;
                        p.FileName = name;
                        Process.Start(p);

                        this.Close();
                        Environment.Exit(0);
                    }
                    else
                    {//OCITSAutoUpdate是最新的，不需要更新
                     //检测测试程序是否是最新的
                        if (IsUpdateOCITSSystem())
                        {
                            CreatSoftwareEnvirment(true);
                        }
                    }
                }
                else
                {//用户选择的产线、工位类型与当前快捷方式不符

                }
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(content);
                return;
            }
        }

        /// <summary>
        /// 检测OCITS测试软件是否需要更新
        /// 判断标准：1.SW...软件是否有更新，2.OCITS公共配置文件是否有更新
        /// </summary>
        /// <returns></returns>
        private bool IsUpdateOCITSSystem()
        {
            try
            {
                string serverSoftwareDir = serverPath + "\\OCITS\\" + softwareName;
                string serverCommonConfigDir = serverPath + "\\OCITS\\CommonConfig\\set";
                string serverSwitchDir = serverPath + "\\OCITS\\CommonConfig\\switch";

                //获取软件环境
                if (System.IO.Directory.Exists(serverSoftwareDir))
                {
                    string localSoftwareDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + "\\" + softwareName;
                    if (!System.IO.Directory.Exists(localSoftwareDir))
                        return false;
                    return Common.CompareFolderVersion(localSoftwareDir, serverSoftwareDir);
                }
                else
                {
                    return false;
                }

                //获取公共配置文件
                if (System.IO.Directory.Exists(serverCommonConfigDir))
                {
                    string localConfigDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + softwareName + "\\OCITestSystem\\set";
                    if (!System.IO.Directory.Exists(localConfigDir))
                        System.IO.Directory.CreateDirectory(localConfigDir);
                    return Common.CompareFolderVersion(localConfigDir, serverCommonConfigDir);
                }
                else
                {
                    return false;
                }

                //获取公共开关配置文件
                if (System.IO.Directory.Exists(serverSwitchDir))
                {
                    string localSwitchDir = localPath + "\\" + selectedLine + "_" + selectStation.Name + "_" + id + softwareName + "\\OCITestSystem\\switch";
                    if (!System.IO.Directory.Exists(localSwitchDir))
                        System.IO.Directory.CreateDirectory(localSwitchDir);
                    return Common.CompareFolderVersion(localSwitchDir, serverSwitchDir);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(content);
                return false;
            }
        }

        /// <summary>
        /// 取消响应函数
        /// </summary>
        private void loginCansel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// 快捷键响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                if (userName.IsFocused)
                {
                    password.Focus();
                }
                else if (password.IsFocused)
                {
                    loginOK_Click(sender, e);
                    loginOK.Focus();
                }
            }
        }
    }
}
