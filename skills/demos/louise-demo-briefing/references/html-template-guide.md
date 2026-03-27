# HTML Template Guide for Demo Briefing

## CSS Variables (Root)

Always define these CSS variables. Override `--client-*` colors from `demo.branding` in config.

```css
:root {
  --snow-blue: #29B5E8;
  --snow-dark: #11567F;
  --snow-navy: #0D2137;
  --client-dark: #1A1A1A;      /* from branding.primary_color or default */
  --client-accent: #C8956C;    /* from branding.secondary_color or default */
  --client-secondary: #4A7C59; /* from branding.accent_color or default */
  --bg: #F8F9FA;
  --card-bg: #FFFFFF;
  --text: #2D3748;
  --text-light: #718096;
  --danger: #E53E3E;
  --success: #38A169;
  --warning: #D69E2E;
}
```

## Required CSS Classes

### Layout
- `.header` — Gradient background (snow-navy → snow-dark), white text, centered
- `.container` — max-width: 960px, centered
- `.section` — margin-bottom: 2rem
- `.section-title` — font-size: 1.4rem, border-bottom: 3px solid snow-blue

### Cards
- `.card` — White background, rounded (12px), subtle shadow, 1px border
- `.card-danger` — Red left border (pain points)
- `.card-success` — Green left border (demo features)
- `.card-warning` — Yellow left border (cautions)
- `.card-blue` — Blue left border (personas, info)

### Grids
- `.grid-2` — 2-column grid, collapses on mobile
- `.grid-3` — 3-column grid, collapses on mobile

### Stat Cards
- `.stat-card` — Centered card with `.number` (2rem, bold, snow-blue) and `.label` (small, light)

### Persona Cards
- `.persona-card` — Flexbox with avatar + info
- `.persona-avatar` — 48px circle, colored background, white initials
- `.persona-info` — Name (bold), role (light), note (regular), tags

### Tags
- `.tag` — Inline pill (rounded 12px, small font, bold)
- `.tag-blue`, `.tag-green`, `.tag-red`, `.tag-orange`, `.tag-purple`

### Architecture Diagram
- `.arch-flow` — Flexbox centered with gaps
- `.arch-box` — White box with colored border (2px), rounded 8px
- `.arch-box.source` — Client accent border, warm background
- `.arch-box.hub` — Snow-blue border, light blue background, larger font
- `.arch-box.output` — Green border, light green background
- `.arch-arrow` — Large arrow character (→), light color

### Demo Flow
- `.demo-flow` — CSS counter-reset: step
- `.demo-step` — Padding-left 3.5rem, counter-increment circle (snow-blue, 2.5rem)
- `.demo-step h4` — Step title
- `.demo-step .talk-track` — Light background, blue left border, italic

### Question Boxes
- `.question-box` — Light background, flex with icon + text
- `.q-icon` — Speech bubble emoji
- `.q-text` — Bold question
- `.q-why` — Small light explanation

### Link Boxes
- `.link-box` — Gradient background (light blue → light green), border, rounded

### Highlight Block
- `.highlight` — Dark background (client-dark), white text, rounded, used for key messages
- `.highlight h2` — Client accent color

### Warning Banner
- `.warning-banner` — Light orange background, orange border, used for caveats

### Tables
- `th` — snow-navy background, white text
- `td` — Light border, alternating row colors

## Section Order

1. Header (with badges: date, SE name, partner)
2. Business Context (highlight box + stat cards grid-3)
3. Personas (grid-2 of persona cards)
4. Pain Points (danger/warning cards)
5. Target Architecture (arch-flow diagram)
6. What the Demo Does (success cards with entity table)
7. Need-to-Demo Mapping (full-width table)
8. Suggested Demo Flow (demo-flow numbered steps with talk tracks)
9. Prepared Talk2Data Questions (question boxes)
10. Links & Access (link boxes)
11. Points of Attention (warning banner + cards)
12. One-Sentence Summary (highlight box, centered)
13. Footer

## Key Patterns

### Stat Cards
```html
<div class="grid-3">
  <div class="card stat-card">
    <div class="number">VALUE</div>
    <div class="label">Description</div>
  </div>
</div>
```

### Persona Card
```html
<div class="card card-blue">
  <div class="persona-card">
    <div class="persona-avatar" style="background: COLOR;">XX</div>
    <div class="persona-info">
      <div class="name">Full Name</div>
      <div class="role">Title - Role</div>
      <div class="note">What they care about. <strong>Key interest</strong>.</div>
      <div style="margin-top:0.5rem;">
        <span class="tag tag-blue">Tag1</span>
        <span class="tag tag-green">Tag2</span>
      </div>
    </div>
  </div>
</div>
```

### Demo Step with Talk Track
```html
<div class="demo-step">
  <h4>Step Title (X min)</h4>
  <p>What to show and why.</p>
  <div class="talk-track">"Verbatim quote the SE can say."</div>
</div>
```

### Question Box
```html
<div class="question-box">
  <div class="q-icon">&#128172;</div>
  <div>
    <div class="q-text"><strong>"The question to ask the agent"</strong></div>
    <div class="q-why">Why this question matters for which persona.</div>
  </div>
</div>
```

### Architecture Flow
```html
<div class="arch-flow">
  <div style="display:flex; flex-direction:column; gap:0.5rem;">
    <div class="arch-box source">Source 1</div>
    <div class="arch-box source">Source 2</div>
  </div>
  <div><span class="arch-arrow">&rarr;</span><br/><span style="font-size:0.75rem;">Ingestion</span></div>
  <div class="arch-box hub">Snowflake<br/><span style="font-size:0.75rem;">+ dbt</span></div>
  <div><span class="arch-arrow">&rarr;</span></div>
  <div style="display:flex; flex-direction:column; gap:0.5rem;">
    <div class="arch-box output">Output 1</div>
    <div class="arch-box output">Output 2</div>
  </div>
</div>
```
