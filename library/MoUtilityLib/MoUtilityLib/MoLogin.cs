using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using MSXML2;

namespace MoUtilityLib
{
    public partial class MoLogin : Form
    {
        private const string consRecTestStepFile = "./TestStep.ini";
        private const string consTestStepSection = "WORK TYPE";
        private const string consTestStepKey = "TestStep";
        private string m_UserInfo = "";
        private int m_nTestStep = 0;
        private bool m_bPass = false;
        public MoLogin(string strConfigFilePath, string[] strComContent)
        {
            InitializeComponent();
            InitLogin(strConfigFilePath, strComContent);
        }

        //获取登陆结果，测试工序
        public bool GetResult(out int nTestStep,out string strUserID)
        {
            nTestStep = m_nTestStep;
            strUserID = m_UserInfo;
            return m_bPass;
        }

        private void InitLogin(string strConfigFilePath, string[] strComContent = null)
        {
            if (strComContent!=null)
            {
                comTestStep.Visible = true;
                lblTestStep.Visible = true;
                comTestStep.Items.Clear();
                //初始化测试工序
                foreach (string element in strComContent)
                {
                    comTestStep.Items.Add(element);
                }
                IniParser IniParser;
                IniParser = new IniParser(consRecTestStepFile);
                m_nTestStep=IniParser.readIntData(consTestStepSection, consTestStepKey,0);
                comTestStep.SelectedIndex = m_nTestStep;
            }
            else
            {
                comTestStep.Visible = false;
                lblTestStep.Visible = false;
            }
            GetUserData(strConfigFilePath);
        }

        private void GetUserData(string strConfigFilePath)
        {
            try
            {
                IniParser m_IniParser;
                m_IniParser = new IniParser(strConfigFilePath);
                string strSection = "XML Set";
                string strKey = "Address";
                string strAddress = m_IniParser.readStringData(strSection, strKey, "");
                strAddress =strAddress + "sys_getuser.aspx";
                XMLHTTP xmlHttp = new XMLHTTP();
                xmlHttp.open("get", strAddress, false, null, null);
                xmlHttp.send(null);
                string strRes = xmlHttp.responseText;
                XmlDocument xmlParser = new XmlDocument();
                xmlParser.LoadXml(strRes);
                XmlNodeList pNode;
                pNode = xmlParser.GetElementsByTagName("UserData");
                m_UserInfo = pNode.Item(0).InnerText;
                CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\temple\\userdata.ini", m_UserInfo);

            }
            catch(Exception ex)
            {
                MessageBox.Show("获取用户信息出错，"+ex.InnerException.Message);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            m_nTestStep = comTestStep.SelectedIndex;
            if (m_UserInfo.Length == 0)
            {
                MessageBox.Show("获取用户信息失败！");
                m_bPass = false;
                this.Close();
                return;
            }
            string[] strIDAndPassword=m_UserInfo.Split('\n');
            foreach (string element in strIDAndPassword)
            {
                string[] strInfo = element.Split(',');
                if (txtOperaterID.Text == strInfo[0] && txtPassword.Text == strInfo[1].Substring(0, strInfo[1].Length - 1))
                {
                    m_bPass = true;
                    //记录测试工序
                    IniParser IniParser;
                    IniParser = new IniParser(consRecTestStepFile);
                    IniParser.writeData(consTestStepSection, consTestStepKey, m_nTestStep.ToString());
                    m_UserInfo = txtOperaterID.Text;
                    this.Close();
                    return;
                }
            }
            m_bPass = false;
            MessageBox.Show("用户名或者密码错误");
        }

        private void btnCansel_Click(object sender, EventArgs e)
        {
            m_bPass = false;
            this.Close();
        }
    }
}
