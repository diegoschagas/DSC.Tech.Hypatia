using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DSC.Tech.Hypatia.Tests.Integration;

public sealed class MessagingIntegrationTests
{
    [Fact]
    public async Task CommandThenQuery_EndToEnd_UsesSameScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<State>();
        services.AddHypatia(typeof(MessagingIntegrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        await dispatcher.Send(new SetValue("Hypatia"));
        var result = await dispatcher.Query<string>(new GetValue());

        Assert.Equal("Hypatia", result);
    }

    public sealed record SetValue(string Value) : ICommand;
    public sealed record GetValue : IQuery<string>;

    public sealed class State
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class SetValueHandler(State state) : ICommandHandler<SetValue>
    {
        public Task Handle(SetValue command, CancellationToken cancellationToken = default)
        {
            state.Value = command.Value;
            return Task.CompletedTask;
        }
    }

    public sealed class GetValueHandler(State state) : IQueryHandler<GetValue, string>
    {
        public Task<string> Handle(GetValue query, CancellationToken cancellationToken = default) =>
            Task.FromResult(state.Value);
    }
}
