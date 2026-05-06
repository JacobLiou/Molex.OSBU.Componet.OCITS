# SW2219 ITL FTS 光器件集成测试系统 — 设计文档

本文档基于仓库当前代码与工程结构梳理，用于说明 **OCITS（光器件集成测试系统）** 类框架的整体架构、业务与数据流、目录与工程划分，以及关键类型职责。

---

## 1. 概括总结

本仓库是一套面向 **光器件产线/研发测试** 的 **WPF（.NET Framework 4.x）** 桌面应用框架，典型场景包括 **Interleaver（交织器）终测**、Demux 相关测试、功率/曲线显示等。

**技术特征：**

| 维度 | 说明 |
|------|------|
| 宿主程序 | `OCITestSystem`：全屏主窗体，负责启动参数解析、工位与布局配置、MEF 插件装载、设备初始化与关闭 |
| 模块化 | **MEF（Managed Extensibility Framework）**：从运行目录下的 `module` 文件夹扫描 DLL，按约定 `Export` 注入 `UserControl` 子界面、`IMenuPlugin` 菜单、`IDeviceHandle` 设备总线、`IEventAggregator` 事件总线等 |
| 模块间通信 | **ProtocolAggregator**：基于 Prism 风格的 `IEventAggregator` + 各类 `CompositePresentationEvent<T>`；部分场景使用 **XML 字符串协议**（`XmlStr` + `MsgXmlParser`） |
| 设备抽象 | **MolexUtility**：定义 `IPowermeter`、`IOpticalSwitch`、`IInterleaverScan` 等接口与协议/数据结构 |
| 设备实现 | **DeviceControl**：`DeviceHandle` 实现 `IDeviceHandle`，按 XML 配置实例化仪器驱动；可选加载 **UDL**（`UDL2_Engine` 等）统一引擎 |
| 运行数据 | 程序在 `Environment.CurrentDirectory` 下按需创建 `temple`、`reference`、`rawdata`、`data`、`lightdata` 等目录 |

**与外部系统关系：** 启动时可接收类 MIMS 的 XML 参数（用户、工序、登录模式、MES 等）；部分 UI 模块内嵌 **AMTS/无纸化**、WebService 等 URL（以具体部署为准）。

---

## 2. 核心业务流

以下按 **一次典型会话** 描述主路径（以 `OCITestSystem` 为准）。

1. **进程启动**  
   `App.xaml.cs` 在 `Startup` 中构造 `MainWindow`，传入 XML 形式的 `AppInfo`（用户、Process、LoginMode、SoftwareID、MesMode、校验账号等）。仓库中当前存在 **硬编码测试用 XML**，便于脱离 MIMS 单机调试。

2. **工位与软件元数据**  
   `MainWindow` 构造函数中读取 `set\stations.xml`（`StationXMLParser`），确定当前产线/工位类型、自动化类型、金样标记、主业务 DLL 路径等；再读取 `module\Module_<工位类型>.xml`（`LayoutXMLParser`）解析 **软件 ID/版本/名称** 及是否使用 UDL 等，用于标题栏展示。

3. **界面装载（Window_Loaded）**  
   - **Compose**：`DirectoryCatalog` 指向 `{exe}\module`，执行 `ComposeParts`，完成 MEF 组合。  
   - **initMenu**：遍历所有 `IMenuPlugin`，按 `MenuDetail` 两级标题挂到菜单（如 ConfigModel 的「设置 → 设备配置」）。  
   - **InitRegerster**：订阅 `EventAggregator` 的 `EventXml`，将 XML 消息解析后路由到主窗体逻辑（如模板路径显示、初始化结果提示）。  
   - **布局**：按 `Module_*.xml` 中的 `Grid` 行列与 `DockPanel` 的 `Module` 属性，从 MEF 中按 **metadata `name`** 匹配 `UserControl`，放入主界面 `Grid`；支持同一模块多实例（`ModuleIndex`）。  
   - **工作目录与目录结构**：确保 `temple`、`reference`、`rawdata`、`data`、`lightdata` 存在。  
   - **延迟设备初始化**：`BackgroundWorker` 等待约 1 秒后执行 `InitDevice`。

