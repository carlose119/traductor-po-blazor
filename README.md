# TraductorPO — .po File Translator

A Blazor Server SPA that translates gettext `.po` files in real time. Upload a file, pick source and target languages, watch each entry translate live, then download the result.

Inspired by [carlose119/traductorpo](https://github.com/carlose119/traductorpo) (PHP). Rewritten in .NET 10 with a web UI.

---

## Quick start

```bash
git clone https://github.com/your-user/traductor-po-blazor
cd traductor-po-blazor
dotnet run
```

Open `http://localhost:5050` (or the URL shown in the terminal).

---

## How to use

| Step | Action |
|------|--------|
| **1 — Import** | Click the upload zone and select a `.po` file (max 50 MB). Chips show total entries / pending / already translated. |
| **2 — Configure** | Choose a translation provider and select source and target languages. |
| **3 — Translate** | Click **Start translation**. A live terminal log shows each entry as it processes. Use **Stop** to cancel without losing progress. |
| **4 — Export** | Click **Download translated .po** to save the result. |

---

## Translation providers

### Google Translate *(default)*

Uses the same unofficial API as the original PHP project — no key required.

```
GET https://translate.googleapis.com/translate_a/single?client=gtx&...
```

Supports auto-detect as source language. Best for occasional use; aggressive use may trigger rate limits.

### LibreTranslate

Open-source translation engine. Works with public instances or a self-hosted server.

| Field | Description |
|-------|-------------|
| URL | Instance address (default: `https://translate.argosopentech.com`) |
| API Key | Optional — required on some instances and `libretranslate.com` |

**Test connection** verifies the instance before you start. The **Public instances** menu lists known free endpoints.

Known public instances:

```
https://libretranslate.com            ← official; free API key at libretranslate.com
https://translate.terraprint.co
https://lt.vern.cc
https://translate.flossboxin.org.in
https://translate.fedilab.app
```

---

## Rate limiting

Both providers apply automatic retry with exponential backoff when a `429 Too Many Requests` response is received.

| Attempt | Wait before retry |
|---------|------------------|
| 1st | 5 s |
| 2nd | 12 s |
| 3rd | 25 s |
| 4th | Error — entry marked ✗ |

A `Retry-After` header in the response overrides the default wait time. There is also an 800 ms pause between every request to reduce the chance of hitting rate limits in the first place.

---

## .po file support

The parser handles the gettext format natively — no external library dependency.

| Feature | Supported |
|---------|-----------|
| `msgid` / `msgstr` | ✓ |
| `msgctxt` (context) | ✓ |
| Multi-line strings (`""\n"..."`) | ✓ |
| Comments (`#`, `#.`, `#:`, `#,`) | ✓ preserved in output |
| `msgid_plural` / `msgstr[n]` | — (skipped) |
| Entries already translated | Skipped (only empty `msgstr` are translated) |

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| UI framework | Blazor Server |
| Component library | MudBlazor 9 (dark theme) |
| Translation — Google | `translate.googleapis.com` unofficial API |
| Translation — Libre | LibreTranslate REST API |
| Real-time updates | `InvokeAsync(StateHasChanged)` per entry |
| File download | JS interop (`window.downloadFile`) |

---

## Project structure

```
TraductorPo/
├── Models/
│   ├── PoEntry.cs              # Entry with MsgId, MsgStr, Status
│   └── TranslationLanguage.cs  # Language code + name record
├── Services/
│   ├── PoFileService.cs        # .po parser and generator
│   ├── LibreTranslateService.cs
│   └── GoogleTranslateService.cs
├── Pages/
│   └── Index.razor             # Main page — full workflow
├── Shared/
│   └── MainLayout.razor        # MudBlazor providers + dark theme
└── wwwroot/css/site.css        # Custom styles (gradient, terminal log)
```

---

## Development

```bash
# Run with hot reload
dotnet watch

# Build only
dotnet build
```

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download)
