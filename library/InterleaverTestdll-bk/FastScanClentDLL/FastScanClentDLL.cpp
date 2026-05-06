// FastScanClentDLL.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"

#include "FastScanClentDLL.h"
#include "FastScanDLL.h"
#include "ClientSocket.h"
#include "FSTPClient.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

//
//	Note!
//
//		If this DLL is dynamically linked against the MFC
//		DLLs, any functions exported from this DLL which
//		call into MFC must have the AFX_MANAGE_STATE macro
//		added at the very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the 
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

/////////////////////////////////////////////////////////////////////////////
// CFastScanClentDLLApp

BEGIN_MESSAGE_MAP(CFastScanClentDLLApp, CWinApp)
	//{{AFX_MSG_MAP(CFastScanClentDLLApp)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CFastScanClentDLLApp construction

CFastScanClentDLLApp::CFastScanClentDLLApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CFastScanClentDLLApp object

CFastScanClentDLLApp theApp;
CFastScanClentDLL::CFastScanClentDLL()
{ 
	m_pClient = new CClientSocket();
	m_pFSTPClient = new CFSTPClient();
	ZeroMemory(&m_pClient->m_ClientoServerInfo,sizeof(stClentTestingConfig));
	ZeroMemory(&m_pFSTPClient->m_ClientoServerInfo, sizeof(stClentTestingConfig));
	m_bFSTP = FALSE;
	return; 
}

CFastScanClentDLL::~CFastScanClentDLL()
{ 
	delete m_pClient;
	delete m_pFSTPClient;
}

void CFastScanClentDLL::CloseSocket()
{
	m_pClient->CloseSocket();
}

BOOL CFastScanClentDLL::TLSScan(BOOL bDoPDL,BOOL bDoRef,int nPort,CString strfilefullname)
{
	//AfxMessageBox("test1");
	/*if (bDoRef&&bDoPDL)
	{
		m_Client.m_strReferencePDLFile = strfilefullname;
	}
	else if(bDoRef&&(!bDoPDL))
	{
		m_Client.m_strReferenceNOPDLFile = strfilefullname;
	}
	else*/
	if (m_bFSTP)
	{
		return m_pFSTPClient->TLSScan(bDoPDL,strfilefullname);
	}
	else
	{
		m_pClient->m_strClentDataFileFullName = strfilefullname;
		if (!m_pClient->TLSScan(bDoPDL, bDoRef, nPort))
		{
			return FALSE;
		}
	}
	return TRUE;
}

CString CFastScanClentDLL::GetMsg()
{
	if (m_bFSTP)
	{
		return m_pFSTPClient->m_strLog;
	}
	else
	{
		return m_pClient->m_strShowMSG;
	}
}

BOOL CFastScanClentDLL::ConnectServer(stClentTestingConfig  m_testinfo)
{
	memcpy(&m_pClient->m_ClientoServerInfo,&m_testinfo,sizeof(stClentTestingConfig));
	memcpy(&m_pFSTPClient->m_ClientoServerInfo, &m_testinfo, sizeof(stClentTestingConfig));
	if (m_testinfo.m_nPort == 0)
	{
		m_bFSTP = TRUE;
	}
	if (m_bFSTP)
	{
		if (!m_pFSTPClient->InitialUDLEngine())
		{
			return FALSE;
		}
	}
	else
	{
		if (!m_pClient->ConnectServer())
		{

			return FALSE;
		}
	}
	return TRUE;
	/*
	m_Client.m_ClientoServerInfo.m_tszServerIP=m_ClientoServerInfo.m_tszServerIP;
	m_Client.m_tszServerIP=m_ClientoServerInfo.m_tszServerIP;
	m_Client.m_nPort=m_ClientoServerInfo.m_nPort;
	m_Client.m_tszClientIP=m_ClientoServerInfo.m_tszClientIP;
	m_Client.m_tszClientName=m_ClientoServerInfo.m_tszClientName;
	m_Client.m_nClientPortIndex=m_ClientoServerInfo.m_nClientPortIndex;
	   m_Client.m_tszServerDatapath=m_ClientoServerInfo.m_tszServerDatapath;
	   m_Client.m_tszClentDatapath=m_ClientoServerInfo.m_tszClentDatapath;
	   
	   m_Client.m_nClientTestPort=m_ClientoServerInfo.m_nClientTestPort;
	   */

}