namespace OllamaApp;

public class FileWorker
{
    public string formPath(string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultFormulas", fileName + ".cs");
        return path;
    }

    public void writeFile(string path, string text)
    {
        File.WriteAllText(path, text);
    }
}