using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Management;

public class GameLogManager : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _logWriter;
    private readonly object _lock = new object();
    private bool _disposed;

    public GameLogManager(Process process, string logFilePath)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        _logWriter = new StreamWriter(logFilePath, false, Encoding.UTF8);
        _logWriter.AutoFlush = true;

        process.OutputDataReceived += OnDataReceived;
        process.ErrorDataReceived += OnDataReceived;
    }

    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;

        string clean = StripColorCodes(e.Data);

        // ---------- 过滤帧率日志 ----------
        if (Regex.IsMatch(clean, @"^\d+\s+fps,\s*\d+"))
        {
            // 可选：如果你想在控制台看到帧率但不想写文件，保留下面这行并取消注释
            // Console.WriteLine("[帧率] " + clean);
            return; // 不写入日志文件
        }

        lock (_lock)
        {
            _logWriter.WriteLine(clean);
        }
        Console.WriteLine("[游戏] " + clean);
    }

    private string StripColorCodes(string input)
    {
        input = Regex.Replace(input, "\u001B\\[[;?\\d]*[ -/]*[@-~]", "");
        input = Regex.Replace(input, "§[0-9a-fk-or]", "");
        return input;
    }

    public void AppendCrashReport(string minecraftDir)
    {
        string latestLog = Path.Combine(Path.Combine(minecraftDir, "logs"), "latest.log");
        if (File.Exists(latestLog))
        {
            lock (_lock)
            {
                _logWriter.WriteLine("--- BEGIN LATEST.LOG ---");
                _logWriter.Write(File.ReadAllText(latestLog));
                _logWriter.WriteLine("--- END LATEST.LOG ---");
            }
        }

        string crashFolder = Path.Combine(minecraftDir, "crash-reports");
        if (Directory.Exists(crashFolder))
        {
            var crashFiles = Directory.GetFiles(crashFolder, "*.txt");
            if (crashFiles.Length > 0)
            {
                var latestCrash = crashFiles[0];
                foreach (var f in crashFiles)
                    if (File.GetLastWriteTime(f) > File.GetLastWriteTime(latestCrash))
                        latestCrash = f;
                lock (_lock)
                {
                    _logWriter.WriteLine("--- CRASH REPORT ---");
                    _logWriter.Write(File.ReadAllText(latestCrash));
                    _logWriter.WriteLine("--- END CRASH REPORT ---");
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _process.OutputDataReceived -= OnDataReceived;
            _process.ErrorDataReceived -= OnDataReceived;
            _logWriter?.Close();
            _logWriter?.Dispose();
            _disposed = true;
        }
    }
}

public static class HardwareInfo
{
    /// <summary>
    /// 检测当前电脑是否有独立显卡（NVIDIA / AMD / 其他）
    /// </summary>
    /// <returns>
    /// true: 存在独立显卡
    /// false: 无独立显卡（只有集成显卡）
    /// null: 无法检测（异常或信息不足）
    /// </returns>
    public static bool? HasDedicatedGpu()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_VideoController");
            bool hasIntel = false;
            bool hasNvidiaOrAmd = false;

            foreach (ManagementObject obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                string pnpId = obj["PNPDeviceID"]?.ToString() ?? "";

                // Intel 显卡：名称含 "Intel" 或设备 ID 包含 VEN_8086（Intel 的 Vendor ID）
                bool isIntel = name.IndexOf("intel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               pnpId.IndexOf("VEN_8086", StringComparison.OrdinalIgnoreCase) >= 0;

                // NVIDIA / AMD：只根据名称判断，因为名称一定包含这些关键字
                bool isNvidia = name.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isAmd = name.IndexOf("amd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             name.IndexOf("radeon", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isIntel) hasIntel = true;
                if (isNvidia || isAmd) hasNvidiaOrAmd = true;
            }

            if (hasNvidiaOrAmd) return true;
            if (hasIntel) return false;
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[硬件检测] 异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取更详细的显卡信息（用于展示或日志）
    /// </summary>
    public static string GetGpuInfo()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var sb = new System.Text.StringBuilder();
            int index = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                index++;
                string name = obj["Name"]?.ToString() ?? "未知";
                string compat = obj["AdapterCompatibility"]?.ToString() ?? "未知";
                ulong ram = 0;
                try { ram = Convert.ToUInt64(obj["AdapterRAM"] ?? 0); } catch { }
                sb.AppendLine($"GPU {index}: {name}");
                sb.AppendLine($"  兼容性: {compat}");
                sb.AppendLine($"  显存: {ram / 1024 / 1024} MB");
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"获取显卡信息失败: {ex.Message}";
        }
    }
}