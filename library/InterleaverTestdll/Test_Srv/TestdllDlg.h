// TestdllDlg.h : header file
//

#if !defined(AFX_TESTDLLDLG_H__CF06F7D7_2A20_442F_B8E8_1523625249A8__INCLUDED_)
#define AFX_TESTDLLDLG_H__CF06F7D7_2A20_442F_B8E8_1523625249A8__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
#include "FastScanClientWrapper.h"
/////////////////////////////////////////////////////////////////////////////
// CTestdllDlg dialog

class CTestdllDlg : public CDialog
{
// Construction
public:
	CTestdllDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	//{{AFX_DATA(CTestdllDlg)
	enum { IDD = IDD_TESTDLL_DIALOG };
	CString	m_strShowMSG;
	//}}AFX_DATA
    //CFastScanClentDLL m_scandll;
	//CFastScanClentDLL m_scandll2;
	  stClentTestingConfig  m_ClientoServerInfo;
	  stClentTestingConfig  m_ClientoServerInfo2;
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CTestdllDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation

protected:
	HICON m_hIcon;

	// Generated message map functions
	//{{AFX_MSG(CTestdllDlg)
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void Onconnect();
	afx_msg void OnRef();
	afx_msg void OnTest();
	afx_msg void OnButton4();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedButton5();
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_TESTDLLDLG_H__CF06F7D7_2A20_442F_B8E8_1523625249A8__INCLUDED_)
