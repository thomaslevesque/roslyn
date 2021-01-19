﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AwaitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeContext()
        {
            VerifyAbsence(@"
class Program
{
    $$
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestStatementInMethod(bool isAsync, bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", isAsync: isAsync, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestExpressionInAsyncMethod(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var z = $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestUsingStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"using $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestUsingDirective()
            => VerifyAbsence("using $$");

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestForeachStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"foreach $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInQuery(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var z = from a in ""char""
          select $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFinally(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"try { }
finally { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCatch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"try { }
catch { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInLock(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"lock(this) { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAsyncLambdaInCatch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"try { }
catch { var z = async () => $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAwaitInLock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"lock($$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
