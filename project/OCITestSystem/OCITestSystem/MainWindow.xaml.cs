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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using System.ComponentModel;
using ProtocolAggregator;
using System.Xml;
using MenuPluginInterface;
using MolexUtility.Device;
using MolexUtility.Protocol;
using MolexUtility;
using System.IO;
using System.Reflection;
using MolexUtility.UIList;

///<summary>
///文件名：MainWindow.xaml.cs
///作用：主程序，动态加载菜单、子模块、设备初始化
///作者：阮锦芳
///编写日期：2018-02-26
///修改记录
///</summary>


namespace OCITestSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private CompositionContainer container;

        /// <summary>
        /// 定义了Export(UserControl)的界面子模块集
        /// </summary>
        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<UserControl> cards;

        private List<UserControl> dynamicCards = new List<UserControl>();

        /// <summary>
        /// 与其他模块通信的事件集 
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// 定义了Export(IMenuPlugin)的菜单项集
        /// </summary>
        [ImportMany]
        private IEnumerable<IMenuPlugin> menuPlugins;

        /// <summary>
        /// 设备处理模块
        /// </summary>
        [Import]
        private IDeviceHandle deviceHandle;

        private string stationType = "";

        private string softwareID = "";

        private string softwareVersion = "";

        private string softwareName = "";

        private string useUDL = "0";

        private MainInitInfo mainInfo = new MainInitInfo();

        /// <summary>
        /// 所有工位类型信息
        /// </summary>
        private List<StationShowConfig> allStations = new List<StationShowConfig>();

        public MainWindow(string type)
        {
            InitializeComponent();
            this.ResizeMode = System.Windows.ResizeMode.CanMinimize;
            this.Left = 0;
            this.Top = 0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            ParserCmdArgs(type);
            //产线;工位类型;ID;应用程序路径;模板类型;工序;UserID
            //按照这个解析
            /*string[] splits = type.Split(';');
            if(splits.Length==7)
            {
                mainInfo.ProductLine = splits[0];
                mainInfo.StationType = splits[1];
                mainInfo.StationID = splits[2];
                //mainInfo.ExePath = splits[3];
                mainInfo.TemplateType = splits[3];
                mainInfo.TestProcess = splits[4];
                mainInfo.UserID = splits[5];
                mainInfo.Goldsample= splits[6];
                stationType = mainInfo.StationType;
                barUserID.Content = "工号：" + mainInfo.UserID;
                barTemplate.Content = "模板类型："+mainInfo.TemplateType;
                barTestProcess.Content = "工序：" + mainInfo.TestProcess;
            }*/
            string errMsg = "";
            //增加工位类型文件解析
            StationXMLParser.GetAllStations(GetExeDir() + "\\set\\stations.xml", ref allStations, ref errMsg);
            if(allStations.Count>0&& allStations[0].Stations.Count>0)
            {
                Environment.CurrentDirectory = GetExeDir();
                SingleStationConfig activeStation = GetSelectedStationConfig();
                if (activeStation == null)
                    activeStation = allStations[0].Stations[0];
                stationType = activeStation.Name;
                mainInfo.AutomationType = Convert.ToInt32(activeStation.Automation);
                string fileName = GetExeDir() + "\\Module\\Module_" + stationType + ".xml";
                string mainDllPath = GetExeDir() +"\\"+ activeStation.MainDllPath;
                mainInfo.Goldsample = activeStation.Goldsample;
                ApplyTestProcessFromStationsXml();
                LayoutXMLParser.ParseSoftIDAndVersion(fileName, ref softwareID, ref softwareVersion, ref softwareName,ref useUDL);
                if (stationType.Length > 0)
                {
                    FileInfo dllInfo = new FileInfo(mainDllPath);
                    //txtTitle.Text = "OCITS 光器件集成测试系统" + "--" + stationType + "(" + softwareID + "_" + softwareVersion + ")";
                    txtTitle.Text = softwareName + "_" + softwareVersion;
                }
            }
            else
            {
                MessageBox.Show("工位配置出错，请检查配置文件stations.xml");
            }
        }

        /// <summary>
        /// 与插件通信
        /// </summary>
        private void InitRegerster()
        {
            //receive Xml format protocal
            EventAggregator.GetEvent<EventXml>().Subscribe
                (
                    msg =>
                    {
                        ParserMsg(msg);
                    }
                );
        }
        private void ParserMsg(XmlStr msg)
        {
            MsgBaseInfo info = new MsgBaseInfo();
            MsgXmlParser.GetMsgBase(msg.Content, ref info);
            if (info.MsgTarget == "MainWindow")
            {
                if (info.MsgType == "Template")
                {
                    if (info.Operate == "ShowTemplatePath")
                    {
                        string tmpltPath = MsgXmlParser.GetNodeInner(msg.Content, "Path");
                        if (tmpltPath != "")
                        {
                            barTmpltPath.Content = "模板："+ tmpltPath;
                        }
                    }
                }
                else if (info.MsgType == "BtnClick")
                {
                    if (info.Operate == "InitDevice")
                    {
                        string result = MsgXmlParser.GetNodeInner(msg.Content, "Result");
                        if (result == "0")
                        {
                            curStatus.Content = "设备正常初始化";
                        }
                        else
                        {
                            string errMsg = MsgXmlParser.GetNodeInner(msg.Content, "NoteMsg");
                            MessageBox.Show(errMsg);
                            curStatus.Content = "设备初始化出错！";
                        }
                    }
                }
            }
        }

        private SingleStationConfig GetSelectedStationConfig()
        {
            if (allStations == null)
                return null;
            foreach (StationShowConfig line in allStations)
            {
                foreach (SingleStationConfig station in line.Stations)
                {
                    if (station.IsSelected)
                        return station;
                }
            }
            return null;
        }

        /// <summary>
        /// 从 set\stations.xml 中已选工位的 TestProcess 覆盖 MIMS 传入工序（ITL_FTS 应配置为 Interleaver-ITL-终测）。
        /// </summary>
        private void ApplyTestProcessFromStationsXml()
        {
            if (allStations == null || allStations.Count == 0)
                return;
            foreach (StationShowConfig line in allStations)
            {
                foreach (SingleStationConfig station in line.Stations)
                {
                    if (!station.IsSelected || string.IsNullOrWhiteSpace(station.TestProcess))
                        continue;
                    if (string.IsNullOrEmpty(stationType) ||
                        string.Equals(station.Name, stationType, StringComparison.OrdinalIgnoreCase))
                    {
                        mainInfo.TestProcess = station.TestProcess.Trim();
                        return;
                    }
                }
            }
        }

        private void ParserCmdArgs(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return;
            if (!args.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                TryParseSemicolonStationArgs(args);
                return;
            }
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(args);
                XmlNode rootNode = doc.SelectSingleNode("MIMS");
                XmlNode appInfoNode = rootNode.SelectSingleNode("AppInfo");
                XmlNodeList childNodes = appInfoNode.ChildNodes;
                foreach (XmlNode child in childNodes)
                {
                    if (child.Name.ToString().ToUpper() == "USER")
                    {
                        mainInfo.UserID = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "PN")
                    {

                    }
                    else if (child.Name.ToString().ToUpper() == "SN")
                    {

                    }
                    else if (child.Name.ToString().ToUpper() == "PROCESS")
                    {
                        mainInfo.TestProcess = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "LOGINMODE")
                    {
                        mainInfo.LoginMode = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "SOFTWAREID")
                    {
                        mainInfo.SoftwareID = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "MESMODE")
                    {
                        mainInfo.MESMode = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "CHECKUSER")
                    {
                        mainInfo.CheckUser = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "CHECKPWD")
                    {
                        mainInfo.CheckPSW = child.InnerText;
                    }
                }
            }
            catch(Exception ex)
            {
                TryParseSemicolonStationArgs(args);
            }
        }

        /// <summary>
        /// 登录界面传入：产线;工位类型;ID;模板类型;工序;UserID;Goldsample
        /// </summary>
        private void TryParseSemicolonStationArgs(string args)
        {
            string[] splits = args.Split(';');
            if (splits.Length < 7)
                return;
            mainInfo.ProductLine = splits[0];
            mainInfo.StationType = splits[1];
            mainInfo.StationID = splits[2];
            mainInfo.TemplateType = splits[3];
            mainInfo.TestProcess = splits[4];
            mainInfo.UserID = splits[5];
            mainInfo.Goldsample = splits[6];
            stationType = mainInfo.StationType;
        }

        /// <summary>
        /// 导入module路径下与成员变量中Import对应的Export所有模块
        /// </summary>
        private void Compose()
        {
            try {
                /*AggregateCatalog aggregateCatalog = new AggregateCatalog();
                AssemblyCatalog assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                DirectoryCatalog directoryCatalog = new DirectoryCatalog("Library1");
                aggregateCatalog.Catalogs.Add(assemblyCatalog);
                aggregateCatalog.Catalogs.Add(directoryCatalog);*/
                //设置目录，让引擎能自动去发现新的扩展
                var catalog = new AggregateCatalog();
                //MessageBox.Show(GetExeDir() + "\\module");
                var dc = new DirectoryCatalog(GetExeDir() + "\\module");
                catalog.Catalogs.Add(dc);
                var ss = dc.LoadedFiles;
                container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Compose();
                initMenu();
                InitRegerster();
                //this.Owner = System.Windows.Application.Current.MainWindow;

                string fileName = GetExeDir() + "\\module\\Module_" + stationType+".xml";
                
                List<GridLength> rowDefines = new List<GridLength>();
                List<GridLength> columnDefines = new List<GridLength>();
                List<PanelConfige> childs = new List<PanelConfige>();
                //从配置文件读取布局信息
                //MessageBox.Show(fileName);
                LayoutXMLParser.ParseConfig(fileName, ref rowDefines, ref columnDefines, ref childs);
                
                //分行信息
                RowDefinitionCollection rowDefs = rootGrid.RowDefinitions;
                foreach (GridLength heigh in rowDefines)
                {
                    RowDefinition rowDef = new RowDefinition();
                    rowDef.Height = heigh;
                    rowDefs.Add(rowDef);
                }
                //MessageBox.Show("1");
                //分列信息
                ColumnDefinitionCollection columnDefs = rootGrid.ColumnDefinitions;
                foreach (GridLength width in columnDefines)
                {
                    ColumnDefinition columnDef = new ColumnDefinition();
                    columnDef.Width = width;
                    columnDefs.Add(columnDef);
                }
                //MessageBox.Show("2");
                //各模块分布信息
                foreach (PanelConfige child in childs)
                {
                    //MessageBox.Show("3");
                    DockPanel panel = new DockPanel();

                    if (child.Row != -1)
                    {
                        Grid.SetRow(panel, child.Row);
                    }
                    if (child.Column != -1)
                        Grid.SetColumn(panel, child.Column);
                    if (child.ColumnSpan != -1)
                        Grid.SetColumnSpan(panel, child.ColumnSpan);
                    if (child.RowSpan != -1)
                        Grid.SetRowSpan(panel, child.RowSpan);                      
                    panel.Margin = new Thickness(2);
                    if (child.ModuleName.Length > 0)
                    {
                        IEnumerable<Lazy<UserControl, IDictionary<string, object>>> modules = null;
                        modules = container.GetExports<UserControl, IDictionary<string, object>>().Where(x => (string)x.Metadata["name"] == child.ModuleName);
                        Lazy<UserControl, IDictionary<string, object>> tmp = modules.ElementAtOrDefault(0);
                        if (child.ModuleIndex > 0)
                        {
                            var typeName = tmp.Value.GetType();
                            UserControl control = (UserControl)Activator.CreateInstance(typeName);
                            control.Name = child.Name;
                            dynamicCards.Add(control);
                            panel.Children.Add(control);

                        }
                        else
                        {
                            panel.Children.Add(tmp.Value);
                            tmp.Value.Name= child.Name;
                        }
                    }
                    //panel根据module，增加自己显示内容
                    rootGrid.Children.Add(panel);
                }

                //
                //initMenu();
                App.DoEvents();
                //等待1s后开始初始化设备
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitInitDevice;
                bw.RunWorkerCompleted += InitDevice;
                bw.RunWorkerAsync();

                //查看是否有文件夹temple，reference、rawdata、data文件夹，如无，则创建。
                string curPath = System.Environment.CurrentDirectory;
                string tempPath = curPath + "\\temple";
                if(false==Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string refPath = curPath + "\\reference";
                if (false == Directory.Exists(refPath))
                {
                    Directory.CreateDirectory(refPath);
                }

                string rawPath = curPath + "\\rawdata";
                if (false == Directory.Exists(rawPath))
                {
                    Directory.CreateDirectory(rawPath);
                }

                string dataPath = curPath + "\\data";
                if (false == Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                string lightDataPath = curPath + "\\lightdata";
                if (false == Directory.Exists(lightDataPath))
                {
                    Directory.CreateDirectory(lightDataPath);
                }
                this.MaxHeight = SystemParameters.WorkArea.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitDevice(object sender, RunWorkerCompletedEventArgs e)
        {
            string errMsg = "";
            if (useUDL == "0")
            {
                curStatus.Content = "正在初始化设备。。。";             
                if (deviceHandle.InitDeviceByConfig(ref errMsg) != 0)
                {
                    if (errMsg.Length > 0)
                    {
                        MessageBox.Show(errMsg);                       
                    }
                    curStatus.Content = "设备初始化出错！";
                    mainInfo.DeviceInitRes = false;
                }
            }
            barUserID.Content ="工号："+ mainInfo.UserID;
            string modeShow = "";
            if (mainInfo.LoginMode.ToUpper().Contains("DEBUG"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                modeShow = "调试模式";
                barTemplate.Content = "工程路径：TP/TT/NA/TD/CF";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
            else if (mainInfo.LoginMode.ToUpper().Contains("RD"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(255, 155, 0));
                modeShow = "研发模式";
                barTemplate.Content = "工程路径：TP/TT/NA/TD/CF";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(255, 155, 0));
            }
            else //if (mainInfo.LoginMode.ToUpper().Contains("MFG"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                modeShow = "生产模式";
                barTemplate.Content = "工程路径：NA";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }

            barLoginType.Content = "模式：" + modeShow + "/MES "+ mainInfo.MESMode;

            
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventMainInit>().Publish(mainInfo);
            }
            if (errMsg.Length == 0)
            {
                curStatus.Content = "设备正常初始化";
                mainInfo.DeviceInitRes = true;
            }

            
        }

        private void WaitInitDevice(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }

        /// <summary>
        /// 初始化菜单
        /// </summary>
        private void initMenu()
        {
            MenuItem item = new MenuItem();
            item.Header = "软件说明";
            menu.Items.Add(item);
            MenuItem item2 = new MenuItem();
            item2.Header = "帮助";
            menu.Items.Add(item2);

            foreach (IMenuPlugin plugin in menuPlugins)
            {
                MenuItem subItem = new MenuItem();
                subItem.Header = plugin.MenuHeader.SubHeader;
                //ToolStripMenuItem subItem = new ToolStripMenuItem(plugin.Text);
                subItem.Click += (s, arg) => { plugin.Show(mainInfo); };
                MenuItem curItem = FindHostItem(plugin.MenuHeader.HostHeader);
                curItem.Items.Add(subItem);
            }
        }

        /// <summary>
        /// 查找最上级菜单，如果存在返回对象，如果不存在，则创建
        /// </summary>
        /// <param name="header">查找菜单名称</param>
        /// <returns>菜单对象</returns>
        private MenuItem FindHostItem(string header)
        {
            ItemCollection items = menu.Items;
            MenuItem item = null;
            for (int i=0;i<items.Count;i++)
            {
                item = (MenuItem)items[i];
                if (item.Header.ToString() == header)
                    break;
                item = null;
            }
            //未找到，则创建
            if(item==null)
            {
                item = new MenuItem();
                item.Header = header;
                menu.Items.Insert(0, item);
            }
            return item;
        }

        
        /// <summary>
        /// 最小化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mini_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; //设置窗口最小化
            App.DoEvents();
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
            App.DoEvents();
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (deviceHandle != null)
            {
                string errMsg="";
                deviceHandle.CloseAllDevice(ref errMsg);
            }
            this.Close();
            System.Environment.Exit(0);
            //关闭所有窗体
            Application.Current.Shutdown();
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
                App.DoEvents();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {            
            /*KeyDownInfo keyInfo = new KeyDownInfo();
            keyInfo.Key = e.Key;
            EventAggregator.GetEvent<EventListKeyDown>().Publish(keyInfo);
            e.Handled = false;*/
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            KeyDownInfo keyInfo = new KeyDownInfo();
            keyInfo.Key = e.Key;
            EventAggregator.GetEvent<EventListKeyDown>().Publish(keyInfo);
            e.Handled = false;
        }
    }
}
