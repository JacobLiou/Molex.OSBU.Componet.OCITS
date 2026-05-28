# ITL 终测 1×16 入 / 1×32 出 — MES 模板约定（16 SN × 1 路）

## 拓扑

见 [`doc/物理拓扑图_new.png`](doc/物理拓扑图_new.png)（工位一简化模型）：

- **入光（COM1）**：MPLUS `interleaverSwitch-MPLUS-IN`，TLS → DUT **IN1–16**（两片 1×8，模块 9/10）。
- **出光（COM2）**：MPLUS `interleaverSwitch-MPLUS-OUT`，DUT **OUT** → N7745C **PM1–4**（EVEN/ODD，flag 维 32）。
- 第 k 个录入的 SN（`ProductIndex = k`）在 **单口/单 PORT** 模式下入光通道默认 **k**。
- **Demux 双口**（Even/Odd，单 SN 或逐个「打开模板」累加至多 16 个 SN）：每 SN 一口入、两口出 — 入光 Even/Odd 均为 `k::k:16`（`ProductIndex` 为 k）；出光 PORT1 Even→`PM::17:32`（SW2 模块11）、PORT2 Odd→`PM::1:32`（SW1 模块9，GROUP 例 `PORT2:PM2`）。
- 出光通道由端口名 **L3-4…L4-1** 或 `PORTn` 解析（1–16；出开关表支持至 32）。

## 入光 MSW（指令表，级联实测）

入光盒为 **SW1(1×2) + SW9(1×8) + SW10(1×8)** 级联（见 [`doc/1X8.jpg`](1X8.jpg)）：

| SW1 段 | 含义 | 接至 |
|--------|------|------|
| `MSW 1,1,2` | 绿灯，上路 | 模块 **9**（IN1~8，L1/L2） |
| `MSW 1,1,1` | 红灯，下路 | 模块 **10**（IN9~16，L4/L5） |

每条 `[产品::入通道:16]` 对应 **一行** MSW：

| 入通道 n | MSW |
|----------|-----|
| 1 ~ 8 | `MSW 1,1,2;9,1,n;` |
| 9 ~ 16 | `MSW 1,1,1;10,1,(n-8);` |

示例：

```text
[1::1:16]
MSW 1,1,2;9,1,1;

[1::9:16]
MSW 1,1,1;10,1,1;
```

再生工具：[`scripts/gen_in_switch.py`](../scripts/gen_in_switch.py)。

## 出光 MSW（指令表，级联实测）

出光盒为 **SW1/SW2(1×2) + SW9~SW12(1×8)** 级联（见 [`doc/1X16.png`](1X16.png)）：

| 级联段 | 含义 | 接至 |
|--------|------|------|
| `MSW 1,1,2` | SW1 绿灯上路 | 模块 **9**（ch1~8，L3-4…L2-1） |
| `MSW 1,1,1` | SW1 红灯下路 | 模块 **10**（ch9~16，L1-4…L4-1） |
| `MSW 2,1,2` | SW2 绿灯上路 | 模块 **11**（ch17~24） |
| `MSW 2,1,1` | SW2 红灯下路 | 模块 **12**（ch25~32） |

每条 `[PM::出通道:32]` 的 MSW **仅由出通道 C 决定**（与 PM 序号无关）：

| 出通道 C | MSW |
|----------|-----|
| 1 ~ 8 | `MSW 1,1,2;9,1,C;` |
| 9 ~ 16 | `MSW 1,1,1;10,1,(C-8);` |
| 17 ~ 24 | `MSW 2,1,2;11,1,(C-16);` |
| 25 ~ 32 | `MSW 2,1,1;12,1,(C-24);` |

示例：

```text
[1::1:32]
MSW 1,1,2;9,1,1;

[1::17:32]
MSW 2,1,2;11,1,1;

[2::2:32]
MSW 1,1,2;9,1,2;
```

再生工具：[`scripts/gen_out_switch.py`](../scripts/gen_out_switch.py)。

## 打开模板

1. 同一 **Spec** 下连续「打开模板」最多 **16** 次；或使用操作区 **批量打开 SN**（每行一个）。
2. 仅 **第一个 SN** 的 CFG 会解析频率范围、`GROUP`、扫描分组；后续 SN 的 CFG 应与首个 SN 一致。

## 端口行命名

- 测试行：`通道名_频率_PORT1`（`PortNameForUser` 按下划线至少 3 段，末段为 `PORTn`）。
- 单端口模式：每 SN 仅 **1** 个 `PORT` 测试口（通常为 `PORT1`）。
- 端口显示名仍使用 **L3-4 … L4-1**（与光路图一致），无需改为 IN/OUT 编号。

## CFG 建议

| 参数 | 说明 |
|------|------|
| `GROUP` | 例：`PORT1:PM1;PORT2:PM2;` — 端口与功率计映射（决定出光 PM 维） |
| `LFRANGE` / `MFRANGE` / `HFRANGE` | 与现网一致 |
| `Algorithm` / `PDLScanStep` | 与现网一致 |

## 设备与指令表

- `set\Deviceconfig.xml`：光源盒下 **两条** `MPLUSSwitch`：
  - `interleaverSwitch-MPLUS-IN` → COM1（入），`<check>` 建议 `MSW 1,1,2;9,1,1;`
  - `interleaverSwitch-MPLUS-OUT` → COM2（出），`<check>` 建议 `MSW 1,1,2;9,1,1;`
- 示例见 [`doc/set/Deviceconfig_ITL_FTS.example.xml`](set/Deviceconfig_ITL_FTS.example.xml)（部署到产线时请 **GB2312** 保存）。
- 运行目录：
  - `switch\interleaverSwitch-MPLUS-IN` — 入光 `[产品::入通道:16]`
  - `switch\interleaverSwitch-MPLUS-OUT` — 出光 `[PM::出通道:32]`
- 文档示例：`doc/switch/ITL_MPLUS_SW_IN.example`、`doc/switch/ITL_MPLUS_SW_OUT.example`

## 开关 Flag

| 侧 | 格式 | 示例 |
|----|------|------|
| 入光 | `产品序号::入通道:16` | Demux 第 k 个 SN：Even/Odd 均为 `k::k:16`（两口共用入光槽） |
| 出光 | `PM序号::出通道:32` | Demux Even（PORT1）：`1::17:32` → SW2 模块11；Odd（PORT2）：`2::1:32` → SW1 模块9 |

> IN 指令表按「16 产品×16 通道」展开，MSW 仅随入通道 C 变化。OUT 表 MSW 仅随出通道 C 变化；Demux 出光固定 ch **17**（Even/SW2）与 ch **1**（Odd/SW1），与 `PORTn→n` 无关。见 `doc/工位接线图.png`。

扫描/归零前业务层 **先后** 切换入光、出光；任一步失败则中止。

## 兼容

- 若仅配置旧单文件 `interleaverSwitch-MPLUS`，程序回退为单 flag 并提示升级 **IN/OUT 双设备**；新工位请用分离的 IN/OUT 表。

## 产线迁移

- 更新 `switch\interleaverSwitch-MPLUS-IN/OUT` 后，将运行目录下同名文件同步到 exe 旁。
- 已在设备配置 UI 保存过的设备请把 IN/OUT **确定命令** 改为 `MSW 1,1,2;9,1,1;`（不会自动改写旧 `Deviceconfig.xml`）。
