namespace DSC.Tech.Hypatia.Dispatching;

public sealed class MultipleHandlersException : InvalidOperationException
{
    public MultipleHandlersException(Type messageType, Type handlerType, int count)
        : base($"Multiple handlers ({count}) registered for message '{messageType.FullName}'. Expected exactly one service '{handlerType.FullName}'.")
    {
        MessageType = messageType;
        HandlerType = handlerType;
        Count = count;
    }

    public Type MessageType { get; }
    public Type HandlerType { get; }
    public int Count { get; }
}
