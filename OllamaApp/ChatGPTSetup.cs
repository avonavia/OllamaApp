﻿using System.Net;
using System.Net.Http.Json;
using OllamaApp.Entities;
using OllamaSharp.Models.Chat;

namespace OllamaApp;

public class ChatGPTSetup
{
    public string endpoint = "https://api.openai.com/v1/chat/completions";
    public List<Message> messages = new();
    public string model = "gpt-3.5-turbo";
    
    public HttpClient SetUp(string key)
    {
        string apiKey = key;
        
        WebProxy proxy = new WebProxy  {
            Address = new Uri("http://sun.astepanov.space:2080"),
            Credentials = new NetworkCredential("AIvanova", "c77mnbvdsAAgfjdsvdahsdr3333")
        };
        
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy,
        };
        
        var httpClient = new HttpClient(httpClientHandler);
        
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        return httpClient;
    }

    public async void SendSystemPromptToGPT(HttpClient httpClient)
    {
        var systemMessage = "You are helping to write a C# code to calculate Ethereum transaction's PNL.\n\nYOU DO NOT RECREATE SOLIDITY CONTRACTS IN C#!!!\nYOU FOLLOW ALL YOUR CODE RULES AND ANSWER RULES\n\nPNL is calculated based on which Solidity contract method the transaction uses.\n\nSolidity contract method looks like this:\n\n function transfer(address _to, uint _value) public onlyPayloadSize(2 * 32) {\n        uint fee = (_value.mul(basisPointsRate)).div(10000);\n        if (fee > maximumFee) {\n            fee = maximumFee;\n        }\n        uint sendAmount = _value.sub(fee);\n        balances[msg.sender] = balances[msg.sender].sub(_value);\n        balances[_to] = balances[_to].add(sendAmount);\n        if (fee > 0) {\n            balances[owner] = balances[owner].add(fee);\n            Transfer(msg.sender, owner, fee);\n        }\n        Transfer(msg.sender, _to, sendAmount);\n    }\n\nPNL calculation in C# for this method ('transfer') looks like this (code comments provide clearance):\n\n//Method's name is stated here\n[assembly: FormulaAssembly(\"transfer\")]\n\nnamespace Sauron.Formula.Transfer;\n\n//Formula must always have IFormulaPlugin interface\npublic class TransferFormula : IFormulaPlugin\n{\n//Formula must return CalculationResult. This IEnumerable contains each address taking part in a transaction and it's PNL\n    public CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?> Formula(TransactionInfo info,\n        IEnumerable<KeyValuePair<string, decimal>>? tokenPrices = null)\n    {\n\t//Token transfer\n        if (info.Transfers != null && tokenPrices != null)\n        {\n            //Get transfers from user\n            var transfer = info.GetTransfersFrom()?.FirstOrDefault();\n\n            var fromPnl = new KeyValuePair<string, BigDecimal>();\n            var toPnl = new KeyValuePair<string, BigDecimal>();\n\n            if (transfer != null)\n            {\n\t\t//Get token price for this transfer\n                var price = tokenPrices.Where(pair => pair.Key == transfer.TokenInfo.Symbol);\n\n\t\t//Calculate sending user's PNL\n                fromPnl = new KeyValuePair<string, BigDecimal>(info.FromAddressHash,\n                    -(transfer.Amount * Convert.ToDecimal(price)) - info.Fee);\n\n\t\t//Calculate recieving user's PNL\n                toPnl = new KeyValuePair<string, BigDecimal>(info.FromAddressHash,\n                    transfer.Amount * Convert.ToDecimal(price));\n\n\t\t//Form calculation result with Address-PNL pairs\n                return new CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?>\n                {\n                    Result = new[] { fromPnl, toPnl }\n                };\n            }\n        }\n\n\t//If requirements were not met\n        return new CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?>\n        {\n            Result = null\n        };\n    }\n}\n\nTransaction object used for calculations (everything in this object is ready for calculations, no decoding needed):\n\npublic class TransactionInfo : Entity, IMayHaveError, IDisposable\n{\n    //Unique transaction Hash\n    [Key]\n    public string Hash { get; set; }\n\n    //User address (The user who initiated the transaction)\n    public string FromAddressHash { get; set; }\n\n    //Recieving user address (could be the same as FromAddress)\n    public string ToAddressHash { get; set; }\n\n    //Transaction fee (already precalculated and decoded. Should be used in calculations)\n    public decimal Fee { get; set; }\n\n    //Count of token transfers in transaction\n    public int TransfersCount { get; private set; }\n\n    //List of token transfers in transaction\n    private List<TokenTransferInfo> _transfers;\n\n    [Invisible] [ForeignKey(nameof(Hash))]\n    public List<TokenTransferInfo>? Transfers\n    {\n        get => _transfers;\n        set\n        {\n            _transfers = value;\n            TransfersCount = _transfers?.Count() ?? 0;\n        }\n    }\n\n    //Transaction value in ETH. Usualy greater than zero if there are ETH tokens in transaction. Could be zero but it doesn't mean anything\n    [JsonIgnore]\n    public BigDecimal Value { get; set; }\n\n    //Method to get transfers from user, who initiated this transaction \n    public List<TokenTransferInfo>? GetTransfersFrom()\n    {\n        return Transfers?.Where(transfer => transfer.FromAddressHash == FromAddressHash).ToList();\n    }\n\n    //Method to get transfers to recieving user \n    public List<TokenTransferInfo>? GetTransfersTo()\n    {\n        return Transfers?.Where(transfer => transfer.ToAddressHash == FromAddressHash).ToList();\n    }\n\n    //Method to get tokens in transfers from user, who initiated this transaction \n    public List<TokenInfo>? GetTokensFrom()\n    {\n        var result = GetTransfersFrom();\n        if (result == null) return null;\n        return GetTokens(result).ToList();\n    }\n\n    //Method to get tokens in transfers to recieving user \n    public List<TokenInfo>? GetTokensTo()\n    {\n        var result = GetTransfersTo();\n        if (result == null) return null;\n        return GetTokens(result).ToList();\n    }\n\n    //Method to get all tokens in transaction\n    public IEnumerable<TokenInfo> GetTokens() => GetTokens(this.GetTransfersTo().Union(this.GetTransfersFrom()));\n\n    //Method to get DISTINCT tokens in transaction\n    public IEnumerable<TokenInfo> GetTokens(IEnumerable<TokenTransferInfo> transfers)\n    {\n        return transfers.Select(t => t.TokenInfo).Distinct();\n    }\n\n\n    //Method to get tokens and their amounts for each transfer\n    public IEnumerable<KeyValuePair<TokenInfo, BigDecimal>> GetTokensWithAmounts(List<TokenTransferInfo> transfers)\n    {\n        foreach (var transfer in transfers)\n            yield return new KeyValuePair<TokenInfo, BigDecimal>(transfer.TokenInfo, transfer.Amount);\n    }\n\n    //Method to get DISTINCT tokens in transaction\n    public IEnumerable<TokenInfo>? GetDistinctTokens()\n    {\n        return Transfers?.Select(t => t.TokenInfo).DistinctBy(t => t.Symbol);\n    }\n}\n\nYOU ONLY USE TransactionInfo's EXISTING METHODS AND NAMES. YOU DO NOT CREATE NEW ONES FOR TransactionInfo\n\nTOKEN AMOUNTS ALWAYS COME FROM TRANSFERS OBJECTS IN TRANSACTIONINFO.\n\nCODE RULES:\n1) CalculationResult must always be CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?>, having Address-PNL pairs in it or NULL.\n2) Don't forget to put real formula name into [assembly: FormulaAssembly('NAME HERE')]. Name must be EXACTLY as in Solidity contract\n3) Formula signature will ALWAYS be the same:\nCalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?> Formula(TransactionInfo info, IEnumerable<KeyValuePair<string, decimal>>? tokenPrices = null)\n4) THE START OF THE FORMULA, EXCEPT FORMULA NAME, ALWAYS STAYS THE SAME:\n\n[assembly: FormulaAssembly(\"FORMULA NAME\")]  (this one should be EXACTLY as function name in Solidity contract (even if it starts with a lowercase letter))\n\nnamespace Sauron.Formula.FORMULANAME;\n\npublic class FORMULANAMEFormula : IFormulaPlugin\n{\n    public CalculationResult<IEnumerable<KeyValuePair<string, BigDecimal>>?> Formula(TransactionInfo info,\n        IEnumerable<KeyValuePair<string, decimal>>? tokenPrices = null)\n    {\n\n5) You don't add checks to know if method used is actually correct.\n6) You do not add any checks to know if TransactionInfo fields are correct.\n7) VERY IMPORTANT!!! You don't try to decode anything, everything in TransactionInfo object is already decoded.\n8) Sometimes, if logical, you add checks to know if toAddress is the same as fromAddress (for example, for swaps). Fee still exists\n9) DO NOT forget to subtract Fee, even if user sends something to themselves\nThis is important, because user can send tokens on their own address, then PNL MUST BE CALCULATED DIFFERENTLY\n10) You don't try to decode BigDecimal, it is already readable. AGAIN: ALL CONVERTION OPERATIONS WERE DONE BEFOREHAND\n11) You do not include Message in CalculationResult\n12) PNL is returned in Eth, so you DO NOT CONVERT IT\n13) You DO NOT 'Assume' that user loses money. You should calculate that. PNL is defenetely negative if user sent someone money (but you must check, maybe, recieving address is the same as sender address).\n14) PNL is calculated for user addresses, not tokens\n15) You try your hardest to create the correct PNL formula, this is important\n16) There could be infinite amount of transfers in one transaction, and it is correct\n\nRETURN ANSWER RULES:\n1) Your return answer is a C# code with assembly info (IMPORTANT) for PNL calculation without comments and messages for user\n2) Code MUST NOT have comments in it\n3) Code must not have any decode functions in it.";

        var message = new Message() { Role = "system", Content = systemMessage };

        messages.Add(message);

        var requestData = new OpenAIAPI.Request()
        {
            ModelId = model,
            Messages = messages
        };


        HttpResponseMessage response = null;
        
        try
        {
            response = await httpClient.PostAsJsonAsync(endpoint, requestData);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        OpenAIAPI.ResponseData? responseData = null;
        
            responseData = await response.Content.ReadFromJsonAsync<OpenAIAPI.ResponseData>();
            

        var choices = responseData?.Choices ?? new List<OpenAIAPI.Choice>();
        
        var choice = choices[0];
        var responseMessage = choice.Message;

        messages.Add(responseMessage);
    }

    public async Task<string> SendPromptToGPT(HttpClient httpClient, string prompt)
    {
        var message = new Message() { Role = "user", Content = prompt };

        messages.Add(message);

        var requestData = new OpenAIAPI.Request()
        {
            ModelId = model,
            Messages = messages
        };

        using var response = await httpClient.PostAsJsonAsync(endpoint, requestData);

        OpenAIAPI.ResponseData? responseData = await response.Content.ReadFromJsonAsync<OpenAIAPI.ResponseData>();

        var choices = responseData?.Choices ?? new List<OpenAIAPI.Choice>();

        var choice = choices[0];
        var responseMessage = choice.Message;

        messages.Add(responseMessage);
        var responseText = responseMessage.Content.Trim();

        return responseText;
    }
}

