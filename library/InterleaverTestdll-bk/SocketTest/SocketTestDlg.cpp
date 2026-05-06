
// SocketTestDlg.cpp : implementation file
//

#include "stdafx.h"
#include "SocketTest.h"
#include "SocketTestDlg.h"
#include "afxdialogex.h"
#include <Ws2tcpip.h>
#include "SendMsg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CAboutDlg dialog used for App About

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ABOUTBOX };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(IDD_ABOUTBOX)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CSocketTestDlg dialog



CSocketTestDlg::CSocketTestDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_SOCKETTEST_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CSocketTestDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CSocketTestDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON1, &CSocketTestDlg::OnBnClickedButton1)
	ON_BN_CLICKED(IDC_SENDREF, &CSocketTestDlg::OnBnClickedSendref)
	ON_BN_CLICKED(IDC_BUTTON2, &CSocketTestDlg::OnBnClickedButton2)
	ON_BN_CLICKED(IDC_ReadData, &CSocketTestDlg::OnBnClickedReaddata)
END_MESSAGE_MAP()


// CSocketTestDlg message handlers

BOOL CSocketTestDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
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
	m_Socket = socket(AF_INET, SOCK_STREAM, 0);
	
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CSocketTestDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CSocketTestDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

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
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CSocketTestDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

UINT Threadtest(LPVOID param)
{
	CSocketTestDlg *pDlg = (CSocketTestDlg *)param;
	pDlg->TestSend();
	/*CSendMsg msg;
	msg.m_byMsgType = MSG_TYPE_NEWCLIENT;
	msg.m_strUserName = "zuhl5cg8183r5l";
	msg.m_nClientCHIndex = 2;
	msg.m_strIpAddress = "10.220.33.172";
	msg.m_nUserPort = 2;

	CFile *pFile = new CFile(_T("test2.txt"), 0x01002);
	CArchive *pAr = new CArchive(pFile, CArchive::store);
	msg.Serialize(*pAr);
	pAr->Flush();
	pAr->Close();
	pFile->Close();
	delete pFile;
	pFile = new CFile(_T("test2.txt"), CFile::modeRead);
	char ch[10 * 1024] = { 0 };
	int length = pFile->GetLength();
	pFile->Read(ch, length);
	
	CFile *pFile1 = new CFile(_T("test3.txt"), 0x01002);
	pFile1->Write(ch, length);
	pFile1->Close();
	delete pFile1;
	pFile1 = new CFile(_T("test3.txt"), CFile::modeRead);
	CArchive *pAread = new CArchive(pFile1, CArchive::load);
	//pAread->Flush();
	CSendMsg msg1;
	msg1.Serialize(*pAread);*/
	
	return 0;
}

void CSocketTestDlg::OnBnClickedButton1()
{
	// TODO: Add your control notification handler code here
	SOCKADDR_IN addrSrv;//socketAddress socket端口
						//服务器端口配置
	addrSrv.sin_family = AF_INET;
	addrSrv.sin_port = htons(8888);
	
	inet_pton(AF_INET,"172.16.134.121", &addrSrv.sin_addr);
	////作为客户端，你要连接【connect】到远端的服务器，也是要指定远端服务器的（ip, port）对。
	

	int res=connect(m_Socket, (SOCKADDR *)&addrSrv, sizeof(SOCKADDR));
	CSendMsg msg;
	msg.m_byMsgType = MSG_TYPE_NEWCLIENT;
	msg.m_strUserName = "zuhl5cg8183r5l";
	msg.m_nClientCHIndex = 2;
	msg.m_strIpAddress = "10.220.33.172";
	msg.m_nUserPort = 2;
	CFile *pFile = new CFile(_T("test2.txt"), 0x01002);
	CArchive *pAr = new CArchive(pFile, CArchive::store);
	msg.Serialize(*pAr);
	pAr->Flush();
	pAr->Close();
	pFile->Close();
	delete pFile;
	pFile = new CFile(_T("test2.txt"), CFile::modeRead);
	char ch[10 * 1024] = { 0 };
	int length = pFile->GetLength();
	pFile->Read(ch, length);
	send(m_Socket, ch, length, 0);

	bool bFinsh = false;
	pFile->Close();
	while (!bFinsh)
	{
		char chRec[1024 * 10] = { 0 };
		int recLen = recv(m_Socket, chRec, 1024 * 10, 0);
		if (recLen > 0)
		{
			CFile *pFile1 = new CFile(_T("test3.txt"), 0x01002);
			pFile1->Write(chRec, recLen);
			pFile1->Close();
			delete pFile1;
			pFile1 = new CFile(_T("test3.txt"), CFile::modeRead);
			CArchive *pAread = new CArchive(pFile1, CArchive::load);
			//pAread->Flush();
			CSendMsg msg1;
			msg1.Serialize(*pAread);
			bFinsh = true;
			pAread->Close();
			pFile1->Close();
			
			delete pAread;
			delete pFile1;

		}
		
		Sleep(50);
	}
	//AfxBeginThread(Threadtest, NULL);
	//send(m_Socket,)

	
}

