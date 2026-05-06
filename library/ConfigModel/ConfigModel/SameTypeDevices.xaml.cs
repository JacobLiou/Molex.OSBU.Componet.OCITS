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
using MolexUtility.Device;

namespace ConfigModel
{
    /// <summary>
    /// 该控件显示、操作同一类设备的配置
    /// </summary>
    public partial class SameTypeDevices : UserControl
    {
        /// <summary>
        /// 配置的设备信息
        /// </summary>
        private List<DeviceConfig> selectDevices;

        /// <summary>
        /// 所有同类设备需要配置项等信息
        /// </summary>
        private List<DeviceConfig> allSameTypeDevice;
        public SameTypeDevices(List<DeviceConfig> deviceList, List<DeviceConfig> config)
        {
            InitializeComponent();
            allSameTypeDevice = deviceList;
            if (config == null)
                config = new List<DeviceConfig>();
            
            selectDevices = config;
            //已经配置多少个设备，就增加几项DetailSeparate
            foreach (DeviceConfig device in selectDevices)
            {
                configPannel.Children.Add(new SingleDevice(allSameTypeDevice, device));
            }
        }

        
        /// <summary>
        /// 增加一个设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void add_Click(object sender, RoutedEventArgs e)
        {
            selectDevices.Add(allSameTypeDevice[0].Clone());
            configPannel.Children.Add(new SingleDevice(allSameTypeDevice, selectDevices[selectDevices.Count-1]));
        }

        /// <summary>
        /// 删除选中设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteSelect_Click(object sender, RoutedEventArgs e)
        {
            List<UIElement> removeDevices = new List<UIElement>();
            List<DeviceConfig> removeConfigs = new List<DeviceConfig>();
            int Index = 0;
            //找出
            foreach(UIElement element in configPannel.Children)
            {
                SingleDevice detail = (SingleDevice)element;
                if (detail.IsSelect)
                {
                    removeDevices.Add(element);
                    //int remove = Index;
                    removeConfigs.Add(selectDevices[Index]);
                }
                Index++;
            }
            for(int i=0;i< removeDevices.Count;i++)
            {
                configPannel.Children.Remove(removeDevices[i]);
                selectDevices.Remove(removeConfigs[i]);
            }
            
        }
    }
}
