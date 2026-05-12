# UIOperateInterleaverFinalTest 工程代码逻辑全解

## 1. 工程定位与作用

`UIOperateInterleaverFinalTest` 是一个基于 WPF 的插件式测试界面模块，主要用于 Interleaver Final Test 场景，负责以下核心职责：

1. 从 AMTS 系统打开模板并加载测试项。
2. 驱动设备进行系统归零（Reference）与测试扫描（Scan）。
3. 对扫描原始数据进行 IL/PDL 等转换与参数计算。
4. 将测试结果展示到 UI（列表、曲线、通过/失败图标、计时状态）。
5. 将结果（含 rawdata）回传 AMTS。

该工程本身是逻辑编排层，算法实现与设备通信主要通过外部依赖接口完成（例如 `IInterleaverAlgorithm`、`IInterleaverScan`、`IDeviceHandle`）。

---

## 2. 工程结构与文件职责

## 2.1 代码文件

1. `InterleaverFinalTestCurve.cs`
- 曲线显示适配层。
- 通过 `EventAggregator` 发布 `EventCurveUpdate` 事件给曲线显示系统。

2. `InterleaverScanResult.cs`
- 扫描 CSV 文件读取与解析。
- 归零数据生成与测试数据转换（Ave、MaxMin、Mueller）。
- 结果数据写回 CSV。

3. `ParamCal.cs`
- 参数计算分发层。
- 将参数名映射到 `IInterleaverAlgorithm` 的对应算法方法。
- 支持通道级参数和端口汇总参数。

4. `OperateInteleaverFinalTest.xaml`
- UI 布局定义（SN/PN/Spec、按钮、测试列表、结果图标、烤温倒计时）。

5. `OperateInteleaverFinalTest.xaml.cs`
- 主控制器（业务核心）。
- 包含模板加载、归零、测试、线程调度、结果刷新、上传逻辑。

6. `Properties/AssemblyInfo.cs`
- 组件元信息（程序集信息、版本等）。

## 2.2 工程文件

`UIOperateInterleaverFinalTest.csproj` 关键点：

1. 类型：Class Library。
2. 框架：.NET Framework 4.0。
3. 平台目标：x86。
4. 输出目录：`bin/debug/module` 与 `bin/release/module`。
5. 关键依赖：
- `MenuPluginInterface.dll`
- `MolexUtility.dll`
- `ProtocolAggregator.dll`
- `Microsoft.Office.Interop.Excel`

---

## 3. 外部依赖与边界

本工程对外部库只做接口使用，不实现底层逻辑。

1. `IEventAggregator`
- 用途：模块间事件发布与订阅。
- 典型事件：`EventMainInit`、`EventTemplateUpdate`、`EventCurveUpdate`、`EventRealTimeStatus`、`EventListItemUpdate`、`EventListSelectChanged`。

2. `IDeviceHandle`
- 用途：获取扫描设备与光开关设备对象。

3. `IInterleaverScan`
- 用途：执行硬件扫描（归零模式/测试模式）和重连。

4. `IOpticalSwitch`
- 用途：切换产品/端口光路。

5. `IInterleaverAlgorithm`
- 用途：执行参数算法（MAXIL、MINIL、PDL、Ripple、Shift、Adj、BW、TDL 等）。

6. `MESControl` 及相关类型
- 用途：模板加载、测试项管理、判定与 AMTS 上传。

---

## 4. 核心数据模型

## 4.1 枚举

1. `ConvertAlgorithm`（位于主类内部）
- `Ave` => `PZ-Averagevalue`
- `MaxMin` => `PZ-MAX`
- `Mueller` => `Muellermatrix`
- 含义：扫描后 4 偏振态数据转换策略。

2. `BakeStatus`
- `UnBake`、`Baking`、`BakeComplete`
- 含义：烤温流程状态。

3. `SCANTYPE`（位于 `ParamCal.cs`）
- `RefWithNoPDL`
- `RefWithPDL`
- `TestWithNoPDL`
- `TestWithPDL`
- `TestWithPDLOnekey`
- 含义：扫描模式/场景标识。

## 4.2 数据类

1. `UIVariable`（INotifyPropertyChanged）
- 用途：WPF 绑定模型。
- 主要属性：`SN`、`PN`、`Spec`、`IsReferenceEnable`、`IsScanEnable`、`IsSaveEnable`、`IsClearSNVisiable`。

2. `TestProductInfo`
- 用途：左侧测试列表项。
- 属性：`SN`、`Index`。

3. `PortAssist`
- 用途：端口配置/状态总表（运行时最关键对象之一）。
- 属性：
  - 标识：`Name`、`Port`、`ProductIndex`、`PortIndex`、`OperateIndex`
  - 设备：`PMIndex`、`ScanIndex`
  - 状态：`IsRef`、`IsTested`
  - 温度：`TestTmpt`、`TmptChangeTimes`
  - 上传：`Rawdata`

