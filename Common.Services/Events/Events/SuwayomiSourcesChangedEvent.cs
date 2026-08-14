namespace Common.Services.Events.Events;

/// <summary>
/// Published when the set of extensions installed on the Suwayomi sidecar has changed, so every service refreshes the
/// download extensions it has registered. Each service process holds its own extension collection, so without this the
/// task service would keep using a stale source list until its periodic refresh.
/// </summary>
public record SuwayomiSourcesChangedEvent : TrangaEvent;
