Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Believe.Net.Models
Imports Believe.Net.Parameters
Imports Newtonsoft.Json

Namespace Believe.Net
    Friend Class RequestClient
        Implements IDisposable

        Private ReadOnly _client As UnbelievaClient
        Private ReadOnly _httpClient As HttpClient
        Private ReadOnly _serializer As JsonSerializer
        Private ReadOnly _config As UnbelievaClientConfig

        Public Sub New(client As UnbelievaClient, config As UnbelievaClientConfig)
            _client = client
            _config = config
            _serializer = New JsonSerializer
            _httpClient = New HttpClient With {.BaseAddress = New Uri(config.ApiBaseUrl)}
            _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            _httpClient.DefaultRequestHeaders.Add("Authorization", config.Token)
        End Sub

        Friend Async Function SendAsync(Of T As {IDataModel, New})(method As HttpMethod, endpoint As String,
                                                                   Optional parameters As RequestParametersBase = Nothing,
                                                                   Optional queryString As String = "") As Task(Of T)
            Using request = New HttpRequestMessage(method, endpoint & queryString)
                parameters = If(parameters, DefaultParameters.Empty)
                request.Content = New StringContent(parameters.Serialize(_serializer), Encoding.UTF8, parameters.Encoding)

                Return Await SendAsync(Of T)(request).ConfigureAwait(False)
            End Using
        End Function

        Private Async Function SendAsync(Of T As {IDataModel, New})(request As HttpRequestMessage) As Task(Of T)
            Try
                Dim timer = Stopwatch.StartNew
                Using responseMsg = Await _httpClient.SendAsync(request).ConfigureAwait(False)
                    timer.Stop()
                    Dim requestUri = request.RequestUri.ToString
                    Dim headers = responseMsg.Headers.ToDictionary(Function(x) x.Key, Function(x) x.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase)
                    Dim stream = Await responseMsg.Content.ReadAsStreamAsync().ConfigureAwait(False)
                    Dim response = New RequestResponse(requestUri, responseMsg.StatusCode, headers, stream, timer.ElapsedMilliseconds) With {
                        .Message = responseMsg.ReasonPhrase
                    }

                    Dim value = ParseResponseAsync(Of T)(response)

                    Await LogRequest(request, response)

                    If response.Retry.HasValue AndAlso _config.RetryOnRateLimit Then
                        Await Task.Delay(response.Retry.Value)
                        Return Await SendAsync(Of T)(New HttpRequestMessage(request.Method, request.RequestUri))
                    End If

                    Return value
                End Using
            Catch ex As Exception
                Dim err = _client.LogAsync(New LogMessage(LogLevel.Critical, ex.ToString))
                Return Nothing
            End Try
        End Function

        Private Function ParseResponseAsync(Of T As {IDataModel, New})(ByRef response As RequestResponse) As T
            Select Case response.StatusCode
                Case HttpStatusCode.OK
                    Return response.Stream.Deserialize(Of T)(_serializer)
                Case 429 'HttpStatusCode.TooManyRequests
                    Dim ratelimit = response.Stream.Deserialize(Of RateLimit)(_serializer)
                    Dim span = TimeSpan.FromMilliseconds(ratelimit.RetryAfter)
                    Dim retry = String.Format("{0:D2}m:{1:D2}s:{2:D3}ms", span.Minutes, span.Seconds, span.Milliseconds)

                    With response
                        .Message = $"{ratelimit.Message}. Retry in {retry}"
                        .Retry = span
                    End With

                    Return New T With {.IsRateLimited = True, .RetryAfter = span}
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Async Function LogRequest(request As HttpRequestMessage, response As RequestResponse) As Task
            If response.IsSuccess Then
                Await _client.LogAsync(New RequestMessage(LogLevel.Verbose,
                                                          $"Status:{response.StatusCode} | {request.Method} {response.Request} {response.Duration}ms",
                                                          response))
            ElseIf response.Retry.HasValue Then
                Await _client.LogAsync(New RatelimitMessage(LogLevel.Warning,
                                                            $"{response.Message} | {request.Method} {response.Request}",
                                                            New RateLimitInfo(response.Headers)))
            Else
                Await _client.LogAsync(New LogMessage(LogLevel.Error,
                                                      $"{response.Message} | {request.Method} {response.Request} {response.Duration}ms"))
            End If
        End Function

        Private disposedValue As Boolean
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    _httpClient.Dispose()
                End If
            End If
            disposedValue = True
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub
    End Class
End Namespace

