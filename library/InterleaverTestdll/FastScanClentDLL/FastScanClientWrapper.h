#pragma once
#include "FastScanDLL.h"

extern "C" _declspec(dllexport) int __stdcall ConnectServer(stClentTestingConfig  m_testinfo);
extern "C" _declspec(dllexport) bool __stdcall TLSScan(bool bDoPDL,bool bDoRef,int nPort, char* strfilefullname);
extern "C" _declspec(dllexport) int __stdcall TLSScanFSTP(bool bDoPDL, bool bDoRef, double dWLStart, double dWLStop, double dStep, char* strfilefullname);
extern "C" _declspec(dllexport) char* __stdcall GetMsg(int nPort);
extern "C" _declspec(dllexport) void __stdcall Release();
