using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using MolexUtility.Device;
using Agilent.LWD.Ag86038x.InstrumentObjects;
using Agilent.LWD.Ag86038x;
using MolexUtility;


namespace DeviceControl
{
    public class CDScan:ICDScan
    {
        public RemoteClient.Communicator pdlaClient = new RemoteClient.Communicator();
        public ODARemoting.NewStatusDelegate NewStatusHandler;
        public ODARemoting.TriggerProgressDelegate TriggerHandler;

        private bool isConnected = false;
        private bool isPDLScan = false;
        private bool isDoRef = false;
        private int isScanCompleted = -1;
        private string rawdataPath = "";
        private static AutoResetEvent getDataEvent = new AutoResetEvent(false);
        private static AutoResetEvent serverMsgEvent = new AutoResetEvent(false);
        private string serverMsg = "";
        private ODACommon.enumStatus scanStatus = ODACommon.enumStatus.START;

        private object lockObj = new object();
        public void NewStatusEvent(string msg, ODACommon.eEventLogType e)
        {
            lock(lockObj)
            {
                serverMsg = msg;
                CommonFunction.WriteLog(serverMsg);
                if (serverMsg.ToUpper().Contains("CLIENT CONNECTED") == true)
                {
                    isConnected = true;
                }
            }
            //serverMsgEvent.Reset();
            //label_status.Text = msg;
            //MessageBox.Show("Instrument Connect Successful!");
        }

        public string GetNewStatusMsg()
        {
            string msg = "";
            lock (lockObj)
            {
                msg = serverMsg;
                serverMsg = "";
            }
            return msg;
        }

        public bool GetIsConnect()
        {
            bool buf = false;
            lock (lockObj)
            {
                buf=isConnected;
            }
            return buf;
        }

        public int GetScanCompleted()
        {
            int buf = -1;
            lock (lockObj)
            {
                buf = isScanCompleted;
                //CommonFunction.WriteLog(string.Format("GetScanCompleted:{0}", isScanCompleted));
            }
            return buf;
        }

        public void DisConnect()
        {
            if(GetIsConnect())
            {
                pdlaClient.Connectivity.Disconnect();
            }
        }

        public void getResultValue()
        {
            double[] dblIL = pdlaClient.Results.YData(ODACommon.eMeasurementType.Gain, ODACommon.eODAPort.One);
            double[] dblGD = pdlaClient.Results.YData(ODACommon.eMeasurementType.GD, ODACommon.eODAPort.One);
            double[] dblPhase = pdlaClient.Results.YData(ODACommon.eMeasurementType.OptPhase, ODACommon.eODAPort.One);

            int nPoint1 = dblIL.Length;

            double[] dblCD = pdlaClient.Results.YData(ODACommon.eMeasurementType.CD, ODACommon.eODAPort.One);
            int nPoint2 = dblCD.Length;

            double xStart1 = pdlaClient.Results.XStart(ODACommon.eMeasurementType.Gain);
            double xStop1 = pdlaClient.Results.XStop(ODACommon.eMeasurementType.Gain);
            double xStart2 = pdlaClient.Results.XStart(ODACommon.eMeasurementType.CD);
            double xStop2 = pdlaClient.Results.XStop(ODACommon.eMeasurementType.CD);
            double xStep1 = (xStop1 - xStart1) / (nPoint1 - 1);
            double xStep2 = (xStop2 - xStart2) / (nPoint2 - 1);

            double[] dblFreq1 = new double[nPoint1];
            double[] dblFreq2 = new double[nPoint2];
            double[] dblPDL = null;
            double[] dblPMD = null;
            Array.Clear(dblFreq1, 0, dblFreq1.Length);
            Array.Clear(dblFreq2, 0, dblFreq2.Length);
            double dblCSpeed = 299792458.458;
            for (int i = 0; i < nPoint1; i++)
            {
                dblFreq1[i] = dblCSpeed / (xStart1 + i * xStep1);
            }

            if (isPDLScan == true)
            {
                dblPDL = pdlaClient.Results.YData(ODACommon.eMeasurementType.PDL, ODACommon.eODAPort.One);
                dblPMD = pdlaClient.Results.YData(ODACommon.eMeasurementType.PMD, ODACommon.eODAPort.One);
            }

            for (int i = 0; i < nPoint2; i++)
            {
                dblFreq2[i] = dblCSpeed / (xStart2 + i * xStep2);
            }
            string path = string.Format("{0}\\rawdata\\CDScanRawdata.csv", Environment.CurrentDirectory);
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            SaveTestData(path, dblFreq1, dblFreq2, dblGD, dblIL, dblPhase, dblCD, dblPDL, dblPMD);
            //getDataEvent.Set();
        }

