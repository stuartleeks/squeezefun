using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace indexer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                string serverName = args[0]; // TODO error handling
                int serverPort = 9000;

                var server = new SqueezeServer(serverName, serverPort);

                var titles = await server.GetTracksAsync();

                foreach (var title in titles)
                {
                    Console.WriteLine($"Title: {title.Title};\t\t{title.Artist};\t\t{title.Album}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class SqueezeServer
    {
        private readonly string _serverUrl;

        public SqueezeServer(string serverName, int serverPort)
            : this($"http://{serverName}:{serverPort}/jsonrpc.js")
        {
        }
        public SqueezeServer(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        /// <summary>
        /// Gets all titles
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Track>> GetTracksAsync()
        {
            async Task<SqueezeCommandResult<TitlesResult>> GetTracksAsync(int start, int count)
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, _serverUrl)
                {
                    Content = GetCommandContent(1, "", "titles", start, count, "tags:ljyatg"),
                    Headers =
                {
                    Accept = {
                        new MediaTypeWithQualityHeaderValue("application/json")
                    }
                }
                };
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                var titlesResult = JsonConvert.DeserializeObject<SqueezeCommandResult<TitlesResult>>(content);
                return titlesResult;
            }

            var result = await GetTracksAsync(0, 1); // call once to get track count - lazy, should do paging ;-p
            result = await GetTracksAsync(0, result.Result.Count);

            return result.Result.TitlesLoop;
        }

        private HttpContent GetCommandContent(int id, string playerId, params object[] parameters)
        {
            
            var json = JsonConvert.SerializeObject(new
            {
                id,
                method = "slim.request",
                @params = new object[] { playerId, parameters }
            });
            return new StringContent(json)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            };
        }
        class SqueezeCommandResult<TResult>
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("method")]
            public string Method { get; set; }
            [JsonProperty("params")]
            public object[] Parameters { get; set; }
            [JsonProperty("result")]
            public TResult Result { get; set; }
        }
        class TitlesResult
        {
            [JsonProperty("count")]
            public int Count { get; set; }
            [JsonProperty("titles_loop")]
            public Track[] TitlesLoop { get; set; }
        }
    }
    public class Track
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("album")]
        public string Album { get; set; }
        [JsonProperty("year")]
        public int Year { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("tracknum")]
        public int TrackNumber { get; set; }
        [JsonProperty("genre")]
        public string Genre { get; set; }
    }
}
