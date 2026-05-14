namespace Esp32Controller.Models;

public record RgbColor(byte R, byte G, byte B)
{
    public static bool TryParse(string input, out RgbColor? result)
    {
        result = null;
        var parts = input.Split(',');
        if (parts.Length != 3) return false;

        if (!byte.TryParse(parts[0].Trim(), out byte r)) return false;
        if (!byte.TryParse(parts[1].Trim(), out byte g)) return false;
        if (!byte.TryParse(parts[2].Trim(), out byte b)) return false;

        result = new RgbColor(r, g, b);
        return true;
    }

    // 序列化成協定格式
    public string ToProtocolString() => $"{R},{G},{B}";

    // WPF 用的 hex 格式
    public string ToHexString() => $"#{R:X2}{G:X2}{B:X2}";
}