using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.Knowledge.Presentation;

public sealed class ArticleExceptionMiddleware(
    RequestDelegate next,
    ILogger<ArticleExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (WorkspaceAccessDeniedException)
        {
            await ArticleProblems.WorkspaceAccessDenied(context).ExecuteAsync(context);
        }
        catch (BadHttpRequestException exception)
        {
            logger.LogInformation(exception, "The Article request body could not be read.");
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
