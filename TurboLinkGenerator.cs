using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;

namespace protoc_gen_turbolink
{
    public struct GeneratedFile
    {
        public string FileName;
        public string Content;
    }
    public class TurboLinkGenerator
    {
        public FileDescriptorProto ProtoFile;
        public string PackageName;
        public string FileName;

        public List<GeneratedFile> GeneratedFiles = new List<GeneratedFile>();
        public TurboLinkGenerator(FileDescriptorProto protoFile)
        {
            ProtoFile = protoFile;
        }
        string GetCamelPackageName(string input)
        {
            string[] words = input.Split('.').ToArray();
            string result = "";
            foreach (string word in words)
            {
                result += char.ToUpper(word[0]) + word.Substring(1);
            }
            return result;
        }
        string GetCamelFileName(string input)
        {
            string fileName = input.Split('/').ToArray().Last(); 
            fileName = fileName.Split('.').ToArray().First();   // remove extension
            var words = fileName.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
            words = words
                .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                .ToArray();
            return string.Join(string.Empty, words);
        }

        public bool Prepare()
		{
            PackageName = GetCamelPackageName(ProtoFile.Package);
            FileName = GetCamelFileName(ProtoFile.Name);
            return true;
        }
        public void BuildOutputFiles()
        {
            GeneratedFile file;

            // xxxMarshaling.h
            Template.MarshalingH marshalingHTemplate = new Template.MarshalingH(this);
            file = new GeneratedFile();
            file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Marshaling.h");
            file.Content = marshalingHTemplate.TransformText();
            GeneratedFiles.Add(file);

            // xxxMarshaling.cpp
            Template.MarshalingCPP marshalingCPPTemplate = new Template.MarshalingCPP(this);
            file = new GeneratedFile();
            file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Marshaling.cpp");
            file.Content = marshalingCPPTemplate.TransformText();
            GeneratedFiles.Add(file);

            // xxxMessage.h
            Template.MessageH messageHTemplate = new Template.MessageH(this);
            file.FileName = Path.Combine("Public", "S" + PackageName, FileName + "Message.h");
            file.Content = messageHTemplate.TransformText();
            GeneratedFiles.Add(file);

            if (ProtoFile.Service.Count > 0)
            {
                // xxxService.h
                Template.ServiceH serviceHTemplate = new Template.ServiceH(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Public", "S" + PackageName, FileName + "Service.h");
                file.Content = serviceHTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxClient.h
                Template.ClientH clientHTemplate = new Template.ClientH(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Public", "S" + PackageName, FileName + "Client.h");
                file.Content = clientHTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxClient.cpp
                Template.ClientCPP clientCPPTemplate = new Template.ClientCPP(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Client.cpp");
                file.Content = clientCPPTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxServicePrivate.h
                Template.ServicePrivateH servicePrivateHTemplate = new Template.ServicePrivateH(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Service_Private.h");
                file.Content = servicePrivateHTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxServicePrivate.cpp
                Template.ServicePrivateCPP servicePrivateCPPTemplate = new Template.ServicePrivateCPP(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Service_Private.cpp");
                file.Content = servicePrivateCPPTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxContext.h
                Template.ContextH contextHTemplate = new Template.ContextH(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Context.h");
                file.Content = contextHTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxContext.cpp
                Template.ContextCPP contextCPPTemplate = new Template.ContextCPP(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Context.cpp");
                file.Content = contextCPPTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxService.cpp
                Template.ServiceCPP serviceCPPTemplate = new Template.ServiceCPP(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Service.cpp");
                file.Content = serviceCPPTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxNode.h
                Template.NodeH nodeHTemplate = new Template.NodeH(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Public", "S" + PackageName, FileName + "Node.h");
                file.Content = nodeHTemplate.TransformText();
                GeneratedFiles.Add(file);

                // xxxNode.cpp
                Template.NodeCPP nodeCPPTemplate = new Template.NodeCPP(this);
                file = new GeneratedFile();
                file.FileName = Path.Combine("Private", "S" + PackageName, FileName + "Node.cpp");
                file.Content = nodeCPPTemplate.TransformText();
                GeneratedFiles.Add(file);
            }
        }
        public string GetFieldType(FieldDescriptorProto field)
		{
            string ueType = "";
            switch (field.Type)
            {
                case FieldDescriptorProto.Types.Type.Double:
                    ueType += "FDouble64"; break;
                case FieldDescriptorProto.Types.Type.Float:
                    ueType += "float"; break;
                case FieldDescriptorProto.Types.Type.Int64:
                    ueType += "FInt64"; break;
                case FieldDescriptorProto.Types.Type.Uint64:
                    ueType += "FUInt64"; break;
                case FieldDescriptorProto.Types.Type.Int32:
                    ueType += "int32"; break;
                case FieldDescriptorProto.Types.Type.Fixed64:
                    ueType += "FUInt64"; break;
                case FieldDescriptorProto.Types.Type.Fixed32:
                    ueType += "FUInt32"; break;
                case FieldDescriptorProto.Types.Type.Bool:
                    ueType += "bool"; break;
                case FieldDescriptorProto.Types.Type.String:
                    ueType += "FString"; break;
                case FieldDescriptorProto.Types.Type.Group:
                    break; //TODO
                case FieldDescriptorProto.Types.Type.Message:
                    ueType += GetMessageName(field.TypeName); break;
                case FieldDescriptorProto.Types.Type.Bytes:
                    ueType += "FBytes"; break;
                case FieldDescriptorProto.Types.Type.Uint32:
                    ueType += "FUInt32"; break;
                case FieldDescriptorProto.Types.Type.Enum:
                    ueType += GetMessageName(field.TypeName).Replace("FGrpc", "EGrpc"); break;
                case FieldDescriptorProto.Types.Type.Sfixed32:
                    ueType += "int32"; break;
                case FieldDescriptorProto.Types.Type.Sfixed64:
                    ueType += "FInt64"; break;
                case FieldDescriptorProto.Types.Type.Sint32:
                    ueType += "int32"; break;
                case FieldDescriptorProto.Types.Type.Sint64:
                    ueType += "FInt64"; break;
                default:
                    ueType += "ERROR_TYPE"; break;
            }
            return ueType;
        }
        public string GetFieldName(FieldDescriptorProto field)
        {
            //EXAMPLE: some_thing => SomeThing
            //convert json name first letter to upper
            string fieldName = field.JsonName;
            if (fieldName.Length > 0)
            {
                return fieldName[0].ToString().ToUpper() + fieldName.Substring(1);
            }
            return fieldName;
        }
        public string GetMessageName(string valueType)
        {
            //EXAMPLE: .Time.NowResponse  => FGrpcTimeNowResponse
            //EXAMPLE: authzed.api.v1.CheckRequest => AuthzedApiV1CheckRequest
            string[] words = valueType.Split('.').ToArray();
            string result = "FGrpc";
            foreach (string word in words)
            {
                if (word.Length > 0)
                    result += char.ToUpper(word[0]) + word.Substring(1);
            }
            return result;
        }
        public string GetMessageGrpcName(string valueType)
        {
            //EXAMPLE: .Time.NowResponse  => ::Time::NowResponse
            return valueType.Replace(".", "::");
        }
        public string GetFieldGrpcName(FieldDescriptorProto field)
        {
            //EXAMPLE: some_Thing => some_thing
            return field.Name.ToLower();
        }
        public string GetContextType(MethodDescriptorProto method)
        {
            if (method.ClientStreaming && method.ServerStreaming)
            {
                return "GrpcContext_Stream_Stream";
            }
            else if (method.ClientStreaming && !method.ServerStreaming)
            {
                return "NOT_SUPPORT_YET";
            }
            else if (!method.ClientStreaming && method.ServerStreaming)
            {
                return "GrpcContext_Ping_Stream";
            }
            else
            {
                return "GrpcContext_Ping_Pong";
            }
        }
        public int GetNoneClientStreamingFunctionCounts(FileDescriptorProto fileDesc)
        {
            int counts = 0;
            foreach (ServiceDescriptorProto service in fileDesc.Service)
            {
                foreach (MethodDescriptorProto method in service.Method)
                {
                    if (!method.ClientStreaming)
                    {
                        counts++;
                    }
                }
            }
            return counts;
        }
        public string PrintNameList(List<string> parentNameList, string dotChar)
        {
            StringBuilder output = new StringBuilder();
            foreach (string n in parentNameList)
            {
                output.Append(n);
                if (dotChar != string.Empty) output.Append(dotChar);
            }
            if (dotChar!=string.Empty && output.Length > dotChar.Length)
                return output.Remove(output.Length - dotChar.Length, dotChar.Length).ToString();

            return output.ToString();
        }
    }
}
