

using System;
using System.Collections.Generic;

namespace DIDATupleImpl
{
 
    public class DIDATuple
    {
        private List<object> tupleList = new List<object>();
        private int size;

        public List<object> GetTupleList()
        {
            return tupleList;
        }

        public DIDATuple(List<string> fields)
        {
            string fieldObj = "";
            int entrei = 0;

            foreach (string field in fields)
            {
                
                if (field[0].ToString().Equals("\"") & entrei == 0)
                {
                    string finalField = field.Replace("\"", "");
                    tupleList.Add(finalField);
                }
                else
                {
                    entrei = 1;
                    if (field.Contains(")"))
                    {
                        fieldObj += field;
                        object final = analisa(fieldObj);
                        fieldObj = "";
                        entrei = 0;
                        tupleList.Add(final);
                    }
                    fieldObj += field;
                  
                }
                size++;
            }
        }

        private object analisa(string field)
        {
            if (field[7].ToString().Equals("A"))
            {
                int tamanho = field.Length - 10;
                string argsJuntos = field.Substring(9, tamanho);
                string[] args = argsJuntos.Split('"');
                int arg1 = Int32.Parse(args[0]);
                string arg2 = args[1];

                DADTestA dadA = new DADTestA(arg1, arg2);
                //Type type = dadA.GetType();
                //object instance = Activator.CreateInstance(type);
                return dadA;
            }
            if (field[7].ToString().Equals("B"))
            {
                int tamanho = field.Length - 10;
                string argsJuntos = field.Substring(9, tamanho);
                string[] args = argsJuntos.Split('"');
                int arg1 = Int32.Parse(args[0]);
                string arg2 = args[1];
                int arg3 = Int32.Parse(args[2]);

                DADTestB dadB = new DADTestB(arg1, arg2, arg3);
                //Type type = dadB.GetType();
                //object instance = Activator.CreateInstance(type);
                return dadB;
            }
            if (field[7].ToString().Equals("C"))
            {
                int tamanho = field.Length - 10;
                string argsJuntos = field.Substring(9, tamanho);
                string[] args = argsJuntos.Split('"');
                int arg1 = Int32.Parse(args[0]);
                string arg2 = args[1];
                string arg3 = args[3];

                DADTestC dadC = new DADTestC(arg1, arg2, arg3);
                //Type type = dadC.GetType();
                //object instance = Activator.CreateInstance(type);
                return dadC;
            }
            return null;
        }

        public bool CompareParams(DIDATuple fields)
        {
            int count = 0;
            Boolean equals = false;
            if (fields.size == this.size)
            {
                foreach (object field in fields.GetTupleList())
                {
                    if (field.GetType().Equals(typeof(string)) && !field.Equals("*") ||
                        field.GetType().Equals(typeof(object)) && !field.Equals("null"))
                    {
                       if(field.Equals(this.GetTupleList()[count]))
                       {
                            equals = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    count++;
                }
            }
            return equals;
        }

        public class DADTestA
        {
            public int i1;
            public string s1;

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
}
