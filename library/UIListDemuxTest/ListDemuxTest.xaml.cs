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
using System.Reflection;
using System.IO.Packaging;
using System.Windows.Markup;
using MolexUtility.UIList;

namespace UIListDemuxTest
{
    /// <summary>
    /// Interaction logic for ListDemuxTest.xaml
    /// </summary>
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIListDemuxTest")]
    public partial class ListDemuxTest : UserControl
    {
        /// <summary>
        /// 将触发者容器注入
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// list显示的所有信息都在此对象中
        /// </summary>
        public List<ShowContent> AllShowContent { get; set; }

        /// <summary>
        /// 已经显示在列表中的产品SN记录
        /// </summary>
        public List<string> ShowSNs { get; set; }

        /// <summary>
        /// 行号显示的对应的产品号，以及参数在ShowContent.TestInfo的index
        /// </summary>
        public List<List<IndexMap>> MapDetail { get; set; }

        /// <summary>
        /// 处理datagridview显示的类
        /// </summary>
        public UIParamShow showControl { get; set; }

        /// <summary>
        /// 列表对象
        /// </summary>
        public System.Windows.Forms.DataGridView testDataShow;
        public ListDemuxTest()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            testDataShow = new System.Windows.Forms.DataGridView();
            //选中行变化
            testDataShow.SelectionChanged += testDataSelectChanged;
            TestDetailDataGrid.Child = testDataShow;
            showControl = new UIParamShow(testDataShow);
            AllShowContent = new List<ShowContent>();
            //MapDetail = new List<IndexMap>();
            MapDetail = new List<List<IndexMap>>();
            //allShowContent = new List<List<ShowContent>>();
            ShowSNs = new List<string>();
            Compose();
            TemplateUpdateRegerster();
            ItemUpdateRegister();
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

