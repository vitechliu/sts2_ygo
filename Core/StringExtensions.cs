namespace VYgo.Core;

public static class StringExtensions
{
    public static string FormatWithNumber(this string template, int number, char placeholder = '#')
    {
        // 找到所有连续的 #，用数字替换，位数保持一致
        int start = template.IndexOf(placeholder);
        int end = template.LastIndexOf(placeholder);
        if (start == -1) return template;
        
        int width = end - start + 1;
        string numberStr = number.ToString($"D{width}");
        return template.Substring(0, start) + numberStr + template.Substring(end + 1);
    }
}