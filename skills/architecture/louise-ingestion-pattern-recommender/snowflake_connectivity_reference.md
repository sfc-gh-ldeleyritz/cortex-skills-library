# Snowflake Connectivity & Data Ingestion Complete Reference Guide
> **For Solution Engineers** | Comprehensive connectivity catalog to recommend the optimal data ingestion method

---

## IMPORTANT: Understanding Snowflake's Native Data Integration Options

Snowflake offers **TWO categories** of first-party data integration:

### 1. OpenFlow Connectors (New - 2025+)
**What it is:** Snowflake's new fully-managed data integration platform built on **Apache NiFi**. Managed NiFi flows running as cloud services.

⚠️ **CAUTION: OpenFlow is relatively new (Summit 2025)**
> - Some OpenFlow connectors are still in **Preview** or **Private Preview** - only recommend GA connectors for production
> - Early adopters have reported issues with some connectors - always verify current reliability
> - Check release notes and community feedback before recommending specific OpenFlow connectors
> - When in doubt, proven third-party tools (Fivetran, Airbyte) may be safer for critical workloads

### 2. Native Connectors (Established)
**What it is:** Snowflake-built connectors that exist **separately from OpenFlow** - these include the Google Analytics connectors, ServiceNow connector, Kafka connector, etc. These are more established and battle-tested.

### Key Distinction
| Category | Examples | Maturity | Recommendation |
|----------|----------|----------|----------------|
| **OpenFlow** | MySQL, PostgreSQL, SQL Server, Marketing Ads, Box, Slack | Newer (2025) | ✅ Consider if **GA** and verified working |
| **Native Connectors** | Google Analytics, ServiceNow, Kafka HP, Power Platform | Established | ✅ Generally safe to recommend |
| **Third-Party ETL** | Fivetran, Airbyte, Matillion | Mature | ✅ Safe fallback, especially for critical workloads |

### When to Recommend Snowflake-Native Options
- ✅ The specific connector is **GA** (not Preview/Private Preview)
- ✅ You have verified the connector is working reliably for the use case
- ✅ Customer wants to minimize third-party dependencies
- ✅ Customer is comfortable with newer technology (for OpenFlow)

### When to Recommend Third-Party ETL Instead
- ⚠️ The OpenFlow connector is in Preview or has known issues
- ⚠️ Mission-critical workload requiring proven reliability
- ⚠️ Source system not covered by any native option
- ⚠️ Customer needs complex transformations or governance features

---

