using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

namespace New_Launcher.Error
{
    public partial class ErrorForm : Form
    {
        public string Error;
        public string LogFilePath;
        private string _minecraftRoot;
        private string _versionId;
        private bool _isIsolated;

        public ErrorForm(string error, string logFilePath, string minecraftRoot, string versionId, bool isIsolated)
        {
            try
            {
                InitializeComponent();
                this.StartPosition = FormStartPosition.CenterParent;
                this.Error = error;
                this.LogFilePath = logFilePath;
                this._minecraftRoot = minecraftRoot;
                this._versionId = versionId;
                this._isIsolated = isIsolated;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误报告窗口初始化失败：{ex.Message}");
            }
        }

        private string GetMinecraftRoot() => _minecraftRoot;
        private string GetCurrentVersion() => _versionId;
        private bool GetIsolatedStatus() => _isIsolated;

        private void ErrorForm_Load(object sender, EventArgs e)
        {
            // 优先使用外部传入的分析结果
            string displayText = this.Error;

            // 如果传入的分析结果为空或为默认值，尝试自动分析日志
            if (string.IsNullOrEmpty(displayText) || displayText == "（无详细分析结果）")
            {
                if (!string.IsNullOrEmpty(LogFilePath) && System.IO.File.Exists(LogFilePath))
                {
                    try
                    {
                        string logContent = System.IO.File.ReadAllText(LogFilePath, Encoding.UTF8);
                        string analyzed = LogErrorAnalyzer.Analyze(logContent);
                        if (!string.IsNullOrEmpty(analyzed))
                            displayText = analyzed;
                        else
                            displayText = "未能自动识别错误原因，请查看下方日志或导出报告寻求帮助。";
                    }
                    catch (Exception ex)
                    {
                        displayText = $"读取日志文件失败：{ex.Message}";
                    }
                }
                else
                {
                    displayText = "未提供日志文件路径，无法自动分析。";
                }
            }

            this.richTextBox_ErrorText.Text = displayText;
        }

        private void button_OutputErrorLog_Click(object sender, EventArgs e)
        {
            // 1. 选择保存路径
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "ZIP 压缩包|*.zip";
                sfd.FileName = $"ErrorReport_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                sfd.Title = "导出错误报告";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                // 2. 准备临时工作目录
                string tempDir = Path.Combine(Path.GetTempPath(), $"ErrorReport_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 3. 获取当前环境参数（从主窗体传过来，或者重新读取）
                    string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); // 启动器路径
                    string minecraftRoot = GetMinecraftRoot();   // 你的 .minecraft 路径
                    string versionId = GetCurrentVersion();      // 当前选中的版本名
                    bool isIsolated = GetIsolatedStatus();       // 从 Settings.json 读

                    // 4. 计算实际路径
                    string gameDir = isIsolated
                        ? Path.Combine(Path.Combine(minecraftRoot, "versions"), versionId)
                        : minecraftRoot;

                    string logDir = Path.Combine(gameDir, "logs");
                    string crashDir = Path.Combine(gameDir, "crash-reports");
                    string versionJsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftRoot, "versions"), versionId), $"{versionId}.json");

                    // 5. 收集文件（只收集存在的）
                    var fileMappings = new Dictionary<string, string>(); // Key: 原始路径, Value: 在zip中的相对路径(含文件夹)

                    // 5.1 日志文件
                    AddIfExists(fileMappings, Path.Combine(logDir, "latest.log"), "logs/latest.log");
                    AddIfExists(fileMappings, Path.Combine(logDir, "debug.log"), "logs/debug.log");

                    // 5.2 崩溃报告（整个文件夹）
                    if (Directory.Exists(crashDir))
                    {
                        foreach (var crashFile in Directory.GetFiles(crashDir, "*.txt"))
                        {
                            string fileName = Path.GetFileName(crashFile);
                            AddIfExists(fileMappings, crashFile, $"crash-reports/{fileName}");
                        }
                    }

                    // 5.3 版本 JSON
                    AddIfExists(fileMappings, versionJsonPath, $"{versionId}.json");

                    // 5.4 JVM 致命错误日志（根目录）
                    var hsErrFiles = Directory.GetFiles(minecraftRoot, "hs_err_pid*.log");
                    foreach (var hsFile in hsErrFiles)
                    {
                        AddIfExists(fileMappings, hsFile, Path.GetFileName(hsFile));
                    }

                    // 5.5 调试启动参数
                    AddIfExists(fileMappings, Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "LastLauncher.bat"), "LastLauncher.bat");

                    // 6. 将文件复制到临时目录并脱敏
                    foreach (var kvp in fileMappings)
                    {
                        string srcPath = kvp.Key;
                        string destRelPath = kvp.Value; // 相对路径（含子文件夹）
                        string destFullPath = Path.Combine(tempDir, destRelPath);

                        // 确保目标文件夹存在
                        Directory.CreateDirectory(Path.GetDirectoryName(destFullPath));

                        // 读取内容并脱敏（替换用户名）
                        string content = System.IO.File.ReadAllText(srcPath, Encoding.UTF8);
                        content = content.Replace(Environment.UserName, "[USER]");
                        // 如果你还想替换具体的路径，可以加：
                        // content = content.Replace(minecraftRoot, "[MINECRAFT_DIR]");

                        System.IO.File.WriteAllText(destFullPath, content, Encoding.UTF8);
                    }

                    // 7. 写入自动分析结果（由 LogErrorAnalyzer 生成，已在 ErrorForm 的 Error 属性中）
                    string analysisFilePath = Path.Combine(tempDir, "_error_analysis.txt");
                    System.IO.File.WriteAllText(analysisFilePath, this.Error ?? "（无详细分析结果）", Encoding.UTF8);

                    // 8. 写入 README 说明
                    string readmeContent =
                        "此错误报告包含以下文件：\n" +
                        "├─ logs/              # 游戏运行日志\n" +
                        "├─ crash-reports/     # 崩溃报告（如果有）\n" +
                        "├─ <版本名>.json      # 版本元数据\n" +
                        "├─ hs_err_pid*.log    # JVM 崩溃日志（如果有）\n" +
                        "├─ _error_analysis.txt # 启动器自动分析结果\n" +
                        "└─ LastLauncher.bat # 调试启动参数，启动器不会使用此文件启动Minecraft但内含完整的启动参数\n\n" +
                        "注意：敏感信息（系统用户名）已替换为 [USER]。\n" +
                        "导出时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    System.IO.File.WriteAllText(Path.Combine(tempDir, "README.txt"), readmeContent, Encoding.UTF8);

                    // 9. ★★★ 使用 SharpZipLib 快速打包 ★★★
                    FastZip fastZip = new FastZip();
                    // 参数：输出zip路径，要打包的文件夹，是否递归，文件筛选器（空字符串表示全部）
                    fastZip.CreateZip(sfd.FileName, tempDir, true, "");

                    MessageBox.Show("错误报告已导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 10. 清理临时目录
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
        }

        // 辅助方法：添加文件到字典（避免重复判断存在性）
        private void AddIfExists(Dictionary<string, string> dict, string srcPath, string destRelPath)
        {
            if (System.IO.File.Exists(srcPath))
            {
                dict[srcPath] = destRelPath;
            }
        }

        private void button_Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void richTextBox_ErrorText_TextChanged(object sender, EventArgs e)
        {

        }
    }
}