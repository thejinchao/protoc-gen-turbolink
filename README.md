# protoc-gen-turbolink
A protoc plugin to generate code for [turbolink](https://github.com/thejinchao/turbolink) 

## Compile

This project uses .NET T4  to generate code. When opening the project for the first time, you need to run custom tool on all template files (.tt)  
![run_custom_tool](https://github.com/thejinchao/turbolink/wiki/image/protoc-gen-turbolink_compile.png)

## With Rider on macOS

1. Install dependencies
    ```bash
    brew install mono dotnet-sdk
    dotnet tool install -g dotnet-t4
    ```
1. Open solution in Rider
1. In the Solution browser, go to the `Template` folder, right-click each .tt file and select _Preprocess Template_
