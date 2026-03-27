# Snowflake Workload Sizing & Estimation Guide

> A comprehensive guide for accurately estimating Snowflake consumption, sizing warehouses, and defending estimates to customers.

---

## Table of Contents
1. [Warehouse Sizing Fundamentals](#1-warehouse-sizing-fundamentals)
2. [Concurrency & Multi-Cluster Warehouses](#2-concurrency--multi-cluster-warehouses)
3. [Workload-Specific Sizing Patterns](#3-workload-specific-sizing-patterns)
4. [Estimation Methodology](#4-estimation-methodology)
5. [Defending Your Estimate](#5-defending-your-estimate)
6. [Common Pitfalls & How to Avoid Them](#6-common-pitfalls--how-to-avoid-them)
7. [Quick Reference Tables](#7-quick-reference-tables)

---

## 1. Warehouse Sizing Fundamentals

### Credit Consumption by Size

| Size | Credits/Hour | Relative Power | Best For |
|------|-------------|----------------|----------|
| XS | 1 | 1x | Dev/test, light queries, small datasets |
| S | 2 | 2x | Small BI dashboards, simple transforms |
| M | 4 | 4x | Standard analytics, moderate ELT |
| L | 8 | 8x | Complex transforms, larger datasets |
| XL | 16 | 16x | Heavy analytics, ML training |
| 2XL | 32 | 32x | Large-scale processing |
| 3XL | 64 | 64x | Very large workloads |
| 4XL | 128 | 128x | Extreme processing needs |
| 5XL | 256 | 256x | Massive parallel processing |
| 6XL | 512 | 512x | Maximum throughput |

### Key Sizing Principles

**1. Right-size, don't over-provision**
- Start with the smallest warehouse that meets SLA
- A query that takes 4 minutes on XS takes ~2 minutes on S, ~1 minute on M
- But: 4 min @ 1 credit/hr = 0.067 credits vs 1 min @ 4 credits/hr = 0.067 credits
- **Same cost, different latency** - size for latency requirements, not speed

**2. Billing Mechanics Matter**
- Per-second billing with 1-minute minimum
- If a query takes 5 seconds, you pay for 60 seconds
- Short, frequent queries → consider batching or right-sizing

**3. Auto-suspend Aggressively**
- Default: 10 minutes (wastes credits)
- Recommended: 1-5 minutes for most workloads
- For BI: 1-2 minutes (queries come in bursts)

---

## 2. Concurrency & Multi-Cluster Warehouses

### Understanding Concurrency

**Single Warehouse Concurrency:**
- Each warehouse can run 8 queries concurrently by default
- Additional queries queue
- Queue time = bad user experience

**Multi-Cluster Warehouse (MCW) Scaling:**
- Auto-scales 1-10 clusters based on demand
- Each cluster = full warehouse credits
- Use for: variable concurrency, BI with many users

### MCW Sizing Formula

```
Peak Concurrent Users × Queries per User per Minute
─────────────────────────────────────────────────── = Min Clusters Needed
            8 (queries per cluster)
```

**Example:**
- 50 concurrent BI users
- Each runs 2 queries/minute on average
- Peak: 100 concurrent queries
- 100 ÷ 8 = 12.5 → Need up to 13 clusters at peak

### MCW Cost Estimation

```
Clusters × Size Credits × Active Hours × Days/Month = Monthly Credits
```

**Example:**
- M warehouse (4 credits/hr)
- Average 3 clusters during 8 business hours
- 22 business days/month
- 3 × 4 × 8 × 22 = **2,112 credits/month**

---

## 3. Workload-Specific Sizing Patterns

### DATA INGESTION

| Pattern | Typical Size | Hours/Day | Notes |
|---------|-------------|-----------|-------|
| Batch Daily (< 100GB) | S-M | 1-2 | Single daily load |
| Batch Daily (100GB-1TB) | M-L | 2-4 | Consider splitting loads |
| Batch Daily (> 1TB) | L-XL | 4-8 | Parallel loading |
| Hourly Micro-batch | XS-S | 24 | Always on, small bursts |
| Near Real-time (5-10 min) | S-M | 24 | Continuous streaming |
| Snowpipe (serverless) | N/A | N/A | 0.06 credits/1000 files |

**Ingestion Credit Formula:**
```
(Data Volume TB × 10-30 credits/TB) + Processing Overhead
```

### TRANSFORMATION (ELT/DBT)

| Pattern | Typical Size | Frequency | Credits/Run |
|---------|-------------|-----------|-------------|
| Light transforms | S | Daily | 2-5 |
| Standard dbt models | M | Daily | 10-30 |
| Complex joins/aggregations | L | Daily | 30-100 |
| ML feature engineering | L-XL | Daily | 50-200 |

**Transform Estimation:**
- Simple transform: ~10-20 credits/TB processed
- Complex transform: ~30-50 credits/TB processed
- Heavy ML prep: ~50-100 credits/TB processed

### BI & ANALYTICS

| User Type | Queries/Day | Typical Size | Credits/User/Month |
|-----------|-------------|--------------|-------------------|
| Executive (light) | 5-10 | XS-S | 5-15 |
| Analyst (medium) | 20-50 | S-M | 20-50 |
| Power User (heavy) | 50-100+ | M-L | 50-150 |
| Data Scientist | 20-40 (heavy) | L-XL | 100-300 |

**BI Workload Formula:**
```
Users × Queries/Day × Avg Query Runtime (hrs) × Warehouse Size Credits × Days/Month
```

### SERVERLESS FEATURES

| Feature | Credit Rate | Use Case |
|---------|-------------|----------|
| Snowpipe | 0.06 per 1000 files | Continuous ingestion |
| Auto-clustering | ~0.1-0.5 per TB clustered | Large, frequently queried tables |
| Search Optimization | ~0.05-0.2 per TB | Text search, point lookups |
| Dynamic Tables | Per refresh (like transform) | Incremental pipelines |
| Tasks | Per run (like compute) | Scheduled jobs |
| Materialized Views | Per refresh | Pre-computed aggregations |

---

## 4. Estimation Methodology

### The BOM (Bill of Materials) Approach

Build estimates bottom-up by workload category:

```
┌─────────────────────────────────────────────────────────────┐
│  USE CASE: Customer 360 Analytics                          │
├─────────────────────────────────────────────────────────────┤
│  WORKLOAD          │ WH SIZE │ HRS/DAY │ DAYS/YR │ CREDITS │
├────────────────────┼─────────┼─────────┼─────────┼─────────┤
│  Data Ingestion    │   M     │   2     │  365    │  2,920  │
│  Transformation    │   L     │   3     │  365    │  8,760  │
│  BI Dashboards     │   M     │   10    │  260    │  10,400 │
│  Ad-hoc Analytics  │   S     │   4     │  260    │  2,080  │
│  Auto-clustering   │   -     │   -     │   -     │  1,500  │
├────────────────────┼─────────┼─────────┼─────────┼─────────┤
│  TOTAL ANNUAL                                    │ 25,660  │
└─────────────────────────────────────────────────────────────┘
```

### Ramp-Up Curves

New workloads don't hit full consumption Day 1:

| Curve | Month 1 | Month 3 | Month 6 | Month 12 |
|-------|---------|---------|---------|----------|
| Slowest | 10% | 30% | 60% | 90% |
| Slow | 20% | 50% | 80% | 100% |
| Linear | 25% | 50% | 75% | 100% |
| Fast | 40% | 70% | 90% | 100% |
| Fastest | 60% | 85% | 95% | 100% |

**Year 1 Credit Multiplier:**
- Slowest: ~55% of steady-state
- Slow: ~65% of steady-state
- Linear: ~70% of steady-state
- Fast: ~80% of steady-state
- Fastest: ~90% of steady-state

### Storage Estimation

```
Raw Data (TB) × Compression Rate = Compressed Storage (TB)
Compressed Storage × $23-40/TB/month = Monthly Storage Cost
```

**Compression Benchmarks:**
| Data Type | Typical Compression |
|-----------|-------------------|
| CSV/JSON logs | 5-10x |
| Structured relational | 3-5x |
| Semi-structured | 3-7x |
| Already compressed | 1-2x |

---

## 5. Defending Your Estimate

### Building Credibility

**1. Show Your Work**
- Break down by workload category
- Cite benchmark patterns from similar customers
- Reference the credit consumption tables

**2. Use Conservative Assumptions**
- Start with "expected" scenario
- Show "high" and "low" ranges
- Build in growth buffers (10-20% annually typical)

**3. Connect to Business Value**
- "This BI workload serves 50 analysts who generate $X in insights"
- "This transform runs in 30 minutes vs 4 hours on legacy system"

### Common Customer Challenges & Responses

| Challenge | Response |
|-----------|----------|
| "This seems expensive" | Show TCO vs legacy (hardware, DBAs, maintenance). Snowflake separation of storage/compute typically 30-50% cheaper. |
| "Why do I need this warehouse size?" | Demo the query on different sizes. Show latency vs cost tradeoff. |
| "What if we grow faster?" | "Credits are fungible - reallocate from one workload to another. Can add mid-contract." |
| "Why serverless vs compute?" | Serverless for sporadic/unpredictable. Compute for predictable, sustained workloads. |
| "How do I know this is accurate?" | "Based on [similar customer], [benchmark data], [your questionnaire responses]. We recommend quarterly reviews." |

### The Three-Option Strategy

Always present 3 scenarios:

| Option | Use Case | Risk Level |
|--------|----------|------------|
| **Conservative** | Proven workloads only, slow ramp | Low (may under-buy) |
| **Expected** | Planned workloads + moderate growth | Medium (recommended) |
| **Aggressive** | All planned + exploratory + fast growth | Higher (buffer for innovation) |

---

## 6. Common Pitfalls & How to Avoid Them

### Sizing Mistakes

| Pitfall | Problem | Solution |
|---------|---------|----------|
| Over-sizing warehouses | Paying for unused capacity | Start small, scale up based on metrics |
| Under-estimating concurrency | Users wait in queue | Use MCW or separate warehouses by workload |
| Long auto-suspend | Idle warehouses burn credits | Set 1-5 minute suspend |
| Ignoring serverless | Missing cost-effective options | Evaluate Snowpipe, clustering for right workloads |
| Single warehouse for all | Contention, sizing conflicts | Separate by workload type |

### Estimation Mistakes

| Pitfall | Problem | Solution |
|---------|---------|----------|
| No questionnaire | Guessing at requirements | Always do discovery first |
| Ignoring ramp-up | Over-estimating Year 1 | Apply appropriate ramp curve |
| Linear growth assumption | May be exponential or step-function | Model realistic adoption patterns |
| Forgetting serverless | Incomplete estimate | Add auto-clustering, Snowpipe, etc. |
| Not validating with customer | Misaligned expectations | Review BOM with technical stakeholders |

---

## 7. Quick Reference Tables

### Annual Credit Estimation by Use Case Size

| Use Case Size | Typical Annual Credits | Example |
|--------------|----------------------|---------|
| Small | 5,000 - 15,000 | Single dept BI |
| Medium | 15,000 - 50,000 | Cross-functional analytics |
| Large | 50,000 - 150,000 | Enterprise data platform |
| Very Large | 150,000 - 500,000+ | Multi-region, ML-heavy |

### Credit-to-Dollar Quick Math (at various price points)

| Credits | @ $2.00 | @ $2.50 | @ $3.00 | @ $3.50 |
|---------|---------|---------|---------|---------|
| 10,000 | $20,000 | $25,000 | $30,000 | $35,000 |
| 25,000 | $50,000 | $62,500 | $75,000 | $87,500 |
| 50,000 | $100,000 | $125,000 | $150,000 | $175,000 |
| 100,000 | $200,000 | $250,000 | $300,000 | $350,000 |

### Cortex AI Quick Sizing

| Feature | Unit | Credits/Unit | Typical Use |
|---------|------|-------------|-------------|
| COMPLETE (llama3.1-70b) | 1M tokens | 1.21 | Complex generation |
| COMPLETE (mistral-large2) | 1M tokens | 1.58 | High-quality responses |
| COMPLETE (llama3.1-8b) | 1M tokens | 0.19 | Cost-effective generation |
| EXTRACT_ANSWER | 1M tokens | 0.12 | Document Q&A |
| SENTIMENT | 1M tokens | 0.10 | Text analysis |
| SUMMARIZE | 1M tokens | 0.10 | Document summarization |
| TRANSLATE | 1M tokens | 0.33 | Multi-language |
| Cortex Search | 1000 queries | 0.07 | RAG retrieval |

---

## Appendix: Birdbox Workflow Summary

For formal capacity contracts, follow this process:

1. **Prerequisites**: Complete sizing questionnaire, know AE/DM targets
2. **Setup**: Copy Birdbox template, set customer/deal metadata
3. **Use Cases**: Define 1-5 use cases with workload breakdown
4. **Storage**: Model raw data, compression, growth
5. **Compute**: Model each workload (size, hours, days, ramp)
6. **Serverless**: Add auto-clustering, Snowpipe, dynamic tables
7. **Replication**: If multi-region, use Replication Calculator
8. **Pricing**: Apply negotiated $/credit, generate options
9. **Validate**: Sanity check vs benchmarks and budget
10. **Present**: Show BOM detail, not just a single number

---

*Last Updated: January 2026*
*Based on: Credit Consumption Table (Effective January 13, 2026), Birdbox Planner V2, Sizing & Cap1 Best Practices*
