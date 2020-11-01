Imports System
Imports BenchmarkDotNet
Imports BenchmarkDotNet.Attributes

Namespace _BenchmarkProjectName_

#If config Then
    <Config(GetType(BenchmarkConfig))>
#End If
    Public Class $(BenchmarkName)
        <Benchmark>
        Public Sub Scenario1()
        
        ' Implement your benchmark here

        End Sub

        <Benchmark>
        Public Sub Scenario2()
        
        ' Implement your benchmark here

        End Sub
    End Class
End Namespace