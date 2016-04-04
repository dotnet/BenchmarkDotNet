Imports BenchmarkDotNet.Attributes

Public Class Sample
    <Params(1, 2)>
    Public Property A As Integer
    <Params(3, 4)>
    Public Property B As Integer

    <Benchmark>
    Public Function Benchmark() As Integer
            return A + B
    End Function
End Class
