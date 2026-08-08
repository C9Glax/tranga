# Add Manga & Start Downloads — Frontend Workflow

Checklist — frontend-focused, no backend changes

- [ ] Enhance `Search.vue` to show existing local Manga matches while typing (instant feedback)
- [ ] Keep existing metadata search (POST /api/mangas/search) as the canonical creation step
- [ ] Improve result inspection UI: when the user clicks a metadata result, open the existing detail view and visually highlight the primary CTA ("Use this source" / "Select")
- [ ] Automatically navigate the user to the Manga Download-Sources page after they confirm a metadata entry (or provide a one-click option)
- [ ] Optionally prefetch download links for the selected metadata in background (POST /api/mangas/{mangaId}/downloadLinks)
- [ ] Make choosing a download link faster: highlight the match button and auto-focus the priority input; provide an "Auto-select best" suggestion
- [ ] Add an optional single-button "Add & Download" that performs: search → ensure manga exists → create/fetch download links → set matched + priority → trigger fetch chapters (PUT /api/tasks/create/getMangaChapters/{mangaId})
- [ ] Improve feedback: show optimistic UI and use polling for task/download progress (GET /api/tasks..., GET /api/mangas/{mangaId})
- [ ] Document in this file the UI state transitions, user flows and small implementation notes so the frontend team can implement without backend changes

Purpose

This document describes a complete frontend-only workflow (no backend changes required) for adding a manga and starting downloads using the existing Tranga HTTP API surface. It includes the sequence of calls, expected payload shapes, UI suggestions and troubleshooting notes so the frontend can implement a smooth "Add & Download" experience.

High-level flow (optimized UX)

This section describes the same server-backed steps but reorganised so the frontend experience is smooth and feels like a single guided flow.

1. User opens Search modal and types
    - While typing show a lightweight client-side/small-server lookup of already existing Manga (GET /api/mangas or filtered local cache). This provides instant suggestions and prevents duplication.
    - Allow quick keyboard navigation through suggestions.

2. User triggers Metadata search (POST /api/mangas/search)
    - Show a focused results pane (keep the search input and filters visible) and present returned Metadata entries.

3. User inspects a result
    - Clicking a result opens the existing metadata detail (`/metadata/{metadataId}`) or an inline inspect panel.
    - In that view visually emphasise the primary action ("Use as Source for Manga" / "Select" / "Add and Download") using color, placement and keyboard focus.

4. User confirms selection
    - On confirm: navigate to `/manga/{mangaId}/downloadLinks` (or `/manga/{mangaId}` with a visible DownloadSources section) so the user lands immediately where they pick a download source.
    - Implement this navigation as either a hard redirect or a soft route push keeping a small success toast.
    - In the background, prefetch POST /api/mangas/{mangaId}/downloadLinks so the download sources are ready when the user arrives.

5. User picks a download link and starts the download
    - Present download links with a prominent Match/Use button. When clicked:
        - PATCH /api/mangas/{mangaId}/downloadLinks/{downloadId} with { matched: true, priority: 0 }
        - Immediately offer a "Fetch chapters now" CTA (PUT /api/tasks/create/getMangaChapters/{mangaId}) or trigger the chained one-click flow if user selected that option.
    - Provide immediate visual feedback (toast, highlight, spinner) and start polling the Tasks API to surface progress.

Detailed step-by-step (API calls, payloads, responses) — with UX notes

Notes:

- All calls go through `/api/...` via the gateway in normal deployments.
- Replace `{mangaId}`, `{metadataId}`, `{downloadId}` with IDs from previous calls.

1. Search metadata and create manga (canonical step)

- Endpoint: POST /api/mangas/search
- Body (example):
  {
  "searchQuery": { "title": "My Favorite Manga" },
  "metadataExtensionIds": null
  }
- What it does: calls metadata extensions and persists metadata + a `DbManga` when missing. Returns Metadata DTOs.
- Response: array of Metadata objects (metadataId, series, identifier, coverId, url...)

UX note:

