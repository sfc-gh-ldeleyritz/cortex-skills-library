# Snowflake Pricing Reference - Master Document

> **Purpose**: Complete pricing bible for building accurate consumption estimates.
> **Source Document**: [Snowflake Service Consumption Table](https://www.snowflake.com/legal-files/CreditConsumptionTable.pdf)
> **Effective Date**: January 13, 2026
> **Extracted via**: Snowflake AI_PARSE_DOCUMENT with LAYOUT mode

---

## 1. CREDIT PRICING BY EDITION & REGION

### Table 2: On Demand Credit Pricing ($/Credit)

| Cloud Provider | Region | Standard | Enterprise | Business Critical | VPS |
|----------------|--------|----------|------------|-------------------|-----|
| **AWS** | US East (Northern Virginia) | $2.00 | $3.00 | $4.00 | $6.00 |
| **AWS** | US West (Oregon) | $2.00 | $3.00 | $4.00 | $6.00 |
| **AWS** | US East 2 (Ohio) | $2.00 | $3.00 | $4.00 | $6.00 |
| **AWS** | AP Mumbai | $2.00 | $3.00 | $4.00 | $6.00 |
| **AWS** | Canada Central | $2.25 | $3.50 | $4.50 | $6.75 |
| **AWS** | AP Singapore | $2.50 | $3.70 | $5.00 | $7.50 |
| **AWS** | EU Dublin | $2.60 | $3.90 | $5.20 | $7.80 |
| **AWS** | EU Frankfurt | $2.60 | $3.90 | $5.20 | $7.80 |
| **AWS** | Europe (London) | $2.70 | $4.00 | $5.40 | $8.10 |
| **AWS** | AP Sydney | $2.75 | $4.05 | $5.50 | $8.25 |
| **AWS** | Asia Pacific (Seoul) | $2.75 | $4.05 | $5.50 | $8.25 |
| **AWS** | AP Northeast 1 (Tokyo) | $2.85 | $4.30 | $5.70 | $8.55 |
| **AWS** | US East 1 Commercial Gov | - | - | $4.80 | $7.20 |
| **Azure** | East US 2 (Virginia) | $2.00 | $3.00 | $4.00 | $6.00 |
| **Azure** | West US 2 (Washington) | $2.00 | $3.00 | $4.00 | $6.00 |
| **Azure** | Canada Central (Toronto) | $2.25 | $3.50 | $4.50 | $6.75 |
| **Azure** | West Europe (Netherlands) | $2.60 | $3.90 | $5.20 | $7.80 |
| **Azure** | North Europe (Ireland) | $2.60 | $3.90 | $5.20 | $7.80 |
| **Azure** | Southeast Asia (Singapore) | $2.50 | $3.70 | $5.00 | $7.50 |
| **Azure** | UK South (London) | $2.70 | $4.00 | $5.40 | $8.10 |
| **Azure** | Australia East (New South Wales) | $2.75 | $4.05 | $5.50 | $8.25 |
| **Azure** | Japan East (Tokyo) | $2.85 | $4.30 | $5.70 | $8.55 |
| **GCP** | US Central 1 (Iowa) | $2.00 | $3.00 | $4.00 | $6.00 |
| **GCP** | US East 4 (N. Virginia) | $2.00 | $3.00 | $4.00 | $6.00 |
| **GCP** | Europe West 4 (Netherlands) | $2.60 | $3.90 | $5.20 | $7.80 |
| **GCP** | Europe West 2 (London) | $2.70 | $4.00 | $5.40 | $8.10 |
| **GCP** | Europe West 3 (Frankfurt) | $2.60 | $3.90 | $5.20 | $7.80 |

---

## 2. VIRTUAL WAREHOUSE CREDITS

### Table 1(a): Standard Warehouses (Credits/Hour)

| XS | S | M | L | XL | 2XL | 3XL | 4XL | 5XL | 6XL |
|----|---|---|---|----|----|-----|-----|-----|-----|
| 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 | 512 |

> Note: 5XL and 6XL may not be GA in all regions.

### Table 1(b): Gen 2 Warehouses (Credits/Hour)

| Cloud Provider | XS | S | M | L | XL | 2XL | 3XL | 4XL |
|----------------|-----|-----|-----|------|------|------|------|-------|
| **AWS** | 1.35 | 2.7 | 5.4 | 10.8 | 21.6 | 43.2 | 86.4 | 172.8 |
| **Azure** | 1.25 | 2.5 | 5.0 | 10.0 | 20.0 | 40.0 | 80.0 | 160.0 |
| **GCP** | 1.35 | 2.7 | 5.4 | 10.8 | 21.6 | 43.2 | 86.4 | 172.8 |

### Table 1(c): Snowpark Optimized Warehouses (Credits/Hour)

| Resource Constraints | XS | S | M | L | XL | 2XL | 3XL | 4XL | 5XL |
|---------------------|------|------|------|-------|-------|-------|--------|--------|--------|
| MEMORY_1X | 1.00 | 2.00 | 4.00 | 8.00 | 16.00 | 32.00 | 64.00 | 128.00 | - |
| MEMORY_1X_x86 | 1.10 | 2.20 | 4.40 | 8.80 | 17.60 | 35.20 | 70.40 | 140.80 | - |
| MEMORY_16X | - | - | 6.00 | 12.00 | 24.00 | 48.00 | 96.00 | 192.00 | 384.00 |
| MEMORY_16X_x86 | - | - | 6.25 | 12.50 | 25.00 | 50.00 | 100.00 | 200.00 | - |
| MEMORY_64X (Preview) | - | - | - | 15.00 | 30.00 | 60.00 | 120.00 | 240.00 | - |
| MEMORY_64X_x86 (Preview) | - | - | - | 16.00 | 32.00 | 64.00 | 128.00 | 256.00 | - |

### Table 1(d): Interactive Warehouse (Credits/Hour) - Preview

| XS | S | M | L | XL | 2XL | 3XL | 4XL |
|-----|-----|-----|-----|------|------|------|------|
| 0.6 | 1.2 | 2.4 | 4.8 | 9.6 | 19.2 | 38.4 | 76.8 |

> **Billing Rules**:
> - Standard/Gen2/Snowpark: 1-minute minimum, per-second thereafter
> - Interactive: 60-minute minimum, per-second thereafter
> - SPCS: 5-minute minimum, per-second thereafter

---

## 3. SNOWPARK CONTAINER SERVICES (SPCS)

### Table 1(e): SPCS Compute, CPU (Credits/Hour)

| Instance Family | SPCS Compute | Openflow Compute (SPCS) |
|-----------------|--------------|-------------------------|
| CPU_X64_XS | 0.06 | - |
| CPU_X64_S | 0.11 | 0.11 |
| CPU_X64_M | 0.22 | - |
| CPU_X64_SL | 0.41 | 0.41 |
| CPU_X64_L | 0.83 | 0.83 |

### Table 1(f): SPCS Compute, High-Memory (Credits/Hour)

| Instance Family | Credits/Hour |
|-----------------|--------------|
| HIGHMEM_X64_S | 0.28 |
| HIGHMEM_X64_M | 1.11 |
| HIGHMEM_X64_SL | 2.93 |
| HIGHMEM_X64_L | 4.44 |

### Table 1(g): SPCS Compute, GPU (Credits/Hour)

| Instance Family | Credits/Hour |
|-----------------|--------------|
| GPU_NV_XS | 0.25 |
| GPU_GCP_NV_L4_1_24G | 0.43 |
| GPU_NV_S | 0.57 |
| GPU_NV_SM | 1.70 |
| GPU_GCP_NV_L4_4_24G | 1.94 |
| GPU_NV_M | 2.68 |
| GPU_NV_2M | 3.50 |
| GPU_NV_3M | 3.55 |
| GPU_GCP_NV_A100_8_40G | 11.68 |
| GPU_NV_SL | 13.50 |
| GPU_NV_L | 14.12 |

---

## 4. OPENFLOW

### Table 1(h): Snowflake Openflow

| Deployment Type | Price |
|-----------------|-------|
| **BYOC Deployment** | 0.0225 Credits per vCPU per Hour |
| **Snowflake Deployment (SPCS)** | See SPCS CPU table above |

---

## 5. SNOWFLAKE POSTGRES COMPUTE (Preview)

### Table 1(i): Postgres Compute (Credits/Hour)

| Instance Family | AWS | Azure | AWS (HA) | Azure (HA) |
|-----------------|-------|-------|----------|------------|
| STANDARD_M | 0.0356 | 0.0376 | 0.0712 | 0.0752 |
| STANDARD_L | 0.0712 | 0.0752 | 0.1424 | 0.1504 |
| STANDARD_XL | 0.1424 | 0.1504 | 0.2848 | 0.3008 |
| STANDARD_2X | 0.2848 | 0.3008 | 0.5696 | 0.6016 |
| STANDARD_4XL | 0.5696 | 0.6016 | 1.1392 | 1.2032 |
| STANDARD_8XL | 1.1392 | 1.2032 | 2.2784 | 2.4064 |
| STANDARD_12XL | 1.7088 | 1.8048 | 3.4176 | 3.6096 |
| STANDARD_24XL | 3.4176 | 3.6096 | 6.8352 | 7.2192 |
| HIGHMEM_L | 0.1024 | 0.1088 | 0.2048 | 0.2176 |
| HIGHMEM_XL | 0.2048 | 0.2176 | 0.4096 | 0.4352 |
| HIGHMEM_2XL | 0.4096 | 0.4352 | 0.8192 | 0.8704 |
| HIGHMEM_4XL | 0.8192 | 0.8704 | 1.6384 | 1.7408 |
| HIGHMEM_8XL | 1.6384 | 1.7408 | 3.2768 | 3.4816 |
| HIGHMEM_12XL | 2.4576 | 2.6112 | 4.9152 | 5.2224 |
| HIGHMEM_16XL | 3.2768 | 3.4816 | 6.5536 | 6.9632 |
| HIGHMEM_24XL | 4.9152 | 5.2224 | 9.8304 | 10.4448 |
| HIGHMEM_32XL | 6.5536 | 6.9632 | 13.1072 | 13.9264 |
| HIGHMEM_48XL | 9.8304 | 10.4448 | 19.6608 | 20.8896 |
| BURST_XS | 0.0068 | - | 0.0136 | - |
| BURST_S | 0.0136 | 0.0144 | 0.0272 | 0.0288 |
| BURST_M | 0.0272 | 0.0288 | 0.0544 | 0.0576 |

---

## 6. SERVERLESS FEATURES

### Table 5: Serverless Feature Multipliers

| Feature | Compute Multiplier | Cloud Services Multiplier | Unit Charges |
|---------|-------------------|--------------------------|--------------|
| Serverless Tasks | 0.9 | 1 | - |
| Serverless Tasks Flex | 0.5 | 1 | - |
| Serverless Alerts | 0.9 | 1 | - |
| Query Acceleration | 1 | - | - |
| Clustered Tables | 2 | 1 | - |
| Materialized Views | 2 | 1 | - |
| Search Optimization | 2 | 1 | - |
| Replication | 2 | 0.35 | - |
| Backup | 2 | 1 | - |
| Failsafe Recovery | 0.9 | 1 | - |
| Data Quality Monitoring | 2 | 1 | - |
| Table Optimization | 0.75 | 1 | - |
| Trust Center | 1 | 1 | - |
| Organization Usage | 1 | 1 | - |
| Storage Lifecycle Policy | 0.50 | 1 | - |

### Serverless Unit Charges

| Feature | Unit Charge |
|---------|------------|
| Snowpipe | 0.0037 Credits per GB |
| Snowpipe Streaming | 0.0037 Credits per uncompressed GB |
| Snowpipe Streaming Classic | 0.01 Credits per client instance per hour |
| Automated Refresh | 0.06 Credits per 1000 files |
| Telemetry Data Ingest | 0.0212 Credits per GB |
| Open Catalog | 0.5 Credits per 1M requests |
| Logging | 0.28 Credits per 1000 file batches |
| Hybrid Tables | 1 Credit per 30GB read, 1 Credit per 7.5GB write |

---

## 7. CORTEX AI FEATURES

### Table 6(a): Cortex AI Functions (Credits per 1M Tokens)

| Model | Input | Output |
|-------|-------|--------|
| claude-3-5-sonnet | 1.50 | 7.50 |
| claude-3-7-sonnet | 1.50 | 7.50 |
| claude-4-sonnet | 1.50 | 7.50 |
| claude-4-5-sonnet | 1.65 | 8.25 |
| claude-haiku-4-5 (Preview) | 0.55 | 2.75 |
| claude-opus-4-5 (Preview) | 2.75 | 13.75 |
| deepseek-r1 | 0.68 | 2.70 |
| llama3.1-405b | 1.20 | 1.20 |
| llama3.1-70b | 0.36 | 0.36 |
| llama3.1-8b | 0.11 | 0.11 |
| llama3.3-70b | 0.36 | 0.36 |
| llama4-maverick | 0.12 | 0.49 |
| llama4-scout | 0.09 | 0.33 |
| mistral-large2 | 1.00 | 3.00 |
| mistral-7b | 0.08 | 0.10 |
| mixtral-8x7b | 0.23 | 0.35 |
| openai-gpt-4.1 | 1.00 | 4.00 |
| openai-gpt-5 (Preview) | 0.69 | 5.50 |
| openai-gpt-5-mini (Preview) | 0.14 | 1.10 |
| snowflake-arctic | 0.84 | 0.84 |
| snowflake-llama-3.3-70b | 0.29 | 0.29 |
| pixtral-large | 1.00 | 3.00 |
| gemini-2-5-flash (Preview) | 0.15 | 1.25 |
| gemini-3-pro (Preview) | 1.00 | 6.00 |

### Cortex Utility Functions (Credits per 1M Tokens)

| Function | Rate |
|----------|------|
| AI Sentiment | 1.60 |
| AI_AGG | 1.60 |
| AI_CLASSIFY | 1.39 |
| AI_EXTRACT (arctic-extract) | 5.00 |
| AI_FILTER (Preview) | 1.39 |
| AI_REDACT | 0.63 |
| AI_TRANSCRIBE | 1.30 |
| AI_TRANSLATE | 1.50 |
| Extract Answer | 0.08 |
| Guard | 0.25 |
| Sentiment | 0.08 |
| Summarize | 0.10 |

### Embedding Models (Credits per 1M Tokens)

| Model | Dimensions | Rate |
|-------|------------|------|
| snowflake-arctic-embed-m | 768 | 0.03 |
| e5-base-v2 | 768 | 0.03 |
| multilingual-e5-large | 1024 | 0.05 |
| snowflake-arctic-embed-l-v2.0 | 1024 | 0.05 |
| nv-embed-qa-4 | 1024 | 0.05 |
| voyage-multimodal-3 | 1024 | 0.06 |
| voyage-multilingual-2 | 1024 | 0.07 |

### Table 6(e): Cortex Agents (Credits per 1M Tokens)

| Model | Input | Output | Cache Write | Cache Read |
|-------|-------|--------|-------------|------------|
| claude-3-5-sonnet | 1.88 | 9.41 | - | - |
| claude-3-7-sonnet | 1.88 | 9.41 | 2.35 | 0.19 |
| claude-4-sonnet | 1.88 | 9.41 | 2.35 | 0.19 |
| claude-haiku-4-5 (Preview) | 0.69 | 3.45 | 0.87 | 0.07 |
| claude-4-5-sonnet | 2.07 | 10.36 | 2.59 | 0.21 |
| openai-gpt-4.1 | 1.38 | 5.52 | - | 0.35 |
| openai-gpt-5 (Preview) | 0.86 | 6.90 | - | 0.09 |

### Table 6(d): Snowflake Intelligence (Credits per 1M Tokens)

| Model | Input | Output | Cache Write | Cache Read |
|-------|-------|--------|-------------|------------|
| claude-3-5-sonnet | 2.51 | 12.55 | - | - |
| claude-3-7-sonnet | 2.51 | 12.55 | 3.14 | 0.25 |
| claude-4-sonnet | 2.51 | 12.55 | 3.14 | 0.25 |
| claude-haiku-4-5 (Preview) | 0.92 | 4.60 | 1.15 | 0.09 |
| claude-4-5-sonnet | 2.76 | 13.81 | 3.45 | 0.28 |
| openai-gpt-4.1 | 1.64 | 7.36 | - | 0.46 |
| openai-gpt-5 (Preview) | 1.15 | 9.21 | - | 0.12 |

### Table 6(h): Other AI Features

| Feature | Rate |
|---------|------|
| AI Parse Document - Layout | 3.33 Credits per 1,000 pages |
| AI Parse Document - OCR | 0.5 Credits per 1,000 pages |
| Cortex Analyst (API) | 67 Credits per 1,000 messages |
| Cortex Search | 6.3 Credits per GB/mo of indexed data |
| Document AI | 8 Credits per hour of compute |

### Fine-tuning (Preview)

| Model | Training (per 1M tokens) | Inference (per 1M tokens) |
|-------|--------------------------|---------------------------|
| llama3.1-70b | 3.40 | 2.42 |
| llama3.1-8b | 0.64 | 0.38 |
| mistral-7b | 0.64 | 0.24 |
| mistral-8x7b | 3.40 | 0.44 |

---

## 8. CLOUD SERVICES

**Rate**: 4.4 Credits per hour of Cloud Services use

**10% Adjustment Rule**: Daily Cloud Services charges are waived if they are ≤10% of daily Virtual Warehouse Services credits consumed.

**Exclusions from adjustment**: 
- Serverless Features usage
- SPCS Compute usage

---

## 9. DATA TRANSFER PRICING

### Table 4(a): AWS Data Transfer (per TB)

| From | Same Region | SPCS Same Region | Different Region | Internet/Other Cloud |
|------|-------------|------------------|------------------|---------------------|
| US East/West | $0.00 | $3.07 | $20.00 | $90.00 |
| EU Regions | $0.00 | $3.07 | $20.00 | $90.00 |
| AP Sydney | $0.00 | $3.07 | $140.00 | $140.00 |
| AP Tokyo | $0.00 | $3.07 | $90.00 | $114.00 |

### Table 4(b): Azure Data Transfer (per TB)

| From | Same Region | Same Continent | Different Continent | Internet |
|------|-------------|----------------|---------------------|----------|
| US/EU Regions | $0.00 | $20.00 | $50.00 | $87.50 |
| AP/AU Regions | $0.00 | $80.00 | $80.00 | $120.00 |

---

## 10. STORAGE PRICING

### Table 3(a): On-Demand Storage ($/TB/Month)

| Cloud | Region | Storage | Time Travel | Failsafe |
|-------|--------|---------|-------------|----------|
| AWS | US East (N. Virginia) | $23.00 | $23.00 | $23.00 |
| AWS | US West (Oregon) | $23.00 | $23.00 | $23.00 |
| AWS | EU Dublin | $25.00 | $25.00 | $25.00 |
| AWS | EU Frankfurt | $27.00 | $27.00 | $27.00 |
| AWS | AP Sydney | $28.00 | $28.00 | $28.00 |
| AWS | AP Tokyo | $29.00 | $29.00 | $29.00 |
| Azure | East US 2 | $23.00 | $23.00 | $23.00 |
| Azure | West Europe | $26.00 | $26.00 | $26.00 |
| GCP | US Central 1 | $23.00 | $23.00 | $23.00 |

### Archive Storage (Preview)

| Tier | Storage ($/TB/mo) | Retrieval ($/TB) |
|------|-------------------|------------------|
| Cool | $4.00 - $8.30 | $30.00 |
| Cold | $1.00 - $1.40 | $2.50 - $8.00 |

---

## 11. BILLING RULES SUMMARY

| Resource Type | Minimum Charge | Billing Granularity |
|---------------|----------------|---------------------|
| Standard/Gen2 Warehouse | 1 minute | Per-second |
| Interactive Warehouse | 60 minutes | Per-second |
| SPCS Compute | 5 minutes | Per-second |
| Postgres Compute | 1 minute | Per-second |
| Openflow | 60 seconds | Per-second |
| Serverless | Varies | Per-second |

---

## 12. FORMULAS & CALCULATIONS

### Monthly Warehouse Cost
```
Monthly Credits = (Credits/Hour) × (Hours/Day) × (Days/Month)
Monthly Cost = Monthly Credits × ($/Credit for Edition)
```

### Example: XS Warehouse, 8 hrs/day, 22 days/month, Enterprise @ $3/credit
```
Monthly Credits = 1 × 8 × 22 = 176 credits
Monthly Cost = 176 × $3.00 = $528/month
```

### SPCS GPU Cost Example
```
GPU_NV_M running 24/7 for a month:
Credits = 2.68 × 24 × 30 = 1,929.6 credits/month
Cost (Enterprise) = 1,929.6 × $3.00 = $5,788.80/month
```

### Cortex AI Token Cost
```
Tokens ≈ Characters / 4
Cost = (Input Tokens / 1M × Input Rate) + (Output Tokens / 1M × Output Rate)
```

---

## APPENDIX: Snowflake Tables in Account

**Database**: CONSUMPTION_ESTIMATOR.PRICING_DATA

| Table | Description |
|-------|-------------|
| CREDIT_CONSUMPTION_RAW | Full parsed PDF content |
| WAREHOUSE_CREDITS | Standard, Gen2, Interactive warehouse pricing |
| SPCS_CREDITS | CPU, High-Memory, GPU instance pricing |
| CREDIT_PRICING | Regional credit pricing by edition |
| AI_FEATURES_CREDITS | Cortex AI model pricing |
| SERVERLESS_FEATURES | Serverless feature multipliers |

---

> **Source**: All pricing data extracted via Snowflake AI_PARSE_DOCUMENT from Snowflake Service Consumption Table PDF
> **Effective Date**: January 13, 2026
> **Last Updated**: January 14, 2026
