using ChaosUtil.Reflection;
using System.Linq;
using FieldInfo = System.Reflection.FieldInfo;
using Flags = System.Reflection.BindingFlags;
using PropertyInfo = System.Reflection.PropertyInfo;
using SysCol = System.Collections.Generic;

namespace ChaosUtil.Debug
{
    /// <summary> Provides functions to create an exception report and save it to a file. </summary>
    public static class ExceptionReport
    {
        /// <summary> The directory to save exception log files to. Ends with a backslash. </summary>
        public static readonly string LOG_DIRECTORY = $"{Constants.LOG_DIRECTORY}Exceptions\\";

        static readonly string[] LOG_COLUMNS = { "Declaration", "Accessor", "FieldType", "DeclaringType", "Name", "Value" };
        static readonly Flags[] allFieldFlags;

        static ExceptionReport()
        {
            SysCol.List<Flags> flags = new SysCol.List<Flags>();
            foreach (Flags @static in new Flags[] { Flags.Instance, Flags.Static })
                foreach (Flags @public in new Flags[] { Flags.Public, Flags.NonPublic })
                    flags.Add(@public | @static);
            allFieldFlags = flags.ToArray();
        }

        /// <summary> Creates an exception report and saves it to the log directory set in <see cref="LOG_DIRECTORY"/>. </summary>
        /// <param name="ex"> The exception to be reported. </param>
        public static void PrintExceptionReport(System.Exception ex)
        {
            System.DateTime time = System.DateTime.Now;
            string path = $"{LOG_DIRECTORY}{time.ToString(Constants.TIME_FORMAT)}_{ex.GetType().Name}";
            if (System.IO.File.Exists($"{path}.log"))
            {
                int i = 2;
                while (System.IO.File.Exists($"{path} ({i}).log"))
                    i++;
                path += $" ({i})";
            }
            path += ".log";

            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            using (System.IO.FileStream str = new System.IO.FileStream(path, System.IO.FileMode.Create))
            using (System.IO.StreamWriter wr = new System.IO.StreamWriter(str))
            {
                wr.WriteLine($"{AssemblyMeta.productName} ({System.DateTime.Now})");
                wr.WriteLine();
                wr.WriteLine(ex.GetType().FullName);
                PrintException(wr, ex);
            }
        }

        /// <summary> Write an exception, all its relevant data and all its inner exceptions to the provided stream. </summary>
        /// <param name="wr"> The writer used to write the exception. </param>
        /// <param name="ex"> The exception to be printed. </param>
        /// <param name="depth"> The indentation depth for the output. </param>
        public static void PrintException(System.IO.StreamWriter wr, System.Exception ex, int depth = 0)
        {
            Write(wr, "{", depth);
            WriteObject(wr, "Message", ex.Message, depth + 1);
            if (ex.StackTrace != null)
                WriteObject(wr, "StackTrace", string.Join("\n", ex.StackTrace.Split('\n').Select(TrimStart)), depth + 1);

            Write(wr, "Fields", depth + 1);
            Write(wr, "{", depth + 1);
            WriteFields(ex, wr, depth + 2);
            Write(wr, "}", depth + 1);
            if (ex.InnerException != null)
            {
                Write(wr, "InnerException: " + ex.InnerException.GetType().FullName, depth + 1);
                PrintException(wr, ex.InnerException, depth + 1);
            }
            Write(wr, "}", depth);
        }

        static string TrimStart(string str) => str.TrimStart();

        static void WriteObject(System.IO.StreamWriter wr, string name, object obj, int depth)
        {
            if (obj != null)
            {
                Write(wr, name + ":", depth);
                Write(wr, "{", depth);
                Write(wr, obj.ToString().Trim(), depth + 1);
                Write(wr, "}", depth);
            }
        }

        static void Write(System.IO.StreamWriter wr, string msg, int depth)
        {
            msg = msg.Replace(" ---> ", "\n--->\n");

            string[] split = msg.Split('\n');
            for (int line = 0; line < split.Length; line++)
                split[line] = new string('\t', depth) + split[line];

            wr.WriteLine(string.Join("\n", split));
        }

