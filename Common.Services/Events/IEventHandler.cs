namespace Common.Services.Events;

/// <summary>Marker interface for a service's registered RabbitMQ event handlers, letting them be tracked/disposed uniformly regardless of the concrete <see cref="TrangaEvent"/> type they handle.</summary>
public interface IEventHandler;