4. `SamePortParamData`
- 用途：缓存同端口同参数在不同频点下的计算结果，用于后续端口级汇总参数。
- 属性：`ParamName`、`Tempreture`、`Port`、`Results`。

5. `ScanDetail`
- 用途：每次扫描任务输入。
- 字段：`ScanType`、`Ports`、`ProductIndex`。

---

## 5. 主类 OperateInteleaverFinalTest 全量逻辑

## 5.1 类职责总览

`OperateInteleaverFinalTest` 是整个工程的编排中心，负责：

1. 生命周期：加载时 MEF 组装，初始化事件订阅。
2. 模板流：打开模板、解析参数、建立端口映射。
3. 归零流：按端口执行参考扫描并保存。
4. 测试流：单项/一键测试、曲线显示、参数刷新。
5. 定时流：归零有效期计时、烤温倒计时。
6. 数据流：rawdata 组装与 AMTS 上传。
7. 并发流：后台扫描 + 多线程参数计算。

## 5.2 关键字段说明

1. 路径与服务
- `amtsUrl`
- `amtsSaveUrl`
- `refWithPDLFile`
- `scanWithPDLFile`

2. 状态控制
- `isOpenTemplateComplete`
- `isScanFinished`
- `curTestTmpt`
- `curBakeStatus`
- `oldestRefTime`

3. 数据缓存
- `portPDLRef`：每端口归零数据（7 行）
- `portResData`：每端口测试结果（6 行）
- `pdlRawData`：4 个偏振态原始数据（每项 3 行）
- `curPortRecords`：中间结果缓存，用于端口汇总参数

4. 映射关系
- `portAndNameDic`：端口名映射
- `portAndPMDic`：端口到功率计
- `_scanList`：可并行扫描分组
- `portAssistant`：运行时端口状态总清单

## 5.3 生命周期与初始化方法

1. `OperateInteleaverFinalTest()`
- 职责：构造函数。
- 输入：无。
- 输出：对象初始化完成。
- 核心动作：
  - 初始化 UI 绑定对象。
  - 读取 XML 配置中的 AMTS 地址。
  - 初始化默认端口-功率计映射。
  - 初始化 `BackgroundWorker`（归零计时、烤温计时）。
  - 预分配 `portPDLRef`、`portResData`、`pdlRawData` 的数组结构。

2. `UserControl_Loaded(...)`
- 职责：控件加载入口。
- 核心动作：`Compose()`、`InitRegerster()`、`SelectedItemChangeRegister()`。

3. `Compose()`
- 职责：MEF 装配。
- 核心动作：从 `Environment.CurrentDirectory + \module` 目录加载组件。

4. `InitRegerster()`
- 职责：订阅 `EventMainInit`。
- 核心动作：收到主程序初始化信息后调用 `Init(MainInitInfo)`。

5. `Init(MainInitInfo info)`
- 职责：业务初始化入口。
- 输入：主程序上下文（工位、模板类型、工序等）。
- 核心动作：
  - 解析 `testProcess` 和 `templateType`。
  - 初始化 `curveShow` 与 `paramCal`。
  - 拼接扫描与参考文件绝对路径。
  - 从设备查询 `scanPowermeterCount`。
  - 启动归零计时后台线程 `refTimeCheckBK`。

## 5.4 定时器方法

1. `RefTimeCheck_DoWork(...)`
- 每秒上报一次进度。

2. `RefTimeCheck_Progress(...)`
- 计算当前距最老归零时间的间隔，更新 UI。
- 超时后删除参考文件、清空参考内存、重置 `IsRef`。

3. `BakeTimeCheck_DoWork(...)`
- 按目标秒数倒计时。

4. `BakeTimeCheck_Progress(...)`
- UI 展示剩余时间。
- 倒计时结束自动触发 `DoScanOnBK()`。

## 5.5 模板加载相关方法

1. `btnOpenTemplate_Click(...)`
- 职责：打开模板按钮入口。
- 核心动作：
  - 首次点击时从 `sn.csv` 读取 SN 列表（Excel Interop）。
  - 校验 SN、主程序初始化状态、可测试产品数量上限。
  - 清理状态后启动后台打开模板任务。

2. `OpenTemplateBK_DoWork(...)`
- 职责：后台打开模板。
- 核心动作：
  - 检查 SN 是否重复。
  - 调用 `MESControl.OpenTemplate(...)`。
  - 校验多产品时 Spec 一致性。

3. `OpenTemplateBK_RunWorkerCompleted(...)`
- 职责：模板加载完成后的主处理。
- 核心动作：
  - 更新产品列表与 UI。
  - 解析模板项：频段范围、配置串、算法模式、带宽等。
  - 建立 `portAndNameDic`、`portAssistant`、扫描分组 `_scanList`。
  - 初始化曲线并调整频率窗口。
  - 调用 `ReadRefData(...)` 尝试读取已有参考数据。
  - 调用 `ParamItemUpdate(..., true)` 初始化显示。
  - 自动触发 `btnOnekeyScan_Click(...)`。

