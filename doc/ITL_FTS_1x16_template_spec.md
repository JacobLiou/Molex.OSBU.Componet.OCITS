# ITL 终测 1×16 工位 — MES 模板约定（16 SN × 1 路）

## 拓扑

- 16 个 SN 并行，每个 SN 占用 **1 路** MPLUS 光开关输出（通道 1–16）。
- 第 k 个录入的 SN（`ProductIndex = k`）默认映射开关通道 **k**（模板可统一使用 `PORT1`）。

## 打开模板

1. 同一 **Spec** 下连续「打开模板」最多 **16** 次；或使用操作区 **批量打开 SN**（每行一个）。
2. 仅 **第一个 SN** 的 CFG 会解析频率范围、`GROUP`、扫描分组；后续 SN 的 CFG 应与首个 SN 一致。

## 端口行命名

- 测试行：`通道名_频率_PORT1`（`PortNameForUser` 按下划线至少 3 段，末段为 `PORTn`）。
- 单端口模式：每 SN 仅 **1** 个 `PORT` 测试口（通常为 `PORT1`）。

## CFG 建议

| 参数 | 说明 |
|------|------|
| `GROUP` | 例：`PORT1:PM1;` — 定义端口与功率计映射 |
| `LFRANGE` / `MFRANGE` / `HFRANGE` | 与现网一致 |
| `Algorithm` / `PDLScanStep` | 与现网一致 |

## 设备与指令表

- `set\Deviceconfig.xml`：光开关 `Type=MPLUSSwitch`，`name=interleaverSwitch-MPLUS`。
- 运行目录 `switch\interleaverSwitch-MPLUS`：含 `[1::1:16]`…`[16::16:16]` 段。

## 开关 Flag

`产品序号::通道号:16`，例如第 3 个 SN：`3::3:16`。
