using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class Translator
    {
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static readonly char[] _exemptions = "０１２３４５６７８９ＸＹＺ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.".ToArray();

        private static StreamWriter _writer;

        public static void Load(string path, string assetsPath)
        {
            path = path.Substring(0, path.Length - 5);

            if (File.Exists(path + ".in.txt"))
                using (var keys = new StreamReader(File.OpenRead(path + ".in.txt")))
                using (var values = new StreamReader(File.OpenRead(path + ".out.txt")))
                    while (!keys.EndOfStream && !values.EndOfStream)
                        _strings[keys.ReadLine()] = values.ReadLine();

            _writer = new StreamWriter(Path.Combine(assetsPath, Path.GetFileName(path) + ".in.txt"));
        }

        public static void Save(string keysPath)
        {
            using (var writer = new StreamWriter(File.OpenWrite(keysPath)))
                foreach (var key in _strings.Keys
                    .Select(k => k.Intersect(_exemptions).Any() ? new string(k.Except(_exemptions).ToArray()) : k)
                    .OrderBy(k => k)
                    .Distinct()
                    .Where(k => k.Any(c => c >= '\u303f')))
                {
                    writer.WriteLine(key);
                }
        }

        public static string ReadString(BinaryReader source)
        {
            var s = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));
            return Translate(s);
        }

        public static string ReadString2(BinaryReader source)
        {
            var s = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadByte() + 3)).TrimStart('\0');
            return Translate(s);
        }

        public static string Translate(string s)
        {
            var word = s.TrimEnd(_exemptions).Normalize();

            if (_strings.TryGetValue(word, out var translation))
                s = translation + new string(s.Substring(word.Length)
                    .Select(c => c >= '０' && c <= '９' ? (char)(c + '0' - '０') : c)
                    .ToArray());
            else
            {
                _strings.Add(word, word);

                if (word.Any(c => c >= '\u303f'))
                {
                    _writer.WriteLine(word);
                    _writer.Flush();
                }
            }

            return s;
        }
    }
}