4. `ParserRange(string source, ref double leftFre, ref double rightFre)`
- 职责：解析模板中 LFRANGE/MFRANGE/HFRANGE 字符串。
- 输出：扫描范围、`productFre`、`passBand`、`convertAlgorithm`。

5. `GetScanIndex(int port)`
- 根据 `_scanList` 返回端口所在扫描分组号。

6. `IsContainPortAssist(...)`
- 判断是否已有同产品同端口名同温度的 `PortAssist`。

## 5.6 归零相关方法

1. `btnScanRef_Click(...)`
- 职责：系统归零入口。
- 动作：重置 `referenceIndex` 后调用 `ScanRef()`。

2. `ScanRef()`
- 职责：逐端口归零调度。
- 核心动作：
  - 跳过非首温度端口（只对一个温度归零）。
  - 弹框提示接线。
  - 切换开关 `SetSwitch(...)`。
  - 构造 `scanDetailInfo` 为 `RefWithPDL`，后台执行扫描。

3. `SetSwitch(int productIndex, string portName)`
- 职责：光开关切换。
- 格式：`productIndex::PORTX:portCount`。

4. `ReadRefData(int productIndex, List<PortAssist> assists, ref string errMsg)`
- 职责：从参考文件读取并验证。
- 校验内容：
  - 文件存在与可读。
  - 归零端口数与模板一致。
  - 归零时间是否超期。
  - 频率覆盖是否满足当前模板扫描范围。
- 成功后写入 `portPDLRef` 并更新 `assist.IsRef`。

5. `IsRefTimePassdue(TimeSpan refSpan)`
- 判定规则：
  - 模板未完全打开前：6 小时。
  - 模板打开后：6.5 小时。

6. `UpdateReferenceStatus(int productID, PortAssist assist)`
- 同步 UI 列表中端口的归零状态。

## 5.7 扫描与计算方法

1. `DoScanOnBK()`
- 职责：创建扫描后台任务。

2. `Scan_DoWork(...)`
- 职责：后台扫描执行体。
- 当前代码行为：
  - `ScanAndCalResult(...)` 调用被注释（重要实现偏差点）。
  - 直接设置 `isScanFinished` 并在测试场景触发 `CalAllResultInThread()`。
- 说明：在当前版本里，一键测试通过 Excel 数据先填充 `portResData`，因此即使注释掉扫描调用，仍会继续参数计算流程。

3. `ScanAndCalResult(ScanDetail scanInfo, ref string errMsg)`
- 职责：完整扫描并转换数据（该方法包含完整硬件扫描路径）。
- 核心步骤：
  - 按扫描类型清空对应缓存。
  - 调用 `DoScan(...)` 执行设备扫描。
  - `RefWithPDL`：读取 4 偏振态数据，生成参考数据并写文件。
  - `TestWithPDL/TestWithPDLOnekey`：读取 4 偏振态并按 `convertAlgorithm` 转换到 `portResData`。

4. `DoScan(ScanDetail scanInfo, ref string resPath, ref string errMsg)`
- 职责：底层扫描调用封装。
- 关键分支：
  - `RefWithPDL` => `scan.Scan(true, true, ...)`
  - `TestWithPDL/TestWithPDLOnekey` => `scan.Scan(true, false, ...)`

5. `Scan_RunWorkerCompleted(...)`
- 职责：扫描结束统一回调。
- 成功时调用 `ScanFinish(...)`。

6. `ScanFinish(ScanDetail scanInfo)`
- 职责：扫描后统一后处理。
- 核心动作：
  - 选择要显示的数据源（参考或测试）。
  - 生成上传用 `Rawdata` 字符串（VOLT-1DB/2DB/3DB/4DB）。
  - 更新曲线显示。
  - 根据扫描类型执行后续动作：
    - 归零：更新 `IsRef`，继续下一个归零端口。
    - 单项测试：刷新参数显示并恢复按钮。
    - 一键测试：刷新后递归进入下一轮 `OnekeyScan()`。

7. `ReconnectServer(ref string errMsg)`
- 职责：扫描超时后的设备重连。

## 5.8 参数计算方法

1. `CalAllResultInThread()`
- 职责：多端口并发参数计算总控。
- 核心动作：
  - 每个待计算端口启动一个线程执行 `ChannelCalThread`。
  - `while + Sleep(100)` 等待全部结束。
  - 结束后执行端口汇总参数 `CalPortRes(...)`。

2. `ChannelCalThread(object param)`
- 职责：单线程端口计算包装。

