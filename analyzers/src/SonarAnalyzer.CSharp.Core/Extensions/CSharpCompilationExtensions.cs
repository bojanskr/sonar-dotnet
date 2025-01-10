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

namespace SonarAnalyzer.CSharp.Core.Extensions;

public static class CSharpCompilationExtensions
{
    public static bool IsCoalesceAssignmentSupported(this Compilation compilation) =>
        compilation.IsAtLeastLanguageVersion(LanguageVersionEx.CSharp8);

    public static bool IsTargetTypeConditionalSupported(this Compilation compilation) =>
        compilation.IsAtLeastLanguageVersion(LanguageVersionEx.CSharp9);

    public static bool IsLambdaDiscardParameterSupported(this Compilation compilation) =>
        compilation.IsAtLeastLanguageVersion(LanguageVersionEx.CSharp9);

    public static bool IsAtLeastLanguageVersion(this Compilation compilation, LanguageVersion languageVersion) =>
        compilation.GetLanguageVersion().IsAtLeast(languageVersion);

    public static LanguageVersion GetLanguageVersion(this Compilation compilation) =>
        ((CSharpCompilation)compilation).LanguageVersion;
}
