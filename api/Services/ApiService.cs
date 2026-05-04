namespace ApiSpace
{
    public class Services
    {
        private string _baseUrl;

        private HttpClient _client;

        private readonly ConcurrentDictionary<string, JObject> cache = new ConcurrentDictionary<string, JObject>();

        private string _logFile;

        private readonly object _logLock;

        private readonly object _cacheLock;

        public ApiService(HttpClient client, string url, string logFile) {
            _baseUrl = url;
            _client = client;
            _logFile = logFile;
            _logLock = new object();
            _cacheLock = new object();
        }

        private List<JObject> Fetch(List<string> fileNames) {
            List<JObject> results = new List<JObject>[fileNames.Count];
            List<Thread> threads = new List<Thread>[fileNames.Count];

            for (int i = 0; i < fileNames.Count; i++)
            {
                int threadIndex = i;
                string file = fileNames[i];

                threads[threadIndex] = new Thread(() => {
                    Log($"[Thread {index + 1}] [{DateTime.Now}] started for file: '{file}'");

                    if (cache.TryGetValue(file, out JObject cacheHit))
                    {
                        Log($"[Thread {index + 1}] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                        results[threadIndex] = cacheHit;
                        return;
                    }

                    Monitor.Enter(_cacheLock);

                    Log($"[Thread {index + 1}] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                    results[threadIndex] = cacheHit;
                    return;

                    Log($"[Thread {index + 1}] [{DateTime.Now}] CACHE MISS -> '{query}' (sending API request)");

                    string url = $"{_baseUrl}/{file}";

                    try {
                        HttpResponseMessage response = client.GetAsync(url).Result;
                        string body = response.Content.ReadAsStringAsync().Result;
                        JObject result = JObject.Parse(body);

                        cache[file] = result;
                        Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] Result stored in cache for query: '{file}'");

                        results[threadIndex] = result;
                    }
                    catch (Exception ex) {
                        Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] Error while fetching file '{file}': {ex.Message}");
                        results[threadIndex] = null;
                    }
                    finally {
                        Monitor.Exit(_cacheLock);
                    }
                });

                threads[threadIndex].Start();
            }

            foreach (Thread thread in threads) {
                thread.Join();
            }

            Log("\nResults gathered.\n");

            return results;
        }

        public void CheckCache() {
            Console.WriteLine($"\n[Cache] [{DateTime.Now}] Currently in cache: {cache.Count} file(s).");
            foreach (var key in cache.Keys) {
                Console.WriteLine($"  -> '{key}'");
            }
        }

        public Log(string log) {
            Monitor.Enter(_logLock);
            try {
                File.AppendAllText(_logFile, log);
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