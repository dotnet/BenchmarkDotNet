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

namespace BenchmarkDotNet.Toolchains.Uap
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

        private RestResponseCookie csrfToken;
        private RestResponseCookie wmid;
        private readonly RestClient client;

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

        public PackageStruct DeployApplication(string fullPath)
        {
            var installRequest = new RestRequest("api/app/packagemanager/package", Method.POST);
            installRequest.AddCookie(csrfToken.Name, csrfToken.Value);
            installRequest.AddCookie(wmid.Name, wmid.Value);
            installRequest.AddFile(Path.GetFileName(fullPath), fullPath);
            foreach(var dependency in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(fullPath), "Dependencies", "ARM")))
            {
                installRequest.AddFile(Path.GetFileName(dependency), dependency);
            }

            installRequest.AddQueryParameter("package", Path.GetFileName(fullPath));
            installRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
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

                installRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
                installRequest.AddCookie(csrfToken.Name, csrfToken.Value);
                installRequest.AddCookie(wmid.Name, wmid.Value);

                progress = client.Execute<ProgressStruct>(progressRequest);
                if (progress.StatusCode != (HttpStatusCode)204)
                {
                    break;
                }
            }

            if (progress.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Install failed");
            }

            var packagesRequest = new RestRequest("api/app/packagemanager/packages", Method.GET);

            packagesRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
            packagesRequest.AddCookie(csrfToken.Name, csrfToken.Value);
            packagesRequest.AddCookie(wmid.Name, wmid.Value);
            packagesRequest.AddQueryParameter("_", GetTime().ToString());

            i = 10;
            PackageStruct ret;
            while ((ret = client.Get<PackagesStruct>(packagesRequest).Data.InstalledPackages.SingleOrDefault(x => x.Name.Contains("BenchmarkDotNet"))) == null)
            {
                if (--i == 0)
                {
                    throw new InvalidOperationException("Installation failed");
                }
            }

            return ret;
        }

        public void RunApplication(PackageStruct id)
        {
            var executeRequest = new RestRequest("api/taskmanager/app", Method.POST);

            executeRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
            executeRequest.AddCookie(csrfToken.Name, csrfToken.Value);
            executeRequest.AddCookie(wmid.Name, wmid.Value);
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

                processesRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
                processesRequest.AddCookie(csrfToken.Name, csrfToken.Value);
                processesRequest.AddCookie(wmid.Name, wmid.Value);
                {
                    var response = client.Execute(processesRequest);
                    if (!response.Content.Contains("BenchmarkDotNet"))
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

            deleteRequest.AddHeader("X-CSRF-Token", csrfToken.Value);
            deleteRequest.AddCookie(csrfToken.Name, csrfToken.Value);
            deleteRequest.AddCookie(wmid.Name, wmid.Value);

            HttpStatusCode status = client.Execute(deleteRequest).StatusCode;
            if (status != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed to delete package: " + status);
            }
        }

        private static long GetTime()
        {
            long retval = 0;
            var st = new DateTime(1970, 1, 1);
            TimeSpan t = (DateTime.Now.ToUniversalTime() - st);
            retval = (long)(t.TotalMilliseconds + 0.5);
            return retval;
        }

        internal ClientWebSocket StartListening()
        {
            var address = new Uri($"wss://{this.client.BaseUrl.Host}/api/etw/session/realtime");
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.Cookies = new CookieContainer();
            ws.Options.SetRequestHeader("X-CSRF-Token", this.csrfToken.Value);

            //ws.Options.SetRequestHeader("Host", address.Host);
            ws.Options.SetRequestHeader("Pragma", "no-cache");
            ws.Options.SetRequestHeader("Cache-Control", "no-cache");
            ws.Options.SetRequestHeader("Origin", $"https://{address.Host}");
            ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, sdch, br");
            ws.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");

            ws.Options.Cookies.Add(this.client.BaseUrl, new Cookie("CSRF-Token", csrfToken.Value, "/", address.Host));
            ws.Options.Cookies.Add(this.client.BaseUrl, new Cookie("WMID", wmid.Value, "/", address.Host));
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
            var buffer = new byte[10240];
            for (;;)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var resTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                try
                {
                    resTask.Wait();
                    var str = Encoding.UTF8.GetString(buffer);
                    var @object = SimpleJson.SimpleJson.DeserializeObject<RootObject>(str);
                    ret.AddRange(@object.Events.Select(x => x.StringMessage));
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

            return ret.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
    }
}
#endif