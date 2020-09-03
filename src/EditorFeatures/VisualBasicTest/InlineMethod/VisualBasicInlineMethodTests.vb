﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InlineMethod

    <Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)>
    Public Class VisualBasicInlineMethodTests
        Private Class TestVerifier
            Inherits VisualBasicCodeRefactoringVerifier(Of VisualBasicInlineMethodRefactoringProvider).Test
            Private Const Marker As String = "##"

            Public Shared Async Function TestInRegularAndScriptInDifferentFilesAsync(
                initialMarkUpForFile1 As String,
                initialMarkUpForFile2 As String,
                expectedMarkUpForFile1 As String,
                expectedMarkUpForFile2 As String,
                diagnosticResults As List(Of DiagnosticResult),
                Optional keepInlinedMethod As Boolean = True) As Task
                Dim test = New TestVerifier() With
                    {
                        .CodeActionIndex = If(keepInlinedMethod, 1, 0),
                        .CodeActionValidationMode = CodeActionValidationMode.None
                    }
                test.TestState.Sources.Add(("File1", initialMarkUpForFile1))
                test.TestState.Sources.Add(("File2", initialMarkUpForFile2))
                test.FixedState.Sources.Add(("File1", expectedMarkUpForFile1))
                test.FixedState.Sources.Add(("File2", expectedMarkUpForFile2))
                If diagnosticResults IsNot Nothing Then
                    test.FixedState.ExpectedDiagnostics.AddRange(diagnosticResults)
                End If
                Await test.RunAsync().ConfigureAwait(False)
            End Function

            Public Shared Async Function TestInRegularAndScriptAsync(
                initialMarkUp As String,
                expectedMarkUp As String,
                Optional diagnnoticResults As List(Of DiagnosticResult) = Nothing,
                Optional keepInlinedMethod As Boolean = True) As Task
                Dim test As New TestVerifier() With {.CodeActionIndex = If(keepInlinedMethod, 1, 0), .CodeActionValidationMode = CodeActionValidationMode.None}
                test.TestState.Sources.Add(initialMarkUp)
                test.FixedState.Sources.Add(expectedMarkUp)
                If diagnnoticResults IsNot Nothing Then
                    test.FixedState.ExpectedDiagnostics.AddRange(diagnnoticResults)
                End If
                Await test.RunAsync().ConfigureAwait(False)
            End Function

            Public Shared Async Function TestBothKeepAndRemoveInlinedMethodAsync(
                initialMarkUp As String,
                expectedMarkUp As String,
                Optional diagnnoticResultsWhenKeepInlinedMethod As List(Of DiagnosticResult) = Nothing,
                Optional diagnnoticResultsWhenRemoveInlinedMethod As List(Of DiagnosticResult) = Nothing) As Task
                Dim firstMarkerIndex = expectedMarkUp.IndexOf(Marker)
                Dim secondMarkerIndex = expectedMarkUp.LastIndexOf(Marker)
                If firstMarkerIndex = -1 OrElse secondMarkerIndex = 1 OrElse firstMarkerIndex = secondMarkerIndex Then
                    Assert.True(False, "Can't find proper marks that contains inlined method.")
                End If

                Dim firstPartitionBeforeMarkUp = expectedMarkUp.Substring(0, firstMarkerIndex)
                Dim inlinedMethod = expectedMarkUp.Substring(firstMarkerIndex + 2, secondMarkerIndex - firstMarkerIndex - 2)
                Dim lastPartitionAfterMarkup = expectedMarkUp.Substring(secondMarkerIndex + 2)

                Await TestInRegularAndScriptAsync(initialMarkUp,
                    String.Concat(firstPartitionBeforeMarkUp, inlinedMethod, lastPartitionAfterMarkup),
                    diagnnoticResultsWhenKeepInlinedMethod,
                    keepInlinedMethod:=True).ConfigureAwait(False)

                Await TestInRegularAndScriptAsync(initialMarkUp,
                    String.Concat(firstPartitionBeforeMarkUp, lastPartitionAfterMarkup),
                    diagnnoticResultsWhenRemoveInlinedMethod,
                    keepInlinedMethod:=False).ConfigureAwait(False)
            End Function

            Public Shared Async Function TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync(
                initialMarkUpForCaller As String,
                initialMarkUpForCallee As String,
                expectedMarkUpForCaller As String,
                expectedMarkUpForCallee As String,
                Optional diagnosticResultsWhenKeepInlinedMethod As List(Of DiagnosticResult) = Nothing,
                Optional diagnosticResultsWhenRemoveInlinedMethod As List(Of DiagnosticResult) = Nothing) As Task
                Dim firstMarkerIndex = expectedMarkUpForCallee.IndexOf(Marker)
                Dim secondMarkerIndex = expectedMarkUpForCallee.LastIndexOf(Marker)
                If firstMarkerIndex = -1 OrElse secondMarkerIndex = -1 OrElse firstMarkerIndex = secondMarkerIndex Then
                    Assert.True(False, "Can't find proper marks that contains inlined method.")
                End If

                Dim firstPartitionBeforeMarkUp = expectedMarkUpForCallee.Substring(0, firstMarkerIndex)
                Dim inlinedMethod = expectedMarkUpForCallee.Substring(firstMarkerIndex + 2, secondMarkerIndex - firstMarkerIndex - 2)
                Dim lastPartitionAfterMarkup = expectedMarkUpForCallee.Substring(secondMarkerIndex + 2)

                Await TestInRegularAndScriptInDifferentFilesAsync(
                    initialMarkUpForCaller,
                    initialMarkUpForCallee,
                    expectedMarkUpForCaller,
                    String.Concat(firstPartitionBeforeMarkUp, inlinedMethod, lastPartitionAfterMarkup),
                    diagnosticResultsWhenKeepInlinedMethod,
                    keepInlinedMethod:=True).ConfigureAwait(False)

                Await TestInRegularAndScriptInDifferentFilesAsync(
                    initialMarkUpForCaller,
                    initialMarkUpForCallee,
                    expectedMarkUpForCaller,
                    String.Concat(firstPartitionBeforeMarkUp, lastPartitionAfterMarkup),
                    diagnosticResultsWhenRemoveInlinedMethod,
                    keepInlinedMethod:=False).ConfigureAwait(False)
            End Function
        End Class

        <Fact>
        Public Function TestInlineExpressionStatement() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##
    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineExpressionStatementInDifferentFiles() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync("
Partial Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub
End Class", "
Partial Public Class TestClass

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Partial Public Class TestClass
    Public Sub Caller(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Partial Public Class TestClass
##
    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineReturnExpression() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim x = Me.Ca[||]llee(i)
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim x = i + 10
    End Sub
##
    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineDefaltValue() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee()
    End Sub

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class",
"
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 20 + If(False, 10, 100) + CType(A.Value1, Integer)
    End Sub
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineDefaltValueInDifferentFile() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync("
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller()
        Ca[||]llee()
    End Sub
End Class", "
Partial Public Class TestClass

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class", "
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 20 + If(False, 10, 100) + CType(A.Value1, Integer)
    End Sub
End Class", "
Partial Public Class TestClass
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineWithLiteralValue() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1, True, A.Value2)
    End Sub

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class",
"
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 1 + If(True, 10, 100) + CType(A.Value2, Integer)
    End Sub
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineWithLiteralValueInDifferentFile() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync("
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1, True, A.Value2)
    End Sub
End Class", "
Partial Public Class TestClass

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class", "
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 1 + If(True, 10, 100) + CType(A.Value2, Integer)
    End Sub
End Class", "
Partial Public Class TestClass
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineWithIdentifierReplacement() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller(c As A)
        Dim a = 10
        Dim b = True
        Ca[||]llee(a, b, c)
    End Sub

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class",
"
Public Enum A
    Value1
    Value2
End Enum
Public Class TestClass
    Public Sub Caller(c As A)
        Dim a = 10
        Dim b = True
        Dim temp As Integer = a + If(b, 10, 100) + CType(c, Integer)
    End Sub
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineWithIdentifierReplacementInDifferentFile() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync("
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller(c As A)
        Dim a = 10
        Dim b = True
        Ca[||]llee(a, b, c)
    End Sub
End Class", "
Partial Public Class TestClass

    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
End Class", "
Public Enum A
    Value1
    Value2
End Enum
Partial Public Class TestClass
    Public Sub Caller(c As A)
        Dim a = 10
        Dim b = True
        Dim temp As Integer = a + If(b, 10, 100) + CType(c, Integer)
    End Sub
End Class", "
Partial Public Class TestClass
##
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False, Optional c As A = Nothing) As Integer
        Return i + If(b, 10, 100) + CType(c, Integer)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray1() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1, 2, 3, 4)
    End Sub

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {1, 2, 3, 4}).Length)
    End Sub
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray1InDifferentFile() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodInDifferentFileAsync("
Partial Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1, 2, 3, 4)
    End Sub
