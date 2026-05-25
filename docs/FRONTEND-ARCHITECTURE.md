# GACS — Frontend Architecture

**Gated Access Compliance Suite**
Last updated: 2026-05-25
Status: Architecture definition — pre-build

---

## 1. Overview

GACS has four distinct frontend surfaces, each serving a different user and context.
Every surface is built on the Microsoft stack using C# and Blazor — no JavaScript frameworks.
Junior developers work in one language across all surfaces.

---

## 2. Frontend Surfaces

| Surface | Who Uses It | Where It Runs | Technology |
|---|---|---|---|
| Admin / IO / Compliance Portal | Property Manager, Information Officer, Auditor | Desktop browser | Blazor Server |
| Visitor Self-Registration | Visitors (public, unauthenticated) | Mobile browser | Blazor WASM (static) |
| Guard Check-In App | Guards / Operators | Surface tablet, Android tablet | MAUI Blazor Hybrid |
| Resident / Host Portal | Residents, authorised hosts | Mobile or desktop browser | Blazor WASM (static) |

### Why these technology choices

**Blazor Server — Admin Portal**
- UI state lives on the server. Junior devs reason about one place.
- Shares the same SignalR infrastructure already in place for real-time gate alerts.
- Entra ID authentication is straightforward with ASP.NET Core middleware.
- No heavy JavaScript bundle on first load — portal opens fast on standard office broadband.

**Blazor WASM — Visitor Registration and Resident Portal**
- Runs entirely in the visitor's browser. No server session needed.
- Static files hosted on Azure Static Web Apps — low cost, global CDN, zero server to manage.
- Visitor registration must work on low-end Android phones on mobile data. WASM after first load is fast.
- Resident portal reuses the same component library — different routes, different role, same codebase.

**MAUI Blazor Hybrid — Guard App**
- MAUI provides the native shell: camera access, QR scanner, offline SQLite storage, device push notifications.
- Blazor renders the UI inside the MAUI shell — guards see the same Fluent UI components as the web portal.
- One C# codebase compiles to Android (primary) and Windows (Surface tablets via Intune).
- Offline-first: guard can check visitors in with no connectivity. Records sync when connection resumes.

---

## 3. Component Library

**Microsoft Fluent UI Blazor**
Repository: `microsoft/fluentui-blazor`
NuGet package: `Microsoft.FluentUI.AspNetCore.Components`

### Why Fluent UI Blazor over alternatives

| Library | Decision | Reason |
|---|---|---|
| Microsoft Fluent UI Blazor | **Selected** | Matches M365/Teams aesthetic clients already trust. AppSource-aligned. WCAG 2.1 AA built in. 100% Microsoft stack. |
| MudBlazor | Rejected | Material Design — looks like Google. Breaks visual alignment with Microsoft ecosystem. |
| Radzen Blazor | Rejected | Generic enterprise aesthetic. Licensing complexity at scale. Smaller community. |

### Fluent UI setup rule

Every surface imports Fluent UI the same way. No surface uses a different component library or mixes libraries.
If a component does not exist in Fluent UI Blazor, build it as a custom GACS component (see Section 5) — do not import a third-party library for a single component.

---

## 4. Design System

### 4.1 Font

**Segoe UI Variable** — Microsoft system font.
- Already installed on every Windows and Surface device. Zero download time for guard tablets.
- Falls back to system-ui on Android for the guard app.
- No Google Fonts. No external font CDN calls.

### 4.2 Color Tokens

Two themes: **Light** (admin portal, visitor registration) and **Dark** (guard app — reduces glare on tablets in outdoor or low-light environments).

Define all colors as Fluent design tokens. Never hardcode a hex value in a component.

#### Brand tokens

| Token Name | Purpose |
|---|---|
| `--colorBrandPrimary` | GACS primary brand color — buttons, links, active states |
| `--colorBrandSecondary` | Supporting brand color — hover states, accents |
| `--colorNeutralBackground` | Page background |
| `--colorNeutralSurface` | Card and panel backgrounds |
| `--colorNeutralBorder` | Dividers, input borders |
| `--colorNeutralForeground` | Body text |
| `--colorNeutralForegroundSubtle` | Labels, captions, placeholder text |

#### Semantic risk tokens (GACS-specific)

