using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Mediation;

namespace PerocheNow.Business.Tests.Mediation;

public sealed class MediatorTests
{
    public sealed record Greet(string Name) : IQuery<string>;

    public sealed record OrphanRequest : IQuery<int>;

    public sealed class GreetHandler(StepLog log) : IHandler<Greet, string>
    {
        public Task<string> HandleAsync(Greet request, CancellationToken cancellationToken)
        {
            log.Steps.Add("gestionnaire");
            return Task.FromResult($"Salut {request.Name}");
        }
    }

    public sealed class StepLog
    {
        public List<string> Steps { get; } = [];
    }

    public sealed class Outer<TRequest, TResponse>(StepLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> next, CancellationToken cancellationToken)
        {
            log.Steps.Add("extérieur avant");
            var response = await next();
            log.Steps.Add("extérieur après");
            return response;
        }
    }

    public sealed class Inner<TRequest, TResponse>(StepLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> next, CancellationToken cancellationToken)
        {
            log.Steps.Add("intérieur avant");
            var response = await next();
            log.Steps.Add("intérieur après");
            return response;
        }
    }

    private static ServiceProvider Build(bool withBehaviors)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<StepLog>();
        services.AddHandlers(typeof(MediatorTests).Assembly);
        if (withBehaviors)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Outer<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Inner<,>));
        }
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Sends_the_request_to_its_handler()
    {
        using var provider = Build(withBehaviors: false);
        using var scope = provider.CreateScope();

        var response = await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new Greet("Alexandre"));

        Assert.Equal("Salut Alexandre", response);
    }

    [Fact]
    public async Task The_first_registered_behavior_is_the_outermost()
    {
        using var provider = Build(withBehaviors: true);
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new Greet("Alexandre"));

        string[] expected = ["extérieur avant", "intérieur avant", "gestionnaire", "intérieur après", "extérieur après"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<StepLog>().Steps);
    }

    [Fact]
    public async Task A_request_without_handler_is_reported_clearly()
    {
        using var provider = Build(withBehaviors: false);
        using var scope = provider.CreateScope();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new OrphanRequest()));

        Assert.Contains(nameof(OrphanRequest), error.Message);
    }
}
