# SW2219 ITL FTS — OCITS 光器件集成测试系统

本仓库为 **Interleaver（交织器/交错器）终测** 等光器件工站使用的 **OCITS（Optical Component Integrated Test System）** 客户端：基于 **WPF** 与 **.NET Framework**，采用 **MEF** 插件化架构，通过 XML 配置工位布局与仪器设备。

---

## 功能概览

- 主程序 **`OCITestSystem`**：解析启动参数与工位配置，动态加载 `module` 下的业务 DLL，初始化设备并聚合菜单与子界面。
- **`library`**：共享协议与设备抽象（`MolexUtility`）、设备实现（`DeviceControl`）、事件总线（`ProtocolAggregator`）、各类 `UIList*` / `UIOperate*` 等业务插件。
- 支持与产线系统集成（如 MIMS 传入 XML `AppInfo`）、MES/无纸化等（以现场配置与模块内 URL 为准）。

更完整的架构、数据流与类说明见：[doc/SW2219_ITL_FTS_设计文档.md](doc/SW2219_ITL_FTS_设计文档.md)

---

## 仓库结构

| 路径 | 说明 |
|------|------|
| `project/OCITestSystem/` | 主 WPF 解决方案与入口工程 |
| `project/OCITSAutoUpdate/` | 辅助程序（自动更新相关壳工程） |
| `library/` | 类库与 UI 插件源码（`MolexUtility`、`DeviceControl`、`ProtocolAggregator`、`UIOperate*` 等） |
| `bin/` | 编译输出与运行时依赖（如 `bin/common/*.dll`、部署包中的 `module` 插件） |
| `doc/` | 设计文档与说明 |

---

## 环境与依赖

- **Visual Studio**（建议 2019 或更高）与 **.NET Framework 4.8** 开发包  
- 主工程平台为 **x86**；仪器相关依赖以 `MolexUtility`、`DeviceControl` 的引用为准，常见包括：
  - **NI-VISA / IVI**（NationalInstruments.Visa、Ivi.Visa 等，路径因本机安装而异）
  - 现场提供的仪器与 **UDL** 相关 DLL（通常位于部署目录 `bin/common`）
- 完整运行需将 **`ProtocolAggregator.dll`**、**`MenuPluginInterface.dll`**、**`DeviceControl.dll`** 及各业务模块 DLL 放到可执行文件旁的 **`module`** 目录（与 `MainWindow` 中 `DirectoryCatalog` 约定一致）

---

## 构建说明

1. 打开 `project/OCITestSystem/OCITestSystem.sln`。  
2. 选择 **Debug** 或 **Release**，平台按解决方案配置（常见为 **Any CPU** / **x86**，以 `.csproj` 为准）。  
3. 生成 `OCITestSystem`；若解决方案中包含 `MolexUtility`，一并生成。  
4. 其余插件工程通常在独立解决方案中构建，输出复制到运行目录的 **`module`** 与 **`bin/common`**（与现有产线部署方式保持一致）。

---

## 运行与配置

| 配置项 | 典型路径 | 作用 |
|--------|-----------|------|
| 工位与产线 | `{运行目录}\set\stations.xml` | 工位类型、主 DLL 路径、自动化与金样等 |
| 界面布局 | `{运行目录}\module\Module_<工位类型>.xml` | 主界面 Grid 与加载的模块 `name` |
| 设备列表 | `{运行目录}\set\Deviceconfig.xml` | 功率计、光源、光开关、扫描器等实例化配置 |
| UDL（可选） | `{运行目录}\set\UDLConfig.xml` | 统一设备引擎配置 |

程序会在当前工作目录下按需创建 **`temple`**、**`reference`**、**`rawdata`**、**`data`**、**`lightdata`** 等文件夹。

**调试注意：** `App.xaml.cs` 中可能包含用于单机调试的 **硬编码启动 XML**；接入真实 MIMS 时需改为使用命令行参数 `e.Args` 中的内容。

### 故障排除：UDL 配置 / `DevKey1` 报错

若启动时出现 **「加载UDL配置出错：解析XML Device 节点 DevKey1属性 失败！」**：

- **原因：** `DeviceHandle`（及个别 UI 模块）在存在 **`set\UDLConfig.xml`** 时会调用 UDL 原生库的 `LoadConfiguration`。该报错表示当前 XML 中 **`<Device>` 节点的 `DevKey1` 属性不符合 UDL 要求**（缺失、为空、或与当前 UDL/SDK 版本不兼容），常见于从其他工站拷来的不完整配置。
- **需要正式使用 UDL 时：** 向设备/UDL 提供方索要 **与本机 DLL 版本匹配的合法 `UDLConfig.xml`**，或对照其文档逐项检查每个 `<Device … DevKey1="…" />`。
- **本地无 UDL、仅调试界面时：** 任选其一：
  1. 将 **`set\UDLConfig.xml`** 改名为备份（例如 `UDLConfig.xml.bak`），使程序不再尝试加载；或  
  2. 在 **`set`** 目录下创建空标记文件 **`DisableUDLEngine.txt`**（内容为不限制，存在即可），则 **跳过整个 UDL 引擎加载**，仍可按 `Deviceconfig.xml` 走传统仪表初始化。（正式连接 UDL 产线时请删除该文件。）

---

## 相关文档

- [doc/SW2219_ITL_FTS_设计文档.md](doc/SW2219_ITL_FTS_设计文档.md) — 业务流、数据流、各工程与核心类说明  
- [doc/design_document Antigravity Gen.md](doc/design_document%20Antigravity%20Gen.md) — 其他设计/生成说明（若存在）

---

## 许可与归属

内部测试程序，版权与发布策略以所属组织规定为准。
