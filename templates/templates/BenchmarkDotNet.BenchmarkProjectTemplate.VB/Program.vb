Imports System
Imports BenchmarkDotNet.Running

Namespace _BenchmarkProjectName_
    Module Program
        Sub Main(args As String())
            Dim summary = BenchmarkRunner.Run(Of $(BenchmarkName))()
        End Sub
    End Module
End Namespace
