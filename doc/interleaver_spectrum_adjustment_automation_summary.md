# Interleaver 光谱调节自动化：代码推导总结

## 1. 结论先行

从现有代码看，这条线并不是从零开始做自动化，而是已经有了“半自动骨架”：

- 已具备：模板打开、归零、FSTP 扫描、参数计算、结果上传、与自动化服务器收发消息。
- 当前短板：自动化协议薄弱（指令语义少、错误回传弱、状态机不完整）、调节闭环能力不够清晰（结果有回传但缺控制步骤定义）、配置与运行态耦合较强。

所以任务重点不是“新增扫描算法”，而是把现有能力整理成稳定的自动化闭环，并补齐可观测性、可恢复性和协议一致性。

---

## 2. 代码证据：当前已经有的能力

### 2.1 调节主控已有自动化入口

主模块 `UIOperateInterleaver` 已有自动化 Socket 回调，能处理如下指令：

- `SNNO;{sn}`：自动触发打开模板。
- `TEST;NOPDL`：触发无 PDL 扫描。
- `TEST;PDL`：触发 PDL 扫描。
- `TEST;UV;...`：处理照光分支并回 ACK。

并且测试后会向自动化侧发送结果串：

- 前缀：`TEST;PASS;`
- 内容：中低高频 shift、每侧 maxIL/FSR 等。

这说明“光谱调节自动化”的数据回传通道已经存在，可直接升级而不是重写。

### 2.2 扫描主链路已统一到 FSTP

在 `DoScan` / `ScanAndCalResultFSTP` 中，当前主路径为 UDL FSTP：

- `RefWithPDL` 与 `TestWithPDL` 通过 `IUDLFSTP.Scan(...)` 执行。
- 归零数据写入 `reference`，测试数据从 `rawdata` 回读计算。
- 结果通过 `InterleaverScanResult` 做扣归零、融合与落盘。

这意味着自动化推进时，核心风险点不在“能不能扫”，而在“何时扫、扫完怎么判、失败如何重试”。

### 2.3 开关与设备层已支持双 MPLUS

`DeviceHandle.InitDeviceByConfig` 已包含 `MPLUSSwitch` 装配，且会检查 `switch` 指令文件存在性。

结合现有文档，入光/出光双开关（IN/OUT）拓扑已经是标准方案。自动化推进应优先做“路由可靠性 + 配置一致性”治理，而不是改硬编码路由。

### 2.4 MES/TMS 上传链路完整

`FusionControl` 已覆盖：

- `OpenTemplate`
- `GoldsampleCheck`
- `UploadTestData`
- `TriggerTestResultUpload`

这说明产线闭环最后一跳可用。自动化推进时要做的是把“每次自动步骤”与“上传动作”的前置校验和失败兜底做清楚。

---

## 3. 你这项任务“要做什么”（按代码推导）

## 3.1 P0：把调节流程变成明确状态机

建议定义统一状态：

`Idle -> TemplateOpened -> RefReady -> Scanning -> Calculating -> ResultAcked -> Uploaded`

当前问题是流程在 UI 事件、回调和后台线程中分散，自动化端很难知道“现在可不可以下下一条命令”。

要做：

- 给每个关键步骤输出结构化状态（至少时间戳、SN、Port、ScanType、Result）。
- 失败时统一返回错误码与可读信息，不只写实时日志。

## 3.2 P0：升级自动化协议（入参与回参）

当前协议值域太少（主要是 `SNNO/TEST`）。建议补齐：

- 入参：`OPEN_TEMPLATE`、`REF`、`SCAN_PDL`、`SCAN_NOPDL`、`SAVE_UPLOAD`、`STOP`。
- 回参：`ACK`、`PROGRESS`、`RESULT`、`ERROR`。
- 每条消息带 `SN`、`PORT`、`REQ_ID`，支持幂等和追踪。

这部分主要改 `UIOperateInterleaver` 的自动化回调与发送逻辑。

## 3.3 P1：归零与参考数据治理

现有归零有超时机制（6h/6.5h）和自动删除逻辑，这是优点；但自动化场景还需：

- 明确“归零有效性判定”对自动调节的阻断规则。
- 在回传里带上 `RefAge`、`RefSource`、`RefBandRange`。
- 将“跳过自动切光”等 runtime flag 影响显式上报，避免线上误用。

