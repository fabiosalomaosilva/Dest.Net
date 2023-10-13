using Dest.Credentials;
using Dest.Models;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace Dest.Services
{
    public class GptService
    {
        public static async Task<string> CreateClassOfUnitTests(ClassCode code)
        {
            var prompt = code.RelatedClasses.Any()
                ? $"Create a complete class using the xUnit and Moq libraries with unit tests to provide coverage of at least 90% of the class's methods, which I will inform below. Please, I want the answer to not contain notes in English, Spanish, Portuguese or any other language. I don't need additional notes, additional information, explanations or comments or any type of documentation that demonstrates the operation of the suggested code. At this moment, I don't need you to be helpful and ready to help. I don't need you to tell me you can take the test. Therefore, only bring code written in C#. I don't even want the answer to contain the addition of auxiliary classes or interfaces or even class definitions. See that I'm not using EntityFramework and I don't use DbSet. But CocDbContext and CocDbSet. Finally, the test must achieve the desired coverage of at least 90%, but the code must be as small and clean as possible (use of clean code).\r\n{code.ClassText}\r\nFor learning and assistance, we have the classes, interfaces and enums that were referenced:\r\n{string.Join("\r\n", code.RelatedClasses)}"
                : $"Create a complete class using the xUnit and Moq libraries with unit tests to provide coverage of at least 90% of the class's methods, which I will inform below. Please, I want the answer to not contain notes in English, Spanish, Portuguese or any other language. I don't need additional notes, additional information, explanations or comments or any type of documentation that demonstrates the operation of the suggested code. At this moment, I don't need you to be helpful and ready to help. I don't need you to tell me you can take the test. Therefore, only bring code written in C#. I don't even want the answer to contain the addition of auxiliary classes or interfaces or even class definitions. See that I'm not using EntityFramework and I don't use DbSet. But CocDbContext and CocDbSet. Finally, the test must achieve the desired coverage of at least 90%, but the code must be as small and clean as possible (use of clean code).\r\n{code.ClassText}";

            var apiKey = CredentialManager.GetCredential();
            if (string.IsNullOrEmpty(apiKey))
            {
                var newApyValid = false;
                Console.WriteLine("---------------");
                Console.WriteLine("");
                Console.ResetColor();
                while (newApyValid == false)
                {
                    Console.WriteLine("");
                    Console.Write("Por favor, insira uma nova chave da API da OpenAI: ");
                    var newApiKey = Console.ReadLine();
                    if (string.IsNullOrEmpty(newApiKey) || newApiKey.Length < 45)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("API Key inválida. Insira um API válida");
                    }
                    else
                    {
                        CredentialManager.SaveCredential(newApiKey);
                        apiKey = newApiKey;
                        newApyValid = true;
                        Console.ResetColor();
                    }
                }
            }

            try
            {
                var api = new OpenAI_API.OpenAIAPI(apiKey);
                var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest
                {
                    Model = Model.ChatGPTTurbo,
                    Temperature = 0.2,
                    Messages = new ChatMessage[]
                    {
                        new(ChatMessageRole.User, prompt)
                    }
                });

                return result.Choices[0].Message.Content.Trim();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
