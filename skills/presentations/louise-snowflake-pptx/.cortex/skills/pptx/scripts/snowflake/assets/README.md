# Snowflake Assets

Brand assets for Snowflake PPTX generation. Used by `AssetCatalog.cs` to resolve icon and image references in slide specs.

## Contents

| Directory | Files | Description |
|-----------|-------|-------------|
| `svg/` | 150 | Vector icons (kebab-case) + Snowflake wordmark logo |
| `jpg/` | 35 | Photos, backgrounds, and a quote arrow graphic |
| `png/` | 1 | Arrow graphic |
| **Total** | **186** | |

## SVG Icons (`svg/`)

149 kebab-case icons covering brand categories (General, Reference Architecture, Industries, New and Notable) plus one Snowflake wordmark logo (`graphic_snowflake_logo_blue.svg`). Examples:

```
data-warehouse.svg        snowflake-cortex.svg      iceberg-tables.svg
snowpark.svg              unistore.svg              data-engineering.svg
analytics.svg             secure-data.svg           cloud.svg
ai.svg                    connected.svg             collaboration.svg
```

## JPG Photos (`jpg/`)

| Prefix | Count | Use |
|--------|-------|-----|
| `photo_*` | 31 | Stock photos (offices, developers, meetings, industry) |
| `background_*` | 3 | Abstract backgrounds (fiber, gradient, SI) |
| `quote-arrow.jpeg` | 1 | Decorative arrow for quote slides |

## Usage

Reference by filename in YAML slide specs:

```yaml
# Brand icon (column/icon slides)
col1_icon: "data-engineering.svg"

# Split slide image (MUST be JPG)
image: "photo_asset_product_build_group_developers.jpg"
```

`AssetCatalog` resolves references by scanning `svg/`, `png/`, and `jpg/` subdirectories with keyword matching. Exact stem matches score highest, followed by prefix and substring matches.

## Brand

- Primary color: `#29B5E8` (Snowflake Blue)
- Icon source: SNOWFLAKE TEMPLATE JANUARY 2026 (2).pptx, slides 44-51
- Extracted by: `tools/extract-icons/`
