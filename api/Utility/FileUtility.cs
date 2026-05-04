namespace Utility
{
    public class FileUtility {

        private string _baseUrl;

        private readonly object _writeLock;

        public FileUtility(string baseUrl) {
            _baseUrl = baseUrl;
            _writeLock = object();
        }

        public WriteAll(List<string> files, List<JObject> results) {
            for (int i = 0; i < files.Count; i++) {
                if (results[i] == null) {
                    Console.WriteLine($"[FileUtility] [{DateTime.Now}] Result null for file {files[i]}");
                    continue;
                }

                Monitor.Enter(_writeLock);
                string path = $"{_baseUrl}/query-results.txt";
                string content = $"File: {files[i]}\n"  +
                                 $"Result: {results[i][avarage]}";
                try {
                    File.AppendAllText(path, content);
                }
                catch(Exception ex) {
                    Console.WriteLine($"[FileUtility] [{DateTime.Now}] Error while writing to file: {ex.Message}");
                }
                finally {
                    Monitor.Exit(_writeLock);
                }

                Console.WriteLine($"[FileUtility] [{DateTime.Now}] Results for file {files[i]} saved");
            }

            Console.WriteLine($"[FileUtility] [{DateTime.Now}] All results saved");
        }
    }
}