| Token Name | Value (Light) | Purpose |
|---|---|---|
| `--colorRiskGreen` | `#107C10` | Low risk visitor — safe to proceed |
| `--colorRiskGreenBackground` | `#DFF6DD` | Risk badge background — green |
| `--colorRiskYellow` | `#835B00` | Medium risk — verify identity |
| `--colorRiskYellowBackground` | `#FFF4CE` | Risk badge background — yellow |
| `--colorRiskRed` | `#A4262C` | High risk — deny or escalate |
| `--colorRiskRedBackground` | `#FDE7E9` | Risk badge background — red |
| `--colorCompliant` | `#107C10` | Compliance score — passing |
| `--colorNonCompliant` | `#A4262C` | Compliance score — failing |
| `--colorWarning` | `#835B00` | Approaching expiry or breach threshold |

#### Dark theme overrides (Guard app)

The guard app uses the Fluent dark theme as its base.
The risk tokens remain the same — green/yellow/red must be identifiable in all lighting conditions.
Test risk badge colors on the actual guard tablet hardware before finalising.

### 4.3 Typography Scale

Use named text styles only. Junior devs pick a style name — they do not choose a font size.

| Style Name | Size | Weight | Where Used |
|---|---|---|---|
| `Display` | 32px | 600 | Compliance score numbers, hero metrics |
| `Heading1` | 24px | 600 | Page titles, module headings |
| `Heading2` | 20px | 600 | Section titles, card headings |
| `Heading3` | 16px | 600 | Sub-section labels |
| `Body` | 14px | 400 | Form content, table rows, descriptions |
| `BodyStrong` | 14px | 600 | Form labels, column headers, emphasis |
| `Caption` | 12px | 400 | Timestamps, metadata, helper text |
| `Code` | 13px | 400 (monospace) | QR reference codes, API keys, record IDs |

### 4.4 Spacing Scale

All spacing values are multiples of 4px. Junior devs pick from this scale — no custom pixel values.

`4px / 8px / 12px / 16px / 24px / 32px / 48px / 64px`

Map these to Fluent spacing tokens:

| Token | Value |
|---|---|
| `--spacingHorizontalXS` | 4px |
| `--spacingHorizontalS` | 8px |
| `--spacingHorizontalM` | 16px |
| `--spacingHorizontalL` | 24px |
| `--spacingHorizontalXL` | 32px |
| `--spacingVerticalXS` | 4px |
| `--spacingVerticalS` | 8px |
| `--spacingVerticalM` | 16px |
| `--spacingVerticalL` | 24px |
| `--spacingVerticalXL` | 32px |

### 4.5 Iconography

**Fluent System Icons** only.
NuGet package: included in `Microsoft.FluentUI.AspNetCore.Components`

Rules:
- Pick icons from Fluent System Icons exclusively. No mixing with Material Icons, Font Awesome, or any other set.
- Use the `Regular` weight for standard UI. Use `Filled` weight for active/selected states only.
- Icon size follows the surface: 16px in tables and lists, 20px in cards, 24px in navigation, 32px in empty states.

---

## 5. Custom GACS Component Patterns

These components do not exist in Fluent UI out of the box.
Build each once, store in the shared component project, reuse across all surfaces.
Each component has defined states — junior devs do not invent new states.

### 5.1 Risk Badge

**Displays the AI-generated risk level for a visitor.**

| State | Color | Label | When |
|---|---|---|---|
| Green | `--colorRiskGreen` | Low Risk | Score 0–30 |
| Yellow | `--colorRiskYellow` | Verify Identity | Score 31–60 |
| Red | `--colorRiskRed` | High Risk | Score 61–100 |
| Pending | Neutral | Assessing... | AI score not yet returned |

Inputs: `RiskScore (int)`, `IsLoading (bool)`
No business logic inside the component — it only displays what it receives.

### 5.2 Visitor Profile Card

**The card the guard sees when a visitor QR is scanned.**

Contains:
- Visitor photo (or placeholder silhouette if no photo)
- Full name
- Purpose of visit
- Host name
- Risk Badge (see 5.1)
- Check-in timestamp
- Action buttons: Approve / Deny / Escalate

States: Loading, Loaded, Flagged (yellow/red risk), Denied, Approved

### 5.3 Consent Card

**Displays a single consent item for the visitor to accept or decline.**

