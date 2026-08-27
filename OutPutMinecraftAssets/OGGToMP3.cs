using NVorbis;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace New_Launcher.OutPutMinecraftAssets
{
    internal class OGGToMP3
    {
        // 注意：这里使用与 Program.cs 加载一致的名称
        private const string LameDll = "libmp3lame.32.dll";

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr lame_init();

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_set_in_samplerate(IntPtr gfp, int rate);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_set_num_channels(IntPtr gfp, int channels);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_set_brate(IntPtr gfp, int bitrate);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_set_quality(IntPtr gfp, int quality);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_init_params(IntPtr gfp);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_encode_buffer_interleaved(
            IntPtr gfp,
            short[] pcm,
            int num_samples,
            byte[] mp3buf,
            int mp3buf_size);

        // ====== 新增：单声道编码函数 ======
        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_encode_buffer(
            IntPtr gfp,
            short[] pcm_l,
            short[] pcm_r,
            int num_samples,
            byte[] mp3buf,
            int mp3buf_size);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lame_encode_flush(
            IntPtr gfp,
            byte[] mp3buf,
            int mp3buf_size);

        [DllImport(LameDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void lame_close(IntPtr gfp);

        /// <summary>
        /// 将 OGG 文件转换为 MP3（使用 LAME 编码器）
        /// </summary>
        public static void ConvertOggToMp3(string oggFilePath, string mp3FilePath, int bitRate = 192)
        {
            if (string.IsNullOrEmpty(oggFilePath)) throw new ArgumentNullException(nameof(oggFilePath));
            if (string.IsNullOrEmpty(mp3FilePath)) throw new ArgumentNullException(nameof(mp3FilePath));

            using (var vorbis = new VorbisReader(oggFilePath))
            {
                int sampleRate = vorbis.SampleRate;
                int channels = vorbis.Channels;

                // 参数合法性检查
                if (channels != 1 && channels != 2)
                    throw new NotSupportedException($"不支持的声道数：{channels}。LAME 仅支持单声道和立体声。");

                IntPtr lame = lame_init();
                if (lame == IntPtr.Zero)
                    throw new Exception("LAME 初始化失败。请确认 libmp3lame.32.dll 已正确加载。");

                try
                {
                    lame_set_in_samplerate(lame, sampleRate);
                    lame_set_num_channels(lame, channels);
                    lame_set_brate(lame, bitRate);
                    lame_set_quality(lame, 2);
                    lame_init_params(lame);

                    int pcmSamples = 4096 * channels;
                    short[] pcmBuffer = new short[pcmSamples];
                    float[] floatBuffer = new float[pcmSamples];
                    byte[] mp3Buffer = new byte[pcmSamples * 4 + 7200];

                    using (var fs = File.Create(mp3FilePath))
                    {
                        int samplesRead;
                        while ((samplesRead = vorbis.ReadSamples(floatBuffer, 0, pcmSamples)) > 0)
                        {
                            // float -> short 转换
                            for (int i = 0; i < samplesRead; i++)
                            {
                                float sample = floatBuffer[i];
                                if (sample > 1.0f) sample = 1.0f;
                                if (sample < -1.0f) sample = -1.0f;
                                pcmBuffer[i] = (short)(sample * 32767.0f);
                            }

                            // ====== 关键修改：根据声道数选择编码函数 ======
                            int bytesEncoded;
                            if (channels == 2)
                            {
                                // 立体声：使用交错编码
                                bytesEncoded = lame_encode_buffer_interleaved(
                                    lame,
                                    pcmBuffer,
                                    samplesRead / channels,
                                    mp3Buffer,
                                    mp3Buffer.Length);
                            }
                            else // channels == 1
                            {
                                // 单声道：使用标准编码，左右声道传相同指针
                                bytesEncoded = lame_encode_buffer(
                                    lame,
                                    pcmBuffer,
                                    pcmBuffer,
                                    samplesRead,
                                    mp3Buffer,
                                    mp3Buffer.Length);
                            }

                            if (bytesEncoded < 0)
                                throw new Exception($"LAME 编码错误 (返回码: {bytesEncoded})");
                            if (bytesEncoded > 0)
                                fs.Write(mp3Buffer, 0, bytesEncoded);
                        }

                        // 刷新编码器
                        int flushBytes = lame_encode_flush(lame, mp3Buffer, mp3Buffer.Length);
                        if (flushBytes < 0)
                            throw new Exception($"LAME 刷新错误 (返回码: {flushBytes})");
                        if (flushBytes > 0)
                            fs.Write(mp3Buffer, 0, flushBytes);
                    }

                    // 校验输出文件
                    if (new FileInfo(mp3FilePath).Length == 0)
                        throw new Exception("生成的 MP3 文件为空，转换失败。");
                }
                finally
                {
                    lame_close(lame);
                }
            }
        }
    }
}