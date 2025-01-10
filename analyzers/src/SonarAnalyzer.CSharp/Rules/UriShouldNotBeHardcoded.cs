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
    public sealed class UriShouldNotBeHardcoded : UriShouldNotBeHardcodedBase<SyntaxKind, LiteralExpressionSyntax, ArgumentSyntax>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;
        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer => CSharpGeneratedCodeRecognizer.Instance;
        protected override SyntaxKind[] StringConcatenateExpressions => new[] { SyntaxKind.AddExpression };
        protected override SyntaxKind[] InvocationOrObjectCreationKind => new[] { SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression };

        protected override SyntaxNode GetRelevantAncestor(SyntaxNode node) =>
            (SyntaxNode)node.FirstAncestorOrSelf<ParameterSyntax>() ?? node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
    }
}
