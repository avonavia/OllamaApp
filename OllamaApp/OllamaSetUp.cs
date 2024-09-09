using OllamaSharp;

namespace OllamaApp;

public class OllamaSetUp
{
    //Ollama
    public static string uri = "http://51.250.7.144:11434";
    public static Chat chat { get; set; }

    public Chat? setUp(string modelName, FileWorker fileWorker)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(uri);
        client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        
        var ollama = new OllamaApiClient(client);
        ollama.SelectedModel = modelName;

        chat = new Chat(ollama);

        //Первые несколько запросов после установки System промпта не отрабатывают, поэтому это лишний запрос, чтобы System промпт обработался
        chat.Send("Testing if everything works. Do not forget to read your system prompt");
        
        return chat;
    }
}