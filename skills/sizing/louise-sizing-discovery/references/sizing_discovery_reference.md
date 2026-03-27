# Snowflake Sizing Discovery Reference Guide

> **Purpose:** Reference document for generating sizing discovery questionnaires. For each Snowflake feature, lists the required inputs for pricing, the pricing model, discovery questions to ask, and common gotchas.

---

## Table of Contents

1. [Compute - Warehouses](#1-compute---warehouses)
2. [Ingestion - Openflow (CDC/ETL)](#2-ingestion---openflow-cdcetl)
3. [Ingestion - Snowpipe & Snowpipe Streaming](#3-ingestion---snowpipe--snowpipe-streaming)
4. [OLTP - Snowflake Postgres](#4-oltp---snowflake-postgres)
5. [Serverless - Dynamic Tables](#5-serverless---dynamic-tables)
6. [Serverless - Tasks](#6-serverless---tasks)
7. [Containers - SPCS](#7-containers---spcs)
8. [AI - Cortex AI Functions](#8-ai---cortex-ai-functions)
9. [AI - Cortex Search](#9-ai---cortex-search)
10. [AI - Cortex Analyst / Semantic Views](#10-ai---cortex-analyst--semantic-views)
11. [AI - Cortex Agents](#11-ai---cortex-agents)
12. [Transformation - dbt Projects](#12-transformation---dbt-projects)
13. [Storage](#13-storage)
14. [Data Sharing & Marketplace](#14-data-sharing--marketplace)
15. [Iceberg Tables](#15-iceberg-tables)

---

## 1. Compute - Warehouses

### Pricing Model
| Size | Credits/Hour | Use Case |
|------|-------------|----------|
| XS | 1 | Dev/test, <10 concurrent users, simple queries |
| S | 2 | Light BI (<20 users), simple transforms |
| M | 4 | Standard BI (20-50 users), moderate transforms |
| L | 8 | Heavy transforms, complex queries, 50-100 users |
| XL | 16 | Large-scale processing, ML training, 100+ users |
| 2XL | 32 | Very large workloads |
| 3XL | 64 | Enterprise scale |
| 4XL | 128 | Maximum scale |

**Multi-cluster:** Same credits × number of clusters running

**Gen2 Warehouses (Preview):** ~35% more credits but faster performance

**Interactive Warehouses:** 0.6× credits (XS = 0.6 cr/hr) - for sub-second latency

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Number of concurrent users | Determines if multi-cluster needed |
| Query complexity (simple/moderate/complex) | Impacts warehouse size |
| Data volume processed daily | Impacts runtime |
| BI tool & mode (Import/DirectQuery/Mixed) | DirectQuery = more concurrent queries |
| Operating hours per day | Runtime calculation |
| Operating days (weekday vs 7-day) | Runtime calculation |
| SLA requirements (response time) | May require larger WH or multi-cluster |

### Discovery Questions
1. How many users will query Snowflake concurrently at peak?
2. What BI tool(s) will connect? Import mode, DirectQuery, or mixed?
3. What are the typical query patterns? (simple lookups vs complex aggregations)
4. How many hours per day will the warehouse be active?
5. Is this weekday-only or 7-day operation?
6. What query response time is acceptable? (<5s, <30s, <1min)
7. Do you need separate warehouses for different workloads? (BI vs ETL vs dev)

### Gotchas
- ⚠️ **DirectQuery with >15 users requires multi-cluster** - single WH will queue
- ⚠️ **Auto-suspend minimum 60 seconds** - factor into cost for bursty workloads
- ⚠️ **Multi-cluster scaling policy matters** - Standard vs Economy affects cost
- ⚠️ **Cloud services can exceed 10% threshold** - metadata-heavy queries cost extra

---

## 2. Ingestion - Openflow (CDC/ETL)

### Pricing Model

**SPCS Deployment (Snowflake-hosted):**
| Component | Pricing |
|-----------|---------|
| Management node | 1× CPU_X64_S (0.11 cr/hr) - always on |
| Runtime nodes | Based on connector size (S/M/L) |
| Small runtime | CPU_X64_S (0.11 cr/hr) - 3 can pack on 1 node |
| Medium runtime | CPU_X64_M (0.22 cr/hr) |
| Large runtime | CPU_X64_L (0.83 cr/hr) |
| Warehouse | For merge operations (minimal) |

**BYOC Deployment (Bring Your Own Cloud):**
- 0.0225 credits per vCPU per hour
- Customer pays cloud infra separately

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Number of source servers/instances | Determines connector count |
| Database sizes (snapshot) | Initial load sizing |
| Number of tables per source | Connector complexity |
| Daily CDC volume (GB/day) | Runtime sizing |
| Latency requirement | Determines sync frequency |
| Source types (PostgreSQL, MySQL, SQL Server, etc.) | Connector availability |
| Cloud region | Network costs |
| Deployment preference (SPCS vs BYOC) | Pricing model |

### Discovery Questions
1. How many database server instances need to connect?
2. What is the total size of each database (snapshot)?
3. How many tables need replication per database?
4. What is the estimated daily change volume (CDC)?
5. What latency is acceptable? (real-time, <1min, <5min, <1hr)
6. What are the exact database versions? (PostgreSQL 14+, MySQL 8+, etc.)
7. Do you need DDL replication (schema changes)?
8. Any special data types? (JSON, arrays, geospatial)
9. Which AWS/Azure/GCP region are sources located?
10. Preference: Snowflake-hosted (SPCS) or your own cloud (BYOC)?

### Calculator Inputs (Official Questionnaire)
| Question | Options |
|----------|---------|
| Total source data volume (snapshot) | <10GB / 10-100GB / 100GB-1TB / 1-10TB / 10-50TB / 50+TB |
| Daily peak throughput | <1GB / 1-10GB / 10-100GB / 100-500GB |
| Daily CDC volume (all sources) | <1GB / 1-10GB / 10-100GB / 100GB-1TB / 1+TB/day |

### Gotchas
- ⚠️ **Pricing is per SERVER, not per database** - 4 DBs on 1 server = 1 connection
- ⚠️ **No Airtable connector** - need alternative (Estuary, Airbyte, custom)
- ⚠️ **Runtime packing matters** - 2 Small + 1 Medium = 2 nodes, not 3
- ⚠️ **Management node always runs** - minimum cost even with no data flowing
- ⚠️ **Cloud Services charges** - tuned but can accumulate on very active deployments
- ⚠️ **Chaining with Dynamic Tables** - can cause cloud services cost explosion (75K case)

### Internal Resources
- Calculator: `go/openflow-pricing-calc`
- Slack: `#ask-openflow-team`

---

## 3. Ingestion - Snowpipe & Snowpipe Streaming

### Pricing Model
| Feature | Rate | Unit |
|---------|------|------|
| Snowpipe | 0.0037 credits | per GB ingested |
| Snowpipe Streaming | 0.0037 credits | per uncompressed GB |
| Automated Refresh | 0.06 credits | per 1000 files |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Daily data volume (GB) | Direct cost driver |
| Number of files per day | Automated refresh cost |
| File sizes | Small files = more overhead |
| Compression ratio | Streaming uses uncompressed |

### Discovery Questions
1. How much data will be ingested daily (GB)?
2. How many files per day?
3. Average file size?
4. Is data compressed? What ratio?
5. Continuous streaming or micro-batch?

### Gotchas
- ⚠️ **Many small files = expensive** - consolidate if possible
- ⚠️ **Streaming uses uncompressed size** - multiply compressed size by ratio

---

## 4. OLTP - Snowflake Postgres

### Pricing Model

**Compute ($/hour by instance family):**
| Instance | AWS | AWS HA | Azure | Azure HA |
|----------|-----|--------|-------|----------|
| STANDARD_M | 0.0356 | 0.0712 | 0.0376 | 0.0752 |
| STANDARD_L | 0.0712 | 0.1424 | 0.0752 | 0.1504 |
| STANDARD_XL | 0.1424 | 0.2848 | 0.1504 | 0.3008 |
| STANDARD_2X | 0.2848 | 0.5696 | 0.3008 | 0.6016 |
| HIGHMEM_L | 0.1024 | 0.2048 | 0.1088 | 0.2176 |
| BURST_S | 0.0136 | 0.0272 | 0.0144 | 0.0288 |

**Storage ($/TB/month):**
| Region | Standard | High Availability |
|--------|----------|-------------------|
| AWS US East | 117.76 | 235.52 |
| AWS EU Dublin | 129.55 | 259.10 |
| AWS EU Frankfurt | 140.15 | 280.30 |

**Billing:** Per-second, minimum 1 minute

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Current database size | Storage cost |
| Current instance type (RDS/Aurora) | Mapping to SF Postgres |
| Current monthly cost | Comparison baseline |
| PostgreSQL version | Compatibility (16+ supported) |
| Extensions used | Compatibility check |
| Connection count (typical/peak) | Instance sizing |
| HA requirements | 2× cost for HA |
| Read replica needs | Additional instances |
| Region | Pricing varies |

### Discovery Questions

**Current State:**
1. What PostgreSQL version are you running?
2. What is the current database size?
3. What RDS/Aurora instance type? Current monthly cost?
4. Which AWS/Azure region?

**Compatibility:**
5. What PostgreSQL extensions do you use? (PostGIS, pg_vector, hstore, etc.)
6. Do you use Foreign Data Wrappers (FDW)?
7. Any stored procedures or triggers in PL/pgSQL?
8. Any RDS-specific features? (Performance Insights, etc.)

**Connectivity:**
9. How many applications connect to the database?
10. What drivers? (psycopg2, JDBC, etc.)
11. Do you use connection pooling? (PgBouncer, etc.)
12. Typical concurrent connections? Peak?

**Availability:**
13. Do you need High Availability (automatic failover)?
14. Do you need Read Replicas?
15. What's your RPO (acceptable data loss)?
16. What's your RTO (acceptable downtime)?
17. Do you use Point-in-Time Recovery?

### Features Available (Public Preview)
✅ PostgreSQL 16+ supported  
✅ Popular extensions (PostGIS, pg_vector)  
✅ Read replicas  
✅ High availability  
✅ Point-in-time recovery  
✅ Private Link  
✅ Customer-managed keys

### Gotchas
- ⚠️ **HA doubles compute AND storage cost**
- ⚠️ **Storage is ~5× more expensive than standard Snowflake storage**
- ⚠️ **Version compatibility** - check deprecated features if migrating from old PG
- ⚠️ **Data transfer costs** - replication to read replicas incurs transfer fees
- ⚠️ **No data transfer cost for HA secondaries**

### Internal Resources
- Slack: `#feat-snowflake-postgres`
- Docs: https://docs.snowflake.com/user-guide/snowflake-postgres/about

---

## 5. Serverless - Dynamic Tables

### Pricing Model
| Component | Multiplier/Rate |
|-----------|-----------------|
| Compute | Standard warehouse credits |
| Cloud Services | 1.0× multiplier |

**Key cost driver:** Target lag × number of DTs × data volume

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Number of Dynamic Tables | Scale factor |
| Target lag per DT | Refresh frequency |
| Data volume per refresh | Compute sizing |
| DT chaining depth | Cloud services impact |

### Discovery Questions
1. How many Dynamic Tables will you create?
2. What target lag is needed? (1min, 5min, 1hr, downstream)
3. How much data changes per refresh cycle?
4. Will DTs be chained (DT on top of DT)?
5. How many layers of chaining?

### Gotchas
- ⚠️ **CRITICAL: Chained DTs with short lag = cost explosion**
  - Real case: 2 layers × 200 DTs × 1min lag = $75K in 5 days cloud services
- ⚠️ **Cloud services charges accumulate** - checking for changes costs credits
- ⚠️ **Use separate warehouses** - isolate DT refresh from other workloads for visibility
- ⚠️ **Incremental vs full refresh** - full refresh on large tables is expensive

---

## 6. Serverless - Tasks

### Pricing Model
| Task Type | Compute Multiplier |
|-----------|-------------------|
| Serverless Tasks | 0.90× |
| Serverless Tasks Flex | 0.50× |
| Serverless Alerts | 0.90× |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Number of tasks | Scale factor |
| Execution frequency | Runtime calculation |
| Task duration | Compute sizing |
| Serverless vs warehouse-based | Pricing model |

### Discovery Questions
1. How many tasks will run?
2. How often does each task run? (every minute, hourly, daily)
3. How long does each task take?
4. Serverless or warehouse-based tasks?
5. Task dependencies (DAGs)?

### Gotchas
- ⚠️ **Flex tasks are 45% cheaper** but have lower SLA
- ⚠️ **Frequent short tasks** - minimum charge per execution adds up

---

## 7. Containers - SPCS

### Pricing Model (Credits/Hour)

**CPU Instances:**
| Family | Credits/Hr |
|--------|-----------|
| CPU_X64_XS | 0.06 |
| CPU_X64_S | 0.11 |
| CPU_X64_M | 0.22 |
| CPU_X64_SL | 0.41 |
| CPU_X64_L | 0.83 |

**High Memory:**
| Family | Credits/Hr |
|--------|-----------|
| HIGHMEM_X64_S | 0.28 |
| HIGHMEM_X64_M | 1.11 |
| HIGHMEM_X64_L | 4.44 |

**GPU:**
| Family | Credits/Hr |
|--------|-----------|
| GPU_NV_XS | 0.25 |
| GPU_NV_S | 0.57 |
| GPU_NV_M | 2.68 |
| GPU_NV_L | 14.12 |

**Block Storage:** ~$82/TB/month + IOPS + throughput

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Application type | CPU/GPU/memory requirements |
| Concurrent instances needed | Scale factor |
| Storage requirements | Block storage cost |
| Runtime hours (24/7 vs scheduled) | Compute hours |
| Auto-scaling requirements | Max instances |

### Discovery Questions
1. What type of application? (API, ML inference, Streamlit, etc.)
2. CPU, memory, or GPU intensive?
3. How many concurrent instances needed?
4. 24/7 or scheduled runtime?
5. Auto-scaling needed? Min/max instances?
6. Persistent storage requirements?

### Gotchas
- ⚠️ **SPCS is ~3× cheaper than equivalent warehouse compute** for suitable workloads
- ⚠️ **But not always better** - some workloads still more efficient on WH
- ⚠️ **Block storage costs add up** - especially with high IOPS
- ⚠️ **GPU availability** - not all regions, may have capacity limits

---

## 8. AI - Cortex AI Functions

### Pricing Model (Credits per 1M tokens)

**Cortex Complete (LLMs):**
| Model | Input | Output |
|-------|-------|--------|
| llama3.1-8b | 0.11 | 0.11 |
| llama3.3-70b | 0.36 | 0.36 |
| llama4-maverick | 0.12 | 0.49 |
| mistral-7b | 0.08 | 0.10 |
| mistral-large2 | 1.00 | 3.00 |
| claude-3-5-sonnet | 1.50 | 7.50 |
| claude-4-sonnet | 1.50 | 7.50 |
| claude-haiku-4-5 | 0.55 | 2.75 |
| openai-gpt-4.1 | 1.00 | 4.00 |
| openai-gpt-5 | 0.69 | 5.50 |
| snowflake-llama-3.3-70b | 0.29 | 0.29 |

**Other AI Functions:**
| Function | Rate | Unit |
|----------|------|------|
| AI_SENTIMENT | 1.60 | Credits/1M tokens |
| AI_SUMMARIZE | 0.10 | Credits/1M tokens |
| AI_TRANSLATE | 1.50 | Credits/1M tokens |
| AI_EMBED (arctic-embed-m) | 0.03 | Credits/1M tokens |
| AI_EMBED (multilingual-e5-large) | 0.05 | Credits/1M tokens |
| AI_PARSE_DOCUMENT (Layout) | 3.33 | Credits/1000 pages |
| AI_PARSE_DOCUMENT (OCR) | 0.50 | Credits/1000 pages |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Use case (summarization, classification, etc.) | Function selection |
| Model preference | Cost varies 10×+ |
| Volume (rows/documents to process) | Scale factor |
| Average text length | Token estimation |
| Frequency (one-time vs recurring) | Total volume |

### Discovery Questions
1. What AI capability do you need? (summarization, sentiment, classification, extraction, translation)
2. Any model preference or requirements?
3. How many rows/documents to process?
4. Average text length per record?
5. One-time processing or recurring?
6. Latency requirements?

### Gotchas
- ⚠️ **Model choice matters hugely** - llama3.1-8b is 50× cheaper than claude-opus
- ⚠️ **Output tokens often more expensive** than input (up to 5×)
- ⚠️ **Runaway queries** - long-running AI queries can accumulate fast
- ⚠️ **Use spending limits** - configure account/user limits
- ⚠️ **Prompt caching** - can reduce costs significantly for repeated patterns

---

## 9. AI - Cortex Search

### Pricing Model
| Component | Rate |
|-----------|------|
| Indexing | 6.30 credits per GB/month indexed |
| Queries | No per-query cost (included) |
| Warehouse | For initial indexing and refresh |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Document corpus size (GB) | Monthly index cost |
| Number of documents | Scale estimate |
| Refresh frequency | Re-indexing cost |
| Query volume | Not billed but affects WH |

### Discovery Questions
1. How large is the document corpus to index (GB)?
2. How many documents?
3. How often does the corpus change?
4. Expected query volume?

### Gotchas
- ⚠️ **Cost is per GB indexed, not per query** - large corpus = high fixed cost
- ⚠️ **Warehouse needed for indexing** - factor in initial and refresh compute
- ⚠️ **Real case: $0.20/question seemed high** - was actually fixed indexing cost misattributed

---

## 10. AI - Cortex Analyst / Semantic Views

### Pricing Model
| Component | Rate |
|-----------|------|
| Cortex Analyst (direct) | 67 credits per 1000 messages |
| Via Cortex Agents | Token-based (see Agents pricing) |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Expected messages/month | Direct cost |
| Number of users | Usage estimate |
| Questions per user per day | Volume calculation |

### Discovery Questions
1. How many users will use natural language queries?
2. How many questions per user per day expected?
3. Will this be via Snowflake Intelligence or API?

### Gotchas
- ⚠️ **67 credits/1000 messages is significant** - ~$200/1000 messages at $3/credit
- ⚠️ **Semantic model complexity doesn't affect cost** - flat per message
- ⚠️ **Consider Cortex Agents** - may be cheaper at high volume (token-based)

---

## 11. AI - Cortex Agents

### Pricing Model (Credits per 1M tokens)
| Model | Input | Output |
|-------|-------|--------|
| claude-3-5-sonnet | 1.88 | 9.41 |
| claude-4-sonnet | 1.88 | 9.41 |
| claude-haiku-4-5 | 0.69 | 3.45 |
| openai-gpt-4.1 | 1.38 | 5.52 |

**Note:** ~25% premium over direct Cortex Complete for orchestration

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Expected conversations/month | Volume |
| Average turns per conversation | Token estimation |
| Tools used (Search, Analyst) | Additional costs |
| Model preference | Cost varies |

### Discovery Questions
1. How many agent conversations per month?
2. Average conversation length (turns)?
3. What tools will the agent use? (Cortex Search, Cortex Analyst, custom)
4. Model requirements?

### Gotchas
- ⚠️ **Tool calls add separate costs** - Search indexing, Analyst messages
- ⚠️ **Long conversations accumulate context** - tokens grow per turn

---

## 12. Transformation - dbt Projects

### Pricing Model
- **Compute:** Standard warehouse credits for `dbt run`
- **Orchestration:** Snowflake Tasks (serverless 0.9× or warehouse-based)
- **No additional license fee** - dbt Core is open source

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Number of dbt models | Scale estimate |
| Model complexity (incremental vs full) | Runtime estimate |
| Data volume processed | Warehouse sizing |
| Run frequency | Total compute hours |
| Current Airflow cost | Comparison baseline |

### Discovery Questions

**Current State:**
1. What version of dbt Core?
2. How many dbt models?
3. How many sources?
4. What packages used? (dbt_utils, dbt_expectations, etc.)
5. Are tests in place?

**Orchestration:**
6. How is dbt currently orchestrated? (Airflow, cron, manual)
7. If Airflow - hosted how? (MWAA, self-hosted) What cost?
8. Run frequency? (hourly, daily, etc.)
9. Dependencies with non-dbt pipelines?

**Migration Interest:**
10. Interest in replacing Airflow with Snowflake Tasks?
11. Any blockers to migration?

### Gotchas
- ⚠️ **dbt Projects runs dbt Core** - same behavior, native execution
- ⚠️ **No cross-project dependencies yet** - unlike dbt Mesh
- ⚠️ **CI/CD changes needed** - use `snow dbt deploy` / `snow dbt execute`

### Internal Resources
- Quickstart: https://www.snowflake.com/en/developers/guides/dbt-projects-on-snowflake/
- Slack: `#feat-dbt-projects`

---

## 13. Storage

### Pricing Model ($/TB/month)

**Standard Storage:**
| Region | Storage | Time Travel | Failsafe |
|--------|---------|-------------|----------|
| AWS US East/West | 23.00 | 23.00 | 23.00 |
| AWS EU Dublin | 25.00 | 25.00 | 25.00 |
| AWS EU Frankfurt | 27.00 | 27.00 | 27.00 |
| Azure West Europe | 26.00 | 26.00 | 26.00 |

**Archive Storage (Preview):**
- Cool tier: $4-8/TB/mo + $30/TB retrieval
- Cold tier: $1-1.40/TB/mo + $2.50-8/TB retrieval

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Data volume (uncompressed) | Base calculation |
| Expected compression ratio | Actual storage |
| Time Travel retention | Overhead calculation |
| Failsafe (Enterprise+) | Additional overhead |
| Growth rate | Year 1-3 projection |

### Discovery Questions
1. What is the expected data volume (uncompressed)?
2. Data types? (structured, semi-structured, unstructured)
3. Compression expectations?
4. Time Travel retention needed? (1-90 days)
5. Annual growth rate?

### Gotchas
- ⚠️ **Compression varies widely** - 3-10× typical, verify with sample data
- ⚠️ **Time Travel + Failsafe** - can add 20-50% overhead
- ⚠️ **Clone storage** - only incremental changes count

---

## 14. Data Sharing & Marketplace

### Pricing Model
| Component | Cost |
|-----------|------|
| Sharing within region | Free (reader pays compute) |
| Cross-region replication | Replication credits (2× multiplier) + data transfer |
| Listing on Marketplace | Free to list |
| Auto-fulfillment | Compute for refresh checks |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Same region or cross-region? | Replication costs |
| Data volume shared | Transfer costs |
| Number of consumers | Scale |
| Refresh frequency | Replication compute |

### Discovery Questions
1. Provider or consumer?
2. Same region as consumers?
3. If cross-region, which regions?
4. Data volume to share?
5. Refresh frequency requirements?

### Gotchas
- ⚠️ **Cross-region is expensive** - replication + transfer
- ⚠️ **Multiple SSAs to same region** - single replication, not multiplied
- ⚠️ **Listing refresh checks** - can accumulate for live listings

---

## 15. Iceberg Tables

### Pricing Model
| Type | Storage | Compute |
|------|---------|---------|
| Snowflake-managed | Standard Snowflake rates | Standard warehouse |
| Externally-managed | Customer's object storage | Read = warehouse, Write = N/A |

### Required Inputs for Sizing
| Input | Why Needed |
|-------|-----------|
| Managed vs external | Pricing model |
| Data volume | Storage cost |
| Query patterns | Compute estimate |
| External catalog? (Glue, Polaris) | Integration complexity |

### Discovery Questions
1. Snowflake-managed or externally-managed Iceberg?
2. If external, which catalog? (AWS Glue, Polaris, Unity Catalog)
3. Read-only or read-write from Snowflake?
4. Data volume?
5. Query frequency?

### Gotchas
- ⚠️ **External Iceberg is read-heavy on catalog** - request charges apply
- ⚠️ **Auto-refresh for external tables** - can have latency and cost
- ⚠️ **Managed Iceberg** - full Snowflake features, external = limited

---

## Appendix: Credit Rates by Region

| Region | Standard | Enterprise | Business Critical |
|--------|----------|------------|-------------------|
| AWS US East | $2.00 | $3.00 | $4.00 |
| AWS EU London | $2.70 | $4.00 | $5.40 |
| AWS EU Frankfurt | $2.60 | $3.90 | $5.20 |
| Azure UK South | $2.70 | $4.00 | $5.40 |
| Azure West Europe | $2.60 | $3.90 | $5.20 |

---

## Appendix: Operating Days Reference

| Pattern | Days/Month | Days/Year |
|---------|------------|-----------|
| Weekdays only | 22 | 264 |
| 7-day operation | 30 | 365 |
| Business hours (8hr) | 22 × 8 = 176 hrs | 2,112 hrs |

---

*Last updated: March 2026*
*Source: Snowflake Service Consumption Table (January 2026)*
