# Debugging

There should be two debug profiles available in VS drop down

![](https://cloud.githubusercontent.com/assets/6011991/15627671/89f2405a-24eb-11e6-8bd1-c9d45613e0f6.png "Debug profiles")

However due to VS 2017 RC4 bug it seems that it's impossible to choose as of 2/19/2017.  If you want to change it then please edit the project file (.csproj) and set your order - the first framework moniker is always used. `<TargetFrameworks>netcoreapp1.1;net46</TargetFrameworks>`