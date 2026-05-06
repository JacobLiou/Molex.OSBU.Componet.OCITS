
#include "TestServerDataType.h"

#ifndef FastScanClentDLL_EXPORTS
#define FastScanClentDLL_API __declspec(dllexport)
#else
#define FastScanClentDLL_API __declspec(dllimport)
#endif
class CFSTPClient;
class CClientSocket;
class FastScanClentDLL_API CFastScanClentDLL 
{
public:

	CFastScanClentDLL(void);
	 ~CFastScanClentDLL();
	// TODO: add your methods here.
	 BOOL m_bFSTP;
  CClientSocket* m_pClient;
  CFSTPClient* m_pFSTPClient;
  
  CString GetMsg();   //获得客户端与服务器之间交互的信息，一般用于获得异常情况的提示
  BOOL TLSScan(BOOL bDoPDL,BOOL bDoRef,int nPort,CString strfilefullname);//归零的时候不需要传入文件名，测试的时候传入全路径带名称
  BOOL ConnectServer(stClentTestingConfig  m_testinfo);    //传入连接结构体，结构体参数如下
  void CloseSocket();
  /*
  typedef  struct  tagClentTestingConfig
  {
  TCHAR   m_tszServerIP[64];             // 服务器IP地址
  int     m_nPort;                       // 服务器网络连接端口   
  TCHAR   m_tszClientIP[64];             // 客户端IP地址
  TCHAR   m_tszClientName[64];           // 客户端用户名（电脑名）
  int     m_nClientPortIndex;            // 连接服务器的物理端口（光纤到客户端对于服务器开关的实际端口）
  TCHAR   m_tszServerDatapath[256];      // 服务器临时数据存放路径
  TCHAR   m_tszClentDatapath[256];       // 客户端保持数据存放路径
  TCHAR   m_tszClentRefDatapath[256];    // 客户端归零数据存放路径
  int     m_nClientTestPort;             // 客户端自身测试端口号，存在文件名中，便于归零数据命名的存储和调用，服务器无实际对应关系
  
	}stClentTestingConfig,*ClentTestingConfig;
  */
		

};


