using OllamaApp;

//Ollama должна быть запущена перед запуском
FileWorker fileWorker = new FileWorker();
OllamaSetUp setup = new OllamaSetUp();
APIWorker apiWorker = new APIWorker();

var chat = setup.setUp("codestral-formulas", fileWorker);

if (chat != null)
    Console.WriteLine("Started successfully");

var count = 0;

var firstPrompt = new KeyValuePair<string, string>("0", "\"\"\"Make a C# Formula method for this Solidity contract method:\n\nfunction swapTokensForExactTokens(\n        uint amountOut,\n        uint amountInMax,\n        address[] calldata path,\n        address to,\n        uint deadline\n    ) external virtual override ensure(deadline) returns (uint[] memory amounts) {\n        amounts = UniswapV2Library.getAmountsIn(factory, amountOut, path);\n        require(amounts[0] <= amountInMax, 'UniswapV2Router: EXCESSIVE_INPUT_AMOUNT');\n        TransferHelper.safeTransferFrom(\n            path[0], msg.sender, UniswapV2Library.pairFor(factory, path[0], path[1]), amounts[0]\n        );\n        _swap(amounts, path, to);\n    }\n\nReturn only a C# code. Follow all the code and answer rules in your system prompt\"\"\"");

var prompts = new List<KeyValuePair<string, string>?>();
prompts.Add(firstPrompt);

string promptCount = "";
Console.WriteLine("Enter prompt count: ");

while (!int.TryParse(promptCount, out int _))
{
    promptCount = Console.ReadLine();
}

Console.WriteLine("Enter prompt query: ");
var promptQuery = Console.ReadLine();

var apiPrompts = apiWorker.GetPrompts(Convert.ToInt32(promptCount), promptQuery);

prompts.AddRange(apiPrompts);

var resultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultFormulas");

foreach (var prompt in prompts)
{
    var doneFiles = Directory.EnumerateFiles(resultPath);
    
    var resultMessage = String.Empty;
    
    var promptName = prompt.Value.Key;

    //Проверка на то, что файл-результат уже существует. Игнорируем 0, его нужно обработать ВСЕГДА, потому что для первого запроса игнорируется System промпт
    if (doneFiles.FirstOrDefault(df => Path.GetFileName(df) == promptName + "Formula.txt") != null && promptName != "0")
    {
        Console.WriteLine("Skipping file [" + promptName + "]");
    }
    else
    {
        Console.WriteLine($"Sending prompt [{promptName}]");

        //Читаем сообщение. Ответ отправляется частями, поэтому так
        await foreach (var answerToken in chat.Send(prompt.Value.Value))
        {
            resultMessage += answerToken;
        }

        var path = fileWorker.formPath(promptName + "Formula");


        Console.WriteLine("Writing file...");
        fileWorker.writeFile(path, resultMessage.Substring(1));
        count++;
        Console.WriteLine($"File [{path}] wrote successfully");
    }
}