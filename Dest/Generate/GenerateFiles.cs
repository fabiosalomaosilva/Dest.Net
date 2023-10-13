using Dest.Models;
using Dest.Services;
using Dest.Settings;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Reflection;

namespace Dest.Generate
{
    public class GenerateFiles
    {
        public static async Task CreateClassFile(ClassCode classCode)
        {
            try
            {
                var config = await Config.GetDestConfig();
                var projectPath = Path.Combine(config.TestProjectPath, classCode.RelativePath);

                var exists = Directory.Exists(projectPath);
                if (!exists)
                {
                    Directory.CreateDirectory(projectPath);
                }

                var testCode = await GptService.CreateClassOfUnitTests(classCode);
                var classPath = Path.Combine(projectPath, $"{classCode.ClassName}Test.cs");
                await File.WriteAllTextAsync(classPath, testCode);
                Console.WriteLine($"Arquivo {classCode.ClassName} criado.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro no CreateClassFile: {e.Message}");
                throw;
            }
        }

        public static async Task<List<ClassCode>> GetClassesWithoutAttribute(string solutionPath, string projectName)
        {
            try
            {
                Console.WriteLine($"Iniciando a análise do projeto");
                var solutionName = FindSolutionFile(solutionPath);
                if (solutionName == null)
                {
                    Console.WriteLine("Não foi possível encontrar o arquivo da solution.");
                    return null;
                }

                var classesWithoutAttribute = new List<ClassCode>();

                MSBuildLocator.RegisterDefaults();
                var workspace = MSBuildWorkspace.Create();

                Console.WriteLine($"Lendo o arquivo de Solution...");
                var solution = await workspace.OpenSolutionAsync(solutionName);

                var project = solution.Projects.First(p => p.Name == projectName);
                var compilation = await project.GetCompilationAsync();

                Console.WriteLine("Carregando os arquivos do projeto...");
                foreach (var document in project.Documents)
                {
                    var root = await document.GetSyntaxRootAsync();
                    var model = compilation!.GetSemanticModel(root!.SyntaxTree);

                    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                    foreach (var classDeclaration in classes)
                    {
                        var classSymbol = model.GetDeclaredSymbol(classDeclaration);
                        var hasAttribute = classSymbol!.GetAttributes()
                            .Any(attr => attr.AttributeClass!.Name == "ExcludeFromCodeCoverage");

                        if (hasAttribute) continue;
                        var relatedClasses = await LoadRelatedClasses(document, compilation);

                        var classCode = new ClassCode
                        {
                            ClassName = classSymbol.Name,
                            RelativePath = GetRelativePath(document.FilePath, solutionPath),
                            ClassText = await ConvertClassesToTextWithoutExcludedMethods(solutionPath, projectName, classSymbol.Name),
                            RelatedClasses = relatedClasses
                        };

                        classesWithoutAttribute.Add(classCode);
                    }
                }

                return classesWithoutAttribute;
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                throw;
            }

            catch (Exception e)
            {
                Console.WriteLine($"Erro no GetClassesWithoutAttribute: {e.Message}");
                throw;
            }
        }

        private static async Task<HashSet<string>> LoadRelatedClasses(Document document, Compilation compilation)
        {
            var root = await document.GetSyntaxRootAsync();
            var model = compilation!.GetSemanticModel(root!.SyntaxTree);

            var relatedClasses = new HashSet<string>();

            // Obtenha todas as referências a tipos na classe
            var typeReferences = root.DescendantNodes().OfType<IdentifierNameSyntax>();

            foreach (var typeReference in typeReferences)
            {
                var symbolInfo = model.GetSymbolInfo(typeReference);

                if (symbolInfo.Symbol != null)
                {
                    var referencedType = symbolInfo.Symbol;

                    // Verifique se o tipo é uma classe, interface ou enum
                    if (referencedType is INamedTypeSymbol namedTypeSymbol)
                    {
                        if (namedTypeSymbol.TypeKind == TypeKind.Class ||
                            namedTypeSymbol.TypeKind == TypeKind.Interface ||
                            namedTypeSymbol.TypeKind == TypeKind.Enum)
                        {
                            var relatedTypeSource = GetRelatedTypeSource(compilation, namedTypeSymbol);
                            if (!string.IsNullOrEmpty(relatedTypeSource))
                            {
                                relatedClasses.Add(relatedTypeSource);
                            }
                        }
                    }
                }
            }

            return relatedClasses;
        }

        private static string GetRelatedTypeSource(Compilation compilation, INamedTypeSymbol typeSymbol)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                // Encontre o tipo na árvore de sintaxe
                var typeDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node is ClassDeclarationSyntax ||
                                   node is InterfaceDeclarationSyntax ||
                                   node is EnumDeclarationSyntax)
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(declaration => semanticModel.GetDeclaredSymbol(declaration) == typeSymbol);

                if (typeDeclaration != null)
                {
                    // Retorne o código-fonte do tipo
                    return typeDeclaration.ToFullString();
                }
            }

            return null;
        }



        private static async Task<string> ConvertClassesToTextWithoutExcludedMethods(string solutionPath, string projectName, string className)
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

                    var targetClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Identifier.Text == className);

                    if (targetClass == null) continue;
                    var methodsToExclude = new List<MethodDeclarationSyntax>();

                    foreach (var method in targetClass.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        var methodSymbol = model.GetDeclaredSymbol(method);
                        if (methodSymbol!.GetAttributes()
                            .Any(attr => attr.AttributeClass!.Name == "ExcludeFromCodeCoverage"))
                        {
                            methodsToExclude.Add(method);
                        }
                    }

                    // Remove os métodos que possuem o atributo ExcludeFromCodeCoverage
                    var modifiedClass = targetClass.RemoveNodes(methodsToExclude, SyntaxRemoveOptions.KeepNoTrivia);

                    return modifiedClass!.NormalizeWhitespace().ToFullString();
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro no ConvertClassesToTextWithoutExcludedMethods: {e.Message}");
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

        private static string GetRelativePath(string fullPath, string solutionPath)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            var relativeToSolution = Path.GetRelativePath(solutionDir!, fullPath);
            var pathWithoutProject = string.Join(Path.DirectorySeparatorChar,
                relativeToSolution.Split(Path.DirectorySeparatorChar).Skip(1));

            var directoryOnly = Path.GetDirectoryName(pathWithoutProject);
            return directoryOnly;
        }
    }
}
