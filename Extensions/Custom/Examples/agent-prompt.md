# Prompt: create a Tranga custom source with an AI agent

Give this whole file to an AI agent that can fetch web pages (for example with `curl`), and tell it which website you
want. It answers with a JSON definition that you paste into **Settings → Sources → Add custom source → Import JSON**
and check with the test buttons before saving.

Everything below the line is the prompt. Replace `<WEBSITE>` in its first sentence with the address of the website (and,
if you like, the title of a series that exists there, which makes the agent's tests easier).

---

Your task: create a **Tranga custom download source** for `<WEBSITE>`, as one JSON document.

Tranga is a manga downloader. A custom source tells it how to use a website without any code: Tranga downloads
HTML pages and picks the data it needs out of them with CSS selectors or XPath expressions (and, where needed, a
regular expression). Three steps are described, each with its own page: **search** (find series by a title),
**chapters** (list the chapters of a series) and **images** (find the page images of a chapter).

## Ground rules

- Tranga only sees what the server sends. It does **not** run JavaScript and it only parses **HTML** (not JSON).
  Work from the raw response (`curl`), never from what a browser shows after scripts ran. Pages that build their
  content with JavaScript do not work, unless the same content is also available as HTML, often as an HTML
  fragment loaded from a separate URL (look for it in the page's scripts, `hx-get` or `x-data` attributes).
- Tranga requests the pages with a normal browser `User-Agent`. Do the same:
  `curl -sL -A 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36' <url>`.
- Be polite: few requests, no hammering. This is for a website the user is allowed to read and download from.
- Never invent selectors. Every selector in the answer has to come from the HTML you actually fetched, and every step
  has to be checked (see "Verify").
- Only public websites work: addresses on a private network (localhost, 192.168.x.x, ...) are refused by default.

## How to work

1. **Search.** Find the search URL of the website (try a search in the page, or look at the search form). Fetch the
   results for a title you know exists. Find the element that is repeated once per result (the *item*), and in it the
   title, the link to the series and the cover image.
2. **Series page.** Fetch a series page from the results. Find the list of its chapters (it may be loaded from
   another URL, see `seriesUrlPattern` and the `{id}` placeholder below). Find the element repeated once per chapter,
   and in it the link, and if there are any the number, title and date. Check whether the list is paginated.
3. **Chapter page.** Fetch a chapter. Find the page images, in reading order. Check whether all images are on this
   page, or whether every page of the chapter is its own HTML page (then use `pageSelector`).
4. **Write the definition**, then **verify** it, then **answer** (see below).

## The definition

All names are camelCase. Every field that is not needed can be left out. This skeleton contains every field there is,
with example values:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "My Manga Site",
  "baseUrl": "https://example.com",
  "iconUrl": "https://example.com/favicon.ico",
  "languages": ["en"],
  "useFlareSolverr": false,
  "requestsPerSecond": 1,
  "headers": { "Cookie": "adult=1" },
  "seriesUrlPattern": "^https://example\\.com/series/(?<id>[^/?#]+)",
  "search": {
    "urlTemplate": "/search?q={query}&page={page}",
    "itemSelector": "div.result",
    "title": { "selector": "h3 a", "attribute": "title", "regex": "^(.+?) \\(Manga\\)$", "replacement": "" },
    "url": { "selector": "h3 a", "attribute": "href" },
    "cover": { "selector": "img", "attribute": "data-src,src" }
  },
  "chapters": {
    "urlTemplate": "/series/{id}/chapters",
    "itemSelector": "ul.chapters li",
    "url": { "selector": "a", "attribute": "href" },
    "number": { "selector": "a", "regex": "Chapter (\\d+(?:\\.\\d+)?)" },
    "title": { "selector": "span.title" },
    "date": { "selector": "time", "attribute": "datetime" },
    "dateFormat": "yyyy-MM-dd",
    "nextPage": { "selector": "a.next", "attribute": "href" }
  },
  "images": {
    "urlTemplate": "{url}",
    "pageSelector": { "selector": "select.pages option", "attribute": "value" },
    "imageSelector": "div.reader img",
    "attribute": "data-src,src"
  }
}
```

### General

| Field | Meaning |
|---|---|
| `id` | Always leave it as all zeros. Tranga assigns the real one when the source is created. |
| `name` | Display name of the source. |
| `baseUrl` | Root of the website, without a trailing `/`. Relative URLs are resolved against it. |
| `iconUrl` | Optional. Defaults to the website's favicon. |
| `languages` | Languages the website publishes in, as codes like `en`, `en-us` or `pt-BR`. At least one. |
| `useFlareSolverr` | `true` only if the website sits behind a Cloudflare challenge (a plain request is answered with a "Just a moment" page or HTTP 403/503). Needs a FlareSolverr instance on the Tranga installation. |
| `requestsPerSecond` | Requests per second to the website, 0.1 to 20. Be conservative (0.5 to 2). |
| `headers` | Extra request headers (a cookie, a `User-Agent`, ...). `Host`, `Content-Length`, `Transfer-Encoding` and `Connection` cannot be set. |
| `seriesUrlPattern` | Optional regular expression a series URL has to match. Its **named groups** (`(?<id>…)`) can be used as `{id}` in `chapters.urlTemplate`. Needed when the chapter list is at a different URL than the series page. |

### Selector fields

Every value to read (`title`, `url`, `cover`, `number`, `date`, `nextPage`, `pageSelector`) is an object with up to four parts:

| Part | Meaning |
|---|---|
| `selector` | CSS selector, or XPath when it starts with `/`, `./`, `../` or `(`. **Relative to the item** (the element repeated per result or chapter). Empty means the item itself, which is how you read the link of an item that is an `<a>`. XPath may end in `/@attribute` or `/text()`. |
| `attribute` | What to read. Empty reads the element's text. `href`, `src`, `data-src`, … read an attribute. A comma separated list (`srcset,src`) is tried in order, the first non-empty value wins. |
| `regex` | Applied to the value. Without `replacement` the first capture group (or the whole match) is kept, and a value that does not match counts as missing. |
| `replacement` | When set, `regex` replaces every match with this text instead of extracting. |

Relative URLs are resolved against the page they were found on. Only `http(s)` URLs are kept. CSS is handled by
AngleSharp, so `:not()`, `:has()`, `:nth-child()`, `:contains()` and attribute selectors work.

### Search

- `urlTemplate`: `{query}` is the URL-encoded search text, `{page}` is always `1`. Relative to `baseUrl`.
- `itemSelector`: one element per result: the wrapper around the whole result. `title`, `url` and `cover` are looked up
  **inside** it (it may also be the link itself, then leave `selector` empty). Make it match results only, not other
  links on the page.
- `title`, `url` and `cover` are all required.

### Chapters

- `urlTemplate`: the page that lists the chapters. `{url}` is the series URL, the named groups of `seriesUrlPattern`
  (for example `{id}`) are available too. Defaults to `{url}`.
- `itemSelector`: one element per chapter. `url` is required.
- `number`: the chapter number. When unused, the item's text is searched. The **first number** in the extracted text is
  used (`Chapter 12.5` becomes `12.5`); chapters without a number are skipped. If the number is only in the link
  (`…/c012.5/`), read the `href` and use a `regex` like `/c(\d+(?:\.\d+)?)/?$`.
- `title`, `date` and `dateFormat` are optional. `dateFormat` is a .NET format such as `yyyy-MM-dd` or `MMM dd,yyyy`;
  ISO 8601 and common formats work without it.
- `nextPage`: optional link to the next page of the chapter list (followed up to 50 times).

### Images

- `urlTemplate`: the page that shows the images. `{url}` is the chapter URL. Defaults to `{url}`.
- `imageSelector`: one element per page image, **in reading order**. `attribute` is where the image URL is. It defaults
  to `data-src,data-lazy-src,src`, which also covers lazy loading.
- `pageSelector`: only for websites that show **one image per HTML page**. It selects the links (or the options of a
  page dropdown) that lead to each page of the chapter, in reading order, and needs `attribute` (it defaults to
  `href,value`). Tranga fetches each page and applies `imageSelector` to it. Leave it out when all images are on the
  chapter page.

### Placeholders

Only these exist, anything else is rejected: `{query}`, `{page}` (search), `{url}` (chapters, images) and the named
groups of `seriesUrlPattern` (chapters).

## Verify

A definition that has not been run against the real website is not finished. Check every step with the real data, and
fix the selectors until the results are right: the right number of items, no empty or wrong values, URLs that open,
chapters in a sensible order, images that are the actual pages (not thumbnails, logos or ads).

- **If you can reach the user's Tranga** (ask for its address and, if it requires authentication, how to
  authenticate; it is the address of the web interface), use its test endpoint. It runs one step against the real
  website and reports what was extracted, with warnings. Nothing is stored.

  ```
  POST <tranga>/api/mangas/customDownloaders/test
  {
    "settings": { …the general fields: name, baseUrl, languages, useFlareSolverr, requestsPerSecond, headers, seriesUrlPattern… },
    "step": "search" | "chapters" | "images",
    "input": "<a search text> | <a series URL> | <a chapter URL>",
    "search": { …the search section… },     // for step "search"
    "chapters": { …the chapters section… }, // for step "chapters"
    "images": { …the images section… }      // for step "images"
  }
  ```

  Only the section of the tested step is needed. The answer has `requestUrl`, `statusCode`, `itemCount`, `rows` (the
  first items) and `warnings` or an `error`. A `400` lists what is wrong with the definition.
- **If you cannot**, evaluate the selectors yourself against the HTML you fetched (any CSS/XPath tool will do) and
  say so in your answer, so that the user knows to use the test buttons in the form.

## Your answer

1. The definition as **one JSON code block**, nothing else inside it (the user pastes it into *Import JSON*).
2. Below it, briefly: which URLs you tested each step with and what came out, anything that is uncertain (a site
   with several languages, pages that only work with FlareSolverr, a chapter list that is paginated, ...), and
   anything that cannot be done with the available fields.

If the website cannot be described with these fields (JavaScript-only, JSON APIs, logins, DRM), say so and explain
why, instead of guessing.

## Examples

Complete, working definitions of two websites. Read them first and copy their style. `weebcentral` loads the search
results and the chapter list from separate HTML fragments (see `seriesUrlPattern` and `{id}`); `mangatown` has one
image per HTML page (`pageSelector`) and reads the chapter number from the link.

- <https://raw.githubusercontent.com/C9Glax/tranga/main/Extensions/Custom/Examples/weebcentral.json>
- <https://raw.githubusercontent.com/C9Glax/tranga/main/Extensions/Custom/Examples/mangatown.json>