Contains:
- Purpose title
- Plain-language explanation (max 2 sentences)
- Legal basis label (Consent / Legitimate Interest / Legal Obligation)
- Toggle: Accept / Decline
- Mandatory flag (some consents cannot be declined — show locked state with explanation)

One card per consent purpose. Never bundle multiple purposes into one card — POPIA requires granular consent.

### 5.4 Compliance Score Ring

**Visual representation of a site's POPIA compliance posture.**

- Circular progress ring
- Score displayed in centre (0–100)
- Color: green above 80, yellow 50–79, red below 50
- Label below: "Compliant" / "Needs Attention" / "At Risk"

Inputs: `Score (int)`, `SiteName (string)`

### 5.5 Gate Status Indicator

**Real-time status of a physical gate — shown on the guard dashboard.**

States:

| State | Color | Label |
|---|---|---|
| Open | Green | Gate Open |
| Closed | Neutral | Gate Closed |
| Processing | Blue (spinner) | Processing... |
| Alert | Red (pulsing) | Alert — Action Required |
| Offline | Grey | Offline |

Driven by SignalR events. Component subscribes to `GateHub` and updates automatically.

### 5.6 Retention Countdown

**Shows how many days remain before a visitor record is auto-deleted.**

- Progress bar (full = retention period start, empty = deletion day)
- Days remaining label
- Warning state when under 7 days
- Expired state (red) when deletion is overdue

### 5.7 AI Coaching Panel

**Appears on the guard app when risk score is Yellow or Red.**

Contains:
- Risk summary sentence (generated by Azure OpenAI, max 1 sentence)
- Checklist of verification steps (e.g. "Check photo ID", "Confirm vehicle registration", "Call host to verify")
- Dismiss button (guard must confirm they have read it — logged for audit)

Only visible in the Guard app. Never shown in admin portal or visitor-facing surfaces.

### 5.8 Multilingual Form Wrapper

**Wraps any form to support language switching across all 11 official South African languages.**

- Language selector at top of form (flag + language name)
- Selected language stored in browser local storage
- All form labels, placeholders, help text, and consent language switch on selection
- Default: English. Falls back to English if translation key is missing.

Language codes to support: `en`, `af`, `zu`, `xh`, `st`, `tn`, `ts`, `ss`, `ve`, `nr`, `nd`

---

## 6. Layout Shells

Three shells. Every screen in GACS uses one of these. Junior devs do not create new layout structures.

### Shell 1 — Admin Shell

**Used by:** Admin portal, IO portal, Compliance dashboard, Resident portal

Structure:
```
┌─────────────────────────────────────────────┐
│  Top navigation bar (logo + user menu)       │
├──────────────┬──────────────────────────────┤
│              │                              │
│  Sidebar     │   Main content area          │
│  navigation  │   (scrollable)               │
│  (fixed)     │                              │
│              │                              │
└──────────────┴──────────────────────────────┘
```

- Sidebar collapses to icon-only on narrow screens
- Top bar always visible — contains user name, role badge, notifications bell
- Content area has max-width constraint — wide screens do not stretch content edge to edge

### Shell 2 — Gate Shell

**Used by:** Guard check-in app

Structure:
```
┌─────────────────────────────────────────────┐
│  Minimal header (site name + gate name)      │
├─────────────────────────────────────────────┤
│                                             │
│   Full-screen content area                  │
│   (large touch targets — min 48px)          │
│                                             │
│   Bottom action bar (primary actions only)  │
└─────────────────────────────────────────────┘
```

- No sidebar. No complex navigation. Guards do one thing: check people in.
- All interactive elements minimum 48×48px touch target.
- Text minimum 16px — readable at arm's length.
- Dark theme default.

### Shell 3 — Visitor Shell

**Used by:** Visitor self-registration, Consent forms, Pre-registration link

Structure:
```
┌─────────────────────────────────────────────┐
│  Minimal header (GACS logo + site name)      │
├─────────────────────────────────────────────┤
│                                             │
│   Single-column content                     │
│   (centred, max-width 480px)                │
│   Mobile-first                              │
│                                             │
│   Fixed bottom CTA button                  │
└─────────────────────────────────────────────┘
```