3. `CalResByPort(int calPort, ref string errMsg)`
- 职责：计算某个端口下所有子通道参数。
- 核心动作：
  - 遍历模板项，筛选当前温度和当前端口。
  - 根据参数名调用 `ParamCal.CalChannelTestParam(...)`。
  - 对非 `MAXIL` 结果写入 `curPortRecords`。
  - 回写 `allProductControl`。

4. `CalPortRes(List<int> calPorts, ref string errMsg)`
- 职责：计算端口级汇总参数（端口总项）。
- 核心动作：
  - 先将 `MAXIL` 数据补入 `curPortRecords`。
  - 对端口总项调用 `ParamCal.CalPortParam(...)`。

5. `AddResultToRecord(...)`
- 职责：线程安全写入中间记录缓存。

6. `SetCalFinished(...)` / `IsAllCalFinished()`
- 职责：线程结束状态管理（同步方法）。

## 5.9 一键测试与单项测试

1. `btnOnekeyScan_Click(...)`
- 校验模板与端口状态后调用 `OnekeyScan()`。

2. `OnekeyScan()`
- 职责：自动化测试调度主函数。
- 当前实现特点：
  - 选择下一组未测试端口（按产品、扫描分组、温度）。
  - 从 `15275/{SN}.xlsx` 读取数据并填充 `portResData`。
  - 设定 `scanDetailInfo = TestWithPDLOnekey`，触发后台处理。
  - 全部完成后自动调用 `btnSaveToAMTS_Click(...)`。
- 注意：当前代码中存在大段硬编码列位映射（常温/低温/高温不同列段）。

3. `btnSingleScan_Click(...)`
- 职责：对当前选中测试项执行单项测试。
- 核心动作：
  - 从 `selectItem` 获取目标端口与温度。
  - 校验归零状态 `IsScanRef(...)`。
  - 切换开关，判定是否需要烤温。
  - 进入 `DoScanOnBK()` 或先倒计时再扫描。

4. `IsScanRef(List<int> scanPorts, ref string errMsg)`
- 职责：检查是否完成参考归零。
- 当前逻辑：只检查首温度对应端口的 `IsRef`。

## 5.10 UI 与交互方法

1. `PassOrFail_Load(...)`、`InitPassFailImage()`
- 职责：加载并显示通过/失败图标。

2. `UpdateResIcon()`
- 职责：根据当前测试项是否全通过决定图标。

3. `ParamItemUpdate(int productID, bool isOpenTemplate = false)`
- 职责：刷新 UI 测试项展示。
- 打开模板时：筛选显示行、发布模板更新事件、刷新参考状态。
- 普通刷新时：把 `allProductControl` 的最新值映射到显示模型。

4. `UpdateItem(...)`
- 职责：发布单行更新事件到 UI 列表。

5. `ClearListData()`
- 职责：清空模板显示区域。

6. `btnClearBakeSN_Click(...)`
- 职责：清空列表和当前状态。

7. `SelectedItemChangeRegister()` / `SelectedItemUpdate(...)`
- 职责：接收并记录外部列表选中项变化。

8. `OperateInteleaverFinalTest_PreviewKeyDown(...)`
- 职责：在 SN 输入框按 Enter 时触发打开模板。

9. `UserControl_Unloaded(...)`
- 职责：退出时取消归零计时线程。

## 5.11 上传方法

1. `btnSaveToAMTS_Click(...)`
- 职责：上传测试数据到 AMTS。
- 核心动作：
  - 按产品收集 `PortAssist.Rawdata` 形成 `AMTSRawdata` 列表。
  - 调用 `SaveDataToAMTS(...)`。
  - 成功后清空界面并自动加载下一个 SN。

---

## 6. InterleaverFinalTestCurve 类详解

## 6.1 类作用

负责曲线模块的初始化、清空、更新与频率窗口调整；本类不直接绘图，采用事件驱动方式。

## 6.2 字段与属性

1. `EventAggregator`：事件总线。
2. `entireArea`：目标图区域名。
3. `entireFreLeft` / `entireFreRight`：当前显示频率边界。
4. `seriesNames`：曲线序列名称缓存。
5. `lineColors`：预置颜色数组。

## 6.3 方法逐项

1. `InterleaverFinalTestCurve(IEventAggregator aggregator)`
- 初始化事件总线与颜色。

2. `InitAllCurve(string[] curveNames, bool isFreChanged = false)`
- 比较新旧曲线名，必要时重建所有曲线。

3. `ClearAllCurve()`
- 通过空点集更新每条曲线，达到清空效果。

4. `UpdateCurveShow(string serName, List<double> xValues, List<double> yValues)`
- 发布 `CurveUpdate.AllPoint` 事件更新整条曲线数据。

5. `UpdateFre(double left, double right)`
- 更新显示频率区间，并触发曲线重初始化。

6. `InitCurve(...)`
- 私有方法，构造并发布 `CurveUpdate.Init` 事件。

---

## 7. InterleaverScanResult 类详解

## 7.1 类作用

