using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class Translator
    {
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static readonly char[] _exemptions = "０１２３４５６７８９0123456789".ToArray();

        public static void Load(string keysPath, string valuesPath)
        {
            using (var keys = new StreamReader(File.OpenRead(keysPath)))
            using (var values = new StreamReader(File.OpenRead(valuesPath)))
                while (!keys.EndOfStream && !values.EndOfStream)
                    _strings.Add(keys.ReadLine(), values.ReadLine());
        }

        public static void Add(string key)
        {
            if (!_strings.ContainsKey(key))
                _strings.Add(key, key);
        }

        public static void Save(string keysPath)
        {
            using (var writer = new StreamWriter(File.OpenWrite(keysPath)))
                foreach (var key in _strings.Keys
                    .Select(k => new string(k.Except(_exemptions).ToArray()))
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

        public static string Translate(string s)
        {
            var word = s.TrimEnd(_exemptions);

            if (_strings.TryGetValue(word, out var translation))
                s = translation + s.Substring(word.Length);

            return s;
        }
    }
}
