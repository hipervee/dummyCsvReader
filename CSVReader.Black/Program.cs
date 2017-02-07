using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSVReader.Black
{
    class Program
    {
        static void Main(string[] args)
        {

            string filePath = "data.csv";

            //Console.WriteLine(fileContent);


            CSVFile<Person> personCsv = new CSVFile<Person>(filePath, ',');

            //Console.WriteLine(file.ToString());
            //seperate("GET ROW");
            //Console.WriteLine(file.GetRow(1).ToString());
            //seperate("GET COLUMN {NAME}");
            //file.GetColumnData("Name").ForEach(o => Console.WriteLine(o));
            //seperate("Get Properties");

            //file.GetDataAs<Employee>();

            List<Person> e = personCsv.GetData();

            //Console.WriteLine("Name,Age");
            Console.WriteLine("{0, -15}|{1, -15}|{2, -15}|{3, -15}", "First Name", "Last Name", "Email", "Age");
            Console.WriteLine("----------------------------------------------------------------");
            e.ForEach(o =>
            {
                Console.WriteLine("{0, -15}|{1, -15}|{2, -15}|{3, -15}", o.FirstName, o.LastName, o.Email, o.Age);
            });
            //Console.WriteLine(e.Count);
            Console.Read();
        }

        static void seperate(string header)
        {
            Console.WriteLine("{0}________________________________________________", header);
        }
        public class CSVFile<T> where T : class
        {
            private string fileContent;
            private char _seperator;
            private string _fileHeader;
            private List<string> _headers;
            private List<string> _lines;
            private List<FileRow> _rows;


            public CSVFile(string path, char seperator)
            {
                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        fileContent = reader.ReadToEnd();
                    };
                    _seperator = seperator;
                    _lines = new List<string>();
                    _headers = new List<string>();
                    _rows = new List<FileRow>();
                    ProcessFile();
                }
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
                        string[] values = line.Split(_seperator);
                        for (var i = 0; i < _headers.Count; i++)
                        {
                            row.Values.Add(new FileValue()
                            {
                                Name = _headers[i],
                                Value = Convert.ToString(values[i])
                            });
                        }
                        _rows.Add(row);
                    }
                }

            }

            public List<T> GetData()
            {
                PropertyInfo[] properties = typeof(T).GetProperties();
                List<T> data = new List<T>();

                _rows.ForEach(row =>
                {
                    T obj = (T)Activator.CreateInstance(typeof(T));
                    properties.AsEnumerable().ToList().ForEach(p =>
                    {
                        if (_headers.Any(o => o.ToLower() == p.Name.ToLower()))
                        {
                            if (p.PropertyType == typeof(string) && p.CanWrite)
                            {
                                p.SetValue(obj, row.GetValue(p.Name).Value);
                            }
                        }
                    });
                    data.Add(obj);
                });
                return data;
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


            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Format("Header: [{0}] Data Columns)", _headers.Count));
                sb.Append(Environment.NewLine);
                sb.Append(_fileHeader);
                sb.Append(Environment.NewLine);
                sb.Append(string.Format("Data: [{0}] Data Rows", _rows.Count));
                sb.Append(Environment.NewLine);
                _lines.ForEach(o =>
                {
                    sb.Append(o);
                    sb.Append(Environment.NewLine);
                });
                return sb.ToString();
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

        public class Employee
        {
            public string Name { get; set; }
            public string Age { get; set; }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Age { get; set; }
            public string Email { get; set; }
            public string Gender { get; set; }
        }
    }
}
