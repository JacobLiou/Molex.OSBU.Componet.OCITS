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
using MolexUtility;
using ProtocolAggregator;

namespace RealtimePower
{
    /// <summary>
    /// Interaction logic for RealtimePowerShow.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "RealtimePower")]
    public partial class RealtimePowerShow : UserControl
    {
        // <summary>
        /// 将触发者容器注入
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        public RealtimePowerShow()
        {
            InitializeComponent();
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
        private void PowerUpdateRegister()
        {
            EventAggregator.GetEvent<EventRealtimePowerUpdate>().Subscribe
                (
                    info =>
                    {
                        UpdatePower(info);
                    }
                );
        }

        private void InitPowerShow(StackPanel pannel, List<RealtimePowerInfo> status)
        {
            double height = this.ActualHeight;
            double width = this.ActualWidth;
            double margin = 0;
            if (height > width)
            {
                margin = (height - width) / status.Count;
                height = width;               
            }
            for (int i = 0; i < status.Count; i++)
            {
                TextBox powerShow = new TextBox();   
                powerShow.Height = (height-((margin +6)*(status .Count +1))) / (status.Count+1);
                powerShow.VerticalContentAlignment = VerticalAlignment.Center;
                powerShow.HorizontalContentAlignment = HorizontalAlignment.Center;
                powerShow.FontSize = powerShow.Height/ 3;
                powerShow.Margin = new Thickness(margin + 3);
                powerShow.BorderThickness = new Thickness(3);
                powerShow.IsReadOnly = true;
                if (status[i].Prefix.Length > 0)
                    powerShow.Text = status[i].Prefix + ": " + status[i].Power;
                else
                    powerShow.Text = status[i].Power;
                pannel.Children.Add(powerShow);
            }
        }

        public void UpdatePower(List<RealtimePowerInfo> status)
        {
            if(rootGrid.Children.Count==0)
            {
                if(status.Count>0)
                {
                    StackPanel pannel = new StackPanel();
                    pannel.Orientation = Orientation.Vertical;
                    pannel.VerticalAlignment = VerticalAlignment.Stretch;
                    rootGrid.Children.Add(pannel);
                    InitPowerShow(pannel, status);
                }
            }
            else
            {
                StackPanel pannel = (StackPanel)rootGrid.Children[0];
                
                if (pannel.Children.Count < status.Count)
                {
                    pannel.Children.Clear();
                    InitPowerShow(pannel, status);
                }
                else
                {
                    for (int i = 0; i < status.Count; i++)
                    {
                        TextBox powShow = (TextBox)pannel.Children[i];
                        if (status[i].Prefix.Length > 0)
                            powShow.Text = status[i].Prefix + ": " + status[i].Power;
                        else
                            powShow.Text = status[i].Power;
                    }
                }
            }
        }

        private void RealtimePower_Loaded(object sender, RoutedEventArgs e)
        {
            Compose();
            PowerUpdateRegister();
        }
    }
}
