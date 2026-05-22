# ITL 终测 1×16 入 / 1×32 出 — MES 模板约定（16 SN × 1 路）

## 拓扑

- **入光（COM1）**：MPLUS `interleaverSwitch-MPLUS-IN`，TLS → DUT **IN1–16**（16 SN 槽位）。
- **出光（COM2）**：MPLUS `interleaverSwitch-MPLUS-OUT`，DUT **OUT** → N7745C **PM1–4**（见 `doc/物理拓扑图.png`）。
- 第 k 个录入的 SN（`ProductIndex = k`）入光通道默认 **k**；出光通道由端口名 **L3-4…L4-1** 或 `PORTn` 解析（仍为 1–16，出开关表支持至 32）。

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
| `GROUP` | 例：`PORT1:PM1;` — 定义端口与功率计映射（决定出光开关 PM 维） |
| `LFRANGE` / `MFRANGE` / `HFRANGE` | 与现网一致 |
| `Algorithm` / `PDLScanStep` | 与现网一致 |

## 设备与指令表

- `set\Deviceconfig.xml`：光源盒下 **两条** `MPLUSSwitch`：
  - `interleaverSwitch-MPLUS-IN` → COM1（入）
  - `interleaverSwitch-MPLUS-OUT` → COM2（出）
- 示例见 `doc/set/Deviceconfig_ITL_FTS.example.xml`（部署到产线时请 **GB2312** 保存）。
- 运行目录：
  - `switch\interleaverSwitch-MPLUS-IN` — 入光 `[产品::入通道:16]`
  - `switch\interleaverSwitch-MPLUS-OUT` — 出光 `[PM::出通道:32]`
- 文档示例：`doc/switch/ITL_MPLUS_SW_IN.example`、`doc/switch/ITL_MPLUS_SW_OUT.example`

## 开关 Flag

| 侧 | 格式 | 示例 |
|----|------|------|
| 入光 | `产品序号::入通道:16` | 第 3 个 SN：`3::3:16` |
| 出光 | `PM序号::出通道:32` | PM1 + L2-4（出通道 5）：`1::5:32` |

扫描/归零前业务层 **先后** 切换入光、出光；任一步失败则中止。

## 兼容

- 若仅配置旧单文件 `interleaverSwitch-MPLUS`，程序回退为单 flag `产品::通道:SWMaxPortFlag` 并提示升级双设备配置。
