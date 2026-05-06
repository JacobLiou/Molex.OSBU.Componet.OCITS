// TestdllDlg.cpp : implementation file
//

#include "stdafx.h"
#include "Testdll.h"
#include "TestdllDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// Dialog Data
	//{{AFX_DATA(CAboutDlg)
	enum { IDD = IDD_ABOUTBOX };
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CAboutDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	//{{AFX_MSG(CAboutDlg)
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
	//{{AFX_DATA_INIT(CAboutDlg)
	//}}AFX_DATA_INIT
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CAboutDlg)
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
	//{{AFX_MSG_MAP(CAboutDlg)
		// No message handlers
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CTestdllDlg dialog

CTestdllDlg::CTestdllDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CTestdllDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(CTestdllDlg)
	m_strShowMSG = _T("");
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CTestdllDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CTestdllDlg)
	DDX_Text(pDX, IDC_EDIT1, m_strShowMSG);
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CTestdllDlg, CDialog)
	//{{AFX_MSG_MAP(CTestdllDlg)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON1, Onconnect)
	ON_BN_CLICKED(IDC_BUTTON2, OnRef)
	ON_BN_CLICKED(IDC_BUTTON3, OnTest)
	ON_BN_CLICKED(IDC_BUTTON4, OnButton4)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CTestdllDlg message handlers

BOOL CTestdllDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon
	
	// TODO: Add extra initialization here
	
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CTestdllDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CTestdllDlg::OnPaint() 
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CTestdllDlg::OnQueryDragIcon()
{
	return (HCURSOR) m_hIcon;
}

void CTestdllDlg::Onconnect() 
{
	// TODO: Add your control notification handler code here
	//m_ClientoServerInfo
	CString strPort = "";
	GetDlgItemText(IDC_EDIT2, strPort);
	m_ClientoServerInfo.m_nClientPortIndex= atoi(strPort);
	m_ClientoServerInfo.m_nClientTestPort= atoi(strPort);
	//m_ClientoServerInfo.m_nPort=8888;	
	m_ClientoServerInfo.m_nPort = 0;
	sprintf(m_ClientoServerInfo.m_tszClentDatapath, "\\rawdata");
	sprintf(m_ClientoServerInfo.m_tszClentRefDatapath, "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\Reference");
	sprintf(m_ClientoServerInfo.m_tszClientIP, "10.220.42.110");
	
	sprintf(m_ClientoServerInfo.m_tszClientName, "zuhl5cg8183r5l");
	sprintf(m_ClientoServerInfo.m_tszServerDatapath, "\\\\10.220.42.23\\Data");
	sprintf(m_ClientoServerInfo.m_tszServerIP, "10.220.42.23");

	
	m_ClientoServerInfo.m_nPowermeterPorts[0] = atoi(strPort);
	m_ClientoServerInfo.m_nPowermeterPorts[1] = 3;
	m_ClientoServerInfo.m_nPowermeterCount = 2;

	CString strtemp;
	if (!ConnectServer(m_ClientoServerInfo))
	{
         strtemp= GetMsg(m_ClientoServerInfo.m_nPowermeterPorts[0]);
	}


	/*m_ClientoServerInfo2.m_nClientPortIndex = 3;
	m_ClientoServerInfo2.m_nClientTestPort = 3;
	m_ClientoServerInfo2.m_nPort = 8888;
	sprintf(m_ClientoServerInfo2.m_tszClentDatapath, "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\data");
	sprintf(m_ClientoServerInfo2.m_tszClentRefDatapath, "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\Reference");
	sprintf(m_ClientoServerInfo2.m_tszClientIP, "10.220.33.172");

	sprintf(m_ClientoServerInfo2.m_tszClientName, "zuhl5cg8183r5l");
	sprintf(m_ClientoServerInfo2.m_tszServerDatapath, "\\\\172.16.134.121\\Data");
	sprintf(m_ClientoServerInfo2.m_tszServerIP, "172.16.134.121");


	CString strtemp2;
	if (!m_scandll2.ConnectServer(m_ClientoServerInfo2))
	{
		strtemp2 = m_scandll2.GetMsg();
	}*/
	
}

UINT ref(LPVOID param)
{
	CTestdllDlg *dlg = (CTestdllDlg*)param;
	CString strtemp;
	CString strfilename = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\reference\\refenceWithPDL";
	if (!TLSScan(true, true, 1, strfilename.GetBuffer()))
	{
		strtemp = GetMsg(1);
	}
	return 0;
}

UINT ref2(LPVOID param)
{
	CTestdllDlg *dlg = (CTestdllDlg*)param;
	CString strtemp;
	CString strfilename = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\reference\\PM3_refenceWithPDL.csv";
	if (!TLSScan(true, true, 3, strfilename.GetBuffer()))
	{
		strtemp = GetMsg(2);
	}
	return 0;
}


void CTestdllDlg::OnRef() 
{
	// TODO: Add your control notification handler code here
	AfxBeginThread(ref, this);
	Sleep(50);
	//AfxBeginThread(ref2, this);
	/*CString strtemp;
	if (!m_scandll.TLSScan(true,true,1)) 
	{
		strtemp=m_scandll.GetMsg();
	}*/
}


UINT Scan(LPVOID param)
{
	CTestdllDlg *dlg = (CTestdllDlg*)param;
	CString strtemp;
	CString strfilename = "rawdata\\TestWithPDL";

	/*if (!m_scandll.TLSScan(false, false, 2, strfilename))
	{
	strtemp = m_scandll.GetMsg();
	}*/
	//while (1)
	{
		if (!TLSScan(FALSE, false, 2, strfilename.GetBuffer()))
		{
			strtemp = GetMsg(2);
		}
		Sleep(10);
	}
	return 0;
}


UINT Scan2(LPVOID param)
{
	CTestdllDlg *dlg = (CTestdllDlg*)param;
	CString strtemp;
	CString strfilename = "\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\data\\PM3TestWithPDL";

	/*if (!m_scandll.TLSScan(false, false, 2, strfilename))
	{
	strtemp = m_scandll.GetMsg();
	}*/

	if (!TLSScan(TRUE, false, 3, strfilename.GetBuffer()))
	{
		strtemp = GetMsg(3);
	}
	return 0;
}

void CTestdllDlg::OnTest() 
{
	// TODO: Add your control notification handler code here
	AfxBeginThread(Scan, this);
	Sleep(50);
	//AfxBeginThread(Scan2, this);
	
}

void CTestdllDlg::OnButton4() 
{
	// TODO: Add your control notification handler code here
	m_strShowMSG=GetMsg(2);
	UpdateData(false);
}
