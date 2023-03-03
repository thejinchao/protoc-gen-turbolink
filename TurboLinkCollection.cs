using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Text.Json;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;
using Google.Protobuf.Collections;

namespace protoc_gen_turbolink
{
	public struct GrpcEnumField
	{
		public string Name { get; set; }				//eg. "Male", "Female"
		public int Number { get; set; }					//eg. "0", "1"
	}
	public struct GrpcEnum
	{
		public string Name { get; set; }                //eg. "EGrpcCommonGender"
		public string DisplayName { get; set; }         //eg. "Common.Gender"
		public List<GrpcEnumField> Fields { get; set; }
	}

	abstract public class GrpcMessageField
	{
		public FieldDescriptorProto FieldDesc;
		public GrpcMessageField(FieldDescriptorProto fieldDesc)
		{
			FieldDesc = fieldDesc;
			NeedNativeMake = false;
		}
		public abstract string FieldType						//eg. "int32", "FString", "EGrpcCommonGender", "TArray<FGrpcUserRegisterRequestAddress>"
		{
			get;
		}
		public string FieldGrpcType								//eg. "::Common::Gender", "::google::protobuf::Value"
		{
			get => FieldDesc.TypeName.Replace(".", "::");
		}
		public string FieldName									//eg. "Age", "MyName", "Gender", "AddressArray"
		{
			get => TurboLinkUtils.GetMessageFieldName(FieldDesc);
		}
		public string FieldGrpcName								//eg. "age", "my_name", "gender", "address_array"
		{
			get => FieldDesc.Name.ToLower();
		}
		public abstract string TypeAsNativeField                //eg. "TSharedPtr<FGrpcUserRegisterRequestAddress>", "TArray<TSharedPtr<FGrpcUserRegisterRequestAddress>>"
		{
			get;
		}
		public virtual string FieldDefaultValue
		{
			get => string.Empty;
		}
		public bool NeedNativeMake { get; set; }
	}
	public class GrpcMessageField_Single : GrpcMessageField
	{
		public GrpcMessageField_Single(FieldDescriptorProto fieldDesc) : base(fieldDesc)
		{ }
		public override string FieldType
		{
			get => TurboLinkUtils.GetFieldType(FieldDesc);
		}
		public override string TypeAsNativeField
		{
			get => NeedNativeMake ? ("TSharedPtr<" + TurboLinkUtils.GetFieldType(FieldDesc) + ">") : FieldType;
		}
		public override string FieldDefaultValue				//eg. "=0", '=""', "= static_cast<EGrpcCommonGender>(0)", ""
		{
			get => TurboLinkUtils.GetFieldDefaultValue(FieldDesc);
		}
	}
	public class GrpcMessageField_Repeated : GrpcMessageField
	{
		public GrpcMessageField ItemField;
		public GrpcMessageField_Repeated(FieldDescriptorProto fieldDesc) : base(fieldDesc)
		{
			ItemField = new GrpcMessageField_Single(fieldDesc);
		}
		public override string FieldType
		{
			get => "TArray<" + ItemField.FieldType + ">";
		}
		public override string TypeAsNativeField
		{
			get => NeedNativeMake ? ("TArray<TSharedPtr<" + ItemField.FieldType + ">>") : FieldType;
		}
	}
	public class GrpcMessageField_Map : GrpcMessageField
	{
		public GrpcMessageField KeyField;
		public GrpcMessageField ValueField;
		public GrpcMessageField_Map(FieldDescriptorProto fieldDesc, FieldDescriptorProto keyField, FieldDescriptorProto valueField) : base(fieldDesc)
		{
			KeyField = new GrpcMessageField_Single(keyField);
			ValueField = new GrpcMessageField_Single(valueField);
		}
		public override string FieldType
		{
			get => "TMap<" + KeyField.FieldType + ", " + ValueField.FieldType + ">";
		}
		public override string TypeAsNativeField
		{
			get => NeedNativeMake ? ("TMap<" + KeyField.FieldType + ", TSharedPtr<" + ValueField.FieldType + ">>") : FieldType;
		}
	}