        private void SaveTestData(string path, double[] dblFreq1, double[] dblFreq2, double[] dblGD, double[] dblIL, double[] dblPhase, double[] dblCD, double[] dblPDL, double[] dblPMD)
        {
            string filename = path;

            StreamWriter swRes = new StreamWriter(filename);
            swRes.WriteLine("Freq,GD,IL/Gain,Phase,PDL,PMD,Freq,CD");

            int i = 0;
            int nFre2Length = dblFreq2.Length;
            int nFre1Length = dblFreq1.Length;
            int nCirleCount = nFre2Length;
           
            for (i = 0; i < nCirleCount; i++)
            {
                if (Math.Abs(dblFreq2[i]) < 100)
                    break;

                string strwrite = "";
                if (dblPDL == null || dblPMD == null)
                {
                    strwrite = dblFreq1[i].ToString() + ","
                + dblGD[i].ToString() + ","
                + dblIL[i].ToString() + ","
                + dblPhase[i].ToString() + ","
                + "0,"  //PDL
                + "0,"  //PMD
                + dblFreq2[i].ToString() + ","
                + dblCD[i].ToString();
                }
                else
                {
                    strwrite = dblFreq1[i].ToString() + ","
                + dblGD[i].ToString() + ","
                + dblIL[i].ToString() + ","
                + dblPhase[i].ToString() + ","
                + dblPDL[i].ToString() + ","
                + dblPMD[i].ToString() + ","
                + dblFreq2[i].ToString() + ","
                + dblCD[i].ToString();
                }

                swRes.WriteLine(strwrite);
            }
            //Freq1多一行
            if (dblPDL == null || dblPMD == null)
            {
                swRes.WriteLine(dblFreq1[i].ToString() + ","
                            + dblGD[i].ToString() + ","
                            + dblIL[i].ToString() + ","
                            + dblPhase[i].ToString() + ","
                            + "0,"
                            + "0,"
                            + ",");
            }
            else
            {
                swRes.WriteLine(dblFreq1[i].ToString() + ","
                            + dblGD[i].ToString() + ","
                            + dblIL[i].ToString() + ","
                            + dblPhase[i].ToString() + ","
                            + dblPDL[i].ToString() + ","
                            + dblPMD[i].ToString() + ","
                            + ",");
            }

            swRes.Close();
        }


