using System;
using System.IO;
using System.Text;

namespace New_Launcher
{
    public static class FileUtils
    {
        public class Result
        {
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
            public string Data { get; set; }

            public static Result Success(string data = null) => new Result { IsSuccess = true, Data = data };
            public static Result Failure(string error) => new Result { IsSuccess = false, ErrorMessage = error };
        }

        public static Result Write(string path, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return Result.Failure("文件路径不能为空");
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, content, Encoding.UTF8);
                return Result.Success();
            }
            catch (Exception ex) { return Result.Failure("写入文件失败：" + ex.Message); }
        }

        public static Result Read(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return Result.Failure("文件路径不能为空");
                if (!File.Exists(path)) return Result.Failure("文件不存在");
                string content = File.ReadAllText(path, Encoding.UTF8);
                return Result.Success(content);
            }
            catch (Exception ex) { return Result.Failure("读取文件失败：" + ex.Message); }
        }

        public static Result Delete(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return Result.Failure("文件路径不能为空");
                if (File.Exists(path)) File.Delete(path);
                return Result.Success();
            }
            catch (Exception ex) { return Result.Failure("删除文件失败：" + ex.Message); }
        }

        public static Result FileExistsSafe(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return Result.Failure("文件路径不能为空");
                return Result.Success(File.Exists(path) ? "True" : "False");
            }
            catch (Exception ex) { return Result.Failure("检查文件存在性失败：" + ex.Message); }
        }

        /// <summary>
        /// 复制整个目录到目标目录，如果目标已存在则合并（同名文件覆盖）。
        /// </summary>
        /// <param name="sourceDir">源目录路径</param>
        /// <param name="destDir">目标目录路径</param>
        /// <param name="overwrite">是否覆盖同名文件，默认为 true</param>
        public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
        {
            // 如果源目录不存在，直接返回
            if (!Directory.Exists(sourceDir))
                return;

            // 创建目标目录（如果不存在）
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            // 1. 复制所有文件
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFile = Path.Combine(destDir, fileName);
                // 复制文件（overwrite参数控制是否覆盖）
                File.Copy(filePath, destFile, overwrite);
            }

            // 2. 递归处理所有子目录
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir, overwrite); // 递归调用
            }
        }
    }
}