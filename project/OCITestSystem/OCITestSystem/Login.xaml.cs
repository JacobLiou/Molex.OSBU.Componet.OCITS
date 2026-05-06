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

namespace OCITestSystem
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        /// <summary>
        /// 所有table页
        /// </summary>
        public static ObservableCollection<TabItem> tabItems = new ObservableCollection<TabItem>();

        /// <summary>
        /// 所有工位类型信息
        /// </summary>
        private List<StationShowConfig> allStations = new List<StationShowConfig>();

        public Login()
        {
            InitializeComponent();
            string errMsg = "";
            //增加工位类型文件解析
            StationXMLParser.GetAllStations(GetExeDir() + "\\set\\stations.xml",ref allStations,ref errMsg);
            //
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
                //tabFrame.Content= new ConfigDetail(allDeviceConfig[i], allConfigInfo[i]);
                //查找deviceNameList[i]类型已有的配置信息
                tabFrame.Content = new SameProductLine(allStations[i]);
                subItem.Content = tabFrame;
                tabItems.Add(subItem);
                //if (i == 0)
                //    subItem.IsSelected = true;

                //subItem.Content=
            }
            tabStations.ItemsSource = tabItems;
            tabStations.SelectionChanged += TabStations_SelectionChanged;
            for (int j = 0; j < tabStations.Items.Count; j++)
            {
                TabItem subItem = tabStations.Items[j] as TabItem;
                for (int i = 0; i < allStations.Count; i++)
                {
                    if (subItem.Header == allStations[i].ProdoctLine)
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

        private void TabStations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem currItem = tabStations.SelectedItem as TabItem;
            for (int i = 0; i < allStations.Count; i++)
            {
                if (currItem.Header != allStations[i].ProdoctLine)
                {
                    for (int j = 0; j < allStations[i].Stations.Count; j++)
                    {
                        allStations[i].Stations[j].IsSelected = false;
                    }
                }
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            
            //tabStationType.ItemsSource = tabItems;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Close();
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
            this.Close();
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

        private void loginOK_Click(object sender, RoutedEventArgs e)
        {
            IniParser xmlSet = new IniParser(GetExeDir() + CommonFunction.GetXmlSetPath());
            string amtsUrl = xmlSet.readStringData(CommonFunction.GetXmlSetSection(), CommonFunction.GetXmlSetKey(), "http://172.18.1.101/amts/");
            string errMsg = "";
            List<List<string>> userAccounts = CommonFunction.GetUserData(amtsUrl, ref errMsg);
            if(errMsg.Length>0|| userAccounts == null)
            {
                MessageBox.Show(errMsg, "登陆错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool bSuccess = false;
            foreach(List<string> single in userAccounts)
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
            if(!bSuccess)
            {
                MessageBox.Show("用户名出错", "登陆错误", MessageBoxButton.OK, MessageBoxImage.Error);
                userName.Text = "";
                password.Password = "";
                return;
            }
            string processArgs = "";
            string selectedLine = "";
            //string selectedStation = "";
            bool isSelected = false;
            SingleStationConfig selectStation = null;
            foreach (StationShowConfig line in allStations)
            {
                foreach(SingleStationConfig single in line.Stations)
                {
                    if(single.IsSelected)
                    {
                        isSelected = true;
                        selectedLine = line.ProdoctLine;
                        //selectedStation = single.Name;
                        selectStation = single;
                        break;
                    }
                }
                if (isSelected)
                    break;
            }
            if(isSelected)
            {
                
                string curPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;            
                
                //应用程序所在那级文件夹名称如果有_ID，则解析ID，如果无，则默认为0，只有一个工位同时使用两个相同软件时才需要
                string[] dirs = curPath.Split('\\');
                if (dirs.Length == 0)
                    return;
                //去除应用程序名，得到路径
                int count = dirs[dirs.Length - 1].Length + 1;
                string idPath = curPath.Remove(curPath.Length - count, count);
                string id = "0";
                //程序所在文件夹是倒数第2个
                if (dirs.Length > 1)
                {
                    string[] ids = dirs[dirs.Length - 2].Split('_');
                    if(ids.Length>1)
                    {
                        id = ids[1];
                    }

                }
                //产线;工位类型;ID;模板类型;工序;UserID;Goldsample
                processArgs = selectedLine + ";";
                processArgs += selectStation.Name + ";";
                processArgs += id + ";";
                processArgs += selectStation.TemplateType + ";";
                processArgs += selectStation.TestProcess + ";";
                processArgs += userName.Text + ";";
                processArgs += selectStation.Goldsample;

                //记录当前选择的工位                
                StationXMLParser.RecordSelectedStation(GetExeDir() + "\\set\\stations.xml", selectedLine, selectStation.Name, ref errMsg);
                //

                //启动自动升级软件
                /*ProcessStartInfo info = new ProcessStartInfo();
                info.Arguments = processArgs;
                info.WindowStyle= ProcessWindowStyle.Normal;
                info.FileName = "C:\\Users\\jruan01\\OneDrive - kochind.com\\Documents\\CSharp\\testcode\\start\\StartTest.exe";//需要启动的程序
                Process.Start(info);
                
                this.Close();*/
                MainWindow mainWindow = new OCITestSystem.MainWindow(processArgs);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("请选择工位类型！", "登陆", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 获取当前exe所在目录
        /// </summary>
        /// <returns>目录</returns>
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

        private void loginCansel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                if(userName.IsFocused)
                {
                    password.Focus();
                }
                else if(password.IsFocused)
                {
                    loginOK_Click(sender, e);
                    loginOK.Focus();
                }
            }
        }
    }
}
