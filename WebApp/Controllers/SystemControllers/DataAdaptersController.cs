using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebApp.Controllers.SystemControllers
{
 

    [Route("/DataAdapters")]
    [ApiController]
    public class DataAdaptersController : ControllerBase
    {
        [HttpGet]
        public async Task GetCommand()
        {
            await ProcessRequest();
        }

        [HttpPost]
        public async Task PostCommand()
        {
            await ProcessRequest();
        }

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private Regex serverCertificateRegex = new Regex(@"Trust\s*Server\s*Certificate\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex sslModeRegex = new Regex(@"SSL\s*Mode|SslMode\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private async Task ProcessRequest()
        {
            var result = new Result { Success = true };
            var encodeResult = false;

            try
            {
                string inputText;
                using (StreamReader stream = new StreamReader(Request.Body))
                {
                    inputText = await stream.ReadToEndAsync();
                }

                if (!string.IsNullOrEmpty(inputText) && inputText[0] != '{')
                {
                    var buffer = Convert.FromBase64String(ROT13(inputText));
                    inputText = Encoding.UTF8.GetString(buffer);
                    encodeResult = true;
                }

                var command = JsonSerializer.Deserialize<CommandJson>(inputText, jsonOptions);

                if (command.Command == "GetSupportedAdapters")
                {
                    result.Success = true;
                    result.Types = new string[] { "MySQL", "Firebird", "MS SQL", "PostgreSQL", "Oracle", "MongoDB" };
                }
                else
                {
                    switch (command.Database)
                    {
 
                        case "MS SQL":
                            if (!serverCertificateRegex.IsMatch(command.ConnectionString))
                                command.ConnectionString += (command.ConnectionString.TrimEnd().EndsWith(";") ? "" : ";") + "TrustServerCertificate=true;";
                            result = SQLAdapter.Process(command, new SqlConnection(command.ConnectionString));
                            break;
                
                        default: result.Success = false; result.Notice = $"Unknown database type [{command.Database}]"; break;
                    }
                }
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Notice = e.Message;
            }

            result.HandlerVersion = "2024.4.3";
            result.CheckVersion = true;

            var contentType = "application/json";
            var resultText = JsonSerializer.Serialize(result, jsonOptions);
            if (encodeResult)
            {
                resultText = ROT13(Convert.ToBase64String(Encoding.UTF8.GetBytes(resultText)));
                contentType = "text/plain";
            }

            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Engaged-Auth-Token");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.ContentType = contentType;
            await Response.WriteAsync(resultText);
            await Response.CompleteAsync();
        }

        private static string ROT13(string input)
        {
            return string.Join("", input.Select(x => char.IsLetter(x) ? x >= 65 && x <= 77 || x >= 97 && x <= 109 ? (char)(x + 13) : (char)(x - 13) : x));
        }
    }
}
