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

bool __stdcall ConnectServer(stClentTestingConfig  m_testinfo)
{
	int scanIndex= GetScanIndex(m_testinfo.m_nClientTestPort);
	CFastScanClentDLL *pScanDll=GetByIndex(m_testinfo.m_nClientTestPort);
	static bool bRes=false;
	if(scanIndex !=-1)
	{		
		pScanDll->CloseSocket();
		vector<int>::iterator iterPort = m_PortList.begin()+ scanIndex;
		m_PortList.erase(iterPort);

		m_scandllList.erase(m_scandllList.begin() + scanIndex);

		delete pScanDll;
	}

	CFastScanClentDLL *m_scandll=new CFastScanClentDLL();
	bRes= m_scandll->ConnectServer(m_testinfo);
		
	m_scandllList.push_back(m_scandll);
	m_PortList.push_back(m_testinfo.m_nClientTestPort);
	
	return bRes;

}

bool __stdcall TLSScan(bool bDoPDL,bool bDoRef,int nPort,char* strfilefullname)
{
	//AfxMessageBox("wrapper1");
	CFastScanClentDLL *pScanDll=GetByIndex(nPort);
	//AfxMessageBox("wrapper2");
	static bool bRes=false;
	if(pScanDll!=NULL)
	{
		//AfxMessageBox("wrapper3");
		CString path="";
		if(strfilefullname!=NULL)
			path.Format("%s", strfilefullname);
		//AfxMessageBox("wrapper4");
		bRes= pScanDll->TLSScan(bDoPDL,bDoRef,nPort, path);
		//AfxMessageBox("wrapper5");
	}
	return bRes;
}

char* __stdcall GetMsg(int nPort)
{
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
	return "¶Ë¿ÚÎ´Á¬½Ó";
}

void __stdcall Release()
{
	for (int i = 0;i < m_scandllList.size();i++)
	{
		delete m_scandllList[i];
	}
}