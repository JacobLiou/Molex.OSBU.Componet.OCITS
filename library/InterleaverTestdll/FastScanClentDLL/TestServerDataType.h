
// Copyright @2005, Oplink communication,Inc
// The data type and the const define here
// Revision 1.0  05-17-2005

#ifndef  _SERVER_NETSERVER_DATA_TYPE_H
#define  _SERVER_NETSERVER_DATA_TYPE_H

struct tagAutoRefParam
{	
	CString strFileTitle;
	CString strFilePathName;
	int     nWorkStationIndex;//归零文件所属工位编号
//	stAutoScanParam 	ScanParam;
	BOOL	m_bReference;
	BOOL	m_bDoPDL;	
	double  m_dblAlphaValue; 
	
	DWORD	m_dwSize;
	double	m_dblStartWL, m_dblStopWL, m_dblStepSize;	// Basic parameters
	double	m_dblTLSPower, m_dblPWMPower;				// Advanced parameters

	DWORD	m_dwNumberOfScan;
	DWORD	m_dwChannelNumber;
	DWORD	m_dwChannelCfgHigh, m_dwChannelCfgLow;
	//	To be retrieved from VXIPnP driver
	DWORD	m_dwSampleCount;

};



typedef  struct  tagTestingConfigure
{
	int      m_nProductType;      // product type: EDFA, Splitter, OPSW

	DWORD    m_dwCOMPortIndex;    // COM port index:  1 or 2 

	DWORD    m_dwCOMBaudRate;     // COM port baud rate value: 9600, 19200, 38400

	DWORD    m_dwI2CBaudRate;     // I2C Baud rate:            100k, 400k

    float    m_fStandardIL;       // standard IL value

	DWORD    m_dwLanguageInfo;    // language information: Chinese or English

	DWORD    m_dwWaitTimeInms;    // wait time is ms

	BOOL     m_bHasHP8164A;
	BOOL     m_bHasHP8169A;
	BOOL     m_bHasHP8166A;
	
	BOOL     m_bHas6024E;
	BOOL     m_bHasFVA3100;
	
	DWORD    m_dwHP8164AAddress;    // HP 8164A Address
	DWORD    m_dwHP8166AAddress;    // HP 8166A Address
	DWORD    m_dwHP8169AAddress;    // HP 8169A address

	BOOL     m_bDoLoopPopupMessage;

	double   m_dblAlphaValue;
	CString m_strAlphaTime;
	// set it to 128 bytes
	// reserverd for future use
	DWORD    m_dwReserved[14];

}stTestingConfigure,*PTestingConfigure;
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
	int     m_nPowermeterPorts[32];        // 使用的功率计index
	int     m_nPowermeterCount;            // 使用的功率计数量
	int		m_ClientType;					//1--IL Scan,2--PDL Scan

}stClentTestingConfig,*ClentTestingConfig;
typedef struct tagAutoRawData
{
	// the wavelength array
	// we only use the wavelength once
	PDWORD	m_pdwWavelengthArray;  
	LPLONG	m_pnLossArray[96];    

} stAutoRawData, *PAutoRawData;  

//define a struct of get scan WL and Power Point
typedef struct tagOp816XRawData
{
	double		*m_pdblWavelengthArray;  // the array of WL
	PDWORD		m_pdwDataArrayAddr;      // the array of power point 
} stOp816XRawData, *POp816XRawData;
//定义设定扫描时激光器与功率及参数的结构体
//在原来的结构体的基础上重新定义
//将结构标准化
typedef struct tagScanParam
{                                 
	double	m_dblStartWL;            //扫描开始波长
	double  m_dblStopWL;             //扫描停止波长  
	double  m_dblStepSize;           //扫描步长                 
	int     m_nSpeed;                //扫描速度                
	double	m_dblTLSPower;           //激光器出光功率
	double  m_dblPWMPower; 		     //功率计探测功率
	DWORD	m_dwNumberOfScan;        //扫描次数
	DWORD	m_dwChannelNumber;       //扫描功率计通道
	DWORD	m_dwChannelCfgHigh;      //这里保留这两个参数，用来兼容8164采用高光或者低光进行扫描
	DWORD	m_dwChannelCfgLow;       //功率计读取通道扫描数据标志，0001--1，0010-2,0011-3  
	DWORD	m_dwSampleCount;         //扫描的点数                   
} stScanParam, *PScanParam;
struct CWorkStationSwitchCtr 
{
	BYTE   bySwitchCtrIndex;
	BYTE   bySwitchIndex;
	BYTE   bySwitchPortIndex;
	BYTE   bySwitchCtrIndexASE;
	BYTE   bySwitchIndexASE;
	BYTE   bySwitchPortIndexASE;
};
#define WORKSTATION_CONT				8	//记录当前服务器支持的工位数
#define MSG_TYPE_EMPTY					0	//空消息
#define MSG_TYPE_NEWCLIENT				1	//新客户登陆
#define MSG_TYPE_CLOSE					2	//客户退出
#define MSG_TYPE_CLIENT_REFFILE			3	//客户端请求刷新归零文件列表
#define MSG_TYPE_SERVER_REFFILE			4	//服务器端返回归零文件列表信息
#define MSG_TYPE_ERROR					5	//错误消息反馈
#define MSG_TYPE_FINSH_REFFILE			6	//归零文件信息刷新完成
#define MSG_TYPE_SERVER_CLOSE			7	//服务器退出
#define MSG_TYPE_SERVER_NOREFFILE		8	//无归零文件
#define MSG_TYPE_CLIENT_CHECK			9	//检查当前设配

