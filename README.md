# protoc-gen-turbolink
A protoc plugin to generate code for [turbolink](https://github.com/thejinchao/turbolink) 

## Compile
This project uses .NET T4  to generate code. When opening the project for the first time, you need to run custom tool on all template files (.tt)  
![run_custom_tool](https://github.com/thejinchao/turbolink/wiki/image/protoc-gen-turbolink_compile.png)

Install .NET 6.0 SDK : https://dotnet.microsoft.com/en-us/download/dotnet/6.0

And build
```
dotnet publish -c Release -r osx-arm64
dotnet publish -c Release -r win-x64
```

### Tips :: Running code generation on MacOS
Based on TurboLink Plugin 1.4.1
1. Convert `generate_code.cmd` to `generate_code.sh`. ChatGPT is your friend.
2. Get `protoc` 4.23.4 build from the [release page](https://github.com/protocolbuffers/protobuf/releases/tag/v23.4)
3. Build `grpc_cpp_plugin` 1.57.1 from source. Follow the [quickstart guide](https://grpc.io/docs/languages/cpp/quickstart/) for instructions.
4. Copy `bin/Release/osx-arm64/publish/protoc-gen-turbolink` to `Plugins/TurboLink/Tools/protoc-gen-turbolink`
5. `chmod +x protoc-gen-turbolink`

