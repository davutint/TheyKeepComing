using System;
using System.IO;
using System.Text;

namespace DeadWalls
{
    /// <summary>
    /// Run/meta JSON dosyalari icin ayni-volume temp + replace yazimi. Temp dosya diske
    /// flush edilmeden authoritative dosyaya gecilmez; ilk yazim rename ile atomiklesir.
    /// </summary>
    internal static class AtomicJsonFile
    {
        private const string TempSuffix = ".tmp";

        public static bool TryWrite(string path, string json, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "Path is empty.";
                return false;
            }

            string tempPath = path + TempSuffix;
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                byte[] bytes = new UTF8Encoding(false).GetBytes(json ?? string.Empty);
                using (var stream = new FileStream(
                           tempPath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                try
                {
                    TryDeletePath(tempPath);
                }
                catch
                {
                    // Authoritative sonuc zaten belirlendi; temp cleanup best-effort'tur.
                }
            }
        }

        /// <summary>
        /// Ilk authoritative yazimdan hemen once process kapanmissa kalan tam temp dosyayi
        /// sahiplenir. Authoritative dosya zaten varsa temp eski/yarim kabul edilip silinir.
        /// </summary>
        public static bool TryRecoverOrphanedTemp(string path, out string error)
        {
            error = null;
            string tempPath = path + TempSuffix;
            try
            {
                if (!File.Exists(tempPath))
                    return true;

                if (File.Exists(path))
                {
                    File.Delete(tempPath);
                    return true;
                }

                File.Move(tempPath, path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryDelete(string path, out string error)
        {
            error = null;
            try
            {
                TryDeletePath(path);
                TryDeletePath(path + TempSuffix);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void TryDeletePath(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
