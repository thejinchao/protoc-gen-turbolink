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
		public GrpcMessageField(FieldDescriptorProto fieldDesc) => FieldDesc = fieldDesc;
		public virtual string FieldType							//eg. "int32", "FString", "EGrpcCommonGender", "TArray<FGrpcUserRegisterRequestAddress>"
		{
			get => TurboLinkUtils.GetFieldType(FieldDesc);
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
		public virtual string FieldDefaultValue
		{
			get => string.Empty;
		}
	}
	public class GrpcMessageField_Single : GrpcMessageField
	{
		public GrpcMessageField_Single(FieldDescriptorProto fieldDesc) : base(fieldDesc)
		{ }
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
	}

	public struct GrpcMessage
	{
		public string Name { get; set; }                        //eg. "FGrpcGreeterHelloResponse",  "FGrpcGoogleProtobufValue"
		public string GrpcName { get; set; }					//eg. "Greeter::HelloResponse", "Google::Protobuf::Value"
		public string DisplayName { get; set; }					//eg. "Greeter.HelloResponse", "GoogleProtobuf.Value"
		public List<GrpcMessageField> Fields { get; set; }
	}
	public struct GrpcServiceMethod
	{
		public string Name { get; set; }
		public bool ClientStreaming { get; set; }
		public bool ServerStreaming { get; set; }
		public string InputType { get; set; }               //eg. "FGrpcUserRegisterRequest"
		public string GrpcInputType { get; set; }           //eg. "::User::RegisterRequest"
		public string OutputType { get; set; }              //eg. "FGrpcUserRegisterResponse"
		public string GrpcOutputType { get; set; }          //eg. "::User::RegisterResponse"
		public string ContextSuperClass { get; set; }       //eg. "GrpcContext_Ping_Pong<UserService_Register_ReaderWriter, ::User::RegisterResponse>"
	}
	public struct GrpcService
	{
		public string Name { get; set; }					//eg. "UserService"
		public List<GrpcServiceMethod> MethodArray { get; set; }
	}
	public struct GrpcServiceFile
	{
		public FileDescriptorProto ProtoFile;
		public string FileName { get; set; }                //eg. "hello.proto", "google/protobuf/struct.proto"
		public string CamelFileName { get; set; }			//eg. "Hello", "Struct"
		public string PackageName { get; set; }				//eg. "Greeter", "google.protobuf"
		public string CamelPackageName { get; set; }		//eg. "Greeter", "GoogleProtobuf"
		public string GrpcPackageName { get; set; }			//eg. "Greeter", "google::protobuf"
		public string TurboLinkBasicFileName { get; set; }	//eg. "SGreeter/Hello", "SGoogleProtobuf/Struct"
		public List<string> DependencyFiles { get; set; }	
		public List<GrpcEnum> EnumArray { get; set; }
		public List<GrpcMessage> MessageArray { get; set; }
		public List<GrpcService> ServiceArray { get; set; }
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
				GatherServiceInfomation(protoFile);
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

			return true;
		}
		private void GatherServiceInfomation(FileDescriptorProto protoFile)
		{
			//basic infomation
			GrpcServiceFile serviceFile = new GrpcServiceFile();
			serviceFile.ProtoFile = protoFile;
			serviceFile.FileName = protoFile.Name;
			serviceFile.CamelFileName = TurboLinkUtils.GetCamelFileName(serviceFile.FileName);
			serviceFile.PackageName = protoFile.Package;
			serviceFile.CamelPackageName = TurboLinkUtils.GetCamelPackageName(serviceFile.PackageName);
			serviceFile.GrpcPackageName = TurboLinkUtils.GetGrpcName(serviceFile.PackageName);
			serviceFile.TurboLinkBasicFileName = "S" + serviceFile.CamelPackageName + "/" + serviceFile.CamelFileName;

			//add to grpc service dictionary
			GrpcServiceFiles[serviceFile.FileName] = serviceFile;
		}
		private void AddDependencyFiles(string protoFileName)
		{
			var serviceFile = GrpcServiceFiles[protoFileName];

			serviceFile.DependencyFiles = new List<string>();
			foreach (string dependency in serviceFile.ProtoFile.Dependency)
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
			foreach (EnumDescriptorProto protoEnum in serviceFile.ProtoFile.EnumType)
			{
				AddEnum(ref serviceFile, 
					string.Join(string.Empty,	"EGrpc", serviceFile.CamelPackageName, protoEnum.Name),
					string.Join(".",			serviceFile.CamelPackageName, protoEnum.Name),
					protoEnum.Value);
			}

			//iterate nested enum in message
			foreach (DescriptorProto message in serviceFile.ProtoFile.MessageType)
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
			foreach (DescriptorProto protoMessage in serviceFile.ProtoFile.MessageType)
			{
				//add nested message first
				foreach (DescriptorProto nestedProtoMessage in protoMessage.NestedType)
				{
					if (nestedProtoMessage.Options != null && nestedProtoMessage.Options.MapEntry) continue;
					AddMessage(ref serviceFile,
						string.Join(string.Empty, "FGrpc", serviceFile.CamelPackageName, protoMessage.Name, nestedProtoMessage.Name),
						string.Join(".", serviceFile.CamelPackageName, protoMessage.Name, nestedProtoMessage.Name),
						string.Join("::", serviceFile.GrpcPackageName, protoMessage.Name, nestedProtoMessage.Name),
						nestedProtoMessage);
				}

				AddMessage(ref serviceFile,
					string.Join(string.Empty, "FGrpc", serviceFile.CamelPackageName, protoMessage.Name),
					string.Join(".", serviceFile.CamelPackageName, protoMessage.Name),
					string.Join("::", serviceFile.GrpcPackageName, protoMessage.Name),
					protoMessage); ;
			}
			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		private void AddMessage(ref GrpcServiceFile serviceFile, string name, string displayName, string grpcName, DescriptorProto protoMessage)
		{
			GrpcMessage message = new GrpcMessage();
			message.Name = name;
			message.DisplayName = displayName;
			message.GrpcName = grpcName;
			message.Fields = new List<GrpcMessageField>();

			foreach (FieldDescriptorProto field in protoMessage.Field)
			{
				bool isMapField;
				FieldDescriptorProto keyField, valueField;
				(isMapField, keyField, valueField) = TurboLinkUtils.IsMapField(field, protoMessage);
				if(isMapField)
				{
					message.Fields.Add(new GrpcMessageField_Map(field, keyField, valueField));
				}
				else if(field.Label == FieldDescriptorProto.Types.Label.Repeated)
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

			foreach (ServiceDescriptorProto service in serviceFile.ProtoFile.Service)
			{
				GrpcService newService = new GrpcService();
				newService.Name = service.Name;
				newService.MethodArray = new List<GrpcServiceMethod>();

				foreach (MethodDescriptorProto method in service.Method)
				{
					GrpcServiceMethod newMethod = new GrpcServiceMethod();
					newMethod.Name = method.Name;
					newMethod.ClientStreaming = method.ClientStreaming;
					newMethod.ServerStreaming = method.ServerStreaming;
					newMethod.InputType = TurboLinkUtils.GetMessageName(method.InputType);
					newMethod.GrpcInputType = method.InputType.Replace(".", "::");
					newMethod.OutputType = TurboLinkUtils.GetMessageName(method.OutputType);
					newMethod.GrpcOutputType = method.OutputType.Replace(".", "::");
					newMethod.ContextSuperClass = TurboLinkUtils.GetContextSuperClass(service, newMethod);

					newService.MethodArray.Add(newMethod);
				}
				serviceFile.ServiceArray.Add(newService);
			}
			GrpcServiceFiles[protoFileName] = serviceFile;
		}
		public string DumpToString()
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			return JsonSerializer.Serialize(GrpcServiceFiles, options);
		}
	}
}
