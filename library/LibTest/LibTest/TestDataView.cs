using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LibTest
{
    class TestDataView
    {
        public TestDataView()
        {
        }
        public void InitView(ref DataGridView dataShow,List<OPTestInfo> allTestData,bool bShowData=false)
        {
            ClearAllData(ref dataShow);
            //去掉最后一行“*”
            dataShow.AllowUserToAddRows = false;
            dataShow.ReadOnly = true;
            dataShow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataShow.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataShow.MultiSelect = false;
            if (allTestData.Count == 0)
                return;
            int nColumnIndex = -1;
            nColumnIndex=dataShow.Columns.Add("Temperature", "温度");
            DataGridViewColumn column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nColumnIndex = dataShow.Columns.Add("Wave", "波长");
            column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nColumnIndex = dataShow.Columns.Add("Port", "PORT");
            column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nColumnIndex = dataShow.Columns.Add("Param", "ITEM");
            column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nColumnIndex = dataShow.Columns.Add("ILREF", "IL REF");
            column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            nColumnIndex = dataShow.Columns.Add("RLREF", "RL REF");
            column = dataShow.Columns[nColumnIndex];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            foreach (OPTestInfo info in allTestData)
            {              
                int rowIndex=dataShow.Rows.Add();
                DataGridViewRow row = dataShow.Rows[rowIndex];
                row.Cells["Temperature"].Value = info.m_dTemperature.ToString();
                //单点，还是波段
                if (info.m_dWLLeft.CompareTo(0.0) != 0)
                {
                    if ((info.m_dWLLeft - info.m_dWLRight).CompareTo(0.0) != 0)
                    {
                        row.Cells["Wave"].Value = string.Format("{0:0.000}~{1:0.000}", info.m_dWLLeft, info.m_dWLRight);
                    }
                    else
                        row.Cells["Wave"].Value = info.m_dWLLeft.ToString("#0.000");
                }
                row.Cells["Port"].Value = info.m_PortNameForUser;
                //ex规则，参数与端口名一致
                if (info.m_ulParamType == ParamRuleEnum.PARAM_EX)
                {
                    if (info.m_dILRef.CompareTo(info.GetDefaultValue()) != 0)
                        row.Cells["Param"].Value = info.m_PortNameForUser;
                }
                else
                {
                    //shift BW等参数
                    if (info.m_dSettingValue.CompareTo(0.0) != 0)
                        row.Cells["Param"].Value = string.Format("{0} @ {1:0.00}", info.m_TestParam.GetStrTestTemplate(), info.m_dSettingValue);
                    else
                        row.Cells["Param"].Value = info.m_TestParam.GetStrTestTemplate();
                }
                //不存在测试项对应列，则增加
                DataGridViewColumnCollection columns = dataShow.Columns;
                if(!columns.Contains(info.m_ParamColumnName))
                {
                    dataShow.Columns.Add(info.m_ParamColumnName, info.m_ParamColumnName);
                }
                //测试过的数据是否显示，从无纸化下载的
                if (bShowData)
                    row.Cells[info.m_ParamColumnName].Value = info.m_dTestedValue.ToString("#0.000");

                if (info.m_dILRef.CompareTo(info.GetDefaultValue()) != 0)
                    row.Cells["ILREF"].Value = info.m_dILRef.ToString("#0.000");
                if (info.m_dRLRef.CompareTo(info.GetDefaultValue()) != 0)
                    row.Cells["RLREF"].Value = info.m_dRLRef.ToString("#0.000");
            }            
        }

        public void ClearAllData(ref DataGridView dataShow)
        {
            dataShow.Rows.Clear();
            dataShow.Columns.Clear();
        }

        public void UpdateDataView(ref DataGridView dataShow,int nRowIdx,OPTestInfo testInfo)
        {
            if (dataShow.Rows.Count > nRowIdx)
            {
                DataGridViewRow row = dataShow.Rows[nRowIdx];
                 DataGridViewColumnCollection columns = dataShow.Columns;
                if(!columns.Contains(testInfo.m_ParamColumnName))
                    return;
                DataGridViewCell cell = row.Cells[testInfo.m_ParamColumnName];
                if (testInfo.m_bPass)
                {
                    cell.Style.ForeColor = System.Drawing.Color.Black;
                    cell.Value = (-testInfo.m_dCurValue).ToString();
                }
                else
                {
                    cell.Style.ForeColor = System.Drawing.Color.Red;
                    row.Cells[testInfo.m_ParamColumnName].Value = string.Format("*{0:0.000}", -testInfo.m_dCurValue);
                }
            }
        }
        
    }
}
