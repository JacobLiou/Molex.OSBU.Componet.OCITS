# Demux Even/Odd 同测 — 现场联调清单

部署 `UIOperateInterleaverFinalTest.dll` 后按下列项确认（代码侧已就绪；本清单不替代真机）。

1. 模板 CFG `GROUP`：`PORT1:PM1;PORT2:PM2;`（Even/Odd 必须不同 PM）。
2. 一键同温：状态栏先切两路出光 flag，再 **一次** FSTP；曲线同时出现 Demux-Even / Demux-Odd。
3. 耗时：同温双口应接近一轮扫描，而非串行两轮。
4. 设温：RT→LT 偶发 UDL 失败时 **不弹「循环箱设温」**，日志含 `silent continue` 或 `heating toward target`，三温继续。
5. 扫前 ±2°C 门禁：常温仍可「通过继续」；**LT/HT 仅「重试/终止」，禁止通过继续测**。

单元测试：`dotnet test library/UIOperateInterleaverFinalTest.Tests/UIOperateInterleaverFinalTest.Tests.csproj -c Debug`

---

# LT/HT 控温 + 归零一致性 — 回归清单

## 改前现场对照（根因确认）

1. 运行目录确认 **无** `set\DisableTccChamberCheck.txt`（有则整段跳过 TCC，与本修复无关）。
2. `Log\*log.txt` 搜：`操作员通过记忆`、`silent continue`、`已跳过循环箱`；若读温通过记忆后立刻扫 LT/HT，即旧策略根因。
3. `reference\` 看是否仅部分 `referenceWithPDLPort-product*-port*.csv` 存在/过期。

## 改后必测

1. **一键三温**：读温失败点「通过」→ LT/HT 仍须设温+烤温；烤温后门禁失败只能重试或终止，不能未控温开扫。
2. **常温**：读温通过记忆可跳过烤温（行为保留）。
3. **重启开模板**：故意删/弄坏 1～3 个口的 reference 文件 → 同产品 **全部** 标未归零，提示「存在无效归零，请整组重新系统归零」；系统归零后全部恢复。
4. 日志：`silent continue` 后 LT/HT 扫前仍须真实 ±2°C 通过（或终止），不应再出现「已有操作员通过记忆」直接放行门禁。

## 归零时间统一 + 测中不提前清除（黄工补充）

1. 系统归零 1~16 按提示依次对接；完成后打开各 `referenceWithPDLPort-*.csv` 首行时间戳应 **完全一致**（会话开始统一写入）。
2. 倒计时用该统一时间，不再按口分别到期。
3. 一键测至 RT/LT 已完成、HT 未完时：即便归零显示超期，**不得**自动删 reference；状态栏提示暂缓清除。全部 HT 测完（或闲置未开测）后才允许超期清除并要求重新归零。
