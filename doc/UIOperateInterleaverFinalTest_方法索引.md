# UIOperateInterleaverFinalTest — 方法索引

本文档为 [UIOperateInterleaverFinalTest_代码说明.md](./UIOperateInterleaverFinalTest_代码说明.md) 的附录：**按源文件列出公开/内部方法及约略行号**，便于检索。行号以仓库当前版本为准。

## OperateInteleaverFinalTest.xaml.cs（partial `OperateInteleaverFinalTest`）

| 方法 | 行号 | 简述 |
|------|------|------|
| `OperateInteleaverFinalTest()` 构造 | 333 | 初始化集合、BackgroundWorker、端口数据缓冲、读 INI 默认 URL/路径 |
| `GetMessage` | 394 | 静态占位，始终返回 `true`（未接 UDL 错误解析） |
| `RefTimeCheck_DoWork` | 408 | 归零倒计时后台：每秒 ReportProgress |
| `RefTimeCheck_Progress` | 423 | 更新 `txtRefTime`；超期删归零文件、清缓冲、更新列表归零状态 |
| `BakeTimeCheck_DoWork` | 455 | 烤温：`e.Argument`（一般为 `TmptChangeTimes*60` 秒）再 `×1000` 为毫秒与 `TickCount` 比较；到点设 `BakeComplete` 并 ReportProgress(0) |
| `BakeTimeCheck_Progress` | 480 | 更新烤温 UI；完成则 `DoScanOnBK` |
| `Compose` | 497 | MEF 从 `module` 目录 `ComposeParts` 本控件 |
| `InitRegerster` | 507 | 订阅 `EventMainInit` → `Init` |
| `GetExeDir` | 518 | 取主程序目录 |
| `Init` | 534 | `MainInitInfo`：MESLESS 校验、建 `InterleaverFinalTestCurve`/`ParamCal`、设备失败提示、读 FSTP 功率计个数、启动归零计时线程 |
| `PassOrFail_Load` | 580 | 加载通过/失败图尺寸 |
| `InitPassFailImage` | 594 | 从磁盘读 Pass/Fail ico 到 `BitmapImage` |
| `UserControl_Loaded` | 629 | `Compose` + `InitRegerster` + `SelectedItemChangeRegister` |
| `WarningBox` / `ErrorBox` | 640 / 649 | MessageBox 封装 |
| `btnOpenTemplate_Click` | 654 | 金样校验、SN/工位/多产品数量校验、后台打开模板 |
| `OpenTemplateBK_DoWork` | 707 | `FusionControl.OpenTemplate`，同 Spec 追加 `allProductControl` |
| `ClearListData` | 743 | 空 `FusionControl` 发布 `EventTemplateUpdate` |
| `IsAllPass` | 755 | 所有产品 `GetAllTestedPassed` |
| `UpdateResIcon` | 773 | 更新 `passOrFailImg` |
| `ParamItemUpdate` | 788 | 打开模板后或测试后同步 `testShowControl`、删隐藏行、`EventTemplateUpdate`/`UpdateItem` |
| `OpenTemplateBK_RunWorkerCompleted` | 901 | 首产品打开注意事项 HTML、解析 CFG/端口/曲线、`ReadRefData`、`ParamItemUpdate` |
| `ShowTmpltPath` | 1160 | `EventXml` 通知主窗模板路径 |
| `GetScanIndex` | 1179 | 端口在 `_scanList` 中的组号 |
| `IsContainPortAssist` | 1191 | 端口助手是否已存在 |
| `ParserRange` | 1210 | `左~右` 频率解析 |
| `RealtimeMsg` | 1225 | 发布 `EventRealTimeStatus` |
| `btnScanRef_Click` | 1241 | 置 `referenceIndex` 调 `ScanRef` |
| `ScanRef` | 1249 | 逐端口确认后 `SetSwitch` + `BackgroundWorker` 归零扫描 |
| `SetSwitch` | 1301 | `IOpticalSwitch.SetSwitch` 光路 flag |
| `ReadRefData` | 1333 | 读 PDL 归零 CSV 到 `portPDLRef`，校验时间与 Spec |
| `IsRefTimePassdue` | 1417 | 6h / 6.5h 归零过期策略 |
| `AddStrToList` | 1425 | 去重追加 `savePathList` |
| `ScanAndCalResultFSTP` | 1437 | FSTP 单文件扫描路径：归零/测试读 CSV、`CalFSTPRawdata` |
| `ScanAndCalResult` | 1537 | 四偏振 CSV + `CalRawdataByAve/MaxMin/Mueller` 分支 |
| `DoScan` | 1664 | 当前固定走 `IUDLFSTP`（GUID 2）`Scan`；否则旧 `IInterleaverScan` 分支（`isFSTP` 恒 true 时不可达） |
| `DoScanOnBK` | 1760 | 新建 `BackgroundWorker` 执行 `Scan_DoWork` |
| `Scan_DoWork` | 1780 | `ScanAndCalResultFSTP` 或 `ScanAndCalResult`，成功后 `CalAllResultInThread` |
| `FindAdjPortIndex` | 1838 | 同组通道相邻端口 index |
| `CalResByPort` | 1877 | 按端口算子通道参数，`ParamCal.CalChannelTestParam`，写 `FusionControl` |
| `GetPortIndexByName` | 1959 | 从 `portAssistant` 查物理端口 index |
| `CalBPParamRes` | 1969 | `_BP` 前后工序差分：另载 `FusionControl` 历史数据 |
| `CalPortRes` | 2053 | 聚合 `MAXIL` 到 `SamePortParamData` 后算总端口项 `CalPortParam` |
| `ChannelCalThread` | 2127 | 单端口线程入口 |
| `CalAllResultInThread` | 2140 | 多线程 `CalResByPort` 后 `CalPortRes` + `CalBPParamRes` |
| `SetCalFinished` / `IsAllCalFinished` | 2161 / 2172 | 线程完成标志 |
| `AddResultToRecord` | 2190 | 同步写 `SamePortParamData` 列表 |
| `ClearResult` | 2235 | 扫描失败时清空相关测试项显示 |
| `Scan_RunWorkerCompleted` | 2335 | 提示扫描结束，调 `ScanFinish` |
| `ScanFinish` | 2360 | 曲线、raw 文件、`ParamItemUpdate`、继续 `ScanRef`/`OnekeyScan` |
| `UpdateReferenceStatus` | 2493 | 列表归零图标 |
| `UpdateItem` | 2513 | 发布 `EventListItemUpdate` |
| `ReconnectServer` | 2534 | `IInterleaverScan.Reconnect`（保留接口） |
| `SetOpenTemplateComplete` / `GetOpenTemplateComplete` | 2551 / 2558 | 模板打开完成标志 |
| `SetIsScanFinished` / `GetIsScanFinished` | 2565 / 2571 | 扫描互斥标志 |
| `btnClearBakeSN_Click` | 2577 | 清空产品与列表 |
| `btnOnekeyScan_Click` | 2597 | 调 `OnekeyScan` |
| `OnekeyScan` | 2613 | 选未测端口组、烤温、TCC、`TestWithPDLOnekey` 链 |
| `GetUDLMessage` / `IsUDLSuccess` | 2815 / 2820 | UDL 消息占位 / Dispatcher 包装 |
| `IsScanRef` | 2832 | 检查同温下是否均已归零（**未使用传入的 `scanPorts`**） |
| `UserControl_Unloaded` | 2849 | `refTimeCheckBK.CancelAsync` |
| `btnSingleScan_Click` | 2854 | 按列表选中项组端口扫描，`TestWithPDL` |
| `ReleaseCom` | 2990 | COM 引用释放 |
| `SelectedItemChangeRegister` / `SelectedItemUpdate` | 3004 / 3015 | 订阅 `EventListSelectChanged` |
| `btnSaveToAMTS_Click` | 3029 | 拷 rawdata、写 XML、`UploadTestData`、清状态、TCC 回 25℃ |
| `UserControl_PreviewKeyDown` | 3110 | 空 |
| `OperateInteleaverFinalTest_PreviewKeyDown` | 3115 | SN 框回车触发打开模板 |

