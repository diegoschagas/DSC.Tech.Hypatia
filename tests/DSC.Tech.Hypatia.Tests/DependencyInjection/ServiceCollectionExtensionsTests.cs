using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DSC.Tech.Hypatia.Tests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHypatia_RegistersDispatcherAndAllHandlerShapes()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDependency, Dependency>();
        services.AddHypatia(typeof(ServiceCollectionExtensionsTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IMessageDispatcher>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICommandHandler<RegisteredCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICommandHandler<RegisteredResultCommand, int>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IQueryHandler<RegisteredQuery, string>>());
    }

    [Fact]
    public async Task HandlerDependencies_AreResolvedByContainer()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDependency, Dependency>();
        services.AddHypatia(typeof(ServiceCollectionExtensionsTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        var result = await dispatcher.Query<string>(new RegisteredQuery());

        Assert.Equal("dependency-ok", result);
    }

    [Fact]
    public void DispatcherAndHandlers_RespectScopedLifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDependency, Dependency>();
        services.AddHypatia(typeof(ServiceCollectionExtensionsTests).Assembly);
        using var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var dispatcher1a = scope1.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        var dispatcher1b = scope1.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        var handler1a = scope1.ServiceProvider.GetRequiredService<IQueryHandler<RegisteredQuery, string>>();
        var handler1b = scope1.ServiceProvider.GetRequiredService<IQueryHandler<RegisteredQuery, string>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<RegisteredQuery, string>>();

        Assert.Same(dispatcher1a, dispatcher1b);
        Assert.NotSame(dispatcher1a, dispatcher2);
        Assert.Same(handler1a, handler1b);
        Assert.NotSame(handler1a, handler2);
    }

    [Fact]
    public void RepeatedRegistration_IsIdempotentForSameAssembly()
    {
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        services.AddHypatia(assembly);
        services.AddHypatia(assembly);

        Assert.Single(services, d => d.ServiceType == typeof(IMessageDispatcher));
        Assert.Single(services, d =>
            d.ServiceType == typeof(IQueryHandler<RegisteredQuery, string>) &&
            d.ImplementationType == typeof(RegisteredQueryHandler));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        IServiceCollection? nullServices = null;

        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddHypatia(nullServices!, typeof(ServiceCollectionExtensionsTests).Assembly));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddHypatia((System.Reflection.Assembly[])null!));

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHypatia([null!]));
    }

    public sealed record RegisteredCommand : ICommand;
    public sealed record RegisteredResultCommand : ICommand<int>;
    public sealed record RegisteredQuery : IQuery<string>;

    public interface IDependency { string Value { get; } }
    public sealed class Dependency : IDependency { public string Value => "dependency-ok"; }

    public sealed class RegisteredCommandHandler : ICommandHandler<RegisteredCommand>
    {
        public Task Handle(RegisteredCommand command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    public sealed class RegisteredResultCommandHandler : ICommandHandler<RegisteredResultCommand, int>
    {
        public Task<int> Handle(RegisteredResultCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);
    }

    public sealed class RegisteredQueryHandler(IDependency dependency) : IQueryHandler<RegisteredQuery, string>
    {
        public Task<string> Handle(RegisteredQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(dependency.Value);
    }
}

