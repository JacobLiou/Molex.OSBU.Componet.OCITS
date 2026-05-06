#pragma once
#include "TestServerDataType.h"

//应用的时候看如何配置数据文件
class CFSTPClient
{
public:
	CFSTPClient();
	~CFSTPClient();
	BOOL InitialUDLEngine();
	int TLSScan(BOOL bDoPDL, double dWLStart, double dWLStop, double dStep,CString strfilefullname);
	void WriteLog(char * chLog);
public:
	IUDL2_FSTPPtr m_pFSTP;
	IUDL2_EnginePtr m_pEngine;
	IUDL2_OSWPtr	m_pOSW;
	stClentTestingConfig  m_ClientoServerInfo;
	stClentTestingConfig  m_ClientoServerInfoPDL;
	char m_strLog[1024];
	//服务器端返回的数据路径，需要逐个配置
	char m_dataPathServer[8][256];
	static BOOL m_UDLInit;
	static BOOL m_HasPDLSwitch;
	static int  m_InitRes;
};