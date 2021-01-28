using System;
using System.Collections.Generic;
using System.IO;

namespace HangAnalyzer
{
    public class Program
    {
        static void Main(string[] args)
        {
            // string[] lines = File.ReadAllLines(@"TestInput.txt");
            string line = Console.ReadLine();
            List<string> lines = new List<string>();
            while (line != null)
                lines.Add(line);
            Analyzer analyzer = new Analyzer(lines.ToArray());
            analyzer.Analyze();
        }
    }
}
