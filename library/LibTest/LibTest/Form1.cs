using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using MoUtilityLib;
using System.Threading;

namespace LibTest
{
    public partial class Form1 : Form
    {
        PWMControl pwmContrl;
        private List<double> m_ChartX = new List<double>();
        private List<double> m_ChartY = new List<double>();
        private CurveChart m_ChartShow;

        public Form1()
        {
            InitializeComponent();

            string strSection = "";
            strSection = string.Format("{0} Port {1} {2}{3:00} Settings", "Tmpt0", "4->1", "WL", 1);
            
            string strCode = "000010001001";
            //strCode = ReverseString(strCode);
            foreach (char ch in strCode)
            {
                if (ch == '1')
                {
                    bool bsuccess = true;
                }
            }
            m_ChartShow = new CurveChart(m_PWMDataShow);
        }

        public void Write(string path,string strWriteContent)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(strWriteContent);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            bool bTest = true;
            
            string[] strNodeContent;
            string errMsg;
            errMsg = bTest.ToString();
            string[] strName = new string[2];
            strName[0] = "PN";
            strName[1] = "TempleData";

            //ParamNameAndCode paramPeakIL = ParamNameAndCode.OP_PeakIL;

            //errMsg=paramPeakIL.GetDescription();
            //bool bSuccess = CommonFunction.GetNodeContentByName("http://zh-amtsdb-srv.oplink.com.cn/AMTS/Atd_SerialNoInfo.aspx?serialno=COUP04561280","AutoTemplate", strName, out strNodeContent,out errMsg);
            /*bool bSuccess = CommonFunction.GetNodeContentByName("http://172.18.1.101/amts/Atd_GenerateTempletIniDC.aspx?serialno=LR4001&adjust=1&user=11091", "AutoTemplate", strName, out strNodeContent, out errMsg);
            
            Write("C:\\Users\\jruan01\\Documents\\test\\template.ini",strNodeContent[1]);
            TemplateData testTemplate = new TemplateData();
            testTemplate.ParserTemplateFile("C:\\Users\\jruan01\\Documents\\test\\template.ini");
            TestDataView dataShow = new TestDataView();
            dataShow.InitView(ref dataGridView1, testTemplate.m_AllTestInfo);
            OPTestInfo testInfo = testTemplate.GetTestInfoByIndex(0);
            testInfo.m_bPass = true;
            testInfo.m_dCurValue = 25.66;
            dataShow.UpdateDataView(ref dataGridView1, 0, testInfo);*/

           /* GlobalSetting testSetting = testTemplate.GetGlobalSetting();
            testSetting.m_dITU = 1550.0;
            OPTestInfo testInfo = testTemplate.GetTestInfoByIndex(0);*/
            /*string[] strTestStep=new string[2];
            strTestStep[0]="调节";
            strTestStep[1]="测试";
            MoLogin testLogin = new MoLogin("set\\xmlset.ini", strTestStep);
            testLogin.ShowDialog();
            int nTestStep=0;
            if (testLogin.GetResult(out nTestStep))
            {
                //MessageBox.Show("登录成功！");
                //webservic wsdl地址
                string url = "http://zh-amtsdb-srv.oplink.com.cn/AMTS/Sys_NormalAPI.asmx";
                string[] args = new string[2];
                args[0] = "16010300110";
                args[1] = "COUP04561280";
                //url: webservic wsdl地址
                //ValidateWMSLabel4SN： webservice 函数名，
                //args：参数
                object result = MoUtilityLib.WebServiceHelper.InvokeWebService(url, "ValidateWMSLabel4SN", args);
                string strWeather = result.ToString();  
            }
            else
                return;*/

