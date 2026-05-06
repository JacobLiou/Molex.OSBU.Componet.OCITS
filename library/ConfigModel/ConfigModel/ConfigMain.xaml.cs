using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using MolexUtility;
using MolexUtility.Device;
using MolexUtility.Protocol;
using System.IO;

namespace ConfigModel
{
    /// <summary>
    /// Interaction logic for ConfigMain.xaml
    /// </summary>
    public partial class ConfigMain : Window
    {
        /// <summary>
        /// 显示设备配置的分页，与界面TabControl绑定
        /// </summary>
        public static ObservableCollection<TabItem> tabItems = new ObservableCollection<TabItem>();

        /// <summary>
        /// 所有设备分类种类名称
        /// </summary>
        private List<string> deviceNameList;

        /// <summary>
        /// 所有设备需要配置信息
        /// </summary>
        private List<List<DeviceConfig>> allDeviceConfig;

        /// <summary>
        /// 使用到的设备配置信息
        /// </summary>
        private List<List<DeviceConfig>> allConfigInfo;

        private MainInitInfo baseMainInfo = null;

        /// <summary>
        /// 设备配置配置文件路径
        /// </summary>
        private string deviceConfigPath = "";

        /// <summary>
        /// 设备配置备份到服务器路径
        /// </summary>
        private string srvBackupPath = "";

        public ConfigMain()
        {
            InitializeComponent();            
            Init();
            
        }

        /// <summary>
        /// 读取记录所有类型设备的配置文件，
        /// 读取当前配置设备的配置文件，
        /// 初始化设备配置工具界面
        /// </summary>
        private void Init()
        {
            try
            {
                deviceConfigPath = System.Environment.CurrentDirectory + "\\set\\Deviceconfig.xml";
                
                srvBackupPath = "\\\\ZH-SOFT-SRV.OPLINK.COM.CN\\public\\passive\\OCITS\\OCITSConfig\\";

                //读取记录所有类型设备的配置文件
                ConfigXmlParser.ParseConfig(System.Environment.CurrentDirectory + "\\set\\AllDevice.xml", out deviceNameList, out allDeviceConfig);
                List<string> useNameList;
                //List<List<DeviceConfig>> useDevice;
                
                //读取当前配置设备的配置文件
                ConfigXmlParser.ParseConfig(deviceConfigPath, out useNameList, out allConfigInfo);
                if (allDeviceConfig == null)
                    return;


                /*allConfigInfo = new List<List<DeviceConfig>>(allDeviceConfig.Count);
                for (int i = 0; i < allDeviceConfig.Count; i++)
                {
                    allConfigInfo.Add(new List<DeviceConfig>());
                }

                if (deviceNameList != null && useNameList != null)
                {
                    for (int i = 0; i < deviceNameList.Count; i++)
                    {
                        for (int j = 0; j < useNameList.Count; j++)
                        {
                            if (deviceNameList[i] == useNameList[j])
                                allConfigInfo[i] = useDevice[j];
                        }
                    }
                }*/
                int newCount = 0;
                //根据设备分类，界面根据设备分类进行分页显示
                for (int i = 0; i < allDeviceConfig.Count; i++)
                {
                    TabItem subItem = new TabItem();
                    subItem.Header = deviceNameList[i];
                    Frame tabFrame = new Frame();
                    //tabFrame.Content= new ConfigDetail(allDeviceConfig[i], allConfigInfo[i]);
                    //查找deviceNameList[i]类型已有的配置信息
                    List<DeviceConfig> curDevices = null;
                    if (allConfigInfo != null)
                    {
                        for (int j = 0; j < allConfigInfo.Count - newCount; j++)
                        {
                            if (deviceNameList[i] == useNameList[j])
                            {
                                //如果没有选中的设备，在下面会插入一个空的list。所以用序号i而不是j
                                curDevices = allConfigInfo[i];
                                break;
                            }
                        }
                    }
                    else
                    {
                        allConfigInfo = new List<List<DeviceConfig>>();
                    }
                    if(curDevices==null)
                    {
                        curDevices = new List<DeviceConfig>();
                        allConfigInfo.Insert(i,curDevices);
                        newCount++;
                    }

                    tabFrame.Content = new SameTypeDevices(allDeviceConfig[i], curDevices);
                    subItem.Content = tabFrame;
                    tabItems.Add(subItem);
                    if (i == 0)
                        subItem.IsSelected = true;
                    //subItem.Content=
                }
                tabDevice.ItemsSource = tabItems;

                
            }
            catch(Exception ex)
            {
                MessageBox.Show(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r");
            }
        }

        

        /// <summary>
        /// 保存配置信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigXmlParser.SaveConfig(deviceConfigPath, deviceNameList, allConfigInfo);
            }
            catch(Exception ex)
            {
                MessageBox.Show("写设备配置文件出错！", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            /*BackgroundWorker srvBackupConfigBK = new BackgroundWorker();
            srvBackupConfigBK.DoWork += srvBackupConfigBK_DoWork;
            srvBackupConfigBK.RunWorkerAsync();*/
        }

        private void srvBackupConfigBK_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                SharedTool tool = new SharedTool("autotest", "China@123", "oplink");
                string computerName = System.Environment.GetEnvironmentVariable("ComputerName");
                string srvComppath = srvBackupPath + computerName;
                CheckAndCreatPath(srvComppath);

                string srvLinePath = srvComppath + "\\" + baseMainInfo.ProductLine;
                CheckAndCreatPath(srvLinePath);

                string srvStationPath = srvLinePath + "\\" + baseMainInfo.StationType + "_" + baseMainInfo.StationID;
                CheckAndCreatPath(srvStationPath);

                string destPath = srvStationPath + "\\Deviceconfig.xml";
                File.Copy(deviceConfigPath, destPath);
            }
            catch (Exception ex)
            {
                
            }
        }

        private void CheckAndCreatPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("创建文件夹错误:"+ex.Message, "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        public void BaseInfo(MainInitInfo info)
        {
            baseMainInfo = info;
        }

        private void test_Click(object sender, RoutedEventArgs e)
        {
            /*string errMsg = "";
            port.WriteSerialString(sendText.Text, ref errMsg);

            //string res;
            //port.ReadSerialString( out res, ref errMsg);
            //recText.Text += "\r" + res;*/
        }

        /// <summary>
        /// 取消关闭，将窗口隐藏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //取消关闭，将窗口隐藏
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