## 3.4 P1：结果语义固化（给控制算法用）

现在会回传 shift/maxIL/FSR，但字段顺序是隐式约定。建议：

- 固定字段名（JSON 或 KV 串），不再依赖位置。
- 区分 `measured` 与 `derived`（例如 FSR 由 shift 差分得到）。
- 增加质量标记（是否 default 值、是否越界、是否参考缺失）。

## 3.5 P2：配置与部署标准化

已有 `switch` 指令生成脚本、设备配置规范、runtime 控制文件。建议再补：

- 自动化上线前自检：设备配置、switch 文件、UDL 关键项、模板关键 CFG。
- 一键导出现场诊断包（日志 + 配置快照 + 最近扫描文件）。

---

## 4. 聚焦哪些工程（优先级）

## 4.1 P0 核心工程（必须先动）

1. `library/UIOperateInterleaver`

- 自动化指令入口、调节扫描触发、结果回传、状态流转都在这里。
- 这是“光谱调节自动化”最核心工程。

2. `library/MolexUtility`

- `FusionControl`、`TasRuntimeConfig`、公共协议对象都在这里。
- 承担模板/上传/运行开关策略和跨模块公共逻辑。

3. `library/DeviceControl`

- 设备初始化、MPLUS/FSTP/Automation 获取都在这里。
- 负责把自动化流程映射到真实设备能力。

## 4.2 P1 支撑工程（第二阶段）

4. `library/InterleaverAlgorithm`

- 不是先改对象，但如果自动化要引入新判定指标，会落到这里。

5. `library/ProtocolAggregator`

- 如果要把状态流标准化为全局事件，需要扩展事件消息模型。

6. `library/UIOperateInterleaverFinalTest`

- 可复用其成熟流程（归零、扫描、上传、状态提示）的模式。
- 也可作为调节模块重构后的对照实现。

## 4.3 配置与脚本（上线必查）

7. `switch/`
8. `set/Deviceconfig.xml`（现场）
9. `scripts/gen_in_switch.py`、`scripts/gen_out_switch.py`

---

## 5. 推荐落地路线（4 周示例）

### 第 1 周：协议和状态机定稿

- 产出自动化消息规范（命令、应答、错误码、字段）。
- 在 `UIOperateInterleaver` 落地统一状态机和统一回包函数。

### 第 2 周：流程固化与失败恢复

- 打通 `OpenTemplate -> Ref -> Scan -> Calculate -> Ack` 全链路。
- 增加超时、设备异常、参考失效的恢复策略和错误回包。

### 第 3 周：数据质量与可观测性

- 固化结果字段语义；增加质量标记。
- 补齐日志主键（SN、Port、ReqId）并形成可检索链路。

### 第 4 周：试产验证

- 真实工位做回归：不同 Spec、不同端口映射、断连重试、长稳测试。
- 出上线检查单与回退策略。

---

## 6. 验收标准（建议）

- 自动化端可用单条命令完成一次完整调节扫描并拿到结构化结果。
- 关键失败场景（模板失败、开关失败、扫描失败、上传失败）均有明确错误码和重试建议。
- 结果可追溯到文件、SN、端口、时间、模板版本。
- 连续运行 8 小时无死锁、无线程阻塞、无消息乱序导致的误动作。

---

## 7. 关键参考（代码/文档）

- `library/UIOperateInterleaver/OperateInterleaver.xaml.cs`
- `library/MolexUtility/FusionControl.cs`
- `library/DeviceControl/DeviceControl/DeviceHandle.cs`
- `library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs`
- `doc/UIOperateInterleaverFinalTest_代码说明.md`
- `doc/ITL_FTS_终测核心数据流.md`
- `doc/ITL_FTS_1x16_template_spec.md`
- `doc/ITL_manual_ref_switch_experiment.md`


Updated todo list

我会基于你认可的“进程外网关”方向，把技术选型细化到可执行级别（.NET 8/C#、协议、部署、安全、验收），并先读取当前文档结构后给出第八节完整草案，确保你可直接落到文档中。接下来我先做一次快速发现并整理为结构化计划。

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/doc/interleaver_spectrum_adjustment_automation_summary.md)

Read memory

我已确认当前文档正好到第7节，适合直接续写第8节方案。下一步我会用一次探索子代理把 .NET 8 网关的技术栈和产线部署细节打磨成可落地清单，再给你可直接粘贴的“第8节内容草案”。

