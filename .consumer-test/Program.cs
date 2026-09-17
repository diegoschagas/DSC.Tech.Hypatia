using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddScoped<State>();
services.AddHypatia(typeof(Program).Assembly);
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
await dispatcher.Send(new SetName("Hypatia"));
var name = await dispatcher.Query<string>(new GetName());
var length = await dispatcher.Send<int>(new CountName(name));
if (name != "Hypatia" || length != 7) throw new InvalidOperationException("Consumer validation failed.");
Console.WriteLine("HYPATIA_CONSUMER_OK");

public sealed class State { public string Name { get; set; } = ""; }
public sealed record SetName(string Value) : ICommand;
public sealed record GetName : IQuery<string>;
public sealed record CountName(string Value) : ICommand<int>;
public sealed class SetNameHandler(State state) : ICommandHandler<SetName> { public Task Handle(SetName c,CancellationToken ct=default){state.Name=c.Value;return Task.CompletedTask;} }
public sealed class GetNameHandler(State state) : IQueryHandler<GetName,string> { public Task<string> Handle(GetName q,CancellationToken ct=default)=>Task.FromResult(state.Name); }
public sealed class CountNameHandler : ICommandHandler<CountName,int> { public Task<int> Handle(CountName c,CancellationToken ct=default)=>Task.FromResult(c.Value.Length); }
