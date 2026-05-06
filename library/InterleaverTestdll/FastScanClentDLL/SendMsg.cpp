// SendMsg.cpp: implementation of the CSendMsg class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
//#include "ctestserver.h"
#include "SendMsg.h"

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[]=__FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////
IMPLEMENT_DYNCREATE(CSendMsg , CObject)
CSendMsg::CSendMsg()
{
	m_bClose = FALSE;
	m_byMsgType = 0;
	m_dbTLSAtten = 0.0;
	m_dbTLSPMWL = 0.0;
	m_dbTLSPW = 0.0;
	m_dbTLSWL = 0.0;
	m_dwScanCount = 0;
	m_nTLSChannelIndex = 0;
	m_pdwScanData = NULL;
	m_pdwScanWL = NULL;
	m_pdwScanPDL = NULL;
//	memset(&m_RefInfo,0,sizeof(tagAutoRefParam)*1);
	m_strErrorMsg = _T("");
	m_strIpAddress = "";
	m_strUserName = "";
	m_nUserPort = 0;
	m_bRefFileType = FALSE;
	m_nRefFileIndex = -1;
	m_b8164Open = FALSE;
	m_b8169Open = FALSE;
	m_bPDL = FALSE;
	m_nHighOrLow = 1;
    m_bautoOSA=TRUE;
	m_dbOSAStartWL = 0;//OSA扫描开始波长
	m_dbOSAStopWL = 0;//OSA扫描结束波长
	m_dbOSAres = 0;//OSA分辨率
	m_dbOSAStep = 0;//OSA 测试步长
	m_bOSAOpen = FALSE;

	
}

CSendMsg::~CSendMsg()
{
	if(m_pdwScanData != NULL)
	{
		delete m_pdwScanData;
		m_pdwScanData = NULL;
	}
	if(m_pdwScanPDL != NULL)
	{
		delete m_pdwScanPDL;
		m_pdwScanPDL = NULL;
	}
	if(m_pdwScanWL != NULL)
	{
		delete m_pdwScanWL;
		m_pdwScanWL = NULL;
	}
	m_dwScanCount = 0;
}

