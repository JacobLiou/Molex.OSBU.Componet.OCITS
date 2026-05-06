using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MolexUtility.Device;
//using Ivi.Visa;

namespace ConfigModel
{
    /// <summary>
    /// 该控件对应一个设备的具体配置。可以对设备类型进行选择，就会给出相应的配置项。
    /// </summary>
    public partial class SingleDevice : UserControl
    {
        /// <summary>
        /// 设备的具体配置信息，以及配置内容
        /// </summary>
        private DeviceConfig selectDevice;

        /// <summary>
        /// 所有设备的配置信息
        /// </summary>
        private List<DeviceConfig> allDeviceInfo;

        /// <summary>
        /// 与datagrid绑定的变量。实际只有一个数据。datagrid做了限制，不允许继续添加行。
        /// </summary>
        private List<DeviceConfig> itemSelect=new List<DeviceConfig>();

        /// <summary>
        /// 系统当前串口
        /// </summary>
        private List<string> comList=new List<string>() ;

        /// <summary>
        /// 系统当前GPIB
        /// </summary>
        private List<string> gpibList = new List<string>();

        /// <summary>
        /// 串口波特率
        /// </summary>
        private List<string> baudrateList = new List<string>();
        //private List<string> allDeviceName = new List<string>();

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelect = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceList">所有设备配置信息</param>
        /// <param name="curDevice">当前设备</param>
        public SingleDevice(List<DeviceConfig> deviceList, DeviceConfig curDevice)
        {
            InitializeComponent();
            //后续改为从系统读取
            /*IEnumerable<string> com = GlobalResourceManager.Find();
            foreach (string res in com)
            {
                ParseResult parse = GlobalResourceManager.Parse(res);
                if (parse.InterfaceType == HardwareInterfaceType.Serial)
                    comList.Add(parse.AliasIfExists);
                else if (parse.InterfaceType == HardwareInterfaceType.Gpib)
                    gpibList.Add(parse.OriginalResourceName);

            }*/
            string[] coms=System.IO.Ports.SerialPort.GetPortNames();
            foreach(string com in coms)
            {
                comList.Add(com);
            }
            
            
            baudrateList.Add("4800");
            baudrateList.Add("9600");
            baudrateList.Add("19200");
            baudrateList.Add("38400");
            baudrateList.Add("115200");

            allDeviceInfo = deviceList;
            selectDevice = curDevice;
            //选择设备combox绑定到所有设备信息
            deviceType.ItemsSource = allDeviceInfo;
            deviceType.SelectedValuePath = "ControlName";
            deviceType.DisplayMemberPath = "ShowName";

            //当前配置设备如果为空，默认为所有设备中第一个
            if(selectDevice.ControlName=="")
            {
                selectDevice = deviceList[0].Clone();
                selectDevice.ShowName = allDeviceInfo[0].ShowName;
            }
            deviceType.Text = selectDevice.ShowName;
            itemSelect.Add(selectDevice);
            GenerateColumn(selectDevice);
            
            
        }

        /// <summary>
        /// 显示需要配置的信息
        /// </summary>
        /// <param name="curDevice">当前选中配置设备</param>
        private void GenerateColumn(DeviceConfig curDevice)
        {
            //重新更新列时保留最后用于测试的两列
            while(configInfo.Columns.Count>2)
                configInfo.Columns.Remove(configInfo.Columns[0]);

            configInfo.ItemsSource = itemSelect;
            List<string> columnList = new List<string>();
            List<string> bindingPath = new List<string>();
            Dictionary<string, string> columnPair = new Dictionary<string, string>();
            
            if (curDevice.ChannelCount != "")
                columnPair["通道数"] = "ChannelCount";
            for (int i = 0; i < curDevice.ControlKey.Length; i++)
            {
                if (curDevice.ControlKey[i] != null && curDevice.ControlKey[i] != "")
                    columnPair[curDevice.ControlKey[i]] = "Control[" + i.ToString() + "]";
                //columnPair[info.Control[i]] = "Control" + i.ToString();
            }

            
            if (columnPair.Count > 0)
            {
                columnPair["确定命令"] = "CheckCmd";
            }
            //dataGridConfig.Columns.Clear();
            int index = 0;
            foreach (KeyValuePair<string, string> col in columnPair)
            {
                if (col.Key.ToUpper() == "COM")
                    configInfo.Columns.Insert(index, new DataGridComboBoxColumn() { Header = col.Key, ItemsSource = comList, SelectedValueBinding = new Binding(col.Value) });
                else if (col.Key.ToUpper() == "GPIB")
                    configInfo.Columns.Insert(index, new DataGridComboBoxColumn() { Header = col.Key, ItemsSource = gpibList, SelectedValueBinding = new Binding(col.Value) });
                else if (col.Key == "通道数")
                    configInfo.Columns.Insert(index, new DataGridTextColumn() { Header = col.Key, Binding = new Binding(col.Value) });
                else if (col.Key == "波特率")
                    configInfo.Columns.Insert(index, new DataGridComboBoxColumn() { Header = col.Key, ItemsSource = baudrateList, SelectedValueBinding = new Binding(col.Value) });
                else
                    configInfo.Columns.Insert(index, new DataGridTextColumn() { Header = col.Key, Binding = new Binding(col.Value) });
                index++;
            }

        }

        
        /// <summary>
        /// combox选中设备改变时间，当设备变化时，更新需要配置的配置项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (deviceType.SelectedItem == null)
                return;
            DeviceConfig comboxCurSelected = (DeviceConfig)deviceType.SelectedItem;
            if (comboxCurSelected.ShowName != selectDevice.ShowName)
            {
                foreach (DeviceConfig cfg in allDeviceInfo)
                {
                    if (cfg.ShowName == comboxCurSelected.ShowName)
                    {
                        selectDevice.AckData = cfg.AckData;
                        selectDevice.ChannelCount = cfg.ChannelCount;
                        selectDevice.CheckCmd = cfg.CheckCmd;
                        selectDevice.ControlName = cfg.ControlName;
                        selectDevice.Control = cfg.Control;
                        selectDevice.ControlKey = cfg.ControlKey;
                        GenerateColumn(selectDevice);
                        selectDevice.ShowName = allDeviceInfo[deviceType.SelectedIndex].ShowName;
                        break;
                    }
                }
            }
            
        }

        /// <summary>
        /// 是否选中值发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectCheck_Checked(object sender, RoutedEventArgs e)
        {
            IsSelect = selectCheck.IsChecked.Value;
        }

        /// <summary>
        /// 测试按钮被按下的响应函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendTest_Click(object sender, RoutedEventArgs e)
        {
            //int i = 0;
            
        }
    }

    
}
