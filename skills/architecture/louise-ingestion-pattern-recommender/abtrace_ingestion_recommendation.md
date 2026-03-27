# Abtrace - Snowflake Ingestion Pattern Recommendation

**Generated:** 2026-01-22  
**Customer:** Abtrace Limited  
**Analyst Confidence:** HIGH  

---

## Executive Summary

| Attribute | Value |
|-----------|-------|
| **Total Sources** | 4 (RDS Aurora MySQL, S3 Data Lake, Athena workloads, EHR APIs) |
| **Complexity Score** | MEDIUM-HIGH |
| **Estimated Effort** | 12-16 weeks |

### Complexity Rationale
While source technologies are standard and well-supported, complexity arises from:
1. Multi-tenant architecture with hundreds of tenants
2. Dual ingestion patterns (bulk + incremental)
3. ~1,000+ dbt models requiring migration
4. NHS compliance requirements
5. OLTP+OLAP workload consolidation

### Key Risks
1. Bulk historical load (billions of rows) requires careful staging and parallelization
2. Daily incremental reconciliation with deletions/updates needs CDC or merge patterns
3. dbt model migration from split systems (Athena + custom MySQL ETL) requires consolidation effort

### Critical Unknowns
1. Exact mechanism for data sync between RDS and S3/Athena today
2. Orchestration tooling (likely manual/cron - needs confirmation)
3. Power BI connection mode (Direct Query vs Import)
4. Tenant isolation model (schemas, row-level security, or column tagging)

---

## Source Systems Analysis

### Source 1: AWS RDS Aurora (MySQL)

| Attribute | Details |
|-----------|---------|
| **Category** | OLTP Database |
| **Confidence** | HIGH |
| **Technology** | AWS RDS Aurora MySQL |
| **Volume** | Terabytes; single writer instance |
| **CDC Status** | Unknown - needs discovery |

#### Extracted Pain Points
- "You have a single writer instance... single point of failure" - cannot scale writes
- "RDS is an OLTP database... it doesn't horizontally scale" - analytics workloads suffer
- "You need to trigger a failover of the cluster and things break at least temporarily"
- "Concurrency at the moment is MySQL which is suffering from that"
- Custom ETL system required because "dbt doesn't actually support MySQL"

---

### Source 2: AWS S3 (Data Lake)

| Attribute | Details |
|-----------|---------|
| **Category** | File Storage |
| **Confidence** | HIGH |
| **Technology** | AWS S3 |
| **Volume** | Terabytes (Parquet/columnar format) |
| **Velocity** | Batch - bulk loads + daily incremental |

#### Extracted Pain Points
- "Athena is not an ACID database... you cannot delete data from [S3 tables]"
- Data sync mechanism between RDS and S3 is unclear/manual

---

### Source 3: AWS Athena (Current Analytics Engine)

| Attribute | Details |
|-----------|---------|
| **Category** | OLAP Query Engine |
| **Confidence** | HIGH |
| **Technology** | AWS Athena (Presto-based) |
| **Infrastructure** | AWS shared cluster |

#### Extracted Pain Points
- "15 minute timeouts... query too complex... athena resources have been exhausted at this scale"
- "There's also a limit as to concurrency of the number of queries you can run. I think it's 24 or something"
- "It's a shared cluster... you cannot arbitrarily scale that up"
- "We're having to throttle it. We're having to spread it across periods of time"

---

### Source 4: EHR Integration (GP Practices)

| Attribute | Details |
|-----------|---------|
| **Category** | API / Healthcare Integration |
| **Confidence** | MEDIUM |
| **Volume** | Billions of rows (historical); daily incremental |
| **Velocity** | Bulk historical + daily 24hr cycle |

#### Extracted Pain Points
- "One off kind of gigantic bulk extract... potentially billions of rows"
- "Daily updates and deletions... every 24 hours"
- "You want to ideally massively scale out/up your ingestion capabilities when [bulk loading]"

---

## Requirements Summary