End Class", "
Partial Public Class TestClass

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Partial Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {1, 2, 3, 4}).Length)
    End Sub
End Class", "
Partial Public Class TestClass
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray2() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(New Integer() {1, 2, 3, 4})
    End Sub

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {1, 2, 3, 4}).Length)
    End Sub
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray3() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1)
    End Sub

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {1}).Length)
    End Sub
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray4() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee()
    End Sub

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {}).Length)
    End Sub
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineParamArray5() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(1)
    End Sub

    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller()
        System.Console.WriteLine((New Integer() {1}).Length)
    End Sub
##
    Private Sub Callee(ParamArray args() as Integer)
        System.Console.WriteLine(args.Length)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineSelf() As Task
            Return TestVerifier.TestInRegularAndScriptAsync("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Callee(Callee(Ca[||]llee(i)))
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Callee(Callee(i + 10))
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineWithLiteralArgument() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(5, True)
    End Sub

    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class",
"
Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 5 + If(True, 10, 100)
    End Sub
##
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineIdentifierArgument() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub Caller()
        Dim i = 2222
        Dim x = True
        Ca[||]llee(i, x)
    End Sub

    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class",
"
Public Class TestClass
    Public Sub Caller()
        Dim i = 2222
        Dim x = True
        Dim temp As Integer = i + If(x, 10, 100)
    End Sub
##
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineInOperator() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Shared Operator +(i As TestClass, j As TestClass)
        Ca[||]llee(10)
        Return Nothing
    End Operator

    Private Shared Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Public Class TestClass
    Public Shared Operator +(i As TestClass, j As TestClass)
        System.Console.WriteLine(10)
        Return Nothing
    End Operator
##
    Private Shared Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestIdentifierRename() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(Cal(i, j), Cal(i, j))
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Integer
        return i * j
    End Function

    Private Function Cal(i As Integer, j As Integer) As Integer
        Return i + j
    End Function
End Class",
                "
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Cal(i, j) * Cal(i, j)
    End Sub
##
    Private Function Callee(i As Integer, j As Integer) as Integer
        return i * j
    End Function
##
    Private Function Cal(i As Integer, j As Integer) As Integer
        Return i + j
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineLambda1() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(i, j)()
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
End Class",
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Function()
                   Return i * j
               End Function()
    End Sub
##
    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineLambda2() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(i, j)()
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function() i * j
    End Function
End Class",
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = (Function() i * j)()
    End Sub
##
    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function() i * j
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression1() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Task
        Return Ca[||]llee()
    End Function

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Task
        Return Task.Delay(100)
    End Function
##
    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression2() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Sub Caller()
        Dim x = Function(i As Integer) As Task
                    Return Cal[||]lee()
                End Function
    End Sub

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Sub Caller()
        Dim x = Function(i As Integer) As Task
                    Return Task.Delay(100)
                End Function
    End Sub
##
    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression3() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Async Function Caller() As Task
        Await Ca[||]llee()
    End Function

    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Async Function Caller() As Task
        Await Task.FromResult(1)
    End Function
##
    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression4() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Task
        Dim x = Ca[||]llee()
    End Function

    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(Await Task.FromResult(100))
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Async Function Caller() As Task
        Dim x = Task.FromResult(Await Task.FromResult(100))
    End Function
##
    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(Await Task.FromResult(100))
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression5() As Task
            Dim diagnostic = New List(Of DiagnosticResult) From
                {
                    DiagnosticResult.CompilerError("BC32050").WithSpan(5, 22, 5, 32).WithArguments("TResult", "Public Shared Overloads Function FromResult(Of TResult)(result As TResult) As System.Threading.Tasks.Task(Of TResult)"),
                    DiagnosticResult.CompilerError("BC37057").WithSpan(5, 33, 5, 38).WithArguments("Integer"),
                    DiagnosticResult.CompilerError("BC32017").WithSpan(5, 39, 5, 58).WithMessage("Comma, ')', or a valid expression continuation expected.")
                }

            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Integer
        Dim x = Ca[||]llee()
        Return 1
    End Function

    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(Await Task.FromResult(100))
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Integer
        Dim x = Task.FromResult(Await Task.FromResult(100))
        Return 1
    End Function
##
    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(Await Task.FromResult(100))
    End Function
##End Class", diagnnoticResultsWhenKeepInlinedMethod:=diagnostic, diagnnoticResultsWhenRemoveInlinedMethod:=diagnostic)
        End Function

        <Fact>
        Public Function TestThrowStatement() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                "
Imports System
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee()
    End Sub

    Private Function Callee() As Integer
        Throw New Exception()
    End Function
End Class",
                "
Imports System
Public Class TestClass
    Public Sub Caller()
        Throw New Exception()
    End Sub
##
    Private Function Callee() As Integer
        Throw New Exception()
    End Function
##End Class")
        End Function

        <Fact>
        Public Function TestInlineInConstructor() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub New(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Public Class TestClass
    Public Sub New(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##
    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
##End Class")
        End Function

        <Fact>
        Public Function TestInlineReturnExpressionWithoutVariableDeclaration() As Task
            Return TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim temp As Integer = i + 10
    End Sub
##
    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
##End Class")
        End Function
    End Class
End Namespace
