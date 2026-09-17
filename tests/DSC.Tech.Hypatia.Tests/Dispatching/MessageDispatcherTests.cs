using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace DSC.Tech.Hypatia.Tests.Dispatching;

public sealed class MessageDispatcherTests
{
    [Fact]
    public async Task CommandWithoutResult_DispatchesOnceAndPropagatesToken()
    {
        var token = new CancellationTokenSource().Token;
        var handler = new PingHandler();
        using var provider = Provider(s => s.AddSingleton<ICommandHandler<Ping>>(handler));
        var dispatcher = new MessageDispatcher(provider);

        await dispatcher.Send(new Ping(), token);

        Assert.Equal(1, handler.Calls);
        Assert.Equal(token, handler.Token);
    }

    [Fact]
    public async Task CommandWithResult_ReturnsResultAndToken()
    {
        var token = new CancellationTokenSource().Token;
        var handler = new NumberHandler();
        using var provider = Provider(s => s.AddSingleton<ICommandHandler<NumberCommand, int>>(handler));
        var dispatcher = new MessageDispatcher(provider);

        var result = await dispatcher.Send<int>(new NumberCommand(41), token);

        Assert.Equal(42, result);
        Assert.Equal(token, handler.Token);
    }

    [Fact]
    public async Task Query_ReturnsResultAndToken()
    {
        var token = new CancellationTokenSource().Token;
        var handler = new TextHandler();
        using var provider = Provider(s => s.AddSingleton<IQueryHandler<TextQuery, string>>(handler));
        var dispatcher = new MessageDispatcher(provider);

        var result = await dispatcher.Query<string>(new TextQuery("Hypatia"), token);

        Assert.Equal("Hypatia!", result);
        Assert.Equal(token, handler.Token);
    }

    [Fact]
    public async Task MissingCommandHandler_ThrowsTypedException()
    {
        using var provider = Provider();
        var dispatcher = new MessageDispatcher(provider);

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() => dispatcher.Send(new Ping()));

        Assert.Equal(typeof(Ping), ex.MessageType);
        Assert.Equal(typeof(ICommandHandler<Ping>), ex.HandlerType);
        Assert.Contains("No handler registered", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingResultCommandHandler_ThrowsTypedException()
    {
        using var provider = Provider();
        var dispatcher = new MessageDispatcher(provider);

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() => dispatcher.Send<int>(new NumberCommand(1)));

        Assert.Equal(typeof(NumberCommand), ex.MessageType);
        Assert.Equal(typeof(ICommandHandler<NumberCommand, int>), ex.HandlerType);
    }

    [Fact]
    public async Task MissingQueryHandler_ThrowsTypedException()
    {
        using var provider = Provider();
        var dispatcher = new MessageDispatcher(provider);

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() => dispatcher.Query<string>(new TextQuery("x")));

        Assert.Equal(typeof(TextQuery), ex.MessageType);
        Assert.Equal(typeof(IQueryHandler<TextQuery, string>), ex.HandlerType);
    }

    [Fact]
    public async Task MultipleHandlers_ThrowsInsteadOfSilentlyChoosingOne()
    {
        using var provider = Provider(s =>
        {
            s.AddSingleton<ICommandHandler<Ping>, PingHandler>();
            s.AddSingleton<ICommandHandler<Ping>, SecondPingHandler>();
        });
        var dispatcher = new MessageDispatcher(provider);

        var ex = await Assert.ThrowsAsync<MultipleHandlersException>(() => dispatcher.Send(new Ping()));

        Assert.Equal(typeof(Ping), ex.MessageType);
        Assert.Equal(typeof(ICommandHandler<Ping>), ex.HandlerType);
        Assert.Equal(2, ex.Count);
        Assert.Contains("Multiple handlers (2)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandlerException_IsNotWrappedByReflection()
    {
        using var provider = Provider(s => s.AddSingleton<ICommandHandler<Explode>, ExplodeHandler>());
        var dispatcher = new MessageDispatcher(provider);

        var ex = await Assert.ThrowsAsync<DomainLikeException>(() => dispatcher.Send(new Explode()));

        Assert.Equal("business failure", ex.Message);
    }

    [Fact]
    public async Task DifferentMessageTypes_AreIsolated()
    {
        var first = new FirstHandler();
        var second = new SecondHandler();
        using var provider = Provider(s =>
        {
            s.AddSingleton<ICommandHandler<First>>(first);
            s.AddSingleton<ICommandHandler<Second>>(second);
        });
        var dispatcher = new MessageDispatcher(provider);

        await dispatcher.Send(new First());

        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task ConcurrentDispatches_AreIndependent()
    {
        var handler = new ConcurrentHandler();
        using var provider = Provider(s => s.AddSingleton<ICommandHandler<ConcurrentPing>>(handler));
        var dispatcher = new MessageDispatcher(provider);

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => dispatcher.Send(new ConcurrentPing())));

        Assert.Equal(100, handler.Calls);
    }

    [Fact]
    public async Task NullMessages_AreRejected()
    {
        using var provider = Provider();
        var dispatcher = new MessageDispatcher(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Send<Ping>((Ping)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Send<int>((ICommand<int>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Query<string>(null!));
    }

    private static ServiceProvider Provider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private sealed record Ping : ICommand;
    private sealed record NumberCommand(int Value) : ICommand<int>;
    private sealed record TextQuery(string Value) : IQuery<string>;
    private sealed record Explode : ICommand;
    private sealed record First : ICommand;
    private sealed record Second : ICommand;
    private sealed record ConcurrentPing : ICommand;

    private sealed class PingHandler : ICommandHandler<Ping>
    {
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task Handle(Ping command, CancellationToken cancellationToken = default)
        {
            Calls++;
            Token = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class SecondPingHandler : ICommandHandler<Ping>
    {
        public Task Handle(Ping command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NumberHandler : ICommandHandler<NumberCommand, int>
    {
        public CancellationToken Token { get; private set; }
        public Task<int> Handle(NumberCommand command, CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return Task.FromResult(command.Value + 1);
        }
    }

    private sealed class TextHandler : IQueryHandler<TextQuery, string>
    {
        public CancellationToken Token { get; private set; }
        public Task<string> Handle(TextQuery query, CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return Task.FromResult(query.Value + "!");
        }
    }

    private sealed class ExplodeHandler : ICommandHandler<Explode>
    {
        public Task Handle(Explode command, CancellationToken cancellationToken = default) =>
            throw new DomainLikeException("business failure");
    }

    private sealed class FirstHandler : ICommandHandler<First>
    {
        public int Calls { get; private set; }
        public Task Handle(First command, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler : ICommandHandler<Second>
    {
        public int Calls { get; private set; }
        public Task Handle(Second command, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ConcurrentHandler : ICommandHandler<ConcurrentPing>
    {
        private int _calls;
        public int Calls => _calls;
        public Task Handle(ConcurrentPing command, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }
    }

    private sealed class DomainLikeException(string message) : Exception(message);
}

