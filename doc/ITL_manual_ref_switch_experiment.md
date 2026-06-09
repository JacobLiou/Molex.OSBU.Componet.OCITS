# ITL FTS — 系统归零手动光路对比实验

用于验证 `switch\` 指令表与现场拓扑（入光盒 + 出光盒）是否一致：操作员**手动**下发 MSW，程序**不自动切光**，但仍执行完整归零扫描。

## 启用 / 关闭

| 操作 | 说明 |
|------|------|
| **启用** | 在程序运行目录创建空文件：`set\DisableAutoSwitchDuringRef.txt` |
| **关闭** | 删除该文件（产线正常生产务必删除，否则归零不会自动切光） |

与 `set\DisableUploadRefCalibrationToTms.txt` 相同，仅需存在空文件，无需写入内容。

实现：`MolexUtility.TasRuntimeConfig.IsRefAutoSwitchDisabled()`。

## 影响范围

- **仅**「系统归零」流程（`ScanRef`）跳过 `SetSwitch`。
- 单项测试、一键测试仍会**自动**切换光开关。
- 归零扫描、写 `reference\referenceWithPDLPort-*.csv`、TMS 上传逻辑**不变**。

## 操作流程

1. 确认 `Deviceconfig.xml` 为双 MPLUS（`interleaverSwitch-MPLUS-IN` / `-OUT`），`switch\` 下已部署对应指令表。
2. 打开测试模板（含 `GROUP`，如 `PORT1:PM1;PORT2:PM2;`）。
3. 创建 `set\DisableAutoSwitchDuringRef.txt`。
4. 点击 **系统归零**：
   - 确认产品/通道对接；
   - 弹出 **「手动光路 — 请切换开关」**，记录其中的 **入光 flag**、**出光 flag** 及 switch 文件路径；
   - 用串口工具对 **入光盒、出光盒** 分别下发与 flag 对应的 MSW（在指令表中查 `[flag]` 块）；
   - 点 **确定**；
   - 查看实时状态中的 **「手动光路功率：PM1=… dBm」**（若 Deviceconfig 未配独立功率计，会提示用扫描结果判断）；
   - 等待 UDL 归零扫描完成。
5. 对每个需归零的口（如 Demux-Even、Demux-Odd）重复步骤 4。
6. 实验结束后 **删除** `set\DisableAutoSwitchDuringRef.txt`，再跑一轮自动归零，对比功率与 reference 曲线。

## Flag 与指令表（双盒示例）

| 口 | 入光 flag（IN 盒） | 出光 flag（OUT 盒） | 典型 MSW（见仓库 switch 文件） |
|----|-------------------|---------------------|-------------------------------|
| Demux-Even / PORT1 | `1::1:16` | `1::1:32` | IN: `MSW 1,1,2;9,1,1;` / OUT: `MSW 1,1,2;9,1,1;` |
| Demux-Odd / PORT2 | `1::1:16` | `2::17:32` | IN: `MSW 1,1,2;9,1,1;` / OUT: `MSW 2,1,2;11,1,1;` |

弹窗中会额外打印 `outChannel`，便于对照 OUT 侧通道号。

单盒/旧版配置时，弹窗显示 `interleaverSwitch-MPLUS` 与 `产品::通道:SWMaxPortFlag`。

## 结果判读

| 现象 | 建议 |
|------|------|
| 手动有光、自动无光 | 修正 `switch\interleaverSwitch-MPLUS-IN/OUT` 中对应 `[flag]` 的 MSW |
| 手动/自动都无光 | 查光纤、拓扑、TLS、UDL PM 索引（`UDLConfig.xml`） |
| 手动有光、扫描报「光太弱」 | 查 UDL 扫描波长范围与 PM 通道 |
| 归零后仍提示「归零文件不存在」 | 扫描未成功写 `reference\`，与光开关实验无关，查 UDL/扫描日志 |

## 相关源码

- 开关：`library/UIOperateInterleaverFinalTest/.../OperateInteleaverFinalTest.xaml.cs` — `ScanRef`、`ShowManualRefSwitchPrompt`、`ReadRefPowerSnapshot`
- 配置：`library/MolexUtility/TasRuntimeConfig.cs` — `IsRefAutoSwitchDisabled()`
- 指令表示例：`switch/interleaverSwitch-MPLUS-IN`、`switch/interleaverSwitch-MPLUS-OUT`