- No navigation. Visitor has one job: complete the form.
- Fixed bottom button always visible — visitor never scrolls to find the next step.
- Progress indicator at top (Step 1 of 3 etc.)
- Works on a 320px wide screen on 3G.

---

## 7. Real-Time UI (SignalR Integration)

The guard app and admin dashboard update in real time without page refreshes.

**How it works at the frontend level:**

- Blazor Server maintains a persistent SignalR connection to the server automatically.
- The guard app (MAUI Hybrid) establishes its own SignalR client connection on launch.
- Components that need live data subscribe to hub events in `OnInitializedAsync` and call `StateHasChanged()` when data arrives.
- Components unsubscribe in `IAsyncDisposable.DisposeAsync()` — junior devs must not skip this or connections leak.

**Hubs the frontend connects to:**

| Hub | Connected By | What It Receives |
|---|---|---|
| `GateHub` | Guard app | Visitor arrivals, QR scan results, gate commands, AI risk scores |
| `AlertHub` | Guard app, Security supervisor dashboard | AI flags, incident reports, breach alerts |
| `AdminHub` | Admin portal, IO portal | Live compliance metrics, active visitor count, dashboard updates |

---

## 8. Offline Capability (Guard App Only)

The guard app must function when the tablet loses connectivity — gates do not stop because the internet is down.

**What works offline:**
- QR scan and check-in (stored locally in SQLite via MAUI)
- Photo capture
- Manual visitor entry
- Viewing pre-loaded consent templates

**What requires connectivity:**
- AI risk scoring (deferred — visitor flagged as "Pending — offline mode" until sync)
- Real-time SignalR alerts
- Hardware gate API commands (gate must have its own local controller as fallback)
- WhatsApp notifications

**Sync behaviour:**
- On reconnect, MAUI app syncs offline records to the backend automatically.
- Conflicts resolved server-side — server record wins.
- Guard sees a sync status indicator in the Gate Shell header.

---

## 9. Accessibility

Baseline: **WCAG 2.1 AA** across all surfaces.

Fluent UI Blazor handles most of this automatically. Junior devs are responsible for:

- Every image has an `alt` attribute
- Every form input has a corresponding `label`
- Color is never the only indicator of state (risk badges always show a text label alongside color)
- Keyboard navigation works on every interactive element
- Focus is never lost after a SignalR update — restore focus to the component that triggered the update

---

## 10. Project Structure (Solution Layout)

```
GACS.sln
│
├── src/
│   ├── GACS.Web.Admin/              ← Blazor Server — admin, IO, compliance portal
│   ├── GACS.Web.Visitor/            ← Blazor WASM — visitor registration, consent forms
│   ├── GACS.Web.Resident/           ← Blazor WASM — resident and host portal
│   ├── GACS.Mobile.Guard/           ← MAUI Blazor Hybrid — guard check-in app
│   └── GACS.Components/             ← Shared Razor Class Library — all custom components,
│                                       design tokens, layout shells, shared styles
│
├── src-backend/                     ← Backend services (defined in separate architecture doc)
│
└── docs/
    ├── FRONTEND-ARCHITECTURE.md     ← This document
    └── BACKEND-ARCHITECTURE.md      ← To be created
```

**The rule:** All custom components live in `GACS.Components`. No surface project defines its own components. If a component is needed in two places, it moves to `GACS.Components` immediately.

---

## 11. Pre-Build Checklist

Before any junior developer writes their first screen, the following must be in place:

- [ ] `GACS.Components` project created and referenced by all surface projects
- [ ] Fluent UI Blazor NuGet package installed in `GACS.Components`
- [ ] GACS color tokens defined in a Fluent theme override file
- [ ] Segoe UI Variable font configured in all surface projects
- [ ] Spacing scale documented and accessible (CSS custom properties or Fluent tokens)
- [ ] Fluent System Icons confirmed working in all surfaces
- [ ] All 8 custom component shells created (empty, with input parameters defined — no logic yet)
- [ ] All 3 layout shells created as Razor components in `GACS.Components`
- [ ] Dark theme configured and tested on a physical Android tablet
- [ ] SignalR hub connection wired up in Guard app and verified connected
- [ ] Multilingual form wrapper tested with at least English and isiZulu before any other form is built

---

*Next architecture document: BACKEND-ARCHITECTURE.md — API gateway, microservices, data layer, AI pipeline.*
