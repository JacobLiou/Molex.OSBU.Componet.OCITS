#include "StdAfx.h"
#include "FSTPClient.h"


BOOL CFSTPClient::m_UDLInit = FALSE;
int  CFSTPClient::m_InitRes = 1;
BOOL CFSTPClient::m_HasPDLSwitch = FALSE;

CFSTPClient::CFSTPClient()
{
	memset(m_dataPathServer, 0, 8 * 256);
	m_UDLInit = FALSE;
}

CFSTPClient::~CFSTPClient()
{
	CoUninitialize();
}

void CFSTPClient::WriteLog(char * chLog)
{
	CFileFind isFind;

	FILE *fp = NULL;
	char fileName[256] = { 0 };
	CTime		tmNow = CTime::GetCurrentTime();
	sprintf_s(fileName, "log\\ITLClient%s.log", tmNow.Format("%Y%m%d"));
	char chWriteLog[1024] = { 0 };
	sprintf_s(chWriteLog, "%s  %s", tmNow.Format("%Y%m%d %H:%M:%S"), chLog);
	fopen_s(&fp, fileName, "a");
	if (fp != NULL)
	{
		fprintf(fp, chWriteLog);
		fprintf(fp, "\r\n");
		fclose(fp);
	}

}

BOOL CFSTPClient::InitialUDLEngine()
{
	WriteLog("InitialUDLEngine");
	char chLog[256] = { 0 };
	int pmCount = m_ClientoServerInfoPDL.m_nPowermeterCount;
	sprintf_s(chLog, 256, "%d,", pmCount);
	for (int i = 0;i < pmCount;i++)
	{
		sprintf_s(chLog, 256, "%s,PM:%d", chLog, m_ClientoServerInfoPDL.m_nPowermeterPorts[i]);
	}
	WriteLog(chLog);

	pmCount = m_ClientoServerInfo.m_nPowermeterCount;
	sprintf_s(chLog, 256, "%d,", pmCount);
	for (int i = 0;i < pmCount;i++)
	{
		sprintf_s(chLog, 256, "%s,PM:%d", chLog, m_ClientoServerInfo.m_nPowermeterPorts[i]);
	}
	WriteLog(chLog);

	if (m_InitRes == 0)
		return 0;
	if (m_UDLInit)
	{
		m_HasPDLSwitch = TRUE;
		return 1;
	}
	CoInitialize(NULL);
	//CString strMsg;
	TCHAR	m_tszAppFolder[255];
	char strUDLConfigXMLFile[256];
	char strLog[256] = { 0 };
	//m_bOpenTLS = FALSE;
	//增加读服务器数据路径功能
	//
	//rjf test
	sprintf_s(m_dataPathServer[0], "\\\\172.16.137.224\\testdata1\\MY4_Slot00");
	sprintf_s(m_dataPathServer[1], "\\\\172.16.137.224\\testdata1\\MY4_Slot00");
	sprintf_s(m_dataPathServer[2], "\\\\172.16.137.224\\testdata1\\MY4_Slot00");
	sprintf_s(m_dataPathServer[3], "\\\\172.16.137.224\\testdata1\\MY4_Slot00");

	GetCurrentDirectory(256, m_tszAppFolder);
	//// 打开第一台ＴＬＳ
	sprintf_s(strUDLConfigXMLFile, "%s\\set\\UDLConfig.xml", m_tszAppFolder);
	WriteLog(strUDLConfigXMLFile);
	char udlLogFile[256] = { 0 };
	sprintf_s(udlLogFile, "%s\\log\\UDLLog.txt", m_tszAppFolder);

	HRESULT hr = m_pEngine.CreateInstance(__uuidof(UDL2_Engine));
	if (hr != S_OK)
	{
		sprintf_s(strLog, "Engine CreateInstance 出错:%d", hr);
		WriteLog(strLog);
	}
	WriteLog("CreateInstance Engine");
	hr = m_pFSTP.CreateInstance(__uuidof(UDL2_FSTP));
	ASSERT(SUCCEEDED(hr));

	hr = m_pOSW.CreateInstance(__uuidof(UDL2_OSW));
	ASSERT(SUCCEEDED(hr));
		
	//	HRESULT hr=m_DeviceOper.m_pEngine->SetDebugLogFile((_bstr_t)udlLogFile);
	BSTR bstrString = (_bstr_t)udlLogFile;
	hr = m_pEngine->SetDebugLogFile(bstrString);
	SysAllocString(bstrString);


	if (m_pEngine->LoadConfiguration((_bstr_t)strUDLConfigXMLFile) != S_OK)
	{
		sprintf_s(m_strLog, "UDL配置文件不存在。");
		WriteLog(m_strLog);
		m_InitRes = 0;
		return 0;
	}
	else
	{
		hr = m_pEngine->OpenEngine();
		ASSERT(SUCCEEDED(hr));
		if (hr == S_FALSE)
		{
			_com_error e(hr);
			
			sprintf_s(m_strLog, "%s", e.ErrorMessage());
			WriteLog("OpenEngine FAIL");
			m_UDLInit = TRUE;
			m_InitRes = 0;
			return 0;
		}
		else
		{
			//m_bOpenTLS = TRUE;
			sprintf_s(strLog, "初始化设备完成！");
			WriteLog(strLog);
		}
	}
	m_UDLInit = TRUE;
	return 1;
}

