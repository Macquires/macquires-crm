# Playwright Live Demo Suite

Synchronized E2E scenarios for live client presentations. Each test is timed for voiceover; each scenario has an HTML cue card in `guides/`.

## Prerequisites

- App running (default `http://localhost:5000`)
- Demo users seeded (`st-mis@`, `st-showroom@`, `st-backoffice@` — password `123456`)

## Setup

```bash
cd Tests/Playwright.Demo
npm install
npm run install:browsers
```

## Run demo suite

```bash
# Headless (CI / smoke)
npm run test:demo

# Live presentation (visible browser + slow motion)
npm run test:demo:headed
```

Equivalent:

```bash
npx playwright test --grep @demo
E2E_HEADLESS=false DEMO_SLOW_MO=500 npx playwright test --grep @demo
```

## Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `BASE_URL` | `http://localhost:5000` | Target app URL |
| `E2E_HEADLESS` | `true` | Set `false` for live demo |
| `DEMO_SLOW_MO` | `500` | Playwright `slowMo` (ms) |

## Presenter cue cards

Open on phone/tablet/second screen while the robot runs:

- `guides/demo_01_command_center.html`
- `guides/demo_02_frontline_hub.html`
- `guides/demo_03_backoffice.html`

`[SPEAK NOW]` and `[DELAY - PAUSE FOR 2 SECONDS]` align with `page.waitForTimeout(2000)` in the specs.

## Scenarios

| File | Persona | Route |
|------|---------|-------|
| `Demo_01_SovereignCommandCenter_Pulse.spec.ts` | MIS executive | `/Executive/CommandCenter` |
| `Demo_02_FrontLine_POS_Hub_Morph.spec.ts` | Showroom | `/Telecom/TelecomHub` |
| `Demo_03_BackOffice_SingleSourceOfTruth.spec.ts` | Back office | `/Telecom/TelecomHub` → approval console |
