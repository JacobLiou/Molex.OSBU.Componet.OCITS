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

namespace OCITSAutoUpdate
{
    /// <summary>
    /// Interaction logic for SameProductLine.xaml
    /// </summary>
    public partial class SameProductLine : UserControl
    {
        private StationShowConfig sameLineStations;
        public SameProductLine(StationShowConfig stations)
        {
            InitializeComponent();
            sameLineStations = stations;

            List<StackPanel> stackPanels = new List<StackPanel>();
            stackPanels.Add(stationColum1);
            stackPanels.Add(stationColum2);
            stackPanels.Add(stationColum3);
            stackPanels.Add(stationColum4);
            if (sameLineStations == null || sameLineStations.Stations.Count == 0)
                return;
            for (int i = 0; i < sameLineStations.Stations.Count; i++)
            {
                RadioButton singleStation = new RadioButton();
                Binding checkBinding = new Binding
                {
                    Source = sameLineStations.Stations[i],
                    Path = new PropertyPath("IsSelected"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,

                };
                BindingOperations.SetBinding(singleStation, RadioButton.IsCheckedProperty, checkBinding);
                
                //singleStation.DataContext = sameLineStations.Stations[i];
                
                //同一组只能有一个选中
                singleStation.GroupName = "Global";
                singleStation.Style=(Style)Application.Current.FindResource("RDOButton");
                //singleStation.IsChecked = new Binding("IsSelected");
                
                //singleStation.Style = new Style(Type.GetType("RDOButton"));
                StackPanel childPanel = new StackPanel();
                childPanel.VerticalAlignment = VerticalAlignment.Center;
                Rectangle rect = new Rectangle();
                rect.Height = 50;
                rect.Width = 50;
                rect.Fill = new SolidColorBrush(Color.FromRgb(56, 176, 222));
                TextBlock nameBlock = new TextBlock();
                nameBlock.Text = sameLineStations.Stations[i].Name;
                childPanel.Children.Add(rect);
                childPanel.Children.Add(nameBlock);
                singleStation.Content = childPanel;
                stackPanels[i % 4].Children.Add(singleStation);
            }
        }

        private void scrList_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //int i = 0;
            /*List<StackPanel> stackPanels = new List<StackPanel>();
            stackPanels.Add(stationColum1);
            stackPanels.Add(stationColum2);
            stackPanels.Add(stationColum3);
            stackPanels.Add(stationColum4);
            if (sameLineStations == null || sameLineStations.Stations.Count == 0)
                return;
            for(int i=0;i<sameLineStations.Stations.Count;i++)
            {
                RadioButton singleStation = new RadioButton();
                singleStation.Content = "ceshi 1";
                /*singleStation.Style = new Style(Type.GetType("RDOButton"));
                StackPanel childPanel = new StackPanel();
                Rectangle rect = new Rectangle();
                rect.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                TextBlock nameBlock = new TextBlock();
                nameBlock.Text = sameLineStations.Stations[i].Name;
                childPanel.Children.Add(rect);
                childPanel.Children.Add(nameBlock);
                singleStation.Content = childPanel;*/
                /*stackPanels[i % 4].Children.Add(singleStation);
            }*/
        }
    }
}
