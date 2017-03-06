# UAP runtime environment

BenchmarkDotNet allows you to run benchmark on remote mobile phone running Windows Phone 10.0 starting from 1511 version. Implementation is based on using Device Portal (DP) REST API. More about DP: https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal.

## How to enable on device
Perfectly described in https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal-mobile

## Job configuration
```cs
[UapJob("<device_portal_uri>", "<csrf_cookie>", "<wmid_cookie>", @"<BDN_UAP10.0_build output>")]
public class Algo_Md5VsSha256
{
   /* benchmarks removed for brevity */
}	
```

```cs
private IConfig CreateConfig()
{
	return ManualConfig.CreateEmpty()
		.With(Job.ShortRun.With(new UapRuntime("<device_portal_uri>", "<csrf_cookie>", "<wmid_cookie>", @"<BDN_UAP10.0_build output>"));
}
```

_device_portal_uri_ - URI of your device using which you can access DP.
_csrf_cookie_ and _wmid_cookie_ - cookies, that you should sniff through Fiddler (with HTTPS decryption on) *after* pairing device with PC.
_BDN_UAP10.0_build output_ - output folder of BenchmarkDotnet release uap 10.0 build. There should persist two assemblies: BenchmarkDotNet.Core and BenchmarkDotNet. 

Also beware that target application built for mobile device uses benchmark class as reference assembly, so you should not use not available APIs in your benchmark code. Perfectly use separate assembly/console as _runner_ and assembly with benchmark code build with nestandart target.

## Possible/known issues
1. Temporary build folder persists on disk after build. .NET Native optimizer consistently locks obj folder.
2. There may be problem with adding DP HTTPS certificate to trusted store. In this case Fiddler's proxying feature may be used as workaround.