静态工具类，负责文件读写、原始数据解析、参考数据生成、测试数据转换。

## 7.2 数据格式约定

1. 参考数据 `double[7][]`
- `0: WL`
- `1: Ave`
- `2: PDL1`
- `3: PDL2`
- `4: PDL3`
- `5: PDL4`
- `6: FRE`

2. 测试结果数据 `double[6][]`
- `0: WL`
- `1: AVG`
- `2: PDL`
- `3: MAX`
- `4: MIN`
- `5: FRE`

3. 原始偏振数据 `double[3][]`
- `0: WL`
- `1: IL`
- `2: FRE`

## 7.3 方法逐项

1. `ReadRefTime(...)`
- 从参考 CSV 首行读取时间戳。

2. `ReadRefPortCount(...)`
- 从参考 CSV 首行读取端口数。

3. `ReadRefSpec(...)`
- 从参考 CSV 首行读取 Spec。

4. `CheckRefRight(...)`
- 参考功率阈值检查，低于 -25dB 判失败并清空。

5. `ReadScanData(...)`
- 读取扫描 CSV，统计点数后分配缓冲，再调用 `ParserRawdata`。

6. `WritePDLRefData(...)`
- 写归零文件（含时间、Spec、端口数量元信息）。

7. `WriteCalData(...)`
- 写测试转换后的结果文件。

8. `CalRawdataByNoPDL(...)`
- 无 PDL 模式下的 IL 计算。

9. `CalPDLRefData(...)`
- 对 4 偏振态计算归零平均数据。
- 含 -25dB 弱光保护。

10. `CalRawdataByAve(...)`
- 以 4 态平均作为 MAX/MIN 基线，PDL=max-min。

11. `CalRawdataByMaxMin(...)`
- 明确取四态最大最小作为 MAX/MIN。

12. `CalRawdataByMueller(...)`
- Mueller 矩阵方式计算 PDL/最大透过/最小透过。

13. `InitRawdataBuffer(...)`
- 清空或重新分配二维数组。

14. `ParserRawdata(...)`（私有）
- 解析 CSV 行到数组。
- 通过常量 `lightSpeed` 将波长转换频率并写入最后一列。

---

## 8. ParamCal 类详解

## 8.1 类作用

对上层暴露统一参数计算入口，将参数名解释后路由到 `IInterleaverAlgorithm`。

## 8.2 方法逐项

1. `ParamCal(IInterleaverAlgorithm alg)`
- 注入算法对象。

2. `CalChannelTestParam(...)`
- 输入：参数名、当前端口结果、相邻端口结果、中心频率、通道间隔等。
- 输出：通道级计算值（失败返回默认值）。
- 支持主要参数：
  - `MAXIL`
  - `MINIL`
  - `PDL`
  - `RIPPLE`
  - `SHIFT`
  - `ADJ`
  - `CT`
  - `STOPBAND`
  - `HBW_MAX/HBW_MIN/HBW_L/HBW_R`
  - `BW`
- 参数串解析规则：
  - `@PB=...`
  - `;DB=...`
  - 或 `@ITU`

3. `CalPortParam(...)`
- 输入：端口级参数名、温度、端口名、记录缓存、温度数组。
- 输出：端口汇总参数。
- 支持主要参数：
  - `MAXSHIFT` / `MINSHIFT`
  - `UNI` / `UNIPDL`
  - `WDL`
  - `MAXISO` / `MINISO`
  - `TDL`
  - `FSR`
  - `MAXBW`
  - `MINPEAKIL`
  - 其他参数按 max/min 规则聚合。

4. `GetRecordResultByParamName(...)`
- 按参数名+温度+端口检索缓存结果序列。

---

## 9. UI 布局与绑定逻辑

## 9.1 布局结构（XAML）

界面主要组成：

1. 左侧信息与动作区
- SN 输入
- PN / Spec 显示
- 归零时间显示
- 系统归零、单项测试、一键测试、上传数据、清空列表按钮
- 通过/失败图标
- 烤温倒计时

2. 右侧列表区
- 测试列表（Index + SN）

## 9.2 绑定行为

`UIVariable` 驱动控件状态：

1. `IsReferenceEnable` 控制系统归零按钮。
2. `IsScanEnable` 控制单项/一键测试按钮。
3. `IsSaveEnable` 控制上传按钮。
4. `IsClearSNVisiable` 控制清空列表按钮可见性。

---

## 10. 端到端流程梳理

## 10.1 启动流程

1. 控件加载。
2. MEF 组合依赖。
3. 注册主程序初始化事件。
4. 主程序推送 `MainInitInfo` 后，完成运行参数初始化。

## 10.2 打开模板流程

1. 读取 SN（首次从 `sn.csv` 批量读取）。
2. 后台调用 AMTS 打开模板。
3. 解析频段、配置、算法、端口映射。
4. 初始化曲线与 UI 测试项。
5. 尝试加载历史参考数据。
6. 自动进入一键测试入口。