- The POST is the canonical creation endpoint. Use the returned Metadata array to determine which metadata has been created. If the user clicked an existing suggestion earlier, skip creating duplicates and navigate directly to that Manga.

2. (Optional) Inspect the created Manga(s)

- Endpoint: GET /api/mangas
- Or to load a single manga: GET /api/mangas/{mangaId}
- Use this to list Manga and confirm the DB object exists. The Manga DTO includes `mangaId`, `monitored`, `metadataEntry` and `downloadLinks`.

3. Find download links for a manga (frontend: "Find download sources")

- Endpoint: POST /api/mangas/{mangaId}/downloadLinks
- What it does: queries all download extensions and persists MangaDownloadLink entries.
- Response: array of MangaDownloadLink objects (downloadId, downloadExtensionId, identifier, series, url, nsfw, matched, priority).

UX suggestion:

- Prefetch this call when the user confirms metadata selection so the download links are available as soon as they land on the page.
- Render each link with a clear primary action: "Use this source" (match) and secondary action to view in new tab.
- Provide an "Auto-select best" button to programmatically choose a link (heuristics: language preference, not NSFW unless user allows, lowest priority, trusted extension).

4. Mark a download link as chosen/matched and set priority (fast path)

- Endpoint: PATCH /api/mangas/{mangaId}/downloadLinks/{downloadId}
- Body (example):
  {
  "matched": true,
  "priority": 0
  }
- What it does: marks the chosen link and assigns a priority; GetMangaChaptersTask uses matched link with smallest priority.

UX suggestion:

- When user clicks "Use this source" immediately show a small confirmation and update the UI optimistically (assume success). Refresh the Manga and DownloadLinks caches after the patch.
- Show a focused priority input; offer sensible defaults (0) and a small help tooltip explaining priority semantics.

5. Trigger a fetch of chapters for the manga (frontend: "Fetch chapters now" or auto-trigger)

- Endpoint: PUT /api/tasks/create/getMangaChapters/{mangaId}
- What it does: registers a one-shot GetMangaChaptersTask which will populate Chapters and ChapterDownloadLinks.
- Response: Task DTO (taskId, taskTypeId, lastRun, mangaId...)

UX suggestion:

- Offer this as an explicit button after matching a download link. For the one-click flow, trigger this automatically and surface the Task DTO and a progress indicator.
- Start polling GET /api/tasks/manga/{mangaId}/downloads and GET /api/tasks to surface progress. Also refresh `GET /api/mangas/{mangaId}` to see created chapters.

6. Downloading chapters (background — ensure Tasks service is running)

- The system has a periodic worker called MissingChapterScanTask that runs every ~15s and creates DownloadChapterTask instances for chapters that have DownloadLinks and no downloaded FileId. Those DownloadChapterTask instances then run and create zipped files in the persistent storage.

- If you want near-immediate downloads make sure the Tasks service/process is running in your dev environment (Tranga.AppHost wires it up in the integrated mode; in Docker Compose `services-tasks` should be up).

- You can monitor active/queued tasks via the Tasks API:
    - GET /api/tasks/manga/{mangaId}/downloads — download tasks related to the manga
    - GET /api/tasks — all tasks

- You can inspect chapters and files by querying the manga endpoints and the chapter endpoints. Example: GET /api/mangas/{mangaId} and GET /api/mangas/chapters (see API spec; the latter expects chapterIds in body). Use the Tasks endpoints for task state and the Manga endpoints for created chapters/download links.

UI pieces to implement in the frontend (concrete suggestions)

- Search modal (`Search.vue`):
    - Keep the current input and metadata extension checkboxes.
    - While typing show a small `existingMatches` list sourced from a cached `GET /api/mangas` call filtered client-side (or a lightweight server search endpoint if available).
    - Present search results in `MetadataList` as today but add an `Inspect` action that opens metadata detail inline or navigates to `/metadata/{metadataId}` with `?mangaId={maybeExisting}` when relevant.

