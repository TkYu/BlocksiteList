using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlocksiteList
{
    class Program
    {
        static void Main(string[] args)
        {
            new Worker().Work().GetAwaiter().GetResult();
        }
    }
}
