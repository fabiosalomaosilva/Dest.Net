using Dest.Generate;
using Dest.Settings;
using System.Reflection;

namespace Dest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Dest.Net. Automatizando seus testes unitátios.");
            Console.WriteLine("---------------");
            Console.ResetColor();
            Console.WriteLine("");

            ParamsCommand.DataArgs = new Dictionary<string, string>();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        ParamsCommand.DataArgs[args[i]] = args[i + 1];
                        i++;
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        ParamsCommand.DataArgs[args[i]] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        ParamsCommand.DataArgs[args[i]] = string.Empty;
                    }
                }
            }
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Por favor, forneça um comando.");
                    return;
                }

                if (args.Length > 0 && args[0] == "config")
                {
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Configurar o Dest.Net CLI:");
                    Console.WriteLine("");

                    await Config.SetTestProject();
                }
                if (args.Length > 0 && args[0] == "help")
                {
                    Console.WriteLine("Comandos do Dest.Net CLI:");
                    Console.WriteLine("------------##------------");
                    Console.WriteLine();
                    Console.WriteLine("add-test -p nome-projeto          Cria os testes unitários do projeto selecionado");
                    Console.WriteLine("");

                    return;
                }

                if (args.Length > 0 && args[0] == "add-tests")
                {
                    var destConfig = await Config.GetDestConfig();
                    if (destConfig == null)
                    {
                        await Config.SetTestProject();
                    }

                    if (ParamsCommand.DataArgs.TryGetValue("-p", out var project))
                    {
                        Console.WriteLine($"Criando os testes do projeto: {project}");
                    }

                    if (string.IsNullOrEmpty(project))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Não foi informado o projeto que será analizado para criação dos testes.");
                        Console.WriteLine("Utilize o atributo '-p' para definir o projeto.");
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Use o seguinte comando: $ dest add-teste -p {destConfig?.RootProjectPath}.NomeDoProjeto");
                        Console.WriteLine("---------------");
                        Console.ResetColor();
                        Console.WriteLine("");
                        return;
                    }

                    Console.WriteLine("");
                    var cocContext = "";
                    var executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
                    var executingDirectory = Path.GetDirectoryName(executingAssemblyPath);

                    var filePath = Path.Combine(executingDirectory, "Utils", "CocDbSet.txt");
                    if (File.Exists(filePath))
                    {
                        cocContext = await File.ReadAllTextAsync(filePath);
                    }

                    var classes = await GenerateFiles.GetClassesWithoutAttribute(destConfig?.RootProjectPath, project);
                    foreach (var item in classes)
                    {
                        if (!string.IsNullOrEmpty(cocContext))
                        {
                            item.RelatedClasses.Add(cocContext);
                        }
                    }

                    foreach (var item in classes)
                    {
                        Console.WriteLine($"Criando o arquivo {item.ClassName}Teste.cs");
                        await GenerateFiles.CreateClassFile(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                throw;
            }
        }
    }
}