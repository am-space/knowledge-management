using Knowledge.Server.Knowledge.Presentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knowledge.Server.UnitTests;

public sealed class ArticleExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RethrowsWhenResponseHasStarted()
    {
        var expected = new InvalidOperationException("Failure after response start.");
        var middleware = new ArticleExceptionMiddleware(
            _ => throw expected,
            NullLogger<ArticleExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Same(expected, actual);
        Assert.True(context.Response.HasStarted);
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
