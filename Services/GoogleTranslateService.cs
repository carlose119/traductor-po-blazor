using System.Text;
using System.Text.Json;
using TraductorPo.Models;

namespace TraductorPo.Services;

public class GoogleTranslateService(HttpClient http)
{
    private const string Endpoint = "https://translate.googleapis.com/translate_a/single";

    public async Task<string> TranslateAsync(
        string text, string source, string target,
        Func<int, Task>? onRateLimit = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var url = $"{Endpoint}?client=gtx&sl={Uri.EscapeDataString(source)}&tl={Uri.EscapeDataString(target)}&dt=t&q={Uri.EscapeDataString(text)}";

        int[] retryDelaysMs = [5_000, 12_000, 25_000];

        for (int attempt = 0; attempt <= retryDelaysMs.Length; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var response = await http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                && attempt < retryDelaysMs.Length)
            {
                var waitMs = retryDelaysMs[attempt];
                if (response.Headers.RetryAfter?.Delta is { } delta)
                    waitMs = Math.Max(waitMs, (int)delta.TotalMilliseconds + 1_000);

                if (onRateLimit != null) await onRateLimit(waitMs / 1000);
                await Task.Delay(waitMs, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return ParseResponse(json);
        }

        throw new HttpRequestException("Rate limit exceeded after retries.");
    }

    private static string ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array) return string.Empty;

        var sentences = root[0];
        if (sentences.ValueKind != JsonValueKind.Array) return string.Empty;

        var sb = new StringBuilder();
        foreach (var sentence in sentences.EnumerateArray())
        {
            if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
            {
                var part = sentence[0];
                if (part.ValueKind == JsonValueKind.String)
                    sb.Append(part.GetString());
            }
        }

        return sb.ToString();
    }

    public static List<TranslationLanguage> GetLanguages() =>
    [
        new("auto", "Auto detect"),
        new("af", "Afrikaans"),
        new("ar", "Arabic"),
        new("az", "Azerbaijani"),
        new("be", "Belarusian"),
        new("bg", "Bulgarian"),
        new("bn", "Bengali"),
        new("bs", "Bosnian"),
        new("ca", "Catalan"),
        new("cs", "Czech"),
        new("cy", "Welsh"),
        new("da", "Danish"),
        new("de", "German"),
        new("el", "Greek"),
        new("en", "English"),
        new("eo", "Esperanto"),
        new("es", "Spanish"),
        new("et", "Estonian"),
        new("eu", "Basque"),
        new("fa", "Persian"),
        new("fi", "Finnish"),
        new("fr", "French"),
        new("ga", "Irish"),
        new("gl", "Galician"),
        new("gu", "Gujarati"),
        new("he", "Hebrew"),
        new("hi", "Hindi"),
        new("hr", "Croatian"),
        new("hu", "Hungarian"),
        new("hy", "Armenian"),
        new("id", "Indonesian"),
        new("is", "Icelandic"),
        new("it", "Italian"),
        new("ja", "Japanese"),
        new("ka", "Georgian"),
        new("kk", "Kazakh"),
        new("km", "Khmer"),
        new("kn", "Kannada"),
        new("ko", "Korean"),
        new("lt", "Lithuanian"),
        new("lv", "Latvian"),
        new("mk", "Macedonian"),
        new("ml", "Malayalam"),
        new("mn", "Mongolian"),
        new("mr", "Marathi"),
        new("ms", "Malay"),
        new("mt", "Maltese"),
        new("my", "Myanmar"),
        new("ne", "Nepali"),
        new("nl", "Dutch"),
        new("no", "Norwegian"),
        new("pl", "Polish"),
        new("pt", "Portuguese"),
        new("ro", "Romanian"),
        new("ru", "Russian"),
        new("si", "Sinhala"),
        new("sk", "Slovak"),
        new("sl", "Slovenian"),
        new("sq", "Albanian"),
        new("sr", "Serbian"),
        new("sv", "Swedish"),
        new("sw", "Swahili"),
        new("ta", "Tamil"),
        new("te", "Telugu"),
        new("th", "Thai"),
        new("tl", "Filipino"),
        new("tr", "Turkish"),
        new("uk", "Ukrainian"),
        new("ur", "Urdu"),
        new("uz", "Uzbek"),
        new("vi", "Vietnamese"),
        new("zh-CN", "Chinese (Simplified)"),
        new("zh-TW", "Chinese (Traditional)"),
        new("zu", "Zulu"),
    ];
}
