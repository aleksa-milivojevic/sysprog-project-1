using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using FileUtil;

#define maxThreads 100

namespace ProjectServer
{
    class Server
    {
        private readonly HttpListener _listener;

        //private readonly FileWorker _worker;
        
        private readonly string hostUrl = "http://localhost:5182/";

        private List<Thread> _threads;

        private bool ShutDownRequested = false;

        private SemaphoreSlim _sem;

        private List<HttpListenerContext> _requests;

        public Server() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(hostUrl);
            //_worker = new FileWorker();
            _threads = new List<Thread>();
            _sem = new SemaphoreSlim(0, maxThreads);
            _requests = new List<HttpListenerContext>();
        }

        // dodaj ogranicenje za broj istovremenih thread-ova
        public void Start() {

            Thread shutDownThread = new Thread(() => {
                GracefulShutdown();
            }).Start();

            Thread listenerThread = new Thread(() => {
                _listener.Start();
                
                while (!ShutDownRequested) {
                    HttpListenerContext context = _listener.GetContext();
                    _requests.add(context);
                }
            }).Start();

            while(!ShutDownRequested) {
                _sem.Wait();

                HttpListenerRequest request = _requests[0].Request;
                HttpListenerResponse response = _requests[0].Response;
                _requests.RemoveAt(0);

                if (request.HttpMethod != "GET") {
                    Console.WriteLine("[Server] Discarded a non-get http request...");
                    continue;
                }

                Thread thread = new Thread(() => {
                    RequestHandle(request, response);
                });

                _threads.Add(thread);
                thread.Start();

                ShutDownRequested = true;
            }

            shutDownThread.Join();
        }

        private void RequestHandle(HttpListenerRequest request, HttpListenerResponse response) {
            var url = request.Url.OriginalString;
            int offset = hostUrl.Length;
            var fileName = url.Substring(offset);

            if (fileName == null) {
                Console.WriteLine("[Server] File name not specified...");
                return;
            }

            var worker = new FileWorker();
            worker.GetAvgWordLen(fileName, out double result);
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY> " + result + "</BODY></HTML>");
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            _sem.Release();
        }

        private GracefulShutdown() {
            var waitForExit = new ManualResetEventSlim(false);
            
            PosixSignalRegistration.Create(PosixSignal.SIGINT => {
                Console.WriteLine("[Server] SIGINT called");
                Console.WriteLine("[Server] Shutting down...");

                ShutDownRequested = true;

                _listenerThread.Join();

                foreach(var request in _requests) {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<HTML><BODY>Server shut down</BODY></HTML>");
                    System.IO.Stream output = request.response.OutputStream;
                    ouput.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
       
                foreach (var thread in _threads) {
                    thread.Join();
                }

            });

            waitForExit.Wait();
        }
    }
}