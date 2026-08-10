namespace Common.Services.Events;

/// <summary>Base type for all messages published on the shared RabbitMQ "tranga" exchange via <see cref="EventPublisher"/>.</summary>
public abstract record TrangaEvent;