Gateway design details

Searched for regex `SNNO|TEST;|PDL|NOPDL|UV` (`**/*.cs`), 801 results

Searched for regex `FusionControl|OpenTemplate|UploadTestData|TriggerTestResultUpload` (`**/*.cs`), 328 results

Searched for regex `TcpClient|Socket|Listener|TcpListener` (`**/*.cs`), 69 results

Searched for regex `SNNO|SeverDataDeal|TEST;PASS|TEST;FAIL|ConnectSever|askPower|auto` (`**/library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs`), 3 results

Searched for regex `SNNO|SeverDataDeal|TEST;UV|TEST;FAIL|ConnectSever` (`**/library/UIOperateInterleaver/OperateInterleaver.xaml.cs`), 17 results

Searched for regex `UploadTestData|TriggerTestResultUpload|OpenTemplate\(` (`**/library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs`), 3 results

Searched for regex `class FusionControl|UploadTestData|TriggerTestResultUpload|OpenTemplate\(` (`**/library/MolexUtility/FusionControl.cs`), 16 results

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaver/OperateInterleaver.xaml.cs#880-880), lines 880 to 1025

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaver/OperateInterleaver.xaml.cs#1450-1450), lines 1450 to 1525

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/MolexUtility/FusionControl.cs#450-450), lines 450 to 520

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs#4435-4435), lines 4435 to 4555

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs#4555-4555), lines 4555 to 4635

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/doc/interleaver_spectrum_adjustment_automation_summary.md)

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/MolexUtility/ClientSocket.cs)

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaver/OperateInterleaver.xaml.cs#1048-1048), lines 1048 to 1115