| Requirement | Description | Confidence |
|-------------|-------------|------------|
| **Latency** | Daily data freshness acceptable (24-hour cycle) | HIGH |
| **Query Speed** | "No spinners" - fast query response for users | MEDIUM |
| **Volume** | Terabyte-scale current; 2-3x growth in 12 months | HIGH |
| **Concurrency** | Hundreds of tenants, thousands of long-running queries | HIGH |
| **Compliance** | NHS IG; UK data residency mandatory | HIGH |
| **ACID** | Full ACID compliance for updates and deletions | HIGH |
| **Governance** | Role-based access control with tenant isolation | HIGH |
| **Audit** | Audit logging and access monitoring | HIGH |
| **Recovery** | Point-in-time recovery capability | HIGH |
| **BI Integration** | Power BI for customer-facing dashboards | HIGH |
| **Transformations** | dbt support for ~1,000+ models | HIGH |
| **Team Constraint** | ~12 engineers; no data engineer hires planned | HIGH |

---

## Ingestion Recommendations

### Source 1: RDS Aurora MySQL → Snowflake

#### Primary Method: Third-Party CDC Tool (Fivetran or Airbyte)

**Rationale:**
1. MySQL CDC requires log-based replication (binlog) for efficient change capture
2. Fivetran/Airbyte have mature MySQL connectors with proven reliability
3. Handles schema changes and deletions automatically
4. Minimal engineering overhead (aligns with "no data engineer hires" constraint)
5. More battle-tested than OpenFlow for complex CDC scenarios

**Grounding:** [Connectivity Reference: Third-Party ETL Partners], [Book: Chapter 5 - CDC Methods]

#### Alternative Methods

| Method | When to Use |
|--------|-------------|
| **Snowflake OpenFlow MySQL** | If cost optimization is critical and simple replication is sufficient |
| **AWS DMS to S3 + Snowpipe** | If already using AWS DMS; leverage existing infrastructure |

#### Supporting Snowflake Features

