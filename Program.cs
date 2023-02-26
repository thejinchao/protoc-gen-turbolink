using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;

namespace protoc_gen_turbolink
{
    class Program
    {
        static void Main(string[] args)
        {
            //read code generator request from stdin.
            CodedInputStream inputStream = new CodedInputStream(Console.OpenStandardInput());
            CodeGeneratorRequest request = new CodeGeneratorRequest();
            request.MergeFrom(inputStream);

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

            foreach (FileDescriptorProto protoFile in request.ProtoFile)
            {
                TurboLinkGenerator generator = new TurboLinkGenerator(protoFile, collection.GrpcServiceFiles[protoFile.Name]);
                generator.BuildOutputFiles();

                foreach (GeneratedFile generatedFile in generator.GeneratedFiles)
                {
                    CodeGeneratorResponse.Types.File newFile = new CodeGeneratorResponse.Types.File();

                    newFile.Name = generatedFile.FileName;
                    newFile.Content = generatedFile.Content;
                    response.File.Add(newFile);
                }
            }
            /*
            //dump input request
            CodeGeneratorResponse.Types.File request_file = new CodeGeneratorResponse.Types.File();
            request_file.Name = collection.InputFileNames + "request.json";
            request_file.Content = request.ToString();
            response.File.Add(request_file);
            
            //dump service collection
            CodeGeneratorResponse.Types.File service_collection = new CodeGeneratorResponse.Types.File();
            service_collection.Name = collection.InputFileNames + "collection.json";
            service_collection.Content = collection.DumpToString();
            response.File.Add(service_collection);
            */
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
