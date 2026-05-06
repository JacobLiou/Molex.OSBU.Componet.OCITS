// ClientSocket.cpp : implementation file
//

#include "stdafx.h"
#include "ClientSocket.h"
#include <Ws2tcpip.h>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>

using namespace std;

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

HANDLE g_SocketHanle =INVALID_HANDLE_VALUE;
/////////////////////////////////////////////////////////////////////////////
// CClientSocket
//IMPLEMENT_DYNAMIC(CClientSocket,CSocket)
CClientSocket::CClientSocket()
{
	
	//m_pArchiveIn = NULL;
	//m_pArchiveOut = NULL;
	//m_pFile = NULL;
	memset(m_SendBufFile, 0, 256);
	memset(m_RecBufFile, 0, 256);
	strcpy(m_SendBufFile, "sendbuf");
	strcpy(m_RecBufFile, "recvbuf");
	m_nMsgCount = 0;
	m_CurrentScanOKTime=0;

	bFinish=FALSE;
		  m_bUsualScan=FALSE;
		  m_dwScanDataCount=0;
		  m_bRefScan=FALSE;
		  m_bConnectNet=FALSE;
		  m_nPDLStatus=0;
		  m_bopendriver=TRUE;
		  m_bRealScanPDL=FALSE;
		  m_bCurrentMsgType=0;
		  m_bCalcResult=FALSE;
		  m_bReadReferenceWithPDL=FALSE;
		  m_bReadReferenceWithNoPDL = FALSE;
		  for (int i=0;i<4;i++)
		  {
			  g_pdbGetPowerptr[i] = (double*)VirtualAlloc(NULL,10000 * sizeof(double),
				  MEM_RESERVE | MEM_COMMIT,PAGE_READWRITE);
			  g_pdbGetPowerptrPDL[i] = (double*)VirtualAlloc(NULL,10000 * sizeof(double),
				  MEM_RESERVE | MEM_COMMIT,PAGE_READWRITE);
		  }
		  g_pdbWLptr = (double*)VirtualAlloc(NULL,10000 * sizeof(double),
			  MEM_RESERVE | MEM_COMMIT,PAGE_READWRITE);
		 
		  /*if (!AfxSocketInit())
		  {
			  AfxMessageBox(IDP_SOCKET_FAILED);
			  return;
		  }*/
		  WORD wVersionRequested;
		  WSADATA wsaData;
		  int err;

		  wVersionRequested = MAKEWORD(1, 1);
		  err = WSAStartup(wVersionRequested, &wsaData);//加载Winsocket DLL
		  if (err != 0)
		  {
			  return;
		  }

		  if (LOBYTE(wsaData.wVersion) != 1 || HIBYTE(wsaData.wVersion) != 1)
		  {
			  WSACleanup();
			  return;
		  }

		  g_SocketHanle = CreateEvent(NULL, FALSE, FALSE, NULL);
}

CClientSocket::~CClientSocket()
{
	//m_Socket.Close();
	/*if(m_pArchiveOut != NULL)
	{
		delete m_pArchiveOut;
		m_pArchiveOut = NULL;
	}
	if(m_pArchiveIn != NULL)
	{
		delete m_pArchiveIn;
		m_pArchiveIn = NULL;
	}
	if(m_pFile != NULL)
	{
		delete m_pFile;
		m_pFile = NULL;
	}*/
}


// Do not edit the following lines, which are needed by ClassWizard.
#if 0
BEGIN_MESSAGE_MAP(CClientSocket, CSocket)
//{{AFX_MSG_MAP(CClientSocket)
//}}AFX_MSG_MAP
END_MESSAGE_MAP()
#endif	// 0

/////////////////////////////////////////////////////////////////////////////
// CClientSocket member functions

//DEL CClientSocket::CClientSocket(CCTestServerDoc *pDoc)
//DEL {
//DEL 	ASSERT(pDoc != NULL);
//DEL 	if(pDoc == NULL)
//DEL 		m_pDoc = pDoc;
//DEL 	m_nMsgCount = 0;
//DEL 	m_pArchiveIn = NULL;
//DEL 	m_pArchiveOut = NULL;
//DEL 	m_pFile = NULL;
//DEL }

void CClientSocket::Init()
{
	
	/*m_pFile = new CSocketFile(this);
	m_pArchiveIn = new CArchive(m_pFile , CArchive::load);
	m_pArchiveOut = new CArchive(m_pFile , CArchive::store);*/
	
}

void CClientSocket::SendMsg(CSendMsg *pMsg)
{
	CFile *pFile = new CFile(m_SendBufFile, CFile::modeCreate | CFile::modeWrite);
	CArchive *pAr = new CArchive(pFile, CArchive::store);
	pMsg->Serialize(*pAr);
	pAr->Flush();
	pAr->Close();
	pFile->Close();
	delete pAr;
	delete pFile;
	pFile = new CFile(m_SendBufFile, CFile::modeRead);
	char ch[10 * 1024] = { 0 };
	int length = pFile->GetLength();
	pFile->Read(ch, length);
	int res=send(m_Socket, ch, length, 0);
	pFile->Close();
	delete pFile;
	DeleteFile(m_SendBufFile);
	if (res != length || res == SOCKET_ERROR)
	{
		int errCode = WSAGetLastError();
		m_strShowMSG.Format("连接不上服务器，错误码：%d,请检查网络和配置文件！", errCode);
	}	
}


CSendMsg* CClientSocket::ReceiveMsg()
{
	CSendMsg *pMsg = NULL;
	//pMsg->Serialize(*m_pArchiveIn);
	char chRec[1024 * 10] = { 0 };
	int timeout = 1000;
	setsockopt(m_Socket, SOL_SOCKET, SO_RCVTIMEO, (char *)&timeout, sizeof(struct timeval));
	int recLen = recv(m_Socket, chRec, 1024 * 10, 0);
	if (recLen > 0)
	{
		pMsg = new CSendMsg();
		CFile *pRecvFile = new CFile(m_RecBufFile, CFile::modeCreate | CFile::modeWrite);
		pRecvFile->Write(chRec, recLen);
		pRecvFile->Close();
		delete pRecvFile;
		pRecvFile = new CFile(m_RecBufFile, CFile::modeRead);
		CArchive *pAread = new CArchive(pRecvFile, CArchive::load);
		pMsg->Serialize(*pAread);
		pAread->Close();
		pRecvFile->Close();
		delete pAread;
		delete pRecvFile;
		DeleteFile(m_RecBufFile);
	}
	return pMsg;
}


