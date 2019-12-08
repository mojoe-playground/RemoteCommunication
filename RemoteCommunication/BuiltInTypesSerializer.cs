namespace RemoteCommunication
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Runtime.InteropServices;

    public class BuiltInTypesSerializer : ISerializer
    {
        const byte _boolean = 1;
        const byte _byte = 2;
        const byte _char = 3;
        const byte _decimal = 4;
        const byte _double = 5;
        const byte _int16 = 6;
        const byte _int32 = 7;
        const byte _int64 = 8;
        const byte _sbyte = 9;
        const byte _single = 10;
        const byte _string = 11;
        const byte _uint16 = 12;
        const byte _uint32 = 13;
        const byte _uint64 = 14;

        const byte _null = 64;
        const byte _array = 128;       // array of bytes: _array + _byte; array of nullable bytes: _array + _null + _byte with an extra byte for every value indicating null (1) or not (0)

        public string Id => "sv";

        public bool CanSerialize(object data, SerializationManager manager) =>
            data == null
            || data is bool || data is bool[] || data is bool?[]
            || data is byte || data is byte[] || data is byte?[]
            || data is char || data is char[] || data is char?[]
            || data is decimal || data is decimal[] || data is decimal?[]
            || data is double || data is double[] || data is double?[]
            || data is short || data is short[] || data is short?[]
            || data is int || data is int[] || data is int?[]
            || data is long || data is long[] || data is long?[]
            || data is sbyte || data is sbyte[] || data is sbyte?[]
            || data is float || data is float[] || data is float?[]
            || data is string || data is string[]
            || data is ushort || data is ushort[] || data is ushort?[]
            || data is uint || data is uint[] || data is uint?[]
            || data is ulong || data is ulong[] || data is ulong?[]
            ;

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var type = reader.ReadByte();
            switch (type)
            {
                case _null: return null;
                case _boolean: return reader.ReadBoolean();
                case _boolean + _array: return ReadArray<bool>(reader);
                case _boolean + _null + _array: return ReadNullableArray<bool>(reader);
                case _byte: return reader.ReadByte();
                case _byte + _array: return ReadArray<byte>(reader);
                case _byte + _null + _array: return ReadNullableArray<byte>(reader);
                case _char: return reader.ReadChar();
                case _char + _array: return ReadArrayFallback(reader, () => reader.ReadChar());
                case _char + _null + _array: return ReadNullableArrayFallback(reader, () => reader.ReadChar());
                case _decimal: return reader.ReadDecimal();
                case _decimal + _array: return ReadArrayFallback(reader, () => reader.ReadDecimal());
                case _decimal + _null + _array: return ReadNullableArrayFallback(reader, () => reader.ReadDecimal());
                case _double: return reader.ReadDouble();
                case _double + _array: return ReadArray<double>(reader);
                case _double + _null + _array: return ReadNullableArray<double>(reader);
                case _int16: return reader.ReadInt16();
                case _int16 + _array: return ReadArray<short>(reader);
                case _int16 + _null + _array: return ReadNullableArray<short>(reader);
                case _int32: return reader.ReadInt32();
                case _int32 + _array: return ReadArray<int>(reader);
                case _int32 + _null + _array: return ReadNullableArray<int>(reader);
                case _int64: return reader.ReadInt64();
                case _int64 + _array: return ReadArray<long>(reader);
                case _int64 + _null + _array: return ReadNullableArray<long>(reader);
                case _sbyte: return reader.ReadSByte();
                case _sbyte + _array: return ReadArray<sbyte>(reader);
                case _sbyte + _null + _array: return ReadNullableArray<sbyte>(reader);
                case _single: return reader.ReadSingle();
                case _single + _array: return ReadArray<float>(reader);
                case _single + _null + _array: return ReadNullableArray<float>(reader);
                case _string: return reader.ReadString();
                case _string + _null + _array: return ReadStringArray(reader);
                case _uint16: return reader.ReadUInt16();
                case _uint16 + _array: return ReadArray<ushort>(reader);
                case _uint16 + _null + _array: return ReadNullableArray<ushort>(reader);
                case _uint32: return reader.ReadUInt32();
                case _uint32 + _array: return ReadArray<uint>(reader);
                case _uint32 + _null + _array: return ReadNullableArray<uint>(reader);
                case _uint64: return reader.ReadUInt64();
                case _uint64 + _array: return ReadArray<ulong>(reader);
                case _uint64 + _null + _array: return ReadNullableArray<ulong>(reader);

                default:
                    throw new InvalidOperationException("Unknown data type: " + type);
            }
        }

        private string[] ReadStringArray(BinaryReader br)
        {
            var count = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var res = new string[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? br.ReadString() : null;

            return res;
        }

        private T[] ReadArrayFallback<T>(BinaryReader br, Func<T> reader)
        {
            var count = br.ReadInt32();
            var res = new T[count];
            for (var i = 0; i < count; i++)
                res[i] = reader();

            return res;
        }

        private T[] ReadArray<T>(BinaryReader br)
        {
            var count = br.ReadInt32();
            var size = br.ReadInt32();
            var res = new T[count];
            var bytes = br.ReadBytes(size);
            Buffer.BlockCopy(bytes, 0, res, 0, size);

            return res;
        }

        private T?[] ReadNullableArrayFallback<T>(BinaryReader br, Func<T> reader) where T : struct
        {
            var count = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var res = new T?[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? (T?)reader() : null;

            return res;
        }

        private T?[] ReadNullableArray<T>(BinaryReader br) where T : struct
        {
            var count = br.ReadInt32();
            var size = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var valueArray = new T[count];
            var bytes = br.ReadBytes(size);
            Buffer.BlockCopy(bytes, 0, valueArray, 0, size);

            var res = new T?[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? (T?)valueArray[i] : null;

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            if (data == null)
            {
                writer.Write(_null);
                return;
            }

            if (Write<bool>(_boolean, writer, data, v => writer.Write(v)) || WriteArray<bool>(_boolean, writer, data) || WriteArrayNull<bool>(_boolean, writer, data))
                return;

            if (Write<byte>(_byte, writer, data, v => writer.Write(v)) || WriteArray<byte>(_byte, writer, data) || WriteArrayNull<byte>(_byte, writer, data))
                return;

            if (Write<char>(_char, writer, data, v => writer.Write(v)) || WriteArrayFallback<char>(_char, writer, data, ch => writer.Write(ch)) || WriteArrayNullFallback<char>(_char, writer, data, ch => writer.Write(ch)))
                return;

            if (Write<decimal>(_decimal, writer, data, v => writer.Write(v)) || WriteArrayFallback<decimal>(_decimal, writer, data, v => writer.Write(v)) || WriteArrayNullFallback<decimal>(_decimal, writer, data, v => writer.Write(v)))
                return;

            if (Write<double>(_double, writer, data, v => writer.Write(v)) || WriteArray<double>(_double, writer, data) || WriteArrayNull<double>(_double, writer, data))
                return;

            if (Write<short>(_int16, writer, data, v => writer.Write(v)) || WriteArray<short>(_int16, writer, data) || WriteArrayNull<short>(_int16, writer, data))
                return;

            if (Write<int>(_int32, writer, data, v => writer.Write(v)) || WriteArray<int>(_int32, writer, data) || WriteArrayNull<int>(_int32, writer, data))
                return;

            if (Write<long>(_int64, writer, data, v => writer.Write(v)) || WriteArray<long>(_int64, writer, data) || WriteArrayNull<long>(_int64, writer, data))
                return;

            if (Write<sbyte>(_sbyte, writer, data, v => writer.Write(v)) || WriteArray<sbyte>(_sbyte, writer, data) || WriteArrayNull<sbyte>(_sbyte, writer, data))
                return;

            if (Write<float>(_single, writer, data, v => writer.Write(v)) || WriteArray<float>(_single, writer, data) || WriteArrayNull<float>(_single, writer, data))
                return;

            if (Write<string>(_string, writer, data, v => writer.Write(v)) || WriteStringArray(_string, writer, data))
                return;

            if (Write<ushort>(_uint16, writer, data, v => writer.Write(v)) || WriteArray<ushort>(_uint16, writer, data) || WriteArrayNull<ushort>(_uint16, writer, data))
                return;

            if (Write<uint>(_uint32, writer, data, v => writer.Write(v)) || WriteArray<uint>(_uint32, writer, data) || WriteArrayNull<uint>(_uint32, writer, data))
                return;

            if (Write<ulong>(_uint64, writer, data, v => writer.Write(v)) || WriteArray<ulong>(_uint64, writer, data) || WriteArrayNull<ulong>(_uint64, writer, data))
                return;

            throw new InvalidOperationException($"Unsupported data: {data} ({data.GetType()})");
        }

        private bool Write<T>(byte type, BinaryWriter bw, object value, Action<T> writer)
        {
            if (value.GetType() != typeof(T))
                return false;

            bw.Write(type);
            writer((T)value);
            return true;
        }

        private bool WriteStringArray(byte baseType, BinaryWriter bw, object value)
        {
            if (value.GetType() != typeof(string[]))
                return false;

            var array = (string[])value;

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(array.Length);

            var bitArray = new BitArray(array.Length, true);
            for (var i = 0; i < array.Length; i++)
                if (array[i] == null)
                    bitArray.Set(i, false);
            var nullArray = new byte[array.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write(nullArray);

            foreach (var s in array)
                if (s != null)
                    bw.Write(s);

            return true;
        }

        private bool WriteArrayFallback<T>(byte baseType, BinaryWriter bw, object value, Action<T> writer)
        {
            if (value.GetType() != typeof(T[]))
                return false;

            var array = (T[])value;

            bw.Write((byte)(baseType + _array));
            bw.Write(array.Length);

            foreach (var i in array)
                writer(i);

            return true;
        }

        private bool WriteArray<T>(byte baseType, BinaryWriter bw, object value)
        {
            if (value.GetType() != typeof(T[]))
                return false;

            var array = (T[])value;

            bw.Write((byte)(baseType + _array));
            var size = GetElementSize(typeof(T)) * array.Length;
            bw.Write(array.Length);
            bw.Write(size);

            var bytes = new byte[size];
            Buffer.BlockCopy(array, 0, bytes, 0, size);
            bw.Write(bytes);

            return true;
        }

        private int GetElementSize(Type t)
        {
            if (t == typeof(bool))
                return 1;
            return Marshal.SizeOf(t);
        }

        private bool WriteArrayNullFallback<T>(byte baseType, BinaryWriter bw, object value, Action<T> writer) where T : struct
        {
            if (value.GetType() != typeof(T?[]))
                return false;

            var nullableArray = (T?[])value;
            var bitArray = new BitArray(nullableArray.Length, true);
            for (var i = 0; i < nullableArray.Length; i++)
                if (!nullableArray[i].HasValue)
                    bitArray.Set(i, false);
            var nullArray = new byte[nullableArray.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(nullableArray.Length);

            bw.Write(nullArray);

            foreach (var i in nullableArray)
                if (i.HasValue)
                    writer(i.Value);

            return true;
        }

        private bool WriteArrayNull<T>(byte baseType, BinaryWriter bw, object value) where T : struct
        {
            if (value.GetType() != typeof(T?[]))
                return false;

            var nullableArray = (T?[])value;
            var bitArray = new BitArray(nullableArray.Length, true);
            var array = new T[nullableArray.Length];
            for (var i = 0; i < nullableArray.Length; i++)
                if (nullableArray[i].HasValue)
                    array[i] = nullableArray[i].Value;
                else
                    bitArray.Set(i, false);
            var nullArray = new byte[nullableArray.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(nullableArray.Length);
            var size = GetElementSize(typeof(T)) * nullableArray.Length;
            bw.Write(size);

            bw.Write(nullArray);

            var bytes = new byte[size];
            Buffer.BlockCopy(array, 0, bytes, 0, size);
            bw.Write(bytes);

            return true;
        }
    }
}