## 10.3 系统归零流程

1. 用户触发系统归零。
2. 按端口提示接线并切换光路。
3. 扫描 4 偏振态数据。
4. 计算参考数据并落盘。
5. 更新端口归零状态。
6. 全部完成后恢复扫描按钮。

## 10.4 单项测试流程

1. 根据当前选中测试项确定产品、端口组、温度。
2. 校验归零状态。
3. 切换开关。
4. 若需要烤温，先倒计时。
5. 执行扫描/计算。
6. 刷新参数行、曲线、按钮状态。

## 10.5 一键测试流程

1. 选择下一组未完成端口。
2. 读取对应 Excel 数据填入 `portResData`。
3. 执行后台计算与显示更新。
4. 循环直到全部端口完成。
5. 自动上传 AMTS 并进入下一个 SN。

## 10.6 上传流程

1. 汇总每个产品的所有端口 rawdata。
2. 调用 `SaveDataToAMTS(...)`。
3. 成功后清空状态并继续下一 SN。

---

## 11. 方法索引清单（全量）

## 11.1 OperateInteleaverFinalTest

1. `OperateInteleaverFinalTest()`
2. `RefTimeCheck_DoWork(...)`
3. `RefTimeCheck_Progress(...)`
4. `BakeTimeCheck_DoWork(...)`
5. `BakeTimeCheck_Progress(...)`
6. `Compose()`
7. `InitRegerster()`
8. `GetExeDir()`
9. `Init(MainInitInfo info)`
10. `PassOrFail_Load(...)`
11. `InitPassFailImage()`
12. `UserControl_Loaded(...)`
13. `WarningBox(...)`
14. `ErrorBox(...)`
15. `btnOpenTemplate_Click(...)`
16. `OpenTemplateBK_DoWork(...)`
17. `ClearListData()`
18. `UpdateResIcon()`
19. `ParamItemUpdate(...)`
20. `OpenTemplateBK_RunWorkerCompleted(...)`
21. `GetScanIndex(int port)`
22. `IsContainPortAssist(...)`
23. `ParserRange(...)`
24. `RealtimeMsg(...)`
25. `btnScanRef_Click(...)`
26. `ScanRef()`
27. `SetSwitch(...)`
28. `ReadRefData(...)`
29. `IsRefTimePassdue(...)`
30. `ScanAndCalResult(...)`
31. `DoScan(...)`
32. `DoScanOnBK()`
33. `Scan_DoWork(...)`
34. `FindAdjPortIndex(...)`
35. `CalResByPort(...)`
36. `GetPortIndexByName(...)`
37. `CalPortRes(...)`
38. `ChannelCalThread(...)`
39. `CalAllResultInThread()`
40. `SetCalFinished(...)`
41. `IsAllCalFinished()`
42. `AddResultToRecord(...)`
43. `ClearResult(...)`
44. `Scan_RunWorkerCompleted(...)`
45. `ScanFinish(...)`
46. `UpdateReferenceStatus(...)`
47. `UpdateItem(...)`
48. `ReconnectServer(...)`
49. `SetOpenTemplateComplete(...)`
50. `GetOpenTemplateComplete()`
51. `SetIsScanFinished(...)`
52. `GetIsScanFinished()`
53. `btnClearBakeSN_Click(...)`
54. `btnOnekeyScan_Click(...)`
55. `OnekeyScan()`
56. `IsScanRef(...)`
57. `UserControl_Unloaded(...)`
58. `btnSingleScan_Click(...)`
59. `SelectedItemChangeRegister()`
60. `SelectedItemUpdate(...)`
61. `btnSaveToAMTS_Click(...)`
62. `UserControl_PreviewKeyDown(...)`
63. `OperateInteleaverFinalTest_PreviewKeyDown(...)`

## 11.2 InterleaverFinalTestCurve

1. `InterleaverFinalTestCurve(...)`
2. `InitAllCurve(...)`
3. `ClearAllCurve()`
4. `UpdateCurveShow(...)`
5. `UpdateFre(...)`
6. `InitCurve(...)`

## 11.3 InterleaverScanResult

1. `ReadRefTime(...)`
2. `ReadRefPortCount(...)`
3. `ReadRefSpec(...)`
4. `CheckRefRight(...)`
5. `ReadScanData(...)`
6. `WritePDLRefData(...)`
7. `WriteCalData(...)`
8. `CalRawdataByNoPDL(...)`
9. `CalPDLRefData(...)`
10. `CalRawdataByAve(...)`
11. `CalRawdataByMaxMin(...)`
12. `CalRawdataByMueller(...)`
13. `InitRawdataBuffer(...)`
14. `ParserRawdata(...)`

## 11.4 ParamCal

1. `ParamCal(...)`
2. `CalChannelTestParam(...)`
3. `CalPortParam(...)`
4. `GetRecordResultByParamName(...)`

