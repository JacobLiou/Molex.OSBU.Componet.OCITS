using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.Visa;
using Ivi.Visa;
using System.Threading;
using System.Windows.Forms;

namespace LibTest
{
    public enum PWMTypeEnum
    {
        PWM_1830,
        PWM_JH,
        PWM_OPLKR152,
        PWM_OPLK1830
    }
    
    public class PWMControl
    {
        public delegate void ShowChartHandler(string serName,double[] xArr,double[] yArr,double xTotal);
        public event ShowChartHandler ShowChartEvent;

        private const byte c_PowerWLCH1 = 0x07;
        private const byte c_PowerWLCH2 = 0x08;

        private const byte c_PowerUnitCH1 = 0x09;
        private const byte c_PowerUnitCH2 = 0x0a;

        private const byte c_PowerUnitDBM = 0x00;
        private const byte c_PowerUnitW = 0x01;
        private const byte c_PowerUNITDB = 0x02;
        private const byte c_PowerUnitREF = 0x03;

       // private  MessageBasedSession m_BaseSession;
        private SerialSession m_BaseSession;
        private PWMTypeEnum m_PWMType;

        private char[] m_Result = new char[32];  //存储读取到的数据
        private bool m_bReading = false;
        private int m_iCWL;
        private double m_dPowerOld;
        private long actualCount = 0;//实际读取到的字节数
        private ReadStatus readStatus = ReadStatus.Unknown;//读取数据后的状态
        public static bool m_bNumPad1 = false;


