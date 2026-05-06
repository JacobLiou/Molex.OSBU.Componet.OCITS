#include "StdAfx.h"
#include "FastScanClientWrapper.h"
#include<vector>
using namespace std;

static vector<CFastScanClentDLL*> m_scandllList;
static vector<int> m_PortList;

CFastScanClentDLL *GetByIndex(int nPWM)
{
	for(int i=0;i<m_scandllList.size();i++)
	{
		if(nPWM==m_PortList[i])
		{
			return m_scandllList[i];
		}
	}
	return NULL;
}

int GetScanIndex(int nPWM)
{
	for (int i = 0;i<m_scandllList.size();i++)
	{
		if (nPWM == m_PortList[i])
		{
			return i;
		}
	}
	return -1;
}

int __stdcall ConnectServer(stClentTestingConfig  m_testinfo)
{
	m_testinfo.m_nPort = 0;
	int scanIndex= GetScanIndex(m_testinfo.m_nPort);
	CFastScanClentDLL *pScanDll=GetByIndex(m_testinfo.m_nPort);
	int bRes=0;
	//UDL只初始化一次
	/*if(scanIndex !=-1)
	{		
		pScanDll->CloseSocket();
		vector<int>::iterator iterPort = m_PortList.begin()+ scanIndex;
		m_PortList.erase(iterPort);

		m_scandllList.erase(m_scandllList.begin() + scanIndex);

		delete pScanDll;
	}*/
	CFastScanClentDLL *m_scandll = NULL;
	if (scanIndex == -1)
		m_scandll = new CFastScanClentDLL();
	else
	{
		m_scandll = pScanDll;
	}
	bRes= m_scandll->ConnectServer(m_testinfo);
	if (scanIndex == -1)
	{
		m_scandllList.push_back(m_scandll);
		m_PortList.push_back(m_testinfo.m_nPort);
	}
	
	return bRes;

}

int __stdcall TLSScanFSTP(bool bDoPDL, bool bDoRef, double dWLStart, double dWLStop, double dStep, char* strfilefullname)
{
	//AfxMessageBox("wrapper1");
	CFastScanClentDLL *pScanDll = GetByIndex(0);
	//AfxMessageBox("wrapper2");
	static bool bRes = false;
	if (pScanDll != NULL)
	{
		//AfxMessageBox("wrapper3");
		CString path = "";
		if (strfilefullname != NULL)
			path.Format("%s", strfilefullname);
		//AfxMessageBox("wrapper4");
		bRes = pScanDll->TLSScan(bDoPDL, bDoRef, dWLStart, dWLStop, dStep, path);
		//AfxMessageBox("wrapper5");
	}
	return bRes;
}

bool __stdcall TLSScan(bool bDoPDL,bool bDoRef,int nPort,char* strfilefullname)
{
	return FALSE;
}

char* __stdcall GetMsg(int nPort)
{
	nPort = 0;
	CFastScanClentDLL *pScanDll=GetByIndex(nPort);
	if(pScanDll!=NULL)
	{	
		CString errMsg= pScanDll->GetMsg();
		char chErr[1024] = { 0 };
		sprintf(chErr, "%s\0", errMsg.GetBuffer(0));
		//strcpy(chErr, errMsg.GetBuffer(0));
		errMsg.ReleaseBuffer();
		return chErr;
	}
	return "端口未连接";
}

void __stdcall Release()
{
	for (int i = 0;i < m_scandllList.size();i++)
	{
		delete m_scandllList[i];
	}
}