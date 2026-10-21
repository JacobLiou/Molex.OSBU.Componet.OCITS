# Demux Even/Odd 同测 — 现场联调清单

部署 `UIOperateInterleaverFinalTest.dll` 后按下列项确认（代码侧已就绪；本清单不替代真机）。

1. 模板 CFG `GROUP`：`PORT1:PM1;PORT2:PM2;`（Even/Odd 必须不同 PM）。
2. 一键同温：状态栏先切两路出光 flag，再 **一次** FSTP；曲线同时出现 Demux-Even / Demux-Odd。
3. 耗时：同温双口应接近一轮扫描，而非串行两轮。
4. 换温：实测与目标差 > 5°C 时状态栏「开始进行X度烤温」，倒计时结束直接扫描；读温/设温各重试 1 次仍失败则结束本次测试（重新点击即可）。

单元测试：`dotnet test library/UIOperateInterleaverFinalTest.Tests/UIOperateInterleaverFinalTest.Tests.csproj -c Debug`

---

# 归零一致性 — 回归清单

## 改前现场对照（根因确认）

1. `reference\` 看是否仅部分 `referenceWithPDLPort-product*-port*.csv` 存在/过期。

## 改后必测

1. **重启开模板**：故意删/弄坏 1～3 个口的 reference 文件 → 同产品 **全部** 标未归零，提示「存在无效归零，请整组重新系统归零」；系统归零后全部恢复。

## 归零时间统一 + 测中不提前清除（黄工补充）

1. 系统归零 1~16 按提示依次对接；完成后打开各 `referenceWithPDLPort-*.csv` 首行时间戳应 **完全一致**（会话开始统一写入）。
2. 倒计时用该统一时间，不再按口分别到期。
3. 一键测至 RT/LT 已完成、HT 未完时：即便归零显示超期，**不得**自动删 reference；状态栏提示暂缓清除。全部 HT 测完（或闲置未开测）后才允许超期清除并要求重新归零。
