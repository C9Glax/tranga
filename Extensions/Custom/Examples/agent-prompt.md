# Prompt: create a Tranga custom source with an AI agent

Give this whole file to an AI agent that can fetch web pages (for example with `curl`). It asks you which website you
want, and answers with a JSON definition that you paste into **Settings → Sources → Add custom source → Import JSON**
and check with the test buttons before saving.

Everything below the line is the prompt.

---

You are going to create a **Tranga custom download source**: one JSON document that tells Tranga how to use a manga
website.

**Start by asking the user which website they want a source for.** Ask for its address, and for the title of a series
that exists there (it makes your tests easier). Do not do anything else before you have the answer. Then follow this
document.

Tranga is a manga downloader. A custom source tells it how to use a website without any code: Tranga requests a
page and picks the data it needs out of the answer. Three steps are described, each with its own request: **search**
(find series by a title), **chapters** (list the chapters of a series) and **images** (find the page images of a
chapter).

A source reads one of two formats, for all of its steps:

- **`html`**: web pages, read with CSS selectors or XPath expressions. What most websites need.
- **`json`**: the API of a website, read with paths (`data`, `manga.title`, `pages[0]`). For websites whose pages are
  empty until JavaScript has loaded the content.

## Ground rules

- Tranga only sees what the server sends. It does **not** run JavaScript. Work from the raw response (`curl`), never
  from what a browser shows after scripts ran.
- Tranga requests the pages with a normal browser `User-Agent`. Do the same:
  `curl -sL -A 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36' <url>`.
- Be polite: few requests, no hammering. This is for a website the user is allowed to read and download from.
- Never invent selectors or paths. Every one in the answer has to come from a response you actually fetched, and
  every step has to be checked (see "Verify").
- Only public websites work: addresses on a private network (localhost, 192.168.x.x, ...) are refused by default.

## How to work

1. **Choose the format.** Fetch a search result page or a series page of the website with `curl`.
   - The titles, links and chapters are in the HTML: use `html`.
   - The HTML is an empty shell (a few kilobytes, a `<div id="app">`, a script): the content comes from an API. Find
     it: look at the JavaScript of the page for `/api/` addresses, try the obvious ones (`/api/search?q=…`), or ask
     the user to open the browser's developer tools on the Network tab (filter Fetch/XHR), search and open a chapter,
     and tell you the requests. If the API answers with JSON, use `json`. An API that answers with HTML fragments is
     an `html` source that uses those addresses.
2. **Search.** Find the address that answers with the results for a title you know exists. Find what is repeated once
   per result (the *item*), and in it the title, the link to the series and the cover image.
3. **Series.** Fetch the chapters of a series from the results (the series page, or the address they are loaded from,
   see `seriesUrlPattern` below). Find what is repeated once per chapter, and in it the link, the number, and if
   there are any the title and date. Check whether the list is paginated.
4. **Chapter.** Fetch a chapter. Find the page images, in reading order. Check whether all of them are in this one
   answer, or whether every page of the chapter has to be fetched on its own (then use `pageSelector`).
5. **Write the definition**, then **verify** it, then **answer** (see below).

## The definition

All names are camelCase. Every field that is not needed can be left out. This skeleton contains every field there is,
with example values for an `html` source:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "My Manga Site",
  "baseUrl": "https://example.com",
  "iconUrl": "https://example.com/favicon.ico",
  "format": "html",
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
    "urlPattern": "/chapter/(?<chapter>\\d+)",
    "urlTemplate": "/chapter/{chapter}/images",
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
| `format` | `html` (the default) or `json`. It applies to every step and decides what a selector is: a CSS selector / XPath expression, or a JSON path. |
| `languages` | Languages the website publishes in, as codes like `en`, `en-us` or `pt-BR`. At least one. |
| `useFlareSolverr` | `true` only if the website sits behind a Cloudflare challenge (a plain request is answered with a "Just a moment" page or HTTP 403/503). Needs a FlareSolverr instance on the Tranga installation. |
| `requestsPerSecond` | Requests per second to the website, 0.1 to 20. Be conservative (0.5 to 2). |
| `headers` | Extra request headers (a cookie, a `User-Agent`, ...). `Host`, `Content-Length`, `Transfer-Encoding` and `Connection` cannot be set. A `json` source asks for JSON (`Accept`) by itself. |
| `seriesUrlPattern` | Optional regular expression a series URL has to match. Its **named groups** (`(?<id>…)`) can be used as `{id}` in `chapters.urlTemplate`. Needed when the chapter list is at a different address than the series page, which is always the case for an API. |