        public string[] m_CurveName = new string[2];
        private List<double> m_ReadIdxList = new List<double>();
        private List<double> m_PowerIdxList = new List<double>();
        private List<double> m_ReadIdxList2 = new List<double>();
        private List<double> m_PowerIdxList2 = new List<double>();
        private object m_LockObj = new object();

        
        /// <summary>
        ///打开设备 
        /// </summary>
        /// <param name="rsName">设备名称</param>
        /// <param name="pwmType">设备类型</param>
        /// <param name="errMsg">消息</param>
        /// <returns>true/false</returns>
        public bool OpenPWM(string rsName, PWMTypeEnum pwmType, out string errMsg,string[] curName)
        {
            errMsg = "";
            m_PWMType = pwmType;
            if(curName.Length==2)
            {
                m_CurveName[0] = curName[0];
                m_CurveName[1] = curName[1];
            }
            else if (curName.Length == 1)
            {
                m_CurveName[0] = curName[0];
            }
            try
            {
                using (var rmSession = new ResourceManager())
                {
                    m_BaseSession = (SerialSession)rmSession.Open(rsName);
                    //SerialSession baseSession = (SerialSession)m_BaseSession;
                    if ((m_PWMType == PWMTypeEnum.PWM_1830) || (m_PWMType == PWMTypeEnum.PWM_OPLK1830))
                    {//newpower1830,自制1830与Newport1830指令相同                       
                        m_BaseSession.TimeoutMilliseconds = 500;
                        m_BaseSession.BaudRate = 9600;
                        m_BaseSession.DataBits = 8;
                        m_BaseSession.Parity = SerialParity.None;
                        m_BaseSession.StopBits = SerialStopBitsMode.One;
                        m_BaseSession.TerminationCharacterEnabled = true;
                        m_BaseSession.TerminationCharacter = 0xA;
                        ReSet1830();
                    }
                    else if (m_PWMType == PWMTypeEnum.PWM_JH)
                    {//嘉惠功率计
                        m_BaseSession.TimeoutMilliseconds = 100;
                        m_BaseSession.BaudRate = 9600;
                        m_BaseSession.DataBits = 8;
                        m_BaseSession.Parity = SerialParity.None;
                        m_BaseSession.StopBits = SerialStopBitsMode.One;
                        m_BaseSession.TerminationCharacterEnabled = false;

                        SetJHUnits(c_PowerUnitCH1, c_PowerUNITDB);
                        SetJHUnits(c_PowerUnitCH2, c_PowerUNITDB);
                    }
                    else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
                    {//自制光功率计
                        m_BaseSession.TimeoutMilliseconds = 500;
                        m_BaseSession.BaudRate = 115200;
                        m_BaseSession.DataBits = 8;
                        m_BaseSession.Parity = SerialParity.None;
                        m_BaseSession.StopBits = SerialStopBitsMode.One;
                        m_BaseSession.TerminationCharacterEnabled = true;
                        m_BaseSession.TerminationCharacter = 0xD;
                    }
                    else
                    {
                        errMsg = "功率计类型错误: " + m_PWMType + ", 只能为 0、1、2、3!";
                        //System.Windows.Forms.MessageBox.Show("功率计类型错误：" + m_PWMType + ",只能为0、1、2、3！", "系统提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        public void ClosePWM()
        {
            if (m_BaseSession != null)
            {
                m_BaseSession.Dispose();
            }
        }

        public PWMTypeEnum GetPWMType()
        {
            return m_PWMType;
        }

        /// <summary>
        /// 设置PWM单位
        /// 写入设置命令，byte数组sendBuf={0xaa,0xbb,0xcc,byChannelIndex,byUniteIndex,0x0,Convert.ToByte(sendBuf[1] ^ sendBuf[2] ^ sendBuf[3] ^ sendBuf[4] ^ sendBuf[5])}
        /// 读取设置后的数据，byte数组m_result，其中m_Result[0]=0x55，m_Result[8]=Convert.ToByte(m_Result[1] ^ m_Result[2] ^ m_Result[3] ^ m_Result[4] ^ m_Result[5] ^ m_Result[6] ^ m_Result[7])，
        /// m_Result[4] = byUniteIndex
        /// </summary>
        /// <param name="byChannelIndex">通道</param>
        /// <param name="byUniteIndex">单位</param>
        /// <returns>true/false</returns>
        public bool SetJHUnits(byte byChannelIndex, byte byUniteIndex)
        {
            try
            {
                byte[] sendBuf = new byte[7];
                sendBuf[0] = 0xaa;
                sendBuf[1] = 0xbb;
                sendBuf[2] = 0xcc;
                sendBuf[3] = byChannelIndex;
                sendBuf[4] = byUniteIndex;
                sendBuf[5] = 0x0;
                sendBuf[6] = Convert.ToByte(sendBuf[1] ^ sendBuf[2] ^ sendBuf[3] ^ sendBuf[4] ^ sendBuf[5]);
                m_BaseSession.RawIO.Write(sendBuf);
                Thread.Sleep(50);
                byte[] res = new byte[9];
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(res, 0, 9, out actualCount, out readStatus);
                //if (readStatus != ReadStatus.Unknown)
                {
                    byte xor = Convert.ToByte(res[1] ^ res[2] ^ res[3] ^ res[4] ^ res[5] ^ res[6] ^ res[7]);
                    if (xor == res[8] && res[0] == 0x55)
                    {
                        if (res[4] != byUniteIndex)
                            return false;
                        return true;
                    }
                    return false;
                }
                //return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        /// <summary>
        /// 设置单位
        /// </summary>
        /// <param name="nUnitsIndex">单位</param>
        /// <param name="nSensor">默认为0</param>
        /// <param name="nCH">默认为0</param>
        /// <returns>单位下标</returns>
        public int Set1830OrOPLKUnits(int nUnitsIndex, int nSensor=0, int nCH=0)
        {
            if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
            {
                //Units(U1:Watts, U2:dB, U3:dBm, U4:REF)
                switch (nUnitsIndex)
                {
                    case 1:
                        m_BaseSession.RawIO.Write("U1\n");  //Watts
                        return 1;
                    case 2:
                        m_BaseSession.RawIO.Write("U2\n");  //dB
                        return 2;
                    case 3:
                        m_BaseSession.RawIO.Write("U3\n"); //dBm
                        return 3;
                    case 4:
                        m_BaseSession.RawIO.Write("U4\n");  //REF
                        return 4;
                    default:
                        return 0;
                }
            }
            else if(m_PWMType==PWMTypeEnum.PWM_OPLKR152)
            {
                //Units(0: dBm, 1: W, 2: dB)
                m_BaseSession.RawIO.Write("sens" + nSensor + ":chan" + nCH + ":pow:unit " + nUnitsIndex + "\r");
                Thread.Sleep(100);

                return nUnitsIndex;
            }
            return 0;
        }

        public bool ReadComBytes(int nReadCount,out byte[] byteres, out string errMsg)
        {
            byte[] res = new byte[nReadCount];
            byteres = res;
            errMsg = "";
            try
            {          
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(byteres, 0, nReadCount, out actualCount, out readStatus);
                if (readStatus != ReadStatus.Unknown)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            return false;
        }

        public bool ReadComString(int nReadCount, out string strRes, out string errMsg)
        {
            errMsg = "";
            strRes = "";
            try
            {
                byte[] res = new byte[nReadCount];
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(res, 0, nReadCount, out actualCount, out readStatus);
                if (readStatus != ReadStatus.Unknown)
                {
                    
                    for (int i = 0; i < actualCount - 1; i++)
                    {
                        strRes += string.Format("{0}", (char)res[i]);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            return false;
        }
        /// <summary>
        /// 获取单位
        /// 如果是1830，写入命令 "U?\n",读取数据m_Result[0]是单位下标。
        /// 如果是嘉惠，直接返回0
        /// 如果是其他，写入命令 "sens" + nSensor + ":chan " + nCH + ":pow:unit?\r",读取数据m_Result[0]是单位下标。
        /// </summary>
        /// <param name="nSensor">默认为0</param>
        /// <param name="nCH">默认为0</param>
        /// <returns>单位下标</returns>
        public int Get1830OrOPLKUnits(out string errMsg,int nSensor = 0, int nCH = 0)
        {
            //string strErr = "";
            //errMsg = strErr;
            errMsg = "";
            try
            {
                if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
                {
                    m_BaseSession.RawIO.Write("U?\n");
                    Thread.Sleep(200);//>15较好               
                    //return 0;
                }
                else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
                {
                    m_BaseSession.RawIO.Write("sens" + nSensor + ":chan " + nCH + ":pow:unit?\r");
                    Thread.Sleep(200);
                }
                string strRes;
                bool bSuccess = ReadComString(32,out strRes,out errMsg);
                if (bSuccess)
                    return Convert.ToInt32(strRes);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return 0;
        }

        /// <summary>
        /// 设置波长
        /// 如果是1830，先将波长四舍五入，写入指令"W" + m_iCWL + "\n"
        /// 如果是嘉惠，先将波长四舍五入，再调用函数SetPWMWavelength(c_PowerWLCH1, m_iCWL)
        /// 如果是152，写入指令"sens" + nSensor + ":chan" + nCH + ":pow:wav " + string.Format("{0:3F}", dCWL) + "\r"
        /// </summary>
        /// <param name="bChannelIndex">通道</param>
        /// <param name="dCWL">波长</param>
        /// <param name="nSensor">默认为0</param>
        /// <param name="nCH">默认为0</param>
        /// <returns>true/false</returns>
        public bool SetWaveLength(byte bChannelIndex, double dCWL, int nSensor=0, int nCH=0)
        {
            if (m_bReading)
            {
                return false;
            }

            m_bReading = true;

            m_iCWL = (int)(dCWL + 0.5);//四舍五入

            if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
            {
                m_BaseSession.RawIO.Write("W" + m_iCWL + "\n");
            }
            else if (m_PWMType == PWMTypeEnum.PWM_JH)
            {
                if (bChannelIndex == 0x00)
                    SetJHWavelength(c_PowerWLCH1, m_iCWL);
                else if (bChannelIndex == 0x01)
                    SetJHWavelength(c_PowerWLCH2, m_iCWL);
            }
            else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
            {
                m_BaseSession.RawIO.Write("sens" + nSensor + ":chan" + nCH + ":pow:wav " + string.Format("{0:3F}", dCWL) + "\r");
            }

            m_bReading = false;

            return true;
        }

        /// <summary>
        /// 如果是1830，写入命令"D?\n"，再读取数据，返回Convert.ToDouble(m_Result)
        /// 如果是嘉惠，调用函数GetPWMPower(dblValue1,  dblValue2)，返回dPower = dblValue1
        /// 如果是152，写入命令"sens" + nSensor + ":chan" + nCH + ":pow:read?\r"，再读取数据，返回Convert.ToDouble(m_Result)
        /// </summary>
        /// <param name="bChannelIndex"></param>
        /// <param name="nSensor">默认为0</param>
        /// <param name="nCH">默认为0</param>
        /// <returns></returns>
        public double ReadOnce(byte bChannelIndex, int nSensor=0, int nCH=0)
        {
            double dPower=0.0;
            char[] dblValue1 = new char[4]; ;
            char[] dblValue2=new char[4];

            if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
            {
                try
                {
                    m_BaseSession.RawIO.Write("D?\n");
                    Thread.Sleep(8);
                    m_Result = new char[32];
                    actualCount = 0;
                    readStatus = ReadStatus.Unknown;
                    m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32,out actualCount,out readStatus);
                    if (readStatus != ReadStatus.Unknown)
                    {
                        m_Result[actualCount] = '\0';    //必需要
                        dPower = Convert.ToDouble(m_Result);
                    }
                }
                catch (Exception ex)
                {
                    //System.Windows.Forms.MessageBox.Show(ex.Message);
                    return 0.0;
                }
            }
            else if (m_PWMType == PWMTypeEnum.PWM_JH)
            {
                double dCH1Value = 0.0;
                double dCH2Value = 0.0;
                string errMsg;
                if (!GetPWMPower(ref dCH1Value, ref dCH2Value,out errMsg))
                    return 0.0;

                if (bChannelIndex == 0x00)
                {
                    dPower =dCH1Value;
                }
                else if (bChannelIndex == 0x01)
                {
                    dPower =dCH2Value;
                }
            }
            else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
            {
                try
                {
                    m_BaseSession.RawIO.Write("sens" + nSensor + ":chan" + nCH + ":pow:read?\r");
                    m_Result= new char[32];
                    m_Result = m_BaseSession.RawIO.ReadString().ToCharArray();
                    dPower = Convert.ToDouble(m_Result);
                }
                catch (Exception ex)
                {
                    return 0.0;
                }
            }

            return dPower;
        }

        /// <summary>
        /// 读取功率
        /// </summary>
        /// <param name="bChannelIndex"></param>
        /// <param name="nAvgSamples">默认为-1</param>
        /// <param name="nSensor">默认为0</param>
        /// <param name="nCH">默认为0</param>
        /// <returns></returns>
        public double ReadPower(byte bChannelIndex, out string errMsg,int nAvgSamples=1, int nSensor=0, int nCH=0)
        {
            errMsg = "";
            if (m_bReading)
            {
                return CommonFunction.GetDefaultValue();
            }
            try
            {
                m_bReading = true;

                double dAvg = 0.0;
                int Number = 0; //实际采样记数
                double dPower = 0.0;
                int nReadErrorNum = 0;

                m_dPowerOld = 1000.1;

                for (int index = 0; index < (nAvgSamples + 10); index++)
                {
                    if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
                    {
                        m_BaseSession.RawIO.Write("D?\n");
                        Thread.Sleep(30);
                        string strRes;
                        bool bSuccess = ReadComString(32,out strRes, out errMsg);

                        if (bSuccess)
                        {
                            nReadErrorNum = 0;
                            dPower = Convert.ToDouble(strRes);
                        }
                        else
                        {
                            nReadErrorNum++;
                            if (nReadErrorNum > 3)
                            {//连续错误3次
                                m_bReading = false;
                                return CommonFunction.GetDefaultValue();
                            }
                        }
                    }
                    else if (m_PWMType == PWMTypeEnum.PWM_JH)
                    {
                        double dCH1Value = 0.0;
                        double dCH2Value = 0.0;
                        if (GetPWMPower(ref dCH1Value, ref dCH2Value, out errMsg))
                        {
                            nReadErrorNum = 0;
                            if (bChannelIndex == 0x00)
                                dPower = dCH1Value;
                            else
                                dPower = dCH2Value;
                        }
                        else
                        {
                            nReadErrorNum++;
                            if(nReadErrorNum>3)
                            {
                                m_bReading=false;
                                return CommonFunction.GetDefaultValue();
                            }
                            continue;
                        }
                    }
                    else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
                    {
                        try
                        {
                            m_BaseSession.RawIO.Write("sens" + nSensor + ":chan" + nCH + ":pow:read?\r");
                            m_Result = new char[32];
                            m_Result = m_BaseSession.RawIO.ReadString().ToCharArray();
                            nReadErrorNum = 0;
                            dPower = Convert.ToDouble(m_Result);
                        }
                        catch (Exception ex)
                        {
                            errMsg = ex.Message;
                            nReadErrorNum++;
                            if (nReadErrorNum > 3)
                            {
                                m_bReading = false;
                                return CommonFunction.GetDefaultValue();
                            }
                        }
                    }

                    if (m_dPowerOld < 1000.0 && dPower < (m_dPowerOld - 6.0))
                    {//突变6dB
                        Thread.Sleep(550);  //等待稳定                    
                        m_dPowerOld = dPower;
                        continue;
                    }

                    m_dPowerOld = dPower;

                    /*if (nAvgSamples < 2)    //单次采样
                    {
                        m_bReading = false;
                        return (m_dPowerOld);
                    }*/

                    dAvg += m_dPowerOld;
                    Number++;

                    if (Number >= nAvgSamples)
                    {
                        m_bReading = false;
                        //保留三位小数处理
                        return Convert.ToDouble((dAvg / Number).ToString("#0.000"));
                    }

                    if (index > (Number + 9))   //错误超过9次
                    {
                        errMsg="错误超过9次!";

                        m_bReading = false;
                        return CommonFunction.GetDefaultValue();
                    }

                    Thread.Sleep(20);
                }
            

                m_bReading = false;

                if (Number > 0)
                {
                    return (dAvg / Number);
                }
                else
                {
                    return -1000.0;
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            finally
            {
                m_bReading = false;
            }
            return -1000.0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bChannelIndex"></param>
        /// <param name="nSamples"></param>
        /// <param name="dPower"></param>
        /// <returns>返回nSamples次采样均值和序列,VOA专用</returns>
        public double GetPower(byte bChannelIndex, ref int nSamples, out double[] dPower,out string errMsg)
        {
            int index, SameNumber;
            double dStep, dPowerOld, dAvg, dAvgOld;
            errMsg = "";
            dPower = new double[nSamples];
            dPowerOld = dPower[0] = ReadPower(bChannelIndex,out errMsg);

            for (index = 1; index < nSamples; index++)
            {
                dPower[index] = ReadPower(bChannelIndex,out errMsg);

                dStep = Math.Abs(dPower[index] - dPowerOld);

                if (dStep > 0.99)   //换档奇异
                    continue;

                if (dStep > 0.015)
                {                  //首次不同
                    dPowerOld = dPower[index];
                    break;
                }
            }

            if (index >= nSamples)//不用进入下一步
                return (dPower[nSamples - 1]);

            SameNumber = 1;

            dAvg = dAvgOld = dPowerOld;

            for (index += 1; index < nSamples; index++)
            {
                dPower[index] = ReadPower(bChannelIndex,out errMsg);

                dStep = Math.Abs(dPower[index] - dPowerOld);

                if (dStep > 0.005)
                {   //不相同,包括奇异
                    dAvgOld = dAvg;

                    SameNumber = 0;

                    dAvg = 0.0;

                    if (dStep < 1.0)
                        dPowerOld = dPower[index];
                }
                else
                {
                    SameNumber++;
                    dAvg += dPower[index];
                    dPowerOld = dPower[index];

                    if (index > 15 && SameNumber > 6)
                    {   //至少采集16次，且连续存在7个相同数
                        nSamples = index + 1;

                        break;
                    }
                }
            }

            return ((SameNumber > 0) ? (dAvg / (double)SameNumber) : dAvgOld);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWndDisplay"></param>
        /// <returns></returns>
        public int GetKeySelect(object sender,KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Clear)
            {
                MessageBox.Show("操作暂停，按“确认”后继续", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else if (e.KeyCode == Keys.Pause)
            {
                MessageBox.Show("操作暂停，按“确认”后继续", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else if (e.KeyCode == Keys.End)
            {
                return 1;
            }
            else if (e.KeyCode == Keys.NumPad1)//小键盘数字1键
            {
                return 1;
            }
            else if (e.KeyCode == Keys.Home)
            {
                return 2;
            }
            return 0;
        }

        private void RecordPower(double dx, double dy, int nChannel)
        {
            lock (m_LockObj)
            {
                if (nChannel == 0)
                {
                    m_ReadIdxList.Add(dx);
                    m_PowerIdxList.Add(dy);
                }
                else
                {
                    m_ReadIdxList2.Add(dx);
                    m_PowerIdxList2.Add(dy);
                }
            }
        }

        public void GetRecordPower(int nChannel, out double[] yArr)
        {
            lock (m_LockObj)
            {
                if (nChannel == 0)
                {                   
                    yArr = new double[m_PowerIdxList.Count];
                    m_PowerIdxList.CopyTo(yArr);
                }
                else
                {                   
                    yArr = new double[m_PowerIdxList2.Count];
                    m_PowerIdxList2.CopyTo(yArr);
                }
            }
        }

        public void GetCurveName(int nChannel, out string strCurveName)
        {
            lock (m_LockObj)
            {
                if (nChannel == 0)
                    strCurveName = m_CurveName[0];
                else
                    strCurveName = m_CurveName[1];
            }
        }
        public void GetRecordPower(int nChannel, out string strCurveName, out double[] xArr, out double[] yArr)
        {
            lock (m_LockObj)
            {
                if (nChannel == 0)
                {
                    xArr = new double[m_ReadIdxList.Count];
                    yArr = new double[m_PowerIdxList.Count];
                    m_ReadIdxList.CopyTo(xArr);
                    m_PowerIdxList.CopyTo(yArr);
                    strCurveName=m_CurveName[0];
                }
                else
                {
                    xArr = new double[m_ReadIdxList2.Count];
                    yArr = new double[m_PowerIdxList2.Count];
                    m_ReadIdxList2.CopyTo(xArr);
                    m_PowerIdxList2.CopyTo(yArr);
                    strCurveName = m_CurveName[1];
                }
            }
        }

        private void ClearData(int nChannel)
        {
            lock (m_LockObj)
            {
                if (nChannel == 0)
                {
                    m_ReadIdxList.Clear();
                    m_PowerIdxList.Clear();
                }
                else
                {
                    m_ReadIdxList2.Clear();
                    m_PowerIdxList2.Clear();
                }
            }
        }

        /// <summary>
        /// 读数据并显示数据图，返回Avg
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="bChannelIndex"></param>
        /// <param name="maxSamples"></param>
        /// <param name="dblref"></param>
        /// <param name="dMaxOut"></param>
        /// <param name="dMinOut"></param>
        /// <param name="d3Segma"></param>
        /// <param name="nSensor"></param>
        /// <param name="nCH"></param>
        /// <returns></returns>
        public bool GetPower_PDL( byte bChannelIndex, int maxSamples, double dblref,out string errMsg,int nSensor=0, int nCH=0)
        {
            char ch=(char)bChannelIndex;
            int nChannel = Convert.ToInt32(ch);
            ClearData(nChannel);
            errMsg = "";
            double  dPowerNew;           
            if (maxSamples < 100)
                maxSamples = 100;
            else if (maxSamples > 1300)
                maxSamples = 1300;
            try
            {
                /*dPowerNew = ReadPower(bChannelIndex,out errMsg, 2, nSensor, nCH);
                RecordPower(0, dPowerNew, nChannel);
                
                if (ShowChartEvent != null)
                {
                    ShowChartEvent(serName, xArr, yArr, maxSamples);
                }*/
                string serName;
                double[] xArr;
                double[] yArr;
                GetRecordPower(nChannel, out serName, out xArr, out yArr);
                for (int index = 0; index < maxSamples; index++)
                {
                    dPowerNew = ReadPower(bChannelIndex, out errMsg, 1, nSensor, nCH);
                    if (dPowerNew == CommonFunction.GetDefaultValue())
                        continue;
                    dPowerNew -= dblref;
                    RecordPower(index, dPowerNew, nChannel);
                    GetRecordPower(nChannel, out serName, out xArr, out yArr);
                    if (ShowChartEvent != null)
                        ShowChartEvent(serName, xArr, yArr, maxSamples);                
                    if (m_bNumPad1)
                    {
                        maxSamples = index + 1;
                        break;
                    }
                }
                m_bReading = false;
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            finally
            {
                m_bReading = false;
            }
            
        }

        /// <summary>
        /// 不明这个函数的意义
        /// </summary>
        /// <param name="nAvgSamples"></param>
        /// <returns></returns>
        public double ReadPowerNw(out string errMsg,int nAvgSamples=1)
        {
            errMsg = "";
            if (m_bReading)
            {
                return -2000.0;
            }

            m_bReading = true;
            double dbPower = 0.0;
            Thread.Sleep(200);
            m_BaseSession.RawIO.Write("U3\n");
            Thread.Sleep(200);
            dbPower = ReadPower((byte)nAvgSamples,out errMsg);
            m_BaseSession.RawIO.Write("U2\n");
            Thread.Sleep(200);
            m_bReading = false;
            return Math.Pow(10, dbPower / 10);
        }

        /// <summary>
        /// 设置PWM波长
        /// 写入命令byte数组send_buf={0xaa,0xbb,0xcc,bChannelIndex,(dwWavelength / 256),(dwWavelength % 256),(send_buf[1] ^ send_buf[2] ^ send_buf[3] ^ send_buf[4] ^ send_buf[5])}
        /// 再读取数据，进行验证
        /// </summary>
        /// <param name="bChannelIndex">通道</param>
        /// <param name="dwWavelength">波长</param>
        /// <returns>true/false</returns>
        public bool SetJHWavelength(byte bChannelIndex, int dwWavelength)
        {
            byte[] send_buf = new byte[7];
            char xor = '\0';

            m_Result = new char[9];

            send_buf[0] = 0xaa;
            send_buf[1] = 0xbb;
            send_buf[2] = 0xcc;
            send_buf[3] = bChannelIndex;
            send_buf[4] = (byte)(dwWavelength / 256);
            send_buf[5] = (byte)(dwWavelength % 256);
            send_buf[6] = (byte)(send_buf[1] ^ send_buf[2] ^ send_buf[3] ^ send_buf[4] ^ send_buf[5]);

            m_BaseSession.RawIO.Write(send_buf, 0, 7);
            Thread.Sleep(20);
            string errMsg;
            byte[] byteRes;
            bool bSuccess = ReadComBytes(9,out byteRes,out errMsg);
            if (bSuccess)   //读取成功
            {
                if (byteRes.Length > 7)
                {
                    xor = (char)(byteRes[1] ^ byteRes[2] ^ byteRes[3] ^ byteRes[4] ^ byteRes[5] ^ byteRes[6] ^ byteRes[7]);

                    if (byteRes[0] == 0x55 && xor == byteRes[8]) //数据校验成功
                    {
                        if (dwWavelength != (byteRes[2] * 256 + byteRes[3]))
                            return false;
                        else
                            return true;
                    }
                }
            }
            return false;
        }



        /// <summary>
        /// 获取PWM功率值
        /// </summary>
        /// <param name="dblValue1"></param>
        /// <param name="dblValue2"></param>
        /// <returns></returns>
        public bool GetPWMPower(ref double dblValue1,ref double dblValue2,out string errMsg)
        {
            if (!StartJHReadPower(out errMsg))
                return false;

            if (!ReadJHPowerValue(ref dblValue1, ref dblValue2, out errMsg))
                return false;

            return true;
        }

        /// <summary>
        /// 开始读取功率
        /// 先写入指令byte数组send_buf={0xaa,0x08,0,0,0,0,0x08},再读取数据。如果m_Result[1] == 0x05 && m_Result[2] == 0x01则开始读取功率成功。
        /// </summary>
        /// <returns>true/false</returns>
        public bool StartJHReadPower(out string errMsg)
        {
            byte[] send_buf = new byte[7];

            m_Result = new char[9];

            send_buf[0] = 0xaa;
            send_buf[1] = 0x08;
            send_buf[2] = 0x0;
            send_buf[3] = 0x0;
            send_buf[4] = 0x0;
            send_buf[5] = 0x0;
            send_buf[6] = 0x08;

            m_BaseSession.RawIO.Write(send_buf, 0, 7);
            Thread.Sleep(50);
            byte[] byteRes;
            bool bSuccess = ReadComBytes(9,out byteRes, out errMsg);
            if (bSuccess&&byteRes.Length>2)   //读取成功
            {
                if (byteRes[1] == 0x05 && byteRes[2] == 0x01)
                    return true;
            }
            return false;
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

        /// <summary>
        /// 读取功率值
        /// 先写入指令byte数组send_buf={0xaa,0x07,0,0,0,0,0x07},再读取数据。
        /// </summary>
        /// <param name="dblValue1"></param>
        /// <param name="dblValue2"></param>
        /// <returns>true/false</returns>
        public bool ReadJHPowerValue(ref double dblValue1, ref double dblValue2, out string errMsg)
        {
            Thread.Sleep(100);
            byte[] send_buf = new byte[7];

            send_buf[0] = 0xaa;
            send_buf[1] = 0x07;
            send_buf[2] = 0x0;
            send_buf[3] = 0x0;
            send_buf[4] = 0x0;
            send_buf[5] = 0x0;
            send_buf[6] = 0x07;
            

            m_BaseSession.RawIO.Write(send_buf,0,7);
            Thread.Sleep(20);
            byte[] bres;
            bool bSuccess = ReadComBytes(9, out bres, out errMsg);
            if (bSuccess&&bres.Length==9)   //读取成功
            {
                //取字符串4~7这4为，index从0开始
                if (bres[8] == JHCheckXor(bres, 1, 7) && bres[1] == 0x11)
                {
                    byte[] byValue = new byte[4];
                    byValue[0] = bres[7];
                    byValue[1] = bres[6];
                    byValue[2] = bres[5];
                    byValue[3] = bres[4];
                    dblValue1 = CommonFunction.Memorytofloat(byValue);
                }
                else
                    dblValue1 = 0;
            }
            else
                return false;
            bSuccess = ReadComBytes(9, out bres, out errMsg);
            if (bSuccess && bres.Length == 9)   //读取成功
            {
                //取字符串4~7这4为，index从0开始
                if (bres[8] == JHCheckXor(bres, 1, 7) && bres[1] == 0x12)
                {
                    byte[] byValue = new byte[4];
                    byValue[0] = bres[7];
                    byValue[1] = bres[6];
                    byValue[2] = bres[5];
                    byValue[3] = bres[4];
                    dblValue2 = CommonFunction.Memorytofloat(byValue);
                }
                else
                    dblValue2 = 0;
            }
            else
                return false;

            return true;
        }

        /// <summary>
        /// 停止读取功率
        /// 先写入指令byte数组send_buf={0xaa,0x09,0,0,0,0,0x09},再读取数据，如果m_Result[1] == 0x05 && m_Result[2] == 0x00则停止读取功率成功。
        /// </summary>
        /// <returns>true/false</returns>
        public bool StopReadPower()
        {
           /* char[] send_buf = new char[7];

            send_buf[0] = (char)0xaa;
            send_buf[1] = (char)0x09;
            send_buf[2] = (char)0;
            send_buf[3] = (char)0;
            send_buf[4] = (char)0;
            send_buf[5] = (char)0;
            send_buf[6] = (char)0x09;

            try
            {
                m_BaseSession.RawIO.Write(Encoding.ASCII.GetBytes(send_buf), 0, 7);
                Thread.Sleep(20);
                m_Result = new char[9];
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 9, out actualCount, out readStatus);
                if (readStatus != ReadStatus.Unknown)   //读取成功
                {
                    if (m_Result[1] == 0x05 && m_Result[2] == 0x00)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }  */
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StoreRef()
        {
            m_BaseSession.RawIO.Write("L1\n");//medunm
            Thread.Sleep(200);
            if (GetControl() == 0)
            {
                m_BaseSession.RawIO.Write("L1\n");
            }

            m_BaseSession.RawIO.Write("G0\n");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 如果是1830，先写入"L?\n"，再读取，返回Convert.ToInt32(m_Result)
        /// 如果是嘉惠，返回0
        /// 如果是152，先写入"sens0:chan0:pow:read?\r"，再读取，如果读到数据返回1，否则返回0
        /// </returns>
        public int GetControl()
        {
            if (m_PWMType == PWMTypeEnum.PWM_1830 || m_PWMType == PWMTypeEnum.PWM_OPLK1830)
            {
                try
                {
                    m_BaseSession.RawIO.Write("L?\n");
                    Thread.Sleep(20);  //>15较好
                    string strRes;
                    string errMsg;
                    bool bSuccess = ReadComString(32,out strRes, out errMsg);
                    int temp = Convert.ToInt32(strRes);
                    return (temp);
                }
                catch(Exception ex)
                {
                    return 0;
                }
            }
            else if (m_PWMType == PWMTypeEnum.PWM_JH)
            {
                return 0;
            }
            else if (m_PWMType == PWMTypeEnum.PWM_OPLKR152)
            {
                try
                {
                    m_BaseSession.RawIO.Write("sens0:chan0:pow:read?\r");
                    Thread.Sleep(30);
                    string strRes;
                    string errMsg;
                    bool bSuccess = ReadComString(32,out strRes, out errMsg);

                    if (bSuccess)
                    {
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            
            return 0;
            
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetZeroControl()
        {
            m_BaseSession.RawIO.Write("Z1\n");
            Thread.Sleep(50);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearZeroControl()
        {
            m_BaseSession.RawIO.Write("Z0\n");
            Thread.Sleep(50);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 如果是1830，先读取，后写入"Z?\n"，再读取，返回Convert.ToInt32(m_Result)
        /// 其他返回1
        /// </returns>
        public int GetZeroControl()
        {
            if (m_PWMType == PWMTypeEnum.PWM_1830||m_PWMType==PWMTypeEnum.PWM_OPLK1830)
            {
                m_Result = new char[32];
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32, out actualCount, out readStatus);
                m_Result[actualCount] = '\0';
                m_BaseSession.RawIO.Write("Z?\n");
                Thread.Sleep(200);//>15较好

                m_Result = new char[32];
                Thread.Sleep(1500);
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32, out actualCount, out readStatus);
                m_Result[actualCount] = '\0';
                int temp = Convert.ToInt32(m_Result);
                return (temp);
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 重设1830
        /// </summary>
        public void ReSet1830()
        {
            //设置测量参数(可以直接在仪器上设置)
            //1、Average of the measurements,same as Filter (F1:16点, F2:4点, F3:1点)
            m_BaseSession.RawIO.Write("F2\n");//medunm 

            //2、Units(U1:Watts, U2:dB, U3:dBm, U4:REF)
            m_BaseSession.RawIO.Write("U2\n");//dB 

            //4、Set Range of the input signal (R0,R1,...R8)
            m_BaseSession.RawIO.Write("R0\n");//Auto
            //5、Store reference power level for any future dB
        }

        /// <summary>
        /// 没明白
        /// </summary>
        /// <returns>true/false</returns>
        public bool CheckControl()
        {
            m_Result = new char[32];
            actualCount = 0;
            readStatus = ReadStatus.Unknown;
            m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32, out actualCount, out readStatus);
            m_Result[actualCount] = '\0';
            m_BaseSession.RawIO.Write("D?\n");
            Thread.Sleep(200);//>15较好

            m_Result = new char[32];
            actualCount = 0;
            readStatus = ReadStatus.Unknown;
            m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32, out actualCount, out readStatus);
           
            if (m_Result[0] == 0)
            {
                Thread.Sleep(1000);

                m_Result = new char[32];
                actualCount = 0;
                readStatus = ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(Encoding.ASCII.GetBytes(m_Result), 0, 32, out actualCount, out readStatus);
               
                if (m_Result[0] == 0)
                {
                    return false;
                }
            }

            return true;
        }
    
    }
}
