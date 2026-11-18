using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DSC.Tech.Hypatia.Tests.Substitution;

/// <summary>
/// Testes de substituição (Liskov): qualquer implementação que respeite o contrato pode ocupar o lugar
/// de outra sem que o código chamador mude, e o dispatcher não altera o comportamento observável do handler.
/// </summary>
public sealed class SubstitutionTests
{
    [Fact]
    public async Task InterchangeableHandlers_HonoringContract_AreTransparentToCaller()
    {
        using var first = Provider(s => s.AddScoped<IQueryHandler<Square, int>, MultiplyingSquareHandler>());
        using var second = Provider(s => s.AddScoped<IQueryHandler<Square, int>, AddingSquareHandler>());

        var fromFirst = await new MessageDispatcher(first).Query<int>(new Square(7));
        var fromSecond = await new MessageDispatcher(second).Query<int>(new Square(7));

        Assert.Equal(49, fromFirst);
        Assert.Equal(fromFirst, fromSecond);
    }

    [Fact]
    public async Task ApplicationCode_BehavesTheSame_WithRealDispatcherAndTestDouble()
    {
        using var provider = Provider(s => s.AddScoped<IQueryHandler<Square, int>, MultiplyingSquareHandler>());
        var withReal = new AreaService(new MessageDispatcher(provider));
        var withDouble = new AreaService(new FakeDispatcher());

        Assert.Equal(await withReal.AreaOf(5), await withDouble.AreaOf(5));
    }

    [Fact]
    public async Task ReplacedHandlerRegistration_ChangesBehavior_WithoutChangingDispatcher()
    {
        using var provider = Provider(s =>
        {
            s.AddScoped<IQueryHandler<Square, int>, MultiplyingSquareHandler>();
            s.Replace(ServiceDescriptor.Scoped<IQueryHandler<Square, int>, FixedSquareHandler>());
        });

        var result = await new MessageDispatcher(provider).Query<int>(new Square(3));

        Assert.Equal(FixedSquareHandler.Value, result);
    }

    [Fact]
    public async Task Cancellation_ReachesCallerAsOperationCanceled_WithoutReflectionWrapper()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var provider = Provider(s => s.AddScoped<ICommandHandler<Work>, CancellationAwareHandler>());

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new MessageDispatcher(provider).Send(new Work(), cts.Token));

        Assert.Equal(cts.Token, ex.CancellationToken);
    }

    [Fact]
    public async Task SubstituteExceptions_KeepTheirOriginalType()
    {
        using var provider = Provider(s => s.AddScoped<ICommandHandler<Work>, FailingHandler>());

        var ex = await Assert.ThrowsAsync<DomainRuleException>(
            () => new MessageDispatcher(provider).Send(new Work()));

        Assert.Equal(FailingHandler.Message, ex.Message);
    }

    [Fact]
    public async Task HandlerBreakingContract_ByReturningNullTask_FailsExplicitly()
    {
        using var provider = Provider(s => s.AddScoped<ICommandHandler<Work>, NullTaskHandler>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new MessageDispatcher(provider).Send(new Work()));

        Assert.Contains(nameof(NullTaskHandler), ex.Message);
    }

    [Fact]
    public async Task DerivedMessage_IsRoutedByConcreteType_NotToBaseHandler()
    {
        // Limitação documentada: o handler da classe base não atende mensagens derivadas.
        using var provider = Provider(s => s.AddScoped<ICommandHandler<BaseNotice>, BaseNoticeHandler>());

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => new MessageDispatcher(provider).Send<BaseNotice>(new DerivedNotice()));

        Assert.Equal(typeof(DerivedNotice), ex.MessageType);
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private sealed record Square(int Side) : IQuery<int>;
    private sealed record Work : ICommand;
    private record BaseNotice : ICommand;
    private sealed record DerivedNotice : BaseNotice;

    private sealed class DomainRuleException(string message) : Exception(message);

    private sealed class AreaService(IMessageDispatcher dispatcher)
    {
        public Task<int> AreaOf(int side) => dispatcher.Query<int>(new Square(side));
    }

    private sealed class FakeDispatcher : IMessageDispatcher
    {
        public Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand => Task.CompletedTask;

        public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResult)!);

        public Task<TResult> Query<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default) =>
            query is Square square
                ? Task.FromResult((TResult)(object)(square.Side * square.Side))
                : Task.FromResult(default(TResult)!);
    }

    private sealed class MultiplyingSquareHandler : IQueryHandler<Square, int>
    {
        public Task<int> Handle(Square query, CancellationToken cancellationToken = default) =>
            Task.FromResult(query.Side * query.Side);
    }

    private sealed class AddingSquareHandler : IQueryHandler<Square, int>
    {
        public Task<int> Handle(Square query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Repeat(query.Side, query.Side).Sum());
    }

    private sealed class FixedSquareHandler : IQueryHandler<Square, int>
    {
        public const int Value = -1;

        public Task<int> Handle(Square query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Value);
    }

    private sealed class CancellationAwareHandler : ICommandHandler<Work>
    {
        public Task Handle(Work command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : ICommandHandler<Work>
    {
        public const string Message = "Regra de domínio violada.";

        public Task Handle(Work command, CancellationToken cancellationToken = default) =>
            throw new DomainRuleException(Message);
    }

    private sealed class NullTaskHandler : ICommandHandler<Work>
    {
        public Task Handle(Work command, CancellationToken cancellationToken = default) => null!;
    }

    private sealed class BaseNoticeHandler : ICommandHandler<BaseNotice>
    {
        public Task Handle(BaseNotice command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
