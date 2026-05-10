namespace Utility
{
    public class Logger {

        private string _logFile;

        private readonly object _logLock;

        public Logger() {
            _logLock = new object();
            _logFile = "logs.txt";
        }

        public void Log(string log) {
            Monitor.Enter(_logLock);
            try {
                File.AppendAllText(_logFile, log+"\n");
                Console.WriteLine(log);
            }
            catch(Exception ex) {
                Console.WriteLine($"[Log] [{DateTime.Now}] Error while logging to file: {ex.Message}");
            }
            finally {
                Monitor.Exit(_logLock);
            }
        }
    }
}