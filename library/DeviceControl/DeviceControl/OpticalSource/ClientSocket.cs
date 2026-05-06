using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using MolexUtility;

namespace DeviceControl
{
    public class ClientSocket
    {
        IPEndPoint ipe;
        private byte[] MsgBuffer = new byte[2048];
        private Socket clientSocket;
        List<string> msgLists = new List<string>();

        public delegate void SeverDataDealDelegate(string recData);
        public static SeverDataDealDelegate SeverDataDeal = null;
        public ClientSocket(string host,int port)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(host);
                ipe = new IPEndPoint(ip, port);
                
            }
            catch (Exception ex)
            {
                
            }
        }

        public void CloseSocket()
        {
            if(clientSocket!=null)
            {
                clientSocket.Close();
            }
        }

        public bool ConnectSever(ref string errMsg,bool isRec=true)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(ipe);
                if(isRec)
                {
                    ReceiveData();
                }
                
                errMsg = "";
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        public bool SendData(string data, ref string errMsg)
        {
            try
            {
                byte[] sendBytes = Encoding.UTF8.GetBytes(data);
                CommonFunction.WriteLog(data);
                clientSocket.Send(sendBytes);
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        //声明整个方法为线程同步
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void AddRevData(string recData)
        {
            recData = recData.Replace("\0", "");
            msgLists.Add(recData);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private string GetFirstRevData()
        {
            string data = "";
            if(msgLists.Count>0)
            {
                data = msgLists[0];
                msgLists.RemoveAt(0);
            }

            return data;
        }

        private void RecDataDealThread()
        {


            while (true)
            {
                string recData = GetFirstRevData();               
                if (recData != "")
                {
                    CommonFunction.WriteLog("GetFirstRevDat");
                    CommonFunction.WriteLog(recData);
                    if (SeverDataDeal != null)
                    {
                        SeverDataDeal(recData);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

        }

        public string ReadData(ref string errMsg)
        {
            try
            {
                byte[] readMsg = new byte[2048];

                clientSocket.Receive(readMsg);
                string readStr = Encoding.GetEncoding("gb2312").GetString(readMsg);
                return readStr;
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
                return "";
            }
        }

        public void ReceiveData()
        {
            clientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, 0, new AsyncCallback(ReceiveCallback), null);
            Thread dealDataThread = new Thread(new ThreadStart(RecDataDealThread));
            dealDataThread.Start();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int REnd = clientSocket.EndReceive(ar);
                if (REnd > 0)
                {
                    byte[] data = new byte[REnd];
                    
                    Array.Copy(MsgBuffer, 0, data, 0, REnd);

                    //在此次可以对data进行按需处理
                    string revMsg = Encoding.GetEncoding("gb2312").GetString(MsgBuffer);
                    CommonFunction.WriteLog("ReceiveCallback");
                    CommonFunction.WriteLog(revMsg);
                    AddRevData(revMsg);
                    
                    Array.Clear(MsgBuffer, 0, 2048);
                    
                    clientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, 0, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    dispose();
                }
            }
            catch (SocketException ex)
            { }
        }

        private void dispose()
        {
            try
            {
                this.clientSocket.Shutdown(SocketShutdown.Both);
                this.clientSocket.Close();
            }
            catch (Exception ex)
            { }
        }
    }
}