### Selector fields

Every value to read (`title`, `url`, `cover`, `number`, `date`, `nextPage`, `pageSelector`) is an object with up to four parts:

| Part | Meaning |
|---|---|
| `selector` | Where the value is, **relative to the item** (what is repeated per result or chapter). `html`: a CSS selector, or XPath when it starts with `/`, `./`, `../` or `(`; empty means the item itself, which is how you read the link of an item that is an `<a>`; XPath may end in `/@attribute` or `/text()`. `json`: a path, see below. |
| `attribute` | `html` only. What to read: empty reads the element's text, `href`, `src`, `data-src`, … read an attribute. A comma separated list (`srcset,src`) is tried in order, the first non-empty value wins. Leave it out for `json`. |
| `regex` | Applied to the value. Without `replacement` the first capture group (or the whole match) is kept, and a value that does not match counts as missing. |
| `replacement` | When set, `regex` replaces every match with this text instead of extracting. `$1` is the first group. The placeholders of the step's URL can be used in it (see "Placeholders"), which is how a link is built from an id: regex `^(.+)$`, replacement `/series/{slug}/chapter/$1`. |

Relative URLs are resolved against the address they were found at. Only `http(s)` URLs are kept.

**`html` selectors.** CSS is handled by AngleSharp, so `:not()`, `:has()`, `:nth-child()`, `:contains()` and attribute
selectors work.

**`json` paths.** A small subset of JSONPath:

| Path | Selects |
|---|---|
| `data`, `manga.title` | Properties, separated by dots. A leading `$.` is allowed. |
| `pages[0]`, `pages[-1]`, `pages[*]` | One element of a list (negative counts from the end), or all of them. |
| `$` | The item itself (for a list of plain values). |
| `english_title, title` | Alternatives: the first one that has a value is used. |
| `['odd.name']` | A property whose name contains dots, spaces or other special characters. |

A list at the end of a path stands for its elements, so `data` and `data[*]` select the same items. Numbers and
booleans are read as they are written. `null`, empty strings, objects and lists count as missing. Filters
(`[?(@.x==1)]`), slices and `..` are **not** supported: if you need one, say so in your answer.

### Search

- `urlTemplate`: `{query}` is the URL-encoded search text, `{page}` is always `1`. Relative to `baseUrl`.
- `itemSelector`: one item per result. `html`: the wrapper around the whole result (it may also be the link itself,
  then leave the `selector` of `url` empty); make it match results only, not other links on the page. `json`: the path
  to the list of results.
- `title`, `url` and `cover` are all required and are looked up **inside** the item. `url` has to become the address
  of the series **page** (what a person would open), also for `json`: build it with `regex` and `replacement` when the
  API only has an id or slug.

### Chapters

- `urlTemplate`: the address that lists the chapters. `{url}` is the series URL, the named groups of
  `seriesUrlPattern` (for example `{id}`) are available too. Defaults to `{url}`.
- `itemSelector`: one item per chapter. `url` is required: the address of the chapter page.
- `number`: the chapter number. The **first number** in the extracted text is used (`Chapter 12.5` becomes `12.5`);
  chapters without a number are skipped. `html`: when unused, the item's text is searched; if the number is only in
  the link (`…/c012.5/`), read the `href` and use a `regex` like `/c(\d+(?:\.\d+)?)/?$`. `json`: required; an API
  often pads numbers (`12.5000`), cut that off with the `regex` `^(\d+(?:\.\d*[1-9])?)`.
- `title`, `date` and `dateFormat` are optional. `dateFormat` is a .NET format such as `yyyy-MM-dd` or `MMM dd,yyyy`;
  ISO 8601 and common formats work without it.
