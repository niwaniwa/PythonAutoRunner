using System.Collections.Generic;
using System.IO;

namespace PythonAutoExecuter.PythonAutoExcuter
{
    public class KadaiDataConfig
    {
        public const string dataPath = "configs";

        public string Path { get; private set; }
        public Dictionary<string, string> Config { get; private set; }
        public bool IsEnded { get; private set; }

        public KadaiDataConfig(string path)
        {
            Path = path;
            Config = new Dictionary<string, string>();
        }

        public bool Initialize()
        {
            if (!File.Exists(Path)) return false;

            var lines = File.ReadAllLines(Path);

            if (lines.Length <= 1) return false;
            
            if (!Validate(lines[0], lines[1])) return false;

            foreach (var line in lines)
            {
                var data = line.Split();
                Config.Add(data[0].TrimEnd(':'), data[1]);
            }

            return true;

        }

        private bool Validate(string line1, string line2)
        {
            if (line1.StartsWith("!") && line1.Split()[1] == "KADAI")
            {
                if (line2.StartsWith("!"))
                {
                    if (line1.Split()[0].TrimStart('!') == "isEnded")
                    {
                        IsEnded = bool.Parse(line1.Split()[1]);
                    }
                }
                return true;
            }
            return false;
        }

        public static List<KadaiDataConfig> GetConfigs()
        {
            var list = new List<KadaiDataConfig>();
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                return list;
            }

            var files = Directory.GetFiles(dataPath);
            
            foreach (var file in files)
            {
                var item = new KadaiDataConfig(file);
                if (!item.Initialize()) continue;
                list.Add(item);
            }

            return list;

        }
        
    }
}