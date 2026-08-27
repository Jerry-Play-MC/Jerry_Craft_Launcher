using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using WebPWrapper; // 如果 WebPWrapper 命名空间没有引入，请添加

namespace New_Launcher
{
    /// <summary>
    /// 自定义控件：显示 Mod 信息卡片，支持点击触发 Mod 详情
    /// </summary>
    public partial class ModCardControl : UserControl
    {
        // 静态占位图，所有卡片共享
        private static Image _placeholderImage;
        // 当前卡片关联的 Mod ID
        private string _currentModId;
        // 当前正在下载的 URL（用于取消，暂未实现）
        private string _currentUrl;

        // 自定义点击事件，点击卡片时触发，并传递 Mod ID
        public event EventHandler<ModClickedEventArgs> ModClicked;

        public ModCardControl()
        {
            InitializeComponent();

            // 创建占位图
            if (_placeholderImage == null)
                _placeholderImage = CreatePlaceholderImage();

            // 设置 PictureBox 的占位图
            picIcon.ErrorImage = _placeholderImage;
            picIcon.InitialImage = _placeholderImage;
            picIcon.Image = _placeholderImage;

            // 为控件本身和所有子控件注册点击事件，使整个卡片可点击
            this.Click += OnCardClick;
            foreach (Control ctrl in this.Controls)
            {
                ctrl.Click += OnCardClick;
                // 如果子控件还有子控件，递归注册（本例中只有一个 PictureBox 和三个 Label，无需递归）
            }
        }

        /// <summary>
        /// 创建灰底问号占位图
        /// </summary>
        private Image CreatePlaceholderImage()
        {
            var bmp = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(230, 230, 230));
                using (var font = new Font("微软雅黑", 14, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Gray))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("?", font, brush, new RectangleF(0, 0, 64, 64), sf);
                }
            }
            return bmp;
        }

        /// <summary>
        /// 设置卡片数据，并触发图标异步加载
        /// </summary>
        public void SetData(ModrinthMod mod)
        {
            _currentModId = mod.Id;                // 保存 ID，用于点击事件

            if (string.IsNullOrEmpty(_currentModId))
            {
                // 如果 ID 无效，禁用整个卡片的点击
                this.Enabled = false;
                System.Diagnostics.Debug.WriteLine("[ModCard] Mod ID 为空，卡片不可点击。");
            }
            else
            {
                this.Enabled = true;
            }

            lblTitle.Text = mod.Title ?? "未知";
            lblDescription.Text = mod.Description ?? "";
            lblDownloads.Text = $"⬇ {mod.Downloads:N0} 下载";

            // 重置图标为占位图
            picIcon.Image = _placeholderImage;
            _currentUrl = mod.IconUrl;

            // 如果有图标 URL，开始异步加载
            if (!string.IsNullOrEmpty(_currentUrl))
            {
                System.Diagnostics.Debug.WriteLine($"[ModCard] 开始下载图标: {_currentUrl}");
                LoadIconWithRetry(_currentUrl, 3);
            }
        }

        /// <summary>
        /// 卡片点击处理：触发 ModClicked 事件
        /// </summary>
        private void OnCardClick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentModId) && ModClicked != null)
            {
                ModClicked(this, new ModClickedEventArgs(_currentModId));
            }
        }

        // ---------- 图标下载与解码（已包含 WebP 支持） ----------
        private void LoadIconWithRetry(string url, int maxRetries)
        {
            DownloadIcon(url, 0, maxRetries);
        }

        private void DownloadIcon(string url, int retryCount, int maxRetries)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
                request.Timeout = 30000;
                request.ReadWriteTimeout = 60000;
                request.KeepAlive = false;

                request.BeginGetResponse(new AsyncCallback(ar =>
                {
                    try
                    {
                        using (var response = (HttpWebResponse)request.EndGetResponse(ar))
                        using (var stream = response.GetResponseStream())
                        {
                            if (response.StatusCode != HttpStatusCode.OK || stream == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ModCard] HTTP {response.StatusCode} - {url}");
                                RetryOrFail(url, retryCount, maxRetries);
                                return;
                            }

                            // 读取完整数据到内存
                            var ms = new System.IO.MemoryStream();
                            byte[] buffer = new byte[8192];
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                ms.Write(buffer, 0, read);
                            byte[] imageData = ms.ToArray();

                            // 尝试解码图片（支持 PNG/JPEG/GIF/WebP）
                            Image img = DecodeImage(imageData, response.ContentType);
                            if (img != null)
                            {
                                if (picIcon.InvokeRequired)
                                    picIcon.Invoke(new Action(() => { picIcon.Image = img; }));
                                else
                                    picIcon.Image = img;
                                System.Diagnostics.Debug.WriteLine($"[ModCard] 图标加载成功: {url}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[ModCard] 无法解码图片 (Content-Type: {response.ContentType}) - {url}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModCard] 下载异常 (重试 {retryCount + 1}/{maxRetries}): {ex.Message} - {url}");
                        RetryOrFail(url, retryCount, maxRetries);
                    }
                }), null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModCard] 启动下载失败: {ex.Message} - {url}");
                RetryOrFail(url, retryCount, maxRetries);
            }
        }

        /// <summary>
        /// 解码图片数据，支持 PNG/JPEG/GIF 和 WebP
        /// </summary>
        private Image DecodeImage(byte[] data, string contentType)
        {
            // 1. 先尝试用 System.Drawing 加载（支持 PNG/JPEG/GIF）
            try
            {
                using (var ms = new System.IO.MemoryStream(data))
                {
                    using (var img = Image.FromStream(ms))
                    {
                        // 创建副本，防止流释放后图像失效
                        return new Bitmap(img);
                    }
                }
            }
            catch
            {
                // 2. 如果失败且 Content-Type 是 WebP，或数据头是 WebP 特征，用 WebPWrapper 解码
                if (IsWebPData(data) || IsWebPContentType(contentType))
                {
                    try
                    {
                        using (var webp = new WebP())
                        {
                            using (var img = webp.Decode(data))
                            {
                                return new Bitmap(img);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModCard] WebP 解码失败: {ex.Message}");
                    }
                }
                return null;
            }
        }

        private bool IsWebPContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;
            return contentType.ToLowerInvariant().Contains("webp");
        }

        private bool IsWebPData(byte[] data)
        {
            if (data == null || data.Length < 12) return false;
            // RIFF 头 + WEBP 标识
            return data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                   data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50;
        }

        private void RetryOrFail(string url, int retryCount, int maxRetries)
        {
            if (retryCount < maxRetries - 1)
            {
                int delay = (retryCount + 1) * 500;
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = delay;
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    DownloadIcon(url, retryCount + 1, maxRetries);
                };
                timer.Start();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ModCard] 所有重试失败: {url}");
            }
        }

        public void CancelAsyncLoad()
        {
            // 可扩展取消逻辑
        }
    }

    /// <summary>
    /// 卡片点击事件参数，携带 Mod ID
    /// </summary>
    public class ModClickedEventArgs : EventArgs
    {
        public string ModId { get; private set; }
        public ModClickedEventArgs(string modId)
        {
            ModId = modId;
        }
    }
}