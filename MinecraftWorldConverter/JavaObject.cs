using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace MinecraftWorldConverter
{
    public class JavaObject
    {
        private const ushort MAGIC = 0xACED;
        private const ushort CURRENT_VERSION = 5;

        #region JAVA

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum JavaIDs
        {
            TC_BOOL = 'Z',
            TC_BYTE = 'B',
            TC_CHAR = 'C',
            TC_SHORT = 'S',
            TC_INTEGER = 'I',
            TC_LONG = 'J',
            TC_FLOAT = 'F',
            TC_DOUBLE = 'D',
            TC_FULLY_QUALIFIED_CLASS = 'L',
            TC_TYPE = '[',

            TC_NULL = 0x70,
            TC_BASE = 0x70,
            TC_REFERENCE = 0x71,
            TC_CLASSDESC = 0x72,
            TC_OBJECT = 0x73,
            TC_STRING = 0x74,
            TC_ARRAY = 0x75,
            TC_CLASS = 0x76,
            TC_BLOCKDATA = 0x77,
            TC_ENDBLOCKDATA = 0x78,
            TC_RESET = 0x79,
            TC_BLOCKDATALONG = 0x7A,
            TC_EXCEPTION = 0x7B,
            TC_LONGSTRING = 0x7C,
            TC_PROXYCLASSDESC = 0x7D,
            TC_ENUM = 0x7E,
            TC_MAX = 0x7E,
        }

        private struct JavaField
        {
            public string Name { get; set; }
            public JavaIDs Type { get; set; }
            public string TypeString { get; set; }
        }

        private class ClassDesc
        {
            public string Name { get; set; }
            public ulong SerialVersionUID { get; set; }
            public byte Flags { get; set; }
            public List<JavaField> Fields { get; set; }
            public ClassDesc Parent { get; set; }
        }
        
        private ClassDesc ClassDescriptor { get; set; }

        private readonly List<string> _references;
        private readonly Dictionary<string, ClassDesc> _classDescMap;

        #endregion

        #region PUBLIC_API
        
        public enum FieldType
        {
            Bool,
            Byte,
            Char,
            Short,
            Integer,
            Long,
            Float,
            Double,
            FullyQualifiedClass,
            Type,
        }

        public class FieldData
        {
            public string Name { get; set; }
            public ObjectData Data { get; set; }
            public FieldType Type { get; set; }
            public string TypeString { get; set; }
        }

        public class ObjectData
        {
            public List<FieldData> Fields { get; set; }
            public byte[] Bytes { get; set; }
            
            public List<ObjectData> ArrayData { get; set; }
        }

        #endregion

        public ObjectData Object { get; set; }

        public JavaObject()
        {
            _references = new List<string>();
            _classDescMap = new Dictionary<string, ClassDesc>();
        }

        public void LoadFromFile(string file)
        {
            LoadFromStream(File.OpenRead(file));
        }

        public void LoadFromStream(Stream stream)
        {
            using BinaryReader2 br = new BinaryReader2(stream);

            if (!br.VerifyNext(MAGIC))
                throw new MCWorldException("Invalid Java Object");

            if (!br.VerifyNext(CURRENT_VERSION))
                throw new MCWorldException("Unsupported Java Object Version");

            ClassDescriptor = ReadClassDescriptors(br, null);
            Object = ReadClassValues(br, ClassDescriptor);
        }

        #region Descriptors

        private ClassDesc ReadClassDescriptors(BinaryReader2 br, ClassDesc baseDesc)
        {
            ClassDesc lastClassDesc = null;
            while (true)
            {
                JavaIDs read = (JavaIDs)br.ReadByte();
                if (read == JavaIDs.TC_NULL || read == JavaIDs.TC_ENDBLOCKDATA)
                    return baseDesc;

                switch (read)
                {
                    case JavaIDs.TC_CLASSDESC:
                        br.BaseStream.Position--;
                        lastClassDesc = ReadBlockDesc(br, lastClassDesc);
                        if (baseDesc == null)
                            baseDesc = lastClassDesc;
                        break;
                    case JavaIDs.TC_REFERENCE:
                        byte unknownValue = br.ReadByte();
                        JavaIDs type = (JavaIDs)br.ReadByte();
                        if (type != JavaIDs.TC_ENUM)
                            throw new MCWorldException($"Expected enum type for reference");
                        ushort referenceNum = br.ReadUInt16();
                        if (referenceNum >= _references.Count)
                            throw new MCWorldException($"Out of range reference {referenceNum+1}/{_references.Count}");
                        string n = _references[referenceNum];
                        if (_classDescMap.ContainsKey(n))
                            lastClassDesc = _classDescMap[n];
                        else
                            lastClassDesc = null; // TODO: This might be bad
                        if (baseDesc == null)
                            baseDesc = lastClassDesc;
                        return baseDesc;
                        break;
                    case JavaIDs.TC_OBJECT:
                    case JavaIDs.TC_ARRAY:
                        lastClassDesc = ReadBlockDesc(br, lastClassDesc);
                        if (baseDesc == null)
                            baseDesc = lastClassDesc;
                        break;
                    default:
                        throw new MCWorldException($"Unsupported Class Descriptor: {read}");
                }
            }
        }

        private ClassDesc ReadBlockDesc(BinaryReader2 br, ClassDesc previous)
        {
            ClassDesc ret = new ClassDesc();
            if (previous != null)
                previous.Parent = ret;
            
            JavaIDs desc = (JavaIDs)br.ReadByte();
            switch (desc)
            {
                case JavaIDs.TC_CLASSDESC:
                    ret.Name = br.ReadJavaString();
                    _references.Add(ret.Name);
                    _classDescMap[ret.Name] = ret;
                    ret.SerialVersionUID = br.ReadUInt64();
                    ret.Flags = br.ReadByte();
                    ushort fields = br.ReadUInt16();

                    ret.Fields = new List<JavaField>();
                    for (var i = 0; i < fields; i++)
                        ret.Fields.Add(ReadField(br));
                    
                    break;
                default:
                    throw new MCWorldException($"Unsupported Block Descriptor: {desc}");
            }

            JavaIDs end = (JavaIDs)br.ReadByte();
            if (end != JavaIDs.TC_ENDBLOCKDATA)
                throw new MCWorldException("Block should have ended");

            return ret;
        }

        private JavaField ReadField(BinaryReader2 br)
        {
            JavaField field = new JavaField();

            field.Type = (JavaIDs)br.ReadByte();
            field.Name = br.ReadJavaString();

            if (field.Type == JavaIDs.TC_FULLY_QUALIFIED_CLASS || field.Type == JavaIDs.TC_TYPE)
            {
                JavaIDs type = (JavaIDs)br.ReadByte();
                if (type == JavaIDs.TC_STRING)
                {
                    field.TypeString = br.ReadJavaString();
                    _references.Add(field.TypeString);
                }
                else if (type == JavaIDs.TC_REFERENCE)
                {
                    byte unknownValue = br.ReadByte();
                    type = (JavaIDs)br.ReadByte();
                    if (type != JavaIDs.TC_ENUM)
                        throw new MCWorldException($"Expected enum type for reference");

                    ushort referenceNum = (ushort)(br.ReadUInt16());
                    if (referenceNum >= _references.Count)
                        referenceNum = (ushort)(_references.Count - 1); // TODO: This is probably a bad idea.
                        //throw new MCWorldException($"Out of range reference {referenceNum+1}/{_references.Count}");
                    field.TypeString = _references[referenceNum];
                    _references.Add(field.TypeString);
                }
                else
                {
                    throw new MCWorldException($"Unsupported type after class or type?");
                }
            }

            return field;
        }

        private List<ClassDesc> TraverseParents(ClassDesc desc)
        {
            List<ClassDesc> ret = new List<ClassDesc>();
            if (desc.Parent != null)
                ret.AddRange(TraverseParents(desc.Parent));
            ret.Add(desc);

            return ret;
        }

        #endregion

        #region Values

        private ObjectData ReadClassValues(BinaryReader2 br, ClassDesc baseDesc)
        {
            ObjectData ret = new ObjectData();
            ret.Fields = new List<FieldData>();

            List<ClassDesc> classDescs = TraverseParents(baseDesc);
            foreach (var desc in classDescs)
            {
                foreach (var field in desc.Fields)
                {
                    FieldData data = new FieldData();
                    data.Name = field.Name;
                    if (data.Name == "tmp")
                        Console.WriteLine("HERE"); // TODO: Read pos 3038, it fails because array length is there, and it doesn't know it's an array because it's using the field type and not the actual type
                    data.Data = new ObjectData();
                    
                    ReadValue(br, baseDesc, field, data);
                }

            }

            return ret;
        }

        private void ReadValue(BinaryReader2 br, ClassDesc baseDesc, JavaField field, FieldData data)
        {
            switch (field.Type)
            {
                case JavaIDs.TC_BYTE:
                {
                    data.Type = FieldType.Byte;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadByte());
                } break;
                case JavaIDs.TC_BOOL:
                {
                    data.Type = FieldType.Bool;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadBoolean());
                } break;
                case JavaIDs.TC_CHAR:
                {
                    data.Type = FieldType.Char;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadChar());
                } break;
                case JavaIDs.TC_SHORT:
                {
                    data.Type = FieldType.Short;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadInt16());
                } break;
                case JavaIDs.TC_INTEGER:
                {
                    data.Type = FieldType.Integer;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadInt32());
                } break;
                case JavaIDs.TC_LONG:
                {
                    data.Type = FieldType.Long;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadInt64());
                } break;
                case JavaIDs.TC_FLOAT:
                {
                    data.Type = FieldType.Float;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadSingle());
                } break;
                case JavaIDs.TC_DOUBLE:
                {
                    data.Type = FieldType.Double;
                    data.Data.Bytes = BitConverter.GetBytes(br.ReadDouble());
                } break;
                case JavaIDs.TC_FULLY_QUALIFIED_CLASS:
                {
                    data.Type = FieldType.FullyQualifiedClass;
                    data.TypeString = field.TypeString;
                    if (data.TypeString == "Ljava/lang/String;")
                    {
                        JavaIDs id = (JavaIDs)br.ReadByte();
                        if (id == JavaIDs.TC_STRING)
                        {
                            data.Data = new ObjectData();
                            data.Data.Bytes = Encoding.ASCII.GetBytes(br.ReadJavaString());
                            break;
                        }
                    }
                    ClassDesc newDesc = ReadClassDescriptors(br, null);
                    if (newDesc != null && newDesc.Name == "[Ljava.util.ArrayList;")
                        Console.WriteLine("HERE3"); // TODO: This breaks somehow
                    if (newDesc != null)
                        data.Data = ReadClassValues(br, newDesc);
                } break;
                case JavaIDs.TC_TYPE:
                {
                    data.Type = FieldType.Type;
                    data.TypeString = field.TypeString;
                    //ClassDesc newDesc = ReadClassDescriptors(br, null);
                    //data.Data = ReadClassValues(br, newDesc);

                    JavaIDs id = (JavaIDs)br.ReadByte();
                    if (id == JavaIDs.TC_ARRAY)
                    {
                        ClassDesc descNew = ReadClassDescriptors(br, null);

                        // TODO: Deal with non-primitive arrays
                        JavaIDs typeName = (JavaIDs)descNew.Name.Substring(1)[0];
                        
                        data.Data.ArrayData = new List<ObjectData>();
                        
                        int len = br.ReadInt32();
                        for (int i = 0; i < len; i++)
                        {
                            JavaField f = new JavaField();
                            f.Type = typeName;
                            FieldData d = new FieldData();
                            d.Data = new ObjectData();
                            ReadValue(br, descNew, f, d);
                            data.Data.ArrayData.Add(d.Data);
                        }
                    }
                    else
                    {
                        if (id != JavaIDs.TC_BLOCKDATA)
                            throw new MCWorldException($"Expected type");

                        byte length = br.ReadByte();
                        int count = br.ReadInt32();
                        data.Data = new ObjectData();
                        data.Data.ArrayData = new List<ObjectData>();
                        List<ClassDesc> descs = new List<ClassDesc>();
                        for (int i = 0; i < count; i++)
                        {
                            descs.Add(ReadClassDescriptors(br, null));
                        }

                        for (int i = descs.Count - 1; i >= 0; i--)
                        {
                            data.Data.ArrayData.Add(ReadClassValues(br, descs[i]));
                        }
                    }

                    //data.Data.ArrayData.Add(ReadClassValues(br, newDesc));

                } break;
                /*case JavaIDs.TC_BLOCKDATA:
                    byte length = br.ReadByte();
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        ClassDesc newDesc = ReadClassDescriptors(br, null);
                        data.Data = ReadClassValues(br, newDesc);
                    }
                    break;*/
            }
        }

        #endregion
    }
}