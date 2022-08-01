using System.Reflection;

namespace SLMP {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SlmpStringAttribute : Attribute {
        /// <summary>
        /// Length of the string.
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// Number of the device words that the string occupies.
        /// </summary>
        public int WordCount => Length % 2 == 0 ? Length / 2 + 1 : (Length + 1) / 2;
    }

    /// <summary>
    /// Functionality related to encoding/decoding struct.
    /// </summary>
    public static class SlmpStruct {
        /// <summary>
        /// This function returns the size of the struct in terms of
        /// device words. (16 bit values)
        /// Int16, UInt16, Boolean size: 1 word (2 bytes)
        /// Int32, UInt32 size: 2 word (4 bytes)
        /// String, user defined size, see `SlmpStringAttribute`
        /// </summary>
        /// <param name="structType">Type of the structure.</param>
        public static int GetStructSize(Type structType) {
            int size = 0;
            var fieldTypes = structType.GetFields();

            foreach (var field in fieldTypes)
                switch (field.FieldType.Name) {
                    case "Int16":
                    case "UInt16":
                    case "Boolean":
                        size++;
                        break;
                    case "Int32":
                    case "UInt32":
                        size += 2;
                        break;
                    case "String":
                        SlmpStringAttribute? attr = field
                            .GetCustomAttributes<SlmpStringAttribute>()
                            .SingleOrDefault();
                        if (attr == default(SlmpStringAttribute))
                            throw new ArgumentException("please add a `SlmpStringAttribute` to the string.");

                        size += attr.WordCount;
                        break;
                    default:
                        throw new ArgumentException($"unsupported type: {field.FieldType.Name}");
                }

            return size;
        }

        /// <summary>
        /// This function builds a `Type` structure from a given byte array.
        /// </summary>
        /// <param name="structType">Type of the target structure.</param>
        /// <param name="words">Structure data in the form of an `ushort` array.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object? FromWords(Type structType, ushort[] words) {
            if (words.Length != GetStructSize(structType))
                return null;

            object? structObject = Activator.CreateInstance(structType);
            if (structObject == null)
                return null;

            int index = 0;
            var fields = structType.GetFields();

            foreach (var field in fields) {
                switch (field.FieldType.Name) {
                    case "Int16":
                        field.SetValue(structObject, (Int16)words[index]);
                        index++;
                        break;
                    case "UInt16":
                        field.SetValue(structObject, (UInt16)words[index]);
                        index++;
                        break;
                    case "Boolean":
                        field.SetValue(structObject, words[index] != 0);
                        index++;
                        break;
                    case "Int32":
                        field.SetValue(
                            structObject, (Int32)((words[index + 1] << 16) | words[index]));
                        index += 2;
                        break;
                    case "UInt32":
                        field.SetValue(
                            structObject, (UInt32)((words[index + 1] << 16) | words[index]));
                        index += 2;
                        break;
                    case "String":
                        SlmpStringAttribute? attr = field
                            .GetCustomAttributes<SlmpStringAttribute>()
                            .SingleOrDefault();
                        if (attr == default(SlmpStringAttribute))
                            throw new ArgumentException("please add a SlmpStringAttribute to the string.");

                        List<char> buffer = new();
                        for (int i = index; i < index + attr.WordCount; i++) {
                            ushort word = words[i];
                            buffer.Add((char)(word & 0xff));
                            buffer.Add((char)(word >> 0x8));
                        }
                        field.SetValue(
                            structObject, string.Join("", buffer.GetRange(0, attr.Length)));
                        index += attr.WordCount;
                        break;
                    default:
                        throw new ArgumentException($"unsupported type: {field.FieldType.Name}");
                }
            }

            return structObject;
        }
    }
}
