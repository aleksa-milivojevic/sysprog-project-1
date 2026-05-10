using Utility;
using Memory;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

namespace Services
{
    class ApiService {

        private readonly HttpListener _listener;
        
        private readonly string _hostUrl = "http://localhost:5134/";

        private List<Thread> _threads;

        private bool ShutDownRequested = false;

        private SemaphoreSlim _sem, _reqSem;

        private List<HttpListenerContext> _requests;

        private Thread _listenerThread;

        private Thread _shutDownThread;

        private readonly Logger _logger;

        private readonly CacheMemory _cache;

        private readonly object _cacheLock;

        private HttpClient _client;

        private string _baseUrl = "http://localhost:5182/";

        public ApiService() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_hostUrl);
            _threads = new List<Thread>(10);
            _sem = new SemaphoreSlim(100, 100);
            _reqSem = new SemaphoreSlim(0);
            _requests = new List<HttpListenerContext>();
            _logger = new Logger();
            _cache = new CacheMemory();
            _cacheLock = new object();
            _client = new HttpClient();

            _listenerThread = new Thread(() => {
                _listener.Start();
                
                while (!ShutDownRequested) {
                    HttpListenerContext context = _listener.GetContext();
                    _requests.Add(context);
                    _reqSem.Release();
                    _logger.Log($"[Listener] [{DateTime.Now}] Heard a request");
                }
            });

            _shutDownThread = new Thread(() => {
                GracefulShutdown();
            });
        }

        public void Start() {
            _logger.Log($"[ApiService] [{DateTime.Now}] Started up!");

            _listenerThread.Start();
            _shutDownThread.Start();

            int i;
            for (i = 0; i < _threads.Capacity; i++) {
                _threads.Add(new Thread(new ParameterizedThreadStart(this.RequestHandle)));
            }
            while(!ShutDownRequested) {

                Thread? thread = null;
                for(i = 0; i < _threads.Count; i++) {
                    if (!_threads[i].IsAlive) {
                        _threads[i] = new Thread(new ParameterizedThreadStart(this.RequestHandle));
                        thread = _threads[i];
                        break;
                    }
                }
                if (thread == null)  {
                    Thread.Sleep(100);
                    continue;
                }
                _sem.Wait();

                _reqSem.Wait();

                if (_requests[0].Request.HttpMethod != "GET") {
                    _logger.Log($"[ApiService] [{DateTime.Now}] Discarded a non-get http request...");
                    continue;
                }
                

                thread.Start(_requests[0]);
                _requests.RemoveAt(0);

            }

            _shutDownThread.Join();
        }

        private void Respond(string file, JObject result, HttpListenerResponse response) {
            FileUtility writer = new FileUtility();
            byte[] buffer;
            if (!result.HasValues) {
                buffer = System.Text.Encoding.UTF8.GetBytes($"Something went wrong");
            }
            else {
                buffer = System.Text.Encoding.UTF8.GetBytes($"Avarage word length: {result["result"]}");
            }
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            writer.Write(file, result);
        }

        public void RequestHandle(object? context) {
            if (context == null) {
                return;
            }
            HttpListenerContext c = (HttpListenerContext)context;
            HttpListenerRequest request = c.Request;
            HttpListenerResponse response = c.Response;
            var requestUrl = request.Url.OriginalString;
            int offset = _hostUrl.Length;
            var file = requestUrl.Substring(offset);

            if (file == null) {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("File not specified");
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                return;
            }
            
            _logger.Log($"[Thread] [{DateTime.Now}] started for file: '{file}'");
            JObject cacheHit;

            if (_cache.Get(file, out cacheHit)) {
                _logger.Log($"[Thread] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                Respond(file, cacheHit, response);
                return;
            }

            Monitor.Enter(_cacheLock);

            if (_cache.Get(file, out cacheHit)) {
                _logger.Log($"[Thread] [{DateTime.Now}] CACHE HIT  -> '{file}' (taken from cache)");
                Respond(file, cacheHit, response);
                Monitor.Exit(_cacheLock);
                return;
            }

            _logger.Log($"[Thread] [{DateTime.Now}] CACHE MISS -> '{file}' (sending API request)");

            string url = $"{_baseUrl}{file}";

            try {
                HttpResponseMessage serverResponse = _client.GetAsync(url).Result;
                string body = serverResponse.Content.ReadAsStringAsync().Result;
                JObject result = JObject.Parse(body);

                _cache.Set(file, result);
                _logger.Log($"[Thread] [{DateTime.Now}] Result stored in cache for query: '{file}'");

                Respond(file, result, response);
            }
            catch (Exception ex) {
                _logger.Log($"[Thread] [{DateTime.Now}] Error while fetching file '{file}': {ex.Message}");
                Respond(file, new JObject(), response);
            }
            finally {
                Monitor.Exit(_cacheLock);
            }

            _sem.Release();
        }

        public void CheckCache() {
            StringBuilder log = new StringBuilder();
            log.Append($"\n[ApiService] [{DateTime.Now}] Currently in cache: {_cache.Count()} file(s).\n");
            foreach (var key in _cache.Keys()) {
                log.Append($"  -> '{key}'\n");
            }
            _logger.Log(log.ToString());
        }
        
        private void GracefulShutdown() {
            var waitForExit = new ManualResetEventSlim(false);
            
            PosixSignalRegistration.Create(PosixSignal.SIGINT, context => {
                _logger.Log($"\n[ApiService] [{DateTime.Now}] SIGINT called");
                Console.WriteLine($"[ApiService] [{DateTime.Now}] Shutting down gracefuly...");

                ShutDownRequested = true;

                _listenerThread.Join(100);

                foreach(var request in _requests) {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Server shut down");
                    System.IO.Stream output = request.Response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
       
                foreach (var thread in _threads) {
                    try {
                        thread.Join(5000);
                    }
                    catch(Exception ex) {
                        _logger.Log($"[ApiService] [{DateTime.Now}] Error: Thread unjoinable - {ex.Message}");
                    }
                }

                waitForExit.Set();
            });

            waitForExit.Wait();
        }
    }
}