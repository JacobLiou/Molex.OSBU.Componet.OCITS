using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility;
using MolexUtility.UIList;
using System.ComponentModel.Composition;
using UIListCommon;
using System.Windows.Controls;

namespace UIListDemuxAdjust
{
    [Export(typeof(UserControl))]
    [ExportMetadata("name", "UIListDemuxAdjust")]
    public class ListDemuxAdjust : ListCommon
    {
        /// <summary>
        /// 显示所有测试信息
        /// </summary>
        /// <param name="info">模板信息</param>
        public override void UpdateTestList(List<FusionControl> info)
        {
            try
            {
                int rowIndex = 0;
                MapDetail.Clear();
               
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
                    commonFront.Add("ILREF");
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
    }
}
