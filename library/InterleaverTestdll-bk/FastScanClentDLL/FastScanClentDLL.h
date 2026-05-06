// FastScanClentDLL.h : main header file for the FASTSCANCLENTDLL DLL
//

#if !defined(AFX_FASTSCANCLENTDLL_H__790400E2_0602_43E3_8B03_3232E087B62E__INCLUDED_)
#define AFX_FASTSCANCLENTDLL_H__790400E2_0602_43E3_8B03_3232E087B62E__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols

/////////////////////////////////////////////////////////////////////////////
// CFastScanClentDLLApp
// See FastScanClentDLL.cpp for the implementation of this class
//

class CFastScanClentDLLApp : public CWinApp
{
public:
	CFastScanClentDLLApp();

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CFastScanClentDLLApp)
	//}}AFX_VIRTUAL

	//{{AFX_MSG(CFastScanClentDLLApp)
		// NOTE - the ClassWizard will add and remove member functions here.
		//    DO NOT EDIT what you see in these blocks of generated code !
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_FASTSCANCLENTDLL_H__790400E2_0602_43E3_8B03_3232E087B62E__INCLUDED_)