            string url = "http://172.18.1.101/amts/Atd_UploadMessage.asmx";
            string[] args = new string[1];
            args[0] = "<AMTS><SN>LR4001</SN><TT>1</TT><TEMPLET>78050797ACG</TEMPLET><VER>1</VER><PN>78050797ACG</PN><SPEC>N/A</SPEC><USER>11091</USER><COMPUTER>ITNB100019</COMPUTER><DN></DN><START>2017-07-31 14:23:26</START><DATE>2017-07-31 14:51:10</DATE><SOFTWARE>DC-A</SOFTWARE><TEMP VALUE=\"0\"><PORT VALUE=\"1->5\"><DB VALUE=\"3.0\"><SHIFT>0.000</SHIFT></DB><WL LEFT=\"1294.2600\" RIGHT=\"1296.8600\"><MAXIL>0.407</MAXIL><PEAKIL>0.638</PEAKIL></WL></PORT><PORT VALUE=\"1->7\"><WL LEFT=\"1298.7500\" RIGHT=\"1301.3500\"><MAXIL>0.262</MAXIL><PEAKIL>0.563</PEAKIL></WL></PORT><PORT VALUE=\"1->6\"><DB VALUE=\"3.0\"><SHIFT>-0.200</SHIFT></DB><WL LEFT=\"1303.2800\" RIGHT=\"1305.8800\"><MAXIL>0.293</MAXIL><PEAKIL>0.539</PEAKIL></WL></PORT><PORT VALUE=\"1->8\"><DB VALUE=\"3.0\"><SHIFT>-0.750</SHIFT></DB><WL LEFT=\"1307.8400\" RIGHT=\"1310.4400\"><MAXIL>0.447</MAXIL><PEAKIL>0.652</PEAKIL></WL></PORT><PORT VALUE=\"1->1\"><WL LEFT=\"1310.0000\" RIGHT=\"1310.0000\"><RL>45.024</RL></WL></PORT></TEMP></AMTS>";
            //url: webservic wsdl地址
            //ValidateWMSLabel4SN： webservice 函数名，
            //args：参数
            object result = MoUtilityLib.WebServiceHelper.InvokeWebService(url, "Upload", args);
            string strWeather = result.ToString();  
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            string strSN = SNBox.Text;
            TemplateData temp = new TemplateData();
            string errMsg;
            temp.OpenTemplate("http://172.18.1.101/amts/", TemplateTypeEnum.Template_DC, strSN, ProcessEnum.Process_Adjust, TestType.Test_Normal, "11091", "", true, out errMsg);
            TestDataView dataShow = new TestDataView();
            dataShow.InitView(ref dataGridView1, temp.m_AllTestInfo);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int nIndex = dataGridView1.CurrentCell.RowIndex;

        }
        
        private void BtnOpenCom_Click(object sender, EventArgs e)
        {
            string strSour = txtCom.Text;
            PWMTypeEnum type = PWMTypeEnum.PWM_JH;
            pwmContrl = new PWMControl();
            string errMsg;
            string[] strName=new string[2];
            strName[0]="PM1";
            pwmContrl.OpenPWM(strSour, type, out errMsg, strName);
            //pwmContrl.ShowChartEvent += PWMDataUpdate;     
        }


        private void PWMDataUpdate(int nChannel)
        {
            string strName;
            double[] xArr;
            double[] yArr;
            pwmContrl.GetRecordPower(0, out strName, out xArr, out yArr);
            if (xArr.Length < 2)
                return;
            m_ChartShow.UpdateChart(strName, xArr, yArr);
        }

        public double  Memorytofloat(byte[] bArr)
        {
            string binaryStr = "";
            foreach(byte b in bArr)
               binaryStr += Convert.ToString(b, 2);
            //float 32位保存，1-符号位 2-9 共8位2的指数为（e-127） 剩余23位小数点位
            int s = Convert.ToInt32(binaryStr.Remove(1,binaryStr.Length-1));
            string eStr = binaryStr.Remove(0, 1);
            eStr = eStr.Remove(8, eStr.Length - 8);
            int e=Convert.ToInt32(eStr,2);
            e -= 127;
            
            string maintain = "1";
            maintain += binaryStr.Remove(0, 9);

            string strInter="";
            string strDecimal="";
            if (e >= 0)
            {
                strInter = maintain.Remove(e + 1, maintain.Length - e - 1);
                strDecimal = maintain.Remove(0, e + 1);
            }
            else if (e == -1)
            {
                strInter = "0";
                strDecimal = maintain;
            }
            else
            {
                strInter = "0";
                for (int i = 1; i < -e; i++)
                    strDecimal += "0";
                strDecimal += maintain;
            }
            
            //将二进制小数转换为10进制算法
            char[] ch1 = new char[strInter.Length];
            char[] ch2 = new char[strDecimal.Length];
            strInter.CopyTo(0, ch1, 0, strInter.Length);
            strDecimal.CopyTo(0, ch2, 0, strDecimal.Length);
            
            double dRes = 0;
            for (int i = 0; i < ch1.Length; i++)
            {
                dRes += Convert.ToInt32(ch1[i].ToString()) * Math.Pow(2, ch1.Length - i - 1);
            }

            for (int i = 0; i < ch2.Length; i++)
            {
                dRes += Convert.ToInt32(ch2[i].ToString()) * Math.Pow(2, -i - 1);
            }

            return dRes;
        }

