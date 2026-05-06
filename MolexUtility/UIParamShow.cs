using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MolexUtility
{
    public class UIParamShow
    {
        private DataGridView paramShow = null;
        public UIParamShow(DataGridView dataShow)
        {
            paramShow = dataShow;
        }
        public void InitView(MESTestInfo[] allTestData)
        {
            //ClearAllData(ref dataShow);
            paramShow.Rows.Clear();
            paramShow.Columns.Clear();
            //去掉最后一行“*”
            paramShow.AllowUserToAddRows = false;
            paramShow.AllowUserToDeleteRows = false;
            paramShow.ReadOnly = true;
            paramShow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            paramShow.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            paramShow.MultiSelect = false;

            if (allTestData.Length == 0)
                return;
            int nColumnIndex = -1;
            nColumnIndex = paramShow.Columns.Add("Temperature", "温度");
            DataGridViewColumn column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            nColumnIndex = paramShow.Columns.Add("Wave", "波长");
            column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            nColumnIndex = paramShow.Columns.Add("Port", "PORT");
            column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            nColumnIndex = paramShow.Columns.Add("Param", "ITEM");
            column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            nColumnIndex = paramShow.Columns.Add("ILREF", "IL REF");
            column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            nColumnIndex = paramShow.Columns.Add("RLREF", "RL REF");
            column = paramShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;

            foreach (MESTestInfo info in allTestData)
            {
                int rowIndex = paramShow.Rows.Count;
                InsertRow(rowIndex-1, info);
            }
            paramShow.Columns.Add("", "");
            column = paramShow.Columns[paramShow.Columns.Count - 1];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        public void InsertRow(int nIndex, MESTestInfo newTestInfo)
        {
            paramShow.Rows.Insert(nIndex+1, 1);
            if (newTestInfo.TestParam == MESParam.Default)
                return;
            DataGridViewRow row = paramShow.Rows[nIndex+1];
            row.Cells["Temperature"].Value = newTestInfo.Temperature.ToString();
            //单点，还是波段
            if (newTestInfo.WLLeft.CompareTo(0.0) != 0)
            {
                if ((newTestInfo.WLLeft - newTestInfo.WLRight).CompareTo(0.0) != 0)
                {
                    row.Cells["Wave"].Value = string.Format("{0:0.000}~{1:0.000}", newTestInfo.WLLeft, newTestInfo.WLRight);
                }
                else
                    row.Cells["Wave"].Value = newTestInfo.WLLeft.ToString("#0.000");
            }
            row.Cells["Port"].Value = newTestInfo.PortNameForUser;
            //ex规则，参数与端口名一致
            if (newTestInfo.ParamType == MESParamRule.EX)
            {
                row.Cells["Param"].Value = newTestInfo.PortNameForUser;
            }
            else if(newTestInfo.ParamType!=MESParamRule.Default)
            {
                //shift BW等参数
                if (newTestInfo.SettingValue.CompareTo(0.0) != 0)
                    row.Cells["Param"].Value = string.Format("{0} @ {1:0.000}", newTestInfo.TestParam.GetMESTemplateKeywords(), newTestInfo.SettingValue);
                else
                    row.Cells["Param"].Value = newTestInfo.TestParam.GetMESTemplateKeywords();
            }
            //不存在测试项对应列，则增加
            DataGridViewColumnCollection columns = paramShow.Columns;
            if ((newTestInfo.TestParam!=MESParam.Default)&&!columns.Contains(newTestInfo.ParamColumnName))
            {
                int nColumnIndex = paramShow.Columns.Add(newTestInfo.ParamColumnName, newTestInfo.ParamColumnName);
                DataGridViewColumn column = paramShow.Columns[nColumnIndex];
                column.MinimumWidth = 80;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

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

            if (newTestInfo.ILRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.ILRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                row.Cells["ILREF"].Value = newTestInfo.ILRef.ToString("#0.000");
            if (newTestInfo.RLRef.CompareTo(CommonFunction.GetDefaultValue()) != 0 && newTestInfo.RLRef.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                row.Cells["RLREF"].Value = newTestInfo.RLRef.ToString("#0.000");
        }

        public void ClearAllData()
        {

            paramShow.BeginInvoke(new ClearDelegate(ClearDelegateMethod));
        }
        private delegate void ClearDelegate();
        public void ClearDelegateMethod()
        {
            paramShow.Rows.Clear();
            paramShow.Columns.Clear();
        }


        public void UpdateRefView(int nRowIdx, MESTestInfo testInfo)
        {
            if (testInfo == null)
                return;
            object[] invokeChartData = new object[2];
            invokeChartData[0] = nRowIdx;
            invokeChartData[1] = testInfo;
            paramShow.BeginInvoke(new UpdateDelegate(UpdateRefDelegateMethod), invokeChartData);
        }
        private delegate void UpdateDelegate(int nRowIdx, MESTestInfo testInfo);
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
        }


        public void UpdateDataView(int nRowIdx, MESTestInfo testInfo)
        {
            if (testInfo == null)
                return;
            object[] invokeChartData = new object[2];
            invokeChartData[0] = nRowIdx;
            invokeChartData[1] = testInfo;
            paramShow.BeginInvoke(new UpdateDelegate(UpdateTestDateDelegateMethod), invokeChartData);
        }
        
        private void UpdateTestDateDelegateMethod(int nRowIdx, MESTestInfo testInfo)
        {
            if (testInfo.CurValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && testInfo.CurValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
            {
                if (paramShow.Rows.Count > nRowIdx)
                {
                    DataGridViewRow row = paramShow.Rows[nRowIdx];
                    DataGridViewColumnCollection columns = paramShow.Columns;
                    if (!columns.Contains(testInfo.ParamColumnName))
                        return;
                    DataGridViewCell cell = row.Cells[testInfo.ParamColumnName];
                    if (testInfo.Pass)
                    {
                        cell.Style.ForeColor = System.Drawing.Color.Black;
                        cell.Value = testInfo.CurValue.ToString();
                    }
                    else
                    {
                        cell.Style.ForeColor = System.Drawing.Color.Red;
                        row.Cells[testInfo.ParamColumnName].Value = string.Format("*{0:0.000}", testInfo.CurValue);
                    }
                    row.Cells[testInfo.ParamColumnName].Style.BackColor = System.Drawing.Color.White;
                }
            }
        }
        
    }
}
