using Dest.Models;
using Newtonsoft.Json;

namespace Dest.Settings
{
    public class Config
    {
        public static async Task<DestConfig> GetDestConfig()
        {
            if (!File.Exists("dest.config.json"))
            {
                Console.WriteLine("Arquivo de configurações não existe.");
                return null;
            }
            var configFile = await File.ReadAllTextAsync("dest.config.json");
            return JsonConvert.DeserializeObject<DestConfig>(configFile);
        }


        public static async Task SetTestProject()
        {
            var projects = ListProjects();
            if (projects == null)
            {
                Environment.Exit(0);
            }

            var selectedProject = SelectFromList("Selecione o projeto de testes:", projects);
            Console.WriteLine($"Você selecionou: {selectedProject}");
            Console.WriteLine("");

            await CreateConfigFile(selectedProject);
        }

        private static async Task CreateConfigFile(string projectName)
        {
            var rootCurrentDirectory = Directory.GetCurrentDirectory();
            var testProjectPath = Path.Combine(rootCurrentDirectory, projectName);

            var config = new DestConfig
            {
                TestProjectName = projectName,
                TestProjectPath = testProjectPath,
                RootProjectPath = rootCurrentDirectory
            };

            var json = JsonConvert.SerializeObject(config);
            await File.WriteAllTextAsync("dest.config.json", json);
        }

        private static List<string> ListProjects()
        {
            var namesOfProjects = new List<string>();

            var solutionFiles = Directory.GetFiles(".", "*.sln");
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Parece que esta não é a pasta raiz da aplicação. Deseja continuar? (s/n)");

                string input;
                while (true)
                {
                    input = Console.ReadLine()?.ToLower();

                    if (input is "s" or "n")
                    {
                        Console.WriteLine($"Você selecionou: '{input.ToUpper()}'");
                        break;
                    }

                    Console.WriteLine("Seleção inválida. Por favor, digite 's' ou 'n'.");
                }

                switch (input.ToLower())
                {
                    case "s":
                        {
                            Console.WriteLine("Você selecionou 'S'");
                            var projectFiles =
                                Directory.EnumerateFileSystemEntries(".", "*.csproj", SearchOption.AllDirectories);
                            namesOfProjects.AddRange(projectFiles.Select(Path.GetFileNameWithoutExtension)!);
                            break;
                        }
                    case "n":
                        Console.WriteLine("O programa será encerrado...");
                        return null;
                }
            }
            else
            {
                var projectFiles = Directory.EnumerateFileSystemEntries(".", "*.csproj", SearchOption.AllDirectories);
                foreach (var projectFile in projectFiles)
                {
                    var projectName =
                        Path.GetFileNameWithoutExtension(projectFile);
                    namesOfProjects.Add(projectName);
                }
            }

            return namesOfProjects;
        }

        private static string SelectFromList(string prompt, List<string> options)
        {
            Console.WriteLine(prompt);
            for (var i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"  [{(i + 1)}] {options[i]}");
            }

            while (true)
            {
                Console.WriteLine("");
                Console.Write("Digite o número da opção e pressione Enter: ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out int selectedIndex) && selectedIndex >= 1 && selectedIndex <= options.Count)
                {
                    return options[selectedIndex - 1];
                }
                else
                {
                    Console.WriteLine("Seleção inválida. Por favor, tente novamente.");
                }
            }
        }
    }



}