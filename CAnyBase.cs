﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AnyBaseLib
{
    public static class CAnyBase
    {
        public static IAnyBase Base(string name)
        {
            switch(name.ToLower())
            {
                case "sqlite": return new Bases.SQLiteDriver();
                case "mysql": return new Bases.MySQLDriver();
                case "postgre": return new Bases.PostgreDriver();
                default: throw new Exception("Unknown DB type");
            }
        }

        public static int Version()
        { return 9; }
    }
}
