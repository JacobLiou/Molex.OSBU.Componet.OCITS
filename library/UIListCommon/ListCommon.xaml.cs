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
using System.Reflection;
using System.IO.Packaging;
using System.Windows.Markup;

///<summary>
///文件名：ListCommon.xaml.cs
///作用：显示参数列表基础类，公共的功能，与其他模块通信的定义。接收信息更新，通知选中行信息。
///作者：阮锦芳
///编写日期：2018-04-19
///修改记录
///R1：
///		修改作者：作者中文名
///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
///		修改内容：xxx
///</summary>

namespace UIListCommon
{
    /// <summary>
    /// Interaction logic for CommonParamList.xaml
    /// </summary>
    
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIListCommon")]
    public partial class ListCommon : UserControl
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
        public ListCommon()
        {
            this.LoadViewFromUri("/UIListCommon;component/ListCommon.xaml");
            //InitializeComponent();
        }

        
        /// <summary>
        /// usercontrol加载函数，初始化定义成员变量等
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        public  virtual void UpdateTestList(List<FusionControl> info)
        {

            //AllShowContent.Clear();
            //MapDetail.Clear();
            int rowIndex = 0;
            int productIndex = -1;
            
            foreach (FusionControl control in info)
            {
                productIndex++;
                if (ShowSNs.Count > productIndex)
                {
                    //如果记录的SN与需要显示的不一致，则删除该SN的测试信息
                    if (ShowSNs[productIndex] != control.ProductSN)
                    {
                        int diff = MapDetail[productIndex][MapDetail[productIndex].Count - 1].RowIndex - MapDetail[productIndex][0].RowIndex + 1;
                        showControl.Remove(MapDetail[productIndex][0].RowIndex, MapDetail[productIndex][MapDetail[productIndex].Count - 1].RowIndex);
                        ShowSNs.RemoveAt(productIndex);
                        AllShowContent.RemoveAt(productIndex);
                        MapDetail.RemoveAt(productIndex);
                        //移除后，产品序号，和行号发生变化，需要修改
                        for (int i = productIndex; i < MapDetail.Count; i++)
                        {
                            foreach (IndexMap map in MapDetail[i])
                            {
                                map.RowIndex = map.RowIndex - diff;
                                if (map.ProductIndex != -1)
                                    map.ProductIndex--;
                            }
                        }
                        //删除后，最大胆行号更新
                        if (MapDetail.Count > 0)
                            rowIndex = MapDetail[MapDetail.Count - 1][MapDetail[MapDetail.Count - 1].Count - 1].RowIndex + 1;
                    }
                    else if (ShowSNs[productIndex] == control.ProductSN) //如果显示与需要显示信息一致，则不做处理
                    {       
                        continue;
                    }
                }
                else
                {
                    ShowContent content = new ShowContent();
                    content.TestInfo = control.GetAllTestInfo();
                    List<IndexMap> map = new List<IndexMap>();
                    AllShowContent.Add(content);
                    ShowSNs.Add(control.ProductSN);
                    int paramIndex = 0;
                    //行与测试项的映射信息
                    foreach (MESTestInfo test in content.TestInfo)
                    {
                        IndexMap indexInfo = new IndexMap();
                        indexInfo.ProductIndex = productIndex;
                        indexInfo.RowIndex = rowIndex;
                        indexInfo.ParamIndex.Add(paramIndex);
                        map.Add(indexInfo);
                        paramIndex++;
                        rowIndex++;
                    }
                    //产品与产品之间增加一空行，空行对应的产品和参数index为-1
                    IndexMap indexEmpty = new IndexMap();
                    indexEmpty.ProductIndex = -1;
                    indexEmpty.RowIndex = rowIndex;
                    //indexEmpty.ParamIndex = -1;
                    map.Add(indexEmpty);
                    rowIndex++;
                    MapDetail.Add(map);
                    if(MapDetail.Count==1)
                    {
                        //第一个产品，则重新初始化
                        showControl.InitView(AllShowContent, map);
                        
                    }
                    else
                    {
                        //第二个产品开始，在列表后增加
                        showControl.AddRows(AllShowContent, map);
                    }
                    
                }
                      
            }

            
            //如果记录的显示的产品需要显示的信息中没有找到，则删除
            int totalCount = ShowSNs.Count;
            for (int i=productIndex+1;i< totalCount; i++)
            {
                showControl.Remove(MapDetail[i][0].RowIndex, MapDetail[i][MapDetail[i].Count - 1].RowIndex);
                ShowSNs.RemoveAt(i);
                MapDetail.RemoveAt(i);
                AllShowContent.RemoveAt(productIndex);
                totalCount = ShowSNs.Count;
            }
            testDataSelectChanged(this, null);
        }

        /// <summary>
        /// 监控EventListItemUpdate事件，更新行信息
        /// </summary>
        private void ItemUpdateRegister()
        {
            EventAggregator.GetEvent<EventListItemUpdate>().Subscribe
                (
                    info =>
                    {
                        UpdateItem(info);
                    }
                );
        }

        /// <summary>
        /// 收到EventListItemUpdate事件后更新行处理函数
        /// </summary>
        /// <param name="content">更新行信息相关内容</param>
        private void UpdateItem(ItemContent content)
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
                        foreach(int index in map.ParamIndex)
                        {
                            if(updateIndex==index)
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

                    if (isFind&& isSelectFind)
                        break;
                }
                if (isFind&& isSelectFind)
                    break;
            }
            //找到对应的行号，更新内容
            if (updateMap.RowIndex != -1)
            {
                showControl.UpdateDataView(updateMap.RowIndex, content.TestInfo);
            }

            //改变选中行
            if (nextMap != null && nextMap.ParamIndex.Count > 0&& isSelectFind)
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
                if (selectIndex < 0|| selectIndex == testDataShow.RowCount-1)
                    return;
                // 查找选中行对应的信息 
                IndexMap selectMap=null;
                foreach (List<IndexMap> maps in MapDetail)
                {
                    foreach (IndexMap map in maps)
                    {
                        if (selectIndex == map.RowIndex)
                            selectMap = map.Clone();

                    }
                }
                if (selectMap!=null)
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