Read [](file:///c%3A/Users/menghl2/WorkSpace/Projects/Test%20Program/SW2219_ITL_FTS/library/UIOperateInterleaverFinalTest/UIOperateInterleaverFinalTest/OperateInteleaverFinalTest.xaml.cs#80-80), lines 80 to 140

Read memory

我已经把 .NET 8 网关的关键设计细化出来了。接下来我会把“可执行计划”写入会话计划文件，并把你可直接放进第8节的文案一次性发给你。

Created memory file [](file:///c%3A/Users/menghl2/AppData/Roaming/Code/User/workspaceStorage/4cf608cbef63067b06cab04aded9fa52/GitHub.copilot-chat/memory-tool/memories/NzIwZjY4MzEtZDZjOC00MTM0LThiZTYtMjYzMDNkZDFjYWNl/plan.md)

**Plan: 第8节网关细化方案**
已按你的方向定稿为最小侵入路线：站侧进程外网关（.NET 8 C#）作为第三方统一入口，终测软件继续做设备执行主体，不做侵入式重构。计划已同步到会话计划文件：/memories/session/plan.md。

1. 确认拓扑：默认站侧网关，不内嵌到现有终测进程。
2. 锁定栈：ASP.NET Core 8 + BackgroundService + SQLite + Serilog + Polly + OpenTelemetry。
3. 定义契约：REST 异步作业模型 + webhook/SSE 推送 + 幂等键。
4. 定义桥接：旧 TCP 协议适配、单飞行命令、错误码映射、结果标准化。
5. 定义安全：API Key、IP 白名单，后续可升 mTLS。
6. 定义可靠性：超时、重试、熔断、重连、审计与可追溯。
7. 分阶段上线：旁路验证 -> 单站灰度 -> 多站复制。
8. 文档落地：将以下内容直接作为第8节加入你当前 markdown。

**可直接粘贴到文档的第8节内容**
## 8. 外部自动化网关细化方案（.NET 8 C#）

### 8.1 方案目标与边界
目标是在不重写现有终测主流程的前提下，为第三方提供标准化自动化接口，并保证产线稳定性与可追溯性。

1. 保留现有终测软件作为设备执行主体（模板、归零、扫描、计算、上传）。
2. 新增进程外网关作为第三方统一入口。
3. 首期不改核心算法与设备控制逻辑，仅做协议桥接与编排治理。
4. 第三方优先获取结构化结果和作业状态，大文件数据通过文件引用方式提供。

### 8.2 部署拓扑与推荐
1. 站侧网关（推荐）：每台工位部署一个网关实例，与终测软件本机或同网段通信。
2. 中心网关（可选后续）：用于多工位汇聚监控和统一审计，不参与实时控制闭环。

推荐理由：
1. 对现有系统改动最小。
2. 低时延，抗网络抖动能力强。
3. 单站故障隔离，不放大全线风险。
4. 上线和回退简单，适合灰度推进。

### 8.3 技术选型（.NET 8）
1. 对外接口层：ASP.NET Core 8 Minimal API。
2. 后台编排层：Generic Host + BackgroundService。
3. 协议适配层：TCP 文本协议适配（兼容 GB2312 与 CRLF）。
4. 序列化：System.Text.Json（统一 JSON 响应模型）。
5. 持久化：SQLite（WAL）保存作业、状态、审计、幂等键。
6. 日志：Serilog 输出结构化日志（JSON）。
7. 弹性策略：Polly（超时、重试、熔断、限流）。
8. 可观测：OpenTelemetry（Metrics、Trace、Logs）。

### 8.4 对外接口模型（第三方视角）
采用异步作业模型，避免扫描类长耗时导致接口阻塞。

1. 提交作业：POST /api/v1/stations/{stationId}/jobs
2. 查询作业：GET /api/v1/jobs/{jobId}
3. 取消作业：POST /api/v1/jobs/{jobId}/cancel
4. 工位状态：GET /api/v1/stations/{stationId}/state
5. 事件订阅：POST /api/v1/subscriptions/webhooks
6. 实时事件（可选）：GET /api/v1/events/stream

操作类型建议：
1. open_template
2. ref
3. scan_nopdl
4. scan_pdl
5. save_upload
6. stop
7. full_cycle

### 8.5 幂等与状态机
幂等与状态可追踪是自动化稳定的核心。

1. 每个请求必须携带 clientReqId 或 Idempotency-Key。
2. 网关以 stationId + 幂等键做唯一约束，重复请求返回已有 jobId。
3. 作业状态统一为：
1. queued
2. dispatched
3. running
4. waiting_ack
5. succeeded
6. failed
7. timeout
8. canceled

### 8.6 网关内部模块设计
1. API 层：鉴权、参数校验、幂等入口、限流。
2. 作业编排层：命令调度、状态推进、超时控制。
3. 旧协议适配层：命令编码、回包解析、连接管理。
4. 结果标准化层：把旧回包转换为结构化 RESULT。
5. 错误映射层：统一 errorCode、errorMessage、retryAdvice。
6. 持久化层：jobs、job_steps、audit_logs、idempotency_keys。
7. 推送层：webhook/SSE 事件发送与重试。

### 8.7 可靠性策略
1. 单工位单连接，单飞行命令，避免并发串扰。
2. 命令超时分级配置（模板、扫描、上传分别配置）。
3. 重试仅用于传输类失败，不对扫描动作盲目重试。
4. 连续失败触发熔断，快速失败并提示恢复动作。
5. 断线自动重连（指数退避 + 抖动）。
6. 网关重启后恢复未完成作业并标记为待对账状态。

### 8.8 安全策略（产线内网）
1. 首期采用 API Key + IP 白名单。
2. 对高等级场景可升级到 mTLS。
3. webhook 使用 HMAC 签名与时间戳防重放。
4. 全量审计字段：who、when、stationId、jobId、sn、operation、result。

### 8.9 第三方数据交付建议
1. 控制与状态数据：REST + 事件推送。
2. 结构化结果数据：JSON 字段化输出，不再使用位置式分号串。
3. 大体量原始谱线：返回文件引用或下载地址，不直接塞入同步响应。
4. 统一追踪主键：stationId、jobId、reqId、sn、port、templateVersion、timestamp。

### 8.10 三阶段上线计划
1. Phase 1（旁路验证）：网关仅做协议镜像与日志，不接管生产。
2. Phase 2（单站灰度）：一台工位切换到网关接管，验证节拍与稳定性。
3. Phase 3（多站复制）：推广至多工位，接入监控告警与安全加固。

### 8.11 验收标准（网关专项）
1. 同一请求重复提交不会重复触发设备动作。
2. 关键失败场景可返回统一错误码与重试建议。
3. 第三方可在单条作业链路中完整追踪状态与结果。
4. 连续运行 8 小时以上无消息乱序导致误动作。
5. 出现断连、超时、上传失败时具备可恢复路径与审计记录。


