# AI Tools Integration Guide
## Nexvoy — AI Packing Assistant & AI Tour Planner

> **Last updated:** October 2026  
> This document covers every file created or modified, every design decision made,
> and everything you need to know to configure, extend, or debug these two features.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture Diagram](#2-architecture-diagram)
3. [Files Created](#3-files-created)
4. [Files Modified](#4-files-modified)
5. [Configuration — Where to Paste Your API Key](#5-configuration--where-to-paste-your-api-key)
6. [How the AI Connection Works](#6-how-the-ai-connection-works)
7. [JSON Contracts (Request & Response Shapes)](#7-json-contracts-request--response-shapes)
8. [Feature Walkthroughs](#8-feature-walkthroughs)
   - [AI Packing Assistant](#81-ai-packing-assistant)
   - [AI Tour Planner](#82-ai-tour-planner)
9. [Design Decisions — Matching the Existing Site](#9-design-decisions--matching-the-existing-site)
10. [Error Handling Reference](#10-error-handling-reference)
11. [Security Notes](#11-security-notes)
12. [How to Extend or Modify](#12-how-to-extend-or-modify)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Overview

Two fully functional AI-powered pages were added to the existing **Nexvoy** ASP.NET MVC 5 application:

| Page | URL | Controller |
|------|-----|------------|
| AI Packing Assistant | `/PackingAssistant` | `PackingAssistantController` |
| AI Tour Planner | `/TourPlanner` | `TourPlannerController` |

Both pages:
- Use the **existing shared layout** (`_Layout.cshtml`) — same navbar, footer, fonts, and colors
- Call a **server-side service** that forwards the request to your AI API
- **Never expose the API key** to the browser — it lives only in a gitignored config file
- Show a **friendly "not connected yet" message** when the API key/URL is missing
- Handle all error cases gracefully with a **Try Again** button
- Are fully functional without the API (form, UI, persistence all work independently)

No new NuGet packages were added. The project already had `Newtonsoft.Json` and `System.Net.Http` available.

---

## 2. Architecture Diagram

```
Browser
  │
  │  1. User fills form and clicks Generate
  │  2. JavaScript sends AJAX POST to /PackingAssistant/Generate
  │     (includes ASP.NET anti-forgery token, no API key)
  ▼
PackingAssistantController.Generate()  ← ASP.NET MVC Action
  │
  │  3. Server validates inputs
  │  4. Calls PackingAssistantService.GenerateAsync(request)
  ▼
PackingAssistantService
  │
  │  5. Reads API key from AiSettings (from Web.secrets.config)
  │  6. Sends HTTP POST to your AI API URL
  │     Header: Authorization: Bearer <ApiKey>
  │     Body:   JSON request object
  ▼
Your AI API  (external)
  │
  │  7. Returns JSON response
  ▼
PackingAssistantService
  │
  │  8. Deserialises JSON into typed C# objects
  │  9. Returns PackingServiceResult { Success, Error, Data }
  ▼
PackingAssistantController
  │
  │  10. Returns Json({ success, data }) — NO key in this response
  ▼
Browser (ai-packing.js)
  │
  │  11. Renders the checklist
  │  12. Saves to localStorage for persistence across refreshes
```

The same pattern applies to `TourPlannerController` / `TourPlannerService`.

---

## 3. Files Created

### `Services/AiSettings.cs`

A single static class that reads the four AI configuration keys from `appSettings`.

```csharp
AiSettings.ApiKey             // the bearer token
AiSettings.PackingApiUrl      // POST URL for packing
AiSettings.TourPlannerApiUrl  // POST URL for tour planner
AiSettings.TimeoutSeconds     // request timeout (default 60)
AiSettings.IsPackingReady     // true only when key + URL are both set
AiSettings.IsTourReady        // true only when key + URL are both set
```

All values fall back safely to `""` or `60` if the key is missing — no crash.

---

### `Services/PackingAssistantService.cs`

The core service for the packing feature. Key responsibilities:

- **Defines the request and response DTOs** (`PackingRequest`, `PackingResponse`, `PackingCategory`, `PackingItem`, `TravelerGroup`)
- **Sanitises all free-text inputs** before sending (trims, removes control characters, caps length)
- **Sends an HTTP POST** using a shared static `HttpClient` (best practice — avoids socket exhaustion)
- **Adds the auth header** server-side: `Authorization: Bearer <ApiKey>`
- **Handles every HTTP failure case**: 401/403 (bad key), 429 (rate limit), timeout, network error, malformed JSON
- **Returns a typed `PackingServiceResult`** — the controller never has to parse raw JSON

The **full JSON contract** (exactly what to send and what to expect back) is in a comment block at the top of this file.

---

### `Services/TourPlannerService.cs`

Same pattern as the packing service, but for the tour planner. Extra validation:

- Checks that end date is after start date
- Caps trip length at **30 days** (returns a friendly error, not a crash)
- Parses dates before sending so the model gets clean ISO-8601 strings

DTOs defined here: `TourPlannerRequest`, `TourPlannerResponse`, `BudgetSummary`, `BudgetBreakdown`, `TourDay`, `TourSlot`.

The **full JSON contract** is in a comment block at the top of this file.

---

### `Controllers/PackingAssistantController.cs`

Two actions:

| Action | Method | Route | Purpose |
|--------|--------|-------|---------|
| `Index()` | GET | `/PackingAssistant` | Renders the page, passes `ViewBag.IsReady` |
| `Generate()` | POST | `/PackingAssistant/Generate` | Called via AJAX, validates form, calls service, returns JSON |

Also defines `PackingFormModel` — the form model that receives the POST values from JavaScript.

Server-side validation in `Generate()`:
- Destination must not be blank
- Days must be between 1 and 365
- Trip type must be selected
- Falls back to 1 adult traveler if no traveler count is given

---

### `Controllers/TourPlannerController.cs`

Same pattern. Server-side validation in `Generate()`:

- From-city and destination must not be blank
- Start date and end date are required and must parse
- End date must be after start date
- Trip cannot exceed 30 days
- Travelers must be 1–50
- Interests string is split, trimmed, and lowercased before being sent

Also defines `TourPlannerFormModel`.

---

### `Views/PackingAssistant/Index.cshtml`

The full Packing Assistant page. Structure:

1. **`@section HeadStyles`** — links `ai-tools.css`, adds a `@media print` rule
2. **Hero section** — uses `hero-wrapper hero-inner` (identical to Packages/Destination pages)
3. **Not-connected banner** — shown when `ViewBag.IsReady` is false
4. **Form card** — floating card with `margin-top: -50px` overlapping the hero (same trick as `pkg-filters-wrapper`)
5. **Error banner** — hidden by default, revealed by JS on error
6. **Loader** — hidden spinner shown during AJAX call
7. **Results section** — hidden, revealed and populated by JS after success

The form uses `@Html.BeginForm` + `@Html.AntiForgeryToken()` so the standard ASP.NET CSRF protection applies even on AJAX calls.

---

### `Views/TourPlanner/Index.cshtml`

Same structure as the packing page. Notable additions:

- **Interest chips** — clickable `<span>` elements styled as pills; JS syncs selections into a hidden `<input>` before form submit
- **Travel style selector** — `relaxed` / `balanced` / `packed`
- **Health notes** — optional free-text field, sent to the API if filled in
- Date fields include `min` attribute logic in JS to prevent end < start

---

### `Content/frontend/ai-tools.css`

Shared stylesheet for both AI pages. Written entirely using the **same design tokens** already established in `_Layout.cshtml`:

| Token/Value | Source |
|-------------|--------|
| `#1a1a1a` (buttons, text) | Layout `--text-dark` |
| `#e5e7eb` (borders) | Layout `--border` |
| `#6b7280` (muted text) | Layout `--text-muted` |
| `#f9fafb` (input bg) | Layout `--bg-body` |
| `50px` border-radius (buttons) | Matches `.btn-primary`, `hero-btn` |
| `12px–16px` card radius | Matches `.pkg-card`, `nav-dropdown-menu` |
| `0 4px 20px rgba(0,0,0,0.06)` shadow | Matches `.pkg-card` |
| `Inter` font | Already loaded in `<head>` |
| `0.2s` transition | Matches all existing hover effects |

Sections defined in this file:

- **Form card** (`.ai-form-card`) — the floating input panel
- **Form grid** (`.ai-grid-2`, `.ai-grid-3`) — two and three-column layouts
- **Form fields** (`.ai-field`) — label + input combo
- **Interest chips** (`.ai-chip`, `.ai-chip.selected`) — matches `.pkg-filter-btn.active`
- **Traveler counters** (`.traveler-row`, `.counter-btn`, `.counter-val`)
- **Action buttons** (`.btn-ai-primary`, `.btn-ai-ghost`) — site-consistent
- **Loader** (`.ai-loader`, `.ai-spinner`) — pure CSS animation
- **Error/warning banners** (`.ai-banner`, `.ai-banner-warn`, `.ai-banner-error`)
- **Results header** (`.ai-results-header`, `.ai-result-btns`)
- **Packing checklist** (`.category-section`, `.check-item`, `.priority-badge`, `.packing-progress-bar`)
- **Tour planner** (`.day-tabs`, `.day-tab`, `.day-card`, `.slot-item`, `.budget-card`, `.health-notes-box`)
- **Responsive rules** — `@media (max-width: 768px)` and `@media (max-width: 480px)`

---

### `Scripts/ai-packing.js`

All client-side logic for the Packing Assistant. Written in plain ES5 (no transpiler needed, compatible with all browsers the project targets).

| Feature | How it works |
|---------|-------------|
| Counter buttons | Read `data-target` and `data-dir` attributes, clamp to `data-min`/`data-max` |
| Form submit | `fetch()` POST to `/PackingAssistant/Generate`, URL-encoded body |
| Result rendering | Builds DOM elements programmatically; all strings HTML-escaped via `esc()` |
| Checklist toggle | Click or Enter/Space on `.item-check` toggles `.packed` class |
| Delete item | Removes the `.check-item` div |
| Add custom item | Reads `.new-item-input`, creates a new `.check-item` |
| Progress bar | Counts `.packed` vs total `.check-item`, updates fill width and label |
| Print | `window.print()` — the CSS `@media print` rule hides the form |
| Copy as text | Builds a plain-text string from `currentData`, uses `navigator.clipboard` |
| Regenerate | Hides results, clears error, re-submits the form |
| Reset ticks | Removes `.packed` class from all items |
| localStorage | Saves `currentData` on every successful API call; restores on page load |

---

### `Scripts/ai-tourplanner.js`

All client-side logic for the Tour Planner.

| Feature | How it works |
|---------|-------------|
| Interest chips | Click toggles `.selected`; on submit, selected `data-value` attrs are joined and put in `#Interests` hidden input |
| Date guard | `startDate` change event sets `endInput.min` and clears invalid end dates |
| Day tabs | Click hides all `.day-card`, shows the one matching `data-day-idx` |
| Slot rendering | Builds `.slot-item` with morning/afternoon/evening icon and colors |
| Inline slot edit | "Edit" button toggles `.slot-edit-area.open`; Save writes back to `.slot-desc` |
| Budget card | Renders `totalBudget.breakdown` as a grid of `.budget-item` tiles |
| Health notes | Renders `.health-notes-box` only if the API returned a non-empty `healthNotes` string |
| Save plan | Writes `currentData` to `localStorage` under key `nexvoy_tourplan_v1` |
| Restore plan | On page load, reads `localStorage` and re-renders if a plan is found |

---

### `Web.secrets.config`

A plain XML file containing only the four AI config keys, all empty by default.

```xml
<appSettings>
  <add key="AI:ApiKey"            value="" />
  <add key="AI:PackingApiUrl"     value="" />
  <add key="AI:TourPlannerApiUrl" value="" />
  <add key="AI:TimeoutSeconds"    value="60" />
</appSettings>
```

**This file is excluded from source control** (see `.gitignore`). It is referenced by `Web.config` via `file="Web.secrets.config"`. Keys in this file override keys in `Web.config`. This is a standard ASP.NET pattern for keeping secrets out of git.

---

## 4. Files Modified

### `Web.config`

Two changes:

1. Added `file="Web.secrets.config"` to the `<appSettings>` opening tag so the external secrets file is merged in at runtime.
2. Added four `AI:*` appSettings keys with empty values and explanatory comments. These serve as documentation and as fallback values when `Web.secrets.config` does not override them.

### `Views/Shared/_Layout.cshtml`

One change only — inside the existing "More" dropdown menu, one new `<a>` tag was added:

```html
<a href="@Url.Action("Index", "TourPlanner")">
    <i class="fas fa-route"></i> AI Tour Planner
</a>
```

The "Packing Assistance" label was also corrected to "Packing Assistant" for consistency with the page title. No other changes were made to the layout.

### `.gitignore`

Added one line:

```
Web.secrets.config
```

This ensures the file containing your API key can never be accidentally committed to source control.

---

## 5. Configuration — Where to Paste Your API Key

**Open `TourFYP/Web.secrets.config`** and replace the empty `value=""` strings:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<appSettings>
  <add key="AI:ApiKey"            value="YOUR_API_KEY_HERE" />
  <add key="AI:PackingApiUrl"     value="https://your-api.example.com/v1/packing" />
  <add key="AI:TourPlannerApiUrl" value="https://your-api.example.com/v1/tour-plan" />
  <add key="AI:TimeoutSeconds"    value="60" />
</appSettings>
```

**Rules:**
- `AI:ApiKey` — the bearer token sent in every request header as `Authorization: Bearer <value>`
- `AI:PackingApiUrl` — must be an `https://` URL that accepts a POST with JSON body
- `AI:TourPlannerApiUrl` — must be an `https://` URL that accepts a POST with JSON body
- `AI:TimeoutSeconds` — increase this if your model is slow (e.g., `120` for large models)
- Both features can use the **same** `AI:ApiKey` but must have **separate** endpoint URLs
- Save the file — **no rebuild or app restart required** in debug mode

---

## 6. How the AI Connection Works

### Request Flow (step by step)

1. User fills the form and clicks **Generate**
2. JavaScript collects all field values and sends an AJAX `POST` to the MVC controller action (e.g., `/PackingAssistant/Generate`)
3. The request body is `application/x-www-form-urlencoded` — standard MVC form data, includes the ASP.NET anti-forgery token
4. The controller action validates inputs, builds a typed request object, and calls the service
5. The service reads `AiSettings.ApiKey` and `AiSettings.PackingApiUrl` from config (never from the browser request)
6. The service sends an `HTTP POST` to your AI API URL with:
   - Header: `Authorization: Bearer <ApiKey>`
   - Header: `Content-Type: application/json`
   - Body: the request object serialised to JSON (see Section 7)
7. The service reads the JSON response and deserialises it into typed C# objects
8. The controller returns `Json({ success: true, data: ... })` to the browser
9. JavaScript renders the result — the API key is **never** in this JSON

### What "not connected" means

If `AI:ApiKey` is empty **or** the relevant URL is empty, `AiSettings.IsPackingReady` / `IsTourReady` returns `false`. The controller still loads the page but:
- A yellow warning banner appears at the top
- Clicking Generate returns `{ success: false, error: "AI is not connected yet. Please add the API details in config." }`
- The form, checklist, and all UI elements work normally

---

## 7. JSON Contracts (Request & Response Shapes)

### Packing Assistant

#### Request (your server → your AI API)

```json
{
  "destination":   "Paris, France",
  "days":          7,
  "startDate":     "2025-03-10",
  "endDate":       "2025-03-17",
  "tripType":      "honeymoon",
  "travelers": [
    { "ageGroup": "adult",  "count": 2 },
    { "ageGroup": "child",  "count": 1 },
    { "ageGroup": "senior", "count": 0 }
  ],
  "weather":       "cold, rainy",
  "specialNeeds":  "one traveler uses a cane"
}
```

| Field | Type | Notes |
|-------|------|-------|
| `destination` | string | Sanitised, max 200 chars |
| `days` | integer | 1–365 |
| `startDate` / `endDate` | string | ISO-8601 (`YYYY-MM-DD`), optional |
| `tripType` | string | `adventure` \| `family` \| `business` \| `honeymoon` \| `religious` \| `budget` |
| `travelers[].ageGroup` | string | `adult` \| `child` \| `senior` |
| `travelers[].count` | integer | Groups with count 0 are excluded |
| `weather` | string | Free text, max 200 chars |
| `specialNeeds` | string | Free text, max 500 chars |

#### Response (your AI API → your server)

```json
{
  "weatherTip":  "Paris in March is cold — pack a waterproof layer.",
  "generalTips": [
    "Carry a universal power adapter",
    "Keep photocopies of all documents"
  ],
  "categories": [
    {
      "name": "Clothes",
      "items": [
        { "name": "Warm jacket",    "quantity": "2",           "priority": "Essential" },
        { "name": "Rain poncho",    "quantity": "1",           "priority": "Recommended" },
        { "name": "Formal outfit",  "quantity": "1",           "priority": "Optional" }
      ]
    },
    {
      "name": "Documents",
      "items": [
        { "name": "Passport",       "quantity": "1 per person","priority": "Essential" },
        { "name": "Travel insurance","quantity": "1 copy",     "priority": "Essential" }
      ]
    }
  ]
}
```

| Field | Rules |
|-------|-------|
| `priority` | Must be exactly: `Essential` \| `Recommended` \| `Optional` |
| `categories[].name` | Should be one of: `Clothes` \| `Documents` \| `Electronics` \| `Toiletries` \| `Medicines` \| `Accessories` \| `Trip-Specific` |
| `weatherTip` | Short string shown in the green tip box |
| `generalTips` | Array of short strings shown as a bullet list |
| `quantity` | Free string — e.g. `"2"`, `"1 per person"`, `"enough for 7 days"` |

---

### Tour Planner

#### Request (your server → your AI API)

```json
{
  "fromCity":    "Karachi",
  "destination": "Istanbul, Turkey",
  "startDate":   "2025-04-01",
  "endDate":     "2025-04-07",
  "budgetMin":   1500,
  "budgetMax":   3000,
  "currency":    "USD",
  "travelers":   2,
  "interests":   ["culture", "food", "history"],
  "travelStyle": "balanced",
  "healthNotes": "one traveler has a knee injury, avoid steep hikes"
}
```

| Field | Type | Notes |
|-------|------|-------|
| `fromCity` / `destination` | string | Sanitised, max 200 chars |
| `startDate` / `endDate` | string | ISO-8601, required, max 30-day span |
| `budgetMin` / `budgetMax` | decimal | In USD |
| `travelers` | integer | 1–50 |
| `interests` | string[] | Subset of: `nature` \| `culture` \| `food` \| `adventure` \| `shopping` \| `history` \| `relaxation` |
| `travelStyle` | string | `relaxed` (2 slots/day) \| `balanced` (3–4) \| `packed` (5+) |
| `healthNotes` | string | Optional, max 500 chars, empty string if not provided |

#### Response (your AI API → your server)

```json
{
  "totalBudget": {
    "estimated":  2400,
    "currency":   "USD",
    "breakdown": {
      "accommodation": 900,
      "food":          480,
      "transport":     320,
      "activities":    400,
      "misc":          300
    }
  },
  "healthNotes": "All venues are wheelchair-accessible. Steep terrain avoided.",
  "days": [
    {
      "dayNumber": 1,
      "date":      "2025-04-01",
      "title":     "Arrival & Old City",
      "slots": [
        {
          "period":        "Morning",
          "time":          "09:00",
          "place":         "Sultanahmet Square",
          "description":   "Arrive and explore the historic square.",
          "estimatedCost": 0
        },
        {
          "period":        "Afternoon",
          "time":          "13:00",
          "place":         "Hagia Sophia",
          "description":   "Guided tour of the iconic cathedral-mosque.",
          "estimatedCost": 25
        },
        {
          "period":        "Evening",
          "time":          "19:00",
          "place":         "Karaköy waterfront",
          "description":   "Dinner overlooking the Bosphorus.",
          "estimatedCost": 35
        }
      ]
    }
  ]
}
```

| Field | Rules |
|-------|-------|
| `days[].slots[].period` | Must be exactly: `Morning` \| `Afternoon` \| `Evening` |
| `totalBudget.breakdown` fields | All five fields should be present (can be `0`) |
| `healthNotes` (response) | Omit or use `""` if no health notes were requested |
| `estimatedCost` | Per-person cost in the requested currency |

---

## 8. Feature Walkthroughs

### 8.1 AI Packing Assistant

**URL:** `/PackingAssistant`

#### User flow

1. User arrives at the page — the hero, form card, and (if not connected) the yellow banner are visible
2. User fills in: destination, trip type, number of days, weather, traveler counts, optional dates and special needs
3. User clicks **Generate Packing List**
4. A spinner appears while the server calls the AI API
5. Results appear below the form — divided into category sections (Clothes, Documents, etc.)
6. Each item has a checkbox, name, quantity, priority badge (red/yellow/blue), and a delete button
7. A progress bar tracks how many items have been ticked as packed
8. User can add custom items to any category using the text input at the bottom of each section
9. User can tick/untick items — the progress bar updates in real time
10. The checklist is automatically saved to `localStorage` — refreshing the page restores it
11. **Regenerate** — re-submits the same form to get a fresh list from the AI
12. **Reset Ticks** — clears all checkmarks but keeps the list
13. **Print** — triggers `window.print()`; the form and nav are hidden via `@media print`
14. **Copy as Text** — copies the entire checklist as plain text to the clipboard

#### Priority badge colours

| Priority | Color |
|----------|-------|
| Essential | Red background (`#fef2f2`) |
| Recommended | Yellow background (`#fffbeb`) |
| Optional | Blue background (`#f0f9ff`) |

---

### 8.2 AI Tour Planner

**URL:** `/TourPlanner`

#### User flow

1. User fills in: starting city, destination, start/end dates, budget range, traveler count
2. User selects interests by clicking pill chips (Nature, Culture, Food, etc.)
3. User selects travel style: Relaxed / Balanced / Packed
4. User optionally enters health conditions
5. User clicks **Generate Itinerary**
6. A spinner appears while the server calls the AI API
7. Results appear — starting with a **Budget Summary card** (total + breakdown grid)
8. If health notes were provided, a **Health-Friendly Notes** box appears
9. **Day tabs** appear (Day 1, Day 2, …) — clicking a tab shows that day's card
10. Each day card shows morning, afternoon, and evening slots with time, place, description, and cost
11. User can click **Edit** on any slot to change the description inline (changes are local only)
12. **Save Plan** — saves the current itinerary to `localStorage`; restoring on next visit
13. **Regenerate** — re-submits the form
14. **Print** — all day cards are shown in print mode

---

## 9. Design Decisions — Matching the Existing Site

### Why a floating form card?

The existing `pkg-filters-wrapper` (on the Packages page) uses `margin-top: -40px` to overlap the hero section. The AI pages use the same trick (`margin-top: -50px` on `.ai-form-card`) to create visual continuity. A visitor switching between Packages and AI tools sees the same layout rhythm.

### Why `hero-wrapper hero-inner`?

All inner pages in the site (Packages, Destination, Booking) use `hero-wrapper hero-inner` for a shorter hero that shares the same background image and overlay as the homepage. Adding the same class means the AI pages automatically pick up any future changes to that hero style.

### Why no dark theme?

The existing user-facing site (`_Layout.cshtml`) uses a light theme: `--bg-body: #f8f9fa`, `--bg-white: #ffffff`, dark text on light backgrounds. The previous stubs (`Views/PackingListGenerator/Index.cshtml`, `Views/RecommendationAI/Index.cshtml`) had overridden `body { background: #020205 }` which made them look completely different from the rest of the site. The new pages do not override `body` at all — they inherit the layout's styles.

### Why no new fonts or icon sets?

`Inter` is already loaded in `<head>` in `_Layout.cshtml`. Font Awesome 6 is also already loaded. Adding another font or icon set would create a flash of unstyled content and slow down the page.

### Why `application/x-www-form-urlencoded` for AJAX?

The existing site uses standard MVC form submissions. Using `application/x-www-form-urlencoded` means the existing `[ValidateAntiForgeryToken]` attribute works without any extra configuration. If `application/json` were used, a custom filter would be needed to read the anti-forgery token from the request header.

### Why separate JS files?

Each feature has one JS file. This keeps each file focused, easy to read, and independently replaceable. Both files are plain ES5 with no bundler or transpiler required — matching the rest of the project's JavaScript (jQuery 3.7, no build step).

---

## 10. Error Handling Reference

| Scenario | What the user sees |
|----------|-------------------|
| API key or URL not configured | Yellow warning banner on page load; on Generate: "AI is not connected yet" |
| Client-side validation fails | Inline error message without contacting server |
| Server-side validation fails | Red error banner with the specific validation message |
| HTTP 401 / 403 (bad key) | "Invalid or expired API key. Please check your configuration." |
| HTTP 429 (rate limit) | "Rate limit reached. Please wait a moment and try again." |
| HTTP 5xx (server error) | "The AI service returned an error (HTTP 5xx)." |
| Timeout | "The request timed out. Please try again." |
| No internet / DNS failure | "Could not reach the AI service. Check your internet connection." |
| Malformed / incomplete JSON | "The AI response could not be read. Please try again." |
| Response with no categories / no days | "The AI returned an incomplete response. Please try again." |

All error states show a **Try Again** button that re-submits the form without the user having to scroll back up.

---

## 11. Security Notes

- **The API key is never sent to the browser.** It is read from `AiSettings.cs` (which reads from `Web.secrets.config`) entirely on the server.
- **`Web.secrets.config` is in `.gitignore`.** It can never be accidentally pushed to source control.
- **Anti-forgery tokens** are used on both AJAX POST actions via `[ValidateAntiForgeryToken]`. This prevents cross-site request forgery.
- **All free-text user inputs are sanitised** before being forwarded to the AI API: trimmed, control characters removed, and capped at sensible lengths (200 or 500 characters).
- **No user input is reflected back as HTML** — all strings pass through the `esc()` function in JavaScript before being inserted into the DOM.
- **The AI response is not trusted blindly** — the service checks for null/empty required fields and returns a safe error if the response is incomplete.

---

## 12. How to Extend or Modify

### Change the request fields sent to the AI

Open the relevant service file (`PackingAssistantService.cs` or `TourPlannerService.cs`).

The request DTO is near the top, after the JSON contract comment:

```csharp
public class PackingRequest
{
    [JsonProperty("destination")]  public string Destination  { get; set; }
    // Add your new field here:
    [JsonProperty("language")]     public string Language     { get; set; }
    ...
}
```

Then update:
1. The form model in the controller (`PackingFormModel`)
2. The controller action to read and assign the new field
3. The view to add a form input
4. The JS file to include the field in the AJAX POST body

### Change the response shape

Update the response DTO (`PackingResponse` / `TourPlannerResponse`) in the service file, then update the JS renderer (`renderResults()` function) to use the new fields.

### Add a third AI feature

Follow the same pattern:
1. Create `Services/YourFeatureService.cs` with request/response DTOs and the same `HttpClient` pattern
2. Create `Controllers/YourFeatureController.cs` with GET + AJAX POST actions
3. Create `Views/YourFeature/Index.cshtml` using `hero-wrapper hero-inner` and `ai-tools.css`
4. Create `Scripts/ai-yourfeature.js`
5. Add the URL to `Web.secrets.config` and a new `AI:YourFeatureApiUrl` key to `Web.config`
6. Add a new `<a>` link in the "More" dropdown in `_Layout.cshtml`

### Change the timeout

In `Web.secrets.config` (or `Web.config`):
```xml
<add key="AI:TimeoutSeconds" value="120" />
```

No code change needed.

---

## 13. Troubleshooting

**Page shows a 404**  
The controller exists but the view directory name must match the controller name exactly. Verify that `Views/PackingAssistant/` and `Views/TourPlanner/` exist with `Index.cshtml` inside them.

**Yellow banner always shows**  
Check that `Web.secrets.config` exists in the `TourFYP/` project folder (same level as `Web.config`) and that `AI:ApiKey` and the relevant URL key both have non-empty values.

**Generate button does nothing (no spinner, no error)**  
Open browser DevTools → Console. A JavaScript error usually means the script file path is wrong. Verify that `Scripts/ai-packing.js` and `Scripts/ai-tourplanner.js` are accessible at `/Scripts/ai-packing.js`.

**"Invalid or expired API key" error**  
The AI API returned HTTP 401 or 403. Check that `AI:ApiKey` in `Web.secrets.config` is correct and hasn't expired.

**Timeout error**  
Increase `AI:TimeoutSeconds` in `Web.secrets.config`. Large language models can take 30–90 seconds for complex requests.

**Results appear then disappear on refresh**  
`localStorage` is working correctly — the checklist/plan is being restored from storage. If it looks empty, the stored data may be incomplete. Open DevTools → Application → Local Storage and delete the `nexvoy_packing_v1` or `nexvoy_tourplan_v1` key to reset.

**CSS looks wrong / different from other pages**  
Make sure `ai-tools.css` is not overriding `body { background: ... }` or any root variable. The file only adds new classes, never overrides existing ones. If in doubt, check the browser DevTools CSS cascade for the affected element.

---

*End of guide.*
