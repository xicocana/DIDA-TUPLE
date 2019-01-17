using DTServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIDATupleImlp
{
    [Serializable]
    public class DIDATuple : IDIDATuple
    {
        public static void Main() { }

     
        private List<object> tupleList = new List<object>();
        private int size;

        private int server_port;
        private string server_name;
        private bool broadme = true;
        private bool takeme = true;


        public void setPort(int port) { server_port = port; }
        public void setName(string name) { server_name = name; }
        public string getName() { return server_name; }
        public int getPort() { return server_port; }
        public bool getBroadme() { return broadme; }
        public void setBroadme(bool val) { broadme = val; }
        public bool getTakeme() { return takeme; }
        public void setTakeme(bool val) { takeme = val; }

        public List<dynamic> GetTupleList()
        {
            return tupleList;
        }

        public DIDATuple(List<string> fields)
        {
            string fieldObj = "";
            int entrei = 0;
            dynamic myObject;
            foreach (string field in fields)
            {

                if ((field.ToString().Equals("null") || field[0].ToString().Equals("\"")) & entrei == 0 )
                {
                    string finalField = field.Replace("\"", "");
                    tupleList.Add(finalField);
                    size++;
                }
                else
                {
                    fieldObj = field;
                    string onlyObject = "";
                    if (fieldObj.Contains('('))
                    {
                        onlyObject = fieldObj.Substring(0, fieldObj.IndexOf("("));
                        //Params
                        string args = fieldObj.Substring(fieldObj.IndexOf("("), fieldObj.Length - fieldObj.IndexOf("("));

                        args =  args.Replace("(", "");
                        args = args.Replace(")", "");
                        string[] argsArray = args.Split(',');
                        dynamic[] finalArgs = new dynamic[argsArray.Length]; 
                        for (int i = 0; i < argsArray.Length; i++)
                        {
                            if (!argsArray[i].Contains("\""))
                            {
                                finalArgs[i] = Int32.Parse(argsArray[i]);
                            }
                            else
                            {
                                finalArgs[i] = argsArray[i].Replace("\"", "");
                            }
                        }

                        String AssemblyName = "DIDATupleImlp." + onlyObject;
                        var type = Type.GetType(AssemblyName);
                        myObject = Activator.CreateInstance(type, finalArgs);
                        tupleList.Add(myObject);
                    }
                    else
                    {
                        onlyObject = fieldObj;
                        String AssemblyName = "DIDATupleImlp." + onlyObject;
                        Type typeObj = Type.GetType(AssemblyName);
                        tupleList.Add(typeObj);
                    }

                  
                    size++;

                }
                
            }
        }

        public bool CompareParams(DIDATuple x)
        {
            int tam = this.size;
            for(int i=0; i<tam; i++)
            {
                int flag = 0;
                if (x.GetTupleList()[i].Equals("*") && this.GetTupleList()[i].GetType().Equals(typeof(string))) { flag = 1; }
                else if (x.GetTupleList()[i].Equals("null") && this.GetTupleList()[i].GetType() is object) { flag = 1; }
                else if(x.GetTupleList()[i].GetType().Equals(typeof(string)) && this.GetTupleList()[i].GetType().Equals(typeof(string)))
                {
                    if (x.GetTupleList()[i].Equals(this.GetTupleList()[i])) { flag = 1; }
                    else if (substr(x.GetTupleList()[i], this.GetTupleList()[i])) { flag = 1; }
                }
                else if(x.GetTupleList()[i].Equals(this.GetTupleList()[i])) { flag = 1; }
                else if((x.GetTupleList()[i].GetType() is object && this.GetTupleList()[i].GetType() is object) && !(x.GetTupleList()[i].GetType().Equals(typeof(string))))
                {
                    if (x.GetTupleList()[i].ToString() == this.GetTupleList()[i].ToString()) {
                        //falta verificar argumentos do objecto
                        flag = 1;
                    }
                }

                if(flag == 0) { return false; }
            }
            return true;
        }

        public bool substr(string a, string b)
        {
            string strinicial = a.Substring(0, a.Length-1);
            string strfinal = a.Substring(1, a.Length-1);
            if (a.EndsWith("*") && b.StartsWith(strinicial)) { return true; }
            if (a.StartsWith("*") && b.EndsWith(strfinal)) { return true; }
            return false;
        }

        public override bool Equals(object obj)
        {
            var tuple = obj as DIDATuple;
            return tuple != null &&
               this.CompareParams(tuple);
        }

        public override int GetHashCode()
        {
            return 1106077701;
        }
    }

    [Serializable]
    public class DADTestA
    {
        public int i1;
        public string s1;


        public DADTestA(string pi1, string ps1)
        {
            i1 = Int32.Parse( pi1);
            s1 = ps1;
        }

        public DADTestA(int pi1, string ps1)
        {
            i1 = pi1;
            s1 = ps1;
        }
        public bool Equals(DADTestA o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)));
            }
        }
    }

    [Serializable]
    public class DADTestB
    {
        public int i1;
        public string s1;
        public int i2;

        public DADTestB(int pi1, string ps1, int pi2)
        {
            i1 = pi1;
            s1 = ps1;
            i2 = pi2;
        }

        public bool Equals(DADTestB o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.i2 == o.i2));
            }
        }
    }

    [Serializable]
    public class DADTestC
    {
        public int i1;
        public string s1;
        public string s2;

        public DADTestC(int pi1, string ps1, string ps2)
        {
            i1 = pi1;
            s1 = ps1;
            s2 = ps2;
        }

        public bool Equals(DADTestC o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.s2.Equals(o.s2)));
            }
        }
    }

}
