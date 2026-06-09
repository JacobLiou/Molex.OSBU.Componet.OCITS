# ITL 终测 — 常温无 TCC 现场测试模式

场地无温度循环箱（TCC）、仅需验证 **常温（RT）** 与 Demux 光路时，可在运行目录 `set\` 下放置空标记文件，**无需改代码、删文件即恢复产线行为**。

## 标记文件

| 文件（`{exe}\set\`） | API | 作用 |
|----------------------|-----|------|
| `DisableTccChamberCheck.txt` | `TasRuntimeConfig.IsTccChamberCheckDisabled()` | 测试前不校验循环箱是否存在/读温/温差 |
| `RtOnlyTest.txt` | `TasRuntimeConfig.IsRtOnlyTestMode()` | 一键测试与单项测试仅调度 **20~30°C** 项，跳过 LT/HT |

示例可复制：

- [`doc/set/DisableTccChamberCheck.example.txt`](set/DisableTccChamberCheck.example.txt)
- [`doc/set/RtOnlyTest.example.txt`](set/RtOnlyTest.example.txt)

## 启用步骤

1. 在程序运行目录（与 `SW2219_ITL_FTS.exe` 同级）创建 `set\`（若不存在）。
2. 新建两个 **空文件**：
   - `set\DisableTccChamberCheck.txt`
   - `set\RtOnlyTest.txt`
3. 重新打开模板或重启程序。
4. 重建并部署 **`MolexUtility.dll`**、**`UIOperateInterleaverFinalTest.dll`**（含本次逻辑的版本）。

## 预期行为

- **一键测试**：只扫常温 Demux Even/Odd 等；常温扫完后若模板仍有 LT/HT 未测项，提示「RtOnlyTest：无常温待测项…」而非继续高低温。
- **单项测试**：手选 LT/HT 行会提示仅支持常温；选 RT 行可正常扫描。
- **状态栏**：出现「已跳过循环箱温度校验（set\DisableTccChamberCheck.txt）」。
- **列表**：LT/HT 行仍显示，便于对照模板，只是不会自动调度。

## 恢复产线

删除上述两个 txt（或仅删除不需要的一项），重新部署/重启即可恢复 TCC 校验与全温区一键测试。

## 与光开关 / Demux 的关系

本模式 **不改变** `switch\interleaverSwitch-MPLUS-IN/OUT` 与 Demux flag（`1::1:16`、`2::17:32` 等）。仅绕过温控箱与非常温测试队列。

## 相关源码

- [`library/MolexUtility/TasRuntimeConfig.cs`](../library/MolexUtility/TasRuntimeConfig.cs)
- [`OperateInteleaverFinalTest.xaml.cs`](../library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs) — `EnsureChamberReadyForTest`、`OnekeyScan`、`btnSingleScan_Click`