#define MSG_TYPE_CLIENT_NOREG			10	//客户端未注册工位

#define MSG_TYPE_TLS_SETING				110	//激光器设定
#define MSG_TYPE_TLS_GETINFO			111	//获得激光器设置信息
#define MSG_TYPE_TLS_READ				112	//读激光器功率
#define MSG_TYPE_TLS_ATTEN				113	//单独设置激光器功率
#define MSG_TYPE_TLS_WL					114	//激光器波长
#define MSG_TYPE_TLS_POWER				115 //激光器功率
#define MSG_TYPE_TLS_PM					116	//功率计
#define MSG_TYPE_TLS_OPEN				117	//打开激光器
#define MSG_TYPE_TLS_HAS				18	//通知客户端已有的设备

#define MSG_TYPE_SCAN_REF				120 //扫描归零
#define MSG_TYPE_SCAN_ONCE				121 //扫描一次
#define	MSG_TYPE_SCAN_ALPHA				122 //请求归零扫描角度
#define MSG_TYPE_SCAN_DATA				22	//扫描数据 
#define MSG_TYPE_SCAN_BEGIN				123	//开始扫描

#define MSG_TYPE_SCAN_PROGRESS			24	//扫描进度
#define MSG_TYPE_SCAN_FINISH			25	//完成扫描
#define MSG_TYPE_REFERENCE_BEGIN		26	//归零开始
#define MSG_TYPE_SCAN_REFDATA			27	//归零扫描数据
#define MSG_TYPE_REFERENCE_DELETEFILE	28	//删除归零文件

#define MSG_TYPE_REFERENCE_NEXTCH		29	//多通道归零切换通道消息
#define MSG_TYPE_REFERENCE_NEXTCHOK		30	//多通道扫描切换通道的应答消息，切换完成
#define MSG_TYPE_REFERENCE_NEXTCHERROR	32	//取消归零，或者通道错误
#define MSG_TYPE_CLIENT_ALPHA			31	//回复角度值
#define MSG_TYPE_TLS_SCAN_FAIL			51  //激光器扫描失败
#define	MSG_TYPE_TLS_SCAN_OK			50  //激光器扫描完成
#define MSG_TYPE_PDL_SCAN				64  //带PDL扫描消息
#define MSG_TYPE_NOPDL_SCAN			    65  //不带PDL扫描消息
#define MSG_TYPE_STOP_SCAN				11  //当前工位在线但是不扫描
#define MSG_TYPE_DEVICE_OPEN_OK			81  //服务器设备打开成功。
#define MSG_TYPE_ALPHA_OK				71  //偏振角度归零
#define MSG_TYPE_REFERENCE_OK           73  //归零扫描完成


#define MSG_TYPE_TLS_TASK				100 //激光器相关任务

#define SCAN_TYPE_OPEN					60	//打开激光器
#define SCAN_TYPE_REF					61	//归零
#define SCAN_TYPE_SCAN					62	//光学测试
#define SCAN_TYPE_ALPHA					63	//扫描角度
#define MSG_TYPE_HREAT_THROB			33	//心跳信号
#define MSG_TYPE_ASE			44	//切换到ASE

#define MSG_TYPE_SWITCH_NEXTCH			34	//开关测试的通道切换消息
#define MSG_TYPE_SWITCH_NEXTCHOK		35	//光开关测试通道切换应答消息
#define MSG_TYPE_SWITCH_NEXTCHERROR		36	//光开关测试通道切换取消或错误消息
#define MSG_TYPE_SWITCH_SCAN_ONCE		124	//光开关测试扫描

#define SCAN_TYPE_SWITCH_SCAN			64	//光开关光学扫描
#define MSG_TYPE_TLS_SETTING_OK			175	//激光器设置成功
#define MSG_TYPE_PDL_COMPLETE			80	//激光器设置成功

#define MSG_TYPE_OSA_SCAN_ONCE			125	//OSA 扫描消息
#define MSG_TYPE_OSA_SCAN_OK			37	//OSA 扫描完成
#define SCAN_TYPE_OSA_SCAN				65	//OSA 扫描

#define	MSG_TYPE_SINGLE_TEST			126	//点测消息
#define MSG_TYPE_SINGLE_PROCESS			66	//点测测试一次
#define MSG_TYPE_SINGLE_LOCK_READ		67	//点测 占用激光器  激光器锁定后不停向客户端发送功率数据（用于从测试PDL）
#define	MSG_TYPE_SINGLE_LOCK_NOREAD		68	//点测 占用激光器后不发送数据，等待解锁消息
#define	MSG_TYPE_SINGLE_UNLOCK			38	//点测 解锁消息 基础激光器的锁定
#define MSG_TYPE_SINGLE_TESTOK			39 


#define MAX_LINE  256
#define CLIENT_CH_COUNT 1
#define  MAX_CHANNEL_COUNT 96 
#define WSSMODULECOUNT  4
#define  LIGHTSPEED 299792458.458     //光速，用来将计算ITU频率或者波长
#define MULTY_DATA				1000
#define IDP_SOCKET_FAILED 101
#endif
