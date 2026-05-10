using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using FileUtil;
using LogUtil;

namespace ProjectServer
{

    class Server
    {
        private readonly HttpListener _listener;
        
        private readonly string hostUrl = "http://localhost:5182/";

        private List<Thread> _threads;

        private bool ShutDownRequested = false;

        private SemaphoreSlim _sem, _reqSem;

        private List<HttpListenerContext> _requests;

        private Thread _listenerThread;

        private Thread _shutDownThread;

        private readonly Logger _logger;

        public Server() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(hostUrl);
            _threads = new List<Thread>(10);
            _sem = new SemaphoreSlim(100, 100);
            _reqSem = new SemaphoreSlim(0);
            _requests = new List<HttpListenerContext>();
            _logger = new Logger();

            _listenerThread = new Thread(() => {
                _listener.Start();
                
                while (!ShutDownRequested) {
                    HttpListenerContext context = _listener.GetContext();
                    _requests.Add(context);
                    _reqSem.Release();
                    _logger.Log($"[ServerListener] [{DateTime.Now}] Heard a request");
                }
            });

            _shutDownThread = new Thread(() => {
                GracefulShutdown();
            });
        }

        public void Start() {
            _logger.Log($"[Server] [{DateTime.Now}] Server is up!");

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
                _logger.Log($"[Server] [{DateTime.Now}] Waited for _sem");

                _reqSem.Wait();
                _logger.Log($"[Server] [{DateTime.Now}] Waited for _reqSem");

                if (_requests[0].Request.HttpMethod != "GET") {
                    _logger.Log($"[Server] [{DateTime.Now}] Discarded a non-get http request...");
                    continue;
                }

                thread.Start(_requests[0]);
                _requests.RemoveAt(0);

            }

            _shutDownThread.Join();
        }

        private void RequestHandle(object? context) {
            if (context == null) {
                return;
            }
            HttpListenerContext c = (HttpListenerContext)context;
            HttpListenerRequest request = c.Request;
            HttpListenerResponse response = c.Response;
            var url = request.Url.OriginalString;
            int offset = hostUrl.Length;
            var fileName = url.Substring(offset);

            if (fileName == null) {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("{'result': 'File not specified'}");
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            else {
                var worker = new FileWorker();
                worker.GetAvgWordLen(fileName, out string result);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes($"{{'result': {result}}}");
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            _sem.Release();
        }

        private void GracefulShutdown() {
            var waitForExit = new ManualResetEventSlim(false);
            
            PosixSignalRegistration.Create(PosixSignal.SIGINT, context => {
                _logger.Log($"\n[Server] [{DateTime.Now}] SIGINT called");
                _logger.Log($"[Server] [{DateTime.Now}] Shutting down gracefuly...");

                ShutDownRequested = true;

                _listenerThread.Join(100);

                foreach(var request in _requests) {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY>Server shut down</BODY></HTML>");
                    System.IO.Stream output = request.Response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
       
                foreach (var thread in _threads) {
                    try {
                        thread.Join(5000);
                    }
                    catch(Exception ex) {
                        _logger.Log($"[Server] [{DateTime.Now}] Error: Thread unjoinable - {ex.Message}");
                    }
                }

                waitForExit.Set();
            });

            waitForExit.Wait();
        }
    }
}