## InterleaverFinalTestCurve.cs

| 方法 | 行号 | 简述 |
|------|------|------|
| `InterleaverFinalTestCurve` | 31 | 注入 `IEventAggregator`，初始化颜色表 |
| `InitAllCurve` | 47 | 按通道名发布多条曲线 Init |
| `ClearAllCurve` | 79 | 清空各 series 点 |
| `UpdateCurveShow` | 95 | `EventCurveUpdate` 全量更新点 |
| `UpdateFre` | 118 | 改横轴范围并重 init |
| `InitCurve` | 139 | 私有：发布 `CurveUpdate.Init` |

## InterleaverScanResult.cs

| 方法 | 行号 | 简述 |
|------|------|------|
| `ReadRefTime` | 25 | 归零文件首行时间 |
| `ReadRefPortCount` | 65 | 归零文件端口数 |
| `ReadRefSpec` | 104 | 归零文件 Spec |
| `CheckRefRight` | 137 | 无 PDL 归零光强检查 |
| `ReadScanData` | 173 | CSV → `double[][]` |
| `WritePDLRefData` | 214 | 写带 PDL 归零 CSV |
| `CalFSTPRawdata` | 244 | FSTP 结果相对归零 ave 扣减 |
| `WriteCalData` | 301 | 写计算后六列 CSV |
| `WriteFusionCalData` | 331 | 带头信息（操作员/工位/设备）的融合 CSV |
| `WriteFusionData` | 369 | 简化头 + 数据行 |
| `CalRawdataByNoPDL` | 411 | 单偏振无 PDL 扣减 |
| `CalPDLRefData` | 460 | 四偏振合成归零 `portPDLRef`（7 行） |
| `CalRawdataByAve` | 518 | 四偏振平均法 IL/PDL |
| `CalRawdataByMaxMin` | 609 | 四偏振最大最小法 |
| `CalRawdataByMueller` | 700 | Mueller 矩阵 PDL；弱光回退 max-min |
| `InitRawdataBuffer` | 824 | 清零或按点数重分配 |
| `ParserRawdata` | 851 | 私有：解析 CSV 到缓冲 |

