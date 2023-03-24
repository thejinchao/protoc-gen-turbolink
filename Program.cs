using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;

namespace protoc_gen_turbolink
{
    class Program
    {
        static bool GetParam(Dictionary<string, string> paramDictionary, string paramName, bool defaultValue)
		{
            if (!paramDictionary.ContainsKey(paramName))
            {
                return defaultValue;
            }
            string paramValue = paramDictionary[paramName].ToLower();
            if (paramValue != "true" && paramValue != "false")
            {
                return defaultValue;
            }
            return paramValue == "true" ? true : false;
        }
        static void Main(string[] args)
        {
            //read code generator request from stdin.
            CodedInputStream inputStream = new CodedInputStream(Console.OpenStandardInput());
            CodeGeneratorRequest request = new CodeGeneratorRequest();
            request.MergeFrom(inputStream);

            //read request param
            bool dumpRequest = false;
            bool dumpCollection = false;
            bool generateServiceCode = true;
            bool generateJsonCode = false;

            if (request.HasParameter)
			{
                Dictionary<string, string> paramDictionary = request.Parameter
                    .Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                    .GroupBy(param => param.Split('=')[0].Trim(), param => param.Split('=')[1].Trim())
                    .ToDictionary(x => x.Key, x => x.First());

                dumpRequest = GetParam(paramDictionary, "DumpRequest", false);
                dumpCollection = GetParam(paramDictionary, "DumpCollection", false);
                generateServiceCode = GetParam(paramDictionary, "GenerateServiceCode", true);
                generateJsonCode = GetParam(paramDictionary, "GenerateJsonCode", false);
            }

            //create code generator reponse
            CodeGeneratorResponse response = new CodeGeneratorResponse();
            //supported features(optional field)
            response.SupportedFeatures = (ulong)CodeGeneratorResponse.Types.Feature.Proto3Optional;

            //gather and analysis information from all service files
            TurboLinkCollection collection = new TurboLinkCollection();
            string error;
            if (!collection.AnalysisServiceFiles(request, out error))
            {
                //no go!
                response.Error = error;
                WriteResponse(response);
                return;
            }

            foreach (GrpcServiceFile serviceFile in collection.GrpcServiceFiles.Values)
            {
                TurboLinkGenerator generator = new TurboLinkGenerator(serviceFile.ProtoFileDesc, serviceFile);
                generator.BuildOutputFiles(generateServiceCode, generateJsonCode);

                foreach (GeneratedFile generatedFile in generator.GeneratedFiles)
                {
                    CodeGeneratorResponse.Types.File newFile = new CodeGeneratorResponse.Types.File();

                    newFile.Name = generatedFile.FileName;
                    newFile.Content = generatedFile.Content;
                    response.File.Add(newFile);
                }
            }

            //dump input request
            if (dumpRequest)
            {
                CodeGeneratorResponse.Types.File request_file = new CodeGeneratorResponse.Types.File();
                request_file.Name = collection.InputFileNames + "request.json";
                request_file.Content = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
                response.File.Add(request_file);
            }

            //dump service collection
            if (dumpCollection)
            {
                CodeGeneratorResponse.Types.File service_collection = new CodeGeneratorResponse.Types.File();
                service_collection.Name = collection.InputFileNames + "collection.json";
                service_collection.Content = JsonSerializer.Serialize(collection.GrpcServiceFiles, new JsonSerializerOptions { WriteIndented = true });
                response.File.Add(service_collection);
            }
            
            WriteResponse(response);
        }
        private static void WriteResponse(CodeGeneratorResponse response)
		{
            //write response from standard output to grpc
            byte[] data = new byte[response.CalculateSize()];
            Google.Protobuf.CodedOutputStream outputStream = new Google.Protobuf.CodedOutputStream(data);
            response.WriteTo(outputStream);

            Console.OpenStandardOutput().Write(data, 0, response.CalculateSize());
        }
    }
}
