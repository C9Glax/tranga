/**
 * Regenerates the README screenshots in ../Screenshots using Playwright.
 *
 * All screenshots are taken at 1920x1080 (1080p).
 *
 * Prerequisites: a running Tranga stack (e.g. `dotnet run --project Tranga.AppHost/Tranga.AppHost.csproj`) that already
 * contains a few mangas, and a Chromium for Playwright (`npx playwright install chromium`).
 *
 * The sources screenshots temporarily create a custom source from the built-in WeebCentral example (through the UI) and
 * delete it again afterwards, so the stack ends up as it was. Its search test needs internet access; if WeebCentral
 * cannot be reached that one screenshot is skipped. Use --skip-sources to leave the custom source parts out entirely.
 *
 * Usage:
 *   npm run screenshots -- --base-url http://localhost:5000 [--out ../Screenshots] [--password <admin password>]
 *
 * Every option can also be set through the environment: TRANGA_URL, TRANGA_SCREENSHOT_DIR, TRANGA_PASSWORD.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import { parseArgs } from 'node:util';
import { resolve } from 'node:path';

const { values } = parseArgs({
    options: {
        'base-url': { type: 'string', default: process.env.TRANGA_URL ?? 'http://localhost:5000' },
        out: { type: 'string', default: process.env.TRANGA_SCREENSHOT_DIR ?? '../Screenshots' },
        password: { type: 'string', default: process.env.TRANGA_PASSWORD },
        'skip-sources': { type: 'boolean', default: false },
    },
});

/** 1080p */
const viewport = { width: 1920, height: 1080 };

const baseUrl = values['base-url'].replace(/\/$/, '');
const outDir = resolve(values.out);
await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();

/** Opens a page in the given color scheme with the first-download tour suppressed. */
const open = async (colorScheme) => {
    const context = await browser.newContext({ viewport, deviceScaleFactor: 1, colorScheme });
    await context.addInitScript(() => localStorage.setItem('tranga-tour-first-download-seen', 'true'));
    return { context, page: await context.newPage() };
};

const settle = async (page) => {
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(750);
};

/** Closes any toast notifications, so they do not end up in the screenshot. */
const dismissToasts = async (page) => {
    for (const close of await page.getByRole('button', { name: 'Close' }).all()) await close.click().catch(() => {});
    await page.waitForTimeout(400);
};

const shot = async (page, name) => {
    await dismissToasts(page);
    await page.screenshot({ path: `${outDir}/${name}.png` });
    console.log(`wrote ${name}.png`);
};

const login = async (page) => {
    await page.goto(baseUrl);
    await settle(page);
    if (page.url().includes('/login') && values.password) {
        await page
            .getByLabel(/password/i)
            .first()
            .fill(values.password);
        await page.keyboard.press('Enter');
        await page.waitForURL((url) => !url.pathname.startsWith('/login'));
    }
};

/** Name the temporary example source gets, so it can be told apart from (and removed without touching) real sources. */
const EXAMPLE_SOURCE = 'WeebCentral';

const sourceCard = (page) =>
    page
        .locator('div')
        .filter({ has: page.getByText(EXAMPLE_SOURCE, { exact: true }) })
        .filter({ has: page.getByRole('button', { name: 'Delete' }) })
        .last(); // the innermost div that holds both the name and its buttons

