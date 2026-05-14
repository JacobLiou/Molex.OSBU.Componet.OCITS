# 产线 Release 目录说明（`bin\New folder`）

本文档说明从产线拷贝的本工程 **Release 部署根目录** 中各文件、文件夹的典型用途。根路径对应仓库内：

`SW2219_ITL_FTS\bin\New folder`

该目录为 **MIMS 测试模块** 形态发布：`Mims.config.xml` 中声明入口程序为 `SW2219_ITL_FTS.exe`，并带有自动升级策略与若干排除文件列表。

---

## 1. 根目录：`Mims.config.xml`

- **作用**：MIMS 模块清单。定义 `EntryFile`（主程序）、`UpgradePolicy`（如 `AutoUpgrade`）、以及升级时 **不覆盖** 的本地文件列表（`Excludes`，例如 `set\Deviceconfig.xml`、`set\UDLConfig.xml` 等），避免产线个性化配置被升级包冲掉。

---

## 2. 根目录：主程序与核心依赖（`.exe` / `.dll`）

| 文件 | 作用简述 |
|------|----------|
| `SW2219_ITL_FTS.exe` | **Interleaver ITL FTS** 工位主程序（WPF/.NET），与 `set\stations.xml` 中 `MainDll=module/UIOperateInterleaverFinalTest.dll` 配合加载业务界面与逻辑。 |
| `OCITestSystem.exe` | **OCI 测试系统壳程序**（宿主），负责加载 `module` 下各 UI/算法 DLL、解析 `set` 下布局与站点配置；产线常通过 MIMS 启动 `SW2219_ITL_FTS.exe` 或由此壳统一拉起。 |
| `MenuPluginInterface.dll` | 菜单/插件接口契约，供各功能模块向主壳注册菜单或扩展点。 |
| `ProtocolAggregator.dll` | 协议聚合层，封装与仪器/中间件通信的协议调度。 |
| `MolexUtility.dll` | Molex 侧通用工具库（序列号、文件、业务辅助等，随解决方案引用）。 |
| `MIMS.BLL.dll` / `MIMS.DBUtility.dll` | MIMS 业务与数据访问组件，用于与 MIMS 数据库或业务服务交互。 |
| `MySql.Data.dll` | MySQL 官方 ADO.NET 驱动，供数据库访问使用。 |
| `Newtonsoft.Json.dll` | JSON 序列化/反序列化（配置、接口数据等）。 |
| `CRC32Lib.dll` | CRC32 校验（文件/数据完整性）。 |
| `FastScanClentDLL.dll` | 快速扫描客户端 DLL，与仪器扫描服务（Socket/共享内存等）交互；代码侧会将工作目录下的 `rawdata` 等路径传给扫描服务。 |
| `UDL.SockCommDll_LSMS.dll` | UDL 侧 Socket 通信库（与光源/控制设备会话相关）。 |
| `USL.SYS.dll` | 平台 **USL 系统层**（用户、站点、设备、与后台 WebService 等）；同目录 `USL.SYS.dll.config` 为其 **运行时配置**（服务端点、超时等）。 |
| `USL.TAS.dll` / `USL.TAS.C.dll` | 测试执行/任务相关 USL 组件（TAS：Test Application Service 一类平台模块）；`.C` 多为本机互操作或 C 风格封装实现。 |
| `UTL.LOG.dll` / `UTL.LOG.C.dll` | 日志工具库及其实现层。 |
| `UTL.ODAP.DBUtility.dll` | 数据访问/ODAP 相关工具库。 |

**说明**：根目录与 `module` 子目录中会出现 **同名 DLL 多份**（版本或部署习惯不同），运行时以 **实际加载顺序与路径** 为准；升级或排错时注意两边时间戳与版本是否一致。

---

## 3. 根目录：其它文件

| 文件 | 作用简述 |
|------|----------|
| `savetmp.xml` | 体积极大时多为 **运行期缓存/中间态**（例如界面或流程临时保存）；是否必需取决于当前版本是否读取；产线拷贝中可能含 **历史数据**，新环境部署可酌情清理后由程序重建。 |

---

## 4. 子目录说明

### 4.1 `image\`

- `Pass.ico`、`Fail.ico`：测试结果 **通过/失败** 图标资源，供界面状态显示使用。

### 4.2 `lightdata\`

- **作用**：与 **照光（UV 等）前后** 相关的数据目录（源码中亦有将照光前后 rawdata 写入网络的逻辑）。
- **当前拷贝状态**：目录存在但 **为空**；产线有数据时会出现按 SN、工序组织的文件。

### 4.3 `module\`

- **作用**：**功能模块 DLL 集**，由 `OCITestSystem.exe` / 主程序按 `set\module.xml` 与 `set\stations.xml` 动态加载。
- **典型内容分类**：
  - **本工程业务**：`UIOperateInterleaverFinalTest.dll`（Interleaver 终测 UI 与流程）、`InterleaverAlgorithm.dll`、`UIListInterleaver.dll`、`UICurve.dll`、`UIRealTimeStatus.dll`、`UIListCommon.dll`、`UIListMultiParam.dll`、`UIListSingleParam.dll`、`RealtimePower.dll` 等。
  - **公共与配置**：`ConfigModel.dll`、`DeviceControl.dll`、`CommonAlgorithm.dll`、`DataProcessing.dll` 等。
  - **仪器与厂商**：`InstrumentServer.dll`、`InstrumentObjects.dll`、`Ivi.Visa.dll`、`NationalInstruments.Visa.dll`、`Interop.*`（Agilent PNA、VBA/OWC 等 COM 互操作）、`Ag86038x_*`（安捷伦/是德相关引擎与接口）。
  - **远程/服务**：`RemoteClient.dll`、`RemoteServices.dll`。
  - **重复依赖**：`CRC32Lib.dll`、`Newtonsoft.Json.dll`、`MySql.Data.dll`、`MIMS.*`、`MolexUtility.dll`、`ProtocolAggregator.dll`、`UDL.SockCommDll_LSMS.dll`、`USL.*`、`UTL.*` 等与根目录形成 **就近加载** 或历史打包布局。