        private byte JHCheckXor(byte[] byArr, int nStart, int nStop)
        {
            if (byArr.Length == 0)
                return 0;
            byte byRes = byArr[nStart];
            for (int i = nStart + 1; i <= nStop; i++)
                byRes = Convert.ToByte(byRes ^ byArr[i]);
            return byRes;

        }

        private void btnGetPDL_Click(object sender, EventArgs e)
        {
            /*byte[] c = new byte[9];
	        float fdbm;
	        c[0]=0x55;
	        c[1]=0x11;
	        c[2]=0x05;
	        c[3]=0x1e;
            c[4] = 0x04;
            c[5] = 0x62;
            c[6] = 0x31;
            c[7] = 0xc2;
            c[8] = 0x9f;

            if (c[8] == JHCheckXor(c, 1, 7) && c[1] == 0x11)
            {
                byte[] byValue=new byte[4];
                byValue[0]=c[4];
                byValue[1]=c[5];
                byValue[2]=c[6];
                byValue[3]=c[7];
                double d1 = Memorytofloat(byValue);
            }


            //double dres = converttofloat(c);
            
            double[] dReadValue = new double[200];
            //功率计读取数值，并计算
            
            /*ClearChartData();
            string strName;
            double[] xArr;
            double[] yArr;
            pwmContrl.GetRecordPower(0, out strName, out xArr, out yArr);
            m_ChartShow.AddSeries(strName, SeriesChartType.Spline, System.Drawing.Color.Green, 100, "", "dB");
            m_ChartShow.ClearChart(strName);
            m_ChartShow.UpdateChartXSet(strName, 200);
            Thread t = new Thread(GetPowerPDLThread);
            t.IsBackground = true;
            t.Start();*/
            string errMsg;
            pwmContrl.GetPower_PDL(0, 200, 0, out errMsg);
            string strName;
            double[] xArr;
            double[] yArr;
            pwmContrl.GetRecordPower(0, out strName, out xArr, out yArr);
            //double dPeak = ParamCalculate.CalculatePeakIL(dReadValue);

        }

        private void GetPowerPDLThread()
        {
            string errMsg;
            pwmContrl.GetPower_PDL(0, 200, 0, out errMsg);
        }

        

       

        private void button2_Click(object sender, EventArgs e)
        {
            string errMsg;
            double dPower = pwmContrl.ReadPower(0,out errMsg);
            //int n = pwmContrl.Get1830OrOPLKUnits(out errMsg);
            double[] d=new double[32];
            int ns = 4;
            double dAP=pwmContrl.GetPower(0,ref ns,out d, out errMsg);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (pwmContrl != null)
            {
                pwmContrl.ClosePWM();
            }
        }

        
 
        public void ClearChartData()
        {
            m_ChartX.Clear();
            m_ChartY.Clear();    
        }

        
        private void button3_Click(object sender, EventArgs e)
        {
            m_ChartShow.AddSeries("test1", SeriesChartType.Spline, System.Drawing.Color.Green, 100, "", "dB");
            m_ChartShow.ClearChart("test1");
            ClearChartData();
            m_ChartShow.UpdateChartXSet("test1", 150);
            
        }

        
    }
}
