---
name: FSTP UDL STA修复
overview: 根因是 UDL COM（fstpCtrl）在 UI 线程创建，却在新 STA 子线程调用；改为进程内唯一常驻 UDL STA 宿主线程统一初始化与调用，恢复快扫并保留 UI 不阻塞。
todos:
  - id: udl-sta-host
    content: 新增 UdlStaHost 常驻 STA 线程与 Invoke 同步封装
    status: completed
  - id: udl-init-host
    content: InitDeviceByConfig UDL 初始化迁入 UdlStaHost
    status: completed
  - id: udl-wrap-drivers
    content: UDLFSTPScan/UDLTccCtrl 全部 UDL 调用经 UdlStaHost
    status: completed
  - id: finaltest-fstp-revert
    content: 删除 RunFstpScanOnStaThread；增强 scan 错误与 CalFSTP return
    status: completed
  - id: verify-fstp-sta
    content: 1SN/16SN 快扫+TCC+编译验证
    status: completed
isProject: false
---

# FSTP 快扫 STA 线程错误修复

## 根因（已确认）

```mermaid
flowchart LR
  init[InitDevice UI STA 创建 fstpCtrl]
  bw[Scan_DoWork MTA]
  newSta[每次新建 STA 子线程]
  fail[UDL COM 线程亲和错误 扫描出错 N]
  init --> bw
  bw --> newSta
  newSta --> fail
```

- [`DeviceHandle.InitDeviceByConfig`](library/DeviceControl/DeviceControl/DeviceHandle.cs) 在 **UI 线程**（`MainWindow.InitDevice` RunWorkerCompleted）创建 `deviceEngine` / `fstpCtrl` / `tccCtrl`。
- 旧版可用：[`Dispatcher.Invoke`](library/UIOperateInterleaver/OperateInterleaver.xaml.cs) 在 **同一 UI STA** 上调 `scan.Scan`（UI 会卡十几秒）。
- 新版 [`RunFstpScanOnStaThread`](library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs) **每次 new Thread(STA)** 调 `scan.Scan` → 底层仍访问静态 `DeviceHandle.fstpCtrl` → **COM 公寓不匹配** → UDL `GetLastErrorMessage` 返回乱码短串（现场表现为 `扫描出错:N`）。

**不能**简单回退到 UI `Invoke`（会恢复 1×16 UI 卡死）。**不能**继续「每次新建 STA 子线程」。

## 方案（锁定）

在 **DeviceControl** 增加进程内 **唯一常驻 UDL STA 宿主线程**；UDL 初始化与所有 `fstpCtrl`/`tccCtrl`/`deviceEngine` 访问均在该线程执行；扫描仍在 `BackgroundWorker` 上 **Join 等待宿主线程**，UI 不阻塞。

```mermaid
flowchart LR
  ui[UI 一键/单项]
  bw[Scan_DoWork]
  host[UdlStaHost 常驻 STA]
  udl[fstpCtrl Scan 等待]
  ui --> bw
  bw -->|"Invoke 同步"| host
  host --> udl
```

## 改动

### 1. 新增 UDL STA 宿主（DeviceControl）

文件：新建 [`UdlStaHost.cs`](library/DeviceControl/DeviceControl/UdlStaHost.cs)（或并入 DeviceHandle）

- 启动时 `SetApartmentState(STA)`，**单线程**消费任务队列（`BlockingCollection` + `ManualResetEvent` 同步 `Invoke`）。
- 提供 `public static T Invoke<T>(Func<T> work)` / `Invoke(Action work)`，异常与返回值回传调用线程。
- 进程内只启动一次；`IsBackground = true`。

### 2. UDL 初始化迁到宿主线程

文件：[`DeviceHandle.cs`](library/DeviceControl/DeviceControl/DeviceHandle.cs)

- `InitDeviceByConfig` 中 UDL 块（`new UDL2_Engine` / `fstpCtrl` / `tccCtrl` / `OpenEngine`）整体包在 `UdlStaHost.Invoke` 内执行。
- 主窗体 `InitDevice` 仍在 UI 调用，但 **COM 对象创建在 UDL STA**，与后续快扫同线程。
- 非 UDL 设备（串口光开关等）仍可在原线程初始化，不必进 UDL 宿主。

### 3. 所有 UDL 入口经宿主线程

- [`UDLFSTPScan.cs`](library/DeviceControl/DeviceControl/FSTPScan/UDLFSTPScan.cs)：每个 public 方法开头 `UdlStaHost.Invoke(() => { ... })` 包裹现有 `fstpCtrl` 逻辑（含 `SweepWaitLock` 仍在宿主线程内，无需改锁语义）。
- [`UDLTccCtrl.cs`](library/DeviceControl/DeviceControl/TCC/UDLTccCtrl.cs)：同样包裹 `tccCtrl` 调用（TCC 三按钮逻辑在 UI 调 `IUDLTCC`，初始化迁走后也必须经宿主，否则 TCC 会再坏）。

### 4. 终测侧：删除错误 STA 包装

文件：[`OperateInteleaverFinalTest.xaml.cs`](library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs)

- **删除** `RunFstpScanOnStaThread`。
- `DoScan` 恢复直接 `scan.Scan(...)`（与旧版一致，但底层已保证在 UDL STA 宿主上执行）。
- 保留 `Scan_DoWork` 在 `BackgroundWorker`、`BeginInvoke(UpdateProductStatuses)`、列表节流等 **UI 不卡**改动。

### 5. 错误信息增强（同批小改，便于现场）

- `Scan_RunWorkerCompleted`：`扫描出错` 附带 `res` 码；若 `scanErrorMsg` 为空则写 `FSTP 失败(res=2)，详见日志`。
- `Scan_DoWork` 空 `catch` 改为写日志并设置 `scanErrorMsg`（避免吞异常只剩 `N`）。
- `ScanAndCalResultFSTP`：`CalFSTPRawdata` 后若 `errMsg` 非空应 **return 2**（当前可能 return 0 仍带 errMsg，逻辑漏洞一并修）。

## 不改

- 一键/归零状态机、IL 闸门、TCC 三按钮业务语义、快扫算法与 CSV 路径。
- 不要求改快扫服务/UDL 驱动本身（现场驱动升级仍并行）。

## 验证

- 1 SN / 16 SN 一键：快扫成功，无 `扫描出错:N`；扫描等待期间 UI 可拖动。
- 归零 + 测试 + 三温：结果与改前一致。
- TCC 读温/设点/三按钮：仍正常（经 UDL 宿主）。
- 日志可见 `fstp scan result:0`、`FSTP wait done`；失败时 UDL 完整错误写入日志。

## 部署 DLL

与上次一致，**至少**：`DeviceControl.dll`、`MolexUtility.dll`（若接口未变可只换 DeviceControl + `UIOperateInterleaverFinalTest.dll`）；建议三件套同版本部署。