        /// <summary>
        ///     Write all of a provided object's fields' and readable properties' names and values
        ///     to the provided stream in a table like format.
        /// </summary>
        /// <param name="obj"> The object whose data is to be written. </param>
        /// <param name="wr"> The writer used to write the object's data. </param>
        /// <param name="depth"> The indentation depth for the output. </param>
        public static void WriteFields(object obj, System.IO.StreamWriter wr, int depth = 1)
        {
            int[] maxLength = new int[LOG_COLUMNS.Length];
            for (int i = 0; i < maxLength.Length; i++)
                maxLength[i] = LOG_COLUMNS[i].Length;
            SysCol.IEnumerable<System.Type> ancestors = AssemblyManager.EnumerateAncestorTypes(obj.GetType(), true);

            SysCol.List<string[]> fieldData = new SysCol.List<string[]>();
            SysCol.Dictionary<string[], FieldInfo> fields = new SysCol.Dictionary<string[], FieldInfo>();
            int fieldIndex = 0;
            foreach (Flags flag in allFieldFlags)
                foreach (System.Type t in ancestors)
                    foreach (FieldInfo field in t.GetFields(flag))
                    {
                        foreach (FieldInfo compare in fields.Values)
                            if (compare.Name == field.Name && compare.DeclaringType == field.DeclaringType)
                                goto field_already_exists;

                        object value = field.GetValue(obj);
                        string[] dat = new string[] {
                            field.GetDeclarationType().ToString(),
                            field.GetAccessor().ToString(),
                            field.FieldType.Name,
                            field.DeclaringType.FullName,
                            field.Name,
                            value == null ? "<null>" : value.ToString().EscapeControlCharacters().Trim(),
                        };
                        fields[dat] = field;
                        fieldData.Add(dat);

                        for (int i = 0; i < maxLength.Length; i++)
                            maxLength[i] = System.Math.Max(maxLength[i], fieldData[fieldData.Count - 1][i].ToString().Length);
                        fieldIndex++;
                    field_already_exists:;
                    }

            fieldIndex = 0;
            SysCol.List<string[]> propertyData = new SysCol.List<string[]>();
            SysCol.Dictionary<string[], PropertyInfo> properties = new SysCol.Dictionary<string[], PropertyInfo>();
            foreach (Flags flag in allFieldFlags)
                foreach (System.Type t in ancestors)
                    foreach (PropertyInfo property in t.GetProperties(flag))
                    {
                        if (!property.CanRead || property.GetIndexParameters().Length != 0)
                            continue;

                        foreach (PropertyInfo compare in properties.Values)
                            if (compare.Name == property.Name && compare.DeclaringType == property.DeclaringType)
                                goto property_already_exists;

                        object value = property.GetValue(obj);
                        string[] dat = new string[] {
                            "Property",
                            property.GetAccessor().ToString(),
                            property.PropertyType.Name,
                            property.DeclaringType.FullName,
                            property.Name,
                            value == null ? "<null>" : value.ToString().EscapeControlCharacters().Trim(),
                        };

                        properties[dat] = property;
                        propertyData.Add(dat);

                        for (int i = 0; i < maxLength.Length; i++)
                            maxLength[i] = System.Math.Max(maxLength[i], propertyData[propertyData.Count - 1][i].ToString().Length);
                        fieldIndex++;
                    property_already_exists:;
                    }

            fieldData.Sort((line1, line2) => MemberInfo.Compare(fields[line1], fields[line2]));
            propertyData.Sort((line1, line2) => MemberInfo.Compare(properties[line1], properties[line2]));

            string[] lineSeperator = new string[maxLength.Length];
            for (int i = 0; i < maxLength.Length; i++)
                for (int j = 0; j < maxLength[i]; j++)
                    lineSeperator[i] += "-";

            fieldData.Add(lineSeperator);
            fieldData.Insert(0, lineSeperator);
            fieldData.Insert(0, LOG_COLUMNS);
            fieldData.AddRange(propertyData);
            foreach (object[] line in fieldData)
            {
                wr.Write("| ".PadLeft(depth + 2, '\t'));
                for (int i = 0; i < maxLength.Length; i++)
                    wr.Write($"{line[i].ToString().PadRight(maxLength[i])} | ");
                wr.Write("\n");
            }
        }
    }
}
