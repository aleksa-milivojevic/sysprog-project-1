using System;
using System.IO;

namespace FileUtil
{
    class FileWorker
    {
        private string TrackFile(string fileName) {

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string targetDir = home + "/sysprog/testfiles";
            var files = Directory.EnumerateFiles(targetDir, fileName, SearchOption.AllDirectories);
            return files.FirstOrDefault();
        }

        public void GetAvgWordLen(string fileName, out double result) {
            Console.WriteLine(fileName);
            if (fileName == "") {
                result = -1;
                return;
            }
            var path = TrackFile(fileName);
            if (path == null) {
                result = -1;
                return;
            }

            int count = 0;
            int sum = 0;
            
            string text = File.ReadAllText(path);
            var words = text.Split(" ", System.StringSplitOptions.None);
            foreach(var word in words) {
                sum += word.Length;
                count += 1;
            }
            
            result = (double)sum/(double)count;
        }
    }
}