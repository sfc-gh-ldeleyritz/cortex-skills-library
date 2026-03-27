# Snowflake Features Complete Reference Guide
> **For Solution Engineers** | Comprehensive feature catalog to map customer pain points to Snowflake solutions

---

## Table of Contents
1. [Data Storage & Table Types](#1-data-storage--table-types)
2. [Data Loading & Ingestion](#2-data-loading--ingestion)
3. [Performance & Compute](#3-performance--compute)
4. [Data Engineering & Pipelines](#4-data-engineering--pipelines)
5. [Security & Authentication](#5-security--authentication)
6. [Data Governance (Snowflake Horizon)](#6-data-governance-snowflake-horizon)
7. [Data Sharing & Collaboration](#7-data-sharing--collaboration)
8. [AI & Machine Learning (Cortex AI)](#8-ai--machine-learning-cortex-ai)
9. [Application Development](#9-application-development)
10. [Business Continuity & Disaster Recovery](#10-business-continuity--disaster-recovery)
11. [Connectivity & Integration](#11-connectivity--integration)
12. [Cost Management & Monitoring](#12-cost-management--monitoring)
13. [Compliance & Certifications](#13-compliance--certifications)
14. [User Experience & Interfaces](#14-user-experience--interfaces)
15. [Migration Tools](#15-migration-tools)
16. [Quickstarts & Hands-On Labs](#16-quickstarts--hands-on-labs)

---

## Quick Reference: Pain Point to Feature Mapping

| Pain Point | Recommended Snowflake Features |
|------------|-------------------------------|
| Vendor lock-in concerns | Iceberg Tables, Horizon Catalog, pg_lake |
| Need for real-time analytics + transactions | Hybrid Tables (Unistore), Interactive Tables |
| Complex ETL orchestration | Dynamic Tables, dbt Projects, Tasks, Streams |
| Data silos across organizations | Secure Data Sharing, Open Table Format Sharing, Data Clean Rooms |
| PII/sensitive data protection | Data Masking, Row Access Policies, Classification, AI_REDACT |
| Unpredictable query performance | Query Acceleration Service, Search Optimization, Snowflake Optima |
| Scaling for concurrent users | Multi-Cluster Warehouses, Auto-Scaling |
| AI/ML infrastructure complexity | Cortex AI, ML Functions, Snowpark ML, Feature Store |
| Natural language analytics | Snowflake Intelligence, Cortex Analyst, Cortex Agents |
| Unstructured document processing | Document AI, Cortex AISQL, AI_PARSE_DOC |
| Disaster recovery requirements | Cross-Region Replication, Failover Groups, Backups |
| Cost control | Resource Monitors, Budgets, Auto-Suspend, Per-Second Billing |
| Real-time streaming ingestion | Snowpipe Streaming V2, Kafka Connector, Openflow |
| Multi-cloud strategy | Cross-Cloud Replication, Horizon Catalog |
| Building data applications | Native App Framework, Streamlit, SPCS, Vercel v0 |
| Data discovery and lineage | Horizon Catalog, Universal Search, External Lineage |
| Spark workload migration | Snowpark Connect for Apache Spark™ |
| PostgreSQL integration | Snowflake Postgres, pg_lake |
| Enterprise AI agents | Cortex Agents, Managed MCP Server, Snowflake Intelligence |
| AI spending control | AI token count, Cost guardrails, Budgets |

---

## 1. Data Storage & Table Types

### 1.1 Standard Tables
**Pain Point Solved:** Traditional data warehousing for structured analytics workloads  
**Description:** Standard Snowflake tables with automatic micro-partitioning, encryption at rest (AES-256), and built-in columnar storage.

### 1.2 Dynamic Tables
**Pain Point Solved:** Complex ETL pipeline management, maintaining materialized transformations  
**Description:** Tables that automatically refresh their contents based on a defined query and freshness settings (`TARGET_LAG`). Now supports Cortex AISQL in SELECT clauses.

### 1.3 Iceberg Tables (Apache Iceberg™)
**Pain Point Solved:** Vendor lock-in, open table format interoperability, multi-engine access  
**Description:** Open-source table format support combining Snowflake's performance with Iceberg semantics on external cloud storage.
- Snowflake-Managed Iceberg
- Externally-Managed Iceberg
- Catalog Linked Databases
- Iceberg V3 Support
- BCDR for Iceberg

### 1.4 Hybrid Tables (Unistore)
**Pain Point Solved:** Need for separate OLTP and OLAP systems  
**Description:** Special table type optimized for low-latency transactions and analytics. Supports secondary indexes and key constraints.

### 1.5 Interactive Tables (NEW)
**Pain Point Solved:** Sub-second analytics requiring low-latency and high concurrency  
**Description:** New "column-store + index" tables optimized for sub-second interactive queries.

### 1.6 External Tables
**Pain Point Solved:** Data residing in cloud storage that shouldn't be moved/copied  
**Description:** References to data files in external stages that can be queried as tables.

### 1.7 Materialized Views
**Pain Point Solved:** Slow complex queries, repetitive aggregations  
**Description:** Pre-computed views that store query results for faster reads.

### 1.8 Time Travel
**Pain Point Solved:** Accidental data deletion, point-in-time recovery  
**Description:** Data versioning that allows querying, cloning, or restoring historical data (up to 1-90 days).

### 1.9 Zero-Copy Cloning
**Pain Point Solved:** Environment duplication, testing without storage overhead  
**Description:** Create instant clones of databases/schemas/tables without copying underlying data.

---

## 2. Data Loading & Ingestion

### 2.1 Bulk Loading (COPY INTO)
**Pain Point Solved:** Large-scale batch data ingestion  
**Description:** Load data from staged files into Snowflake tables efficiently.

### 2.2 Snowpipe
**Pain Point Solved:** Near real-time data ingestion, continuous loading  
**Description:** Continuous ingestion service for near-real-time loading. 50-68% improvement in latency.

### 2.3 Snowpipe Streaming V2 (NEW Architecture)
**Pain Point Solved:** True real-time streaming, sub-second latency requirements  
**Description:** Serverless streaming ingestion with:
- Up to 10GB/s per table throughput
- <10 second end-to-end ingest-to-query latency
- New REST interface
- Java, Python SDK, or REST API

### 2.4 Kafka Connector
**Pain Point Solved:** Integration with Apache Kafka ecosystems  
**Description:** Kafka Connect sink for continuous data writing to Snowflake.

### 2.5 Snowflake Openflow
**Pain Point Solved:** Complex data integration from multiple sources  
**Description:** Open, extensible, managed data integration service with connectors for Oracle, SAP, and more.

### 2.6 Schema Detection & Evolution
**Pain Point Solved:** Schema management for incoming data  
**Description:** Automatically evolve table schemas during loading.

---

## 3. Performance & Compute

### 3.1 Virtual Warehouses
**Pain Point Solved:** Scalable compute, workload isolation  
**Description:** Elastic compute clusters (X-Small to 6X-Large). Per-second billing with 60-second minimum.

### 3.2 Multi-Cluster Warehouses
**Pain Point Solved:** Concurrency scaling, handling variable user loads  
**Description:** Auto-scale horizontally when concurrent load increases.

### 3.3 Interactive Warehouses (NEW)
**Pain Point Solved:** Low-latency analytics requiring always-on performance  
**Description:** New warehouse type for ultra-low latency, on-demand workloads.

### 3.4 Snowpark-Optimized Warehouses
**Pain Point Solved:** Memory-intensive ML training  
**Description:** Single-node warehouse with 16x more memory and 10x larger cache.

### 3.5 Query Acceleration Service (QAS)
**Pain Point Solved:** Unpredictable query performance  
**Description:** On-demand service that accelerates long-running queries. Now supports DELETE, UPDATE, MERGE.

### 3.6 Search Optimization Service (SOS)
**Pain Point Solved:** Point lookup queries, selective filters  
**Description:** Search indexes for equality predicates, LIKE patterns, geospatial, and VARIANT fields.

### 3.7 Snowflake Optima (NEW)
**Pain Point Solved:** Manual query tuning  
**Description:** ML-driven query optimization layer. Automatically applied on Gen2 warehouses.

### 3.8 Gen2 (Next-Gen) Warehouses
**Pain Point Solved:** Performance demands  
**Description:** ~2x faster analytics performance on latest hardware.

---

## 4. Data Engineering & Pipelines

### 4.1 Streams (Change Data Capture)
**Pain Point Solved:** CDC implementation, tracking table changes  
**Description:** Capture inserts/updates/deletes on tables for subsequent processing.

### 4.2 Tasks
**Pain Point Solved:** Job scheduling, workflow orchestration  
**Description:** Serverless compute for scheduling SQL operations. Can form DAGs.

### 4.3 Serverless Tasks
**Pain Point Solved:** Task compute management  
**Description:** Snowflake-managed tasks (~35% cost savings).

### 4.4 Triggered Tasks
**Pain Point Solved:** Event-based pipeline execution  
**Description:** Tasks that run immediately in response to stream data.

### 4.5 Dynamic Tables
**Pain Point Solved:** ETL orchestration complexity  
**Description:** Materialized, auto-refreshing tables (see Storage section).

### 4.6 dbt Projects on Snowflake (NEW)
**Pain Point Solved:** Managing dbt infrastructure separately  
**Description:** Native support for running dbt Core projects within Snowflake.

### 4.7 Snowpark Connect for Apache Spark™ (NEW)
**Pain Point Solved:** Migrating Spark workloads  
**Description:** Run PySpark code on Snowflake compute. 5.6x faster, 41% TCO savings.

### 4.8 Stored Procedures
**Pain Point Solved:** Complex business logic  
**Description:** Procedural logic in SQL, JavaScript, Python, Java, or Scala.

### 4.9 User-Defined Functions (UDFs)
**Pain Point Solved:** Custom transformations  
**Description:** Scalar, tabular (UDTFs), and aggregate (UDAFs) functions.

### 4.10 Snowpark
**Pain Point Solved:** DataFrame-based programming  
**Description:** Developer framework (Java, Scala, Python) with:
- DataFrame API
- Snowpark pandas API (NEW)
- Snowpark ML API
- Snowpark Local Testing

---

## 5. Security & Authentication

### 5.1 Authentication Methods
- Username/Password
- Multi-Factor Authentication (MFA)
- Federated Authentication/SSO (SAML 2.0)
- OAuth
- Key-Pair Authentication
- SCIM
- Programmatic Access Tokens (PAT)
- Workload Identity Federation (WIF)

### 5.2 Network Policies & Rules
**Pain Point Solved:** Network isolation  
**Description:** IP allowlists/denylists for account access.

### 5.3 Role-Based Access Control (RBAC)
**Pain Point Solved:** Permission management  
**Description:** Privileges granted to roles, which are assigned to users.

### 5.4 Column-Level Security (Dynamic Data Masking)
**Pain Point Solved:** Protecting sensitive data  
**Description:** Masking policies hide sensitive column data at query time.

### 5.5 Row Access Policies
**Pain Point Solved:** Multi-tenant data isolation  
**Description:** Row-level security via policies that filter visible rows.

### 5.6 Object Tagging
**Pain Point Solved:** Metadata management  
**Description:** Assign metadata tags to Snowflake objects.

### 5.7 Tag-Based Masking
**Pain Point Solved:** Scaling masking policies  
**Description:** Assign masking policy to a tag for automatic protection.

### 5.8 Encryption
- At Rest: AES-256 with auto key rotation
- In Transit: TLS 1.2+
- Tri-Secret Secure (Business Critical+)
- End-to-End Encryption

### 5.9 Privacy Policies (Differential Privacy)
**Pain Point Solved:** Privacy-preserving analytics  
**Description:** Differential privacy support with epsilon budgets.

---

## 6. Data Governance (Snowflake Horizon)

### 6.1 Snowflake Horizon Catalog
**Pain Point Solved:** Unified governance  
**Description:** Unified data catalog with automated discovery, access tracking, lineage, profiling.

### 6.2 Sensitive Data Classification
**Pain Point Solved:** PII discovery  
**Description:** Automatically analyze and tag columns containing PII.

### 6.3 Data Security Posture Management (NEW)
**Pain Point Solved:** Sensitive data visibility  
**Description:** Automatic detection, tagging, and protection of sensitive data.

### 6.4 AI_REDACT (NEW)
**Pain Point Solved:** Preparing unstructured data for AI  
**Description:** LLM-powered PII detection and replacement.

### 6.5 Access History
**Pain Point Solved:** Audit requirements  
**Description:** Detailed audit of every DML/DDL statement.

### 6.6 Data Lineage
**Pain Point Solved:** Understanding data flow  
**Description:** Built-in lineage tracking in Snowsight.

### 6.7 External Lineage (NEW)
**Pain Point Solved:** Non-Snowflake object lineage  
**Description:** Ingest lineage from external tools via OpenLineage.

### 6.8 Data Quality Monitoring
**Pain Point Solved:** Data quality issues  
**Description:** Data Metric Functions (DMFs) for automated quality checks.

### 6.9 Immutable Backups (WORM)
**Pain Point Solved:** Compliance, cyber resilience  
**Description:** Point-in-time copies with optional immutability locks.

---

## 7. Data Sharing & Collaboration

### 7.1 Secure Data Sharing
**Pain Point Solved:** Data collaboration without copying  
**Description:** Share datasets across accounts via Snowflake Shares.

### 7.2 Open Table Format Sharing (NEW)
**Pain Point Solved:** Sharing in open formats  
**Description:** Share Iceberg and Delta Lake tables with zero-ETL.

### 7.3 Declarative Sharing (NEW)
**Pain Point Solved:** Complex sharing setup  
**Description:** Simplified sharing with Native App Framework.

### 7.4 Snowflake Marketplace / Data Exchange
**Pain Point Solved:** Third-party data acquisition  
**Description:** Discover and subscribe to third-party data products.

### 7.5 Data Clean Rooms
**Pain Point Solved:** Privacy-preserving collaboration  
**Description:** Analyze combined datasets without exposing raw data.

### 7.6 Cross-Cloud Auto-Fulfillment
**Pain Point Solved:** Sharing across regions and clouds  
**Description:** Automated fulfillment regardless of location.

---

## 8. AI & Machine Learning (Cortex AI)

### 8.1 Snowflake Intelligence (NEW)
**Pain Point Solved:** AI-powered insights for business users  
**Description:** Ready-to-run conversational AI application at ai.snowflake.com.

### 8.2 Cortex LLM Functions
**Pain Point Solved:** Text analysis and generation  
- COMPLETE(): Text generation, summarization, Q&A
- SUMMARIZE(): Document summarization
- TRANSLATE(): 25+ languages
- EXTRACT_ANSWER(): Question answering
- SENTIMENT(): Sentiment analysis

### 8.3 Cortex AISQL Functions
**Pain Point Solved:** AI on multimodal data  
- AI_COMPLETE(), AI_CLASSIFY(), AI_EXTRACT()
- AI_PARSE_DOC(), AI_SENTIMENT(), AI_TRANSLATE()
- AI_TRANSCRIBE(), AI_REDACT(), AI_OCR()

### 8.4 Cortex Agents API
**Pain Point Solved:** Enterprise AI agents  
**Description:** Managed AI agents with robust reasoning. REST API interface.

### 8.5 Managed MCP Server (NEW)
**Pain Point Solved:** Exposing Snowflake AI externally  
**Description:** Turn internal AI into secure MCP endpoint for external agents.

### 8.6 Cortex Analyst
**Pain Point Solved:** Natural language to SQL  
**Description:** Chat interface converting natural language to SQL queries.

### 8.7 Cortex Search
**Pain Point Solved:** Semantic search  
**Description:** Hybrid lexical and vector search over text data.

### 8.8 Vector Search & Embeddings
**Pain Point Solved:** Similarity search for AI  
**Description:** VECTOR type with similarity search (VECTOR_SIMILARITY()).

### 8.9 Cortex Fine-Tuning
**Pain Point Solved:** Customizing LLMs  
**Description:** Fine-tune supported LLMs on your data.

### 8.10 Document AI
**Pain Point Solved:** Extracting from unstructured documents  
**Description:** Extract structured data from PDFs, images, scanned documents.

### 8.11 ML Functions (AutoML)
- FORECAST(): Time-series forecasting
- ANOMALY_DETECTION(): Outlier identification
- CONTRIBUTION_EXPLORER(): Metric change explanation
- CLASSIFICATION(): Binary/multi-class
- REGRESSION(): Numeric prediction
- Model Explainability: Shapley values

### 8.12 Feature Store
**Pain Point Solved:** ML feature management  
**Description:** Centralized repository with versioning and serving.
- Online Feature Serving (NEW): <50ms P90 latency

### 8.13 Model Registry
**Pain Point Solved:** ML model versioning and deployment  
**Description:** Store, version, and deploy ML models.

### 8.14 Experiment Tracking (NEW)
**Pain Point Solved:** ML experiment management  
**Description:** Track and compare model training experiments.

### 8.15 One-Click Hugging Face Deployment (NEW)
**Pain Point Solved:** Deploying pre-trained models  
**Description:** Instant access to HuggingFace models from Snowsight.

### 8.16 Snowpark ML
**Pain Point Solved:** ML development with familiar APIs  
**Description:** Framework for building and deploying ML models.
- Feature-level RBAC (NEW)
- Model-level RBAC (NEW)

### 8.17 Notebooks
**Pain Point Solved:** Interactive data exploration  
**Description:** Collaborative notebooks in Snowsight with Git integration.

---

## 9. Application Development

### 9.1 Snowflake Native Apps
**Pain Point Solved:** Building data applications  
**Description:** Package code, data, and UI together. Distribute via Marketplace.

### 9.2 Snowpark Container Services (SPCS)
**Pain Point Solved:** Running containerized workloads  
**Description:** Deploy Docker containers within Snowflake. GPU support for ML.
- Advanced Autoscaling & Auto-Suspension
- Stage Mount Improvements
- Per-Placement Group Compute Pools
- TSS for Block Storage

### 9.3 Streamlit in Snowflake
**Pain Point Solved:** Building data apps without frontend expertise  
**Description:** Develop Streamlit apps directly in Snowflake UI.

### 9.4 Vercel v0 Integration (NEW - Preview)
**Pain Point Solved:** Rapid AI-powered app development  
**Description:** Vibe code apps that deploy to SPCS.

### 9.5 Snowflake CLI (SnowCLI)
**Pain Point Solved:** Command-line automation  
**Description:** Modern CLI for apps, Streamlit, Snowpark, SPCS, SQL, dbt.

### 9.6 Snowflake Workspaces
**Pain Point Solved:** Disorganized code  
**Description:** Centralized file-based environment with version control.

### 9.7 Git Integration
**Pain Point Solved:** Version control  
**Description:** Connect to GitHub, GitLab, Bitbucket.

### 9.8 Cortex Code (NEW - Preview)
**Pain Point Solved:** AI assistance for coding  
**Description:** AI assistant for coding and investigating Snowflake infrastructure.

---

## 10. Business Continuity & Disaster Recovery

### 10.1 Cross-Region Replication
**Pain Point Solved:** Geographic redundancy  
**Description:** Replicate to accounts in different regions.

### 10.2 Cross-Cloud Replication
**Pain Point Solved:** Multi-cloud strategy  
**Description:** Replicate across AWS, Azure, and GCP.

### 10.3 Failover & Failback
**Pain Point Solved:** Minimizing RTO  
**Description:** Promote replica to primary during outages.

### 10.4 Client Redirect
**Pain Point Solved:** Seamless application failover  
**Description:** Redirect connections within 30-45 seconds.

### 10.5 Failover Groups & Account Replication
**Pain Point Solved:** Coordinated replication  
**Description:** Group databases, shares, users, roles, warehouses for coordinated replication.

---

## 11. Connectivity & Integration

### 11.1 Connectors & Drivers
- Python, JDBC/ODBC, Node.js, Go, .NET
- Spark, Kafka, BI Connectors

### 11.2 Native Connectors
- ServiceNow, Google Analytics
- SAP (NEW), Salesforce (Coming), Workday (Coming)

### 11.3 External Network Access
**Pain Point Solved:** Calling external APIs  
**Description:** Network rules for outbound connections.

### 11.4 Private Connectivity
- AWS PrivateLink
- Azure Private Link
- GCP Private Service Connect

### 11.5 Federated Queries (Postgres)
**Pain Point Solved:** Querying external databases  
**Description:** Query Postgres/Redshift like tables (Preview).

---

## 12. Cost Management & Monitoring

### 12.1 Resource Monitors
**Pain Point Solved:** Cost control  
**Description:** Credit usage thresholds with alerts or suspends.

### 12.2 Budgets
**Pain Point Solved:** Cost forecasting  
**Description:** Spending limits for compute costs.

### 12.3 Query Profiling
**Pain Point Solved:** Performance troubleshooting  
**Description:** Detailed metrics per query.

### 12.4 Per-Second Billing
**Pain Point Solved:** Cost efficiency  
**Description:** 60-second minimum, then per-second.

### 12.5 Auto-Suspend & Auto-Resume
**Pain Point Solved:** Forgetting to pause warehouses  
**Description:** Automatic suspend after idle period.

### 12.6 Well-Architected Framework (NEW)
**Pain Point Solved:** Best practices  
**Description:** Five pillars: security, operations, reliability, performance, cost.

---

## 13. Compliance & Certifications

- SOC 1, SOC 2, SOC 3
- ISO 27001
- PCI DSS
- HIPAA/HITRUST (Business Critical+)
- FedRAMP Moderate & High
- StateRAMP
- ITAR
- IRAP

---

## 14. User Experience & Interfaces

### 14.1 Snowsight
Modern web UI with SQL Worksheets, dashboards, data profiling, lineage.

### 14.2 SnowSQL CLI
Interactive command-line client.

### 14.3 VS Code / IDE Integration
Snowflake SQL and Snowpark plugins.

---

## 15. Migration Tools

### 15.1 SnowConvert AI
**Pain Point Solved:** Migrating from legacy databases  
**Description:** AI-powered migration with:
- AI-Powered Code Verification & Repair
- Automated Data Validation
- Full Ecosystem Migration (SSIS, Informatica → dbt)
- PowerBI and Tableau repointing

### 15.2 pg_lake (NEW)
**Pain Point Solved:** PostgreSQL to lakehouse  
**Description:** Open source PostgreSQL extensions for Iceberg.

### 15.3 Snowflake Postgres (NEW - Preview)
**Pain Point Solved:** Consolidating OLTP and analytics  
**Description:** Fully-managed PostgreSQL for AI agent applications.

---

## Snowflake Editions Comparison

| Feature | Standard | Enterprise | Business Critical | VPS |
|---------|----------|------------|-------------------|-----|
| Time Travel | 1 day | 90 days | 90 days | 90 days |
| Fail-Safe | ❌ | 7 days | 7 days | 7 days |
| Multi-cluster Warehouses | ❌ | ✅ | ✅ | ✅ |
| Materialized Views | ❌ | ✅ | ✅ | ✅ |
| Dynamic Data Masking | ❌ | ✅ | ✅ | ✅ |
| Row Access Policies | ❌ | ✅ | ✅ | ✅ |
| Data Sharing | ✅ | ✅ | ✅ | ✅ |
| Tri-Secret Secure | ❌ | ❌ | ✅ | ✅ |
| Private Connectivity | ❌ | ❌ | ✅ | ✅ |
| Database Failover | ❌ | ❌ | ✅ | ✅ |
| HIPAA/HITRUST Support | ❌ | ❌ | ✅ | ✅ |
| Dedicated Resources | ❌ | ❌ | ❌ | ✅ |

---

*Document Version: January 2026 (BUILD November 2025 Edition)*
