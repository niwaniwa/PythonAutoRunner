using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PythonAutoExecuter.PythonAutoExcuter
{
    public class KadaiExcuter
    {

        private const string Config = "data.yaml";

        public string SourceDirectoryPath
        {
            get;
            private set;
        }
        
        public string RunDirectoryPath
        {
            get;
            private set;
        }
        
        public string Prefix
        {
            get;
            private set;
        }
        
        public string FunctionCode { get; set; }
        public string Answer { get; set; }
        public string Library { get; set; }
        public string LibraryFileName { get; set; }
        
        public KadaiExcuter(string directoryPath, string prefix, string runFunctionCode, string answer, string library, string libraryFileName)
        {
            SourceDirectoryPath = directoryPath;
            RunDirectoryPath = Path.Combine(SourceDirectoryPath, "./work");
            if (!Directory.Exists(RunDirectoryPath))
                Directory.CreateDirectory(RunDirectoryPath);
            Prefix = prefix;
            FunctionCode = runFunctionCode;
            Answer = answer;
            Library = library;
            LibraryFileName = libraryFileName;
        }

        public void Initialize()
        {
            ChangeFileName();
            CopyLibrary();
        }

        private void ChangeFileName()
        {
            // 正規表現のパターン
            string pattern = @"^(?<index>\d+)_(?<id1>\d+)_(?<name>.+)_Q(?<qid>\d+)_(?<kadai>.+?)_(?<id3>\d+)$";
            
            foreach (var filePath in Directory.GetFiles(SourceDirectoryPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var match = Regex.Match(fileName, pattern);
                
                if (match.Success)
                {
                    var newFileName = Prefix + "_" + fileName.Split('_')[6] + Path.GetExtension(filePath);
                    var newFilePath = Path.Combine(RunDirectoryPath, newFileName);

                    if (File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }
                    File.Copy(filePath, newFilePath);
                    Console.WriteLine($"Copied {filePath} to {newFilePath}");

                    modifyScript(newFilePath, FunctionCode);
                }
            }
        }

        private void CopyLibrary()
        {
            File.Copy(Library, Path.Combine(RunDirectoryPath, LibraryFileName));
        }

        private void modifyScript(string filePath, string code)
        {
            // 1. Pythonファイルを読み込む
            var pythonCode = File.ReadAllText(filePath);

            // 2. ファイルの末尾に関数の呼び出しを追加する
            pythonCode += code;

            // 3. ファイルを再度保存する
            File.WriteAllText(filePath, pythonCode);
        }

        private void ReadSettings()
        {
            
        }
        
        public delegate bool KadaiDelegate(params object[] args);

        public bool CheckNameExist(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string firstLine = sr.ReadLine();
                return firstLine.Contains("#");
            }
        }

        public void Run(IKadaiDelegate inputDelegate, IKadaiDelegate outputDelegate, IKadaiDelegate completeDelegate)
        {
            string outputDirectory = @"./result/"; // 間違った出力を保存するディレクトリ
            string outputDirectoryPath = Path.Combine(RunDirectoryPath, outputDirectory);
            Dictionary<int, int> results = new Dictionary<int, int>();

            string pattern = @"^(?<kadai>.+?)_(?<id>\d+)$";
            
            if (!Directory.Exists(outputDirectoryPath))
                Directory.CreateDirectory(outputDirectoryPath);

            foreach (var filePath in Directory.GetFiles(RunDirectoryPath))
            {
                var file = Path.GetFileNameWithoutExtension(filePath);
                var match = Regex.Match(file, pattern);
                
                if (!match.Success)
                { 
                    continue;
                }
                
                int fileNumber = int.Parse(file.Split('_')[1]);
                Console.WriteLine($"Executing {fileNumber}...");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = filePath, 
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                
                if (inputDelegate != null) inputDelegate.Run(process); // 入力
                
                int timeout = 5000;
                
                if (!process.WaitForExit(timeout))
                {
                    // タイムアウト後にプロセスを強制終了
                    process.Kill();
                    Console.WriteLine($"Process was terminated due to a timeout of {timeout} milliseconds.");
                    results[fileNumber] = -1;
                    File.WriteAllText(Path.Combine(outputDirectoryPath, $"{fileNumber}.txt"), "正常に動作しません。コードを確認してください。");
                    continue;
                }

                
                string output = process.StandardOutput.ReadToEnd(); // 出力
                string error = process.StandardError.ReadToEnd(); // エラー
                
                var isSuccess = outputDelegate.Run(output);

                if (!string.IsNullOrEmpty(error))
                {
                    error = error.Replace(@"C:\Users\konya\Develop\Programs\PythonAutoExecuter\students_data\", @"C:\\..\");
                    Console.WriteLine($"Error executing {fileNumber}: {error}\n");
                    File.WriteAllText(Path.Combine(outputDirectoryPath, $"{fileNumber}.txt"), $"正常に実行できませんでした。コードを確認してください。 {error}");
                    results[fileNumber] = -1;
                    continue;
                }

                if (!isSuccess)
                {
                    File.WriteAllText(Path.Combine(outputDirectoryPath, $"{fileNumber}.txt"), $"{output}");
                }
                
                
                results[fileNumber] = isSuccess ? (CheckNameExist(filePath) ? 5 : 2) : 0;
            }

            Console.WriteLine("End Process");
            // 結果をCSVに保存
            File.WriteAllLines(Path.Combine(RunDirectoryPath, "result.csv"), results.Select(r => $"{r.Key},{r.Value}").ToArray());
        }

    }
}