void CSendMsg::Serialize(CArchive &ar)
{
	DWORD dwI=0;
	if(ar.IsStoring())//存入
	{
		ar<<m_byMsgType;
		switch(m_byMsgType)
		{
		case MSG_TYPE_ASE:
            ar<<m_bASE;
			 break;
		case MSG_TYPE_NEWCLIENT:
			ar<<m_strUserName<<m_strIpAddress<<m_nUserPort;
			ar<<m_nClientCHIndex;
			break;
		case MSG_TYPE_EMPTY:
			break;
		case MSG_TYPE_CLOSE:
			break;
		case MSG_TYPE_CLIENT_REFFILE:
			break;
		case MSG_TYPE_SERVER_REFFILE:
			ar<<m_bRefFileType;
			ar<<m_nRefFileIndex;
			ar<<m_RefInfo.strFileTitle;
			ar<<m_RefInfo.strFilePathName;
			ar<<m_RefInfo.m_bDoPDL;
			ar<<m_RefInfo.m_bReference;
			ar<<m_RefInfo.m_dblAlphaValue;
			ar<<m_RefInfo.m_dblPWMPower;
			ar<<m_RefInfo.m_dblStartWL;
			ar<<m_RefInfo.m_dblStepSize;
			ar<<m_RefInfo.m_dblStopWL;
			ar<<m_RefInfo.m_dblTLSPower;
			ar<<m_RefInfo.m_dwChannelCfgHigh;
			ar<<m_RefInfo.m_dwChannelCfgLow;
			ar<<m_RefInfo.m_dwChannelNumber;
			ar<<m_RefInfo.m_dwNumberOfScan;
			ar<<m_RefInfo.m_dwSampleCount;
			ar<<m_RefInfo.m_dwSize;
			break;
		case MSG_TYPE_ERROR:
			ar<<m_strErrorMsg;
			break;
		case MSG_TYPE_FINSH_REFFILE:
			break;
		case MSG_TYPE_SERVER_CLOSE:
			break;
		case MSG_TYPE_SERVER_NOREFFILE:
			break;
		case MSG_TYPE_TLS_OPEN:
			ar<<m_bHas8164<<m_dw8164Address;
			ar<<m_bHas8169<<m_dw8169Address;
			break;
		case MSG_TYPE_TLS_HAS:
//			ar<<m_b8164Open<<m_b8169Open<<m_bOSAOpen;//delete by xiaomingg2010-10-
			ar<<m_b8164Open<<m_b8169Open;
			break;
		case MSG_TYPE_SCAN_REF:
			ar<<m_nClientCHIndex;
			ar<<m_nPDLStatus;
			ar<<m_byPDLScan;
			break;
		case MSG_TYPE_SCAN_REFDATA:
			ar<<m_dwScanCount;

			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
			}
			break;
	
		case MSG_TYPE_SCAN_DATA:
//			ar<<m_dwScanCount<<m_bPDL<<m_nTLSChannelIndex;
//			if(m_bPDL)
//			{
//				for(dwI=0;dwI < m_dwScanCount;dwI++)
//				{
//					ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI]<<m_pdwScanPDL[dwI];
//				}	
//			}
//			else
//			{				
//				for(dwI=0;dwI < m_dwScanCount;dwI++)
//				{
//					ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
//				}
//			}
			ar<<m_nHighOrLow<<m_nTLSChannelIndex;
			if(m_nHighOrLow > 0 && m_nHighOrLow <= m_nTLSChannelIndex)
			{
				ar<<m_dwScanCount;				
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
				}
			}
			break;
		case MSG_TYPE_TLS_SETING:
			ar<<m_dbTLSAtten<<m_dbTLSPMWL;
			ar<<m_dbTLSPW<<m_dbTLSWL<<m_nHighOrLow;
			break;
		case MSG_TYPE_TLS_READ:
			break;
		case MSG_TYPE_TLS_POWER:
			ar<<m_dbTLSPW;
			break;
		case MSG_TYPE_SCAN_ONCE:
			ar<<m_strRefFileName;
			ar<<m_RefInfo.m_dwChannelCfgHigh;
			ar<<m_RefInfo.m_dwChannelCfgLow;
			ar<<m_RefInfo.m_dwChannelNumber;
			ar<<m_RefInfo.m_dwNumberOfScan;
			ar<<m_RefInfo.strFilePathName;
			break;
		case MSG_TYPE_NOPDL_SCAN:
			ar<<m_nClientCHIndex;
			ar<<m_nPDLStatus;
			ar<<m_byPDLScan;
			break;
		case MSG_TYPE_PDL_SCAN:
			ar<<m_nClientCHIndex;
			ar<<m_nPDLStatus;
			ar<<m_byPDLScan;
			break;
		case MSG_TYPE_STOP_SCAN:
			ar<<m_nClientCHIndex;
			break;
		case MSG_TYPE_ALPHA_OK:
			 ar<<m_dbAlphaData;
			 ar<<m_strRefAlphaTime;
			 break;
		case MSG_TYPE_REFERENCE_OK:
			ar<<m_nPDLStatus;
			break;
		case MSG_TYPE_TLS_SCAN_OK:
			ar<<m_nPDLStatus;
			ar<<m_dwScanCount;
			break;
		case MSG_TYPE_CLIENT_CHECK:
			break;
		case MSG_TYPE_REFERENCE_DELETEFILE:
			ar<<m_strRefFileName;
			break;
		case MSG_TYPE_SCAN_ALPHA:
//			ar<<m_RefInfo.m_dblStartWL;
//			ar<<m_RefInfo.m_dblStopWL;
// 			ar<<m_RefInfo.m_dblTLSPower;
			break;
		case MSG_TYPE_CLIENT_ALPHA:
			ar<<m_dbTLSAtten;
			break;
		case MSG_TYPE_REFERENCE_NEXTCH:
			ar<<m_nHighOrLow<<m_nTLSChannelIndex;
			if(m_nHighOrLow > 1 && m_nHighOrLow <= m_nTLSChannelIndex+1)
			{
				ar<<m_dwScanCount;				
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
				}
			}
			
			break;
		case MSG_TYPE_REFERENCE_NEXTCHOK:
			break;
		case MSG_TYPE_REFERENCE_NEXTCHERROR:
			break;
		case MSG_TYPE_SWITCH_SCAN_ONCE:
			ar<<m_strRefFileName;
			ar<<m_RefInfo.m_dwChannelCfgHigh;
			ar<<m_RefInfo.m_dwChannelCfgLow;
			ar<<m_RefInfo.m_dwChannelNumber;
			ar<<m_RefInfo.m_dwNumberOfScan;
			ar<<m_RefInfo.strFilePathName;
			break;
		case MSG_TYPE_SWITCH_NEXTCH:
			ar<<m_nHighOrLow<<m_nTLSChannelIndex;
			if(m_nHighOrLow > 1 && m_nHighOrLow <= m_nTLSChannelIndex+1)
			{
				ar<<m_dwScanCount;				
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
				}
			}
			
			break;
		case MSG_TYPE_SWITCH_NEXTCHOK:
			break;
		case MSG_TYPE_DEVICE_OPEN_OK:
			ar<<m_b8164Open;
			ar<<m_b8169Open;
			ar<<m_dbAlphaData;
			ar<<m_strRefAlphaTime;
			break;
		case MSG_TYPE_SWITCH_NEXTCHERROR:
			break;
		case MSG_TYPE_OSA_SCAN_ONCE:
			ar<<m_dbOSAStartWL<<m_dbOSAStopWL<<m_dbOSAStep<<m_dbOSAres;
			break;
		case MSG_TYPE_OSA_SCAN_OK:
			ar<<m_dwScanCount;				
			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar<<m_pdwScanWL[dwI]<<m_pdwScanData[dwI];
			}
			break;
		case MSG_TYPE_SINGLE_TEST:
			ar<<m_dwScanCount;//用于记录点测类型 MSG_TYPE_SINGLE_PROCESS，MSG_TYPE_SINGLE_LOCK_READ，MSG_TYPE_SINGLE_LOCK_NOREAD
			ar<<m_dw8164Address;//用于记录本次测试的光源序号
			ar<<m_dbOSAStartWL;//用于记录本次测试使用的激光器波长

			break;
		case MSG_TYPE_SINGLE_TESTOK:
			ar<<m_dwScanCount;
			if(m_dwScanCount == 1)
			{
				ar<<m_dbTLSPW;//记录单点功率

			}
			else
			{
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar<<m_pdwScanData[dwI];
				}
			}
			break;
		default:
			break;
		}
	}
	else//取出
	{
		ar>>m_byMsgType;
		switch(m_byMsgType)
		{
		case MSG_TYPE_ASE:
			ar>>m_bASE;
			break;
		case MSG_TYPE_NEWCLIENT:
			ar>>m_strUserName>>m_strIpAddress>>m_nUserPort;
			ar>>m_nClientCHIndex;
			break;
		case MSG_TYPE_EMPTY:
			break;
		case MSG_TYPE_CLOSE:
			m_bClose = TRUE;
			break;
		case MSG_TYPE_CLIENT_REFFILE:
			break;
		case MSG_TYPE_SERVER_REFFILE:
			ar>>m_bRefFileType;
			ar>>m_nRefFileIndex;
			ar>>m_RefInfo.strFileTitle;
			ar>>m_RefInfo.strFilePathName;
			ar>>m_RefInfo.m_bDoPDL;
			ar>>m_RefInfo.m_bReference;
			ar>>m_RefInfo.m_dblAlphaValue;
			ar>>m_RefInfo.m_dblPWMPower;
			ar>>m_RefInfo.m_dblStartWL;
			ar>>m_RefInfo.m_dblStepSize;
			ar>>m_RefInfo.m_dblStopWL;
			ar>>m_RefInfo.m_dblTLSPower;
			ar>>m_RefInfo.m_dwChannelCfgHigh;
			ar>>m_RefInfo.m_dwChannelCfgLow;
			ar>>m_RefInfo.m_dwChannelNumber;
			ar>>m_RefInfo.m_dwNumberOfScan;
			ar>>m_RefInfo.m_dwSampleCount;
			ar>>m_RefInfo.m_dwSize;
			break;
		case MSG_TYPE_ERROR:
			ar>>m_strErrorMsg;
			break;
	
		case MSG_TYPE_FINSH_REFFILE:
			break;
		case MSG_TYPE_SERVER_CLOSE:
			break;
		case MSG_TYPE_SERVER_NOREFFILE:
			break;
		case MSG_TYPE_TLS_OPEN:
			ar>>m_bHas8164>>m_dw8164Address;
			ar>>m_bHas8169>>m_dw8169Address;
			break;
		case MSG_TYPE_TLS_HAS:
//			ar>>m_b8164Open>>m_b8169Open>>m_bOSAOpen;//delete by xiaomingg2010-10-29
			ar>>m_b8164Open>>m_b8169Open;
			break;
		case MSG_TYPE_SCAN_REF:
			ar>>m_nClientCHIndex;
			ar>>m_nPDLStatus;
			ar>>m_byPDLScan;
			break;
		case MSG_TYPE_SCAN_REFDATA:
			ar>>m_dwScanCount;
			
			if(m_pdwScanWL!=NULL)
			{
				delete m_pdwScanWL;
				m_pdwScanWL = NULL;
			}
			if(m_pdwScanData != NULL)
			{
				delete m_pdwScanData;
				m_pdwScanData = NULL;
			}
				
			m_pdwScanWL = new DWORD[m_dwScanCount];
			m_pdwScanData = new DWORD[m_dwScanCount];
			
			
			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
			}
			break;
		case MSG_TYPE_SCAN_DATA:
//			ar>>m_dwScanCount>>m_bPDL>>m_nTLSChannelIndex;
//			if(m_bPDL)
//			{
//				if(m_pdwScanWL!=NULL)
//				{
//					delete m_pdwScanWL;
//					m_pdwScanWL = NULL;
//				}
//				if(m_pdwScanData != NULL)
//				{
//					delete m_pdwScanData;
//					m_pdwScanData = NULL;
//				}
//				if(m_pdwScanPDL != NULL)
//				{
//					delete m_pdwScanPDL;
//					m_pdwScanPDL = NULL;
//				}
//				m_pdwScanWL = new DWORD[m_dwScanCount];
//				m_pdwScanData = new DWORD[m_dwScanCount];
//				m_pdwScanPDL = new DWORD[m_dwScanCount];
//
//				for( dwI=0;dwI < m_dwScanCount;dwI++)
//				{
//					ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI]>>m_pdwScanPDL[dwI];
//				}	
//			}
//			else
//			{
//				if(m_pdwScanWL!=NULL)
//				{
//					delete m_pdwScanWL;
//					m_pdwScanWL = NULL;
//				}
//				if(m_pdwScanData != NULL)
//				{
//					delete m_pdwScanData;
//					m_pdwScanData = NULL;
//				}
//				m_pdwScanWL = new DWORD[m_dwScanCount];
//				m_pdwScanData = new DWORD[m_dwScanCount];
//				
//				for(dwI=0;dwI < m_dwScanCount;dwI++)
//				{
//					ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
//				}
//			}
			ar>>m_nHighOrLow>>m_nTLSChannelIndex;

			if(m_nHighOrLow > 0 && m_nHighOrLow <= m_nTLSChannelIndex)
			{
				ar>>m_dwScanCount;	
				if(m_pdwScanWL!=NULL)
				{
					delete m_pdwScanWL;
					m_pdwScanWL = NULL;
				}
				if(m_pdwScanData != NULL)
				{
					delete m_pdwScanData;
					m_pdwScanData = NULL;
				}
			
				m_pdwScanWL = new DWORD[m_dwScanCount];
				m_pdwScanData = new DWORD[m_dwScanCount];
			
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
				}
			}

			break;
		case MSG_TYPE_TLS_SETING:
			ar>>m_dbTLSAtten>>m_dbTLSPMWL;
			ar>>m_dbTLSPW>>m_dbTLSWL>>m_nHighOrLow;
			break;
		case MSG_TYPE_TLS_READ:
			break;
		case MSG_TYPE_TLS_POWER:
			ar>>m_dbTLSPW;
			break;
		case MSG_TYPE_DEVICE_OPEN_OK:
			ar>>m_b8164Open;
			ar>>m_b8169Open;
			ar>>m_dbAlphaData;
			ar>>m_strRefAlphaTime;
			break;
		case MSG_TYPE_REFERENCE_OK:
			ar>>m_nPDLStatus;
			break;
		case MSG_TYPE_SCAN_ONCE:
			ar>>m_strRefFileName;
			ar>>m_RefInfo.m_dwChannelCfgHigh;
			ar>>m_RefInfo.m_dwChannelCfgLow;
			ar>>m_RefInfo.m_dwChannelNumber;
			ar>>m_RefInfo.m_dwNumberOfScan;
			ar>>m_RefInfo.strFilePathName;
			break;
		case MSG_TYPE_NOPDL_SCAN:
			ar>>m_nClientCHIndex;
			ar>>m_nPDLStatus;
			ar>>m_byPDLScan;
			break;
		case MSG_TYPE_PDL_SCAN:
			ar>>m_nClientCHIndex;
			ar>>m_nPDLStatus;
			ar>>m_byPDLScan;
			break;
		case MSG_TYPE_STOP_SCAN:
			ar>>m_nClientCHIndex;
			break;
		case MSG_TYPE_CLIENT_CHECK:
			break;
		case MSG_TYPE_REFERENCE_DELETEFILE:
			ar>>m_strRefFileName;
			break;
		case MSG_TYPE_SCAN_ALPHA:
