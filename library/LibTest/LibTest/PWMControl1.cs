using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.Visa;
using Ivi.Visa;
using System.Threading;

namespace LibTest
{
    enum PWMTypeEnum
    {
        PWM_1830,
        PWM_JH,
        PWM_OPLKR152,
        PWM_OPLK1830
    }
    class PWMControl
    {
        private SerialSession m_BaseSession;
        private PWMTypeEnum m_PWMType;

        public void Clear()
        {
            if (m_BaseSession != null)
                m_BaseSession.Dispose();
        }

        public bool OpenPWM(string rsName,PWMTypeEnum pwmType,out string errMsg)
        {
            string strErr = "";
            errMsg = strErr;
            try
            {
                using (var rmSession = new ResourceManager())
                {
                    m_BaseSession = (SerialSession)rmSession.Open(rsName);

                    if ((m_PWMType == PWMTypeEnum.PWM_1830) || (m_PWMType == PWMTypeEnum.PWM_OPLK1830))
                    {
                        m_BaseSession.TimeoutMilliseconds = 200;
                        m_BaseSession.BaudRate = 9600;
                        m_BaseSession.DataBits = 8;
                        m_BaseSession.Parity = SerialParity.None;
                        m_BaseSession.StopBits = SerialStopBitsMode.One;
                        m_BaseSession.TerminationCharacterEnabled = true;
                        m_BaseSession.TerminationCharacter = 0xA;
                        //1、Average of the measurements,same as Filter (F1:16点, F2:4点, F3:1点)
                        m_BaseSession.RawIO.Write("F2\n");
                        //2、Units(U1:Watts, U2:dB, U3:dBm, U4:REF)
                        m_BaseSession.RawIO.Write("U2\n");
                        //4、Set Range of the input signal (R0,R1,...R8)
                        m_BaseSession.RawIO.Write("R0\n");
                        //5、Store reference power level for any future dB
                        //6、Turn Zero,subtract any background power level from future measurements.

                    }
                    /*else if (m_PWMType == PWMTypeEnum.PWM_JH)
                    {
                        m_BaseSession.TimeoutMilliseconds = 100;
                        m_BaseSession.BaudRate = 9600;
                        m_BaseSession.DataBits = 8;
                        m_BaseSession.Parity = SerialParity.None;
                        m_BaseSession.StopBits = SerialStopBitsMode.One;
                        m_BaseSession.TerminationCharacterEnabled = false;
                        
                    }*/
                    
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
            return true;
        }

        public bool SetPWMUnits(byte byChannelIndex, byte byUniteIndex)
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
                byte[] readBuf = new byte[9];
                long nActRead = 0;
                ReadStatus readStatus=ReadStatus.Unknown;
                m_BaseSession.RawIO.Read(readBuf, 0, 9, out nActRead, out readStatus);
                byte xor = Convert.ToByte(readBuf[1] ^ readBuf[2] ^ readBuf[3] ^ readBuf[4] ^ readBuf[5] ^ readBuf[6] ^ readBuf[7]);
                if (xor == readBuf[8] || xor == 0x55)
                {
                    if (readBuf[4] != byUniteIndex)
                        return false;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