## Table of Contents
1. [OpenFlow Connectors (New)](#1-openflow-connectors-new)
2. [Native Snowflake Connectors (Established)](#2-native-snowflake-connectors-established)
3. [Bulk Data Loading](#3-bulk-data-loading)
4. [Continuous Data Loading (Streaming)](#4-continuous-data-loading-streaming)
5. [Third-Party ETL/ELT Partners](#5-third-party-etlelt-partners)
6. [Programmatic Interfaces (Drivers & SDKs)](#6-programmatic-interfaces-drivers--sdks)
7. [Cloud Storage Integration](#7-cloud-storage-integration)
8. [Apache Kafka Integration](#8-apache-kafka-integration)
9. [Data Lake & Open Formats](#9-data-lake--open-formats)
10. [Transformation & Orchestration](#10-transformation--orchestration)
11. [Source System Coverage Matrix](#11-source-system-coverage-matrix)
12. [Decision Matrix](#12-decision-matrix)

---

## 1. OpenFlow Connectors (New - 2025+)

> ⚠️ **OpenFlow is Snowflake's newest integration platform. While promising, verify connector maturity and reliability before recommending for production workloads.**

### What is OpenFlow?

**OpenFlow is Snowflake's fully-managed data integration platform** (announced Summit 2025), built on **Apache NiFi**. It provides a unified way to move data from any source into Snowflake via curated, versioned NiFi flows.

📚 **Documentation:**
- [About OpenFlow](https://docs.snowflake.com/en/user-guide/data-integration/openflow/about)
- [OpenFlow Connectors List](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/about-openflow-connectors)
- [Getting Started with OpenFlow](https://www.snowflake.com/en/developers/guides/getting-started-with-openflow-spcs/)
- [OpenFlow Version History](https://docs.snowflake.com/en/user-guide/data-integration/openflow/version-history)

### OpenFlow Architecture: Control Plane vs Data Plane

OpenFlow uses a split-plane architecture:

| Component | Description | Where it runs |
|-----------|-------------|---------------|
| **Control Plane** | UI (Canvas), orchestration, monitoring, metadata | Snowflake-managed (Snowsight) |
| **Data Plane (SPCS)** | Execution engine, data processing | Snowpark Container Services (fully managed) |
| **Data Plane (BYOC)** | Execution engine for sensitive data | Customer's VPC (Bring Your Own Cloud) |

📚 **Docs:** [OpenFlow Deployment Options](https://docs.snowflake.com/en/user-guide/data-integration/openflow/setup-openflow-spcs-sf-allow-list)

### ⚠️ OpenFlow Maturity Guidance

| Status | Meaning | Recommendation |
|--------|---------|----------------|
| **GA** | Generally Available | ✅ Consider for production (verify reliability first) |
| **Preview** | Beta, may have issues | ⚠️ Test thoroughly, have fallback ready |
| **Private Preview** | Early access only | ❌ Do not recommend for production |

### OpenFlow Connector Inventory

#### Database CDC Connectors

| Connector | Source | Status | Key Features | Documentation |
|-----------|--------|--------|--------------|---------------|
| **OpenFlow for MySQL** | MySQL | 🆕 **GA** | Log-based CDC (binlog), no agent | [MySQL Connector Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/mysql/about) |
| **OpenFlow for PostgreSQL** | PostgreSQL | 🆕 **GA** | Log-based CDC (WAL), no agent | [PostgreSQL Connector Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/postgresql/about) |
| **OpenFlow for SQL Server** | SQL Server | 🆕 **GA** | Transaction log CDC | [SQL Server Connector Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/sqlserver/about) |
| **OpenFlow for Oracle** | Oracle | ⚠️ **Private Preview** | XStream API CDC (same as GoldenGate) | [Oracle Integration Blog](https://www.snowflake.com/en/blog/oracle-database-integration-connector/) |

#### Marketing & Advertising Connectors

| Connector | Source | Status | Key Features | Documentation |
|-----------|--------|--------|--------------|---------------|
| **OpenFlow for Amazon Ads** | Amazon Ads | 🆕 GA | Performance and spending data | [Amazon Ads Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/amazon-ads/about) |
| **OpenFlow for Google Ads** | Google Ads | 🆕 GA | Campaign metrics | [Google Ads Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-ads/about) |
| **OpenFlow for LinkedIn Ads** | LinkedIn Ads | 🆕 GA | Campaign performance | [LinkedIn Ads Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/linkedin-ads/about) |
| **OpenFlow for Meta Ads** | Facebook/Meta | 🆕 GA | Facebook/Instagram ads | [Meta Ads Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/meta-ads/about) |

#### Cloud Storage & Files Connectors (AI/RAG Ready)

| Connector | Source | Status | Key Features | Documentation |
|-----------|--------|--------|--------------|---------------|
| **OpenFlow for SharePoint** | SharePoint | 🆕 GA | Files + metadata, Cortex Search prep | [SharePoint Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/sharepoint/about) |
| **OpenFlow for Box** | Box | 🆕 GA | Content ingestion, vectorization ready | [Box Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/box/about) |
| **OpenFlow for Google Drive** | Google Drive | 🆕 GA | Content for AI/Cortex processing | [Google Drive Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-drive/about) |
| **OpenFlow for Google Sheets** | Google Sheets | 🆕 GA | Spreadsheet data to tables | [Google Sheets Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-sheets/about) |
| **OpenFlow for Slack** | Slack | 🆕 GA | Messages for enterprise search | [Slack Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/slack/about) |

#### Business Applications Connectors

| Connector | Source | Status | Key Features | Documentation |
|-----------|--------|--------|--------------|---------------|
| **OpenFlow for Salesforce** | Salesforce | 🆕 GA | Bulk API, case-sensitivity controls | [Salesforce Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/salesforce/about) |
| **OpenFlow for Workday** | Workday | 🆕 GA | Report-as-a-Service (RaaS) API | [Workday Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/workday/about) |
| **OpenFlow for Jira Cloud** | Atlassian Jira | 🆕 GA | Issues, projects, workflows | [Jira Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/jira/about) |
| **OpenFlow for Microsoft Dataverse** | Dynamics 365 | 🆕 GA | Power Platform data | [Dataverse Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/dataverse/about) |

#### Streaming Connectors

| Connector | Source | Status | Key Features | Documentation |
|-----------|--------|--------|--------------|---------------|
| **OpenFlow for Kafka** | Apache Kafka | 🆕 GA | Real-time events, in-flight transforms | [Kafka OpenFlow Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/kafka/about) |
| **OpenFlow for Kinesis** | AWS Kinesis | 🆕 GA | Stream event ingestion | [Kinesis Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/kinesis/about) |
| **Snowflake to Kafka** | Snowflake → Kafka | 🆕 GA | Reverse ETL: publish changes to Kafka | [Reverse Kafka Docs](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/snowflake-to-kafka/about) |

### OpenFlow Custom Processors

OpenFlow supports custom Python/Java processors for bespoke formats (HL7, SEG-Y, etc.):
📚 **Docs:** [Building Custom Processors](https://medium.com/@rahul.reddy.ai/building-custom-source-processors-for-snowflake-openflow-a-python-deep-dive-fd9367d2ff8f)

---

## 2. Native Snowflake Connectors (Established)

> ✅ **These are battle-tested connectors that exist SEPARATELY from OpenFlow.** They are generally safer to recommend for production workloads.

### What are Native Connectors?

Native Connectors are Snowflake-built integrations that predate OpenFlow and run as **Native Apps** or **Kafka Connect plugins**. They use the Native Apps Framework and are more mature.

📚 **Documentation:**
- [Native Connectors Hub](https://other-docs.snowflake.com/en/connectors)
- [Database Connector Concepts](https://docs.snowflake.com/en/connectors/db-connector-concepts)
- [Native SDK for Connectors](https://docs.snowflake.com/en/developer-guide/native-apps/connector-sdk/about-connector-sdk)

### Native Connector Inventory

| Connector | Source | Architecture | Status | Documentation |
|-----------|--------|--------------|--------|---------------|
| **Google Analytics (Aggregate)** | GA4 | Native App | ✅ GA | [GA Aggregate Docs](https://docs.snowflake.com/en/connectors/google/gaad/gaad-connector-about) |
| **Google Analytics (Raw)** | GA4 | Native App | ✅ GA | [GA Raw Docs](https://docs.snowflake.com/en/connectors/google/gard/gard-connector-about) |
| **ServiceNow (V2)** | ServiceNow | Native App | ✅ GA | [ServiceNow Docs](https://docs.snowflake.com/en/connectors/servicenow/about) |
| **HP Kafka Connector** | Kafka | Kafka Connect | ✅ GA | [Kafka Connector Docs](https://docs.snowflake.com/en/user-guide/kafka-connector) |
| **Google Looker Studio** | Looker Studio | Partner Connector | ✅ GA | [Looker Studio Docs](https://docs.snowflake.com/en/connectors/google-looker-studio-connector) |
| **Microsoft Power Platform** | Power Apps/Automate | REST Connector | ✅ GA | [Power Platform Docs](https://docs.snowflake.com/en/connectors/microsoft/powerapps/about) |
| **SharePoint (Legacy)** | SharePoint | Native App | Preview (deprecated) | [Legacy SharePoint Docs](https://docs.snowflake.com/en/connectors/unstructured-data-connectors/sharepoint/about) |

### When to Use Native Connectors vs OpenFlow

| Scenario | Recommendation |
|----------|----------------|
| Need Google Analytics data | ✅ Use **Native GA Connector** (established) |
| Need ServiceNow data | ✅ Use **Native ServiceNow Connector** (established) |
| Need Kafka streaming | ✅ Use **HP Kafka Connector** (established) |
| Need MySQL/PostgreSQL CDC | ⚠️ Consider **OpenFlow** (if GA) OR **third-party** (Fivetran, Airbyte) |
| Mission-critical workload | ✅ Prefer **Native Connectors** or **proven third-party** |

---

## 3. Bulk Data Loading

### COPY INTO Command
**Use Case:** One-time or batch loading of files into tables  
**Best For:** Initial migrations, scheduled batch loads, large historical backfills  
**Latency:** Minutes to hours (depends on warehouse size and file volume)  
**Cost Model:** Warehouse credits

**Supported Sources:**
- Amazon S3, Google Cloud Storage, Azure Blob/ADLS
- Internal Snowflake stages

**Supported Formats:** CSV, TSV, JSON, Avro, ORC, Parquet, XML

📚 **Documentation:**
- [Data Loading Overview](https://docs.snowflake.com/en/user-guide/data-load-overview)
- [COPY INTO Reference](https://docs.snowflake.com/en/sql-reference/sql/copy-into-table)
- [Schema Evolution](https://docs.snowflake.com/en/user-guide/data-load-schema-evolution)

### PUT Command
**Use Case:** Upload local files to internal stage  
📚 **Docs:** [PUT Command](https://docs.snowflake.com/en/sql-reference/sql/put)

---

## 4. Continuous Data Loading (Streaming)

### Snowpipe (File-Based)
**Use Case:** Automatically load new files as they arrive  
**Latency:** Seconds to minutes  
**Cost:** ~0.0037 credits/GB (serverless)  
**Limitation:** "Small file problem" - inefficient for millions of tiny files

📚 **Documentation:**
- [Snowpipe Introduction](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-intro)
- [Snowpipe Costs](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-billing)
- [Auto-Ingest Setup](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-auto)

### Snowpipe Streaming (Row-Based)
**Use Case:** Sub-second latency streaming ingestion  
**Best For:** Kafka, IoT, CDC, real-time events

#### Two Architectures Comparison

| Feature | Classic Architecture | High-Performance Architecture |
|---------|---------------------|------------------------------|
| **Target Object** | Writes to TABLE | Writes to PIPE (enables transforms) |
| **SDK Support** | Java only | Java & Python (Rust core) |
| **Latency** | Seconds | Sub-second to <5 seconds |
| **Throughput** | Moderate | Up to 10 GB/s per table |
| **Pricing** | Compute + connection fees | Throughput-based (per GB) |
| **Transforms** | None | In-flight SQL transforms |

📚 **Documentation:**
- [Snowpipe Streaming Overview](https://docs.snowflake.com/en/user-guide/snowpipe-streaming/data-load-snowpipe-streaming-overview)
- [High-Performance Architecture](https://www.snowflake.com/en/engineering-blog/next-gen-snowpipe-streaming-architecture/)
- [Classic Architecture](https://docs.snowflake.com/en/user-guide/snowpipe-streaming/snowpipe-streaming-classic-overview)

---

## 5. Third-Party ETL/ELT Partners

> **When to use:** When Native/OpenFlow connectors don't cover the source, or for complex transformations and governance.

### Tier 1 Partners (Partner Connect)

| Partner | Best For | Documentation |
|---------|----------|---------------|
| **Fivetran** | 200+ sources, zero-config | [Fivetran Docs](https://fivetran.com/docs/destinations/snowflake) |
| **Airbyte** | Open-source, customizable | [Airbyte Docs](https://docs.airbyte.com/integrations/destinations/snowflake) |
| **Matillion** | Visual ETL, AI agents (Maia) | [Matillion Docs](https://docs.matillion.com/data-productivity-cloud/designer/docs/snowflake-destination/) |
| **dbt Labs** | SQL transformations | [dbt Snowflake Setup](https://docs.getdbt.com/docs/core/connect-data-platform/snowflake-setup) |
| **Informatica** | Enterprise governance | [Informatica IDMC](https://docs.informatica.com/cloud-integration.html) |
| **Talend/Qlik** | Enterprise ETL | [Talend Docs](https://help.talend.com/r/en-US/Cloud/snowflake-connector-user-guide) |
| **Census** | Reverse ETL | [Census Docs](https://docs.getcensus.com/destinations/snowflake) |
| **Hightouch** | Reverse ETL | [Hightouch Docs](https://hightouch.com/docs/destinations/snowflake) |

📚 **Snowflake Partner Docs:**
- [ETL Partner Ecosystem](https://docs.snowflake.com/en/user-guide/ecosystem-etl)
- [Partner Connect](https://docs.snowflake.com/en/user-guide/ecosystem-partner-connect)

### Cloud Provider Integrations

**AWS:**
| Service | Documentation |
|---------|---------------|
| **AWS Glue** | [Glue Snowflake Connector](https://www.phdata.io/blog/using-aws-glues-native-connector-to-load-data-into-snowflake/) |
| **Kinesis Firehose** | [Firehose to Snowflake](https://docs.aws.amazon.com/firehose/latest/dev/create-destination.html#create-destination-snowflake) |
| **Lambda** | [Lambda Integration Guide](https://aws.amazon.com/blogs/apn/enriching-snowflake-data-with-amazon-location-service-and-aws-lambda/) |

**Azure:**
| Service | Documentation |
|---------|---------------|
| **Azure Data Factory** | [ADF Snowflake Guide](https://www.snowflake.com/en/developers/guides/getting-started-with-azure-data-factory-and-snowflake/) |
| **Synapse Pipelines** | [Synapse Connector](https://learn.microsoft.com/en-us/azure/data-factory/connector-snowflake) |

**GCP:**
| Service | Documentation |
|---------|---------------|
| **Cloud Data Fusion** | [Data Fusion Plugin](https://www.cdata.com/kb/tech/snowflake-jdbc-google-data-fusion.rst) |
| **Dataflow (Beam)** | [SnowflakeIO Docs](https://beam.apache.org/documentation/io/built-in/snowflake/) |

### CDC/Replication Specialists

| Tool | Best For | Documentation |
|------|----------|---------------|
| **Qlik Replicate** | Legacy/mainframe CDC | [Qlik Replicate Docs](https://help.qlik.com/en-US/replicate/) |
| **Striim** | Real-time CDC | [Striim Docs](https://www.striim.com/docs/) |
| **HVR (Fivetran)** | High-volume replication | [HVR Docs](https://fivetran.com/docs/hvr) |
| **Oracle GoldenGate** | Oracle CDC | [GoldenGate Docs](https://docs.oracle.com/en/middleware/goldengate/) |

### SAP Integration

| Option | Description | Documentation |
|--------|-------------|---------------|
| **SAP Datasphere** | Zero-copy federation | [SAP Partnership](https://www.snowflake.com/en/blog/sap-snowflake-partnership-ai-data-cloud/) |
| **SNP Glue** | Native App, direct ABAP push | [SNP Native App](https://www.snowflake.com/en/blog/snp-snowflake-native-app-sap-analytics/) |

---

## 6. Programmatic Interfaces (Drivers & SDKs)

### Official Snowflake Drivers

| Driver | Language | Key Features | Documentation |
|--------|----------|--------------|---------------|
| **JDBC** | Java | Type 4, Apache Arrow fetching, FIPS-compliant | [JDBC Docs](https://docs.snowflake.com/en/developer-guide/jdbc/jdbc) |
| **ODBC** | C/C++ | Legacy tools, SSIS, Excel | [ODBC Docs](https://docs.snowflake.com/en/developer-guide/odbc/odbc) |
| **Python Connector** | Python | pandas integration, async queries | [Python Docs](https://docs.snowflake.com/en/developer-guide/python-connector/python-connector) |
| **Go Driver** | Go | High concurrency, database/sql interface | [Go Docs](https://docs.snowflake.com/en/developer-guide/go/go-driver) |
| **Node.js** | JavaScript | Async/event-driven, serverless | [Node.js Docs](https://docs.snowflake.com/en/developer-guide/node-js/node-js-driver) |
| **.NET** | C#/F# | Azure Functions, .NET Core | [.NET Docs](https://docs.snowflake.com/en/developer-guide/dotnet/dotnet-driver) |
| **PHP PDO** | PHP | Web applications | [PHP Docs](https://docs.snowflake.com/en/developer-guide/php-pdo-driver) |

📚 **All Drivers:** [Drivers Overview](https://docs.snowflake.com/en/developer-guide/drivers) | [Downloads](https://www.snowflake.com/en/developers/downloads/drivers-and-libraries/)

### CLI Tools

| Tool | Use Case | Documentation |
|------|----------|---------------|
| **SnowSQL** | SQL scripts, PUT command | [SnowSQL Docs](https://docs.snowflake.com/en/user-guide/snowsql) |
| **Snowflake CLI** | Snowpark, Native Apps, CI/CD | [CLI Docs](https://docs.snowflake.com/en/developer-guide/snowflake-cli/index) |

### APIs

| API | Use Case | Documentation |
|-----|----------|---------------|
| **SQL REST API** | HTTP-based SQL execution | [SQL API Docs](https://docs.snowflake.com/en/developer-guide/sql-api/index) |
| **Snowpipe REST API** | Programmatic file ingestion | [Snowpipe REST Docs](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-rest-api) |

---

## 7. Cloud Storage Integration

### External Stages
📚 **Docs:** [External Stages](https://docs.snowflake.com/en/user-guide/data-load-s3-create-stage)

### Storage Integrations
📚 **Docs:** [Storage Integrations](https://docs.snowflake.com/en/sql-reference/sql/create-storage-integration)

### Directory Tables
📚 **Docs:** [Directory Tables](https://docs.snowflake.com/en/user-guide/data-load-dirtables)

---

## 8. Apache Kafka Integration

### Snowflake Connector for Kafka

**Modes:**
1. **Snowpipe Mode (Legacy):** File-based staging
2. **Snowpipe Streaming Mode (Recommended):** Direct row ingestion, 50-60% cost reduction

📚 **Documentation:**
- [Kafka Connector Docs](https://docs.snowflake.com/en/user-guide/kafka-connector)
- [Kafka Connector GitHub](https://github.com/snowflakedb/snowflake-kafka-connector)
- [High-Performance Mode](https://docs.snowflake.com/en/user-guide/kafka-connector-streaming)

---

## 9. Data Lake & Open Formats

### Apache Iceberg Tables

**Catalog Options:**
1. **Snowflake-Managed:** Snowflake as catalog, external storage
2. **External Catalog:** AWS Glue, Apache Polaris - "Zero-ETL" pattern
3. **Snowflake Open Catalog:** Bi-directional with Spark/Flink/Trino

📚 **Documentation:**
- [Iceberg Tables](https://docs.snowflake.com/en/user-guide/tables-iceberg)
- [Iceberg Management](https://docs.snowflake.com/en/user-guide/tables-iceberg-manage)

### External Tables
📚 **Docs:** [External Tables](https://docs.snowflake.com/en/user-guide/tables-external-intro)

### Delta Lake Support
📚 **Docs:** [Delta Lake via Iceberg](https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table-delta)

---

## 10. Transformation & Orchestration

### Dynamic Tables
📚 **Docs:** [Dynamic Tables](https://docs.snowflake.com/en/user-guide/dynamic-tables-about) | [Getting Started](https://www.snowflake.com/en/developers/guides/getting-started-with-dynamic-tables/)

### Streams (CDC)
📚 **Docs:** [Streams](https://docs.snowflake.com/en/user-guide/streams)

### Tasks
📚 **Docs:** [Tasks](https://docs.snowflake.com/en/user-guide/tasks-intro)

### dbt Integration
📚 **Docs:** [dbt Snowflake Setup](https://docs.getdbt.com/docs/core/connect-data-platform/snowflake-setup)

---

## 11. Source System Coverage Matrix

> **Legend:** 
> - ✅ **Native (GA)** = Established, safe to recommend
> - 🆕 **OpenFlow (GA)** = New, verify reliability first  
> - ⚠️ **Preview** = Not production-ready
> - ❌ **No native** = Use third-party

| Source | Snowflake Option | Status | Recommendation | Third-Party Options | Docs |
|--------|------------------|--------|----------------|---------------------|------|
| **MySQL** | OpenFlow | 🆕 GA | Verify first | Fivetran, Airbyte, Matillion | [OpenFlow MySQL](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/mysql/about) |
| **PostgreSQL** | OpenFlow | 🆕 GA | Verify first | Fivetran, Airbyte | [OpenFlow PostgreSQL](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/postgresql/about) |
| **SQL Server** | OpenFlow | 🆕 GA | Verify first | Fivetran, Airbyte, ADF | [OpenFlow SQL Server](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/sqlserver/about) |
| **Oracle** | OpenFlow | ⚠️ Private Preview | Use third-party | Qlik Replicate, GoldenGate | [Oracle Blog](https://www.snowflake.com/en/blog/oracle-database-integration-connector/) |
| **Salesforce** | OpenFlow | 🆕 GA | Verify first | Fivetran, Informatica | [OpenFlow Salesforce](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/salesforce/about) |
| **Workday** | OpenFlow | 🆕 GA | Verify first | Fivetran, Rivery | [OpenFlow Workday](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/workday/about) |
| **Jira Cloud** | OpenFlow | 🆕 GA | Verify first | Fivetran, Airbyte | [OpenFlow Jira](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/jira/about) |
| **Microsoft Dataverse** | OpenFlow | 🆕 GA | Verify first | Fivetran, ADF | [OpenFlow Dataverse](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/dataverse/about) |
| **SharePoint** | OpenFlow | 🆕 GA | Verify first | Fivetran, manual COPY INTO | [OpenFlow SharePoint](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/sharepoint/about) |
| **Box** | OpenFlow | 🆕 GA | Verify first | Custom API | [OpenFlow Box](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/box/about) |
| **Google Drive** | OpenFlow | 🆕 GA | Verify first | Custom API | [OpenFlow Google Drive](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-drive/about) |
| **Google Sheets** | OpenFlow | 🆕 GA | Verify first | Fivetran, Airbyte | [OpenFlow Google Sheets](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-sheets/about) |
| **Slack** | OpenFlow | 🆕 GA | Verify first | Custom API | [OpenFlow Slack](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/slack/about) |
| **Amazon Ads** | OpenFlow | 🆕 GA | Verify first | Fivetran | [OpenFlow Amazon Ads](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/amazon-ads/about) |
| **Google Ads** | OpenFlow | 🆕 GA | Verify first | Fivetran | [OpenFlow Google Ads](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-ads/about) |
| **LinkedIn Ads** | OpenFlow | 🆕 GA | Verify first | Fivetran | [OpenFlow LinkedIn Ads](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/linkedin-ads/about) |
| **Meta Ads** | OpenFlow | 🆕 GA | Verify first | Fivetran | [OpenFlow Meta Ads](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/meta-ads/about) |
| **AWS Kinesis** | OpenFlow | 🆕 GA | Verify first | Firehose direct | [OpenFlow Kinesis](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/kinesis/about) |
| **Google Analytics** | Native | ✅ GA | **Recommended** | Fivetran, Airbyte | [GA Aggregate](https://docs.snowflake.com/en/connectors/google/gaad/gaad-connector-about) |
| **ServiceNow** | Native | ✅ GA | **Recommended** | Fivetran, Boomi | [ServiceNow Docs](https://docs.snowflake.com/en/connectors/servicenow/about) |
| **Kafka** | Native HP + OpenFlow | ✅ GA | **Recommended** | Confluent | [Kafka Connector](https://docs.snowflake.com/en/user-guide/kafka-connector) |
| **SAP** | Partner only | ❌ | Use third-party | SNP Glue, Qlik, Informatica | [SNP Native App](https://www.snowflake.com/en/blog/snp-snowflake-native-app-sap-analytics/) |
| **MongoDB** | None | ❌ | Use third-party | Fivetran, Stitch | [Fivetran MongoDB](https://fivetran.com/docs/databases/mongodb) |

---

## 12. Decision Matrix

### Source System → Recommended Method

| Source Type | Best Option | Reliability | Documentation |
|-------------|-------------|-------------|---------------|
| **Google Analytics** | ✅ Native Connector | Established | [GA Docs](https://docs.snowflake.com/en/connectors/google/gaad/gaad-connector-about) |
| **ServiceNow** | ✅ Native Connector | Established | [ServiceNow Docs](https://docs.snowflake.com/en/connectors/servicenow/about) |
| **Kafka** | ✅ HP Kafka Connector | Established | [Kafka Docs](https://docs.snowflake.com/en/user-guide/kafka-connector) |
| **PostgreSQL/SQL Server** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Connectors](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/about-openflow-connectors) |
| **MySQL** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow MySQL](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/mysql/about) |
| **Oracle** | Use Qlik/GoldenGate | Third-party | [Qlik Replicate](https://help.qlik.com/en-US/replicate/) |
| **Salesforce** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Salesforce](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/salesforce/about) |
| **Jira Cloud** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Jira](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/jira/about) |
| **Workday** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Workday](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/workday/about) |
| **SharePoint (files)** | 🆕 OpenFlow | Verify first | [OpenFlow SharePoint](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/sharepoint/about) |
| **Box / Google Drive** | 🆕 OpenFlow | Verify first | [OpenFlow Box](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/box/about) |
| **Google Sheets** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Google Sheets](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/google-sheets/about) |
| **Slack** | 🆕 OpenFlow | Verify first | [OpenFlow Slack](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/slack/about) |
| **Ad platforms** | 🆕 OpenFlow OR Fivetran | Verify first | [OpenFlow Connectors](https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/about-openflow-connectors) |
| **Files (S3/GCS/Azure)** | ✅ COPY INTO/Snowpipe | Established | [Data Loading](https://docs.snowflake.com/en/user-guide/data-load-overview) |
| **Real-time streams** | ✅ Snowpipe Streaming | Established | [Streaming Docs](https://docs.snowflake.com/en/user-guide/snowpipe-streaming/data-load-snowpipe-streaming-overview) |

### Latency Requirements

| Latency | Method | Docs |
|---------|--------|------|
| **Hours (batch)** | COPY INTO | [COPY INTO](https://docs.snowflake.com/en/sql-reference/sql/copy-into-table) |
| **Minutes** | Snowpipe | [Snowpipe](https://docs.snowflake.com/en/user-guide/data-load-snowpipe-intro) |
| **Seconds** | Snowpipe Streaming | [Streaming](https://docs.snowflake.com/en/user-guide/snowpipe-streaming/data-load-snowpipe-streaming-overview) |

---

## Security & Governance

### Network Security
📚 **PrivateLink:** [AWS PrivateLink](https://docs.snowflake.com/en/user-guide/admin-security-privatelink) | [Azure Private Link](https://docs.snowflake.com/en/user-guide/admin-security-privatelink-azure)

### Authentication
📚 **Key-Pair Auth:** [Key Pair Authentication](https://docs.snowflake.com/en/user-guide/key-pair-auth)  
📚 **OAuth:** [OAuth Overview](https://docs.snowflake.com/en/user-guide/oauth-intro)

### Network Rules
📚 **Docs:** [External Access Integrations](https://docs.snowflake.com/en/developer-guide/external-network-access/external-network-access-overview)

---

## Documentation Quick Links

| Category | Resource | URL |
|----------|----------|-----|
| **OpenFlow** | Overview | https://docs.snowflake.com/en/user-guide/data-integration/openflow/about |
| **OpenFlow** | Connectors List | https://docs.snowflake.com/en/user-guide/data-integration/openflow/connectors/about-openflow-connectors |
| **Native** | Connectors Hub | https://other-docs.snowflake.com/en/connectors |
| **Loading** | Data Load Overview | https://docs.snowflake.com/en/user-guide/data-load-overview |
| **Loading** | Snowpipe | https://docs.snowflake.com/en/user-guide/data-load-snowpipe-intro |
| **Streaming** | Snowpipe Streaming | https://docs.snowflake.com/en/user-guide/snowpipe-streaming/data-load-snowpipe-streaming-overview |
| **Streaming** | Kafka Connector | https://docs.snowflake.com/en/user-guide/kafka-connector |
| **Partners** | ETL Ecosystem | https://docs.snowflake.com/en/user-guide/ecosystem-etl |
| **Partners** | Partner Connect | https://docs.snowflake.com/en/user-guide/ecosystem-partner-connect |
| **Drivers** | All Drivers | https://docs.snowflake.com/en/developer-guide/drivers |
| **Tables** | Iceberg | https://docs.snowflake.com/en/user-guide/tables-iceberg |
| **Tables** | External Tables | https://docs.snowflake.com/en/user-guide/tables-external-intro |
| **Tables** | Dynamic Tables | https://docs.snowflake.com/en/user-guide/dynamic-tables-about |
| **API** | SQL REST API | https://docs.snowflake.com/en/developer-guide/sql-api/index |
| **API** | Snowpark | https://docs.snowflake.com/en/developer-guide/snowpark/index |

---

*Last Updated: January 2026*  
*Version: 4.0 - Added comprehensive documentation links, Gemini research integration*  
*Sources: Snowflake Documentation, Summit 2025, ChatGPT Research, Gemini Deep Research*
