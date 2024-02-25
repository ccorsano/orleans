using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Orleans.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GenerateConverterSingleSurrogateTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "ORLEANS0014";
        private const string Category = "Usage";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ConverterSupportsSingleSurrogateTypeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ConverterSupportsSingleSurrogateTypeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ConverterSupportsSingleSurrogateTypeDescription), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(CheckSyntaxNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
        }

        private void CheckSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is TypeDeclarationSyntax declaration && !declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                if (declaration.HasAttribute(Constants.RegisterConverterAttributeName))
                {
                    INamedTypeSymbol baseIConverter = context.Compilation.GetTypeByMetadataName(Constants.ConverterInterfaceMetadataName)!;
                    Func<INamedTypeSymbol, bool> isIConverter = (INamedTypeSymbol t) => t is not null && SymbolEqualityComparer.Default.Equals(t, baseIConverter);

                    List<BaseTypeSyntax> declaredIConverters = new List<BaseTypeSyntax>();
                    foreach (BaseTypeSyntax baseListDeclaredType in declaration.BaseList.Types)
                    {
                        var typeInfo = context.SemanticModel.GetTypeInfo(baseListDeclaredType.Type).ConvertedType as INamedTypeSymbol;
                        if (typeInfo != null && typeInfo.IsGenericType && isIConverter(typeInfo.OriginalDefinition))
                        {
                            declaredIConverters.Add(baseListDeclaredType);
                        }
                    }

                    if (declaredIConverters.Count() > 1)
                    {
                        BaseTypeSyntax secondType = declaredIConverters.Skip(1).First();
                        context.ReportDiagnostic(Diagnostic.Create(Rule, secondType.GetLocation(), new object[] { declaration.Identifier.ToString() }));
                    }
                }
            }
        }
    }
}
