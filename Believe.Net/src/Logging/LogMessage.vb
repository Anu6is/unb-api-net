Namespace Believe.Net
    Public Class LogMessage
        Public ReadOnly Property Message As String
        Public ReadOnly Property Level As LogLevel

        Public Sub New(level As LogLevel, message As String)
            Me.Message = message
            Me.Level = level
        End Sub

        Public Overrides Function ToString() As String
            Return Message
        End Function
    End Class

    Public Enum LogLevel
        Critical = 5
        [Error] = 4
        Warning = 3
        Info = 2
        Debug = 0
    End Enum
End Namespace