using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;

namespace protoc_gen_turbolink
{
	class TurboLinkUtils
	{
        public static string MakeCamelString(string inputString)
        {
            if (inputString.Length == 0) return string.Empty;
            return char.ToUpper(inputString[0]) + inputString.Substring(1);
        }
        public static string[] MakeCamelStringArray(string[] inputStringArray)
		{
            if (inputStringArray.Length == 0) return inputStringArray;
            return inputStringArray.Select(world => char.ToUpper(world[0]) + world.Substring(1)).ToArray();
		}
        public static string JoinCamelString(string[] inputStringArray, string connection)
		{
            if (inputStringArray.Length == 0) return string.Empty;
            return string.Join(connection, MakeCamelStringArray(inputStringArray)) + connection;
        }
        //eg. "common.proto" -> "Common"
        //eg. "google/protobuf/field_mask.proto" -> "FieldMask"
        public static string GetCamelFileName(string input)
        {
            string fileName = input.Split('/').ToArray().Last();
            fileName = fileName.Split('.').ToArray().First();   // remove extension
            var words = fileName.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(string.Empty, MakeCamelStringArray(words));
        }
        public static string GetMessageName(string grpcName, string prefix="FGrpc")
        {
            //eg.  ".Time.NowResponse"  -> "FGrpcTimeNowResponse"
            //eg.  "authzed.api.v1.CheckRequest" => "FGrpcAuthzedApiV1CheckRequest"
            string[] words = grpcName.Split('.').ToArray();
            string result = prefix;// "FGrpc";
            foreach (string word in words)
            {
                if (word.Length > 0)
                    result += char.ToUpper(word[0]) + word.Substring(1);
            }
            return result;
        }
        public static string GetFieldType(FieldDescriptorProto field)
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
                    ueType += GetMessageName(field.TypeName, "EGrpc"); break;
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
        public static string GetMessageFieldName(FieldDescriptorProto field)
        {
            //convert json name first letter to upper
            //eg. "some_thing" -> "SomeThing"
            string fieldName = field.JsonName;
            if (fieldName.Length > 0)
            {
                return fieldName[0].ToString().ToUpper() + fieldName.Substring(1);
            }
            return fieldName;
        }
        public static string GetFieldDefaultValue(FieldDescriptorProto field)
        {
            string defaultValue = " = ";
            switch (field.Type)
            {
                case FieldDescriptorProto.Types.Type.Double:
                case FieldDescriptorProto.Types.Type.Float:
                case FieldDescriptorProto.Types.Type.Int64:
                case FieldDescriptorProto.Types.Type.Uint64:
                case FieldDescriptorProto.Types.Type.Int32:
                case FieldDescriptorProto.Types.Type.Fixed64:
                case FieldDescriptorProto.Types.Type.Fixed32:
                case FieldDescriptorProto.Types.Type.Uint32:
                case FieldDescriptorProto.Types.Type.Sfixed32:
                case FieldDescriptorProto.Types.Type.Sfixed64:
                case FieldDescriptorProto.Types.Type.Sint32:
                case FieldDescriptorProto.Types.Type.Sint64:
                    defaultValue += "0"; break;
                case FieldDescriptorProto.Types.Type.Bool:
                    defaultValue += "false"; break;
                case FieldDescriptorProto.Types.Type.String:
                    defaultValue += "\"\""; break;
                case FieldDescriptorProto.Types.Type.Enum:
                    defaultValue += "static_cast<" + GetFieldType(field) + ">(0)"; break;
                default:
                    return "";
            }
            return defaultValue;
        }
        public static (bool, FieldDescriptorProto, FieldDescriptorProto) IsMapField(FieldDescriptorProto field, DescriptorProto message)
        {
            if (field.Label != FieldDescriptorProto.Types.Label.Repeated) return (false, null, null);
            if (field.Type != FieldDescriptorProto.Types.Type.Message) return (false, null, null);
            int lastdot = field.TypeName.LastIndexOf('.');
            if (lastdot > 0)
            {
                string mapEntryName = field.TypeName.Substring(lastdot + 1);
                //search in nested message
                foreach (DescriptorProto messageInMsg in message.NestedType)
                {
                    if (messageInMsg.Name == mapEntryName &&
                         messageInMsg.Options != null && messageInMsg.Options.MapEntry &&
                         messageInMsg.Field.Count() == 2)
                    {
                        if (messageInMsg.Field[0].Name == "key" && messageInMsg.Field[1].Name == "value")
                        {
                            return (true, messageInMsg.Field[0], messageInMsg.Field[1]);
                        }
                        else if (messageInMsg.Field[1].Name == "key" && messageInMsg.Field[0].Name == "value")
                        {
                            return (true, messageInMsg.Field[1], messageInMsg.Field[0]);
                        }
                    }
                }
            }
            return (false, null, null);
        }
        public static string GetContextSuperClass(MethodDescriptorProto method)
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
    }
}
