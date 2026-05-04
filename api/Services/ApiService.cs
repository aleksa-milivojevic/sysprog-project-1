namespace ApiSpace
{
    public class ApiService
    {
        private string _url;
        private HttpClient _client;

        public ApiService(HttpClient client, string url) {
            this.client = client;
            _url = url;
            _client = client;
        }
    }
}