	public class GrpcMessage
	{
		public readonly DescriptorProto MessageDesc;
		public readonly GrpcServiceFile ServiceFile;
		public int Index { get; set; }
		public string Name                                      //eg. "FGrpcGreeterHelloResponse",  "FGrpcGoogleProtobufValue"
		{
			get => "FGrpc" +
				ServiceFile.CamelPackageName +
				TurboLinkUtils.JoinCamelString(ParentMessageNameList, string.Empty) +
				TurboLinkUtils.MakeCamelString(MessageDesc.Name);
		}
		public string GrpcName                                  //eg. "Greeter::HelloResponse", "google::protobuf::Value"
		{
			get => ServiceFile.GrpcPackageName + "::" +
				TurboLinkUtils.JoinString(ParentMessageNameList, "::") +
				MessageDesc.Name;
		}
		public string DisplayName                               //eg. "Greeter.HelloResponse", "GoogleProtobuf.Value"
		{
			get => ServiceFile.CamelPackageName + "." +
				TurboLinkUtils.JoinCamelString(ParentMessageNameList, ".") +
				TurboLinkUtils.MakeCamelString(MessageDesc.Name);
		}
		public string[] ParentMessageNameList;
		public List<GrpcMessageField> Fields { get; set; }
		public bool HasNativeMake { get; set; }
		public GrpcMessage(DescriptorProto messageDesc, GrpcServiceFile serviceFile)
		{
			MessageDesc = messageDesc;
			ServiceFile = serviceFile;
			Index = serviceFile.MessageArray.Count;
			Fields = new List<GrpcMessageField>();
			HasNativeMake = false;
		}
	}
	public class GrpcServiceMethod
	{
		public MethodDescriptorProto MethodDesc;
		public string Name
		{
			get => MethodDesc.Name;
		}
		public bool ClientStreaming
		{
			get => MethodDesc.ClientStreaming;
		}
		public bool ServerStreaming
		{
			get => MethodDesc.ServerStreaming;
		}
		public string InputType                             //eg. "FGrpcUserRegisterRequest"
		{
			get => TurboLinkUtils.GetMessageName(MethodDesc.InputType);
		}
		public string GrpcInputType                         //eg. "::User::RegisterRequest"
		{
			get => MethodDesc.InputType.Replace(".", "::");
		}
		public string OutputType                            //eg. "FGrpcUserRegisterResponse"
		{
			get => TurboLinkUtils.GetMessageName(MethodDesc.OutputType);
		}
		public string GrpcOutputType                        //eg. "::User::RegisterResponse"
		{
			get => MethodDesc.OutputType.Replace(".", "::");
		}
		public string ContextSuperClass                     //eg. "GrpcContext_Ping_Pong", "GrpcContext_Ping_Stream"
		{
			get => TurboLinkUtils.GetContextSuperClass(MethodDesc);
		}
		public GrpcServiceMethod(MethodDescriptorProto methodDesc)
		{
			MethodDesc = methodDesc;
		}
	}
	public class GrpcService
	{
		public readonly ServiceDescriptorProto ServiceDesc;
		public string Name                                      //eg. "UserService"
		{
			get => ServiceDesc.Name;
		}
		public List<GrpcServiceMethod> MethodArray { get; set; }

		public GrpcService(ServiceDescriptorProto serviceDesc)
		{
			ServiceDesc = serviceDesc;
		}
	}
	public class GrpcServiceFile
	{
		public readonly FileDescriptorProto ProtoFileDesc;
		//split package name as string array
		public readonly string[] PackageNameAsList;
		public string FileName								//eg. "hello.proto", "google/protobuf/struct.proto"
		{
			get => ProtoFileDesc.Name;
		}
		public string CamelFileName                         //eg. "Hello", "Struct"
		{
			get => TurboLinkUtils.GetCamelFileName(FileName);
		}
		public string PackageName                           //eg. "Greeter", "google.protobuf"
		{
			get => ProtoFileDesc.Package;
		}
		public string CamelPackageName                      //eg. "Greeter", "GoogleProtobuf"
		{
			get => string.Join(string.Empty, TurboLinkUtils.MakeCamelStringArray(PackageNameAsList));
		}
		public string GrpcPackageName                       //eg. "Greeter", "google::protobuf"
		{
			get => string.Join("::", PackageNameAsList);
		}
		public string TurboLinkBasicFileName                //eg. "SGreeter/Hello", "SGoogleProtobuf/Struct"
		{
			get => "S" + CamelPackageName + "/" + CamelFileName;
		}
		public List<string> DependencyFiles { get; set; }
		public List<GrpcEnum> EnumArray { get; set; }
		public List<GrpcMessage> MessageArray { get; set; }
		public List<GrpcService> ServiceArray { get; set; }
		public Dictionary<string, int> Message2IndexMap { get; set; }
		public GrpcServiceFile(FileDescriptorProto protoFileDesc)
		{
			ProtoFileDesc = protoFileDesc;
			PackageNameAsList = PackageName.Split('.').ToArray();
			Message2IndexMap = new Dictionary<string, int>();
		}
	}
	public class TurboLinkCollection
	{
		public string InputFileNames;
		//key=ProtoFileName
		public Dictionary<string, GrpcServiceFile> GrpcServiceFiles = new Dictionary<string, GrpcServiceFile>();

