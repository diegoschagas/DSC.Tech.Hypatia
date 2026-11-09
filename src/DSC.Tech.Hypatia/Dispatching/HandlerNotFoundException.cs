namespace DSC.Tech.Hypatia.Dispatching;

/// <summary>Thrown when no handler is registered for a dispatched message.</summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    public HandlerNotFoundException(Type messageType, Type handlerType)
        : base($"No handler registered for message '{messageType.FullName}'. Expected service '{handlerType.FullName}'.")
    {
        MessageType = messageType;
        HandlerType = handlerType;
    }

    public Type MessageType { get; }
    public Type HandlerType { get; }
}
