using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StringInterpolation
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringInterpolationCodeFixProvider)), Shared]
    public class StringInterpolationCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Wrap with Invariant(...)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(StringInterpolationAnalyzer.DiagnosticId);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create(Title, c => ApplyFix(context, c), Title), context.Diagnostics.First());
            return Task.FromResult(true);
        }

        private static async Task<Solution> ApplyFix(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We need to get the InterpolatedStringExpression that was found by the Analyser
            var diagnostic = context.Diagnostics.First();
            var position = diagnostic.Location.SourceSpan.Start;
            var interpolatedString = root.FindToken(position).Parent.AncestorsAndSelf()
                .OfType<InterpolatedStringExpressionSyntax>().First();

            var replacement = WrapWithCallToInvariant(interpolatedString);
            root = root.ReplaceNode(interpolatedString, replacement);
            
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root);
        }

        /// <summary>Wrap the InterpolatedString $"..." with Invariant($"...")</summary>
        private static InvocationExpressionSyntax WrapWithCallToInvariant(ExpressionSyntax expressionToWrap) =>
            InvocationExpression(
                InvariantExpressionSyntax,
                ArgumentList(SeparatedList(new[] { Argument(expressionToWrap) }))
            );

        /// <summary>Represents System.FormattableString.Invariant</summary>
        private static readonly MemberAccessExpressionSyntax InvariantExpressionSyntax =
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("System"),
                    IdentifierName("FormattableString")),
                IdentifierName("Invariant")
                )
            .WithAdditionalAnnotations(Simplifier.Annotation);
    }
}