4. **设备初始化**  
   若未走 UDL 独占路径，则调用 MEF 导入的 `IDeviceHandle.InitDeviceByConfig`（实现为 `DeviceHandle`）：读取 `set\Deviceconfig.xml`（`ConfigXmlParser`），按需加载 `set\UDLConfig.xml` 打开 UDL 引擎，再按配置创建功率计、光开关、光源、扫描器、偏振控制器等实例列表。

5. **全局上下文广播**  
   初始化结束后，`EventAggregator.GetEvent<EventMainInit>().Publish(mainInfo)`，将 `MainInitInfo`（工号、登录模式、MES、设备初始化结果等）分发给各子模块，用于列表、模板、MES 行为一致化。

6. **测试执行（各业务 UserControl 内）**  
   具体测试序列在对应 `UIOperate*` / `UIDemux*` 等控件中实现：通过 `IDeviceHandle` 取设备接口、通过 `IEventAggregator` 更新列表/曲线/实时功率、通过 `MsgXmlParser` 与主窗或其它模块交换 XML 消息；算法可通过 MEF 的 `IInterleaverAlgorithm`、`IAlgotithm` 等扩展。

7. **退出**  
   关闭主窗时调用 `IDeviceHandle.CloseAllDevice`，释放仪器与 UDL 资源。

---

## 3. 核心数据流

### 3.1 配置数据（XML / 文件）

| 数据 | 路径/载体 | 消费方 |
|------|-----------|--------|
| 工位与产线 | `set\stations.xml` | `StationXMLParser` → `MainWindow` |
| 主界面布局与模块清单 | `module\Module_<StationType>.xml` | `LayoutXMLParser` → `Grid` 布局与模块名 |
| 设备实例与路由 | `set\Deviceconfig.xml` | `ConfigXmlParser` → `DeviceHandle` |
| UDL 引擎配置（可选） | `set\UDLConfig.xml` | `DeviceHandle`（`UDL2_Engine`） |

### 3.2 运行时上下文对象

- **`MainInitInfo`**：贯穿登录、工序、模板类型、MES、自动化类型、设备初始化成功与否等，经 `EventMainInit` 广播。  
- **设备访问**：子模块通过 MEF 导入 `IDeviceHandle`，按 index/type/GUID 获取具体接口实例（功率计、扫描、TCC、FSTP 等）。

### 3.3 模块间消息

- **强类型事件**：`IEventAggregator.GetEvent<T>().Publish/Subscribe`，例如 `EventListItemUpdate`、`EventCurveUpdate`、`EventRealtimePowerUpdate`、`EventRealTimeStatus`、`EventListKeyDown` 等。  
- **XML 事件**：`EventXml` 携带 `XmlStr`；`MsgXmlParser` 解析 `MsgBaseInfo`（Type/Target/Source/Operate）及业务节点，用于与主窗或跨模块松耦合通信。

### 3.4 测试与参考数据目录

主程序确保存在：`temple`、`reference`、`rawdata`、`data`、`lightdata`。各业务模块将模板、参考曲线、原始扫描、计算结果等写入对应目录（具体文件名约定由各 `UIOperate*` 实现）。

---

## 4. 顶层文件夹说明

