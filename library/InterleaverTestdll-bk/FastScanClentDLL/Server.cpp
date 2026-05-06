// Lan.cpp: implementation of the CLan class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "Server.h"
#include <stdlib.h>
#include <stdio.h>

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

CServer::CServer()
{

}

CServer::~CServer()
{

}

BOOL CServer::Open(LPCTSTR lpCommunicationInfo)
{
	sockaddr_in	sockAddr;
	LPCTSTR lpDeviceAddr = 0;
	UINT uPort = 0;
	UINT uBroadIndex = 0;
	long lInfoLen = strlen(lpCommunicationInfo);
	long lFlagCount = 0;
	char chFlag = ',';

/*	for (int n = 0; n < lInfoLen; n++)
	{
		if (chFlag == lpCommunicationInfo[n])
		{
			lFlagCount++;
		}
	}
	
	TCHAR		tszTok[] = ",\r\n";
	char		*pRegComment;  // single register comment 
	char pchInfo[MAX_PATH];
	ZeroMemory(pchInfo, sizeof(pchInfo));
	if (lInfoLen > MAX_PATH)
		return FALSE;

	memcpy(pchInfo, lpCommunicationInfo, lInfoLen);
	//broad index 
	pRegComment = strtok(pchInfo, tszTok); 
	uBroadIndex = atol(pRegComment);
	//IP Address 
	pRegComment = strtok(NULL, tszTok); 
	lpDeviceAddr = pRegComment;
	//Port
	pRegComment = strtok(NULL, tszTok);
	uPort = atol(pRegComment);*/
	uPort=8888;
	lpDeviceAddr="172.16.143.173";

	//Init Ethernet 
	BYTE byResult;
	WSADATA	wsaData;
	WORD	wVersionRequested = MAKEWORD((WINSOCK_VERSION&0xF0)>>4, (WINSOCK_VERSION&0x0F));

	if(WSAStartup(wVersionRequested, &wsaData))
	{
		return FALSE;
	}

	m_hSocket = socket(AF_INET, SOCK_STREAM, 0);

	//Init Parameter
	sockAddr.sin_family = AF_INET;
	lpDeviceAddr="172.16.143.173";
	sockAddr.sin_addr.S_un.S_addr = inet_addr(lpDeviceAddr);
	sockAddr.sin_port = htons(uPort);

	byResult=connect(m_hSocket,(LPSOCKADDR)&sockAddr, sizeof(sockAddr));
	if(byResult)
	{
		return FALSE;
	}
	
	return TRUE;
}

BOOL CServer::Close()
{
	closesocket(m_hSocket);

	return TRUE;
}

BOOL CServer::Clear()
{
//	PVOID pReadBuffer[MAX_PATH];
	if (!Write("\r\n", strlen("\r\n")))
	{
		return FALSE;
	}

//	if (!Read(pReadBuffer, MAX_PATH))
//	{
//		return FALSE;
//	}

	return TRUE;
}

BOOL CServer::Write(PVOID pWriteBuffer, DWORD dwWriteLength)
{
	LPCSTR pszWriteBuffer = (LPSTR)pWriteBuffer;

	if (!send(m_hSocket, pszWriteBuffer, dwWriteLength, 0))
	{
		return FALSE;
	}
	
	return TRUE;
}

BOOL CServer::Read(PVOID pReadBuffer, DWORD dwReadLength)
{
	ZeroMemory(pReadBuffer, dwReadLength);
	
	if(!recv(m_hSocket, (char*)pReadBuffer, dwReadLength, 0))
	{
		return FALSE;
	}

	return TRUE;
}

BOOL CServer::SerialPoll(PCHAR pbSPByte)
{
	return TRUE;
}
 
BOOL CServer::WaitForCompletion(WORD wMask)
{
	return TRUE;
}

BOOL CServer::GetError(PSTR pszErrorMessage)
{
	return TRUE;
}

BOOL CServer::IsRequestCompleted()
{

	return TRUE;
}

void CServer::SetHandle(unsigned long pHandle)
{
	m_hSocket = (SOCKET)pHandle;
}


