using System;

/// <summary>
/// 中文字符检测工具（兼容 .NET 2.0+）
/// </summary>
public static class IfHaveChineseCode
{
    /// <summary>
    /// 判断字符串是否包含中文字符（基本 CJK 统一汉字区）
    /// </summary>
    /// <param name="input">待检测的字符串</param>
    /// <returns>如果包含中文字符则返回 true，否则返回 false</returns>
    public static bool HasChineseCode(string input)
    {
        // 空字符串或 null 直接返回 false
        if (string.IsNullOrEmpty(input))
            return false;

        // 遍历每个字符
        foreach (char c in input)
        {
            // 基本 CJK 统一汉字：0x4E00 ~ 0x9FA5
            // 如需包含扩展区（A、B等）可自行扩展范围
            if (c >= 0x4E00 && c <= 0x9FA5)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断字符是否为中文字符（基本 CJK 统一汉字区）
    /// </summary>
    public static bool IsChineseChar(char c)
    {
        return c >= 0x4E00 && c <= 0x9FA5;
    }
}