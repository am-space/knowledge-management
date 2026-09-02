using Knowledge.Server.Knowledge.Presentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
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
        var middleware = new ArticleExceptionMiddleware(
            _ => throw expected,
            NullLogger<ArticleExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var actual = await Record.ExceptionAsync(() =>
            middleware.InvokeAsync(context, new FixedWorkspaceContext()));

        Assert.Same(expected, actual);
        Assert.True(context.Response.HasStarted);
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
