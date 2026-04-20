using System;
using System.IO;

namespace FileUtil
{
    class FileWorker
    {
        private string TrackFile(string fileName) {

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var files = Directory.EnumerateFiles(home, fileName, SearchOption.AllDirectories);
            return files.FirstOrDefault();
        }

        public void GetAvgWordLen(string fileName, out double result) {
            var path = TrackFile(fileName);

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