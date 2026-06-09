# ITL FTS 终测核心数据流

本文只描述 Interleaver Final Test 的主路径数据流：从扫描到结果上传。

## 1. 核心主链路（Scan -> UploadTestData）

```mermaid
flowchart TD
	A[打开模板 OpenTemplate] --> B[系统归零 RefWithPDL]
	B --> C[FSTP 扫描输出原始 CSV]
	C --> D[ReadScanData 读回内存]
	D --> E[按算法计算 Ave/MaxMin/Mueller]
	E --> F[WriteFusionData 写测试结果 CSV]
	F --> G[点击保存/上传]
	G --> H[GetSNDir 获取服务器 SN 目录]
	H --> I[复制 rawdata CSV 到 SN 目录]
	I --> J[SaveDataToFile 生成上传 XML]
	J --> K[UploadTestData]
	K --> L[TriggerTestResultUpload]
```

## 2. 落盘与文件名图示

```mermaid
flowchart LR
	subgraph Local[运行目录 本地]
		L1[rawdata/ScanWithPDLPort*.csv]
		L2[reference/referenceWithPDLPort-productX-portY.csv]
		L3[rawdata/SN_IL_SCAN_端口_工序_温度ID.csv]
		L4[savetmp.xml]
	end

	subgraph Server[服务器 SN 目录]
		S1[snPath/SN_IL_SCAN_*.csv]
		S2[snPath/upload/SN.xml]
	end

	L1 -->|ReadScanData| L3
	L2 -->|参与归零补偿| L3
	L3 -->|保存上传时复制| S1
	L4 -->|SaveDataToFile 输出| S2
	S2 -->|UploadTestData + Trigger| U[TMS/TAS]
```

## 3. 关键目录与文件约定

- 本地扫描原始文件：`rawdata/ScanWithPDLPort*.csv`
- 本地归零参考文件：`reference/referenceWithPDLPort-product{产品序号}-port{端口号}.csv`
- 本地测试结果文件：`rawdata/{SN}_IL_SCAN_{端口名}_{工序}_{温度ID}.csv`
- 服务器归档结果文件：`{snPath}/{SN}_IL_SCAN_*.csv`
- 最终上传 XML：`{snPath}/upload/{SN}.xml`

## 4. 终测关注点（数据一致性）

- 归零参考与测试扫描要使用同一波段和步进，否则会导致补偿失真。
- `savePathList` 决定哪些本地 CSV 会被复制到服务器 SN 目录。
- 上传前若未写入软件信息、测试类型、权限等级，`SaveDataToFile` 会失败。
- `UploadTestData` 成功后仍需 `TriggerTestResultUpload` 成功，才算产线结果完成上报。
