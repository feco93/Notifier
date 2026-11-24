using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifier.Core.Sources;

namespace Notifier.Sources.Bbc;

public static class BbcServiceCollectionExtensions
{
    public static IServiceCollection AddBbcNewsSource(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BbcOptions>()
            .Bind(configuration.GetSection(BbcOptions.Section))
            .Validate(o => o.Feeds.Count > 0 && o.Feeds.Values.All(IsWebUrl), "Every BBC feed must be an absolute http(s) URL.")
            .Validate(o => o.Timeout > TimeSpan.Zero, "BBC timeout must be positive.")
            .Validate(o => new HttpRequestMessage().Headers.UserAgent.TryParseAdd(o.UserAgent), "BBC user agent is not a valid header value.")
            .ValidateOnStart();

        services.AddHttpClient<IArticleSource, BbcRssSource>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<BbcOptions>>().Value;
            http.Timeout = options.Timeout;
            http.MaxResponseContentBufferSize = 5_000_000;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });
        return services;
    }

    private static bool IsWebUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp);
}
