using System.Net;
using FileUtil;

namespace ProjectServer
{
    class Server
    {
        private readonly HttpListener _listener;

        //private readonly FileWorker _worker;
        
        private readonly string hostUrl = "http://localhost:5182/";

        private List<Thread> _threads;

        private bool ShutDownRequested = false;

        public Server() {
            _listener = new HttpListener();
            _listener.Prefixes.Add(hostUrl);
            //_worker = new FileWorker();
            _threads = new List<Thread>();
        }

        // dodaj ogranicenje za broj istovremenih thread-ova
        public void Start() {
            _listener.Start();

            while(!ShutDownRequested) {
                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

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

            foreach (var thread in _threads) {
                thread.Join();
            }
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
        }
    }
}