        /// <summary>
        /// 显示所有测试信息
        /// </summary>
        /// <param name="info">模板信息</param>
        public void UpdateTestList(List<FusionControl> info)
        {
            try
            {
                int rowIndex = 0;
                MapDetail.Clear();
                ShowSNs.Clear();
                AllShowContent.Clear();

                ShowContent content = new ShowContent();
                content.TestInfo = info[0].GetAllTestInfo();

                List<IndexMap> map = new List<IndexMap>();
                AllShowContent.Add(content);
                int paramIndex = 0;
                IndexMap indexInfo = null;

                List<string> paraTest = new List<string>();
                foreach (MESTestInfo test in content.TestInfo)
                {
                    indexInfo = new IndexMap();
                    indexInfo.ProductIndex = 0;
                    indexInfo.RowIndex = rowIndex;
                    indexInfo.ParamIndex.Add(paramIndex);
                    map.Add(indexInfo.Clone());
                    rowIndex++;
                    paramIndex++;
                    if (!paraTest.Contains(test.ParamColumnName))
                        paraTest.Add(test.ParamColumnName);
                }

                rowIndex++;
                MapDetail.Add(map);
                if (MapDetail.Count == 1)
                {
                    List<string> commonFront = new List<string>();
                    List<string> commonBehind = new List<string>();
                    commonFront.Add("温度");
                    commonFront.Add("波长");
                    commonFront.Add("PORT");
                    commonFront.Add("ITEM");
                    commonFront.Add("范围");
                    commonFront.Add("PM1 IL REF");
                    commonFront.Add("PM2 IL REF");
                    commonFront.Add("PM3 IL REF");
                    commonFront.Add("PM4 IL REF");
                    for (int i = 0; i < paraTest.Count; i++)
                    {
                        commonBehind.Add(paraTest[i]);
                    }
                    showControl.InitView(AllShowContent, map, commonFront, commonBehind);
                }
                testDataSelectChanged(this, null);
            }
            catch (Exception ex)
            {
                string errMsg = "";
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                            + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                CommonFunction.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// 监控EventListItemUpdateDemux事件，更新行信息
        /// </summary>
        private void ItemUpdateRegister()
        {
            EventAggregator.GetEvent<EventListItemUpdateDemux>().Subscribe
                (
                    info =>
                    {
                        UpdateItem(info);
                    }
                );
        }

        /// <summary>
        /// 收到EventListItemUpdateDemux事件后更新行处理函数
        /// </summary>
        /// <param name="content">更新行信息相关内容</param>
        private void UpdateItem(ItemDemuxContent content)
        {
            if (content == null)
                return;
            //单行更新，所以ParamIndex参数行序号只取第一项
            IndexMap updateMap = content.UpdateItemMap;
            IndexMap nextMap = content.NextSelectMap;
            int updateIndex = -1;
            if (updateMap.ParamIndex.Count > 0)
                updateIndex = updateMap.ParamIndex[0];
            bool isFind = false;
            bool isSelectFind = false;
            foreach (List<IndexMap> maps in MapDetail)
            {
                foreach (IndexMap map in maps)
                {
                    //找到需要更新的信息对应行
                    if (map.ProductIndex == updateMap.ProductIndex)
                    {
                        foreach (int index in map.ParamIndex)
                        {
                            if (updateIndex == index)
                            {
                                updateMap.RowIndex = map.RowIndex;
                                isFind = true;
                                break;
                            }
                        }

                    }

                    //找到需要选中的行
                    if (nextMap != null && nextMap.ParamIndex.Count > 0)
                    {
                        if (map.ProductIndex == nextMap.ProductIndex)
                        {
                            foreach (int index in map.ParamIndex)
                            {
                                if (nextMap.ParamIndex[0] == index)
                                {
                                    nextMap.RowIndex = map.RowIndex;
                                    isSelectFind = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                        isSelectFind = true;

                    if (isFind && isSelectFind)
                        break;
                }
                if (isFind && isSelectFind)
                    break;
            }
            //找到对应的行号，更新内容
            if (updateMap.RowIndex != -1)
            {
                UpdateDataView(updateMap.RowIndex, content.TestInfo,content.Offset1,content.Offset2,content.Offset3);
            }

            //改变选中行
            if (nextMap != null && nextMap.ParamIndex.Count > 0 && isSelectFind)
                showControl.ChangeSelect(nextMap);
        }

        /// <summary>
        /// datagridview选中行变化事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void testDataSelectChanged(object sender, EventArgs e)
        {
            if (testDataShow.RowCount > 0 && testDataShow.SelectedRows.Count != 0)
            {
                int selectIndex = testDataShow.SelectedRows[0].Index;
                if (selectIndex < 0)
                    return;
                // 查找选中行对应的信息 
                IndexMap selectMap = null;
                foreach (List<IndexMap> maps in MapDetail)
                {
                    foreach (IndexMap map in maps)
                    {
                        if (selectIndex == map.RowIndex)
                            selectMap = map.Clone();

                    }
                }
                if (selectMap != null)
                {
                    EventAggregator.GetEvent<EventListSelectChanged>().Publish(selectMap);
                }
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            KeyDownInfo keyInfo = new KeyDownInfo();
            keyInfo.Key = e.Key;
            EventAggregator.GetEvent<EventListKeyDown>().Publish(keyInfo);
        }
        private delegate void UpdateDelegate(int index, MESTestInfo testInfo, double offset1, double offset2, double offset3);
        /// <summary>
        /// 行数据更新
        /// </summary>
        /// <param name="nRowIdx">需要更新的行</param>
        /// <param name="testInfo">行显示信息</param>
        public void UpdateDataView(int nRowIdx, MESTestInfo testInfo, double offset1,double offset2,double offset3)
        {
            if (testInfo == null)
                return;
            object[] invokeChartData = new object[5];
            invokeChartData[0] = nRowIdx;
            invokeChartData[1] = testInfo;
            invokeChartData[2] = offset1;
            invokeChartData[3] = offset2;
            invokeChartData[4] = offset3;
            testDataShow.BeginInvoke(new UpdateDelegate(UpdateTestDateDelegateMethod), invokeChartData);
        }

        /// <summary>
        /// 行更新的实现代码
        /// </summary>
        /// <param name="nRowIdx">需要更新的行</param>
        /// <param name="testInfo">行显示信息</param>
        private void UpdateTestDateDelegateMethod(int nRowIdx, MESTestInfo testInfo,double offset1,double offset2,double offset3)
        {
            if (testDataShow.Rows.Count > nRowIdx)
            {
                System.Windows.Forms. DataGridViewRow row = testDataShow.Rows[nRowIdx];
               System.Windows.Forms. DataGridViewColumnCollection columns = testDataShow.Columns;
                if (!columns.Contains("ILREF") && !columns.Contains("RLREF"))
                {
                    if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["PM1 IL REF"].Value = testInfo.ILRef.ToString("#0.000");
                    else
                        row.Cells["PM1 IL REF"].Value = "";
                    if (offset1.CompareTo(CommonFunction.GetDefaultValue()) != 0 && offset1.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["PM2 IL REF"].Value = offset1.ToString("#0.000");
                    else
                        row.Cells["PM2 IL REF"].Value = "";
                    if (offset2.CompareTo(CommonFunction.GetDefaultValue()) != 0 && offset2.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["PM3 IL REF"].Value = offset2.ToString("#0.000");
                    else
                        row.Cells["PM3 IL REF"].Value = "";
                    if (offset3.CompareTo(CommonFunction.GetDefaultValue()) != 0 && offset3.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["PM4 IL REF"].Value = offset3.ToString("#0.000");
                    else
                        row.Cells["PM4 IL REF"].Value = "";
                }
                else if (columns.Contains("ILREF") && !columns.Contains("RLREF"))
                {
                    if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["ILREF"].Value = testInfo.ILRef.ToString("#0.000");
                    else
                        row.Cells["ILREF"].Value = "";
                }
                else
                {
                    if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["ILREF"].Value = testInfo.ILRef.ToString("#0.000");
                    if (testInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["RLREF"].Value = testInfo.RLRef.ToString("#0.000");
                }
            }

            if (testDataShow.Rows.Count > nRowIdx)
            {
               System.Windows.Forms. DataGridViewRow row = testDataShow.Rows[nRowIdx];
               System.Windows.Forms. DataGridViewColumnCollection columns = testDataShow.Columns;
                if (!columns.Contains(testInfo.ParamColumnName))
                    return;
               System.Windows.Forms. DataGridViewCell cell = row.Cells[testInfo.ParamColumnName];
                if (testInfo.CurValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.CurValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                {
                    if (testInfo.Pass)
                    {
                        cell.Style.ForeColor = System.Drawing.Color.Black;
                        cell.Value = testInfo.CurValue.ToString();
                    }
                    else
                    {
                        cell.Style.ForeColor = System.Drawing.Color.Red;
                        cell.Value = string.Format("*{0:0.000}", testInfo.CurValue);
                    }
                    row.Cells[testInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.White;
                }
                else
                {
                    cell.Value = "";
                    row.Cells[testInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.SkyBlue;
                }


            }
            else
            {

            }
        }
    }


    static class Extension
    {
        public static void LoadViewFromUri(this UserControl userControl, string baseUri)
        {
            try
            {
                var resourceLocater = new Uri(baseUri, UriKind.Relative);
                var exprCa = (PackagePart)typeof(Application).GetMethod("GetResourceOrContentPart", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { resourceLocater });
                var stream = exprCa.GetStream();
                var uri = new Uri((Uri)typeof(BaseUriHelper).GetProperty("PackAppBaseUri", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), resourceLocater);
                var parserContext = new ParserContext
                {
                    BaseUri = uri
                };
                typeof(XamlReader).GetMethod("LoadBaml", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { stream, parserContext, userControl, true });
            }
            catch (Exception)
            {
                //log
            }
        }
    }
        
}
