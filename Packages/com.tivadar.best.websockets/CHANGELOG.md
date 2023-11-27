# Changelog

## 3.0.0 (2023-01-01)

__Additions and improvements__

- New namespace hierarchy, instead of `BestHTTP.WebSocket` the WebSocket class is in the `Best.Websockets` namespace.
- The `OnBinary` event now receives a `BufferSegment` and the memory will be reused after the event.
- The `OnClosed` event now receives an [WebSocketStatusCodes](https://bestdocshub.pages.dev/WebSockets/api-reference/WebSockets/WebSocketStatusCodes.md) enum as its status code.

__Removals__

- Removed the `OnBinaryNoAlloc` event.

__Fixes__

- Fixed confusing naming by renaming `StartPingThread` to `SendPings`
- Fixed confusing behavior because of the two closure events `OnError` and `OnClosed` by mergind the two into one `OnClosed` event. The behavior of `OnClosed` is now matching what browsers have.