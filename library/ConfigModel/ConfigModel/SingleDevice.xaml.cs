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
using System.Threading;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using MolexUtility;
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
            ISerial port = null;
            bool closeAfterTest = false;
            bool pausedBackgroundRead = false;
            string errMsg = "";
            selectDevice.AckData = "";
            try
            {
                string com = (selectDevice.Control[0] ?? "").Trim();
                if (string.IsNullOrEmpty(com))
                {
                    MessageBox.Show("请先选择 COM 口。", "设备测试", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int baud = 9600;
                int.TryParse((selectDevice.Control[1] ?? "").Trim(), out baud);
                if (baud <= 0)
                    baud = 9600;

                string cmd = (selectDevice.CheckCmd ?? "").Trim();
                if (string.IsNullOrEmpty(cmd))
                {
                    cmd = GetDefaultCheckCmd(selectDevice.ControlName, selectDevice.ShowName);
                    if (string.IsNullOrEmpty(cmd))
                    {
                        MessageBox.Show("请在「确定命令」中填写测试指令后再测试。", "设备测试", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                port = SerialDotNet.TryGetOpenPort(com);
                bool reusedOpenPort = port != null;
                if (!reusedOpenPort)
                {
                    port = new SerialDotNet(com, baud, ref errMsg, 3000, false);
                    if (!string.IsNullOrEmpty(errMsg))
                    {
                        selectDevice.AckData = errMsg.Trim();
                        RefreshConfigGrid();
                        return;
                    }
                    closeAfterTest = true;
                }
                else
                {
                    // 主程序光开关已挂 DataReceived，须先暂停事件读再同步写读（见 ISerial 注释）
                    port.SetEndThreadRead();
                    pausedBackgroundRead = true;
                }

                string payload = FormatTestCommand(selectDevice.ControlName, cmd);
                if (port.WriteSerialString(payload, ref errMsg) != 0)
                {
                    selectDevice.AckData = errMsg.Trim();
                    RefreshConfigGrid();
                    return;
                }

                string reply = ReadTestResponse(port, selectDevice.ControlName, ref errMsg);
                if (!string.IsNullOrEmpty(errMsg) && string.IsNullOrEmpty(reply))
                    selectDevice.AckData = errMsg.Trim();
                else if (string.IsNullOrWhiteSpace(reply))
                    selectDevice.AckData = reusedOpenPort
                        ? "(无回复；已复用主程序打开的串口，请检查确定命令)"
                        : "(无回复，请检查 COM/波特率/确定命令)";
                else
                    selectDevice.AckData = reusedOpenPort
                        ? "[复用已打开串口] " + reply.Trim()
                        : reply.Trim();

                RefreshConfigGrid();
            }
            catch (Exception ex)
            {
                selectDevice.AckData = ex.Message;
                RefreshConfigGrid();
            }
            finally
            {
                if (port != null && pausedBackgroundRead)
                    port.StartThreadRead();
                if (closeAfterTest && port != null)
                    port.Close();
            }
        }

        private void RefreshConfigGrid()
        {
            configInfo.ItemsSource = null;
            configInfo.ItemsSource = itemSelect;
        }

        private static string GetDefaultCheckCmd(string controlName, string showName)
        {
            if (controlName == Devices.MPLUSSwitch.GetAdditional())
            {
                if (OpticalSwitchConfigNames.InterleaverMplus1X32Out.Equals(
                        OpticalSwitchConfigNames.SanitizeMplusSwitchShowName(showName),
                        StringComparison.OrdinalIgnoreCase))
                    return "MSW 1,1,2;9,1,1;";
                return "MSW 1,1,2;9,1,1;";
            }
            if (controlName == Devices.OMSSwitch.GetAdditional())
                return "*IDN?";
            return "";
        }

        private static string FormatTestCommand(string controlName, string cmd)
        {
            if (controlName == Devices.MPLUSSwitch.GetAdditional() ||
                controlName == Devices.OMSSwitch.GetAdditional())
            {
                if (!cmd.EndsWith("\r\n", StringComparison.Ordinal) && !cmd.EndsWith("\r", StringComparison.Ordinal))
                    return cmd + "\r\n";
            }
            else if (controlName == Devices.Min1X8Switch.GetAdditional() ||
                     controlName == Devices.PboxSwitch.GetAdditional())
            {
                if (!cmd.EndsWith("\r", StringComparison.Ordinal))
                    return cmd + "\r";
            }
            return cmd;
        }

        private static string ReadTestResponse(ISerial port, string controlName, ref string errMsg)
        {
            if (controlName == Devices.MPLUSSwitch.GetAdditional())
                Thread.Sleep(150);

            var sb = new StringBuilder();
            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline)
            {
                string chunk;
                if (port.ReadSerialString(out chunk, ref errMsg) == 0 && !string.IsNullOrEmpty(chunk))
                    sb.Append(chunk);

                string buffered = sb.ToString();
                if (controlName == Devices.MPLUSSwitch.GetAdditional())
                {
                    if (buffered.IndexOf('>') >= 0 ||
                        buffered.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        buffered.IndexOf("Err:", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
                else if (controlName == Devices.OMSSwitch.GetAdditional())
                {
                    if (buffered.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
                else if (!string.IsNullOrEmpty(chunk))
                    break;

                Thread.Sleep(50);
            }
            return sb.ToString();
        }
    }

    
}
