using Microsoft.AspNetCore.Http.HttpResults;
using Snap.Modules.Auth;

namespace Snap.Modules.Urls;

/// <summary>
/// Capa HTTP: traduce peticiones a llamadas al servicio y resultados a respuestas.
/// Crear, borrar y "mis URLs" exigen JWT; el listado general y el redirect son públicos.
/// </summary>
public static class UrlsEndpoints
{
    public static IEndpointRouteBuilder MapUrlsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/urls");
        api.MapPost("/", CreateAsync).RequireAuthorization();
        api.MapDelete("/{code}", DeleteAsync).RequireAuthorization();
        api.MapGet("/", ListAsync);
        api.MapGet("/mine", ListMineAsync).RequireAuthorization();

        // Ruta comodín para los códigos cortos. Las rutas literales (/health, /api/...) tienen prioridad.
        app.MapGet("/{code}", RedirectAsync);
        return app;
    }

    private static async Task<Results<Created<UrlResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateUrlRequest request, AuthenticatedUser user, UrlsService urls, HttpRequest http, CancellationToken cancellationToken)
    {
        // Validación temprana para responder 400 con cada campo inválido a la vez. El servicio vuelve a validar.
        var alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim();
        var errors = new Dictionary<string, string[]>();
        if (request.Url is null || !UrlsService.TryNormalizeUrl(request.Url, out _))
        {
            errors["url"] = [UrlsService.InvalidUrlMessage];
        }
        if (alias is not null && UrlsService.GetAliasError(alias) is { } aliasError)
        {
            errors["alias"] = [aliasError];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var created = alias is null
            ? await urls.CreateAsync(request.Url!, user.Id, cancellationToken)
            : await urls.TryCreateWithAliasAsync(request.Url!, alias, user.Id, cancellationToken);
        if (created is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: UrlsService.AliasTakenMessage);
        }

        var response = UrlResponse.From(created, http);
        return TypedResults.Created(response.ShortUrl, response);
    }

    private static async Task<Results<Ok<PagedResponse<MyUrlResponse>>, ValidationProblem>> ListMineAsync(
        AuthenticatedUser user, UrlsService urls, HttpRequest http, CancellationToken cancellationToken,
        int? page = null, int? pageSize = null)
    {
        var pageNumber = page ?? 1;
        var size = pageSize ?? UrlsService.DefaultPageSize;
        var errors = new Dictionary<string, string[]>();
        if (pageNumber < 1)
        {
            errors["page"] = [UrlsService.InvalidPageMessage];
        }
        if (size is < 1 or > UrlsService.MaxPageSize)
        {
            errors["pageSize"] = [UrlsService.InvalidPageSizeMessage];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await urls.ListMineAsync(user.Id, pageNumber, size, cancellationToken);
        return TypedResults.Ok(new PagedResponse<MyUrlResponse>(
            result.Items.Select(url => MyUrlResponse.From(url, http)).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        string code, AuthenticatedUser user, UrlsService urls, CancellationToken cancellationToken) =>
        await urls.DeleteAsync(code, user.Id, cancellationToken) switch
        {
            DeleteUrlResult.Deleted => TypedResults.NoContent(),
            DeleteUrlResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Solo el dueño de la URL puede borrarla."),
        };

    private static async Task<Ok<UrlResponse[]>> ListAsync(
        UrlsService urls, HttpRequest http, CancellationToken cancellationToken)
    {
        var all = await urls.ListAsync(cancellationToken);
        return TypedResults.Ok(all.Select(url => UrlResponse.From(url, http)).ToArray());
    }

    private static async Task<Results<RedirectHttpResult, NotFound>> RedirectAsync(
        string code, UrlsService urls, HttpRequest http, CancellationToken cancellationToken)
    {
        // Cada visita a un código existente queda registrada como click (para el dashboard).
        var url = await urls.VisitAsync(code, http.Headers.Referer.ToString(), cancellationToken);
        // 302 (no 301) para que el navegador no cachee la redirección y cada visita llegue al servidor (analíticas).
        return url is null ? TypedResults.NotFound() : TypedResults.Redirect(url.OriginalUrl);
    }
}
