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

using ProtocolAggregator;
using MolexUtility;
using MolexUtility.UIList;


namespace TestDetailShow
{
    /// <summary>
    /// Interaction logic for PD1X8Show.xaml
    /// </summary>
     
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "ParamList")]
    public partial class PD1X8Show : UserControl
    {
        /// <summary>
        /// 将触发者容器注入
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }
        public List<ShowContent> AllShowContent { get; set; }

        public List<IndexMap> MapDetail { get; set; }

        private UIParamShow showControl { get; set; }

        private System.Windows.Forms.DataGridView testDataShow;
        public PD1X8Show()
        {
            InitializeComponent();
        }

        private void Compose()
        {
            var catalog = new DirectoryCatalog(Environment.CurrentDirectory + "\\..\\module");
            CompositionContainer container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        /// <summary>
        /// 与插件通信，将传进的模板信息进行显示
        /// </summary>
        private void TemplateUpdateRegerster()
        {
            EventAggregator.GetEvent<EventTemplateUpdate>().Subscribe
                (
                    info =>
                    {
                        UpdateTestList(info);
                    }
                );
        }

        private void UpdateTestList(List<MESControl> info)
        {
            AllShowContent.Clear();
            MapDetail.Clear();
            int rowIndex = 0;
            int productIndex = 0;
            foreach (MESControl control in info)
            {
                ShowContent content = new ShowContent();
                content.TestInfo = control.GetAllTestInfo();
                ColumnMap columnSN = new ColumnMap();
                columnSN.Name = "SN";
                columnSN.Value= control.ProductSN;
                content.Addition.Add(columnSN);
                AllShowContent.Add(content);
                int paramIndex = 0;
                foreach(MESTestInfo test in content.TestInfo)
                {
                    IndexMap indexInfo = new IndexMap();
                    indexInfo.ProductIndex = productIndex;
                    indexInfo.RowIndex = rowIndex;
                    indexInfo.ParamIndex.Add(paramIndex);
                    MapDetail.Add(indexInfo);
                    paramIndex++;
                    rowIndex++;
                }
                productIndex++;

                //产品与产品之间增加一空行，空行对应的产品和参数index为-1
                IndexMap indexEmpty = new IndexMap();
                indexEmpty.ProductIndex = -1;
                indexEmpty.RowIndex = rowIndex;
                indexEmpty.ParamIndex.Add(-1);
                MapDetail.Add(indexEmpty);
                rowIndex++;
            }
            showControl.InitView(AllShowContent, MapDetail);


        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            testDataShow = new System.Windows.Forms.DataGridView();
            TestDetailDataGrid.Child = testDataShow;
            showControl = new UIParamShow(testDataShow);
            AllShowContent = new List<ShowContent>();
            MapDetail = new List<IndexMap>();
            Compose();
            TemplateUpdateRegerster();
        }
    }
}