- **`module_ITL_FTS.xml`**：本工位 **界面布局描述**（WPF `Grid` + `DockPanel` 的 `Module` 属性），声明加载 `UIOperateInterleaverFinalTest`、`UICurve`、`UIRealTimeStatus`、`UIListInterleaver` 等，并标注软件 ID `SW2219` 与版本信息。

### 4.4 `rawdata\`

- **作用**：扫描得到的 **原始曲线/融合数据 CSV** 的本地落盘目录。程序使用 `Environment.CurrentDirectory + "\\rawdata\\" + 文件名` 写入；命名通常包含 **SN**、**IL_SCAN**、端口/通道名（如 `Demux-Even`/`Odd`）、**工序**（如 `Interleaver-ITL-*`）、**温区/模板 ID**（如 `_LT`/`_RT`/`_HT`）等。
- **产线拷贝含义**：目录内大量 CSV 为 **历史测试残留**；新站点部署可备份后清空，避免混淆；若需保留追溯请勿随意删除。

### 4.5 `reference\`

- **作用**：**参考曲线/基准数据**（如 `referenceWithPDLPort-product*-port*.csv`），用于 PDL 等测试与产品/端口维度的对比；设备控制侧会将参考路径指向工作目录下的 `Reference`（与 `reference` 在 Windows 上通常等价）。

### 4.6 `set\`

- **作用**：**站点与运行参数配置**（XML/INI/MDB 等），决定壳程序加载哪个 DLL、布局文件、设备与网络参数等。
- **常见文件**：
  - `stations.xml`：产品线、工位类型、`MainDll` 路径、`TestProcess`、`Goldsample` 等（当前片段可见 Interleaver / `ITL_FTS` → `module/UIOperateInterleaverFinalTest.dll`）。
  - `module.xml`：另一工位或视图的模块布局（示例中含 `UIOperate1X8`、`UIListMultiParam` 等，可与 `module_ITL_FTS.xml` 区分用途）。
  - `Deviceconfig.xml`、`AllDevice.xml`、`showConfig.xml`、`UDLConfig*.xml`、`XMLSet.ini`、`Light.ini`、`UVATAConfig.ini`、`UVATASocket.ini`、`UvataConfig.mdb`：设备列表、显示配置、UDL/UVATA 光源与 Socket 参数、数据库型配置等。
- **注意**：`Mims.config.xml` 中部分文件列入 **升级排除**，产线本地修改应保留备份。

### 4.7 `switch\`

- **作用**：**光开关串口/路由配置文件**（无扩展名的二进制或专有格式文本，由 `DeviceControl` 等读取）。文件名如 `interleaverSwitch`、`InterleaverFinalTestSwitch`、`InterleaverFinalTestMsPlus` 及带后缀的变体对应不同机台或校准版本。

### 4.8 `temple\`（部署习惯上多为 **template** 简写）

- **作用**：**测试模板/临时文件**（如 `temp.xml`、`temp_data.xml`、`tempdata.ini`、`*-Test-Manual.xml`、`*-Adjust-Manual.xml`），用于人工或半自动流程的界面默认值与工序模板；`Mims.config.xml` 曾引用 `temple\PWMReset.csv` 一类文件（若存在则与复位流程相关）。

### 4.9 `UDL\`

- **作用**：按 **设备或站点 ID** 分子目录存放 **UDL 相关日志或导出**（示例中为 `ITPC180117` 下多条 `*-UDL.FSTP-*.txt`），与 FSTP/光源控制会话记录对应，便于产线追溯。

---

## 5. 与源码路径的对应关系（便于维护）

- `rawdata`、`reference`（代码中亦写为 `Reference`）路径在 `DeviceControl\Interleaver\InterleaverScan.cs` 与 `UIOperateInterleaverFinalTest` 等中拼接到 **当前工作目录**，部署时需保证 **启动目录** 即为该 Release 根目录。
- `set\stations.xml` 的 `MainDll` 与 `module` 子目录中的 `UIOperateInterleaverFinalTest.dll` 必须 **路径一致且版本匹配**，否则会出现界面空白或加载异常。

---

## 6. 部署与拷贝建议

1. **整目录拷贝**：保持相对路径不变；不要单独挪走 `module` 或 `set` 而不改配置。
2. **敏感信息**：`USL.SYS.dll.config` 与 `set` 下 XML 可能含 **内网服务地址**；对外分发时注意脱敏或单独管理。
3. **清理**：`rawdata`、`temple`、`savetmp.xml`、`UDL` 下日志是否保留，按质量与审计要求决定；升级前建议备份 `set` 中被 `Mims.config.xml` 排除的文件。

---

*文档生成依据：目录树、`Mims.config.xml`、`set\stations.xml`、`module\module_ITL_FTS.xml` 及仓库内 `InterleaverScan`、`UIOperateInterleaverFinalTest` 等对路径的引用。*