- `nextPage`: optional link to the next page of the chapter list (followed up to 50 times).

### Images

- `urlTemplate`: the address that has the images. `{url}` is the chapter URL, the named groups of `urlPattern` are
  available too. Defaults to `{url}`.
- `urlPattern`: optional regular expression the chapter URL is matched against. Its named groups can be used in
  `urlTemplate`. Needed when the images are loaded from an address that only contains a part of the chapter URL
  (`/api/chapter/{id}`).
- `imageSelector`: one item per page image, **in reading order**. `html`: the elements, and `attribute` is where the
  image URL is (it defaults to `data-src,data-lazy-src,src`, which also covers lazy loading). `json`: the path to the
  image URLs (`pages`, `pages[*].url`).
- `pageSelector`: only for websites that show **one image per page**. It selects the links to each page of the
  chapter, in reading order (`html`: links or the options of a page dropdown, `attribute` defaults to `href,value`).
  Tranga fetches each of them and applies `imageSelector` to it. Leave it out when all images are in one answer.

### Placeholders

Only these exist in the URL templates, anything else is rejected: `{query}`, `{page}` (search), `{url}` and the named
groups of `seriesUrlPattern` (chapters), `{url}` and the named groups of `images.urlPattern` (images). The same ones
can be used in the `replacement` of the fields of that step.

## Verify

A definition that has not been run against the real website is not finished. Check every step with the real data, and
fix it until the results are right: the right number of items, no empty or wrong values, URLs that open, chapters in
a sensible order, images that are the actual pages (not thumbnails, logos or ads).

- **If you can reach the user's Tranga** (ask for its address and, if it requires authentication, how to
  authenticate; it is the address of the web interface), use its test endpoint. It runs one step against the real
  website and reports what was extracted, with warnings. Nothing is stored.

  ```
  POST <tranga>/api/mangas/customDownloaders/test
  {
    "settings": { …the general fields: name, baseUrl, format, languages, useFlareSolverr, requestsPerSecond, headers, seriesUrlPattern… },
    "step": "search" | "chapters" | "images",
    "input": "<a search text> | <a series URL> | <a chapter URL>",
    "search": { …the search section… },     // for step "search"
    "chapters": { …the chapters section… }, // for step "chapters"
    "images": { …the images section… }      // for step "images"
  }
  ```

  Only the section of the tested step is needed. The answer has `requestUrl`, `statusCode`, `itemCount`, `rows` (the
  first items) and `warnings` or an `error`. A `400` lists what is wrong with the definition.
- **If you cannot**, evaluate the selectors or paths yourself against the responses you fetched (any CSS/XPath tool,
  `jq`) and say so in your answer, so that the user knows to use the test buttons in the form.

## Your answer

1. The definition as **one JSON code block**, nothing else inside it (the user pastes it into *Import JSON*).
2. Below it, briefly: which URLs you tested each step with and what came out, anything that is uncertain (a site
   with several languages, pages that only work with FlareSolverr, a chapter list that is paginated, ...), and
   anything that cannot be done with the available fields.

If the website cannot be described with these fields (logins, DRM, an API that needs a signed token or a `POST`,
image addresses that have to be computed), say so and explain why, instead of guessing.

## Examples

Complete, working definitions of three websites. Read the ones of your format first and copy their style.

- `html`: <https://raw.githubusercontent.com/C9Glax/tranga/main/Extensions/Custom/Examples/weebcentral.json>
  loads the search results and the chapter list from separate HTML fragments (see `seriesUrlPattern` and `{id}`).
- `html`: <https://raw.githubusercontent.com/C9Glax/tranga/main/Extensions/Custom/Examples/mangatown.json>
  has one image per HTML page (`pageSelector`) and reads the chapter number from the link.
- `json`: <https://raw.githubusercontent.com/C9Glax/tranga/main/Extensions/Custom/Examples/mangauno.json>
  is a JavaScript application on top of an API: links are built from a slug and an id with `replacement`, and
  `images.urlPattern` leads from the chapter page back to the API.
