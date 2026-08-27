using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class LoadingForm : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string SettingsDir = Path.Combine(LauncherPath, "Launcher Setting");

        private List<DownloadTask> _tasks = new List<DownloadTask>();
        private int _completed = 0;
        private bool _hasError = false;
        private Exception _firstException = null;

        public LoadingForm()
        {
            InitializeComponent();
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            this.Shown += LoadingForm_Shown;
        }

        private void LoadingForm_Shown(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(StartDownload);
        }

        private void StartDownload(object state)
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                // ---- 定义下载任务（每个任务包含多个镜像 URL，按优先级排列） ----
                _tasks.Add(new DownloadTask
                {
                    Urls = new List<string>
                    {
                        "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json",
                        "https://piston-meta.mojang.com/mc/game/version_manifest.json"
                    },
                    LocalPath = Path.Combine(SettingsDir, "version_manifest.json"),
                    Name = "原版版本清单"
                });

                _tasks.Add(new DownloadTask
                {
                    Urls = new List<string>
                    {
                        "https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/maven-metadata.xml",
                        "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml"
                    },
                    LocalPath = Path.Combine(SettingsDir, "forge_versions.xml"),
                    Name = "Forge 版本列表"
                });

                _tasks.Add(new DownloadTask
                {
                    Urls = new List<string>
                    {
                        "https://bmclapi2.bangbang93.com/maven/net/neoforged/neoforge/maven-metadata.xml",
                        "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml"
                    },
                    LocalPath = Path.Combine(SettingsDir, "neoforge_versions.xml"),
                    Name = "NeoForge 版本列表"
                });

                // Fabric 和 Quilt 暂无可用的国内镜像，仅官方源（文件很小，不影响速度）
                _tasks.Add(new DownloadTask
                {
                    Urls = new List<string>
                    {
                        "https://meta.fabricmc.net/v2/versions/loader",
                        "https://meta.fabricmc.net/v2/versions/loader"  // 备用相同
                    },
                    LocalPath = Path.Combine(SettingsDir, "fabric_loaders.json"),
                    Name = "Fabric 加载器列表"
                });

                _tasks.Add(new DownloadTask
                {
                    Urls = new List<string>
                    {
                        "https://meta.quiltmc.org/v3/versions/loader",
                        "https://meta.quiltmc.org/v3/versions/loader"
                    },
                    LocalPath = Path.Combine(SettingsDir, "quilt_loaders.json"),
                    Name = "Quilt 加载器列表"
                });

                int total = _tasks.Count;
                UpdateStatus($"正在检查版本清单缓存 ({0}/{total})...");

                using (var resetEvent = new ManualResetEvent(false))
                {
                    int remaining = total;
                    foreach (var task in _tasks)
                    {
                        ThreadPool.QueueUserWorkItem(obj =>
                        {
                            DownloadTask t = (DownloadTask)obj;
                            bool success = false;
                            try
                            {
                                // ---------- 缓存有效期检查 ----------
                                if (File.Exists(t.LocalPath))
                                {
                                    DateTime lastWrite = File.GetLastWriteTime(t.LocalPath);
                                    if ((DateTime.Now - lastWrite).TotalHours < 4)
                                    {
                                        Console.WriteLine($"[缓存] 使用缓存文件: {t.Name} (修改时间: {lastWrite})");
                                        success = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[缓存] 缓存已过期 ({t.Name}，距上次修改已超过4小时)，重新下载...");
                                    }
                                }

                                // 如果缓存无效，则尝试下载
                                if (!success)
                                {
                                    foreach (string url in t.Urls)
                                    {
                                        try
                                        {
                                            DownloadFileWithRetry(url, t.LocalPath, 2);
                                            success = true;
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[下载] 从 {url} 下载 {t.Name} 失败: {ex.Message}，尝试下一个镜像...");
                                        }
                                    }
                                }

                                if (!success)
                                    throw new Exception($"所有镜像尝试均失败: {t.Name}");

                                Interlocked.Increment(ref _completed);
                                int done = Interlocked.CompareExchange(ref _completed, 0, 0);
                                UpdateStatus($"正在下载版本清单 ({done}/{total})...");
                                UpdateProgress(done * 100 / total);
                            }
                            catch (Exception ex)
                            {
                                if (!_hasError)
                                {
                                    _hasError = true;
                                    _firstException = ex;
                                }
                            }
                            finally
                            {
                                if (Interlocked.Decrement(ref remaining) == 0)
                                    resetEvent.Set();
                            }
                        }, task);
                    }
                    resetEvent.WaitOne();
                }

                // 所有任务完成（无论成败）
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (_hasError)
                    {
                        MessageBox.Show($"部分版本清单下载失败：\n{_firstException.Message}\n\n启动器将继续运行，但部分功能可能受影响。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show($"下载过程中发生严重错误：\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Abort;
                    this.Close();
                });
            }
        }

        /// <summary>
        /// 下载文件，失败时重试（最多 maxRetries 次）
        /// </summary>
        private void DownloadFileWithRetry(string url, string localPath, int maxRetries)
        {
            string dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        // 强制 TLS 1.2
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                        client.DownloadFile(url, localPath);
                    }
                    return; // 成功
                }
                catch (WebException ex)
                {
                    // 如果是协议错误（如 404），不再重试（因为镜像不存在）
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                            throw; // 直接抛出，表示该 URL 无效
                    }
                    // 其他网络错误（超时、连接失败）可以重试
                    if (attempt == maxRetries)
                        throw; // 重试次数用完
                    Thread.Sleep(500 * attempt); // 递增等待
                }
                catch
                {
                    if (attempt == maxRetries)
                        throw;
                    Thread.Sleep(500 * attempt);
                }
            }
        }

        private void UpdateStatus(string text)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                labelStatus.Text = text;
            });
        }

        private void UpdateProgress(int percent)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                progressBar.Value = Math.Min(percent, 100);
            });
        }

        private class DownloadTask
        {
            public List<string> Urls { get; set; }
            public string LocalPath { get; set; }
            public string Name { get; set; }
        }
    }
}