| 文件夹 | 作用 |
|--------|------|
| **`project/`** | 可交付/宿主级工程：主测试程序 `OCITestSystem`、辅助程序 `OCITSAutoUpdate` 等 |
| **`library/`** | 大量类库：设备控制、协议与事件总线、通用工具、按产品区分的 WPF 子模块、原生扫描 DLL 等 |
| **`bin/`** | 编译输出与依赖 DLL 聚集地（如 `bin\common\` 下的 `MolexUtility.dll`、`ProtocolAggregator.dll`、`MenuPluginInterface.dll` 及第三方仪器库）；**运行期 `module` 目录常与部署包中的插件 DLL 对应** |
| **`doc/`** | 文档（本设计说明等） |

---

## 5. `library/` 子目录说明（按职责分组）

以下为源码树中各子文件夹的**主要用途**（名称以仓库为准；带 `-old`、`-bk`、` - tool` 的多为历史备份或工具副本）。

### 5.1 基础设施与契约

| 目录 | 说明 |
|------|------|
| **MenuPluginInterface** | `IMenuPlugin`、`MenuDetail`：菜单扩展契约 |
| **ProtocolAggregator** | `IEventAggregator`、`EventAggregator` 及各类 `Event*`，模块间发布/订阅 |
| **MolexUtility** | 设备接口（`MolexUtility.Device`）、协议模型（`MainInitInfo`、`XmlStr`、`MsgBaseInfo`）、XML 解析、串口/VISA、UI 列表数据结构、算法接口等 **核心共享库** |
| **MoUtilityLib** | 登录窗、INI、WebService 辅助、曲线/模板等通用工具（偏传统 WinForms/WPF 混合支撑） |
| **CommonAlgorithm** | MEF 导出 `IAlgotithm`：IL/RL 等通用光学计算 |
| **InterleaverAlgorithm** | MEF 导出 `IInterleaverAlgorithm`：交织器相关频域/带宽/漂移等算法 |
| **ConfigModel** | 设备配置界面；`ConfigPlugin` 实现 `IMenuPlugin` |

### 5.2 设备与仪器

| 目录 | 说明 |
|------|------|
| **DeviceControl** | `DeviceHandle`：`IDeviceHandle` 的默认实现；封装功率计、光开关、光源、Interleaver 扫描、CD/FSTP 扫描、PDL、自动化、UDL 绑定等 |
| **InterleaverTestdll** | C++/MFC 与托管封装相关工程（快速扫描客户端 DLL 等），供扫描路径使用 |
| **InterleaverTestdll-bk** | 上述工程的备份副本 |

### 5.3 UI 插件（均为 `UserControl` + `[ExportMetadata("name", "...")]`）

| 目录 | 说明 |
|------|------|
| **UIListCommon** | 通用参数列表基座，订阅/发布列表与选中项事件 |
| **UIListSingleParam / UIListMultiParam** | 单参数/多参数列表展示 |
| **UIListInterleaver** | 交织器列表 |
| **UIListDemuxTest / UIListDemuxAdjust** | Demux 测试/调节列表 |
| **UICurve** | 曲线图控件 |
| **RealtimePower** | 实时功率显示 |
| **UIRealTimeStatus** | 实时状态 |
| **TestDetailShow** | 明细/1×8 等参数展示（如 `ParamList` 元数据名） |
| **UIOperateInterleaver** | 交织器操作主界面 |
| **UIOperateInterleaverFinalTest** | 交织器终测（体量大，含模板、烤温、归零、MES 等逻辑） |
| **UIOperateInterleaverMaterialTest** | 材料级测试 UI |
| **UIOperateInterleaver-old / UIOperateInterleaverFinalTest-old / UIOperateInterleaverFinalTest - tool** | 历史或工具链副本 |
| **UIDemuxTest / UIDemuxAdjust** | Demux 测试与调节（含嵌套工程变体） |
| **UIOperate1X8** | 1×8 PD 相关操作 |
| **UIOperatCIR** | CIR 操作 |
| **UIOperateITLCD** | ITLCD 相关 |
| **UIOperateLLCCAdjust** | LLCC 调节 |

### 5.4 工具与示例

| 目录 | 说明 |
|------|------|
| **TestMolexUtility** | 独立 WinExe，用于在简化环境中验证 `MolexUtility` 等能力 |
| **LibTest** | 控制台/窗体类测试工程 |
| **commondll** | 预置或第三方通用 DLL 存放（若存在） |

---

## 6. `project/` 工程介绍

### 6.1 OCITestSystem（主程序）

- **类型**：WPF 应用程序（`WinExe`），目标框架 **.NET Framework 4.8**，平台 **x86**。  
- **解决方案**：`project/OCITestSystem/OCITestSystem.sln` 当前包含 `OCITestSystem` + `MolexUtility` 两个项目引用；其余依赖以 **`bin\common\*.dll`** 及 **`module\*.dll`** 形式在运行时加载。  
- **职责**：登录后宿主、MEF 组合根、布局 XML 解析、设备生命周期管理、菜单聚合、XML 与事件总线入口。

### 6.2 OCITSAutoUpdate

- **类型**：WPF `WinExe`。  
- **现状**：模板级工程，业务代码较少，命名上用于 **软件自动更新/发布辅助**；可与主程序解耦部署。

---

## 7. 核心类与接口说明

### 7.1 宿主与配置解析（命名空间 `OCITestSystem`）

| 类型 | 职责 |
|------|------|
| **`MainWindow`** | MEF `CompositionContainer`；导入 `IEventAggregator`、`IDeviceHandle`、`IEnumerable<UserControl>`、`IEnumerable<IMenuPlugin>`；解析工位与布局；装载子模块；设备初始化与 `EventMainInit` 发布；全局按键转发 `EventListKeyDown` |
| **`StationXMLParser` / `StationShowConfig`** | 解析 `stations.xml`，构建产线-工位类型列表及单工位属性 |
| **`LayoutXMLParser` / `PanelConfige`** | 解析 `Module_*.xml` 的 Grid 行列与模块占位 |
| **`App`** | 启动主窗；`DoEvents` 辅助 UI 刷新 |

### 7.2 协议与事件（`ProtocolAggregator` / `MolexUtility.Protocol`）

| 类型 | 职责 |
|------|------|
| **`IEventAggregator` / `EventAggregator`** | `GetEvent<T>()` 获取单例事件通道 |
| **`EventMainInit`** | 广播 `MainInitInfo` |
| **`EventXml`** | 广播 `XmlStr`，用于 XML 驱动型消息 |
| **`EventListItemUpdate`、`EventListItemUpdateDemux`** | 列表行数据更新 |
| **`EventCurveUpdate`、`EventRealtimePowerUpdate`、`EventRealTimeStatus`** | 曲线与实时功率/状态 |
| **`EventListKeyDown`** | 键盘事件下沉到子模块 |
| **`MainInitInfo`** | 全局会话上下文 DTO |
| **`MsgXmlParser` / `MsgBaseInfo` / `XmlStr`** | OCITS XML 消息封装与解析 |

### 7.3 设备层（`MolexUtility.Device` / `DeviceControl`）

| 类型 | 职责 |
|------|------|
| **`IDeviceHandle`** | 设备初始化/关闭；按索引或类型返回功率计、光开关、电流表、光源、Interleaver 扫描、CD/FSTP、自动化、UDL 绑定对象等 |
| **`DeviceHandle`** | `IDeviceHandle` 实现；读取设备配置；维护各类 `List<I*>` 静态集合；集成 **UDL2_Engine** 可选路径 |
| **`IPowermeter`、`IOpticalSwitch、IOpticalSource、IInterleaverScan、ICDScan、IFSTPScan`** 等 | 仪器能力抽象，具体类在 `DeviceControl` 子目录（如 `Powermeter1830`、`SrcBank`、`InterleaverScan`） |

### 7.4 扩展点与典型 UI 基类

| 类型 | 职责 |
|------|------|
| **`IMenuPlugin`** | 主菜单扩展：`MenuHeader` + `Show(MainInitInfo)` |
| **`ConfigPlugin`** | 菜单项打开设备配置主窗 |
| **`ListCommon`（UIListCommon）** | 列表模块基类：持有 `EventAggregator`、`ShowContent` 列表、`UIParamShow` 与 WinForms `DataGridView` 桥接 |
| **`OperateInteleaverFinalTest`（UIOperateInterleaverFinalTest）** | 终测业务核心 UI 之一：设备、模板、后台线程、Web 接口等综合逻辑 |

### 7.5 算法扩展

| 类型 | 职责 |
|------|------|
| **`IInterleaverAlgorithm` / `InterleaverAlgorithm`** | 交织器频谱指标计算（CCF、漂移等） |
| **`IAlgotithm` / `CommonAlgorithm.Algorithm`** | 通用插入损耗等计算（命名中 `Algotithm` 为历史拼写） |

---

## 8. 构建与部署注意

- 主工程通过 **HintPath** 引用 `bin\common\` 下程序集；完整运行需将 **ProtocolAggregator、MenuPluginInterface、DeviceControl、各 UI 模块 DLL** 部署到约定目录（尤其是 **`module`**）。  
- `MolexUtility` 依赖 **NI-VISA / IVI** 等本机驱动路径（csproj 中 HintPath 指向 Program Files），换机构建需对齐环境或使用统一内部 Nuget/私有包管理。  
- **UDL** 与 **Agilent / 自研 InstrumentObjects** 等 DLL 位于 `bin\common`，与 `DeviceControl` 强相关。

---

## 9. 文档修订

| 项目 | 内容 |
|------|------|
| 文档版本 | 1.0 |
| 依据仓库路径 | `SW2219_ITL_FTS` |
| 说明 | 子模块内部分 URL、硬编码调试参数以源码为准；产线部署时应以实际配置与启动器（MIMS）为准 |
