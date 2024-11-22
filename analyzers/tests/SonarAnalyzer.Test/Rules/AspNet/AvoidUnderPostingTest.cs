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

#if NET

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class AvoidUnderPostingTest
{
    private static readonly IEnumerable<MetadataReference> AspNetReferences = [
                AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcAbstractions,
                AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcCore,
                AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcViewFeatures,
                ..NuGetMetadataReference.SystemComponentModelAnnotations(Constants.NuGetLatestVersion)];

    private readonly VerifierBuilder builder = new VerifierBuilder<AvoidUnderPosting>()
            .WithBasePath("AspNet")
            .AddReferences([..AspNetReferences, .. NuGetMetadataReference.SystemTextJson("7.0.4"), ..NuGetMetadataReference.NewtonsoftJson("13.0.3")]);

    [TestMethod]
    public void AvoidUnderPosting_CSharp() =>
        builder.AddPaths("AvoidUnderPosting.cs", "AvoidUnderPosting.AutogeneratedModel.cs").Verify();

    [TestMethod]
    public void AvoidUnderPosting_Latest() =>
        builder.AddPaths("AvoidUnderPosting.Latest.cs", "AvoidUnderPosting.Latest.Partial.cs")
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();

    [DataTestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    [DataRow("record")]
    [DataRow("record struct")]
    public void AvoidUnderPosting_EnclosingTypes_CSharp(string enclosingType) =>
        builder.AddSnippet($$"""
            using Microsoft.AspNetCore.Mvc;
            using System.ComponentModel.DataAnnotations;

            public {{enclosingType}} Model
            {
                public int ValueProperty { get; set; }                      // Noncompliant
                public int? NullableValueProperty { get; set; }             // Compliant
                public required int RequiredValueProperty { get; set; }     // Compliant
            }

            public class ControllerClass : Controller
            {
                [HttpPost] public IActionResult Create(Model model) => View(model);
            }
            """)
        .WithOptions(ParseOptionsHelper.FromCSharp11)
        .Verify();

    [DataTestMethod]
    [DataRow("HttpDelete")]
    [DataRow("HttpGet")]
    [DataRow("HttpPost")]
    [DataRow("HttpPut")]
    public void AvoidUnderPosting_HttpHandlers_CSharp(string attribute) =>
        builder.AddSnippet($$"""
            using Microsoft.AspNetCore.Mvc;
            using System.ComponentModel.DataAnnotations;

            public class Model
            {
                public int ValueProperty { get; set; }  // Noncompliant
            }

            public class ControllerClass : Controller
            {
                [{{attribute}}] public IActionResult Create(Model model) => View(model);
            }
            """).Verify();

    [TestMethod]
    public void AvoidUnderPosting_ModelInDifferentProject_CSharp()
    {
        const string modelCode = """
            namespace Models
            {
                public class Person
                {
                    public int Age { get; set; }    // FN - Roslyn can't raise an issue when the location is in different project than the one being analyzed
                }
            }
            """;
        const string controllerCode = """
            using Microsoft.AspNetCore.Mvc;
            using Models;

            namespace Controllers
            {
                public class PersonController : Controller
                {
                    [HttpPost] public IActionResult Post(Person person) => View(person);
                }
            }
            """;
        var solution = SolutionBuilder.Create()
            .AddProject(AnalyzerLanguage.CSharp)
            .AddSnippet(modelCode)
            .Solution
            .AddProject(AnalyzerLanguage.CSharp)
            .AddProjectReference(x => x.ProjectIds[0])
            .AddReferences(AspNetReferences)
            .AddSnippet(controllerCode)
            .Solution;
        var compiledAspNetProject = solution.Compile()[1];
        DiagnosticVerifier.Verify(compiledAspNetProject, new AvoidUnderPosting());
    }
}

#endif