int CFSTPClient::TLSScan(BOOL bDoPDL, double dWLStart, double dWLStop, double dStep, CString strfilefullname)
{
	//DO PDL的GUID为2，NO PDL的GUID是1
	int nScanGUID = 1;
	int nSWGUID = 1;
	long pmCount = m_ClientoServerInfo.m_nPowermeterCount;
	long *pmIndexs = new long[pmCount];
	double *dblRang = new double[pmCount];
	

	if (bDoPDL)
	{
		WriteLog("PDL Scan");
		char chLog[256] = { 0 };
		nSWGUID = 2;
		nScanGUID = 2;
		pmCount = m_ClientoServerInfoPDL.m_nPowermeterCount;
		sprintf_s(chLog, 256, "%d,", pmCount);
		for (int i = 0;i < pmCount;i++)
		{
			pmIndexs[i] = m_ClientoServerInfoPDL.m_nPowermeterPorts[i];
			dblRang[i] = 0;
			sprintf_s(chLog, 256, "%s,PM:%d,Range:%d", chLog,pmIndexs[i], dblRang[i]);
		}
		WriteLog(chLog);
	}
	else
	{
		WriteLog("IL Scan");
		pmCount = m_ClientoServerInfo.m_nPowermeterCount;
		char chLog[256] = { 0 };
		sprintf_s(chLog, 256, "%d,", pmCount);
		for (int i = 0;i < pmCount;i++)
		{
			pmIndexs[i] = m_ClientoServerInfo.m_nPowermeterPorts[i];
			dblRang[i] = 0;
			sprintf_s(chLog, 256, "%s,PM:%d,Range:%d", chLog, pmIndexs[i], dblRang[i]);
		}
		WriteLog(chLog);
	}

	HRESULT hr=S_FALSE;
	if (m_HasPDLSwitch)
	{
		hr = m_pOSW->SetSwitchPosition(nSWGUID, 1, 1);
		if (S_OK != hr)
		{
			sprintf_s(m_strLog, "SetSwitchPosition失败,错误码：%d", hr);
			WriteLog(m_strLog);
			return 0;
		}
		WriteLog("SetSwitchPosition success");
	}

	hr = m_pFSTP->SetFSTPParameters(nScanGUID, dWLStart, dWLStop, dStep);
	if (S_OK != hr)
	{
		sprintf_s(m_strLog, "SetFSTPParameters失败,错误码：%d",hr);
		WriteLog(m_strLog);
		return 0;
	}
	WriteLog("SetFSTPParameters success");

	hr = m_pFSTP->SetAllPMParameters(nScanGUID, 0, NULL, dblRang, 0, pmCount, pmIndexs);
	if (S_OK != hr)
	{
		sprintf_s(m_strLog, "SetAllPMParameters失败,错误码：%d", hr);
		WriteLog(m_strLog);
		delete[] pmIndexs;
		delete[] dblRang;
		return 0;
	}
	WriteLog("SetAllPMParameters success");

	if (bDoPDL)
	{
		hr = m_pFSTP->ExecutePDLSingleSweep(nScanGUID);
		if (S_OK != hr)
		{
			sprintf_s(m_strLog, "ExecutePDLSingleSweep失败,错误码：%d", hr);
			WriteLog(m_strLog);
			delete[] pmIndexs;
			delete[] dblRang;
			return 0;
		}
		WriteLog("ExecutePDLSingleSweep success");
	}
	else
	{
		hr = m_pFSTP->ExecuteILSingleSweep(nScanGUID);
		if (S_OK != hr)
		{
			sprintf_s(m_strLog, "ExecuteILSingleSweep失败,错误码：%d", hr);
			WriteLog(m_strLog);
			delete[] pmIndexs;
			delete[] dblRang;
			return 0;
		}
		WriteLog("ExecuteILSingleSweep success");
	}
	
	int waitTime = 180;
	bool bScanSuccess = false;
	while (waitTime > 0)
	{
		long lTime = 0;
		long lStatus = 0;
		m_pFSTP->GetSweepStatus(nScanGUID, &lStatus, &lTime);
		if (lStatus == 0)
		{
			WriteLog("GetSweepStatus:0");
			waitTime--;
			Sleep(1000);
			continue;
		}
		else if (lStatus == -1)
		{
			char chErr[1024] = { 0 };
			m_pEngine->GetLastErrorMessage(chErr, 1024);
			sprintf_s(m_strLog, "获取扫描数据失败:%s", chErr);
			WriteLog(m_strLog);
			delete[] pmIndexs;
			delete[] dblRang;
			return 0;
		}
		else if (lStatus == 1)
		{
			bScanSuccess = true;
			WriteLog("GetSweepStatus ScanSuccess");
			break;
		}
	}
	if (bScanSuccess)
	{
		long dataCount = (int)((dWLStop - dWLStart) / dStep+0.5) + 1;
		if (dataCount <= 0)
		{
			delete[] pmIndexs;
			delete[] dblRang;
			sprintf_s(m_strLog, "扫描点数出错，为0！");
			WriteLog(m_strLog);
			return 0;
		}
		if (bDoPDL)
		{
			WriteLog("GetMeasureResultWithTETM begin");
			double *dblIL = new double[dataCount];
			double *dblWL = new double[dataCount];
			double *dblPDL = new double[dataCount];
			double *dblTE = new double[dataCount];
			double *dblTM = new double[dataCount];
			double *dblTapIL = new double[dataCount];
			char chLog[256] = { 0 };
			sprintf_s(chLog, "pm count:%d,data count:%d", pmCount, dataCount);
			WriteLog(chLog);
			for (int i = 0;i < pmCount;i++)
			{
				memset(dblIL, 0, sizeof(double)*dataCount);
				memset(dblWL, 0, sizeof(double)*dataCount);
				hr = m_pFSTP->GetMeasureResultWithTETM(nScanGUID, pmIndexs[i] - 1, dblWL, dblIL, dblPDL, dblTE, dblTM, dblTapIL, &dataCount);
				if (S_OK != hr)
				{
					delete[] dblIL;
					delete[] dblWL;
					delete[] dblPDL;
					delete[] dblTE;
					delete[] dblTM;
					delete[] dblTapIL;
					delete[] pmIndexs;
					delete[] dblRang;
					sprintf_s(m_strLog, "GetMeasureResult失败，错误码：%d", hr);
					WriteLog(m_strLog);
					return 0;
				}
				WriteLog("GetMeasureResultWithTETM success");
				char localFile[256] = { 0 };
				sprintf_s(localFile, "%s%d.csv", strfilefullname.GetBuffer(), i + 1);
				WriteLog(localFile);
				FILE *fp = NULL;
				fp = fopen(localFile, "w");
				if (fp != NULL)
				{
					fprintf_s(fp, "WL,Power\n");
					//rjf test
					//
					//for (int nData = 0;nData < 10;nData++)
					for (int nData = 0;nData < dataCount;nData++)
					{
						fprintf_s(fp, "%f,%f,%f,%f,%f\n", dblWL[nData], dblIL[nData], dblPDL[nData], dblTE[nData], dblTM[nData]);
					}
					fclose(fp);
				}
				WriteLog("写PDL数据文件结束");
			}
			/*for (int i = 0;i < pmCount;i++)
			{
				for (int nPDL = 1;nPDL < 5;nPDL++)
				{
					char chPDLFilePath[256] = { 0 };
					sprintf_s(chPDLFilePath, "%s%d_%d.dat", m_dataPathServer[i], pmIndexs[i], nPDL);
					char chDestPath[256] = { 0 };
					sprintf_s(chDestPath, "Chan%d_PDL%d.dat", pmIndexs[i], nPDL);
					if (!CopyFile(chPDLFilePath, chDestPath, false))
					{
						if (!CopyFile(chPDLFilePath, chDestPath, false))
						{
							delete[] dblIL;
							delete[] dblWL;
							delete[] pmIndexs;
							delete[] dblRang;
							sprintf_s(m_strLog, "从服务器拷贝扫描数据失败！");
							return FALSE;
						}
					}
				}
			}
			for (int i = 0;i < pmCount;i++)
			{
				for (int nPDL = 1;nPDL < 5;nPDL++)
				{
					char chDestPath[256] = { 0 };
					sprintf_s(chDestPath, "Chan%d_PDL%d.dat", pmIndexs[i], nPDL);
					HANDLE hDataFile = CreateFile(chDestPath, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);
					if (hDataFile == INVALID_HANDLE_VALUE)
					{
						delete[] dblIL;
						delete[] dblWL;
						delete[] pmIndexs;
						delete[] dblRang;
						sprintf_s(m_strLog, "打开扫描原始数据文件失败！");
						return FALSE;
					}

					DWORD dwReadSize = GetFileSize(hDataFile, NULL);
					if (dwReadSize != dataCount * sizeof(double) * 2)
					{
						delete[] dblIL;
						delete[] dblWL;
						delete[] pmIndexs;
						delete[] dblRang;
						sprintf_s(m_strLog, "扫描点数与返回数据不对应！");
						return FALSE;
					}

					DWORD dwBufferSize = dataCount * sizeof(double);
					DWORD dwBytesReturned = 0;
					ReadFile(hDataFile, dblWL, dwBufferSize, &dwBytesReturned, NULL);
					if (dwBytesReturned != dwBufferSize)
					{
						delete[] dblIL;
						delete[] dblWL;
						delete[] pmIndexs;
						delete[] dblRang;
						sprintf_s(m_strLog, "读取扫描数据出错");
						return FALSE;
					}

					ReadFile(hDataFile, dblIL+(nPDL-1)*dataCount, dwBufferSize, &dwBytesReturned, NULL);
					if (dwBytesReturned != dwBufferSize)
					{
						delete[] dblIL;
						delete[] dblWL;
						delete[] pmIndexs;
						delete[] dblRang;
						sprintf_s(m_strLog, "读取扫描数据出错");
						return FALSE;
					}
					CloseHandle(hDataFile);
				}
				
				for (int j = 0;j < 4;j++)
				{
					char localFile[256] = { 0 };
					sprintf_s(localFile, "%s%d%d.csv", strfilefullname.GetBuffer(), i + 1, j + 1);
					FILE *fp = NULL;
					fp = fopen(localFile, "w");
					if (fp != NULL)
					{
						fprintf_s(fp, "WL,Power\n");
						for (int nData = 0;nData < dataCount;nData++)
						{
							fprintf_s(fp, "%f,%f\n", dblWL[nData], dblIL[nData + j*dataCount]);
						}
						fclose(fp);
					}
				}		
			}*/
			delete[] dblIL;
			delete[] dblWL;
			delete[] dblPDL;
			delete[] dblTE;
			delete[] dblTM;
			delete[] dblTapIL;
		}
		else
		{
			double *dblIL = new double[dataCount];
			double *dblWL = new double[dataCount];
			double *dblPDL = new double[dataCount];
			double *dblTapIL = new double[dataCount];
			for (int i = 0;i < pmCount;i++)
			{
				memset(dblIL, 0, sizeof(double)*dataCount);
				memset(dblWL, 0, sizeof(double)*dataCount);
				hr = m_pFSTP->GetMeasureResult(nScanGUID, pmIndexs[i] - 1, dblWL, dblIL, dblPDL, dblTapIL, &dataCount);
				if (S_OK != hr)
				{
					delete[] dblIL;
					delete[] dblWL;
					delete[] pmIndexs;
					delete[] dblRang;
					sprintf_s(m_strLog, "GetMeasureResult失败，错误码：%d", hr);
					WriteLog(m_strLog);
					return FALSE;
				}

				char localFile[256] = { 0 };
				sprintf_s(localFile, "%s%d.csv", strfilefullname.GetBuffer(), i + 1);
				WriteLog(localFile);
				FILE *fp = NULL;
				fp = fopen(localFile, "w");
				if (fp != NULL)
				{
					fprintf_s(fp, "WL,Power\n");
					for (int nData = 0;nData < dataCount;nData++)
					{
						fprintf_s(fp, "%f,%f\n", dblWL[nData], dblIL[nData]);
					}
					fclose(fp);
				}
				WriteLog("写IL数据文件结束");
			}
			delete[] dblIL;
			delete[] dblWL;	
			delete[] dblPDL;
			delete[] dblTapIL;
		}
	}
	else
	{
		sprintf_s(m_strLog, "扫描超时，未收到服务器数据");
		WriteLog(m_strLog);
		return 0;
	}
	delete[] pmIndexs;
	delete[] dblRang;
	WriteLog("测试结束");
	return 1;
}