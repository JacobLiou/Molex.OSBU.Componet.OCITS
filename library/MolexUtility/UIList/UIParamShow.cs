using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MolexUtility;

namespace MolexUtility.UIList
{
    /// <summary>
    /// datagridview显示处理
    /// </summary>
    public class UIParamShow
    {
        /// <summary>
        /// datagridview对象
        /// </summary>
        private DataGridView paramShow = null;

        private List<string> commonLastColumns = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataShow">datagridview对象</param>
        public UIParamShow(DataGridView dataShow)
        {
            paramShow = dataShow;
            dataShow.RowsDefaultCellStyle.BackColor = System.Drawing.Color.Gray;
        }



        /// <summary>
        /// 初始化datagridview
        /// </summary>
        /// <param name="allTestData">所有产品的测试信息</param>
        /// <param name="mapDetail">datagridview列和产品测试信息对应关系</param>
        /// <param name="commonFront">参数列之前需要显示的列，支持"温度"、"波长"、"PORT"、"ITEM"、"IL REF"、"RL REF"</param>
        /// <param name="commonBehind">参数列之后显示的列，支持"温度"、"波长"、"PORT"、"ITEM"、"IL REF"、"RL REF"</param>
        public void InitView(List<ShowContent> allTestData, List<IndexMap> mapDetail,List<string> commonFront=null,List<string> commonBehind=null)
        {
            commonLastColumns = commonBehind;
            //ClearAllData(ref dataShow);
            paramShow.Rows.Clear();
            paramShow.Columns.Clear();
            //去掉最后一行“*”
            paramShow.AllowUserToAddRows = false;
            paramShow.AllowUserToDeleteRows = false;
            paramShow.ReadOnly = true;
            
            paramShow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            paramShow.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            paramShow.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            paramShow.ColumnHeadersHeight = 40;
            paramShow.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Raised;

            paramShow.MultiSelect = false;

            if (allTestData.Count == 0)
                return;
            if(commonFront==null)
            {
                commonFront = new List<string>();
                commonFront.Add("温度");
                commonFront.Add("对象");
                commonFront.Add("波长");
                commonFront.Add("PORT");
                commonFront.Add("ITEM");
                commonFront.Add("范围");
                commonFront.Add("IL REF");
                commonFront.Add("RL REF");
            }
            int nColumnIndex = -1;
            DataGridViewColumn column = null;
            //参数前面的列
            foreach (string front in commonFront)
            {
                if (front == "归零状态")
                {
                    DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                    checkBoxColumn.HeaderText = front;
                    checkBoxColumn.Name = front;
                    nColumnIndex = paramShow.Columns.Add(checkBoxColumn);
                }
                else
                {
                    nColumnIndex = paramShow.Columns.Add(front, front);
                }
                column = paramShow.Columns[nColumnIndex];
                column.MinimumWidth = 50;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            }

            

            //额外增加显示信息以第一个产品信息为准，原则上多个产品测试时必须为同一产品，所以不会冲突
            int additionCount = allTestData[0].Addition.Count;
            int behindCount = 0;

            foreach (ColumnMap map in allTestData[0].Addition)
            {
                nColumnIndex = paramShow.Columns.Add(map.Name, map.Name);
                column = paramShow.Columns[nColumnIndex];
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            //参数后面的列
            if (commonBehind != null)
            {
                foreach (string behind in commonBehind)
                {
                    nColumnIndex = paramShow.Columns.Add(behind, behind);
                    column = paramShow.Columns[nColumnIndex];
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                behindCount = commonBehind.Count;
            }

            IndexMap selMap = new IndexMap();
            selMap.RowIndex = 0;

            foreach (IndexMap map in mapDetail)
            {
                int rowIndex = paramShow.Rows.Count;
                if (map.ProductIndex == -1 || map.ParamIndex.Count==0)
                    InsertEmptyRow(rowIndex - 1);
                else
                {
                    InsertRow(allTestData[map.ProductIndex], map, additionCount+ behindCount);
                    //InsertRow(rowIndex - 1, allTestData[map.ProductIndex].TestInfo[map.ParamIndex], additionCount);
                    //额外信息的显示
                    InsertAddition(rowIndex, allTestData[map.ProductIndex].Addition);
                }
                if(selMap.RowIndex==map.RowIndex)
                {
                    selMap.ParamIndex = map.ParamIndex;
                    selMap.ProductIndex = map.ProductIndex;
                }
            }

            
            
            paramShow.Columns.Add("", "");
            column = paramShow.Columns[paramShow.Columns.Count - 1];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            

            if (paramShow.Rows.Count > 0)
            {
                paramShow.Rows[0].Selected = true;
                paramShow.CurrentCell = paramShow.Rows[selMap.RowIndex].Cells[0];
                
            }
        }

        /// <summary>
        /// 移除从beginIndex到endIndex行
        /// </summary>
        /// <param name="beginIndex">需要移除的起始行</param>
        /// <param name="endIndex">需要移除的最后行</param>
        public void Remove(int beginIndex, int endIndex)
        {
            //移除从beginIndex到endIndex的行
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            for (int i = beginIndex; i <= endIndex; i++)
            {
                rows.Add(paramShow.Rows[i]);
            }

            //修改选中行，如果删除的产品后面还有产品，则选中后面产品测试项第一行，否则选中第一行。
            //避免没删除一行选中行都有变化，模块之间通信频繁，增加耗时
            if (paramShow.Rows.Count> (endIndex+1))
                paramShow.Rows[endIndex+1].Selected = true;
            else if(beginIndex>0)
                paramShow.Rows[0].Selected = true;

            foreach (DataGridViewRow row in rows)
            {
                paramShow.Rows.Remove(row);
            }
        }


        /// <summary>
        /// 增加行
        /// </summary>
        /// <param name="allTestData">需要增加的行的具体信息</param>
        /// <param name="mapDetail">行与产品信息之间映射关系</param>
        public void AddRows(List<ShowContent> allTestData, List<IndexMap> mapDetail)
        {
            //额外增加显示信息以第一个产品信息为准，原则上多个产品测试时必须为同一产品，所以不会冲突
            int additionCount = allTestData[0].Addition.Count;

            foreach (IndexMap map in mapDetail)
            {
                int rowIndex = paramShow.Rows.Count;
                if (map.ProductIndex == -1 || map.ParamIndex.Count==0)
                    InsertEmptyRow(rowIndex - 1);
                else
                {
                    int behindCount = 0;
                    if(commonLastColumns!=null)
                    {
                        behindCount = commonLastColumns.Count;
                    }
                    InsertRow(allTestData[map.ProductIndex], map, additionCount + behindCount);
                    //InsertRow(rowIndex - 1, allTestData[map.ProductIndex].TestInfo[map.ParamIndex], additionCount);
                    //额外信息的显示
                    InsertAddition(rowIndex, allTestData[map.ProductIndex].Addition);
                }
            }
            
        }

        /// <summary>
        /// 插入附加的信息
        /// </summary>
        /// <param name="index">从index行开始</param>
        /// <param name="addition">增加的列和内容的对应关系</param>
        private void InsertAddition(int index,List<ColumnMap> addition)
        {
            DataGridViewRow row = paramShow.Rows[index];
            foreach (ColumnMap map in addition)
            {
                row.Cells[map.Name].Value = map.Value;
            }
        }

        /// <summary>
        /// 在index之后插入空行
        /// </summary>
        /// <param name="nIndex">行序号</param>
        public void InsertEmptyRow(int nIndex)
        {
            paramShow.Rows.Insert(nIndex + 1, 1);
        }

        /// <summary>
        /// 在index之后插入行
        /// </summary>
        /// <param name="nIndex">行序号</param>
        /// <param name="newTestInfo">需要插入的行信息</param>
        /// <param name="additionCount">附加的列数量，如果有新的参数项，在附加列之前增加</param>
        public void InsertRow(int nIndex, MESTestInfo newTestInfo,int additionCount)
        {
            paramShow.Rows.Insert(nIndex+1, 1);
            if (newTestInfo.TestParam == MESParam.Default)
                return;
            DataGridViewColumnCollection columns = paramShow.Columns;
            DataGridViewRow row = paramShow.Rows[nIndex+1];
            if(columns.Contains("温度"))
                row.Cells["温度"].Value = newTestInfo.TemperStr.ToString();
            //单点，还是波段
            if (columns.Contains("波长")&&newTestInfo.WLLeft.CompareTo(0.0) != 0)
            {
                if ((newTestInfo.WLLeft - newTestInfo.WLRight).CompareTo(0.0) != 0)
                {
                    row.Cells["波长"].Value = string.Format("{0:0.000}~{1:0.000}", newTestInfo.WLLeft, newTestInfo.WLRight);
                }
                else
                    row.Cells["波长"].Value = newTestInfo.WLLeft.ToString("#0.000");
            }
            if(columns.Contains("PORT"))
                row.Cells["PORT"].Value = newTestInfo.PortNameForUser;
            if (columns.Contains("对象"))
                row.Cells["对象"].Value = newTestInfo.ObjectID;
            if (columns.Contains("ITEM"))
            {
                //ex规则
                if (newTestInfo.ParamType == MESParamRule.EX)
                {
                    row.Cells["ITEM"].Value = newTestInfo.ExParamName;
                }
                else //if (newTestInfo.ParamType != MESParamRule.Default)
                {
                    //shift BW等参数
                    if (newTestInfo.SettingValue.CompareTo(0.0) != 0)
                        row.Cells["ITEM"].Value = string.Format("{0} @ {1:0.000}", newTestInfo.TestParam.GetMESTemplateKeywords(), newTestInfo.SettingValue);
                    else
                        row.Cells["ITEM"].Value = newTestInfo.TestParam.GetMESTemplateKeywords();
                }
            }

            if (columns.Contains("归零状态"))
            {
                DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)row.Cells["归零状态"];
                checkCell.Value = newTestInfo.IsScanRef;
            }

            //参数范围
            if (columns.Contains("范围"))
            {
                string result = "";
                string portName = "";
                portName = newTestInfo.TestParam.GetMESTemplateKeywords();

                double criterion = Convert.ToDouble(newTestInfo.Criterion);
                double criterion1 = Convert.ToDouble(newTestInfo.Criterion1);
                /*if ((Math.Abs(criterion)) > 9999 || (Math.Abs(criterion) >= 1000))
                {
                    criterion = -Math.Abs(criterion) / 1000000.0;
                }
                else
                    criterion = Math.Abs(criterion);

                if ((Math.Abs(criterion1)) > 9999 || (Math.Abs(criterion1) >= 1000))
                {
                    criterion1 = -Math.Abs(criterion1) / 1000000.0;
                }
                else
                    criterion1 = Math.Abs(criterion1);*/

                //正号为>=,负号为<=
                /*if (newTestInfo.Criterion.Substring(0, 1) == "-")
                {
                    if (criterion != CommonFunction.GetDefaultValue() && criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + criterion1 + "≤" + portName + "≤" + criterion + ")";
                    else if (criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≥" + criterion1 + ")";
                    else if (criterion != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≤" + criterion1 + ")";

                }
                else if (newTestInfo.Criterion1.Substring(0, 1) == "-")
                {*/
                    if (criterion != CommonFunction.GetDefaultValue() && criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + criterion + "≤" + portName + "≤" + criterion1 + ")";
                    else if (criterion != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≥" + criterion + ")";
                    else if (criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≤" + criterion1 + ")";
                //}
                row.Cells["范围"].Value = result;
            }

            //不存在测试项对应列，则增加
            //DataGridViewColumnCollection columns = paramShow.Columns;
            if ((newTestInfo.TestParam!=MESParam.Default)&&!columns.Contains(newTestInfo.ParamColumnName))
            {
                DataGridViewColumn column = new DataGridViewColumn(row.Cells["温度"]);
                column.Name = newTestInfo.ParamColumnName;
                column.HeaderText = newTestInfo.ParamColumnName;
                column.MinimumWidth = 80;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                paramShow.Columns.Insert(paramShow.Columns.Count - additionCount, column);
                //int nColumnIndex = paramShow.Columns.Add(newTestInfo.ParamColumnName, newTestInfo.ParamColumnName);
                //DataGridViewColumn column = paramShow.Columns[nColumnIndex];
            }
            row.Cells[newTestInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.SkyBlue;
            //测试过的数据是否显示，从无纸化下载的
            //if (bShowData)
            {
                if (newTestInfo.TestedValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.TestedValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                {
                    if (newTestInfo.Pass)
                    {
                        row.Cells[newTestInfo.ParamColumnName].Style.ForeColor = System.Drawing.Color.Black;
                        row.Cells[newTestInfo.ParamColumnName].Value = newTestInfo.TestedValue.ToString("#0.000");
                    }
                    else
                    {
                        row.Cells[newTestInfo.ParamColumnName].Style.ForeColor = System.Drawing.Color.Red;
                        row.Cells[newTestInfo.ParamColumnName].Value = string.Format("*{0:0.000}", newTestInfo.TestedValue);
                    }
                    row.Cells[newTestInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.White;
                }
            }

            if (columns.Contains("IL REF"))
            {
                if (newTestInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["IL REF"].Value = newTestInfo.ILRef.ToString("#0.000");
                if (newTestInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["RL REF"].Value = newTestInfo.RLRef.ToString("#0.000");
            }
        }


        /// <summary>
        /// 在index之后插入行
        /// </summary>
        /// <param name="nIndex">行序号</param>
        /// <param name="newTestInfo">需要插入的行信息</param>
        /// <param name="additionCount">附加的列数量，如果有新的参数项，在附加列之前增加</param>
        public void InsertRow(ShowContent showData, IndexMap map, int additionCount)
        {
            if (map.ProductIndex == -1 && map.ParamIndex.Count == 0)
                return;
            //显示在同一行的波长、端口这些必须一致，所以公共项以第一项为准
            MESTestInfo newTestInfo = showData.TestInfo[map.ParamIndex[0]];
            paramShow.Rows.Insert(map.RowIndex, 1);
            if (newTestInfo.TestParam == MESParam.Default)
                return;
            DataGridViewRow row = paramShow.Rows[map.RowIndex];
            DataGridViewColumnCollection columns = paramShow.Columns;
            if (columns.Contains("温度"))
                row.Cells["温度"].Value = newTestInfo.TemperStr.ToString();
            //单点，还是波段
            if (columns.Contains("波长") && newTestInfo.WLLeft.CompareTo(0.0) != 0)
            {
                if ((newTestInfo.WLLeft - newTestInfo.WLRight).CompareTo(0.0) != 0)
                {
                    row.Cells["波长"].Value = string.Format("{0:0.000}~{1:0.000}", newTestInfo.WLLeft, newTestInfo.WLRight);
                }
                else
                    row.Cells["波长"].Value = newTestInfo.WLLeft.ToString("#0.000");
            }
            if (columns.Contains("PORT"))
                row.Cells["PORT"].Value = newTestInfo.PortNameForUser;
            if (columns.Contains("对象"))
                row.Cells["对象"].Value = newTestInfo.ObjectID;
            if (columns.Contains("归零状态"))
            {
                DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)row.Cells["归零状态"];
                checkCell.Value = newTestInfo.IsScanRef;
            }


            string paramTotal = "";
            foreach (int index in map.ParamIndex)
            {
                MESTestInfo testInfo = showData.TestInfo[index];

                //ex规则
                if (testInfo.ParamType == MESParamRule.EX)
                {
                    paramTotal += testInfo.ExParamName;
                    paramTotal += "  ";
                    //row.Cells["Param"].Value = newTestInfo.PortNameForUser;
                }
                else //if (testInfo.ParamType != MESParamRule.Default)
                {
                    //shift BW等参数
                    if (testInfo.SettingValue.CompareTo(0.0) != 0)
                    {
                        paramTotal += string.Format("{0} @ {1:0.000}", testInfo.TestParam.GetMESTemplateKeywords(), testInfo.SettingValue); ;
                        paramTotal += "  ";
                    }
                    //row.Cells["Param"].Value = string.Format("{0} @ {1:0.000}", newTestInfo.TestParam.GetMESTemplateKeywords(), newTestInfo.SettingValue);
                    else
                    {
                        paramTotal += testInfo.TestParam.GetMESTemplateKeywords();
                        paramTotal += "  ";
                    }
                        //row.Cells["Param"].Value = newTestInfo.TestParam.GetMESTemplateKeywords();
                }
                //不存在测试项对应列，则增加               
                if (!columns.Contains(testInfo.ParamColumnName))
                {
                    DataGridViewColumn column = new DataGridViewColumn(row.Cells["温度"]);
                    column.Name = testInfo.ParamColumnName;
                    column.HeaderText = testInfo.ParamColumnName;
                    column.MinimumWidth = 80;
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    paramShow.Columns.Insert(paramShow.Columns.Count - additionCount, column);
                    //int nColumnIndex = paramShow.Columns.Add(newTestInfo.ParamColumnName, newTestInfo.ParamColumnName);
                    //DataGridViewColumn column = paramShow.Columns[nColumnIndex];
                }
                row.Cells[testInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.SkyBlue;
                //测试过的数据是否显示，从无纸化下载的
                //if (bShowData)
                {
                    if (testInfo.TestedValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.TestedValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    {
                        if (testInfo.Pass)
                        {
                            row.Cells[testInfo.ParamColumnName].Style.ForeColor = System.Drawing.Color.Black;
                            row.Cells[testInfo.ParamColumnName].Value = testInfo.TestedValue.ToString("#0.000");
                        }
                        else
                        {
                            row.Cells[testInfo.ParamColumnName].Style.ForeColor = System.Drawing.Color.Red;
                            row.Cells[testInfo.ParamColumnName].Value = string.Format("*{0:0.000}", testInfo.TestedValue);
                        }
                        row.Cells[testInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.White;
                    }
                }
            }

            if(columns.Contains("ITEM"))
                row.Cells["ITEM"].Value = paramTotal;

            //参数范围
            if (columns.Contains("范围"))
            {
                string result = "";
                string portName = "";
                portName = newTestInfo.TestParam.GetMESTemplateKeywords();

                double criterion = Convert.ToDouble(newTestInfo.Criterion);
                double criterion1 = Convert.ToDouble(newTestInfo.Criterion1);
                /*if ((Math.Abs(criterion)) > 9999 || (Math.Abs(criterion) >= 1000))
                {
                    criterion = -Math.Abs(criterion) / 1000000.0;
                }
                else
                    criterion = Math.Abs(criterion);

                if ((Math.Abs(criterion1)) > 9999 || (Math.Abs(criterion1) >= 1000))
                {
                    criterion1 = -Math.Abs(criterion1) / 1000000.0;
                }
                else
                    criterion1 = Math.Abs(criterion1);

                //正号为>=,负号为<=
                if (newTestInfo.Criterion.Substring(0, 1) == "-")
                {
                    if (criterion != CommonFunction.GetDefaultValue() && criterion1 != CommonFunction.GetDefaultValue())
                        result ="(" + criterion1 + "≤" + portName + "≤" + criterion + ")";
                    else if (criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≥" + criterion1 + ")";
                    else if(criterion !=CommonFunction.GetDefaultValue ())
                        result = "(" + portName + "≤" + criterion1 + ")";

                }
                else if (newTestInfo.Criterion1.Substring(0, 1) == "-")
                {*/
                    if (criterion != CommonFunction.GetDefaultValue() && criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + criterion + "≤" + portName + "≤" + criterion1 + ")";
                    else if (criterion != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≥" + criterion + ")";
                    else if (criterion1 != CommonFunction.GetDefaultValue())
                        result = "(" + portName + "≤" + criterion1 + ")";
                //}
                row.Cells["范围"].Value = result;
            }

            if (columns.Contains("IL REF"))
            {
                if (newTestInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["IL REF"].Value = newTestInfo.ILRef.ToString("#0.000");
                if (newTestInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["RL REF"].Value = newTestInfo.RLRef.ToString("#0.000");
            }
        }

        /// <summary>
        /// 将测试项参数列按优先顺序重排；锚点列（如 TDL）与优先列置于参数区最前，其余列保持原相对顺序。
        /// </summary>
        /// <param name="priorityColumnNames">优先列名（按显示顺序，支持 MaxIL 匹配 MaxIL@ITU 等）</param>
        /// <param name="leadingFixedCount">左侧固定列数量（如温度、PORT）</param>
        /// <param name="trailingFixedCount">右侧固定列数量（如 SN、末尾填充列）</param>
        /// <param name="insertAfterColumnName">锚点列名（如 TDL）；为 null 时仅将优先列排在参数区最前</param>
        public void ReorderParamColumns(IReadOnlyList<string> priorityColumnNames, int leadingFixedCount, int trailingFixedCount, string insertAfterColumnName = null)
        {
            if (paramShow == null || priorityColumnNames == null || priorityColumnNames.Count == 0)
                return;

            DataGridViewColumnCollection columns = paramShow.Columns;
            int paramDisplayEnd = columns.Count - trailingFixedCount;
            if (paramDisplayEnd <= leadingFixedCount)
                return;

            var paramColumns = columns.Cast<DataGridViewColumn>()
                .Where(c => c.DisplayIndex >= leadingFixedCount && c.DisplayIndex < paramDisplayEnd)
                .OrderBy(c => c.DisplayIndex)
                .ToList();
            if (paramColumns.Count == 0)
                return;

            var priorityColumns = new List<DataGridViewColumn>();
            var prioritySet = new HashSet<DataGridViewColumn>();
            foreach (string name in priorityColumnNames)
            {
                foreach (DataGridViewColumn col in paramColumns)
                {
                    if (prioritySet.Contains(col))
                        continue;
                    if (ColumnMatchesParamName(col.Name, name))
                    {
                        priorityColumns.Add(col);
                        prioritySet.Add(col);
                    }
                }
            }

            DataGridViewColumn anchorColumn = null;
            if (!string.IsNullOrEmpty(insertAfterColumnName))
            {
                anchorColumn = paramColumns.FirstOrDefault(c => ColumnMatchesParamName(c.Name, insertAfterColumnName));
            }

            var ordered = new List<DataGridViewColumn>();
            if (anchorColumn != null)
            {
                ordered.Add(anchorColumn);
                ordered.AddRange(priorityColumns);
                foreach (DataGridViewColumn col in paramColumns)
                {
                    if (col != anchorColumn && !prioritySet.Contains(col))
                        ordered.Add(col);
                }
            }
            else
            {
                ordered.AddRange(priorityColumns);
                foreach (DataGridViewColumn col in paramColumns)
                {
                    if (!prioritySet.Contains(col))
                        ordered.Add(col);
                }
            }

            if (ordered.Count != paramColumns.Count)
                return;

            // DisplayIndex 必须在 [0, ColumnCount-1] 内；从右到左赋最终位置，避免列互相抢占
            for (int i = ordered.Count - 1; i >= 0; i--)
                ordered[i].DisplayIndex = leadingFixedCount + i;
        }

        private static string GetParamBaseName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return columnName;
            int at = columnName.IndexOf('@');
            if (at >= 0)
                return columnName.Substring(0, at);
            int semi = columnName.IndexOf(';');
            if (semi >= 0)
                return columnName.Substring(0, semi);
            return columnName;
        }

        private static bool ColumnMatchesParamName(string columnName, string paramName)
        {
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(paramName))
                return false;
            return string.Equals(GetParamBaseName(columnName), GetParamBaseName(paramName), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearAllData()
        {

            paramShow.BeginInvoke(new ClearDelegate(ClearDelegateMethod));
        }
        private delegate void ClearDelegate();
        private void ClearDelegateMethod()
        {
            paramShow.Rows.Clear();
            paramShow.Columns.Clear();
        }

        public void ChangeSelect(IndexMap selectMap)
        {
            object[] invokeChartData = new object[1];
            invokeChartData[0] = selectMap;
            
            paramShow.BeginInvoke(new ChangeSelectDelegate(ChangeSelectDelegateMethod), invokeChartData);
        }
        private delegate void ChangeSelectDelegate(IndexMap selectMap);
        private void ChangeSelectDelegateMethod(IndexMap selectMap)
        {
            if (paramShow.RowCount > 0 && paramShow.SelectedRows.Count != 0)
            {
                if (paramShow.SelectedRows[0].Index != selectMap.RowIndex && paramShow.Rows.Count > selectMap.RowIndex)                   
                {
                    paramShow.Rows[selectMap.RowIndex].Selected = true;
                    paramShow.CurrentCell = paramShow.Rows[selectMap.RowIndex].Cells[0];
                }
            }

        }


        /*public void UpdateRefView(int nRowIdx, MESTestInfo testInfo)
        {
            if (testInfo == null)
                return;
            object[] invokeChartData = new object[2];
            invokeChartData[0] = nRowIdx;
            invokeChartData[1] = testInfo;
            paramShow.BeginInvoke(new UpdateDelegate(UpdateRefDelegateMethod), invokeChartData);
        }
        private void UpdateRefDelegateMethod(int nRowIdx, MESTestInfo testInfo)
        {
            if (paramShow.Rows.Count > nRowIdx)
            {
                DataGridViewRow row = paramShow.Rows[nRowIdx];
                 DataGridViewColumnCollection columns = paramShow.Columns;
                 if (!columns.Contains("ILREF") || !columns.Contains("RLREF"))
                    return;
                 if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["ILREF"].Value = testInfo.ILRef.ToString("#0.000");
                 if (testInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    row.Cells["RLREF"].Value = testInfo.RLRef.ToString("#0.000");
            }
        }*/
        private delegate void UpdateDelegate(int nRowIdx, MESTestInfo testInfo);

        /// <summary>
        /// 行数据更新
        /// </summary>
        /// <param name="nRowIdx">需要更新的行</param>
        /// <param name="testInfo">行显示信息</param>
        public void UpdateDataView(int nRowIdx, MESTestInfo testInfo)
        {
            if (testInfo == null)
                return;
            object[] invokeChartData = new object[2];
            invokeChartData[0] = nRowIdx;
            invokeChartData[1] = testInfo;
            paramShow.BeginInvoke(new UpdateDelegate(UpdateTestDateDelegateMethod), invokeChartData);
        }

        /// <summary>
        /// 行更新的实现代码
        /// </summary>
        /// <param name="nRowIdx">需要更新的行</param>
        /// <param name="testInfo">行显示信息</param>
        private void UpdateTestDateDelegateMethod(int nRowIdx, MESTestInfo testInfo)
        {
            if (paramShow.Rows.Count > nRowIdx)
            {
                DataGridViewRow row = paramShow.Rows[nRowIdx];
                DataGridViewColumnCollection columns = paramShow.Columns;
                if (columns.Contains("IL REF") && !columns.Contains("RL REF"))
                {
                    if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["IL REF"].Value = testInfo.ILRef.ToString("#0.000");
                    else
                        row.Cells["IL REF"].Value = "";
                }
                else if (columns.Contains("IL REF") && columns.Contains("RL REF"))
                {
                    if (testInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["IL REF"].Value = testInfo.ILRef.ToString("#0.000");
                    if (testInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                        row.Cells["RL REF"].Value = testInfo.RLRef.ToString("#0.000");
                }

                if (columns.Contains("归零状态"))
                {
                    DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)row.Cells["归零状态"];
                    checkCell.Value = testInfo.IsScanRef;
                }
            }
            
            if (paramShow.Rows.Count > nRowIdx)
            {
                DataGridViewRow row = paramShow.Rows[nRowIdx];
                DataGridViewColumnCollection columns = paramShow.Columns;
                if (!columns.Contains(testInfo.ParamColumnName))
                    return;
                DataGridViewCell cell = row.Cells[testInfo.ParamColumnName];
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
}
