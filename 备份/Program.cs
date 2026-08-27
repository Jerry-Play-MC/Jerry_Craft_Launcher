using New_Launcher;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Jerry_Studio_Minecraft_Launcher
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("==================== Jerry Studio Minecraft Launcher 开始运行 ====================");

            try
            {
                // 加载嵌入的 libwebp_x86.dll
                NativeLibraryLoader.LoadEmbeddedDll(
                    "New_Launcher.libwebp_x86.dll",
                    "libwebp_x86.dll"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载 WebP 库失败：" + ex.Message);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072; // TLS 1.2
            System.Net.ServicePointManager.Expect100Continue = false;

            string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string SettingPass = Path.Combine(LauncherPath, "Launcher Setting");
            Console.WriteLine(GetTime.GetCurrentTimeString() + "获取的程序路径：" + LauncherPath); // 获取当前程序的路径

            if (!Directory.Exists(SettingPass)) // 首次使用
            {
                string UserRead = @"用户须知：
1.我们没有在代码中添加恶意操作，因为此启动器导致电脑崩溃的Jerry Studio概不负责。
2.请不要二次分发。";
                MessageBox.Show(UserRead, "欢迎使用Jerry Studio Minecraft Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show("初来乍到，创建一个你的角色吧！\r\n 注：每个角色是存档从配置文件中读取人物背包物品信息的唯一凭证，请妥善保管您的角色信息！", "创建你的角色", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Directory.CreateDirectory(SettingPass);

                Application.Run(new CreateRole(true));
            }
            else
            {
                Application.Run(new MainForm());
            }
        }

        /// <summary>
        /// 将嵌入的程序集资源提取到指定目录
        /// </summary>
        /// <param name="resourceName">资源的完整名称（命名空间.文件名）</param>
        /// <param name="destinationPath">目标文件路径（含文件名）</param>
        public static void CopyFileInEXE(string resourceName, string destinationPath)
        {
            // 1. 获取当前程序集（也可以是 typeof(SomeClass).Assembly）
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 2. 打开嵌入资源的流
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new Exception($"未找到嵌入的资源: {resourceName}");

                // 3. 确保目标目录存在
                string destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                // 4. 将流写入目标文件
                using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    // .NET Framework 3.5 没有 Stream.CopyTo，需要手动缓冲循环
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = resourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}