		public bool AnalysisServiceFiles(CodeGeneratorRequest request, out string error)
		{
			error = null;

			StringBuilder inputFileNames = new StringBuilder();

			//step 1: gather service information
			foreach (FileDescriptorProto protoFile in request.ProtoFile)
			{
				GrpcServiceFiles.Add(protoFile.Name, new GrpcServiceFile(protoFile));
				inputFileNames.Insert(0, System.IO.Path.GetFileNameWithoutExtension(protoFile.Name) + "_");
			}
			InputFileNames = inputFileNames.ToString();

			//step 2: imported proto files
			foreach (string protoFileName in GrpcServiceFiles.Keys.ToList())
			{
				AddDependencyFiles(protoFileName);
			}

			//setp 3: enum (include nested enum)
			foreach (string protoFileName in GrpcServiceFiles.Keys.ToList())
			{
				AddEnums(protoFileName);
			}

			//step 4: message(include nested message)
			foreach (string protoFileName in GrpcServiceFiles.Keys.ToList())
			{
				AddMessages(protoFileName);
			}
			//step 5: service
			foreach (string protoFileName in GrpcServiceFiles.Keys.ToList())
			{
				AddServices(protoFileName);
			}
			//step 6: scan message field to analyze the interdependencies between messages
			foreach (string protoFileName in GrpcServiceFiles.Keys.ToList())
			{
				AnalyzeMessage(protoFileName);
			}

			return true;
		}
		private void AddDependencyFiles(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];

			serviceFile.DependencyFiles = new List<string>();
			foreach (string dependency in serviceFile.ProtoFileDesc.Dependency)
			{
				//set dependency file as turbolink base name, eg. "SGoogleProtobuf/Struct"
				serviceFile.DependencyFiles.Add(GrpcServiceFiles[dependency].TurboLinkBasicFileName);
			}
			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		private void AddEnums(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];
			serviceFile.EnumArray = new List<GrpcEnum>();

			//iterate enum in protofile
			foreach (EnumDescriptorProto protoEnum in serviceFile.ProtoFileDesc.EnumType)
			{
				AddEnum(ref serviceFile, 
					string.Join(string.Empty,	"EGrpc", serviceFile.CamelPackageName, protoEnum.Name),
					string.Join(".",			serviceFile.CamelPackageName, protoEnum.Name),
					protoEnum.Value);
			}

			//iterate nested enum in message
			foreach (DescriptorProto message in serviceFile.ProtoFileDesc.MessageType)
			{
				foreach (EnumDescriptorProto protoEnum in message.EnumType)
				{
					AddEnum(ref serviceFile,
						string.Join(string.Empty,	"EGrpc", serviceFile.CamelPackageName, message.Name, protoEnum.Name),
						string.Join(".",			serviceFile.CamelPackageName, message.Name, protoEnum.Name),
						protoEnum.Value);
				}
			}

			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		private void AddEnum(ref GrpcServiceFile serviceFile, string name, string displayName, RepeatedField<EnumValueDescriptorProto> enumFields)
		{
			GrpcEnum newEnum = new GrpcEnum();
			newEnum.Name = name;
			newEnum.DisplayName = displayName;
			newEnum.Fields = new List<GrpcEnumField>();

			foreach (EnumValueDescriptorProto enumValue in enumFields)
			{
				GrpcEnumField newEnumField = new GrpcEnumField();
				newEnumField.Name = enumValue.Name;
				newEnumField.Number = enumValue.Number;
				newEnum.Fields.Add(newEnumField);
			}
			serviceFile.EnumArray.Add(newEnum);
		}
		private void AddMessages(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];
			serviceFile.MessageArray = new List<GrpcMessage>();

