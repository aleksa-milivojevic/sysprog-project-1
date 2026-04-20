using System.Net;
using FileUtil;

namespace ProjectServer
{
    class Server
    {
        private readonly HttpListener _listener;
        
        private readonly string hostUrl = "http://localhost:5182/";

        private List<Thread> _threads;

        private bool ShutDownRequested = false;

        public Server() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(hostUrl);
            _threads = new List<Thread>();
        }

        public void Start() {
            _listener.Start();

            while(!ShutDownRequested) {
                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod != "GET") {
                    Console.WriteLine("[Server] Discarded a non-get http request...");
                    continue;
                }

                Thread thread = new Thread(() => {
                    //thread funkcija za obradu http request-a
                    RequestHandle(request);
                    Console.WriteLine("Hello world!");
                });

                _threads.Add(thread);
                thread.Start();

                ShutDownRequested = true;
            }

            foreach (var thread in _threads) {
                thread.Join();
            }
        }

        private void RequestHandle(HttpListenerRequest request) {
            var query = request.QueryString;
            foreach(var key in query)
            
            //var worker = new FileWorker();
            //tracker.GetAvgWordLen();
        }
    }
}