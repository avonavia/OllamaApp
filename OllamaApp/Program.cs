using System.Text.RegularExpressions;
using OllamaApp;

//Ollama должна быть запущена перед запуском
FileWorker fileWorker = new FileWorker();
OllamaSetUp setup = new OllamaSetUp();
APIWorker apiWorker = new APIWorker();

var codestral_formulas_chat = setup.setUp("codestral-formulas");

if (codestral_formulas_chat != null)
    Console.WriteLine("Started successfully");

var count = 0;

var prompts = new List<KeyValuePair<string, string>?>();

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
    var fileName = promptName.Replace("prompt-", string.Empty).Trim();

    //Проверка на то, что файл-результат уже существует
    if (doneFiles.FirstOrDefault(df => Path.GetFileName(df) == fileName + "-Formula.cs") != null)
    {
        Console.WriteLine("Skipping file [" + fileName + "]");
    }
    else
    {
        var attempt = 1;
        var checkResult = false;
        while (!checkResult && attempt < 6)
        {
            resultMessage = String.Empty;
            Console.WriteLine($"Sending prompt [{promptName}]");

            //Читаем сообщение. Ответ отправляется частями, поэтому так
            await foreach (var answerToken in codestral_formulas_chat.Send(prompt.Value.Value))
            {
                resultMessage += answerToken;
            }

            checkResult = checkPrompt(resultMessage);

            if (!checkResult)
            {
                Console.WriteLine("Regenerating... " + "Attempt: " + attempt);
                attempt++;
            }
        }

        if (attempt < 6)
        {
            var path = fileWorker.formPath(fileName + "-Formula");

            Console.WriteLine("Writing file...");
            var codeToWrite = TrimOutput(resultMessage.Substring(1));
            fileWorker.writeFile(path, codeToWrite);
            count++;
            Console.WriteLine($"File [{path}] wrote successfully");
        }
        else
        {
            Console.WriteLine("Out of attempts");
        }
    }
}

bool checkPrompt(string code)
{
    if (code != String.Empty)
    {
        string patternAssembly = @"\[assembly: FormulaAssembly\(""(?<formulaName>\w+)""\)\]";
        string patternNamespace = @"namespace Sauron\.Formula\.(?<formulaName>\w+)";
        string patternClass = @"public class (?<formulaName>\w+)Formula : IFormulaPlugin";

        var matchAssembly = Regex.Match(code, patternAssembly);
        var matchNamespace = Regex.Match(code, patternNamespace);
        var matchClass = Regex.Match(code, patternClass);

        if (!matchAssembly.Success || !matchNamespace.Success || !matchClass.Success)
        {
            return false;
        }
        
        string formulaName = matchAssembly.Groups["formulaName"].Value.ToLower();
        if (matchNamespace.Groups["formulaName"].Value.ToLower() != formulaName ||
            matchClass.Groups["formulaName"].Value.ToLower() != formulaName)
        {
            return false;
        }
        
        string patternMethod =
            @"public CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>\?> Formula\(TransactionInfo info,\s*IEnumerable<KeyValuePair<string, decimal>>\? tokenPrices = null\)";
        string patternReturn = @"new CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>\?>";

        var matchMethod = Regex.Match(code, patternMethod);
        var matchReturn = Regex.Match(code, patternReturn);

        if (!matchMethod.Success || !matchReturn.Success)
        {
            return false;
        }

        return true;
    }

    return false;
}

string TrimOutput(string originalText)
{
    string trimmedText = "";

    string pattern = @"\[assembly: FormulaAssembly\(""(.*)""\)\]";
    Match match = Regex.Match(originalText, pattern, RegexOptions.Singleline);
    if (match.Success)
    {
        int startIndex = match.Index;
        int endIndex = originalText.LastIndexOf("}");

        if (startIndex >= 0 && endIndex >= 0)
        {
            trimmedText = originalText.Substring(startIndex, endIndex - startIndex + 1);
        }
    }

    return trimmedText;
}