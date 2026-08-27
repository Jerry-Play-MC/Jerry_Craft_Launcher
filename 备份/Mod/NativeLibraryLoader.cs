using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

public static class NativeLibraryLoader
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    /// <summary>
    /// 从嵌入资源中提取 DLL 到启动器目录下的 Launcher Setting\Natives 文件夹
    /// </summary>
    /// <param name="resourceName">资源的完整名称，例如 "New_Launcher.libwebp_x86.dll"</param>
    /// <param name="outputFileName">输出到目标目录的文件名，例如 "libwebp_x86.dll"</param>
    public static void LoadEmbeddedDll(string resourceName, string outputFileName)
    {
        // 获取启动器根目录（当前 exe 所在目录）
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // 目标目录：启动器根目录/Launcher Setting/Natives
        string targetDir = Path.Combine(Path.Combine(baseDir, "Launcher Setting"), "Natives");

        // 确保目录存在
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                // 如果找不到资源，列出所有可用资源方便调试
                var allResources = string.Join("\n", assembly.GetManifestResourceNames());
                throw new FileNotFoundException($"无法找到嵌入资源 '{resourceName}'。\n可用资源列表：\n{allResources}");
            }

            string dllPath = Path.Combine(targetDir, outputFileName);

            // 如果文件已存在，直接跳过写入（保留已有文件）
            if (!File.Exists(dllPath))
            {
                using (var fileStream = File.Create(dllPath))
                {
                    // .NET 3.5 兼容写法：手动复制流
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            // 将目标目录添加到 DLL 搜索路径（确保 LoadLibrary 能找到）
            SetDllDirectory(targetDir);

            // 加载 DLL
            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
                throw new Exception($"加载 {outputFileName} 失败，错误码: {Marshal.GetLastWin32Error()}");
        }
    }
}