---

## 12. 关键实现特征与注意点

1. 事件驱动架构
- UI 列表更新、模板刷新、曲线更新都通过事件发布完成，模块解耦明显。

2. 数据结构以 jagged array 为核心
- `double[][]` 结构大量用于高频扫描数据处理，性能导向明显，但可读性和边界控制复杂。

3. 一键测试路径与实时扫描路径并存
- `ScanAndCalResult(...)` 保留硬件扫描完整逻辑。
- `OnekeyScan()` 当前通过 Excel 读取直接填充数据，再走参数计算与显示链路。

4. 多线程策略较传统
- `Thread` + `Sleep` + 同步方法的模式稳定性依赖异常控制。

5. 文件/路径强依赖运行目录
- 参考文件、rawdata 文件、SN 文件、温度数据文件都绑定 `Environment.CurrentDirectory`。

---

## 13. 风险与边界说明（基于当前代码）

1. `Scan_DoWork(...)` 中扫描核心调用被注释。
- 影响：单项/部分场景可能依赖已有数据，不一定实时触发设备扫描。

2. 一键测试列位映射硬编码。
- 影响：Excel 模板列结构变化会直接导致数据错位。

3. 线程等待采用轮询。
- `CalAllResultInThread()` 使用 `while + Sleep(100)` 等待线程结束。

4. 路径与文件存在性依赖较强。
- 参考、rawdata、sn、15275 数据文件不存在时会失败。

5. 浮点比较部分存在直接比较。
- 例如温度判断某些位置使用 `CompareTo`，存在边界误差风险。

6. 错误信息以字符串累加为主。
- 多步骤失败时错误上下文可能过长或覆盖定位不清。

---

## 14. 工程价值总结

该工程是测试系统中的“终测 UI 编排器”，其核心价值在于：

1. 把模板、设备、算法、UI、上传五个域统一串联成闭环。
2. 支持多产品、多端口、分温度维度的批量测试组织。
3. 对 IL/PDL 相关转换与参数计算提供可切换算法路径。
4. 对 AMTS 回写和现场状态展示形成一体化流程。

在当前代码基础上，业务流程完整，工程化重点在于路径健壮性、并发可靠性与一键测试数据来源策略的持续规范化。

---

## 15. 全方法逐项行为说明（补充版）

本节对所有方法给出逐项说明，确保“方法级全覆盖”。

## 15.1 OperateInteleaverFinalTest 方法明细

