using System;

namespace New_Launcher
{
    public class LaunchContext
    {
        public string VersionId { get; set; }
        public string GameVersion { get; set; }
        public string MinecraftDir { get; set; }
        public string VersionDataPath { get; set; }
        public string JavaPath { get; set; }
        public string Username { get; set; }
        public string Uuid { get; set; }
        public string AccessToken { get; set; }
        public string UserType { get; set; }
        public string MaxMemory { get; set; }
        public string InitMemory { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // 以下为用户自定义的额外参数
        public bool IsVersionIsolated { get; set; }// 版本隔离
    }

    public interface ILauncher
    {
        void Launch(LaunchContext context);
    }
}