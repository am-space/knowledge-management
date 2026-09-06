using Knowledge.Server.Knowledge.Presentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.UnitTests;

public sealed class ArticleExceptionMiddlewareTests
{
    [Theory]
    [InlineData("unexpected")]
    [InlineData("workspace")]
    [InlineData("bad-request")]
    public async Task InvokeAsync_RethrowsHandledExceptionsWhenResponseHasStarted(string exceptionKind)
    {
        Exception expected = exceptionKind switch
        {
            "workspace" => new global::Knowledge.Server.Workspaces.Features.WorkspaceAccessDeniedException(),
            "bad-request" => new BadHttpRequestException("Invalid request."),
            _ => new InvalidOperationException("Failure after response start."),
        };
        var logger = new CapturedLogger();
        var middleware = new ArticleExceptionMiddleware(_ => throw expected, logger);
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var actual = await Record.ExceptionAsync(() =>
            middleware.InvokeAsync(context, new FixedWorkspaceContext()));

        Assert.Same(expected, actual);
        Assert.True(context.Response.HasStarted);
        Assert.All(logger.Entries, entry =>
        {
            Assert.Null(entry.Exception);
            Assert.DoesNotContain(expected.Message, entry.Message, StringComparison.Ordinal);
            Assert.Contains(context.TraceIdentifier, entry.Message, StringComparison.Ordinal);
        });
        if (exceptionKind != "workspace") Assert.Single(logger.Entries);
    }

    private sealed class CapturedLogger : ILogger<ArticleExceptionMiddleware>
    {
        public List<(string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((formatter(state, exception), exception));
    }

    private sealed class FixedWorkspaceContext : IWorkspaceContext
    {
        public Guid WorkspaceId { get; } = Guid.NewGuid();

        public Guid ActorId { get; } = Guid.NewGuid();
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
