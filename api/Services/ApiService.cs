using Utility;
using Memory;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Services
{
    public class ApiService {
        private string _baseUrl;

        private HttpClient _client;

        private readonly CacheMemory _cache;

        private readonly object _cacheLock;

        private readonly Logger _logger;

        public ApiService(HttpClient client, string url) {
            _baseUrl = url;
            _client = client;
            _cacheLock = new object();
            _cache  = new CacheMemory();
            _logger = new Logger();
        }

        public List<JObject> Fetch(List<string> fileNames) {
            List<JObject> results = new List<JObject>(fileNames.Count);
            List<Thread> threads = new List<Thread>(fileNames.Count);

            for (int i = 0; i < fileNames.Count; i++)
            {
                int threadIndex = i;
                string file = fileNames[i];

                threads.Add(new Thread(() => {
                    _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] started for file: '{file}'");
                    JObject cacheHit;

                    if (_cache.Get(file, out cacheHit)) {
                        _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                        results.Add(cacheHit);
                        return;
                    }

                    Monitor.Enter(_cacheLock);

                    if (_cache.Get(file, out cacheHit)) {
                        _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                        results.Add(cacheHit);
                        return;
                    }

                    _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] CACHE MISS -> '{file}' (sending API request)");

                    string url = $"{_baseUrl}{file}";

                    try {
                        HttpResponseMessage response = _client.GetAsync(url).Result;
                        string body = response.Content.ReadAsStringAsync().Result;
                        JObject result = JObject.Parse(body);

                        _cache.Set(file, result);
                        _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] Result stored in cache for query: '{file}'");

                        results.Add(result);
                    }
                    catch (Exception ex) {
                        _logger.Log($"[Thread {threadIndex + 1}] [{DateTime.Now}] Error while fetching file '{file}': {ex.Message}");
                        results.Add(new JObject());
                    }
                    finally {
                        Monitor.Exit(_cacheLock);
                    }
                }));

                threads[threadIndex].Start();
            }

            foreach (Thread thread in threads) {
                thread.Join();
            }

            return results;
        }

        public void CheckCache() {
            StringBuilder log = new StringBuilder();
            log.Append($"\n[Cache] [{DateTime.Now}] Currently in cache: {_cache.Count()} file(s).\n");
            foreach (var key in _cache.Keys()) {
                log.Append($"  -> '{key}'\n");
            }
            _logger.Log(log.ToString());
        }
    }
}