void CClientSocket::CloseSocket()
{
	closesocket(m_Socket);
}
BOOL CClientSocket::ProcessReceive()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CSendMsg* pMsg=NULL;
	//AfxMessageBox("test4");
	int timeout = 180 * 1000;
	int curTick = GetTickCount();
	int nowTick = GetTickCount();
	while(!bFinish)
	{
		nowTick = GetTickCount();
		if ((nowTick - curTick) > timeout)
		{
			m_strShowMSG = "扫描数据返回超时！";
			return FALSE;
		}
		do 
		{
			pMsg = ReadMsg();
			if (pMsg == NULL)
			{
				break;
			}
			else
			{
				ParesReciveMsg(pMsg);
				if (pMsg->m_bClose)
				{
					delete pMsg;
					break;
				}
				delete pMsg;
			}
			
		} while(1);
		Sleep(10);
	}
	return TRUE;
}
CSendMsg* CClientSocket::ReadMsg()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CSendMsg *pSendMsg = NULL;
	CString strMsg;
	TRY
	{
		pSendMsg=ReceiveMsg();
	}
	CATCH (CFileException,e) 
	{
	}
	END_CATCH
		return pSendMsg;
}
void CClientSocket::SendStopScan()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	m_bUsualScan = FALSE;
	CSendMsg tempMsg;
	tempMsg.m_byMsgType = MSG_TYPE_STOP_SCAN;
	tempMsg.m_nClientCHIndex = m_ClientoServerInfo.m_nClientPortIndex;
    SendMsg(&tempMsg);
	
	
}
BOOL CClientSocket::TLSScan(BOOL bDoPDL,BOOL bDoRef,int nPort)
{
	//AfxSocketInit();
	//AfxMessageBox("test2");
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CString strPrintMsg;
	for (int i = 1;i<5;i++)
	{
		//如果是测试，先删除rawdata
		if (!bDoRef)
		{
			CString strUsedData = "";
			strUsedData.Format("%s\\PDL%d_RawData_CH%d.csv", m_ClientoServerInfo.m_tszClentDatapath, i, m_ClientoServerInfo.m_nClientTestPort);
			DeleteFile(strUsedData);
			strUsedData.Format("%s\\PDL%d_RawData_CH%d.dat", m_ClientoServerInfo.m_tszClentDatapath, i, m_ClientoServerInfo.m_nClientTestPort);
			DeleteFile(strUsedData);
		}
	}
	CSendMsg tempMsg;
	if (bDoPDL)
		tempMsg.m_byMsgType = MSG_TYPE_PDL_SCAN;
	else
		tempMsg.m_byMsgType = MSG_TYPE_NOPDL_SCAN;
   m_bUsualScan=TRUE;
    m_bRealScanPDL=bDoPDL;
	m_bRefScan = bDoRef;
	tempMsg.m_byPDLScan = bDoPDL;
	m_ClientoServerInfo.m_nClientTestPort=nPort;
	tempMsg.m_nClientCHIndex = m_ClientoServerInfo.m_nClientPortIndex;
	m_bCurrentMsgType = tempMsg.m_byMsgType;
	SendMsg(&tempMsg);
	bFinish = FALSE;
	//AfxMessageBox("test1");
	return ProcessReceive();
	
	
}
BOOL CClientSocket::ConnectServer()
{	
	CString strTemp,strHostName,strMsg;
	DWORD  dwSize = 1024;

	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	m_Socket = socket(AF_INET, SOCK_STREAM, 0);
	if(!m_bConnectNet)
	{
		SOCKADDR_IN addrSrv;//socketAddress socket端口
							//服务器端口配置
		addrSrv.sin_family = AF_INET;
		addrSrv.sin_port = htons(m_ClientoServerInfo.m_nPort);

		inet_pton(AF_INET, m_ClientoServerInfo.m_tszServerIP, &addrSrv.sin_addr);
		////作为客户端，你要连接【connect】到远端的服务器，也是要指定远端服务器的（ip, port）对。

		int res = connect(m_Socket, (SOCKADDR *)&addrSrv, sizeof(SOCKADDR));
		if (res == 0)
		{
			CSendMsg msg;
			msg.m_byMsgType = MSG_TYPE_NEWCLIENT;
			msg.m_strUserName = m_ClientoServerInfo.m_tszClientName;
			msg.m_nClientCHIndex = m_ClientoServerInfo.m_nClientPortIndex;
			msg.m_strIpAddress = m_ClientoServerInfo.m_tszClientIP;

			msg.m_nUserPort = m_ClientoServerInfo.m_nPort;
			SendMsg(&msg);
			m_bConnectNet = TRUE;
			if(!ProcessReceive())
				return false;
			if (!m_bopendriver)
			{
				return false;
			}
		}
		else if (res == SOCKET_ERROR)
		{
			int errCode=WSAGetLastError();
			m_strShowMSG.Format("连接不上服务器，错误码：%d,请检查网络和配置文件！", errCode);
			return false;
		}
			
	}
	
	return TRUE;
}
void CClientSocket::ParesReciveMsg(CSendMsg *pMsg)
{
	//AfxMessageBox("test5");
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if(pMsg == NULL)
	{
		m_strShowMSG="命令解析出错!";
		SendStopScan();
		return;
	}
	CSendMsg TempMsg;
	CString sql,strtemp;
	DWORD index = 0;
	BOOL  bError = FALSE;
	
	switch(pMsg->m_byMsgType)
	{
	case MSG_TYPE_REFERENCE_OK:
		break;
	case MSG_TYPE_TLS_SCAN_OK:		
		{
			m_dwScanDataCount = pMsg->m_dwScanCount;		
			/*if (m_bRefScan)
			{
				//LogInfo("接收扫描数据成功!");
				if (m_bRealScanPDL) 
				{
					m_nPDLStatus = pMsg->m_nPDLStatus;
					if (m_nPDLStatus != -1)//无效或者NOPDL数据
					{
						if (!SavePDLReference()) 
						{					
							SendStopScan();
							bFinish = TRUE;
							return;
						}
						
					}
				}
				else 
				{
					if (!SaveNOPDLReference())
					{				
						SendStopScan();
						bFinish = TRUE;
						return;
					}
					
				}
			}
			else if (m_bUsualScan)*/
			{
				
				if (m_bCurrentMsgType==MSG_TYPE_PDL_SCAN && !m_bCalcResult)
				{
					m_nPDLStatus = pMsg->m_nPDLStatus;
					if (m_nPDLStatus != -1)//无效或者NOPDL数据
					{
						//归零和非归零分开处理
						/*if (m_bRefScan)
						{
							GetPDLRefDataAndDisplay(m_nPDLStatus);
						}
						else*/
						{
							GetPDLScanDataAndDisplay(m_nPDLStatus);
						}
					}
				}
				else if (m_bCurrentMsgType==MSG_TYPE_NOPDL_SCAN && !m_bCalcResult)
				{
						GetNOPDLScanDataAndDisplay();
					//		Showinfo("GetNOPDLScanDataAndDisplay!",FALSE);
				}
			}
			
		}
		
		break;
	case MSG_TYPE_ASE:
		if (pMsg->m_bASE) 
		{
			//	LogInfo("ASE光源切换成功！");		
		}
		else
		{
			//	LogInfo("ASE光源切换失败！");
		}
		
		break;
	case MSG_TYPE_EMPTY:
		//	LogInfo("接收一条空消息!");
		break;
	case MSG_TYPE_SERVER_REFFILE:
		
		break;
		  case MSG_TYPE_FINSH_REFFILE:
			  
			  break;
		  case MSG_TYPE_SERVER_CLOSE:
			  //			  LogInfo("服务器退出!");
			  CloseSocket();
			  m_bConnectNet = FALSE;
			  //			  m_bClose = TRUE;
			  pMsg->m_bClose = TRUE;
			  break;
		  case MSG_TYPE_SERVER_NOREFFILE:
			  
			  //			  LogInfo("无归零文件!");
			  break;
		  case MSG_TYPE_TLS_HAS:
		  /*
		  m_b8164Open = pMsg->m_b8164Open;
		  if(m_b8164Open)
		  {
		  LogInfo("激光器已打开！");
		  }
		  else
		  {
		  LogInfo("激光器未打开!");
		  }
		  m_b8169Open = pMsg->m_b8169Open;
		  if(m_b8169Open)
		  {
		  LogInfo("偏振控制仪打开!");
		  }
		  else
		  {
		  LogInfo("偏振控制仪未打开!");
		  }
			  */
			  break;
		  case MSG_TYPE_ERROR:
			  //			  LogInfo(pMsg->m_strErrorMsg);
			  SendStopScan();
			  //			  m_bHaveScanTask = FALSE;
			  break;
		  case MSG_TYPE_TLS_SCAN_FAIL:
			  SendStopScan();
			  //			  LogInfo("服务器端激光器扫描出现错误,请重新打开服务器端软件连接设备!");
			  break;
		  case MSG_TYPE_DEVICE_OPEN_OK:
			  
			  bFinish = TRUE;
			  m_bopendriver=TRUE;
			  if (pMsg->m_b8164Open)
			  {
				  //		  Showinfo("激光器已打开！",FALSE);
			  }
			  else
			  {
				  m_bopendriver=false;
				  m_strShowMSG="激光器未打开！";
			  }
			  if (pMsg->m_b8169Open)
			  {
				  //			  Showinfo("偏振控制仪已打开！",FALSE);
			  }
			  else
			  {
				  m_bopendriver=false;
				  m_strShowMSG="偏振控制仪未打开！";
			  }
			  if (pMsg->m_strRefAlphaTime.IsEmpty())
			  {
				  m_bopendriver=false;
				  m_strShowMSG="系统Alpha角度未归零！";
			  }
			  else
			  {
				  CString strTemp="";
				  strTemp.Format("系统Alpha角度归零值为%.2f", pMsg->m_dbAlphaData);
				  m_strShowMSG = strTemp;
				  //				  Showinfo(strTemp,FALSE);
				  //strTemp.Format("系统Alpha角度归零时间为%s",pMsg->m_strRefAlphaTime);
				  //				  Showinfo(strTemp,FALSE);
			  }
			  break;
		
		  case MSG_TYPE_REFERENCE_NEXTCH:
			  
			  break;
		  case MSG_TYPE_SCAN_REFDATA:
			  
			  break;
		  case MSG_TYPE_SCAN_DATA:
			  
			  break;
		  case MSG_TYPE_CLIENT_ALPHA:
			  
			  break;
		  case MSG_TYPE_CLIENT_NOREG:
			  //			  LogInfo("工位未注册，命令不接受，请联系工程师!");
			  break;
			  //		  case MSG_TYPE_HREAT_THROB:
			  //			  m_bHeart_Throb = TRUE;
			  //			  break;
		  case MSG_TYPE_TLS_SETTING_OK:
			  //			  LogInfo(pMsg->m_strErrorMsg);
			  
			  break;
		  case MSG_TYPE_PDL_COMPLETE:
			  //m_bIsPDLComplete = TRUE;
			  break;
		  default:
			  break;
	}
}
BOOL CClientSocket::SaveNOPDLReference()
{
	CString strTemp;
	CString strLocalFile;
	CString strNetFile;
	
	m_CurrentScanOKTime++;
	if (m_CurrentScanOKTime>1)
	{
		m_strShowMSG="不带PDL归零完成!";
		
		m_CurrentScanOKTime =0;
		m_bRefScan = FALSE;
		bFinish = TRUE;
		SendStopScan();
		strNetFile.Format("%s\\Test_CH%d.csv",m_ClientoServerInfo.m_tszServerDatapath,m_ClientoServerInfo.m_nClientPortIndex);		
		//strLocalFile.Format("%s\\ReferenceWithNOPDL%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,m_ClientoServerInfo.m_nClientTestPort);
		DeleteFile(m_strReferenceNOPDLFile);
		//m_strReferenceNOPDLFile=strLocalFile;
		if(!CopyFile(strNetFile, m_strReferenceNOPDLFile,FALSE))
		{
			m_strShowMSG="提取测试数据失败!";
			
			m_CurrentScanOKTime = 0;
			SendStopScan();
			return FALSE;
		}
		
		if (!ReadNoPDLRawDataFile(FALSE))
		{
			m_strShowMSG="读取归零数据失败!";
			
			m_CurrentScanOKTime = 0;
			SendStopScan();
			return FALSE;
		}
		
		
	}
	else
	{
		m_strShowMSG.Format("扫描第%d次归零完成!",m_CurrentScanOKTime);
		
	}
	return TRUE;
}
BOOL CClientSocket::SavePDLReference()
{
   	CString strTemp;
	CString strLocalFile;
	CString strNetFile;
	
	m_CurrentScanOKTime++;
	if (m_CurrentScanOKTime>4)
	{
		strTemp.Format("PDL状态%d归零完成!",m_nPDLStatus);	
		m_CurrentScanOKTime =0;
		m_bRefScan = FALSE;
		bFinish = TRUE;
		SendStopScan();
		for (int i=1;i<=4;i++)
		{
			strLocalFile.Format("%s\\PDL%d_RawData_CH%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,i,m_ClientoServerInfo.m_nClientTestPort);
			DeleteFile(strLocalFile);
			
			strNetFile.Format("%s\\PDL%d_Test_CH%d.csv",m_ClientoServerInfo.m_tszServerDatapath,i,m_ClientoServerInfo.m_nClientPortIndex);
		//	m_strReferencePDLFile=strLocalFile;
			if(!CopyFile(strNetFile,strLocalFile,FALSE))
			{
				m_strShowMSG="提取测试数据失败!";
				
				m_CurrentScanOKTime = 0;
				
				return FALSE;
			}
		}
		if (!ReadPDLRawDataFile(FALSE))
		{
			m_strShowMSG="读取扫描测试数据失败!";
			
			m_CurrentScanOKTime = 0;
			
			return FALSE;
		}
		//m_strReferencePDLFile.Format("%s\\ReferenceWithPDL%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,m_ClientoServerInfo.m_nClientTestPort);
		if (!SaveReferenceFile(TRUE))
		{
			m_strShowMSG="保存PDL归零数据失败!";
			
			m_CurrentScanOKTime = 0;
			
			return FALSE;
		}
		
		
	}
	else
	{
		m_strShowMSG.Format("PDL状态%d归零完成!",m_nPDLStatus);
		
	}
	return TRUE;
}
BOOL CClientSocket::SaveReferenceFile(BOOL bScanPDL)
{
	CString strFileName;
	CString strTemp,strData;
	FILE *fFilePtr =NULL;
	double* pdbData =NULL;
	DWORD dwDataCount=0;
	PAutoRawData        pAutoScanRawData = NULL;
	
	if (bScanPDL)
	{
		pAutoScanRawData = &m_stRefRawData;
		strFileName=m_strReferencePDLFile;
		dwDataCount = 5;
	}
	else
	{
		pAutoScanRawData = &m_stNoPDLRefRawData;
		strFileName=m_strReferenceNOPDLFile;
		dwDataCount = 1;
	}
	
	if ((fFilePtr = fopen(strFileName,"w"))==NULL)
	{
		AfxMessageBox("Save File error");
		return FALSE;
	}
	SYSTEMTIME curTime;
	GetLocalTime(&curTime);
	strTemp.Format("WL,Power,%d-%d-%d %d:%d:%d\n",curTime.wYear,curTime.wMonth, curTime.wDay,curTime.wHour,curTime.wMinute,curTime.wSecond);
	fprintf(fFilePtr,"%s",strTemp);
	
	
	for (int i=0;i<(int)m_dwScanDataCount;i++)
	{
		double dbTempData = (double)pAutoScanRawData->m_pdwWavelengthArray[i]/MULTY_DATA;
		strTemp.Format("%.3f",dbTempData);
		
		for (int nIndex=0;nIndex<(int)dwDataCount;nIndex++)
		{
			PLONG plData = (PLONG)pAutoScanRawData->m_pnLossArray[0]+nIndex*m_dwScanDataCount;
			dbTempData = ((double)plData[i])/MULTY_DATA;
			strData.Format(",%.3f",dbTempData);
			strTemp += strData;
		}
		fprintf(fFilePtr,"%s\n",strTemp);
	}
	if (fFilePtr !=NULL)
	{
		fclose(fFilePtr);
	}
	return TRUE;
}
BOOL CClientSocket::ReadNoPDLRawDataFile(BOOL bUsualRawData)
{	
	PLONG				PLongTempData = NULL;
	CString				strRawDataFile;
	char				pszThisLine[MAX_LINE];
    FILE*				pfCSVFile = NULL;
	char				ch1[256],ch2[256]; 
	PAutoRawData        pAutoScanRawData = NULL;
	if (bUsualRawData)
	{
		FreeRefPowerRawData(&m_stTestRawData);
		// allocate the reference raw data pointer
		if(!AllocateTestRawArray(&m_stTestRawData,CLIENT_CH_COUNT,m_dwScanDataCount))
		{
			AfxMessageBox("Allocate Reference raw data array pointer error");
			return FALSE;
		}
		// m_stRefRawData.m_pdwWavelengthArray
		pAutoScanRawData =(PAutoRawData)&m_stTestRawData;	
		strRawDataFile.Format("%s\\RawData_CH%d.csv",m_ClientoServerInfo.m_tszClentDatapath,m_ClientoServerInfo.m_nClientTestPort);
	}
	else
	{
		// free the reference raw data  
		FreeRefPowerRawData(&m_stNoPDLRefRawData);
		// allocate the reference raw data pointer
		if(!AllocateRefRawArray(&m_stNoPDLRefRawData,CLIENT_CH_COUNT,m_dwScanDataCount))
		{
			AfxMessageBox("Allocate Reference raw data array pointer error");
			return FALSE;
		}
		// m_stRefRawData.m_pdwWavelengthArray
		pAutoScanRawData =(PAutoRawData)&m_stNoPDLRefRawData;
		strRawDataFile = m_strReferenceNOPDLFile;
	}
	
	// set the wavelength array address is the allcated memory at the 
	
	
	//for (int i=1;i<5;i++)
	{
		PDWORD pdwWavelengthArray = pAutoScanRawData->m_pdwWavelengthArray;
		PLongTempData = (PLONG)(pAutoScanRawData->m_pnLossArray[0]);		
		pfCSVFile = fopen(strRawDataFile, "rt");
		if(NULL == pfCSVFile)
		{
			m_strShowMSG="Open PDL_file fail !";		
			return FALSE;
		}
		int nTemp=0;
		while(!feof(pfCSVFile))
		{
			ZeroMemory(pszThisLine, sizeof(char) * (MAX_LINE)); 
			
			//  If we hit end-of-file, or error, end this loop
			if(NULL == fgets((LPSTR)pszThisLine, (MAX_LINE), pfCSVFile))
				break; 
			
			nTemp++;
			if (nTemp >1)
			{
				sscanf((LPSTR)pszThisLine, "%[^','],%[^',']",ch1,ch2);	
				
				pdwWavelengthArray[nTemp-2] = (DWORD)(atof(ch1)*MULTY_DATA);
				
				PLongTempData[nTemp-2] = (LONG)(atof(ch2)*MULTY_DATA);
			}	
			//	YieldToPeers();
		} 
		fclose(pfCSVFile); 		
	}
	/*if (bUsualRawData) 
		DeleteFile(strRawDataFile);*/
	
	return TRUE;
}

BOOL CClientSocket::ConvertBinToCsv(char *chBinFile, char *chCsvFile)
{
	ifstream readStr(chBinFile, ios::binary);
	int filesize = 0;
	readStr.seekg(0, ios::end);
	filesize = readStr.tellg();
	readStr.seekg(0, ios::beg);
	if (filesize == 0)
	{
		readStr.close();
		return FALSE;
	}
	int nSampleCount = filesize / sizeof(double) / 2;
	double *pRead = new double[filesize / sizeof(double)];
	readStr.read((char *)pRead, filesize);
	readStr.close();

	FILE *fFilePtr = NULL;
	if ((fFilePtr = fopen(chCsvFile, "w")) == NULL)
	{
		delete[] pRead;
		return FALSE;
	}
	CString strTemp = "";
	strTemp.Format("WL,Power\n");
	for (int i = 0;i < nSampleCount;i++)
	{
		strTemp.Format("%s%lf,%lf\n", strTemp, pRead[i], pRead[nSampleCount+i]);
	}
	fprintf(fFilePtr, "%s", strTemp);
	fclose(fFilePtr);
	delete[] pRead;
	return TRUE;
}

void CClientSocket::GetPDLScanDataAndDisplay(int nPDLStatus)
{
	CString strTemp;
	CString strLocalFile;
	CString strNetFile;
	
	m_CurrentScanOKTime++;
	if (m_CurrentScanOKTime>5)
	{
		m_strShowMSG.Format("偏振态%d的光学扫描完成!",nPDLStatus);
		//		Showinfo(strTemp,FALSE);
		m_bCalcResult = TRUE;		
		bFinish = TRUE;
		
		m_CurrentScanOKTime = 0;
		SendStopScan();
		
		
		for (int i=1;i<=4;i++)
		{
			for (int j = 0;j < m_ClientoServerInfo.m_nPowermeterCount;j++)
			{
				if (m_ClientoServerInfo.m_nPowermeterPorts[j] == 0)
					continue;

				strLocalFile.Format("%s%d%d.dat", m_strClentDataFileFullName, j+1, i);

				DeleteFile(strLocalFile);

				strNetFile.Format("%s\\PDL%d_Test_CH%d.dat", m_ClientoServerInfo.m_tszServerDatapath, i, m_ClientoServerInfo.m_nPowermeterPorts[j]);

				if (!CopyFile(strNetFile, strLocalFile, FALSE))
				{
					m_strShowMSG = "提取测试数据失败!";

					m_CurrentScanOKTime = 0;
					m_bReadReferenceWithPDL = FALSE;
					SendStopScan();
					return;
				}
			}
		}
		WriteLog("begin deal data!", "scan.log");
		//将二进制文件转为csv格式
		for (int i = 1;i <= 4;i++)
		{
			for (int j = 0;j < m_ClientoServerInfo.m_nPowermeterCount;j++)
			{
				if (m_ClientoServerInfo.m_nPowermeterPorts[j] == 0)
					continue;

				strLocalFile.Format("%s%d%d.dat", m_strClentDataFileFullName, j + 1, i);
				CString strLocalCsv = "";
				strLocalCsv.Format("%s%d%d.csv", m_strClentDataFileFullName, j + 1, i);
				if (!ConvertBinToCsv(strLocalFile.GetBuffer(0), strLocalCsv.GetBuffer(0)))
					return;
			}
		}
		/*m_strShowMSG="带PDL光学扫描完成!";
		if (m_bReadReferenceWithPDL == FALSE)//如果没有读取归零数据。
		{
			WriteLog("read reference data!", "scan.log");
			m_strReferencePDLFile.Format("%s\\ReferenceWithPDL%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,m_ClientoServerInfo.m_nClientTestPort);
	
			if (!ReadReferenceData(TRUE))
			{
				m_strShowMSG="读取归零数据失败!";
				
				m_CurrentScanOKTime = 0;
				m_bReadReferenceWithPDL = FALSE;
				SendStopScan();
				return;
			}
			WriteLog("read reference data success!", "scan.log");
		}
		
		WriteLog("read raw data!", "scan.log");
		if (!ReadPDLRawDataFile())
		{
			m_strShowMSG="读取扫描数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithPDL = FALSE;
			SendStopScan();
			return;
		}
		WriteLog("AllocateResultRawArray!", "scan.log");
		ZeroMemory(&m_stResultRawData, sizeof(stAutoRawData));
		FreeResultRawData(&m_stResultRawData);	
		if(!AllocateResultRawArray(&m_stResultRawData,
			CLIENT_CH_COUNT,
			m_dwScanDataCount,
			TRUE))
		{
			m_strShowMSG="计算扫描数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithPDL = FALSE;
			SendStopScan();
			return;
		}
		
		WriteLog("CalculateTestResult!", "scan.log");
		if(!CalculateTestResult(&m_stRefRawData,&m_stTestRawData,&m_stResultRawData,m_dwScanDataCount,4))
		{
			m_strShowMSG="计算扫描结果数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithPDL = FALSE;
			SendStopScan();
			return;
		}
		WriteLog("SaveScanResult!", "scan.log");
		if (!SaveScanResult()) 
		{
			m_strShowMSG="保存计算扫描结果数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithPDL = FALSE;
			SendStopScan();
			return;
		}*/
		
		//	fnDisplayCurveToGraph() ;
		m_CurrentScanOKTime = 0;
		m_bCalcResult = FALSE;
	}
	else
	{
		m_strShowMSG.Format("偏振态%d的光学扫描完成!",nPDLStatus);
		
	}
}

void CClientSocket::GetPDLRefDataAndDisplay(int nPDLStatus)
{
	CString strTemp;
	CString strLocalFile;
	CString strNetFile;

	m_CurrentScanOKTime++;
	if (m_CurrentScanOKTime > 1)
	{
		m_strShowMSG.Format("偏振态%d的光学扫描完成!", nPDLStatus);
		//		Showinfo(strTemp,FALSE);
		m_bCalcResult = TRUE;
		bFinish = TRUE;

		m_CurrentScanOKTime = 0;
		SendStopScan();


		//for (int i = 1;i <= 4;i++)
		{
			for (int j = 0;j < m_ClientoServerInfo.m_nPowermeterCount;j++)
			{
				if (m_ClientoServerInfo.m_nPowermeterPorts[j] == 0)
					continue;

				strLocalFile.Format("%s%d.dat", m_strClentDataFileFullName, j + 1);

				DeleteFile(strLocalFile);

				strNetFile.Format("%s\\PDL%d_Test_CH%d.dat", m_ClientoServerInfo.m_tszServerDatapath, nPDLStatus, m_ClientoServerInfo.m_nPowermeterPorts[j]);

				if (!CopyFile(strNetFile, strLocalFile, FALSE))
				{
					m_strShowMSG = "提取测试数据失败!";

					m_CurrentScanOKTime = 0;
					m_bReadReferenceWithPDL = FALSE;
					SendStopScan();
					return;
				}
			}
			//将二进制文件转为之前的csv格式
			
			for (int j = 0;j < m_ClientoServerInfo.m_nPowermeterCount;j++)
			{
				if (m_ClientoServerInfo.m_nPowermeterPorts[j] == 0)
					continue;

				strLocalFile.Format("%s%d.dat", m_strClentDataFileFullName, j + 1);
				CString strLocalCsv = "";
				strLocalCsv.Format("%s%d.csv", m_strClentDataFileFullName, j + 1);
				if (!ConvertBinToCsv(strLocalFile.GetBuffer(0), strLocalCsv.GetBuffer(0)))
					return;
			}
			

		}
		WriteLog("begin deal data!", "scan.log");		
		m_CurrentScanOKTime = 0;
		m_bCalcResult = FALSE;
	}
	else
	{
		m_strShowMSG.Format("偏振态%d的光学扫描完成!", nPDLStatus);

	}
}

void CClientSocket::WriteLog(char * chLog, char * pFilename)
{
	return;
	CFileFind isFind;

	FILE *fp = NULL;
	if (pFilename == NULL)
		return;
	fp = fopen(pFilename, "a");
	if (fp != NULL)
	{
		fseek(fp, 0, SEEK_END);
		long len = ftell(fp);
		fclose(fp);
		fp = NULL;
		if (len>1024 * 1024 * 100)
		{
			DeleteFile(pFilename);
		}
	}

	fp = fopen(pFilename, "a");
	if (fp != NULL)
	{
		CTime		tmNow = CTime::GetCurrentTime();
		char EMsg[1024] = { 0 };
		sprintf(EMsg, "%d-%d-%d %d:%d:%d %d %s\r\n",
			tmNow.GetYear(), tmNow.GetMonth(), tmNow.GetDay(), tmNow.GetHour(), tmNow.GetMinute(), tmNow.GetSecond(), GetTickCount(), chLog);
		fprintf(fp, EMsg);
		fclose(fp);
	}

}
BOOL CClientSocket::AllocateResultRawArray(PAutoRawData pResultRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL)
{
	BOOL           bFunctionOK = FALSE;
	DWORD          dwChannelIndex;
	DWORD          dwLossArrayMultiple;
	
	try
	{
		// check the input parameter
		if(dwChannelCount == 0)
		{
			throw "Error: the scan channel count is 0 ";
		}
		// if not result raw data pointer is null
		if(pResultRawData->m_pdwWavelengthArray == NULL)
		{
			// allocate the wavelength array here
			pResultRawData->m_pdwWavelengthArray = 
				(PDWORD)VirtualAlloc(NULL,
				dwSampleCount * sizeof(DWORD),
				MEM_RESERVE | MEM_COMMIT,
				PAGE_READWRITE);
			
			if(pResultRawData->m_pdwWavelengthArray == NULL)
			{
				throw "Not enough memory for result wavelength array";
			}
		}
		
		// allocate the reference power raw data array
		for(dwChannelIndex = 0; dwChannelIndex < dwChannelCount;dwChannelIndex++)
		{
			if(bDoPDL)  // do PDL testing 
			{
#if 1 
				dwLossArrayMultiple = 4;
				// 0: the average Power array
				// 1: the PDL array
				// 2: the MaxIL array
				// 3: the MinIL array
#else
				dwLossArrayMultiple = 4;
#endif
			}
			else
			{
				dwLossArrayMultiple = 1;
				// 0: the average Power array
			}
			// if the result raw data is NULL
			// allocata the memory
			if(pResultRawData->m_pnLossArray[dwChannelIndex] == NULL)
			{
				pResultRawData->m_pnLossArray[dwChannelIndex] = 
					(LONG*)VirtualAlloc(NULL,
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG),
					MEM_RESERVE | MEM_COMMIT,
					PAGE_READWRITE);
				
				if(pResultRawData->m_pnLossArray[dwChannelIndex] == NULL)
				{
					throw "Not enough memory for result raw data array";
				}
				
				// Zeromemory the test power raw data
				ZeroMemory(pResultRawData->m_pnLossArray[dwChannelIndex],
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG));
			}
		} // end for (channelcount)
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		AfxMessageBox(ptszErrorMsg,MB_OK|MB_ICONERROR);
		bFunctionOK = FALSE;	
	}
	catch(...)
	{
		AfxMessageBox("Other Exception occured",MB_OK|MB_ICONERROR);
		bFunctionOK = FALSE;
	}
	return bFunctionOK;
}
BOOL CClientSocket::AllocateRefRawArray(PAutoRawData pRefPowerRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL)
{
	BOOL           bFunctionOK = FALSE;
	DWORD          dwChannelIndex;
	DWORD          dwLossArrayMultiple;
	
	try
	{
		// check the input parameter
		if(dwChannelCount == 0)
		{
			throw "Error: the scan channel count is 0 ";
		}
		// if not allocat the reference power raw data pointer
		if(pRefPowerRawData->m_pdwWavelengthArray == NULL)
		{
			// allocate the wavelength array here
			pRefPowerRawData->m_pdwWavelengthArray = 
				(PDWORD)VirtualAlloc(NULL,
				dwSampleCount * sizeof(DWORD),
				MEM_RESERVE | MEM_COMMIT,
				PAGE_READWRITE);
			
			if(pRefPowerRawData->m_pdwWavelengthArray == NULL)
			{
				throw "Not enough memory for reference wavelength array";
			}
		}
		
		// allocate the reference power raw data array
		for(dwChannelIndex = 0; dwChannelIndex < dwChannelCount;dwChannelIndex++)
		{
			if(bDoPDL)  // do PDL testing 
			{
				dwLossArrayMultiple = 5;
				// 0: the average Power array
				// 1: the horizontal power array
				// 2: the vertical power array
				// 3: the diagonal power array
				// 4: the right hand circuital power array
			}
			else
			{
				dwLossArrayMultiple = 1;
				// 0: the average Power array
			}
			// if not allocata the reference power raw data pointer
			if(pRefPowerRawData->m_pnLossArray[dwChannelIndex] == NULL)
			{
				pRefPowerRawData->m_pnLossArray[dwChannelIndex] = 
					(LONG*)VirtualAlloc(NULL,
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG),
					MEM_RESERVE | MEM_COMMIT,
					PAGE_READWRITE);
				
				if(pRefPowerRawData->m_pnLossArray[dwChannelIndex] == NULL)
				{
					throw "Not enough memory for reference Power raw data array";
				}
				
				// Zeromemory the reference power raw data
				ZeroMemory(pRefPowerRawData->m_pnLossArray[dwChannelIndex],
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG));
			}
			
		} // end for (channelcount)	
		// until to here ,the function is OK
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		AfxMessageBox(ptszErrorMsg,MB_OK|MB_ICONERROR);
		bFunctionOK = FALSE;		
	}
	catch(...)
	{
		AfxMessageBox("Other Exception occured",MB_OK|MB_ICONERROR);
		bFunctionOK = FALSE;
	}	
	return bFunctionOK;
}
BOOL CClientSocket::SaveScanResult()
{
#if 1		
	CString strFileName;
	CString strTemp;
	FILE *fFilePtr =NULL;
	double* pdbData =NULL;
	
	strFileName=m_strClentDataFileFullName;//,m_nDebugTime);
	//strFileName="E:\\Testdll\\Data\\Test1.csv";
	WriteLog(strFileName.GetBuffer(0), "scan.log");
	if ((fFilePtr = fopen(strFileName,"w"))==NULL)
	{
		AfxMessageBox("Save ScanResult File error");
		return FALSE;
	}
	strTemp.Format("WL(nm),IL(dB),PDL(dB),MaxIL(dB),MinIL(dB)\n");
	fprintf(fFilePtr,"%s",strTemp);
	PLONG plData = (PLONG)m_stResultRawData.m_pnLossArray[0];
	PLONG plPDLData = (PLONG)m_stResultRawData.m_pnLossArray[0] + m_dwScanDataCount;
	PLONG plMaxILData = (PLONG)m_stResultRawData.m_pnLossArray[0] + m_dwScanDataCount*2;
	PLONG plMinData = (PLONG)m_stResultRawData.m_pnLossArray[0] + m_dwScanDataCount*3;
	WriteLog("write data begin", "scan.log");
	for (int i=0;i<(int)m_dwScanDataCount;i++)
	{
		
		g_pdbWLptr[i]=((double)m_stResultRawData.m_pdwWavelengthArray[i])/MULTY_DATA;
        g_pdbGetPowerptr[0][i]=((double)plData[i])/MULTY_DATA;
        g_pdbGetPowerptrPDL[0][i]=((double)plPDLData[i])/MULTY_DATA;
		strTemp.Format("%.3f,%.3f,%.3f,%.3f,%.3f",((double)m_stResultRawData.m_pdwWavelengthArray[i])/MULTY_DATA,((double)plData[i])/MULTY_DATA,
			((double)plPDLData[i])/MULTY_DATA, ((double)plMaxILData[i]) / MULTY_DATA, ((double)plMinData[i]) / MULTY_DATA);
		fprintf(fFilePtr,"%s\n",strTemp);
	}
	WriteLog("write data finish", "scan.log");
	if (fFilePtr !=NULL)
	{
		fclose(fFilePtr);
		//						if (!CopyFile(strFileName,strNewFile,FALSE))
		//						{
		//							AfxMessageBox("保存文件失败!");
		//							FreeHP816XRawData(&m_stRawData);
		//							return FALSE;
		// 						}
	}
	
#endif
	//	m_nDebugTime++;
	return TRUE;
}
BOOL CClientSocket::CalculateTestResult(PAutoRawData pRefRawDataArray, PAutoRawData pTestRawDataArray, PAutoRawData pResultArray, DWORD dwSampleCount, BOOL bWithPDL)
{
	BOOL         bFunctionOK = FALSE;
	PLONG	     pLRefArray = NULL;
	PLONG        pLTestArray =NULL;
	PLONG        pLResultArray = NULL;	
	DWORD        dwSampleIndex;
	
	try 
	{
		if(pRefRawDataArray == NULL)
		{
			throw "The reference raw power array is NULL";
		}
		if(pTestRawDataArray == NULL)//Include Average Power
		{
			throw " The testing raw data array is NULL";
		}
		if(pResultArray == NULL)
		{
			throw "The result raw data array is NULL";
		}
		WriteLog("get WavelengthArray!", "scan.log");
		pResultArray->m_pdwWavelengthArray = pRefRawDataArray->m_pdwWavelengthArray;
		// calculate the IL Value
		//for(dwChannelIndex = 0;dwChannelIndex < dwChannelCount;dwChannelIndex ++)
		{	
			// set pdwDataArrya to the average Power array
			WriteLog("get RefArray!", "scan.log");
			pLRefArray = pRefRawDataArray->m_pnLossArray[0];
			if(pLRefArray  == NULL)
			{
				throw "The reference data power pointer is NULL";
			}
			
			WriteLog("get TestArray!", "scan.log");
			//Get Average Power
			pLTestArray = pTestRawDataArray->m_pnLossArray[0];
			if(pLTestArray == NULL)
			{
				throw " the testing data power pointer is NULL";
			}
			WriteLog("get ResultArray!", "scan.log");
			//Get the result Loss array address
			//在这里加计算最大损耗的函数
			pLResultArray = pResultArray->m_pnLossArray[0];
			if(pLResultArray == NULL)
			{
				throw " the result pointer is NULL";
			}
			
			WriteLog("calculate the average IL!", "scan.log");
			// calculate the average IL here
			for(dwSampleIndex = 0; dwSampleIndex < dwSampleCount; dwSampleIndex ++)
			{
				// calculate the IL value
				// set the average power
				pLResultArray[dwSampleIndex] =  pLRefArray[dwSampleIndex]-pLTestArray[dwSampleIndex];
				
				
				
			} // end sample index
			
			// calcultate the PDL value
			if(bWithPDL)
			{
				WriteLog("CalculatePDL!", "scan.log");
				if(!CalculatePDL(dwSampleCount,pLRefArray,pLTestArray,pLResultArray))
				{
					throw "Calculate the PDL value error";
				}		
			}		
		}// end for(dwChannelIndex = 0;dwChannelIndex < p8164Param->m_dwChannelNumber;dwChannelIndex ++)
		
		// until to here the function is OK now
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		AfxMessageBox(ptszErrorMsg);
		bFunctionOK = FALSE;
	}
	catch(...)
	{
		throw "UNHANDLE ERROR!";
		bFunctionOK = FALSE;
	}
	
	return bFunctionOK;
}
BOOL CClientSocket::CalculatePDL(DWORD dwSampleCount, const PLONG pRefRawData, const PLONG pTestRawData, PLONG pResultRawData)
{
	BOOL   bFunctionOK = FALSE;
	BOOL isMueller = FALSE;
	PLONG m_Pas, m_Pbs, m_Pcs, m_Pds;
	PLONG m_P1s, m_P2s, m_P3s, m_P4s;
	PLONG m_pResult;
	PLONG m_pMaxILResult;
	PLONG m_pMinILResult;
	double Pa, Pb, Pc, Pd, P1, P2, P3, P4;
	double T1, T2, T3, T4, m11, m12, m13, m14;
	double TempSqrt, Tmax, Tmin; 
	
	// the reference raw data (four polarization: H ,V, Diagonal, Right Circular)
	m_Pas	 = pRefRawData + dwSampleCount;
	m_Pbs	 = pRefRawData + dwSampleCount*2;
	m_Pcs	 = pRefRawData + dwSampleCount*3;
	m_Pds	 = pRefRawData + dwSampleCount*4;
	
	// the testing raw data value (four polarization: H ,V, Diagonal, Right Circular)
	m_P1s	 = pTestRawData + dwSampleCount;
	m_P2s	 = pTestRawData + dwSampleCount*2;
	m_P3s	 = pTestRawData + dwSampleCount*3;
	m_P4s	 = pTestRawData + dwSampleCount*4;
	
	// the PDL result value
	m_pResult = pResultRawData + dwSampleCount;
	m_pMaxILResult = pResultRawData + dwSampleCount * 2;
	m_pMinILResult = pResultRawData + dwSampleCount * 3;
	
	try
	{
		WriteLog("get array!", "scan.log");
		if(pRefRawData == NULL)
		{
			throw "Reference raw data array is NULL";
		}
		if(pTestRawData == NULL)
		{
			throw "Testing raw data array is NULL";
		}
		if(pResultRawData == NULL)
		{
			throw "the result raw data array is NULL";
		}
		if(m_pResult == NULL)
		{
			throw "the result raw data array is NULL";
		}
		if (m_pMaxILResult == NULL)
		{
			throw "the result raw data array is NULL";
		}
		if (m_pMinILResult == NULL)
		{
			throw "the result raw data array is NULL";
		}
		
		WriteLog("calculate the PDL value!", "scan.log");
		// calculate the PDL value
		for (DWORD i = 0; i < dwSampleCount; i++)
		{
			
			if (i==3500)
			{
				int kk=0;
			}
			if (isMueller)
			{
				Pa = pow(10, (m_Pas[i] / -10000.00));
				Pb = pow(10, (m_Pbs[i] / -10000.00));
				Pc = pow(10, (m_Pcs[i] / -10000.00));
				Pd = pow(10, (m_Pds[i] / -10000.00));

				P1 = pow(10, (m_P1s[i] / -10000.00));
				P2 = pow(10, (m_P2s[i] / -10000.00));
				P3 = pow(10, (m_P3s[i] / -10000.00));
				P4 = pow(10, (m_P4s[i] / -10000.00));

				T1 = P1 / Pa;
				T2 = P2 / Pb;
				T3 = P3 / Pc;
				T4 = P4 / Pd;

				m11 = (T1 + T2) / 2.0;
				m12 = (T1 - T2) / 2.0;
				m13 = T3 - m11;
				m14 = T4 - m11;

				TempSqrt = sqrt(m12 * m12 + m13 * m13 + m14 * m14);
				Tmax = m11 + TempSqrt;
				Tmin = m11 - TempSqrt;

				m_pResult[i] = (LONG)(10000 * log10(Tmax / Tmin));
				m_pMaxILResult[i]= (LONG)(10000 * log10(Tmax));
				m_pMinILResult[i]= (LONG)(10000 * log10(Tmin));
			}
			else
			{
				char chLog[256] = { 0 };
				WriteLog("begin circle", "scan.log");
				double dRes[4];
				dRes[0] = m_Pas[i] - m_P1s[i];
				dRes[1] = m_Pbs[i] - m_P2s[i];
				dRes[2] = m_Pcs[i] - m_P3s[i];
				dRes[3] = m_Pds[i] - m_P4s[i];
				double dmax = dRes[0];
				double dMin = dRes[0];
				for (int i = 1;i < 4;i++)
				{
					if (dmax < dRes[i])
						dmax = dRes[i];
					if (dMin > dRes[i])
						dMin = dRes[i];
				}
				m_pResult[i] = dmax - dMin;
				m_pMaxILResult[i] = dmax;
				m_pMinILResult[i] = dMin;
				sprintf(chLog, "i:%d,pdl:%f,max:%f;%f,mim:%f;%f", i, m_pResult[i], dmax, m_pMaxILResult[i], dMin, m_pMinILResult[i]);
				WriteLog(chLog, "scan.log");
			}
		}
		// until to here the function is OK now
		WriteLog("calculate the PDL is OK!", "scan.log");
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		AfxMessageBox(ptszErrorMsg,  MB_OK | MB_ICONERROR);
		bFunctionOK = FALSE;
	}
	catch(...)
	{
		AfxMessageBox("Other fatal exception occurred",  MB_OK | MB_ICONERROR);
		bFunctionOK = FALSE;
	}
	return bFunctionOK;
}
void CClientSocket::GetNOPDLScanDataAndDisplay()
{
	CString strTemp;
	CString strLocalFile;
	CString strNetFile;
	m_CurrentScanOKTime++;
	if (m_CurrentScanOKTime>2)
	{
		m_bCalcResult = TRUE;
		m_strShowMSG.Format("第%d次不带PDL扫描完成!",m_CurrentScanOKTime);
			
		bFinish = TRUE;
		m_CurrentScanOKTime = 0;
		SendStopScan();	

		
		m_strShowMSG.Format("扫描完成!");
		strLocalFile = m_strClentDataFileFullName;
		//strLocalFile.Format("%s\\RawData_CH%d.csv",m_ClientoServerInfo.m_tszClentDatapath,m_ClientoServerInfo.m_nClientTestPort);
		//	strLocalFile.Format("%s\\PDL%d_RawData_CH%d.csv",m_strSaveFolder,i,m_nCHIndex);
		DeleteFile(strLocalFile);
		for (int i = 0;i < m_ClientoServerInfo.m_nPowermeterCount;i++)
		{
			if (m_ClientoServerInfo.m_nPowermeterPorts[i] == 0)
				continue;
			strNetFile.Format("%s\\Test_CH%d.dat", m_ClientoServerInfo.m_tszServerDatapath, m_ClientoServerInfo.m_nPowermeterPorts[i]);
			strLocalFile.Format("%s%d.dat", m_strClentDataFileFullName, i+1);

			if (!CopyFile(strNetFile, strLocalFile, FALSE))
			{
				m_strShowMSG = "提取测试数据失败!";

				m_CurrentScanOKTime = 0;
				m_bReadReferenceWithNoPDL = FALSE;
				SendStopScan();
				return;
			}
		}
		//将二进制文件转为之前的格式
		
		for (int i = 0;i < m_ClientoServerInfo.m_nPowermeterCount;i++)
		{
			if (m_ClientoServerInfo.m_nPowermeterPorts[i] == 0)
				continue;

			strLocalFile.Format("%s%d.dat", m_strClentDataFileFullName, i + 1);
			CString strLocalCsv = "";
			strLocalFile.Format("%s%d.csv", m_strClentDataFileFullName, i + 1);
			if (!ConvertBinToCsv(strLocalFile.GetBuffer(0), strLocalCsv.GetBuffer(0)))
				return;
		}
		

		//		strTemp.Format("提取测试数据完成!",TRUE,COLOR_BLUE);
		///		LogInfo(strTemp);
		/*if (!m_bReadReferenceWithNoPDL)
		{
				m_strReferenceNOPDLFile.Format("%s\\ReferenceWithNOPDL%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,m_ClientoServerInfo.m_nClientTestPort);
	
			if (!ReadReferenceData())
			{
				m_strShowMSG="读取归零数据失败!";
				
				m_CurrentScanOKTime = 0;
				m_bReadReferenceWithNoPDL = FALSE;
				SendStopScan();
				return;
			}
		}
		
		if (!ReadNoPDLRawDataFile())
		{
			m_strShowMSG="读取归零数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithNoPDL = FALSE;
			SendStopScan();
			return;
		}
		//		strTemp.Format("读取扫描数据完成!",TRUE,COLOR_BLUE);
		//		LogInfo(strTemp);
		ZeroMemory(&m_stResultRawData, sizeof(stAutoRawData));
		FreeResultRawData(&m_stResultRawData);	
		if(!AllocateResultRawArray(&m_stResultRawData,
			CLIENT_CH_COUNT,
			m_dwScanDataCount,
			TRUE))
		{
			m_strShowMSG="计算扫描数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithNoPDL = FALSE;
			SendStopScan();
			return;
		}
		//		strTemp.Format("计算扫描数据完成!",TRUE,COLOR_BLUE);
		//		LogInfo(strTemp);
		if(!CalculateTestResult(&m_stNoPDLRefRawData,&m_stTestRawData,&m_stResultRawData,m_dwScanDataCount))
		{
			m_strShowMSG="计算扫描结果数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithNoPDL = FALSE;
			SendStopScan();
			return;
		}
		//		strTemp.Format("计算扫描结果数据完成!",TRUE,COLOR_BLUE);
		//		LogInfo(strTemp);
		if (!SaveScanResult()) 
		{
			m_strShowMSG="保存计算扫描结果数据失败!";
			
			m_CurrentScanOKTime = 0;
			m_bReadReferenceWithNoPDL = FALSE;
			SendStopScan();
			return;
		}*/
		//		strTemp.Format("保存计算扫描结果数据完成!",TRUE,COLOR_BLUE);
		//		LogInfo(strTemp);
		//	fnDisplayCurveToGraph() ;
		m_CurrentScanOKTime = 0;
		m_bCalcResult = FALSE;
	}
	else
	{
		m_strShowMSG.Format("第%d次不带PDL扫描完成!",m_CurrentScanOKTime);
		
	}
}


