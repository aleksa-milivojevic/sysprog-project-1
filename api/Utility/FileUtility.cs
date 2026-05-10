using Newtonsoft.Json.Linq;

namespace Utility
{
    public class FileUtility {

        private readonly Logger _logger;

        private readonly object _writeLock;

        public FileUtility() {
            _logger = new Logger();
            _writeLock = new object();
        }

        public void WriteAll(List<string> files, List<JObject> results) {
            string path = $"query-results.txt";
            for (int i = 0; i < files.Count; i++) {
                if (!results[i].HasValues) {
                    _logger.Log($"[FileUtility] [{DateTime.Now}] Result null for file {files[i]}");
                    continue;
                }

                Monitor.Enter(_writeLock);
                string content = $"\nFile: {files[i]}\n"  +
                                 $"Result: {results[i]["result"]}\n";
                try {
                    File.AppendAllText(path, content);
                }
                catch(Exception ex) {
                    _logger.Log($"[FileUtility] [{DateTime.Now}] Error while writing to file: {ex.Message}");
                }
                finally {
                    Monitor.Exit(_writeLock);
                }

                _logger.Log($"[FileUtility] [{DateTime.Now}] Results for file {files[i]} saved");
            }

            _logger.Log($"[FileUtility] [{DateTime.Now}] All results saved");
        }
    }
}