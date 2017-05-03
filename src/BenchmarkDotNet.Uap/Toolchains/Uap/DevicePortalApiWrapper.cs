#if !UAP
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using BenchmarkDotNet.Environments;
using RestSharp.Authenticators;

namespace BenchmarkDotNet.Uap
{
#if CORE
    internal static class RestClientExtensions
    {
        public static IRestResponse Execute(this RestClient @this, IRestRequest request)
        {
            TaskCompletionSource<IRestResponse> tcs = new TaskCompletionSource<IRestResponse>();
            @this.ExecuteAsync(request, (response) => tcs.SetResult(response));
            return tcs.Task.Result;
        }

        public static IRestResponse<T> Execute<T>(this RestClient @this, IRestRequest request) where T : new()
        {
            TaskCompletionSource<IRestResponse<T>> tcs = new TaskCompletionSource<IRestResponse<T>>();
            @this.ExecuteAsync<T>(request, (response) => tcs.SetResult(response));
            return tcs.Task.Result;
        }

        public static IRestResponse<T> Get<T>(this RestClient @this, IRestRequest request) where T : new()
        {
            TaskCompletionSource<IRestResponse<T>> tcs = new TaskCompletionSource<IRestResponse<T>>();
            @this.GetAsync<T>(request, (response, handle) => tcs.SetResult(response));
            return tcs.Task.Result;
        }

        public static IRestResponse<T> Post<T>(this RestClient @this, IRestRequest request) where T : new()
        {
            TaskCompletionSource<IRestResponse<T>> tcs = new TaskCompletionSource<IRestResponse<T>>();
            @this.PostAsync<T>(request, (response, handle) => tcs.SetResult(response));
            return tcs.Task.Result;
        }
    }
#endif

    internal class DevicePortalApiWrapper
    {
        public class Event
        {
            public int ID { get; set; }
            public int Keyword { get; set; }
            public int Level { get; set; }
            public string ProviderName { get; set; }
            public string StringMessage { get; set; }
            public string TaskName { get; set; }
            public object Timestamp { get; set; }
        }

        public class RootObject
        {
            public int Frequency { get; set; }
            public List<Event> Events { get; set; }
        }

        class ProgressStruct
        {
            public bool Success { get; set; }
            public string Reason { get; set; }
            public string CodeText { get; set; }
            public int? Code { get; set; }
        }

        class PackagesStruct
        {
            public List<PackageStruct> InstalledPackages { get; set; }
        }

        public class PackageStruct
        {
            public string PackageFullName { get; set; }
            public string PackageRelativeId { get; set; }
            public string Name { get; set; }
        }

        private readonly RestResponseCookie csrfToken;
        private readonly RestResponseCookie wmid;
        private readonly RestClient client;
        private readonly NetworkCredential credentials;

        public DevicePortalApiWrapper(string csrf, string wmid, string devicePortalAddress)
        {
            this.csrfToken = new RestResponseCookie()
            {
                Name = "CSRF-Token",
                Value = csrf
            };
            this.wmid = new RestResponseCookie()
            {
                Name = "WMID",
                Value = wmid
            };

            this.client = new RestClient(devicePortalAddress);
        }

        public DevicePortalApiWrapper(string username, string password, string devicePortalAddress, bool dummy = false)
        {
            this.credentials = new NetworkCredential(string.Format("auto-{0}", username), password);
            this.client = new RestClient(devicePortalAddress)
            {
                Authenticator = new NtlmAuthenticator(this.credentials)
            };
        }

        public DevicePortalApiWrapper(string pin, string devicePortalAddress)
        {
            this.client = new RestClient(devicePortalAddress);
            var csrfRequest = new RestRequest("authenticate.htm", Method.GET);
            var csrfResponse = client.Execute(csrfRequest);
            csrfToken = csrfResponse.Cookies.FirstOrDefault(x => x.Name == "CSRF-Token");

            var authorizeRequest = new RestRequest("api/authorize/pair", Method.POST);
            authorizeRequest.AddQueryParameter("pin", pin);
            authorizeRequest.AddQueryParameter("persistent", 1.ToString());

            var authorizeResponse = client.Execute(authorizeRequest);
            wmid = authorizeResponse.Cookies.FirstOrDefault(x => x.Name == "WMID");

            if (csrfToken == null || wmid == null)
            {
                throw new InvalidOperationException("Unable to authorize");
            }
        }

        private void AddAuthenticationCookiesIfNeeded(IRestRequest request)
        {
            if (csrfToken != null)
            {
                request.AddCookie(csrfToken.Name, csrfToken.Value);
                request.AddCookie(wmid.Name, wmid.Value);
                request.AddHeader("X-CSRF-Token", csrfToken.Value);
            }
        }

        public PackageStruct DeployApplication(string fullPath, Platform platform)
        {
            PackageStruct ret = this.GetBenchmarkDotnetPackage();
            if(ret != null)
            {
                this.DeleteApplication(ret);
            }

            var installRequest = new RestRequest("api/app/packagemanager/package", Method.POST);

            AddAuthenticationCookiesIfNeeded(installRequest);

            string certPath = Path.ChangeExtension(fullPath, ".cer");
            installRequest.AddFile(Path.GetFileName(certPath), certPath);
            installRequest.AddFile(Path.GetFileName(fullPath), fullPath);
            foreach(var dependency in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(fullPath), "Dependencies", platform.ToString())))
            {
                installRequest.AddFile(Path.GetFileName(dependency), dependency);
            }

