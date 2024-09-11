using System.Text.Json;

namespace OllamaApp;

public class APIWorker
{
    HttpClient _httpClient;
    string _url = "http://localhost:5078/";
    JsonSerializerOptions serializerOptions;

    public APIWorker()
    {
        _httpClient = new();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    public async Task<List<string>?>? GetPromptsAsync(int count)
    {
        var prompts = new List<string>();
        if (count != null)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_url + "getPrompts/" + count);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                prompts = JsonSerializer.Deserialize<List<string>>(content, serializerOptions);
            }
        }

        return prompts;
    }
    
    public async Task<List<KeyValuePair<string, string>?>>? GetPromptsAsync(int count, string query = "")
    {
        var prompts = new List<KeyValuePair<string, string>?>();
        if (count != null)
        {
            HttpResponseMessage response;
            if (query != "")
            {
                 response = await _httpClient.GetAsync(_url + "getPromptsWithQuery/" + count + "/" + query);
            }
            else
            {
                response = await _httpClient.GetAsync(_url + "getPrompts/" + count);
            }

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                prompts = JsonSerializer.Deserialize<List<KeyValuePair<string, string>?>>(content, serializerOptions);
            }
        }

        return prompts;
    }
    
    public List<KeyValuePair<string, string>?>? GetPrompts(int count, string query = "")
    {
        var result = Task.Run(() => GetPromptsAsync(count, query));
        
        Task.WaitAll(result);
        
        return result.Result;
    }
}