void CSocketTestDlg::TestSend()
{
	CSendMsg msg;
	msg.m_byMsgType = MSG_TYPE_PDL_SCAN;
	msg.m_byPDLScan = true;
	msg.m_nClientCHIndex = 2;

	CFile *pFile = new CFile(_T("test2.txt"), 0x01002);
	CArchive *pAr = new CArchive(pFile, CArchive::store);
	msg.Serialize(*pAr);
	pAr->Flush();
	pAr->Close();
	pFile->Close();
	delete pFile;
	delete pAr;
	pFile = new CFile(_T("test2.txt"), CFile::modeRead);
	char ch[10 * 1024] = { 0 };
	int length = pFile->GetLength();
	pFile->Read(ch, length);
	send(m_Socket, ch, length, 0);
	pFile->Close();
	bool bFinsh = false;
	int count = 0;
	while (!bFinsh)
	{
		char chRec[1024 * 10] = { 0 };
		length = recv(m_Socket, chRec, 1024 * 10, 0);
		CFile *pFile1 = new CFile(_T("test3.txt"), 0x01002);
		pFile1->Write(chRec, length);
		pFile1->Close();
		delete pFile1;
		pFile1 = new CFile(_T("test3.txt"), CFile::modeRead);
		CArchive *pAread = new CArchive(pFile1, CArchive::load);
		//pAread->Flush();
		CSendMsg msg1;
		msg1.Serialize(*pAread);
		pAread->Close();
		pFile1->Close();
		delete pAread;
		delete pFile1;
		
		if (msg1.m_byMsgType == MSG_TYPE_TLS_SCAN_OK)
		{
			count++;
			if (count > 4)
			{
				OnBnClickedButton2();
				bFinsh = true;
			}
		}
	}
}


void CSocketTestDlg::OnBnClickedSendref()
{
	// TODO: Add your control notification handler code here
	AfxBeginThread(Threadtest, this);
	//CSendMsg msg;
	//msg.m_byMsgType = MSG_TYPE_NOPDL_SCAN;
	//msg.m_byPDLScan = true;
	//msg.m_nClientCHIndex = 2;
	//
	//CFile *pFile = new CFile(_T("test2.txt"), 0x01002);
	//CArchive *pAr = new CArchive(pFile, CArchive::store);
	//msg.Serialize(*pAr);
	//pAr->Flush();
	//pAr->Close();
	//pFile->Close();
	//delete pFile;
	//delete pAr;
	//pFile = new CFile(_T("test2.txt"), CFile::modeRead);
	//char ch[10 * 1024] = { 0 };
	//int length = pFile->GetLength();
	//pFile->Read(ch, length);
	//send(m_Socket, ch, length, 0);
	//pFile->Close();
	//bool bFinsh = false;
	//while (!bFinsh)
	//{
	//	char chRec[1024 * 10] = { 0 };
	//	length=recv(m_Socket, chRec, 1024 * 10, 0);
	//	CFile *pFile1 = new CFile(_T("test3.txt"), 0x01002);
	//	pFile1->Write(chRec, length);
	//	pFile1->Close();
	//	delete pFile1;
	//	pFile1 = new CFile(_T("test3.txt"), CFile::modeRead);
	//	CArchive *pAread = new CArchive(pFile1, CArchive::load);
	//	//pAread->Flush();
	//	CSendMsg msg1;
	//	msg1.Serialize(*pAread);
	//	pAread->Close();
	//	pFile1->Close();
	//	delete pAread;
	//	delete pFile1;
	//	bFinsh = true;
	//	
	//}
}


void CSocketTestDlg::OnBnClickedButton2()
{
	// TODO: Add your control notification handler code here
	CSendMsg msg;
	msg.m_byMsgType = MSG_TYPE_STOP_SCAN;
	msg.m_byPDLScan = true;
	msg.m_nClientCHIndex = 2;

	CFile *pFile = new CFile(_T("test2.txt"), 0x01002);
	CArchive *pAr = new CArchive(pFile, CArchive::store);
	msg.Serialize(*pAr);
	pAr->Flush();
	pAr->Close();
	pFile->Close();
	delete pFile;
	delete pAr;
	pFile = new CFile(_T("test2.txt"), CFile::modeRead);
	char ch[10 * 1024] = { 0 };
	int length = pFile->GetLength();
	pFile->Read(ch, length);	
	
	send(m_Socket, ch, length, 0);
	pFile->Close();
	bool bFinsh = false;
	while (!bFinsh)
	{
		char chRec[1024 * 10] = { 0 };
		length = recv(m_Socket, chRec, 1024 * 10, 0);
		CFile *pFile1 = new CFile(_T("test3.txt"), 0x01002);
		pFile1->Write(chRec, length);
		pFile1->Close();
		delete pFile1;
		pFile1 = new CFile(_T("test3.txt"), CFile::modeRead);
		CArchive *pAread = new CArchive(pFile1, CArchive::load);
		//pAread->Flush();
		CSendMsg msg1;
		msg1.Serialize(*pAread);
		pAread->Close();
		pFile1->Close();
		delete pAread;
		delete pFile1;
		bFinsh = true;

	}
}


void CSocketTestDlg::OnBnClickedReaddata()
{
	// TODO: Add your control notification handler code here
	CString dataFilePath = _T("\\\\zh-nas-srv4.oplink.com.cn\\DEPT\\AutoTestSoft\\interleaver\\data\\alltest.csv");


}
