# Changelog

## 3.0.0 (2023-01-01)

__Additions and improvements__

- New namespace hierarchy.
- Added [DNSCache](https://bestdocshub.pages.dev/Shared/DNS/dns-cache.md) implementation to speed up consecutive connection processes.
- Added support for [DNSCache](https://bestdocshub.pages.dev/HTTP/api-reference/Cache/DNSCache.md) to manually store and retrieve entries.
- Added new [Negotiator](https://bestdocshub.pages.dev/HTTP/api-reference/Tcp/Negotiator.md) class help building new plugins that doesn't use HTTP, but require the same lower-level infrastructure.
- Added new [TCPRingmaster](https://bestdocshub.pages.dev/HTTP/api-reference/Tcp/TCPRingmaster.md) class to speed up TCP connection process by sending out multiple tcp connection requests and use the first connected one.
- Reimplemented connection logic to use the new DNS cache, negotiator and tcp-ringmaster.
- Reimplemented network read and write operations. Instead of blocking Reads&Writes, now the plugin uses non-blocking functions. This enabled implementing new ways for downloads and uploads. There's forward and backward feedback between the low level tcp layer and higher level connection layers. If the [download-stream](https://bestdocshub.pages.dev/HTTP/api-reference/Response/DownloadContentStream.md)'s buffer is full, it can notify the tcp layer that it can resume receiving from the server.
- Reimplemented [HTTPCache](https://bestdocshub.pages.dev/HTTP/api-reference/Caching/HTTPCache.md), it's got more robust and future proof.
- (#69) Now it's possible and easy to populate the local HTTP cache.
- Added new, cleaner samples for both old and new features.
- Halved active threads per connections.
- Added [Memory](https://bestdocshub.pages.dev/Shared/Profiler/memory.md) and [Network](https://bestdocshub.pages.dev/Shared/Profiler/memory.md) profilers.

__Removals__

- Removed old, cluttered samples.

__Fixes__

- Fixed chaos around different upload sources (RawData, Forms, UploadStream) and unified them in one [UploadStream](https://bestdocshub.pages.dev/HTTP/getting-started/uploads.md).

For API changes and upgrade guides see the [Upgrade Guide topic](https://bestdocshub.pages.dev/HTTP/upgrade-guide.md).