- Metadata detail (`/metadata/{metadataId}`):
    - When the page is opened from search include `?mangaId` to allow "Use as Source for Manga" to act as "Confirm and go to download links".
    - Visually highlight the primary CTA and focus it so keyboard users can confirm quickly.

- Manga Download Links page (`/manga/{mangaId}/downloadLinks`):
    - Prefetch this page's data on confirm. Show download links using `DownloadLink/ListCard.vue` with a prominent Match button and priority input.
    - After matching, present an inline small CTA: "Fetch chapters now" and an optional checkbox "Also start downloads" for the one-click flow.

- One-click flow button (optional):
    - Implement a single action that performs: search (if needed) → find/create manga → POST downloadLinks → PATCH choose link → PUT create-getMangaChapters.
    - Show a stepper/toast that reports progress and errors and a final link to the Manga page.

Important caveats and notes (unchanged but emphasised for UX)

- No backend changes required. The flow uses the existing public API surface.
- `Monitored` remains backend-controlled and created mangas default to `Monitored = false`. The manual `PUT /api/tasks/create/getMangaChapters/{mangaId}` + MissingChapterScanTask is sufficient to populate and download chapters.
- MissingChapterScanTask runs every ~15s; if you need immediate downloads in dev shorten that interval by running the Tasks service locally (or call the fetch endpoints manually).
- Ensure the frontend runs behind the gateway and uses `/api/...` routes in production; if running the frontend against individual service ports adjust base URLs.

Implementation tips and small code notes (frontend devs)

1. Existing components to reuse
    - `Search.vue`, `Metadata/List.vue`, `Metadata/ListCard.vue`, `DownloadLink/List.vue`, `DownloadLink/ListCard.vue` already provide most visuals. Prefer enhancing these instead of creating new templates.

2. Prefetching and routing
    - When user confirms metadata selection run POST `/mangas/{mangaId}/downloadLinks` in background and then call `navigateTo(`/manga/${mangaId}/downloadLinks`)` so the page is populated immediately.

3. Optimistic UI and polling
    - After PATCH matching, optimistically update the card state and show a toast. Start polling `/tasks/manga/{mangaId}/downloads` every 2–5s and stop polling once tasks are finished.

4. Accessibility & keyboard flow
    - Focus the primary CTA on inspect view. Make sure keyboard users can accept by Enter and escape back to search results.

5. Small heuristics for "Auto-select best" (client-only)
    - Prefer non-NSFW, matching language preference (if the UI has a language setting), and common extensions. Expose an override.

6. Safety and retries
    - Network errors should show a small inline retry. For critical steps (patch/match/fetch) use a retry with exponential backoff and surface failures with contextual help.

7. Testing and dev experience
    - Add an E2E test or a small Cypress/playwright scenario that exercises the one-click flow. Also provide a small dev-only toggle to shorten MissingChapterScanTask polling for faster local testing.

Troubleshooting

- If downloads do not start:
    - Confirm `services-tasks` is running. In integrated mode run `dotnet run --project Tranga.AppHost` or `docker compose up services-tasks`.
    - Check `GET /api/tasks` to see if DownloadChapterTask entries exist and whether they have run.
    - Ensure download extensions are configured and available; missing extension will log errors and no chapters will be produced.

Appendix — quick example: simple "one-click" sequence (pseudo-code)

1. POST /api/mangas/search with { title }
2. From returned Metadata take metadataId and use GET/POST to find the created mangaId (e.g. GET /api/mangas and match by metadata identifier or call GET /api/mangas/metadata/{metadataId}/manga)
3. POST /api/mangas/{mangaId}/downloadLinks
4. PATCH /api/mangas/{mangaId}/downloadLinks/{downloadId} with { matched: true, priority: 0 }
5. PUT /api/tasks/create/getMangaChapters/{mangaId}

This sequence can be wired as a single frontend button (execute each API call sequentially and show progress to the user).

If you want, I can:

- produce a small Nuxt page/component scaffold (Vue 3 + composition API) implementing the "Add and download" one-click flow, or
- add a small CLI script (curl or node) that executes the sequence for testing.

-- End of document
