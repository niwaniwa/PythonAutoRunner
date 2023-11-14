using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using PythonAutoExecuter.PythonAutoExcuter;

namespace PythonAutoExecuter
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            StartProcess();
        }

        public static int StartProcess()
        {
            var config = KadaiDataConfig.GetConfigs();

            if (config.Count == 0)
            {
                Console.WriteLine("何もせずに終わりました... / file not found");
                return 0;
            }

            var group = config.Where(v => !v.IsEnded).ToArray();
            // 何回もしたいならそのまま
            if (group.Length == 0)
            {
                Console.WriteLine("何もせずに終わりました... / already end files...");
                return 0;
            }
            
            var target = group[0];
            
            Console.WriteLine($"{target.Config["Prefix"]} loading...");

            var path =　target.Config["DataSourcePath"];
            var templatePath = Path.Combine(path, target.Config["TemplatePath"]);
            var answerPath = Path.Combine(path, target.Config["AnswerPath"]);
            var libraryFile = Path.Combine(path, target.Config["LibraryFile"]);
            var prefix = target.Config["Prefix"];
            
            var code = File.ReadAllText(templatePath);
            var answer = File.ReadAllText(answerPath);
            
            var kadaiExcuter = new KadaiExcuter(path, prefix, code, answer, libraryFile, target.Config["LibraryFile"]);
            
            
            Func<object[], bool> inputFunc = args => 
            {
                // var process = (Process) input[0];
                //
                // process.StandardInput.WriteLine();
                return true;
            };
            
            Func<object[], bool> outputFunc = args => 
            {
                var answers = answer.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var outputs = ((string)args[0]).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                outputs = outputs.Where(x => x.Length != 0).ToArray();
                
                if (outputs.Length < answers.Length)
                {
                    Console.WriteLine($"outputs.Length < answer.Length, {outputs.Length}: {answers.Length}");
                    return false;
                }

                for (int i = 0; i < answers.Length; i++)
                {
                    if (answers[i].TrimEnd().TrimStart() != outputs[i].TrimEnd())
                    {
                        Console.WriteLine($"   answers[i] != outputs[i] {answers[i]}:{outputs[i]}");
                        return false;
                    };
                }
                return true;
            };
            
            Func<object[], bool> completeFunc = args => 
            {
                // var process = (Process) input[0];
                //
                // process.StandardInput.WriteLine();
                return true;
            };
            
            kadaiExcuter.Initialize();
            kadaiExcuter.Run(inputFunc.ToKadaiDelegate(), outputFunc.ToKadaiDelegate(), completeFunc.ToKadaiDelegate());
            return 0;
        }
        
    }
}