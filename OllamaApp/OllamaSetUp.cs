using OllamaSharp;

namespace OllamaApp;

public class OllamaSetUp
{
    //Ollama
    public static string uri = "http://localhost:11434";
    public static Chat chat { get; set; }

    public Chat? setUp(string modelName, FileWorker fileWorker)
    {
        var ollama = new OllamaApiClient(uri);
        ollama.SelectedModel = modelName;
        
        //Путь к System промпту (ВАЖНО)
        var sysPromptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system prompt.txt");

        var sysPrompt = fileWorker.readFile(sysPromptPath);

        chat = new Chat(ollama, sysPrompt);

        //Первые несколько запросов после установки System промпта не отрабатывают, поэтому это лишний запрос, чтобы System промпт обработался
        chat.Send("Testing if everything works. Do not forget to read your system prompt");
        
        return chat;
    }
}