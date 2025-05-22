using System.Text.Json;
using System.Text.Json.Serialization;

namespace S5Converter.CommonExtensions
{
    [JsonConverter(typeof(RpUserDataArrayJsonConverter))]
    internal class RpUserDataArray
    {
        internal enum RpUserDataFormat : int
        {
            rpNAUSERDATAFORMAT = 0,
            rpINTUSERDATA,          /**< 32 bit int data */
            rpREALUSERDATA,         /**< 32 bit float data */
            rpSTRINGUSERDATA,       /**< unsigned byte pointer data */
        };
        internal class DataObj
        {
            public int I;
            public float F;
            public string? S;

            internal int GetSize(RpUserDataFormat f)
            {
                if (f == RpUserDataFormat.rpSTRINGUSERDATA)
                    return S.GetRWLength();
                return sizeof(int); // float/int same size
            }
        }

        public RpUserDataFormat Format;
        public DataObj[] Data = [];

        private int Size => sizeof(int) * 2 + Data.Sum(x => x.GetSize(Format));
        internal static int GetSize(Dictionary<string, RpUserDataArray> d)
        {
            return sizeof(int) + d.Sum(x => x.Key.GetRWLength() + x.Value.Size);
        }
        internal static int GetSizeH(Dictionary<string, RpUserDataArray> d)
        {
            return GetSize(d) + ChunkHeader.Size;
        }

        internal static Dictionary<string, RpUserDataArray> Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.USERDATAPLUGIN);
            Dictionary<string, RpUserDataArray> r = [];
            int numUD = s.ReadInt32();
            for (int i = 0; i < numUD; ++i)
            {
                string udname = s.ReadRWString() ?? Guid.NewGuid().ToString();
                RpUserDataFormat type = (RpUserDataFormat)s.ReadInt32();
                int nelems = s.ReadInt32();
                RpUserDataArray o = new()
                {
                    Format = type,
                    Data = new DataObj[nelems]
                };
                for (int j = 0; j < nelems; ++j)
                {
                    switch (type)
                    {
                        case RpUserDataFormat.rpINTUSERDATA:
                            o.Data[j] = new() { I = s.ReadInt32() };
                            break;
                        case RpUserDataFormat.rpREALUSERDATA:
                            o.Data[j] = new() { F = s.ReadSingle() };
                            break;
                        case RpUserDataFormat.rpSTRINGUSERDATA:
                            o.Data[j] = new() { S = s.ReadRWString() };
                            break;
                    }
                }
                r[udname] = o;
            }
            return r;
        }

        internal static void Write(Dictionary<string, RpUserDataArray> d, BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = GetSize(d),
                    Type = RwCorePluginID.USERDATAPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(d.Count);
            foreach (var (k, v) in d)
            {
                if (v.Format == RpUserDataFormat.rpNAUSERDATAFORMAT)
                    throw new IOException("invalid format");
                s.WriteRWString(k);
                s.Write((int)v.Format);
                s.Write(v.Data.Length);
                foreach (var e in v.Data)
                {
                    switch (v.Format)
                    {
                        case RpUserDataFormat.rpINTUSERDATA:
                            s.Write(e.I);
                            break;
                        case RpUserDataFormat.rpREALUSERDATA:
                            s.Write(e.F);
                            break;
                        case RpUserDataFormat.rpSTRINGUSERDATA:
                            s.WriteRWString(e.S);
                            break;
                    }
                }
            }
        }
    }
    internal class RpUserDataArrayJsonConverter : JsonConverter<RpUserDataArray>
    {
        public override RpUserDataArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();
            List<RpUserDataArray.DataObj> l = [];
            RpUserDataArray.RpUserDataFormat f = RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT;
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA || f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT)
                        {
                            f = RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA;
                            l.Add(new() { S = reader.GetString()! });
                            reader.Read();
                            continue;
                        }
                        else
                        {
                            throw new JsonException("type missmatch");
                        }

                    case JsonTokenType.Number:
                        {
                            if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA)
                                throw new JsonException("type missmatch");
                            if (f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT || f == RpUserDataArray.RpUserDataFormat.rpINTUSERDATA)
                            {
                                if (reader.TryGetInt32(out int v))
                                {
                                    f = RpUserDataArray.RpUserDataFormat.rpINTUSERDATA;
                                    l.Add(new() { I = v });
                                    reader.Read();
                                    continue;
                                }
                            }
                            if (reader.TryGetSingle(out float vf))
                            {
                                if (f == RpUserDataArray.RpUserDataFormat.rpINTUSERDATA)
                                {
                                    foreach (RpUserDataArray.DataObj c in l)
                                        c.F = c.I;
                                }
                                f = RpUserDataArray.RpUserDataFormat.rpREALUSERDATA;
                                l.Add(new() { F = vf });
                                reader.Read();
                                continue;
                            }

                            break;
                        }

                    case JsonTokenType.Null:
                        if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA || f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT)
                        {
                            f = RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA;
                            l.Add(new() { });
                            reader.Read();
                            continue;
                        }
                        else
                        {
                            throw new JsonException("type missmatch");
                        }
                }
                throw new JsonException();
            }

            return new() { Format = f, Data = [.. l] };
        }

        public override void Write(Utf8JsonWriter writer, RpUserDataArray value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Data.Length; ++i)
            {
                switch (value.Format)
                {
                    case RpUserDataArray.RpUserDataFormat.rpINTUSERDATA:
                        writer.WriteNumberValue(value.Data[i].I);
                        break;
                    case RpUserDataArray.RpUserDataFormat.rpREALUSERDATA:
                        writer.WriteNumberValue(value.Data[i].F);
                        break;
                    case RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA:
                        writer.WriteStringValue(value.Data[i].S);
                        break;
                }
            }
            writer.WriteEndArray();
        }
    }
}