## ParamCal.cs

| 方法 | 行号 | 简述 |
|------|------|------|
| `ParamCal` 构造 | 13 | 保存 `IInterleaverAlgorithm` |
| `CalChannelTestParam` | 24 | 子通道：按 `param` 关键字调 algorithm |
| `CalPortParam` | 374 | 总端口：多温度聚合、TDL/FSR/BW 等 |
| `GetRecordResultByParamName` | 631 | 从 `SamePortParamData` 取结果列表 |

## 同文件内其它类型（ParamCal.cs）

| 类型 | 行号 | 说明 |
|------|------|------|
| `SamePortParamData` | 659 | 端口+温度+参数名+多频点结果 |
| `SCANTYPE` | 687 | 扫描类型枚举 |
| `ScanDetail` | 696 | 扫描参数：类型、端口列表、产品 index |

## UIVariable / TestProductInfo / PortAssist（OperateInteleaverFinalTest.xaml.cs 尾部）

| 类型 | 行号 | 说明 |
|------|------|------|
| `UIVariable` | 3130 | `INotifyPropertyChanged`：SN/PN/Spec、按钮 Enable、清空列表可见性 |
| `TestProductInfo` | 3247 | 列表行：Index + SN |
| `PortAssist` | 3253 | 端口测试辅助：名称、物理口、PM、扫描组、烤温时间、归零/已测标志、raw 路径等 |