const deleteExampleSource = async (page) => {
    await page.goto(`${baseUrl}/settings/sources`);
    await settle(page);
    await sourceCard(page).getByRole('button', { name: 'Delete' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click();
    await page.getByText(EXAMPLE_SOURCE, { exact: true }).waitFor({ state: 'detached' });
};

/**
 * Settings -> Sources: the list, the "new source" form filled from the built-in example, a live test of its search step and
 * the list again with the new custom source. The example source is deleted again at the end.
 */
const sourcesScreenshots = async (page) => {
    await page.goto(`${baseUrl}/settings/sources`);
    await settle(page);
    await shot(page, 'settings-sources');

    if (values['skip-sources']) return;
    if ((await page.getByText(EXAMPLE_SOURCE, { exact: true }).count()) > 0) {
        console.warn(`A source named "${EXAMPLE_SOURCE}" already exists - skipped the custom source screenshots.`);
        return;
    }

    let created = false;
    try {
        await page.goto(`${baseUrl}/settings/sources/new`);
        await settle(page);
        await page.getByRole('button', { name: /start from an example/i }).click();
        await page.getByRole('menuitem', { name: /weebcentral/i }).click();
        await page.getByLabel('Name', { exact: true }).fill(EXAMPLE_SOURCE);
        await page.waitForTimeout(500);
        await shot(page, 'settings-sources-new');

        // Go to the search step (the form is a stepper), run it against the real website and show what the selectors
        // extracted: the result appears next to the step.
        await page.getByRole('button', { name: 'Next', exact: true }).click();
        await page.getByPlaceholder('one piece').fill('one piece');
        await page.getByRole('button', { name: 'Test', exact: true }).first().click();
        try {
            await page.getByText(/item\(s\) extracted/).waitFor({ timeout: 60_000 });
            // Bring the test (input + button) into the middle of the view. The page scrolls inside the dashboard panel,
            // so scrolling the window would do nothing.
            await page.getByText('Search text', { exact: true }).evaluate((el) => el.scrollIntoView({ block: 'center' }));
            await page.waitForTimeout(500);
            await shot(page, 'settings-sources-test');
        } catch {
            console.warn(
                'The search test did not finish (no internet access / WeebCentral unreachable?) - skipped settings-sources-test.png.',
            );
        }

        // Creating is only possible on the last step (search -> chapters -> pages).
        await page.getByRole('button', { name: 'Next', exact: true }).click();
        await page.getByRole('button', { name: 'Next', exact: true }).click();
        await page.getByRole('button', { name: 'Create source' }).click();
        await page.waitForURL((url) => url.pathname === '/settings/sources');
        created = true;
        await settle(page);
        await shot(page, 'settings-sources-custom');
    } finally {
        if (created) await deleteExampleSource(page);
    }
};

try {
    const dark = await open('dark');
    await login(dark.page);

    await dark.page.goto(baseUrl);
    await settle(dark.page);
    await shot(dark.page, 'startpage');

    // Search dialog (the search button in the navbar opens the dashboard search).
    await dark.page
        .getByRole('button', { name: /search/i })
        .first()
        .click();
    await dark.page.getByPlaceholder(/search for a title/i).fill('destiny unchain');
    await settle(dark.page);
    await shot(dark.page, 'search');
    await dark.page.keyboard.press('Escape');

    await dark.page.goto(`${baseUrl}/tasks`);
    await settle(dark.page);
    await shot(dark.page, 'tasks');

    await sourcesScreenshots(dark.page);

    // Settings is a single tabbed page; Appearance is one of its tabs.
    await dark.page.goto(`${baseUrl}/settings`);
    await settle(dark.page);
    await dark.page.getByRole('tab', { name: 'Appearance' }).click();
    await settle(dark.page);
    await shot(dark.page, 'settings-appearance');

    // Manga page: first manga on the start page, in dark and light mode.
    await dark.page.goto(baseUrl);
    await settle(dark.page);
    const mangaLink = dark.page.locator('a[href^="/manga/"]').first();
    const mangaHref = (await mangaLink.count()) > 0 ? await mangaLink.getAttribute('href') : null;
    if (mangaHref) {
        await dark.page.goto(`${baseUrl}${mangaHref}`);
        await settle(dark.page);
        await shot(dark.page, 'manga');

        const light = await open('light');
        await light.page.goto(baseUrl);
        await settle(light.page);
        if (light.page.url().includes('/login')) await login(light.page);
        await light.page.goto(`${baseUrl}${mangaHref}`);
        await settle(light.page);
        await shot(light.page, 'manga-light');
        await light.context.close();
    } else {
        console.warn('No manga found on the start page - skipped manga screenshots. Add a manga first.');
    }

    await dark.context.close();
} finally {
    await browser.close();
}
