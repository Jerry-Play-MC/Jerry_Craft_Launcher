/* 调用方式：
 *     string time = GetTime.GetCurrentTimeString();
*/

using System;

/// <summary>
/// 时间格式化辅助类
/// </summary>
public static class GetTime
{
    /// <summary>
    /// 获取当前时间的格式化字符串：[年/月/日] 时:分:秒
    /// </summary>
    public static string GetCurrentTimeString()
    {
        DateTime now = DateTime.Now;
        return string.Format("[{0:yyyy/MM/dd}] {0:HH:mm:ss}", now);
    }

    // 或者使用 ToString 重载：
    // return now.ToString("[yyyy/MM/dd] HH:mm:ss");
}