//			ar>>m_RefInfo.m_dblStartWL;
//			ar>>m_RefInfo.m_dblStopWL;
// 			ar>>m_RefInfo.m_dblTLSPower;
			break;
		case MSG_TYPE_CLIENT_ALPHA:
			ar>>m_dbTLSAtten;
			break;
		case MSG_TYPE_ALPHA_OK:
			ar>>m_dbAlphaData;
			ar>>m_strRefAlphaTime;
			break;
		case MSG_TYPE_TLS_SCAN_OK:
			ar>>m_nPDLStatus;
			ar>>m_dwScanCount;
		//	ar>>m_dblStepSize;
			break;
		case MSG_TYPE_REFERENCE_NEXTCH:
			ar>>m_nHighOrLow>>m_nTLSChannelIndex;

			if(m_nHighOrLow > 1 && m_nHighOrLow <= m_nTLSChannelIndex+1)
			{
				ar>>m_dwScanCount;	

				if(m_pdwScanWL!=NULL)
				{
					delete m_pdwScanWL;
					m_pdwScanWL = NULL;
				}
				if(m_pdwScanData != NULL)
				{
					delete m_pdwScanData;
					m_pdwScanData = NULL;
				}
				m_pdwScanWL = new DWORD[m_dwScanCount];
				m_pdwScanData = new DWORD[m_dwScanCount];
				
			
			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
			}
			
			}
			
			break;
		case MSG_TYPE_REFERENCE_NEXTCHOK:
			break;
		case MSG_TYPE_REFERENCE_NEXTCHERROR:
			break;
		case MSG_TYPE_SWITCH_SCAN_ONCE:
			ar>>m_strRefFileName;
			ar>>m_RefInfo.m_dwChannelCfgHigh;
			ar>>m_RefInfo.m_dwChannelCfgLow;
			ar>>m_RefInfo.m_dwChannelNumber;
			ar>>m_RefInfo.m_dwNumberOfScan;
			ar>>m_RefInfo.strFilePathName;
			break;
		case MSG_TYPE_SWITCH_NEXTCH:
			ar>>m_nHighOrLow>>m_nTLSChannelIndex;

			if(m_nHighOrLow > 1 && m_nHighOrLow <= m_nTLSChannelIndex+1)
			{
				ar>>m_dwScanCount;	

				if(m_pdwScanWL!=NULL)
				{
					delete m_pdwScanWL;
					m_pdwScanWL = NULL;
				}
				if(m_pdwScanData != NULL)
				{
					delete m_pdwScanData;
					m_pdwScanData = NULL;
				}
			
				
			m_pdwScanWL = new DWORD[m_dwScanCount];
			m_pdwScanData = new DWORD[m_dwScanCount];
			
			
			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
			}
				
			}
			
			break;
		case MSG_TYPE_SWITCH_NEXTCHOK:
			break;
		case MSG_TYPE_SWITCH_NEXTCHERROR:
			break;
		case MSG_TYPE_OSA_SCAN_ONCE:
			ar>>m_dbOSAStartWL>>m_dbOSAStopWL>>m_dbOSAStep>>m_dbOSAres;
			break;
		case MSG_TYPE_OSA_SCAN_OK:
			ar>>m_dwScanCount;	
			if(m_pdwScanWL!=NULL)
			{
				delete m_pdwScanWL;
				m_pdwScanWL = NULL;
			}
			if(m_pdwScanData != NULL)
			{
				delete m_pdwScanData;
				m_pdwScanData = NULL;
			}
		
			m_pdwScanWL = new DWORD[m_dwScanCount];
			m_pdwScanData = new DWORD[m_dwScanCount];
			
			
			for(dwI=0;dwI < m_dwScanCount;dwI++)
			{
				ar>>m_pdwScanWL[dwI]>>m_pdwScanData[dwI];
			}

			break;
		case MSG_TYPE_SINGLE_TEST:
			ar>>m_dwScanCount;//用于记录点测类型 MSG_TYPE_SINGLE_PROCESS，MSG_TYPE_SINGLE_LOCK_READ，MSG_TYPE_SINGLE_LOCK_NOREAD
			ar>>m_dw8164Address;//用于记录本次测试的光源序号
			ar>>m_dbOSAStartWL;//用于记录本次测试使用的激光器波长

			break;

		case MSG_TYPE_SINGLE_TESTOK:
			ar>>m_dwScanCount;
			if(m_dwScanCount == 1)
			{
				ar>>m_dbTLSPW;//记录单点功率

			}
			else
			{
				if(m_pdwScanData != NULL)
				{
					delete m_pdwScanData;
					m_pdwScanData = NULL;
				}
				m_pdwScanData = new DWORD[m_dwScanCount];
				for(dwI=0;dwI < m_dwScanCount;dwI++)
				{
					ar>>m_pdwScanData[dwI];
				}
			}
			break;
		case MSG_TYPE_TLS_SETTING_OK:
			ar>>m_strErrorMsg;
			break;

		default:
			break;
		}
	}
}
