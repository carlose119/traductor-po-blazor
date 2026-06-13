namespace TraductorPo.Models;

public class PoEntry
{
    public List<string> Comments { get; set; } = [];
    public string? Context { get; set; }
    public string MsgId { get; set; } = string.Empty;
    public string MsgStr { get; set; } = string.Empty;
    public TranslationStatus Status { get; set; } = TranslationStatus.Pending;

    public bool NeedsTranslation =>
        !string.IsNullOrWhiteSpace(MsgId) && string.IsNullOrWhiteSpace(MsgStr);
}

public enum TranslationStatus
{
    Pending,
    Translating,
    Done,
    Error,
    Skipped
}
