
// SocketTest.h : main header file for the PROJECT_NAME application
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CSocketTestApp:
// See SocketTest.cpp for the implementation of this class
//

class CSocketTestApp : public CWinApp
{
public:
	CSocketTestApp();

// Overrides
public:
	virtual BOOL InitInstance();

// Implementation

	DECLARE_MESSAGE_MAP()
};

extern CSocketTestApp theApp;