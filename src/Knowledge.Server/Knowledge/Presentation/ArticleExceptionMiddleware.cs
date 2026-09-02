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
            logger.LogInformation(exception, "The Article request body could not be read.");
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
            logger.LogError(exception, "An unexpected error occurred while processing an Article request.");
            if (context.Response.HasStarted)
            {
                throw;
            }

            await ArticleProblems.Unexpected(context).ExecuteAsync(context);
        }
    }
}
