
// SocketTestDlg.h : header file
//

#pragma once
#pragma comment(lib, "Ws2_32.lib")

#include <WinSock2.h>

// CSocketTestDlg dialog
class CSocketTestDlg : public CDialogEx
{
// Construction
public:
	CSocketTestDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_SOCKETTEST_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
public:
	SOCKET m_Socket;

protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedButton1();
	afx_msg void OnBnClickedSendref();
	afx_msg void OnBnClickedButton2();
	void TestSend();
	afx_msg void OnBnClickedReaddata();
};
