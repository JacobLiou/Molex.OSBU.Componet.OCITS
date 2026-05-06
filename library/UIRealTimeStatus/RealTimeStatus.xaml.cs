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
using System.Collections.ObjectModel;
using System.Globalization;
using ProtocolAggregator;
using MolexUtility;

namespace UIRealTimeStatus
{
    /// <summary>
    /// Interaction logic for RealTimeStatus.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIRealTimeStatus")]
    public partial class RealTimeStatus : UserControl
    {
        /// <summary>
        /// 将触发者容器注入
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        public ObservableCollection<RealtimeStatusInfo> AllStatus { get; set; }

        public RealTimeStatus()
        {
            InitializeComponent();
            AllStatus = new ObservableCollection<RealtimeStatusInfo>();
           
            listStatus.ItemsSource = AllStatus;

        }

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        /// <summary>
        /// 与插件通信，将传进的模板信息进行显示
        /// </summary>
        private void StatusUpdateRegister()
        {
            EventAggregator.GetEvent<EventRealTimeStatus>().Subscribe
                (
                    info =>
                    {
                        UpdateStatus(info);
                    }
                );
        }

        public void UpdateStatus(RealtimeStatusInfo status)
        {
            status.Index = AllStatus.Count;
            AllStatus.Insert(0, status);
            listStatus.SelectedIndex = 0;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            StatusUpdateRegister();
        }
    }

    public sealed class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            /*if (type == StatusType.Normal)
            {
                return Brushes.LightBlue;
            }
            else if (type == StatusType.Warning)
            {
                return Brushes.Beige;
            }
            else if (type == StatusType.Error)
            {
                return Brushes.Red;
            }
            else*/
                return Brushes.LightBlue;

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
