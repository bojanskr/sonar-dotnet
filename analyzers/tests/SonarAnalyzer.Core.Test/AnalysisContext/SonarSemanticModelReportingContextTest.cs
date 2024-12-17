﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
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

using SonarAnalyzer.AnalysisContext;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace SonarAnalyzer.Core.Test.AnalysisContext;

[TestClass]
public class SonarSemanticModelReportingContextTest
{
    [TestMethod]
    public void Properties_ArePropagated()
    {
        var cancel = new CancellationToken(true);
        var (tree, model) = TestCompiler.CompileCS("// Nothing to see here");
        var options = AnalysisScaffolding.CreateOptions();
        var context = new SemanticModelAnalysisContext(model, options, _ => { }, _ => true, cancel);
        var sut = new SonarSemanticModelReportingContext(AnalysisScaffolding.CreateSonarAnalysisContext(), context);

        sut.Tree.Should().BeSameAs(tree);
        sut.Compilation.Should().BeSameAs(model.Compilation);
        sut.SemanticModel.Should().BeSameAs(model);
        sut.Options.Should().BeSameAs(options);
        sut.Cancel.Should().Be(cancel);
    }

    [TestMethod]
    public void RegistrationIsExecuted_SonarAnalysisContext_CS() =>
        new VerifierBuilder().AddAnalyzer(() => new TestAnalyzerCS((context, g) =>
            context.RegisterSemanticModelAction(g, c =>
                c.ReportIssue(TestAnalyzer.Rule, c.Tree.GetRoot().GetFirstToken()))))
        .AddSnippet("""
        using System; // Noncompliant
        """)
        .Verify();

    [TestMethod]
    public void RegistrationIsNotExecuted_SonarAnalysisContext_TestScope_CS() =>
        new VerifierBuilder()
            .AddTestReference()
            .AddAnalyzer(() => new TestAnalyzerCS((context, g) =>
                context.RegisterSemanticModelAction(g, c => Assert.Fail("Registration should not happen"))))
            .AddSnippet("""
            using System;
            """)
        .VerifyNoIssues();

    [TestMethod]
    public void RegistrationIsNotExecuted_SonarAnalysisContext_GeneratedCode_CS() =>
        new VerifierBuilder()
            .AddAnalyzer(() => new TestAnalyzerCS((context, g) =>
                context.RegisterSemanticModelAction(g, c => Assert.Fail("Action should not be called"))))
            .AddSnippet("""
            // <auto-generated>
            using System;
            """)
        .VerifyNoIssues();

    [TestMethod]
    public void RegistrationIsExecuted_SonarAnalysisContext_VB() =>
        new VerifierBuilder().AddAnalyzer(() => new TestAnalyzerVB((context, g) =>
            context.RegisterSemanticModelAction(g, c =>
                c.ReportIssue(TestAnalyzer.Rule, c.Tree.GetRoot().GetFirstToken()))))
        .AddSnippet("""
        Imports System ' Noncompliant
        """)
        .Verify();

    [CombinatorialDataTestMethod]
    public void RegistrationInAllFilesIsExecuted_SonarCompilationStartAnalysisContext_CS(
        [DataValues("", "// <auto-generated/>")] string autoGenerated,
        [DataValues(true, false)] bool testScope)
    {
        var verifier = new VerifierBuilder().AddAnalyzer(() => new TestAnalyzerCS((context, _) =>
            context.RegisterCompilationStartAction(start =>
                start.RegisterSemanticModelActionInAllFiles(c =>
                {
                    if (c.Tree.GetRoot().GetFirstToken() is { RawKind: not (int)CS.SyntaxKind.None } token)
                    {
                        c.ReportIssue(TestAnalyzer.Rule, token);
                    }
                }))))
            .AddSnippet($$"""
            {{autoGenerated}}
            using System; // Noncompliant
            """);
        if (testScope)
        {
            verifier.AddTestReference().VerifyNoIssues();
        }
        else
        {
            verifier.Verify();
        }
    }

    [TestMethod]
    public void RegistrationIsNotExecuted_SonarCompilationStartAnalysisContext_TestScope_CS() =>
        new VerifierBuilder()
            .AddTestReference()
            .AddAnalyzer(() => new TestAnalyzerCS((context, generatedCodeRecognizer) =>
                context.RegisterCompilationStartAction(start =>
                    start.RegisterSemanticModelAction(generatedCodeRecognizer, c => Assert.Fail("Registration should not be done")))))
            .AddSnippet("""
            using System;
            """)
            .VerifyNoIssues();

    [TestMethod]
    public void RegistrationIsNotExecuted_SonarCompilationStartAnalysisContext_GeneratedCode_CS() =>
        new VerifierBuilder().AddAnalyzer(() => new TestAnalyzerCS((context, generatedCodeRecognizer) =>
            context.RegisterCompilationStartAction(start =>
                start.RegisterSemanticModelAction(generatedCodeRecognizer, c => Assert.Fail("Action should not be called")))))
            .AddSnippet("""
            // <auto-generated>
            using System;
            """)
            .VerifyNoIssues();

    [TestMethod]
    public void RegistrationIsExecuted_SonarCompilationStartAnalysisContext_VB() =>
        new VerifierBuilder().AddAnalyzer(() => new TestAnalyzerVB((context, _) =>
            context.RegisterCompilationStartAction(start =>
                start.RegisterSemanticModelActionInAllFiles(c =>
                {
                    if (c.Tree.GetRoot().GetFirstToken() is { RawKind: not (int)VB.SyntaxKind.None } token)
                    {
                        c.ReportIssue(TestAnalyzer.Rule, token);
                    }
                }))))
            .AddSnippet("""
            Imports System ' Noncompliant
            """)
            .Verify();
}
