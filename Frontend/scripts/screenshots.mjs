/**
 * Regenerates the README screenshots in ../Screenshots using Playwright.
 *
 * Prerequisites: a running Tranga stack (e.g. `dotnet run --project Tranga.AppHost/Tranga.AppHost.csproj`) that already
 * contains a few mangas, and a Chromium for Playwright (`npx playwright install chromium`).
 *
 * Usage:
 *   npm run screenshots -- --base-url http://localhost:3000 [--out ../Screenshots] [--password <admin password>]
 *
 * Every option can also be set through the environment: TRANGA_URL, TRANGA_SCREENSHOT_DIR, TRANGA_PASSWORD.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import { parseArgs } from 'node:util';
import { resolve } from 'node:path';

const { values } = parseArgs({
    options: {
        'base-url': { type: 'string', default: process.env.TRANGA_URL ?? 'http://localhost:3000' },
        out: { type: 'string', default: process.env.TRANGA_SCREENSHOT_DIR ?? '../Screenshots' },
        password: { type: 'string', default: process.env.TRANGA_PASSWORD },
    },
});

const baseUrl = values['base-url'].replace(/\/$/, '');
const outDir = resolve(values.out);
await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();

/** Opens a page in the given color scheme with the first-download tour suppressed. */
const open = async (colorScheme) => {
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, colorScheme });
    await context.addInitScript(() => localStorage.setItem('tranga-tour-first-download-seen', 'true'));
    return { context, page: await context.newPage() };
};

const settle = async (page) => {
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(750);
};

const shot = async (page, name) => {
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
    await dark.page.getByPlaceholder(/search for a title/i).fill('one piece');
    await settle(dark.page);
    await shot(dark.page, 'search');
    await dark.page.keyboard.press('Escape');

    await dark.page.goto(`${baseUrl}/tasks`);
    await settle(dark.page);
    await shot(dark.page, 'tasks');

    for (const [route, name] of [
        ['/settings/sources', 'settings-sources'],
        ['/settings/appearance', 'settings-appearance'],
    ]) {
        await dark.page.goto(`${baseUrl}${route}`);
        await settle(dark.page);
        await shot(dark.page, name);
    }

    // Manga page: first manga on the start page, in dark and light mode.
    await dark.page.goto(baseUrl);
    await settle(dark.page);
    const mangaHref = await dark.page.locator('a[href^="/manga/"]').first().getAttribute('href');
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
