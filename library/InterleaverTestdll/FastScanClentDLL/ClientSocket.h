#if !defined(AFX_CLIENTSOCKET_H__F245045B_8706_4D3B_BEAF_2A7B8588C953__INCLUDED_)
#define AFX_CLIENTSOCKET_H__F245045B_8706_4D3B_BEAF_2A7B8588C953__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

//#include <afxsock.h>		// MFC socket extensions


#include <WinSock2.h>
#include "SendMsg.h"
#include "StdAfx.h"
#include <math.h>
/////////////////////////////////////////////////////////////////////////////
// CClientSocket command target
//class CCTestServerDoc;
class CSendMsg;


class CClientSocket
{
	// Attributes
public:
	//DECLARE_DYNAMIC(CClientSocket)
// Operations
public:
	CClientSocket();
	virtual ~CClientSocket();

	// Overrides
public:
	CSendMsg* ReceiveMsg();
	void SendMsg(CSendMsg* pMsg);
	void Init();
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CClientSocket)
public:

	//}}AFX_VIRTUAL

	// Generated message map functions
	//{{AFX_MSG(CClientSocket)
		// NOTE - the ClassWizard will add and remove member functions here.
	//}}AFX_MSG

// Implementation
public:
	void CloseSocket();
	//	CCTestServerDoc* m_pDoc;

		//CSocketFile*     m_pFile;
		//CArchive*        m_pArchiveIn;
		//CArchive*        m_pArchiveOut;
	char m_SendBufFile[256];
	char m_RecBufFile[256];
	SOCKET m_Socket;
	int              m_nMsgCount;
	stClentTestingConfig  m_ClientoServerInfo;
	stAutoRawData		m_stRefRawData;
	stAutoRawData		m_stNoPDLRefRawData;
	stAutoRawData		m_stTestRawData; // export
	stAutoRawData		m_stResultRawData;// export
	double *g_pdbWLptr;
	double *g_pdbGetPowerptr[WSSMODULECOUNT];
	double *g_pdbGetPowerptrPDL[WSSMODULECOUNT];

	CString m_strReferenceNOPDLFile, m_strReferencePDLFile;
	CString   m_strClentDataFileFullName;
	int m_CurrentScanOKTime;
	BOOL bFinish;
	BOOL  m_bUsualScan;
	DWORD    m_dwScanDataCount;
	BOOL m_bRefScan;
	BOOL m_bConnectNet;
	int m_nPDLStatus;
	BOOL m_bopendriver;
	BOOL m_bRealScanPDL;
	BYTE m_bCurrentMsgType;
	BOOL m_bCalcResult;
	BOOL m_bReadReferenceWithNoPDL;
	BOOL m_bReadReferenceWithPDL;
	CString m_strShowMSG;
	void ParesReciveMsg(CSendMsg *pMsg);
	BOOL ProcessReceive();
	CSendMsg* ReadMsg();
	void SendStopScan();
	BOOL TLSScan(BOOL bDoPDL, BOOL bDoRef, int nPort);
	BOOL ConnectServer();


	BOOL  SaveNOPDLReference();
	BOOL SavePDLReference();
	BOOL SaveScanResult();
	BOOL ReadNoPDLRawDataFile(BOOL bUsualRawData = TRUE);
	BOOL ReadPDLRawDataFile(BOOL bUsualRawData = TRUE);
	void FreeResultRawData(PAutoRawData pResultRawData);
	BOOL ReadReferenceData(BOOL bWithPDL = FALSE);
	BOOL CalcuateAvePower(PAutoRawData pAutoScanRawPower, DWORD dwChannelIndex, DWORD dwSampleCount);
	BOOL SaveReferenceFile(BOOL bScanPDL = FALSE);
	void FreeTestPowerRawData(PAutoRawData pTestPowerRawData);
	void FreeRefPowerRawData(PAutoRawData pRefRawData);
	BOOL AllocateResultRawArray(PAutoRawData pResultRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL = FALSE);
	BOOL AllocateRefRawArray(PAutoRawData pRefPowerRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL = FALSE);
	BOOL AllocateTestRawArray(PAutoRawData pTestPowerRawData, DWORD dwChannelCount, DWORD dwSampleCount, BOOL bDoPDL = FALSE);
	//VOID ReleaseAllocStruct(POp816XRawData pData);
	//BOOL CalTestResult(POp816XRawData pRefData,POp816XRawData pScanData,POp816XRawData pResult,PScanParam pParam,BOOL bDoPDL);
	//void AllocPDLScanStruct(POp816XRawData pData);
	void GetNOPDLScanDataAndDisplay();
	void GetPDLScanDataAndDisplay(int nPDLStatus);
	void GetPDLRefDataAndDisplay(int nPDLStatus);
	BOOL ConvertBinToCsv(char *chBinFile, char *chCsvFile);
	BOOL CalculateTestResult(PAutoRawData pRefRawDataArray, PAutoRawData pTestRawDataArray, PAutoRawData pResultArray, DWORD dwSampleCount, BOOL bWithPDL = FALSE);
	BOOL CalculatePDL(DWORD dwSampleCount, const PLONG pRefRawData, const PLONG pTestRawData, PLONG pResultRawData);
	void WriteLog(char * chLog, char * pFilename);
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_CLIENTSOCKET_H__F245045B_8706_4D3B_BEAF_2A7B8588C953__INCLUDED_)