            installRequest.AddQueryParameter("package", Path.GetFileName(fullPath));
            int i = 10;
            while (client.Execute(installRequest).StatusCode != HttpStatusCode.Accepted)
            {
                if (--i == 0)
                {
                    throw new InvalidOperationException("Install failed");
                }
            }

            IRestResponse<ProgressStruct> progress;
            for (;;)
            {
                var progressRequest = new RestRequest("api/app/packagemanager/state", Method.GET);

                AddAuthenticationCookiesIfNeeded(progressRequest);

                progress = client.Execute<ProgressStruct>(progressRequest);
                if (progress.Data != null && progress.Data.Code != null)
                {
                    break;
                }
            }

            if(progress.Data.Code != null && progress.Data.Code != 0)
            {
                throw new InvalidOperationException($"Install failed. Error code {progress.Data.Code}. Error text {progress.Data.CodeText}");
            }

            var packagesRequest = new RestRequest("api/app/packagemanager/packages", Method.GET);

            AddAuthenticationCookiesIfNeeded(packagesRequest);

            i = 10;
            while ((ret = this.GetBenchmarkDotnetPackage()) == null)
            {
                if (--i == 0)
                {
                    throw new InvalidOperationException("Installation failed. Package still not listed.");
                }

                Task.Delay(1000).Wait(); // Adding this because on local PC requests processed quite fast.
            }

            return ret;
        }

        PackageStruct GetBenchmarkDotnetPackage()
        {
           var packagesRequest = new RestRequest("api/app/packagemanager/packages", Method.GET);

            AddAuthenticationCookiesIfNeeded(packagesRequest);

            return client.Get<PackagesStruct>(packagesRequest)
                .Data.InstalledPackages.SingleOrDefault(x => x.Name.Equals("BenchmarkDotNet.Autogenerated"));
        }

        public void RunApplication(PackageStruct id)
        {
            var executeRequest = new RestRequest("api/taskmanager/app", Method.POST);

            AddAuthenticationCookiesIfNeeded(executeRequest);
            executeRequest.AddQueryParameter("appid", Convert.ToBase64String(Encoding.UTF8.GetBytes(id.PackageRelativeId)));
            {
                var response = client.Post<ProgressStruct>(executeRequest);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("Execute request failed: " + response.Data.CodeText + " " + response.Data.Reason);
                }
            }

            for (;;)
            {
                var processesRequest = new RestRequest("/api/resourcemanager/processes", Method.GET);

                AddAuthenticationCookiesIfNeeded(processesRequest);
                {
                    var response = client.Execute(processesRequest);
                    if (!response.Content.Contains(id.PackageFullName))
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }
            }
        }

        public void DeleteApplication(PackageStruct appId)
        {
            var deleteRequest = new RestRequest("api/app/packagemanager/package", Method.DELETE);
            deleteRequest.AddQueryParameter("package", appId.PackageFullName);
            
            AddAuthenticationCookiesIfNeeded(deleteRequest);

            HttpStatusCode status = client.Execute(deleteRequest).StatusCode;
            if (status != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed to delete package: " + status);
            }
        }

        internal ClientWebSocket StartListening()
        {
            var address = new Uri($"wss://{this.client.BaseUrl.Host}:{this.client.BaseUrl.Port}/api/etw/session/realtime");
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.Cookies = new CookieContainer();
            if (this.csrfToken != null)
            {
                ws.Options.SetRequestHeader("X-CSRF-Token", this.csrfToken.Value);
                ws.Options.Cookies.Add(this.client.BaseUrl, new Cookie("CSRF-Token", csrfToken.Value, "/", address.Host));
                ws.Options.Cookies.Add(this.client.BaseUrl, new Cookie("WMID", wmid.Value, "/", address.Host));
            }
            else
            {
                ws.Options.Credentials = this.credentials;
            }

            ws.Options.SetRequestHeader("Pragma", "no-cache");
            ws.Options.SetRequestHeader("Cache-Control", "no-cache");
            ws.Options.SetRequestHeader("Origin", $"https://{address.Host}");
            ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, sdch, br");
            ws.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
            ws.ConnectAsync(address, CancellationToken.None).Wait();

            var command = Encoding.UTF8.GetBytes("provider 4bd2826e-54a1-4ba9-bf63-92b73ea1ac4a enable 5");
            ws.SendAsync(new ArraySegment<byte>(command), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            return ws;
        }

        private static Task AsTask(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
            return tcs.Task;
        }

        internal string[] StopListening(ClientWebSocket ws)
        {
            List<string> ret = new List<string>();
            StringBuilder sb = new StringBuilder();
            var buffer = new byte[10240];
            for (;;)
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    var resTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    cts.CancelAfter(TimeSpan.FromMinutes(1));
                    try
                    {
                        var res = resTask.Result;
                        var str = Encoding.UTF8.GetString(buffer, 0, res.Count);
                        sb.Append(str);
                        if (res.EndOfMessage)
                        {
                            var @object = SimpleJson.SimpleJson.DeserializeObject<RootObject>(sb.ToString());
                            ret.AddRange(@object.Events.Select(x => x.TaskName));
                            sb.Clear();
                        }
                    }
                    catch (AggregateException ex)
                    {
                        if (ex.InnerException is OperationCanceledException)
                        {
                            break;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            
            return ret.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
    }
}
#endif