using System.Text;
using TraductorPo.Models;

namespace TraductorPo.Services;

public class PoFileService
{
    public List<PoEntry> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var entries = new List<PoEntry>();
        PoEntry? current = null;
        string? currentField = null;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                if (current != null)
                {
                    if (!string.IsNullOrEmpty(current.MsgId))
                        entries.Add(current);
                    current = null;
                    currentField = null;
                }
            }
            else if (line.StartsWith('#'))
            {
                current ??= new PoEntry();
                current.Comments.Add(line);
            }
            else if (line.StartsWith("msgctxt "))
            {
                current ??= new PoEntry();
                current.Context = Unquote(line[8..]);
                currentField = "msgctxt";
            }
            else if (line.StartsWith("msgid "))
            {
                current ??= new PoEntry();
                current.MsgId = Unquote(line[6..]);
                currentField = "msgid";
            }
            else if (line.StartsWith("msgstr "))
            {
                current ??= new PoEntry();
                current.MsgStr = Unquote(line[7..]);
                currentField = "msgstr";
            }
            else if (line.StartsWith('"') && current != null)
            {
                var cont = Unquote(line);
                if (currentField == "msgctxt") current.Context += cont;
                else if (currentField == "msgid") current.MsgId += cont;
                else if (currentField == "msgstr") current.MsgStr += cont;
            }
        }

        if (current != null && !string.IsNullOrEmpty(current.MsgId))
            entries.Add(current);

        return entries;
    }

    public byte[] Generate(List<PoEntry> entries)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(false));

        foreach (var entry in entries)
        {
            foreach (var comment in entry.Comments)
                writer.WriteLine(comment);

            if (!string.IsNullOrEmpty(entry.Context))
                writer.WriteLine($"msgctxt \"{Escape(entry.Context)}\"");

            writer.WriteLine($"msgid \"{Escape(entry.MsgId)}\"");
            writer.WriteLine($"msgstr \"{Escape(entry.MsgStr)}\"");
            writer.WriteLine();
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.StartsWith('"') && s.EndsWith('"'))
            s = s[1..^1];
        return s.Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "\\r")
         .Replace("\t", "\\t");
}