1. `OperateInteleaverFinalTest()`：完成构造级初始化、绑定、路径与缓存初始化。
2. `RefTimeCheck_DoWork(...)`：归零计时线程心跳，每秒上报一次。
3. `RefTimeCheck_Progress(...)`：刷新归零倒计时并在超期时清理参考数据。
4. `BakeTimeCheck_DoWork(...)`：烤温倒计时后台执行。
5. `BakeTimeCheck_Progress(...)`：刷新烤温剩余时间，到时触发扫描。
6. `Compose()`：执行 MEF 依赖装配。
7. `InitRegerster()`：订阅主初始化事件。
8. `GetExeDir()`：返回进程可执行文件目录。
9. `Init(MainInitInfo info)`：接收主程序上下文并完成业务级初始化。
10. `PassOrFail_Load(...)`：结果图标初始化与默认显示。
11. `InitPassFailImage()`：加载通过/失败图标资源。
12. `UserControl_Loaded(...)`：控件加载入口，触发依赖装配与注册。
13. `WarningBox(...)`：警告提示包装（当前消息框被注释）。
14. `ErrorBox(...)`：错误提示包装（当前消息框被注释）。
15. `btnOpenTemplate_Click(...)`：打开模板按钮入口，含 SN 列表读取与前置校验。
16. `OpenTemplateBK_DoWork(...)`：后台打开模板并做 SN/Spec 校验。
17. `ClearListData()`：清空模板显示数据。
18. `UpdateResIcon()`：根据测试结果更新 pass/fail 图标。
19. `ParamItemUpdate(...)`：刷新显示层测试项数据与状态。
20. `OpenTemplateBK_RunWorkerCompleted(...)`：模板加载完成后解析配置、建模、初始化曲线并进入后续流程。
21. `GetScanIndex(int port)`：返回端口所属扫描组。
22. `IsContainPortAssist(...)`：判断端口辅助信息是否已存在。
23. `ParserRange(...)`：解析频段字符串并更新关键测试参数。
24. `RealtimeMsg(...)`：发布实时状态消息。
25. `btnScanRef_Click(...)`：系统归零按钮入口。
26. `ScanRef()`：按端口推进归零流程。
27. `SetSwitch(...)`：切换光开关到指定产品与端口。
28. `ReadRefData(...)`：读取并校验参考文件，更新 `IsRef`。
29. `IsRefTimePassdue(...)`：判断参考是否超时失效。
30. `ScanAndCalResult(...)`：执行扫描、读取数据、转换数据、写结果文件。
31. `DoScan(...)`：封装扫描设备调用。
32. `DoScanOnBK()`：创建并启动扫描后台任务。
33. `Scan_DoWork(...)`：后台扫描主逻辑，触发后续参数计算。
34. `FindAdjPortIndex(...)`：查找同输入组的相邻端口索引。
35. `CalResByPort(...)`：计算单端口子通道参数并写回。
36. `GetPortIndexByName(...)`：按端口名获取端口号。
37. `CalPortRes(...)`：计算端口总项参数。
38. `ChannelCalThread(...)`：端口计算线程入口。
39. `CalAllResultInThread()`：并发计算调度与汇总计算。
40. `SetCalFinished(...)`：线程安全设置某计算线程结束标记。
41. `IsAllCalFinished()`：线程安全判断所有计算线程是否结束。
42. `AddResultToRecord(...)`：线程安全写入参数中间记录。
43. `ClearResult(...)`：在失败或异常时清理指定端口结果。
44. `Scan_RunWorkerCompleted(...)`：扫描线程完成回调。
45. `ScanFinish(...)`：扫描后统一处理（曲线/状态/UI 流转）。
46. `UpdateReferenceStatus(...)`：更新指定端口的归零状态显示。
47. `UpdateItem(...)`：发布指定测试项更新事件。
48. `ReconnectServer(...)`：尝试重连扫描服务。
49. `SetOpenTemplateComplete(...)`：线程安全设置模板完成标记。
50. `GetOpenTemplateComplete()`：线程安全读取模板完成标记。
51. `SetIsScanFinished(...)`：线程安全设置扫描完成标记。
52. `GetIsScanFinished()`：线程安全读取扫描完成标记。
53. `btnClearBakeSN_Click(...)`：清空产品列表与当前显示。
54. `btnOnekeyScan_Click(...)`：一键测试入口校验并触发调度。
55. `OnekeyScan()`：一键测试核心调度逻辑（选端口组、读数据、触发扫描/计算）。
56. `IsScanRef(...)`：检查当前测试场景的归零完成状态。
57. `UserControl_Unloaded(...)`：控件卸载时取消计时线程。
58. `btnSingleScan_Click(...)`：单项测试流程入口。
59. `SelectedItemChangeRegister()`：注册选中项变化事件。
60. `SelectedItemUpdate(...)`：接收并保存当前选中项。
61. `btnSaveToAMTS_Click(...)`：上传数据并推进到下一 SN。
62. `UserControl_PreviewKeyDown(...)`：预留按键处理（当前空实现）。
63. `OperateInteleaverFinalTest_PreviewKeyDown(...)`：Enter 键快捷触发打开模板。

## 15.2 InterleaverFinalTestCurve 方法明细

1. `InterleaverFinalTestCurve(...)`：保存事件总线并初始化曲线颜色。
2. `InitAllCurve(...)`：根据曲线名变化或频段变化重建曲线。
3. `ClearAllCurve()`：清空所有曲线点数据。
4. `UpdateCurveShow(...)`：推送某条曲线的新点集。
5. `UpdateFre(...)`：更新显示频段并触发曲线重建。
6. `InitCurve(...)`：构造曲线初始化事件并发布。

## 15.3 InterleaverScanResult 方法明细

1. `ReadRefTime(...)`：从参考文件读时间戳。
2. `ReadRefPortCount(...)`：从参考文件读端口数。
3. `ReadRefSpec(...)`：从参考文件读 Spec 字段。
4. `CheckRefRight(...)`：校验参考光强阈值。
5. `ReadScanData(...)`：读取扫描 CSV 到数组。
6. `WritePDLRefData(...)`：输出参考数据 CSV。
7. `WriteCalData(...)`：输出转换结果 CSV。
8. `CalRawdataByNoPDL(...)`：无 PDL 路径下计算 IL。
9. `CalPDLRefData(...)`：由四偏振态构建参考数据。
10. `CalRawdataByAve(...)`：Average 算法生成 AVG/PDL/MAX/MIN。
11. `CalRawdataByMaxMin(...)`：MaxMin 算法生成 AVG/PDL/MAX/MIN。
12. `CalRawdataByMueller(...)`：Mueller 算法生成 AVG/PDL/MAX/MIN。
13. `InitRawdataBuffer(...)`：数组清空或重分配。
14. `ParserRawdata(...)`：内部 CSV 解析并补充频率列。

## 15.4 ParamCal 方法明细

1. `ParamCal(...)`：保存算法对象。
2. `CalChannelTestParam(...)`：通道级参数计算分发与返回。
3. `CalPortParam(...)`：端口级汇总参数计算分发与返回。
4. `GetRecordResultByParamName(...)`：从缓存中检索参数结果序列。
