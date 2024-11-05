﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

namespace SonarAnalyzer.Rules;

public abstract class ConstructorArgumentValueShouldExistBase<TSyntaxKind, TAttribute> : SonarDiagnosticAnalyzer<TSyntaxKind>
    where TSyntaxKind : struct
    where TAttribute : SyntaxNode
{
    private const string DiagnosticId = "S4260";

    protected abstract SyntaxNode GetFirstAttributeArgument(TAttribute attributeSyntax);

    protected override string MessageFormat => "Change this 'ConstructorArgumentAttribute' value to match one of the existing constructors arguments.";

    protected ConstructorArgumentValueShouldExistBase() : base(DiagnosticId) { }

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(Language.GeneratedCodeRecognizer,
            c =>
            {
                var attribute = (TAttribute)c.Node;
                if (Language.Syntax.IsKnownAttributeType(c.SemanticModel, c.Node, KnownType.System_Windows_Markup_ConstructorArgumentAttribute)
                    && GetFirstAttributeArgument(attribute) is { } firstAttribute
                    && c.SemanticModel.GetConstantValue(Language.Syntax.NodeExpression(firstAttribute)) is { HasValue: true, Value: string constructorParameterName }
                    && c.ContainingSymbol is IPropertySymbol { ContainingType: { } containingType }
                    && !GetConstructorParameterNames(containingType).Contains(constructorParameterName))
                {
                    c.ReportIssue(Rule, firstAttribute.GetLocation());
                }
            }, Language.SyntaxKind.Attribute);

    private static IEnumerable<string> GetConstructorParameterNames(INamedTypeSymbol containingSymbol) =>
        containingSymbol?.Constructors.SelectMany(x => x.Parameters).Select(x => x.Name) ?? [];
}
