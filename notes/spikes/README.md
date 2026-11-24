# Verification spikes (2026-11-24, prompt 018)

Throwaway code that settles the "to verify" items in
[ADR 0001](../../docs/adr/0001-tech-stack.md) with evidence instead of model
memory. Not part of the product.

## 1. RSS parsing: `rss-syndication/`

**Run:** `dotnet run -- ../../samples`. Needs the local, git-ignored BBC
captures.

| Check | Result |
|---|---|
| Package | `System.ServiceModel.Syndication` 10.0.15, MIT, ships `net10.0` (NuGet metadata) |
| All 5 BBC feeds parse | ✅ items 33/24/58/42/21: **identical** to the independent Python count (phase 1) |
| Item order preserved (needed for rank) | ✅ first items match the Python analysis |
| `pubDate` "… GMT" → offset +00:00 | ✅ |
| Numeric offset (+0100) | ✅ offset kept. **Must store `.UtcDateTime`** |
| `guid` / link / summary / `media:thumbnail` accessible | ✅ thumbnail visible as an extension, which we deliberately ignore (D-013) |
| **Missing `pubDate`** | ⚠️ silently becomes `0001-01-01`. Treat MinValue as "unknown" → not eligible |
| **Unparseable `pubDate`** | ❌ default parser **throws for the whole feed**. One bad item would block the topic |
| Fix: `Rss20FeedFormatter.DateTimeParser` (lenient) | ✅ bad date → MinValue, feed still loads |
| Article URLs | ⚠️ span `/news/articles/`, `/news/videos/`, `/weather/articles/`. ID = last path segment |

**AI errors caught:** the first `DateTimeParser` lambda used a 4-argument
signature from memory → build error CS1593. The real signature,
`bool (XmlDateTimeData, out DateTimeOffset)`, was read from the package's XML
docs.

## 2. Email: `email-mailkit/` + smtp4dev

| Check | Result |
|---|---|
| Microsoft guidance on `System.Net.Mail.SmtpClient` | ✅ docs (learn.microsoft.com, updated 2026-10-16): not recommended for new development; names MailKit as the alternative. **Not** marked `[Obsolete]` |
| MailKit licence / TFM | MailKit 5.0.0, MIT, ships `net10.0` |
| smtp4dev runtime (open risk in ADR 0001) | ✅ `rnwood.smtp4dev` 3.15.0, BSD-3, tool targets **net10.0 win-x64**. Installs with `dotnet tool install` and runs on the installed runtime. Mailpit fallback not needed |
| smtp4dev options | `--urls`, `--smtpport`, `--db=""` (in-memory), `--imapport=""`/`--pop3port=""` to disable |
| MailKit → smtp4dev end to end | ✅ message received; checked via smtp4dev's HTTP API |
| Header injection via article title (`\r\nBcc: …`) | ✅ blocked: MimeKit removed the CR/LF and folded the text into the Subject. No Bcc header, recipients unchanged. Subject is ugly, so **we strip CR/LF from titles ourselves** |

**AI errors caught:**
1. `SendAsync(MimeMessage)` is the **MailKit 4.x** API, from memory → build
   error CS1503. MailKit 5.0 takes `ISendRequest` / `new SendRequest(msg)`,
   verified from its XML docs.
2. The first injection check read smtp4dev's `/source` endpoint, which
   returns the **body** only, and reported "no Bcc header". That conclusion
   was **invalid**, because it inspected the wrong data. Redone against
   `/raw`, the full source, before accepting the result.

## 3. Test libraries (NuGet metadata, read live)

| Package | Latest stable | Licence |
|---|---|---|
| xunit.v3 | 4.0.3 | Apache-2.0 |
| xunit (v2) | 2.9.3 | Apache-2.0 |
| Shouldly | 4.3.0 | BSD-3-Clause |
| AwesomeAssertions (community fork of FluentAssertions 7) | 9.6.0 | Apache-2.0 |
| FluentAssertions 7.2.0 | | Apache-2.0 |
| **FluentAssertions 8.x** (8.11.0) | | **Xceed Community License, non-commercial use only; commercial use needs a paid licence** (read from the LICENSE file inside the package) |

The memory claim about FluentAssertions was **correct**, and is now backed by
evidence.
