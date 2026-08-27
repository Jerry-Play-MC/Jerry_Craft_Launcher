using System;
using System.Collections.Generic;
using System.IO;
using fNbt;

namespace New_Launcher.SavesManager
{
    public static class LevelDatManager
    {
        public const int MIN_DATA_VERSION_26_1 = 4189; // 26.1 快照6

        public static List<ModificationItem> GetModifications(string worldPath)
        {
            var editor = GetEditor(worldPath);
            return editor.GetModifications();
        }

        public static void ApplyModification(string worldPath, string name, object value)
        {
            var editor = GetEditor(worldPath);
            editor.ApplyModification(name, value);
        }

        private static ILevelDatEditor GetEditor(string worldPath)
        {
            if (string.IsNullOrEmpty(worldPath))
                throw new ArgumentException("存档路径不能为空", nameof(worldPath));

            string levelDatPath = Path.Combine(worldPath, "level.dat");
            if (!File.Exists(levelDatPath))
                throw new FileNotFoundException($"找不到 level.dat: {levelDatPath}");

            int dataVersion = -1;
            try
            {
                var tempFile = new NbtFile();
                // 修复：显式传入 null 作为 selector
                tempFile.LoadFromFile(levelDatPath, NbtCompression.GZip, null);
                var dataTag = tempFile.RootTag?.Get<NbtCompound>("Data");
                var verTag = dataTag?.Get<NbtInt>("DataVersion");
                if (verTag != null) dataVersion = verTag.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("无法读取 level.dat 的 DataVersion", ex);
            }

            if (dataVersion >= MIN_DATA_VERSION_26_1)
                return new LevelDatEditor26(worldPath);
            else
                return new LevelDatEditorLegacy(levelDatPath);
        }

        /// <summary>
        /// 急救极限模式：不关闭极限模式，仅重置玩家为生存并清除死亡标记
        /// </summary>
        /// <param name="worldPath">存档文件夹路径</param>
        public static void ResurrectHardcorePlayer(string worldPath)
        {
            var editor = GetEditor(worldPath);
            if (editor is LevelDatEditorLegacy legacy)
            {
                legacy.ResurrectPlayerOnly();
                legacy.Save();
            }
            else if (editor is LevelDatEditor26 modern)
            {
                modern.ResurrectPlayerOnly();
                modern.Save();
            }
            else
            {
                throw new NotSupportedException("不支持的存档格式");
            }
        }
    }
}