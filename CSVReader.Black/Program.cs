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
            //CSV<Lookup> personCsv = new CSV<Lookup>("C:\\Temp\\lookups.csv", ',', "LookupTypeId");
            //List<CSVRow<Lookup>> e = personCsv.GetData();
            //string outputFormat = "[{0, -5}]|{1, -3}|{2, -15}|{3, -15}|{4, -15}|{5, -15}|{6, -15} - [{7}]";
            ////Console.WriteLine(outputFormat, "VALID", "Id", "First Name", "Last Name", "Email", "Age", "Error");
            //Console.WriteLine(outputFormat, "VALID", "LookupTypeID", "Name", "Description", "IsActive", "LookupTypeCategoryId", "ConfigDisplay", "Errors");
            //Console.WriteLine("----------------------------------------------------------------");
            //e.ForEach(o =>
            //{
            //    Console.WriteLine(outputFormat, o.Valid.ToString(), o.row.LookupTypeId, o.row.Name, o.row.Description, "", o.row.LookupTypeCategoryId, o.row.ConfigDisplay, o.Valid ? "" : Convert.ToString(o.validation.Errors.FirstOrDefault()));
            //});
            var supportedTypes = new List<Type>()
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
            var obj = new Lookup();
            string value = "2";
            List<PropertyInfo> properties = typeof(Lookup).GetProperties().AsEnumerable().ToList();
            properties.ForEach(p =>
            {
                if (supportedTypes.Any(o => o == GetType(p)))
                {
                    if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                    {
                        var val = Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType));
                        p.SetValue(obj, val);
                        Console.WriteLine("{0} - {1}", p.Name, Nullable.GetUnderlyingType(p.PropertyType));
                    }
                    else
                    {
                        p.SetValue(obj, Convert.ChangeType(value, p.PropertyType));
                        Console.WriteLine("{0} - {1}", p.Name, p.PropertyType);
                    }
                }
            });

            Console.Read();
        }

        public static Type GetType(PropertyInfo p)
        {
            if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                return Nullable.GetUnderlyingType(p.PropertyType);
            return p.PropertyType;
        }

        public static bool IsNullable<T>(T value)
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }

        public class CSV<T> where T : class
        {
            private string fileContent;
            private char _seperator;
            private string _fileHeader;
            private List<string> _headers;
            private List<string> _lines;
            private List<CSVRow<T>> _tuples;
            private List<Type> supportedTypes;
            private List<string> _requiredHeaders;

            public CSV(string path, char seperator, string requiredHeaders)
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
                    _tuples = new List<CSVRow<T>>();

                    if (!string.IsNullOrEmpty(requiredHeaders))
                    {
                        _requiredHeaders = requiredHeaders.Split(',').ToList();
                    }
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
                        CSVRow<T> tuple = new CSVRow<T>(line);
                        string[] values = line.Split(_seperator);

                        for (var i = 0; i < _headers.Count; i++)
                        {
                            tuple.Values.Add(new CSVValue()
                            {
                                Name = _headers[i],
                                Value = Convert.ToString(values[i])
                            });
                        }
                        _tuples.Add(tuple);
                    }
                }
            }
            Type GetType(PropertyInfo p)
            {
                if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                    return Nullable.GetUnderlyingType(p.PropertyType);
                return p.PropertyType;
            }
            public List<CSVRow<T>> GetData()
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
                                if (supportsType(GetType(p)))
                                {
                                    if (p.CanWrite)
                                    {
                                        var value = tuple.GetValue(p.Name).Value;
                                        try
                                        {
                                            if (!string.IsNullOrEmpty(value))
                                            {
                                                if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                                                    p.SetValue(obj, Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType)));
                                                else
                                                    p.SetValue(obj, Convert.ChangeType(value, p.PropertyType));
                                            }
                                            else
                                            {
                                                if (isRequired(p.Name))
                                                {
                                                    tuple.Error(string.Format("[{0}] {1}", p.Name, ErrorStrings.PropertyIsRequired));
                                                }
                                            }
                                        }
                                        catch (Exception ex)
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
            }

            bool supportsType(Type t)
            {
                return supportedTypes.Any(o => o == t);
            }

            bool hasHeader(string header)
            {
                return _headers.Any(o => o.ToLower() == header.ToLower());
            }

            bool isRequired(string header)
            {
                return _requiredHeaders.Any(o => o.ToLower() == header.ToLower());
            }
            CSVRow<T> GetRow(int index)
            {
                if (index >= 0 && index < _tuples.Count)
                {
                    return _tuples[index];
                }
                return null;
            }

            List<U> GetColumnData<U>(string columnnName)
            {
                string colname = columnnName.ToLower();
                List<U> fv = new List<U>();
                if (_headers.Any(o => o.ToLower() == colname))
                {
                    _tuples.ForEach(o =>
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
            public static string PropertyIsRequired = "Property is Required and Cannot be empty";
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
        public class CSVRow<V> where V : class
        {
            public List<CSVValue> Values { get; set; }
            public V row { get; set; }
            public string rawString { get; }
            public Validation validation { get; set; }
            public CSVRow(string line)
            {
                rawString = line;
                validation = new Validation();
                Values = new List<CSVValue>();
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

            public CSVValue GetValue(string Name)
            {
                return Values.Where(o => o.Name.ToLower() == Name.ToLower()).FirstOrDefault();
            }
            public override string ToString()
            {
                return string.Join(",", Values.Select(o => o.Value).ToArray());
            }
        }
        public class CSVValue
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
            public string Email { get; set; }
            public string Gender { get; set; }
        }

        public class Lookup
        {
            public Guid LookupId { get; set; }
            public Guid FacilityId { get; set; }
            public int LookupTypeId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            //public bool IsActiveA { get; set; }
            //public bool IsNew { get; set; }
            public int? LookupTypeCategoryId { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedBy { get; set; }
            public DateTime? ModifiedOn { get; set; }
            //public bool? ConfigDisplay { get; set; }
        }

    }
}
