using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringInterpolation
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringInterpolationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "StringInterpolation";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Globalization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInterpolatedStringExpression, SyntaxKind.InterpolatedStringExpression);
        }

        private static void AnalyzeInterpolatedStringExpression(SyntaxNodeAnalysisContext context)
        {
            // If an interpolated string has any interpolations that implement IFormattable, 
            // the interpolated string SHOULD be assigned to a FormattableString.
            // This way we are sure the developer has made an explicit choice on how the 
            // formattable interpolations should be formatted.

            var interpolatedStringExpression = (InterpolatedStringExpressionSyntax)context.Node;
            var typeInfo = context.SemanticModel.GetTypeInfo(interpolatedStringExpression);

            // If the Interpolated String Expression is assigned to FormattableString
            // we trust it will be formatted correctly
            if (typeInfo.ConvertedType.Name == "FormattableString") return;


            // Find all InterpolationExpressions that have a type which implements IFormattable
            var formattableInterpolations = interpolatedStringExpression.Contents
                                                .OfType<InterpolationSyntax>()
                                                .Where(i => IsFormattable(i.Expression,
                                                                           context.SemanticModel));

            // For each formatable interpolation report a separate Diagnostic
            foreach (var interpolation in formattableInterpolations)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule,
                                                           interpolation.Expression.GetLocation(),
                                                           interpolation));
            }
        }

        private static bool IsFormattable(ExpressionSyntax expression, SemanticModel semanticModel) => 
            semanticModel.GetTypeInfo(expression).ConvertedType?.AllInterfaces
                .Any(i => i.Name == typeof(IFormattable).Name) ?? false;
    }
}
