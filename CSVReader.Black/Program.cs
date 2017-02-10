using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSVReader.Black
{
   
    class Program
    {
        static void Main(string[] args)
        {
            CSVFile<Person> personCsv = new CSVFile<Person>("data.csv", ',');
            List<Row<Person>> e = personCsv.GetData();
            string outputFormat = "[{0, -5}]|{1, -3}|{2, -15}|{3, -15}|{4, -15}|{5, -15} - [{6}]";
            Console.WriteLine(outputFormat, "VALID", "Id", "First Name", "Last Name", "Email", "Age", "Error");
            Console.WriteLine("----------------------------------------------------------------");
            e.ForEach(o =>
            {
                Console.WriteLine(outputFormat, o.Valid.ToString() ,  o.row.Id, o.row.FirstName, o.row.LastName, o.row.Email, o.row.Age, o.Valid ? "" : Convert.ToString(o.validation.Errors.FirstOrDefault()));
            });
            Console.Read();
        }



        public class CSVFile<T> where T : class
        {
            private string fileContent;
            private char _seperator;
            private string _fileHeader;
            private List<string> _headers;
            private List<string> _lines;
            private List<FileRow> _rows;
            private List<Row<T>> _tuples;
            private List<Type> supportedTypes;

            public CSVFile(string path, char seperator)
            {
                if (File.Exists(path) && !string.IsNullOrEmpty(Convert.ToString(seperator)))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        fileContent = reader.ReadToEnd();
                    };
                    _seperator = seperator;
                    _lines = new List<string>();
                    _headers = new List<string>();
                    _rows = new List<FileRow>();
                    _tuples = new List<Row<T>>();
                    initSupportedTypes();
                    ProcessFile();
                }
            }

            void initSupportedTypes()
            {
                supportedTypes = new List<Type>()
                {
                    typeof(bool         ),
                    typeof(string       ),
                    typeof(String       ),
                    typeof(Boolean      ),
                    typeof(byte         ),
                    typeof(Byte         ),
                    typeof(SByte        ),
                    typeof(int          ),
                    typeof(Int16        ),
                    typeof(UInt16       ),
                    typeof(Int32        ),
                    typeof(UInt32       ),
                    typeof(Int64        ),
                    typeof(UInt64       ),
                    typeof(char         ),
                    typeof(Char         ),
                    typeof(Double       ),
                    typeof(Single       )
                };
            }

            void ProcessFile()
            {
                StringReader reader = new StringReader(fileContent);
                string line;
                do
                {
                    line = reader.ReadLine();
                    _lines.Add(line);
                } while (!string.IsNullOrEmpty(line));

                _fileHeader = _lines.FirstOrDefault();
                _lines = _lines.Skip(1).ToList();
                ProcessHeader();
            }

            void ProcessHeader()
            {
                _headers = _fileHeader.Split(_seperator).AsEnumerable().ToList();
                foreach (var line in _lines.ToList())
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        FileRow row = new FileRow();
                        Row<T> tuple = new Row<T>(line);
                        string[] values = line.Split(_seperator);

                        for (var i = 0; i < _headers.Count; i++)
                        {
                            tuple.Values.Add(new FileValue()
                            {
                                Name = _headers[i],
                                Value = Convert.ToString(values[i])
                            });
                        }
                        _rows.Add(row);
                        _tuples.Add(tuple);
                    }
                }
            }

            public List<Row<T>> GetData()
            {
                List<PropertyInfo> properties = typeof(T).GetProperties().AsEnumerable().ToList();
                List<T> data = new List<T>();

                _tuples.ForEach(tuple =>
                {

                    T obj = null;
                    try
                    {
                        Type t = typeof(T);
                        obj = (T)Activator.CreateInstance(t);
                    }
                    catch (Exception e)
                    {
                        tuple.Error(ErrorStrings.InstanceNonCreatable);
                    }
                    if (obj != null)
                    {
                        properties.ForEach(p =>
                        {
                            if (hasHeader(p.Name))
                            {
                                if (supportsType(p.PropertyType))
                                {
                                    if (p.CanWrite)
                                    {
                                        var value = tuple.GetValue(p.Name).Value;
                                        try
                                        {
                                            p.SetValue(obj, Convert.ChangeType(value, p.PropertyType));
                                        }
                                        catch(Exception ex)
                                        {
                                            tuple.Error(string.Format("{0} of Property [{1}] with value [\"{2}\"] of type [{3}]", ErrorStrings.UnableToSetValue, p.Name, value, p.PropertyType.Name));
                                        }      
                                    }
                                    else
                                    {
                                        tuple.Error(string.Format("[{0}] {1}", p.Name, ErrorStrings.PropertyNoWritable));
                                    }
                                }
                                else
                                {
                                    tuple.Error(string.Format("[{0} - {1}] {2}", p.Name, p.PropertyType.Name, ErrorStrings.TypeNotSupported));
                                }

                            }
                        });
                        tuple.row = obj;
                        data.Add(obj);
                    }

                });
                return _tuples;
                //return data;
            }

            bool supportsType(Type t)
            {
                return supportedTypes.Any(o => o == t);
            }

            bool hasHeader(string header)
            {
                return _headers.Any(o => o.ToLower() == header.ToLower());
            }

            FileRow GetRow(int index)
            {
                if (index >= 0 && index < _rows.Count)
                {
                    return _rows[index];
                }
                return null;
            }

            List<U> GetColumnData<U>(string columnnName)
            {
                string colname = columnnName.ToLower();
                List<U> fv = new List<U>();
                if (_headers.Any(o => o.ToLower() == colname))
                {
                    _rows.ForEach(o =>
                    {
                        string value = o.Values.Where(m => m.Name.ToLower() == colname).Select(n => n.Value).FirstOrDefault();
                        fv.Add((U)Convert.ChangeType(value, typeof(U)));
                    });
                }

                return fv;
            }

        

        }
        enum ErrorType
        {
            DuplicateRow,
            NotSerializable,
            NotWritable,
            InsufficientData
        }
        public static class ErrorStrings
        {
            public static string DataNotSerializable = "Data in Specified Column Could not be serialized";
            public static string ColumnTypeConversionNotSupported = "Column of Specified Type cannot be serialzed";
            public static string InstanceNonCreatable = "Instance of Specified Type Could not be created";
            public static string TypeNotSupported = "Instance of Specified Type Could not be created";
            public static string PropertyNoWritable = "Instance of Specified Type Could not be created";
            public static string UnableToSetValue = "Unable to set value";

        }

        public class Validation
        {
            public List<string> Errors { get; set; }
            public Validation()
            {
                Errors = new List<string>();
            }
            public bool IsValid
            {
                get
                {
                    return Errors.Count == 0;
                }
            }

            public void Error(string column, string error)
            {
                Errors.Add(string.Format("[{0}] - {1}", column, error));
            }

            public void Error(string error)
            {
                Errors.Add(error);
            }

            void Error(ErrorType type, string error)
            {
                Errors.Add(error);
            }
        }
        public class Row<V> where V : class
        {
            public List<FileValue> Values { get; set; }
            public V row { get; set; }
            public string rawString { get; }
            public Validation validation { get; set; }
            public Row(string line)
            {
                rawString = line;
                validation = new Validation();
                Values = new List<FileValue>();
            }
            public bool Valid
            {
                get
                {
                    return validation.IsValid;
                }
            }

            public void Error(string error)
            {
                validation.Error(error);
            }

            public FileValue GetValue(string Name)
            {
                return Values.Where(o => o.Name.ToLower() == Name.ToLower()).FirstOrDefault();
            }
            public override string ToString()
            {
                return string.Join(",", Values.Select(o => o.Value).ToArray());
            }
        }

        public class FileRow
        {
            public List<FileValue> Values { get; set; }
            public FileRow()
            {
                Values = new List<FileValue>();
            }

            public FileValue GetValue(string Name)
            {
                return Values.Where(o => o.Name.ToLower() == Name.ToLower()).FirstOrDefault();
            }
            public override string ToString()
            {
                return string.Join(",", Values.Select(o => o.Value).ToArray());
            }
        }

        public class FileValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Age { get; set; }
            public List<string> Email { get; set; }
            public string Gender { get; set; }
        }
    }
}