        public void TriggerProgessEvent(ODACommon.enumStatus status, ODACommon.enumAcquisitionMode acqMode)
        {
            try
            {
                scanStatus = status;
                CommonFunction.WriteLog(string.Format("ODACommon.enumStatus:{0},ODACommon.enumAcquisitionMode:{1}", status, acqMode));
                if (acqMode == ODACommon.enumAcquisitionMode.eNormalization)
                {
                    if ((status == ODACommon.enumStatus.COMPLETE) || (status == ODACommon.enumStatus.COMPLETE_WARN)
                        || (status == ODACommon.enumStatus.ABORTED))
                    {
                        //button_Normalize.Enabled = true;
                        //SaveData(strCurFile);
                        //MessageBox.Show("instrument normalize complete!");
                        //getDataEvent.Set();
                        lock (lockObj)
                        {
                            isScanCompleted = Convert.ToInt32(status);
                            CommonFunction.WriteLog(string.Format("isScanCompleted:{0}", isScanCompleted));
                        }
                    }
                }

                if (acqMode == ODACommon.enumAcquisitionMode.eMeasurement)
                {
                    if ((status == ODACommon.enumStatus.COMPLETE) || (status == ODACommon.enumStatus.COMPLETE_WARN))
                    {
                        //CommonFunction.WriteLog("scan finish");
                        getResultValue();
                        //CommonFunction.WriteLog("getResultValue");
                        lock (lockObj)
                        {
                            isScanCompleted = Convert.ToInt32(status);
                            CommonFunction.WriteLog(string.Format("isScanCompleted:{0}", isScanCompleted));
                        }
                    }
                    else if(status == ODACommon.enumStatus.ABORTED)
                    {
                        //getDataEvent.Set();
                        lock (lockObj)
                        {
                            isScanCompleted = Convert.ToInt32(status);
                            CommonFunction.WriteLog(string.Format("isScanCompleted:{0}", isScanCompleted));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="serverAddr">服务器地址</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        public bool InitAndConnectServer(ref string errMsg, string serverAddr)
        {
            pdlaClient.Connectivity.Connect(serverAddr);

            this.NewStatusHandler = new ODARemoting.NewStatusDelegate(this.NewStatusEvent);
            pdlaClient.NewStatus += this.NewStatusHandler;

            this.TriggerHandler = new ODARemoting.TriggerProgressDelegate(this.TriggerProgessEvent);
            pdlaClient.TriggerProgress += this.TriggerHandler;
            
            
            //等待回复信息后才知是否成功，设置一个超时，超时则认为失败

           /* serverMsgEvent.WaitOne(1000);
            //rjf test
            if(serverMsg.ToUpper().Contains("CLIENT CONNECTED")==false)
            {

                if (serverMsg != null && serverMsg.Length > 0)
                    errMsg = serverMsg;
                else
                    errMsg = "连接出错";
                return false;
            }*/
            return true;
        }

        public int SetScanParam(double xStart,double xStop,double dRFModulFre,double dStep,int dIFBW)
        {
            pdlaClient.MeasurementRange.XStart = xStart;
            pdlaClient.MeasurementRange.XStop = xStop;
            pdlaClient.Resolution.RFModulationFrequency = dRFModulFre;
            pdlaClient.Resolution.Increment = dStep;
            pdlaClient.Sensitivity.IFBandwidth = dIFBW;
            return 0;
        }

        /// <summary>
        /// 扫描
        /// </summary>
        /// <param name="doPDL">是否带PDL true-带PDL，false-不带PDL</param>
        /// <param name="doRef">是否归零 true-归零 false-测试</param>
        /// <param name="dataPath">保存数据路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功，1-超时，此时需要重连服务器，2-失败</returns>
        public int Scan(bool doPDL, bool doRef, ref string dataPath, ref string errMsg)
        {
            lock (lockObj)
            {
                isScanCompleted=-1;
            }
            if (doPDL == true)
            {
                pdlaClient.DispersionMode = (ODACommon.eDispersionMode.CD_PMD_Swept);
                isPDLScan = true;
            }
            else
            {
                pdlaClient.DispersionMode = ODACommon.eDispersionMode.CD_Swept;
                isPDLScan = false;
            }
            if (doRef)
            {
                pdlaClient.GenerateMuellerAndPMDData = true;
                pdlaClient.Actions.Normalize(ODACommon.enumAcquisitionMode.eNormalization);
            }
            else
            {
                pdlaClient.Measure();
            }
            string path = string.Format("{0}\\rawdata\\CDScanRawdata.csv", Environment.CurrentDirectory);
            dataPath = path;
            //等待消息，等待数据
            /*serverMsgEvent.WaitOne();
            //rjf test
            if (serverMsg == "")
            {
                errMsg = serverMsg;
                return 1;
            }*/
            /* getDataEvent.WaitOne();

             if ((scanStatus == ODACommon.enumStatus.COMPLETE))
             {
                 dataPath = rawdataPath;
                 return 0;
             }
             else if ((scanStatus == ODACommon.enumStatus.COMPLETE_WARN))
             {
                 errMsg = serverMsg;
                 return 1;
             }
             else if ((scanStatus == ODACommon.enumStatus.ABORTED))
             {
                 errMsg = serverMsg;
                 return 2;
             }
             return 3;*/
            return 0;
        }

        /// <summary>
        /// 重连服务器
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功，true-成功，false-失败</returns>
        public bool Reconnect(ref string errMsg)
        {
            return true;
        }
    }
}
