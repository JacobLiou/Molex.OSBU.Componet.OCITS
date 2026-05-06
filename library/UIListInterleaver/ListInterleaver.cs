using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using UIListCommon;
using MolexUtility;
using MolexUtility.UIList;

namespace UIListCommon
{
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIListInterleaver")]
    public class ListInterleaver:ListCommon
    {
        /// <summary>
        /// 是否波长、端口、温度、settingvalue一致
        /// </summary>
        /// <param name="record">记录的测试信息</param>
        /// <param name="current">当前测试信息</param>
        /// <returns>false--不一致， true--一致</returns>
        private bool IsSameRow(MESTestInfo record, MESTestInfo current)
        {
            if (record == null)
                return false;
            if (record.Temperature == current.Temperature && record.WLLeft == current.WLLeft
                && record.WLRight == current.WLRight && record.SettingValue == current.SettingValue
                && record.PortNameForUser == current.PortNameForUser)
                return true;
            return false;
        }

        /// <summary>
        /// 显示所有测试信息
        /// </summary>
        /// <param name="info">模板信息</param>
        public override void UpdateTestList(List<FusionControl> info)
        {

            int rowIndex = 0;
            if (MapDetail.Count > 0)
                rowIndex = MapDetail[MapDetail.Count - 1][MapDetail[MapDetail.Count - 1].Count - 1].RowIndex + 1;
            int productIndex = -1;

            foreach (FusionControl control in info)
            {
                productIndex++;
                if (ShowSNs.Count > productIndex)
                {
                    //如果记录的SN与需要显示的不一致，则需要重新显示
                    if (ShowSNs[productIndex] != control.ProductSN|| info.Count==1)
                    {
                        //showControl.ClearAllData();
                        ShowSNs.Clear();
                        MapDetail.Clear();
                        AllShowContent.Clear();
                        rowIndex = 0;
                        break;
                    }
                    else if (ShowSNs[productIndex] == control.ProductSN) //如果显示与需要显示信息一致，则不做处理
                    {
                        continue;
                    }
                }
            }
            productIndex = -1;
            foreach (FusionControl control in info)
            {
                productIndex++;
                //当只有一个产品时，每次都需要重新显示
                if (ShowSNs.Count > productIndex&& info.Count>1)
                {
                    if (ShowSNs[productIndex] == control.ProductSN) //如果显示与需要显示信息一致，则不做处理
                    {
                        continue;
                    }
                }
                else
                {
                    ShowContent content = new ShowContent();
                    content.TestInfo = control.GetAllTestInfo();

                    //在所有测试项之后增加SN显示
                    ColumnMap columnSN = new ColumnMap();
                    columnSN.Name = "SN";
                    columnSN.Value = control.ProductSN;
                    content.Addition.Add(columnSN);

                    List<IndexMap> map = new List<IndexMap>();
                    AllShowContent.Add(content);
                    ShowSNs.Add(control.ProductSN);
                    int paramIndex = 0;

                    MESTestInfo recInfo = null;

                    IndexMap indexInfo = null;
                    //行与测试项的映射信息
                    //将相同端口名称、相同波长、相同温度，相同settingvalue的项放在同一行
                    foreach (MESTestInfo test in content.TestInfo)
                    {
                        if (!IsSameRow(recInfo, test))
                        {
                            if (indexInfo != null)
                                map.Add(indexInfo.Clone());
                            indexInfo = new IndexMap();
                            indexInfo.ProductIndex = productIndex;
                            indexInfo.RowIndex = rowIndex;
                            indexInfo.ParamIndex.Add(paramIndex);
                            rowIndex++;
                            recInfo = test;
                        }
                        else
                        {
                            if (indexInfo != null)
                                indexInfo.ParamIndex.Add(paramIndex);
                            else
                            {
                                indexInfo = new IndexMap();
                                indexInfo.ProductIndex = productIndex;
                                indexInfo.RowIndex = rowIndex;
                                indexInfo.ParamIndex.Add(paramIndex);
                                rowIndex++;
                            }
                        }

                        paramIndex++;
                    }
                    //最后一个测试项
                    if (indexInfo != null)
                        map.Add(indexInfo.Clone());
                    //产品与产品之间增加一空行，空行对应的产品和参数index为-1
                    IndexMap indexEmpty = new IndexMap();
                    indexEmpty.ProductIndex = -1;
                    indexEmpty.RowIndex = rowIndex;
                    //indexEmpty.ParamIndex = -1;
                    map.Add(indexEmpty);
                    rowIndex++;
                    MapDetail.Add(map);
                    if (MapDetail.Count == 1)
                    {
                        List<string> commonFront = new List<string>();
                        List<string> commonBehind = new List<string>();
                        commonFront.Add("温度");
                        commonFront.Add("对象");
                        commonFront.Add("PORT");
                        commonFront.Add("归零状态");
                        //commonBehind.Add("ITEM");
                        //第一个产品，则重新初始化
                        showControl.InitView(AllShowContent, map, commonFront, commonBehind);
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
            for (int i = productIndex + 1; i < totalCount; i++)
            {
                showControl.Remove(MapDetail[i][0].RowIndex, MapDetail[i][MapDetail[i].Count - 1].RowIndex);
                ShowSNs.RemoveAt(i);
                MapDetail.RemoveAt(i);
                AllShowContent.RemoveAt(productIndex);
                totalCount = ShowSNs.Count;
            }
            testDataSelectChanged(this, null);
        }
    }
}
