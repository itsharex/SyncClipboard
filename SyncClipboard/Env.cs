using System.IO;

namespace SyncClipboard
{
    internal static class Env
    {
        public const string VERSION = "1.3.8";
        internal static readonly string Directory = System.Windows.Forms.Application.StartupPath;
        internal static string FullPath(string relativePath)
        {
            return Path.Combine(Directory, relativePath);
        }
    }
}