void CClientSocket::FreeTestPowerRawData(PAutoRawData pTestPowerRawData)
{
	int nChannelIndex;
	// release the wavelength array
	if(pTestPowerRawData->m_pdwWavelengthArray != NULL)
	{
		// release the wavelength array
		VirtualFree(pTestPowerRawData->m_pdwWavelengthArray,0,MEM_RELEASE);
		
		// set the pointer is null
		pTestPowerRawData->m_pdwWavelengthArray = NULL;
	}
	
	// release the loss array
	for(nChannelIndex = 0; nChannelIndex < MAX_CHANNEL_COUNT; nChannelIndex++)
	{
		if(pTestPowerRawData->m_pnLossArray[nChannelIndex] != NULL)
		{
			// release the wavelength array
			VirtualFree(pTestPowerRawData->m_pnLossArray[nChannelIndex],0,MEM_RELEASE);
			
			// set the pointer is null
			pTestPowerRawData->m_pnLossArray[nChannelIndex] = NULL;
		}
	}
	// Zeromemory the pointer 
	// we must add the function here
	ZeroMemory(pTestPowerRawData,sizeof(stAutoRawData));
}
BOOL CClientSocket::AllocateTestRawArray(PAutoRawData pTestPowerRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL)
{
	BOOL           bFunctionOK = FALSE;
	DWORD          dwChannelIndex;
	DWORD          dwLossArrayMultiple;
	
	try
	{
		// check the input parameter
		if(dwChannelCount == 0)
		{
			throw "Error: the scan channel count is 0 ";
		}
		// if not allocata the testing power raw data pointer
		if(pTestPowerRawData->m_pdwWavelengthArray == NULL)
		{
			
			// allocate the wavelength array here
			pTestPowerRawData->m_pdwWavelengthArray = 
				(PDWORD)VirtualAlloc(NULL,
				dwSampleCount * sizeof(DWORD),
				MEM_RESERVE | MEM_COMMIT,
				PAGE_READWRITE);
			
			if(pTestPowerRawData->m_pdwWavelengthArray == NULL)
			{
				throw "Not enough memory for test wavelength array";
			}
		}
		
		// allocate the reference power raw data array
		for(dwChannelIndex = 0; dwChannelIndex < dwChannelCount;dwChannelIndex++)
		{
			if(bDoPDL)  // do PDL testing 
			{
				dwLossArrayMultiple = 5;
				// 0: the average Power array
				// 1: the horizontal power array
				// 2: the vertical power array
				// 3: the diagonal power array
				// 4: the right hand circuital power array
			}
			else
			{
				dwLossArrayMultiple = 1;
				// 0: the average Power array
			}
			// if not allocata the testing power raw data pointer
			if(pTestPowerRawData->m_pnLossArray[dwChannelIndex] == NULL)
			{
				pTestPowerRawData->m_pnLossArray[dwChannelIndex] = 
					(LONG*)VirtualAlloc(NULL,
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG),
					MEM_RESERVE | MEM_COMMIT,
					PAGE_READWRITE);
				
				if(pTestPowerRawData->m_pnLossArray[dwChannelIndex] == NULL)
				{
					throw "Not enough memory for test Power raw data array";
				}
				
				// Zeromemory the test power raw data
				ZeroMemory(pTestPowerRawData->m_pnLossArray[dwChannelIndex],
					dwLossArrayMultiple*dwSampleCount * sizeof(LONG));
			}
			
		}
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		bFunctionOK = FALSE;
		throw ptszErrorMsg;
	}
	catch(...)
	{
		bFunctionOK = FALSE;
		throw "UNHAND ERROR!";
	}
	return bFunctionOK;
}
void CClientSocket::FreeRefPowerRawData(PAutoRawData pRefRawData)
{
	int nChannelIndex;
	// release the wavelength array
	if(pRefRawData->m_pdwWavelengthArray != NULL)
	{
		// release the wavelength array
		VirtualFree(pRefRawData->m_pdwWavelengthArray,0,MEM_RELEASE);
		
		// set the pointer is null
		pRefRawData->m_pdwWavelengthArray = NULL;
	}
	
	// release the loss array
	for(nChannelIndex = 0; nChannelIndex < MAX_CHANNEL_COUNT; nChannelIndex++)
	{
		if(pRefRawData->m_pnLossArray[nChannelIndex] != NULL)
		{
			// release the wavelength array
			VirtualFree(pRefRawData->m_pnLossArray[nChannelIndex],0,MEM_RELEASE);
			
			// set the pointer is null
			pRefRawData->m_pnLossArray[nChannelIndex] = NULL;
		}
		
	}
	// Zeromemory the pointer 
	// we must add the function here
	ZeroMemory(pRefRawData,sizeof(stAutoRawData));
}
BOOL CClientSocket::CalcuateAvePower(PAutoRawData pAutoScanRawPower, DWORD dwChannelIndex, DWORD dwSampleCount)
{
	BOOL         bFunctionOK = FALSE;
	double        dwTempValue  = 0;
	DWORD        dwSampleIndex,dwSweepIndex;
	DWORD        dwSweepCount =4;
	try 
	{
		if(pAutoScanRawPower == NULL)
		{
			throw "The auto scan raw power pointer is NULL";
		}
		// calculate the average power
		//for(dwChannelIndex = 0;dwChannelIndex < dwChannelCount;dwChannelIndex ++)
		//{
		// set pdwDataArray to the average Power array
		PLONG plDataArray = (PLONG)pAutoScanRawPower->m_pnLossArray[0];
		if(plDataArray == NULL)
		{
			throw "The scan raw data power pointer is NULL";
		}
		
		for(dwSampleIndex = 0; dwSampleIndex < dwSampleCount; dwSampleIndex ++)
		{
			// do PDL
			dwTempValue = 0;
			// calculate the average power
			for(dwSweepIndex = 0; dwSweepIndex < dwSweepCount; dwSweepIndex++)
			{
				dwTempValue  +=
					pAutoScanRawPower->m_pnLossArray[0][(dwSweepIndex+1)*dwSampleCount +dwSampleIndex];
			}
			// set the average power
			plDataArray[dwSampleIndex] = (long)dwTempValue/4;
		} // end sample index
		
		//}// end for(dwChannelIndex = 0;dwChannelIndex < p8164Param->m_dwChannelNumber;dwChannelIndex ++)
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		AfxMessageBox(ptszErrorMsg);
		bFunctionOK = FALSE;
	}
	catch(...)
	{
		AfxMessageBox("UNHANDLE ERROR!");
		bFunctionOK = FALSE;
	}
	return bFunctionOK;
}
BOOL CClientSocket::ReadReferenceData(BOOL bWithPDL)
{
	BOOL                bFunctionOK = FALSE;
	DWORD	            dwSweepCount,dwMulitplyValue;	
	PLONG				PLongTempData = NULL;
	PLONG				PLongTempData1 = NULL;
	PLONG				PLongTempData2 = NULL;
	PLONG				PLongTempData3 = NULL;
	PLONG				PLongTempData4 = NULL;
	CString				strReferenceFile;
	char				pszThisLine[MAX_LINE];
    FILE*				pfCSVFile = NULL;
	char				ch1[256],ch2[256],ch3[256],ch4[256],ch5[256],ch6[256];
	try
	{
		PAutoRawData        pAutoScanRawData = NULL;
		if (bWithPDL)
		{
			dwSweepCount    = 4;
			dwMulitplyValue = 5;
			strReferenceFile = m_strReferencePDLFile;
			pAutoScanRawData = &m_stRefRawData;
		}
		else // no PDL, the scan count is 0
		{
			dwSweepCount    = 1;
			dwMulitplyValue = 1; 
			strReferenceFile = m_strReferenceNOPDLFile;
			pAutoScanRawData = &m_stNoPDLRefRawData;
		}
		// Get the sample count here
		
		// free the reference raw data  
		FreeRefPowerRawData(pAutoScanRawData);
		
		// allocate the reference raw data pointer
		if(!AllocateRefRawArray(pAutoScanRawData,CLIENT_CH_COUNT,m_dwScanDataCount,bWithPDL))
		{
			throw "Allocate Reference raw data array pointer error";
		}
		// set the wavelength array address is the allcated memory at the 
		// m_stRefRawData.m_pdwWavelengthArray
		PDWORD pdwWavelengthArray = pAutoScanRawData->m_pdwWavelengthArray;
		//	Adjust Power array from double to DWORD, 
		//	i.e., power value are exaggerated by 1000 time
		PLongTempData = pAutoScanRawData->m_pnLossArray[0];
		if (bWithPDL)
		{
			PLongTempData1 = pAutoScanRawData->m_pnLossArray[0]+m_dwScanDataCount;
			PLongTempData2 = pAutoScanRawData->m_pnLossArray[0]+2*m_dwScanDataCount;
			PLongTempData3 = pAutoScanRawData->m_pnLossArray[0]+3*m_dwScanDataCount;
			PLongTempData4 = pAutoScanRawData->m_pnLossArray[0]+4*m_dwScanDataCount;
		}
		
		
		pfCSVFile = fopen(strReferenceFile, "rt");
		if(NULL == pfCSVFile)
		{
			throw "Open reference file fail !";
		}
		int nTemp=0;
		while(!feof(pfCSVFile))
		{
			ZeroMemory(pszThisLine, sizeof(char) * (MAX_LINE)); 
			
			//  If we hit end-of-file, or error, end this loop
			if(NULL == fgets((LPSTR)pszThisLine, (MAX_LINE), pfCSVFile))
				break; 
			
			nTemp++;
			if (nTemp >1)
			{
				if (bWithPDL) 
				{
					sscanf((LPSTR)pszThisLine, "%[^','],%[^','],%[^','],%[^','],%[^','],%[^',']",ch1,ch2,ch3,ch4,ch5,ch6);
					pdwWavelengthArray[nTemp-2] = (DWORD)(atof(ch1)*MULTY_DATA);
					PLongTempData[nTemp-2] = (LONG)(atof(ch2)*MULTY_DATA);
					PLongTempData1[nTemp-2] = (LONG)(atof(ch3)*MULTY_DATA);
					PLongTempData2[nTemp-2] = (LONG)(atof(ch4)*MULTY_DATA);
					PLongTempData3[nTemp-2] = (LONG)(atof(ch5)*MULTY_DATA);
					PLongTempData4[nTemp-2] = (LONG)(atof(ch6)*MULTY_DATA);
				}
				else
				{
					sscanf((LPSTR)pszThisLine, "%[^','],%[^',']",ch1,ch2);
					pdwWavelengthArray[nTemp-2] = (DWORD)(atof(ch1)*MULTY_DATA);
					PLongTempData[nTemp-2] = (LONG)(atof(ch2)*MULTY_DATA);
				}
			}	
			//			YieldToPeers();
		} 
		fclose(pfCSVFile); 
		// until to here, the function is OK now
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		throw ptszErrorMsg;
		bFunctionOK = FALSE;
	}
#ifndef _DEBUG
	catch(...)
	{
		throw "Other execption occured";
		bFunctionOK = FALSE;
	}
#endif
	if (bWithPDL)
	{
		m_bReadReferenceWithPDL = TRUE;
	}
	else
	{
		m_bReadReferenceWithNoPDL = TRUE;
	}
	
	return bFunctionOK;
}
void CClientSocket::FreeResultRawData(PAutoRawData pResultRawData)
{
	int nChannelIndex;
	// release the wavelength array
	if(pResultRawData->m_pdwWavelengthArray != NULL)
	{
		// release the wavelength array
		VirtualFree(pResultRawData->m_pdwWavelengthArray,0,MEM_RELEASE);
		
		// set the pointer is null
		pResultRawData->m_pdwWavelengthArray = NULL;
	}
	
	// release the loss array
	for(nChannelIndex = 0; nChannelIndex < MAX_CHANNEL_COUNT; nChannelIndex++)
	{
		if(pResultRawData->m_pnLossArray[nChannelIndex] != NULL)
		{
			// release the wavelength array
			VirtualFree(pResultRawData->m_pnLossArray[nChannelIndex],0,MEM_RELEASE);
			
			// set the pointer is null
			pResultRawData->m_pnLossArray[nChannelIndex] = NULL;
		}
	}
	
	// Zeromemory the pointer 
	// we must add the function here
	ZeroMemory(pResultRawData,sizeof(stAutoRawData));
}
BOOL CClientSocket::ReadPDLRawDataFile(BOOL bUsualRawData)
{
	BOOL                bFunctionOK = FALSE;	
	PLONG				PLongTempData = NULL;
	CString				strReferenceFile;
	char				pszThisLine[MAX_LINE];
    FILE*				pfCSVFile = NULL;
	char				ch1[256],ch2[256]; 
	PAutoRawData        pAutoScanRawData = NULL;
	try
	{	
		if (bUsualRawData) 
		{
			FreeTestPowerRawData(&m_stTestRawData);
			if(!AllocateTestRawArray(&m_stTestRawData,CLIENT_CH_COUNT,m_dwScanDataCount,TRUE))
			{
				throw "Allocate Reference raw data array pointer error";
			}
			
			//ZeroMemory(&m_stTestRawData, sizeof(stAutoRawData));
			// set the wavelength array address is the allcated memory at the 
			pAutoScanRawData =(PAutoRawData)&m_stTestRawData;
		}
		else
		{
			// free the reference raw data  
			FreeRefPowerRawData(&m_stRefRawData);
			// allocate the reference raw data pointer
			if(!AllocateRefRawArray(&m_stRefRawData,CLIENT_CH_COUNT,m_dwScanDataCount,TRUE))
			{
				AfxMessageBox("Allocate Reference raw data array pointer error");
				return FALSE;
			}
			// m_stRefRawData.m_pdwWavelengthArray
			pAutoScanRawData =(PAutoRawData)&m_stRefRawData;
			
		}
		for (int i=1;i<5;i++)
		{
			PDWORD pdwWavelengthArray = pAutoScanRawData->m_pdwWavelengthArray;
			//	Adjust Power array from double to DWORD, 
			//	i.e., power value are exaggerated by 1000 time
			PLongTempData = (PLONG)(pAutoScanRawData->m_pnLossArray[0]+ i*m_dwScanDataCount);
			if (bUsualRawData)
				strReferenceFile.Format("%s\\PDL%d_RawData_CH%d.csv",m_ClientoServerInfo.m_tszClentDatapath,i,m_ClientoServerInfo.m_nClientTestPort);
			else
				strReferenceFile.Format("%s\\PDL%d_RawData_CH%d.csv",m_ClientoServerInfo.m_tszClentRefDatapath,i,m_ClientoServerInfo.m_nClientTestPort);	
			
			pfCSVFile = fopen(strReferenceFile, "rt");
			if(NULL == pfCSVFile)
			{
				m_strShowMSG="Open PDL_file fail !";		
				return FALSE;
			}
			int nTemp=0;
			while(!feof(pfCSVFile))
			{
				ZeroMemory(pszThisLine, sizeof(char) * (MAX_LINE)); 
				
				//  If we hit end-of-file, or error, end this loop
				if(NULL == fgets((LPSTR)pszThisLine, (MAX_LINE), pfCSVFile))
					break; 
				
				nTemp++;
				if (nTemp >1)
				{
					sscanf((LPSTR)pszThisLine, "%[^','],%[^',']",ch1,ch2);	
					if (i==1)
					{
						pdwWavelengthArray[nTemp-2] = (DWORD)(atof(ch1)*MULTY_DATA);
					}
					PLongTempData[nTemp-2] = (LONG)(atof(ch2)*MULTY_DATA);
				}	
				//				YieldToPeers();
			} 
			fclose(pfCSVFile); 
			
			/*if (bUsualRawData) 
				DeleteFile(strReferenceFile);*/
		}
		if (!CalcuateAvePower(pAutoScanRawData,m_ClientoServerInfo.m_nClientTestPort,m_dwScanDataCount))
		{
			m_strShowMSG="Calc AVE PDL Power fail !";		
			return FALSE;
		}
		// until to here, the function is OK now
		bFunctionOK = TRUE;
	}
	catch(TCHAR* ptszErrorMsg)
	{
		throw ptszErrorMsg;
		bFunctionOK = FALSE;
	}
#ifndef _DEBUG
	catch(...)
	{
		throw "Other execption occured";
		bFunctionOK = FALSE;
	}
#endif
	return bFunctionOK;
}