| Feature | Purpose | Documentation |
|---------|---------|---------------|
| **Streams** | Track CDC changes within Snowflake for incremental processing | [docs.snowflake.com/streams](https://docs.snowflake.com/en/user-guide/streams) |
| **Tasks** | Schedule incremental merge operations | [docs.snowflake.com/tasks](https://docs.snowflake.com/en/user-guide/tasks) |
| **Dynamic Tables** | Replace custom MySQL ETL with declarative pipelines | [docs.snowflake.com/dynamic-tables](https://docs.snowflake.com/en/user-guide/dynamic-tables) |

#### Implementation Notes
- Enable MySQL binlog (ROW format) if not already enabled
- Plan for initial bulk load of historical data (consider parallel table loads)
- Implement MERGE pattern for handling updates and deletes
- Custom MySQL ETL system can be retired and replaced with dbt on Snowflake

---

### Source 2: AWS S3 (Data Lake) → Snowflake

#### Primary Method: Snowpipe (Auto-ingest)

**Rationale:**
1. S3 event notifications trigger automatic ingestion (serverless)
2. Near real-time loading (minutes latency) for daily files
3. No infrastructure management
4. Native integration between AWS S3 and Snowflake
5. Parquet files are optimally handled with auto schema detection

**Grounding:** [Connectivity Reference: Snowpipe], [Book: Chapter 5 - File-based Sources]

#### Alternative Methods

| Method | When to Use |
|--------|-------------|
| **COPY INTO (Bulk)** | For initial historical bulk load of billions of rows |
| **External Tables with Iceberg** | If data must remain in S3 for other consumers |

#### Supporting Snowflake Features

| Feature | Purpose | Documentation |
|---------|---------|---------------|
| **Snowpipe** | Continuous serverless ingestion via SQS notifications | [docs.snowflake.com/snowpipe](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-auto-s3) |
| **Schema Detection** | Auto-infer schema from Parquet files | [docs.snowflake.com/copy-into](https://docs.snowflake.com/en/sql-reference/sql/copy-into-table) |
| **Storage Integration** | Secure IAM-based S3 access | [docs.snowflake.com/s3-config](https://docs.snowflake.com/en/user-guide/data-load-s3-config) |

#### Implementation Notes
- Create Storage Integration with IAM role for S3 access
- Configure S3 event notifications (SQS) for Snowpipe auto-ingest
- Use COPY INTO with ON_ERROR = CONTINUE for bulk loads
- Consider partitioning by tenant_id for query pruning

---

### Source 3: AWS Athena Workloads → Snowflake

#### Primary Method: Migration / Retirement (Not Ingestion)

**Rationale:**
Athena is a query engine over S3 data, not a source system. The migration approach is:
1. Ingest underlying S3 data to Snowflake (see Source 2)
2. Migrate dbt models from Athena adapter to Snowflake adapter
3. Retire Athena once queries are validated on Snowflake

**Grounding:** [Transcript: Pain points with Athena], [Book: Chapter 5 - OLAP Migration]

#### Supporting Snowflake Features

| Feature | Purpose | Documentation |
|---------|---------|---------------|
| **dbt Snowflake Adapter** | Migrate ~1,000+ models with minimal code changes | [docs.getdbt.com/snowflake](https://docs.getdbt.com/docs/core/connect-data-platform/snowflake-setup) |
| **Multi-cluster Warehouses** | Auto-scale for thousands of concurrent queries | [docs.snowflake.com/multicluster](https://docs.snowflake.com/en/user-guide/warehouses-multicluster) |
| **Query Acceleration** | Offload ad-hoc queries for faster performance | [docs.snowflake.com/qas](https://docs.snowflake.com/en/user-guide/query-acceleration-service) |

#### Implementation Notes
- dbt adapter change: `athena` → `snowflake` in profiles.yml
- Most SQL should be compatible; review Athena-specific functions
- Test complex joins that previously timed out on Athena
- No more 24-query concurrency limit or 15-minute timeouts

---

### Source 4: EHR Integration → Snowflake

#### Primary Method: Snowpipe (via S3 landing zone)

**Rationale:**
1. EHR data likely lands in S3 first (current architecture pattern)
2. Snowpipe provides automatic ingestion from S3 landing zone
3. Handles both bulk historical loads and daily incremental files
4. Serverless - scales automatically for billion-row bulk loads

**Grounding:** [Connectivity Reference: Snowpipe], [Book: Chapter 5 - API/File Patterns]

#### Alternative Methods

| Method | When to Use |
|--------|-------------|
| **Snowpipe Streaming** | If EHR integration evolves to near-real-time (currently daily batch) |
| **External Network Access + Python UDFs** | If direct API-to-Snowflake ingestion needed |

#### Supporting Snowflake Features

| Feature | Purpose | Documentation |
|---------|---------|---------------|
| **Time Travel** | Point-in-time recovery for healthcare data | [docs.snowflake.com/time-travel](https://docs.snowflake.com/en/user-guide/data-time-travel) |
| **Fail-safe** | Additional 7-day recovery for compliance | [docs.snowflake.com/failsafe](https://docs.snowflake.com/en/user-guide/data-failsafe) |
| **Access History** | NHS IG compliance - audit all data access | [docs.snowflake.com/access-history](https://docs.snowflake.com/en/user-guide/access-history) |

#### Implementation Notes
- Maintain current S3 landing zone pattern
- Use MERGE statements for handling updates and deletes
- Implement data retention policies aligned with NHS requirements
- Enable Access History for compliance audit trail

---

## Glean Validation Prompts

Run these prompts to verify feature availability before finalizing recommendations:

### Snowpipe
> What is the current status of Snowpipe in Snowflake? I need to know: (1) whether it's GA, Public Preview, or Private Preview, (2) if there are any regional availability restrictions or BYOC/BYOB limitations, and (3) any known issues, bugs, or limitations reported in the last 6 months.

### Dynamic Tables
> What is the current status of Dynamic Tables in Snowflake? I need to know: (1) whether it's GA, Public Preview, or Private Preview, (2) if there are any regional availability restrictions or BYOC/BYOB limitations, and (3) any known issues, bugs, or limitations reported in the last 6 months.

### Multi-cluster Warehouses
> What is the current status of Multi-cluster Warehouses in Snowflake? I need to know: (1) whether it's GA, Public Preview, or Private Preview, (2) if there are any regional availability restrictions or BYOC/BYOB limitations, and (3) any known issues, bugs, or limitations reported in the last 6 months.

### OpenFlow MySQL Connector
> What is the current status of the OpenFlow MySQL Connector in Snowflake? I need to know: (1) whether it's GA, Public Preview, or Private Preview, (2) if there are any regional availability restrictions or BYOC/BYOB limitations, and (3) any known issues, bugs, or limitations reported in the last 6 months.

### Access History
> What is the current status of Access History in Snowflake? I need to know: (1) whether it's GA, Public Preview, or Private Preview, (2) if there are any regional availability restrictions or BYOC/BYOB limitations, and (3) any known issues, bugs, or limitations reported in the last 6 months.

---

## Discovery Questions

### Critical Priority

| Source | Question | Purpose |
|--------|----------|---------|
| RDS MySQL | Is MySQL binary logging (binlog) enabled? If so, what format (ROW, STATEMENT, MIXED)? | Determines if log-based CDC is possible |
| RDS MySQL | How does data currently flow between RDS Aurora and S3/Athena? | Informs Snowflake ingestion design |
| RDS MySQL | What is the schema structure? Single database with tenant columns, separate schemas, or separate databases? | Affects access control design |

### High Priority

| Source | Question | Purpose |
|--------|----------|---------|
| S3 | What is the S3 bucket structure? How are files organized? | Informs Snowpipe configuration |
| EHR | What EHR system(s) do you integrate with? How does extraction work? | Clarifies connector requirements |
| EHR | For daily incremental loads, how are updates/deletes identified? | Determines MERGE strategy |
| General | What schedules and monitors your ETL jobs and dbt runs today? | Determines orchestration needs |
| Power BI | Is Power BI using Direct Query or Import mode? | Affects warehouse sizing |

### Medium Priority

| Source | Question | Purpose |
|--------|----------|---------|
| S3 | Are all files Parquet, or are there other formats? | Affects schema detection config |
| Cost | Of the £20-30K monthly spend, what % goes to RDS vs Athena vs S3? | Enables TCO comparison |
| Compliance | Beyond NHS IG, are there specific certifications required? | Validates Snowflake compliance |

---

## Complexity Assessment

| Factor | Value | Score |
|--------|-------|-------|
| Source Count | 4 sources | LOW |
| Source Diversity | 3 types (OLTP, files, API) | MEDIUM |
| Real-time Requirements | Daily batch acceptable | LOW |
| Schema Stability | Stable healthcare data model | LOW |
| Data Volume | Terabytes; 2-3x growth; billions of rows | MEDIUM-HIGH |
| CDC Availability | Unknown for MySQL | MEDIUM |
| Transformation Complexity | ~1,000+ dbt models across two systems | HIGH |
| Multi-tenancy | Hundreds of tenants with isolation | HIGH |
| Compliance | NHS IG, UK data residency, audit | MEDIUM |

**Overall Complexity: MEDIUM-HIGH**

---

## Risks and Mitigations

### High Severity

| Risk | Mitigation | Owner |
|------|------------|-------|
| **Bulk historical load** (billions of rows) may take extended time | Use parallel COPY with 4XL warehouse; schedule during low-usage windows; stage by tenant cohorts | Customer + Snowflake PS |
| **dbt model migration** from split systems requires significant effort | Inventory all models; prioritize critical models; plan 2-4 week parallel running | Customer Engineering |

### Medium Severity

| Risk | Mitigation | Owner |
|------|------------|-------|
| **CDC status unknown** for MySQL - may require binlog changes | Verify binlog early; fall back to timestamp-based CDC if unavailable | Customer DBA |
| **Tenant isolation model** unclear - affects access control design | Document current model; design RBAC + Row Access Policies early | Customer + Snowflake |
| **Power BI integration mode** unknown - may impact warehouse sizing | Clarify connection mode; size multi-cluster warehouse accordingly | Customer BI Team |

### Low Severity

| Risk | Mitigation | Owner |
|------|------------|-------|
| **Limited data engineering capacity** - relies on existing 12 engineers | Leverage Snowflake PS; use managed tools (Fivetran/Airbyte); implement in phases | Customer + Snowflake PS |

---

## Implementation Roadmap

### Phase 1: Foundation & S3 Ingestion (2-3 weeks)

**Sources:** S3 Data Lake

**Dependencies:**
- AWS IAM roles and permissions
- Snowflake account provisioned in AWS UK region
- S3 bucket inventory complete

**Deliverables:**
- Storage Integration configured
- Snowpipe pipelines for all S3 paths
- Initial historical data loaded
- Data validation queries comparing S3/Athena to Snowflake

---

### Phase 2: MySQL CDC Setup (2-4 weeks)

**Sources:** RDS Aurora MySQL

**Dependencies:**
- Phase 1 complete
- Binlog access verified
- CDC tool selected (Fivetran/Airbyte/OpenFlow)

**Deliverables:**
- CDC pipeline operational for MySQL → Snowflake
- Initial full sync complete
- Incremental replication validated
- MERGE patterns implemented

---

### Phase 3: dbt Migration (4-6 weeks)

**Sources:** Athena workloads

**Dependencies:**
- Phase 1 and 2 complete
- dbt model inventory complete
- Critical model list prioritized

**Deliverables:**
- dbt profiles.yml updated for Snowflake
- All ~1,000+ models migrated and tested
- Custom MySQL ETL retired
- Parallel running validation complete

---

### Phase 4: Governance & BI Cutover (2-3 weeks)

**Sources:** All

**Dependencies:**
- Phase 3 complete
- Tenant isolation model designed
- Power BI integration tested

**Deliverables:**
- RBAC and Row Access Policies implemented
- Access History and audit logging enabled
- Power BI connected to Snowflake
- Athena retired
- Production cutover complete

---

### Phase 5: Optimization & ML Enablement (2-4 weeks, ongoing)

**Dependencies:**
- Phase 4 complete
- Production workloads stable

**Deliverables:**
- Query performance optimization
- Multi-cluster warehouse tuning
- Snowpark setup for data science
- Cost monitoring and optimization

---

## Next Steps

### Immediate Actions
1. Schedule follow-up discovery call to answer critical questions (CDC status, tenant isolation, orchestration)
2. Verify Snowflake AWS UK region availability and provision trial account
3. Run Glean validation prompts to confirm feature status
4. Request S3 bucket inventory and sample files for schema analysis

### After Discovery
1. Create detailed POC plan with synthetic data generation approach
2. Design Snowflake architecture (databases, schemas, warehouses, RBAC)
3. Develop cost estimate based on confirmed volumes and query patterns
4. Build business case comparing current £20-30K/month to projected Snowflake costs
5. Prepare Technical Deep Dive demo (Jan 27, 2026) with ingestion patterns

---

## Quick Reference: Ingestion Pattern Summary

| Source | Primary Method | Alternative | Key Feature |
|--------|---------------|-------------|-------------|
| **RDS Aurora MySQL** | Fivetran/Airbyte CDC | OpenFlow MySQL, AWS DMS | Streams + Tasks |
| **AWS S3** | Snowpipe (Auto-ingest) | COPY INTO, External Tables | Storage Integration |
| **Athena Workloads** | Retire (migrate dbt) | Parallel running | Multi-cluster Warehouses |
| **EHR Integration** | Snowpipe via S3 | Snowpipe Streaming | Time Travel + Access History |

---

*Document based on Discovery Call (Dec 17, 2025) and Technical Discovery (Jan 21, 2026)*
