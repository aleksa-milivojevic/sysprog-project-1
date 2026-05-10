using System.Net.Http;
using Utility;
using Services;
using Newtonsoft.Json.Linq;

namespace MainSpace
{
    public class Program
    {
        static void Main(string[] args) {
            HttpClient client = new HttpClient();
            string url = "http://localhost:5182/";

            ApiService service = new ApiService(client, url);

            FileUtility writer = new FileUtility();

            List<string> files = new List<string>();
            for (int i = 1; i <= 10; i++) {
                files.Add($"f{i}.txt");                
            }

            List<JObject> results = service.Fetch(files);

            writer.WriteAll(files, results);

            files.Clear();

            for (int i = 1; i <= 3; i++)
                files.Add($"f{i}.txt");

            results = service.Fetch(files);

            writer.WriteAll(files, results);
            
            service.CheckCache();
        }
    }
}