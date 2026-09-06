using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.Knowledge.Presentation;

public sealed class ArticleExceptionMiddleware(
    RequestDelegate next,
    ILogger<ArticleExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IWorkspaceContext workspaceContext)
    {
        try
        {
            if (context.Request.Path.StartsWithSegments("/api/articles"))
            {
                _ = workspaceContext.WorkspaceId;
                _ = workspaceContext.ActorId;
            }

            await next(context);
        }
        catch (WorkspaceAccessDeniedException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ArticleProblems.WorkspaceAccessDenied(context).ExecuteAsync(context);
        }
        catch (BadHttpRequestException exception)
        {
            logger.LogInformation(
                "The Article request body could not be read. TraceId: {TraceId}; ErrorType: {ErrorType}.",
                context.TraceIdentifier,
                exception.GetType().Name);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ArticleProblems.Validation(
                    context,
                    new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["request"] = ["The request body is invalid."],
                    })
                .ExecuteAsync(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Provider and parser exception messages can contain private content.
            logger.LogError(
                "An unexpected error occurred while processing an Article request. TraceId: {TraceId}; ErrorType: {ErrorType}.",
                context.TraceIdentifier,
                exception.GetType().Name);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ArticleProblems.Unexpected(context).ExecuteAsync(context);
        }
    }
}
