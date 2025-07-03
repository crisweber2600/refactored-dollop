namespace ExampleLib.Messages;

/// <summary>
/// Message published when committing a save fails.
/// </summary>
public record SaveCommitFault<T>(
    string AppName,
    string EntityType,
    string EntityId,
    T? Payload,
    string ErrorMessage);
