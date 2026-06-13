using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TraductorPo.Models;

namespace TraductorPo.Services;

public class LibreTranslateService(HttpClient http)
{
    public static readonly string[] PublicInstances =
    [
        "https://libretranslate.com",
        "https://translate.terraprint.co",
        "https://lt.vern.cc",
        "https://translate.flossboxin.org.in",
        "https://translate.fedilab.app",
    ];

    public async Task<(bool ok, string message)> TestConnectionAsync(string baseUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await http.GetAsync($"{baseUrl}/languages", cts.Token);
            return response.IsSuccessStatusCode
                ? (true, "Connection OK")
                : (false, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<TranslationLanguage>> GetLanguagesAsync(string baseUrl)
    {
        try
        {
            var langs = await http.GetFromJsonAsync<List<LibreLanguageDto>>($"{baseUrl}/languages");
            return langs?.Select(l => new TranslationLanguage(l.Code, l.Name)).ToList()
                   ?? Fallback();
        }
        catch
        {
            return Fallback();
        }
    }

    public async Task<string> TranslateAsync(
        string text, string source, string target,
        string baseUrl, string apiKey,
        Func<int, Task>? onRateLimit = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var body = new LibreTranslateRequest
        {
            Q = text,
            Source = source,
            Target = target,
            ApiKey = apiKey
        };

        int[] retryDelaysMs = [5_000, 12_000, 25_000];

        for (int attempt = 0; attempt <= retryDelaysMs.Length; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var response = await http.PostAsJsonAsync($"{baseUrl}/translate", body, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                && attempt < retryDelaysMs.Length)
            {
                var waitMs = retryDelaysMs[attempt];
                if (response.Headers.RetryAfter?.Delta is { } delta)
                    waitMs = Math.Max(waitMs, (int)delta.TotalMilliseconds + 1_000);

                if (onRateLimit != null)
                    await onRateLimit(waitMs / 1000);

                await Task.Delay(waitMs, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken: ct);
            return result?.TranslatedText ?? text;
        }

        throw new HttpRequestException("Rate limit exceeded after retries.");
    }

    private static List<TranslationLanguage> Fallback() =>
    [
        new("en", "English"),
        new("es", "Spanish"),
        new("fr", "French"),
        new("de", "German"),
        new("it", "Italian"),
        new("pt", "Portuguese"),
        new("nl", "Dutch"),
        new("pl", "Polish"),
        new("ru", "Russian"),
        new("zh", "Chinese"),
        new("ja", "Japanese"),
        new("ko", "Korean"),
        new("ar", "Arabic"),
        new("tr", "Turkish"),
        new("sv", "Swedish"),
        new("da", "Danish"),
        new("fi", "Finnish"),
        new("uk", "Ukrainian"),
        new("hi", "Hindi"),
    ];

    private sealed class LibreLanguageDto
    {
        [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }

    private sealed class LibreTranslateRequest
    {
        [JsonPropertyName("q")] public string Q { get; set; } = string.Empty;
        [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
        [JsonPropertyName("target")] public string Target { get; set; } = string.Empty;
        [JsonPropertyName("format")] public string Format { get; set; } = "text";
        [JsonPropertyName("api_key")] public string ApiKey { get; set; } = string.Empty;
    }

    private sealed class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")] public string TranslatedText { get; set; } = string.Empty;
    }
}
