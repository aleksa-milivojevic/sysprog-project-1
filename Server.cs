using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using FileUtil;

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

        public Server() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(hostUrl);
            _threads = new List<Thread>();
            _sem = new SemaphoreSlim(100, 100);
            _reqSem = new SemaphoreSlim(0);
            _requests = new List<HttpListenerContext>();

            _listenerThread = new Thread(() => {
                _listener.Start();
                
                while (!ShutDownRequested) {
                    HttpListenerContext context = _listener.GetContext();
                    _requests.Add(context);
                    _reqSem.Release();
                    Console.WriteLine("[Listener] Heard a request");
                }
            });

            _shutDownThread = new Thread(() => {
                GracefulShutdown();
            });
        }

        // dodaj ogranicenje za broj istovremenih thread-ova
        public void Start() {
            Console.WriteLine("[Server] Server is up");

            _listenerThread.Start();
            _shutDownThread.Start();

            while(!ShutDownRequested) {
                _sem.Wait();
                Console.WriteLine("[Main Thread] Waited for _sem");

                _reqSem.Wait();
                Console.WriteLine("[Main Thread] Waited for _reqSem");
                HttpListenerRequest request = _requests[0].Request;
                HttpListenerResponse response = _requests[0].Response;
                _requests.RemoveAt(0);

                if (request.HttpMethod != "GET") {
                    Console.WriteLine("[Server] Discarded a non-get http request...");
                    continue;
                }

                Thread thread = new Thread(() => {
                    Console.WriteLine("[Thread] Created");
                    RequestHandle(request, response);
                });

                _threads.Add(thread);
                thread.Start();

            }

            _shutDownThread.Join();
        }

        private void RequestHandle(HttpListenerRequest request, HttpListenerResponse response) {
            var url = request.Url.OriginalString;
            int offset = hostUrl.Length;
            var fileName = url.Substring(offset);

            if (fileName == null) {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY>No file specified</BODY></HTML>");
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            else {
                var worker = new FileWorker();
                worker.GetAvgWordLen(fileName, out string result);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY> " + result + "</BODY></HTML>");
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            _sem.Release();
        }

        private void GracefulShutdown() {
            var waitForExit = new ManualResetEventSlim(false);
            
            PosixSignalRegistration.Create(PosixSignal.SIGINT, context => {
                Console.WriteLine("\n[Server] SIGINT called");
                Console.WriteLine("[Server] Shutting down gracefuly...");

                ShutDownRequested = true;

                _listenerThread.Join(100);

                foreach(var request in _requests) {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY>Server shut down</BODY></HTML>");
                    System.IO.Stream output = request.Response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
       
                foreach (var thread in _threads) {
                    thread.Join(5000);
                }

                waitForExit.Set();
            });

            waitForExit.Wait();
        }
    }
}