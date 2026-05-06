// SendMsg.h: interface for the CSendMsg class.
//
//////////////////////////////////////////////////////////////////////

#if !defined(AFX_SENDMSG_H__AF603E62_4411_4CE4_874E_7A4E04E27744__INCLUDED_)
#define AFX_SENDMSG_H__AF603E62_4411_4CE4_874E_7A4E04E27744__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
//

#include "TestServerDataType.h"

class CSendMsg : public CObject  
{
	DECLARE_DYNCREATE(CSendMsg)
public:
	BOOL m_bClose;
	virtual void Serialize(CArchive& ar);
	CSendMsg();
	virtual ~CSendMsg();

public:
	//Scan
	DWORD* m_pdwScanWL;
	DWORD* m_pdwScanData;
	DWORD* m_pdwScanPDL;
	DWORD m_dwScanCount;
	double m_dblStepSize;
	BOOL  m_bPDL;
	CString m_strRefFileName;

	int   m_byMsgType;
	int     m_nUserPort;	

	double m_dbTLSPW;//激光器功率
	double m_dbTLSWL;//激光器波长

	CString m_strUserName;  //用户名
	CString m_strIpAddress;
	CString m_strErrorMsg;   // 错误信息

	double  m_dbTLSAtten;
	double  m_dbTLSPMWL;
	int     m_nHighOrLow;   //该参数复用为多通道归零时下一通道的索引，从1开始//同时复用为发送扫描时的通道索引
	int     m_nTLSChannelIndex;//该参数复用为多通道归零时的通道个数，
	int     m_nClientCHIndex;
	BYTE    m_byPDLScan;
	int		m_nPDLStatus;
	double  m_dbAlphaData;
	CString m_strRefAlphaTime;

	tagAutoRefParam m_RefInfo;

	BOOL  m_bRefFileType; // 归零文件更新类型 0 为刷新 ，1 为添加；
	int   m_nRefFileIndex; // 归零文件的个数索引 ,
							//


    BOOL  m_bautoOSA;
	BOOL  m_bHas8164;
	DWORD m_dw8164Address;
	BOOL   m_bHas8169;
	DWORD  m_dw8169Address;

	BOOL   m_b8164Open;
	BOOL   m_b8169Open;

	double m_dbOSAStartWL;//OSA扫描开始波长
	double m_dbOSAStopWL;//OSA扫描结束波长
	double m_dbOSAStep; //测试步长
	double	m_dbOSAres;//OSA分辨率
	BOOL    m_bOSAOpen;//OSA是否打开
	BOOL  m_bASE;

	
};

#endif // !defined(AFX_SENDMSG_H__AF603E62_4411_4CE4_874E_7A4E04E27744__INCLUDED_)
