﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AllBranchesShouldNotHaveSameImplementation : AllBranchesShouldNotHaveSameImplementationBase
    {
        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                context => Analyze(context, (SwitchExpressionSyntaxWrapper)context.Node),
                SyntaxKindEx.SwitchExpression);

            context.RegisterNodeAction(
                new SwitchStatementAnalyzer().GetAnalysisAction(rule),
                SyntaxKind.SwitchStatement);

            context.RegisterNodeAction(
                new TernaryStatementAnalyzer().GetAnalysisAction(rule),
                SyntaxKind.ConditionalExpression);

            context.RegisterNodeAction(
                new IfStatementAnalyzer().GetAnalysisAction(rule),
                SyntaxKind.ElseClause);
        }

        private static void Analyze(SonarSyntaxNodeReportingContext context, SwitchExpressionSyntaxWrapper switchExpression)
        {
            var arms = switchExpression.Arms;
            if (arms.Count < 2)
            {
                return;
            }
            var firstArm = arms[0];
            if (switchExpression.HasDiscardPattern() &&
                arms.Skip(1).All(arm => SyntaxFactory.AreEquivalent(arm.Expression, firstArm.Expression)))
            {
                context.ReportIssue(rule, switchExpression.SwitchKeyword, StatementsMessage);
            }
        }

        private class IfStatementAnalyzer : IfStatementAnalyzerBase<ElseClauseSyntax, IfStatementSyntax>
        {
            protected override bool IsLastElseInChain(ElseClauseSyntax elseSyntax) =>
                !(elseSyntax.Statement is IfStatementSyntax);

            protected override IEnumerable<SyntaxNode> GetStatements(ElseClauseSyntax elseSyntax) =>
                new[] { elseSyntax.Statement };

            protected override IEnumerable<IEnumerable<SyntaxNode>> GetIfBlocksStatements(ElseClauseSyntax elseSyntax,
                out IfStatementSyntax topLevelIf)
            {
                var allStatements = new List<IEnumerable<SyntaxNode>>();

                var currentElse = elseSyntax;

                topLevelIf = null;

                while (currentElse?.Parent is IfStatementSyntax currentIf)
                {
                    topLevelIf = currentIf;
                    allStatements.Add(new[] { currentIf.Statement });
                    currentElse = currentIf.Parent as ElseClauseSyntax;
                }

                return allStatements;
            }

            protected override Location GetLocation(IfStatementSyntax topLevelIf) => topLevelIf.IfKeyword.GetLocation();
        }

        private class TernaryStatementAnalyzer : TernaryStatementAnalyzerBase<ConditionalExpressionSyntax>
        {
            protected override SyntaxNode GetWhenFalse(ConditionalExpressionSyntax ternaryStatement) =>
                ternaryStatement.WhenFalse.RemoveParentheses();

            protected override SyntaxNode GetWhenTrue(ConditionalExpressionSyntax ternaryStatement) =>
                ternaryStatement.WhenTrue.RemoveParentheses();

            protected override Location GetLocation(ConditionalExpressionSyntax ternaryStatement) =>
                ternaryStatement.Condition.CreateLocation(ternaryStatement.QuestionToken);
        }

        private class SwitchStatementAnalyzer : SwitchStatementAnalyzerBase<SwitchStatementSyntax, SwitchSectionSyntax>
        {
            protected override bool AreEquivalent(SwitchSectionSyntax section1, SwitchSectionSyntax section2) =>
                SyntaxFactory.AreEquivalent(section1.Statements, section2.Statements);

            protected override IEnumerable<SwitchSectionSyntax> GetSections(SwitchStatementSyntax switchStatement) =>
                switchStatement.Sections;

            protected override bool HasDefaultLabel(SwitchStatementSyntax switchStatement) =>
                switchStatement.HasDefaultLabel();

            protected override Location GetLocation(SwitchStatementSyntax switchStatement) =>
                switchStatement.SwitchKeyword.GetLocation();
        }
    }
}
