using OllamaApp;

//Ollama должна быть запущена перед запуском
FileWorker fileWorker = new FileWorker();
OllamaSetUp setup = new OllamaSetUp();

var chat = setup.setUp("codestral-formulas", fileWorker);

if (chat != null)
    Console.WriteLine("Started successfully");

var count = 0;
//Пути к запросам и результатам. В Release папке проекта есть примеры
var promptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompts");
var resultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultFormulas");

foreach (var file in Directory.EnumerateFiles(promptPath))
{
    var doneFiles = Directory.EnumerateFiles(resultPath);
    
    var resultMessage = String.Empty;
    
    var fileName = Path.GetFileName(file);

    //Проверка на то, что файл-результат уже существует. Игнорируем 0.txt, его нужно обработать ВСЕГДА, потому что для первого запроса игнорируется System промпт
    if (doneFiles.FirstOrDefault(df => Path.GetFileName(df) == fileName.Substring(0, fileName.Length - 4) + "Formula.txt") != null && fileName != "0.txt")
    {
        Console.WriteLine("Skipping file [" + fileName + "]");
    }
    else
    {
        Console.WriteLine($"Sending prompt from [{file}]");
        
        var prompt = fileWorker.readFile(file);

        //Читаем сообщение. Ответ отправляется частями, поэтому так
        await foreach (var answerToken in chat.Send(prompt))
        {
            resultMessage += answerToken;
        }

        var path = fileWorker.formPath(fileName.Substring(0, fileName.Length - 4) + "Formula");


        Console.WriteLine("Writing file...");
        fileWorker.writeFile(path, resultMessage.Substring(1));
        count++;
        Console.WriteLine($"File [{path}] wrote successfully");
    }
}