using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ciodans;

namespace testapp
{
    class Program {
        static void Main(string[] args) {
            int idcon1=0, idcon2=0, idstmt1=0, idstmt2=0;
            string _now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var rnd = new Random();
            double _dens = 0.72 + rnd.NextDouble()/11.0;
            cioda co = new cioda();
            co.initlog(@"F:\csharp\cioda\logs");
//            co.initlog(@"D:\Work\projects\csharp\cioda\logs");

            co.oconnect("orcl", "fox", "redfox", ref idcon1);
            co.oconnect("orcl", "fox", "redfox", ref idcon2);

            co.osetstatement(".//Select[@name=\"test insert1\"]", ref idstmt1, @"F:\csharp\cioda\logs\appcfg.xml");
            co.osetparameter(idstmt1, ":density", _dens.ToString());


            co.osetstatement( ".//Select[@name=\"test insert2\"]", ref idstmt2, @"F:\csharp\cioda\logs\appcfg.xml" );
            co.osetparameter(idstmt2, ":datetime", _now);
            _dens = 0.72 + rnd.NextDouble() / 11.0;
            co.osetparameter(idstmt2, ":density", _dens.ToString());
            
            co.oexecute(idcon2,idstmt2);
            co.oclrstatement(idstmt2);

            co.oexecute(idcon1, idstmt1);
            co.oclrstatement(idstmt1);

            co.osetstatement( ".//Select[@name=\"TANKINFO\"]", ref idstmt2, @"F:\csharp\cioda\logs\appcfg.xml" );
            co.osetparameter(idstmt2, ":seqno1", "10");
            co.osetparameter(idstmt2, ":seqno2", "20");
            co.oaddbindlist(idstmt2, ".//BindList[@name=\"TANKINI\"]", @"F:\csharp\cioda\logs\appcfg.xml");
            co.oexecute(idcon2, idstmt2);
            co.ogetrecord(idstmt2, 2, 3);
            co.ogetrecord(idstmt2, 5, 4);
            co.oend(idstmt2);

            co.oinsert(idcon2, ".//BindList[@name=\"INSERT_SENSOR_READING\"]", @"F:\csharp\cioda\logs\appcfg.xml", 3);
            co.oinsert(idcon1, ".//BindList[@name=\"INSERT_SENSOR_READING_OBJ\"]", @"F:\csharp\cioda\logs\appcfg.xml", 112);
            co.oupdate(idcon1, ".//BindList[@name=\"UPDTEST\"]", @"F:\csharp\cioda\logs\appcfg.xml", 3);
            co.oupdate(idcon2, ".//BindList[@name=\"UPDTEST\"]", @"F:\csharp\cioda\logs\appcfg.xml", 4);

            co.odisconnect(idcon2);
            co.odisconnect(idcon1);
            
            Console.Write("Press any key");
            Console.ReadKey();
        }
    }
}