			string[] parentNameList = new string[] { };
			foreach (DescriptorProto protoMessage in serviceFile.ProtoFileDesc.MessageType)
			{
				AddMessage(ref serviceFile, parentNameList, protoMessage);
			}
			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		private void AddMessage(ref GrpcServiceFile serviceFile, string[] parentMessageNameList, DescriptorProto protoMessage)
		{
			//add nested message first
			if (protoMessage.NestedType.Count > 0)
			{
				string[] currentMessageNameList = new string[parentMessageNameList.Length + 1];
				parentMessageNameList.CopyTo(currentMessageNameList, 0);
				currentMessageNameList[parentMessageNameList.Length] = protoMessage.Name;
				
				foreach (DescriptorProto nestedProtoMessage in protoMessage.NestedType)
				{
					if (nestedProtoMessage.Options != null && nestedProtoMessage.Options.MapEntry) continue;
					AddMessage(ref serviceFile, currentMessageNameList, nestedProtoMessage);
				}
			}

			GrpcMessage message = new GrpcMessage(protoMessage, serviceFile);
			message.ParentMessageNameList = parentMessageNameList;
			serviceFile.Message2IndexMap.Add(
				"." + serviceFile.PackageName + "." +
				TurboLinkUtils.JoinString(parentMessageNameList, ".") +
				protoMessage.Name, 
				message.Index);

			foreach (FieldDescriptorProto field in protoMessage.Field)
			{
				bool isMapField;
				FieldDescriptorProto keyField, valueField;
				(isMapField, keyField, valueField) = TurboLinkUtils.IsMapField(field, protoMessage);
				if (isMapField)
				{
					message.Fields.Add(new GrpcMessageField_Map(field, keyField, valueField));
				}
				else if (field.Label == FieldDescriptorProto.Types.Label.Repeated)
				{
					message.Fields.Add(new GrpcMessageField_Repeated(field));
				}
				else
				{
					message.Fields.Add(new GrpcMessageField_Single(field));
				}
			}
			serviceFile.MessageArray.Add(message);

		}
		private void AddServices(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];
			serviceFile.ServiceArray = new List<GrpcService>();

			foreach (ServiceDescriptorProto service in serviceFile.ProtoFileDesc.Service)
			{
				GrpcService newService = new GrpcService(service);
				newService.MethodArray = new List<GrpcServiceMethod>();

				foreach (MethodDescriptorProto method in service.Method)
				{
					newService.MethodArray.Add(new GrpcServiceMethod(method));
				}
				serviceFile.ServiceArray.Add(newService);
			}
			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		private void AnalyzeMessage(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];

			//find message index that each field directly depends on
			foreach(GrpcMessage message in serviceFile.MessageArray)
			{
				foreach(GrpcMessageField messageField in message.Fields)
				{
					if (messageField.FieldDesc.Type != FieldDescriptorProto.Types.Type.Message) continue;
					string typeName = messageField.FieldDesc.TypeName;

					if (messageField is GrpcMessageField_Map)
					{
						//for map field, pick value field name, eg. "map<string, Address>" => "Address"
						GrpcMessageField_Map mapMessageField = (GrpcMessageField_Map)messageField;
						typeName = mapMessageField.ValueField.FieldDesc.TypeName;
					}
					if (serviceFile.Message2IndexMap.ContainsKey(typeName))
					{
						if(serviceFile.Message2IndexMap[typeName] >= message.Index)
						{
							messageField.NeedNativeMake = true;
							message.HasNativeMake = true;
						}
					}
				}
			}
		}

		public string DumpToString()
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			return JsonSerializer.Serialize(GrpcServiceFiles, options);
		}
	}
}
