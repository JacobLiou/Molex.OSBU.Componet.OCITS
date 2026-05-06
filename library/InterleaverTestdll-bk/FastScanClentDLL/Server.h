// Communication.h: interface for the CCommunication class.
//
//////////////////////////////////////////////////////////////////////

#if !defined(AFX_SERVER_H__78E7CC93_4279_4615_BCC8_FC59591157E5__INCLUDED_)
#define AFX_SERVER_H__78E7CC93_4279_4615_BCC8_FC59591157E5__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000


#include <winsock.h>

#pragma comment(lib, "wsock32.lib")

#define		WINSOCK_VERSION					0x11
#define		MAX_FIELD						8192
#define		MAX_DATA_COUNT					2048

class CServer
{
public:
	CServer();
	virtual ~CServer();

	virtual BOOL Open(LPCTSTR lpCommunicationInfo);
	virtual BOOL Close();
	virtual BOOL Write(PVOID pWriteBuffer, DWORD dwWriteLength);
	virtual BOOL Read(PVOID pREadBuffer, DWORD dwReadLength);
	virtual BOOL SerialPoll(PCHAR pbSPByte);
	virtual BOOL WaitForCompletion(WORD wMask);
  
	virtual BOOL IsRequestCompleted();
	virtual BOOL GetError(PSTR pszErrorMessage);
	virtual BOOL Clear();
	virtual void SetHandle(unsigned long pHandle);
  
	SOCKET				m_hSocket;
		


};

#endif // !defined(AFX_SERVER_H__78E7CC93_4279_4615_BCC8_FC59591157E5__INCLUDED_)
