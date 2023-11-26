using System.Text;

namespace J18n.Translator;

public class GlobalSettings
{
    public static class Logger
    {
        public const string LogFile = "./Log/Log.txt";
        public const long MaxFileSize = 4 * 1024 * 1024; // 4MB
        public static Encoding Encoding = Encoding.UTF8;
    }
}


