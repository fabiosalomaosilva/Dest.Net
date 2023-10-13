using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Dest.Generate;

public class Converter
{
    public static async Task<string> ConvertClassToTextWithoutComments(string solutionPath, string projectName,
        string className)
    {
        try
        {
            var solutionName = FindSolutionFile(solutionPath);
            if (solutionName == null)
            {
                Console.WriteLine("Não foi possível encontrar o arquivo da solution.");
                return null;
            }

            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionName);
            var project = solution.Projects.First(p => p.Name == projectName);
            var compilation = await project.GetCompilationAsync();

            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                var model = compilation!.GetSemanticModel(root!.SyntaxTree);

                // Encontre a classe com o nome especificado
                var targetClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text == className);

                if (targetClass != null)
                {
                    return targetClass!.NormalizeWhitespace().ToFullString();
                }
            }

            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro na conversão da classe: {e.Message}");
            throw;
        }
    }

    public static string FindSolutionFile(string projectPath)
    {
        var directory = new DirectoryInfo(projectPath);
        while (directory != null)
        {
            var solutionFile = directory.GetFiles("*.sln").FirstOrDefault();
            if (solutionFile != null)
            {
                return solutionFile.FullName;
            }
            directory = directory.Parent;
        }

        return null;
    }
}