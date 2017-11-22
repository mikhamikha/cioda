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
            double _dens = 0.7654;
            cioda co = new cioda();
            co.initlog(@"F:\csharp\cioda\logs");
//            co.initlog(@"D:\Work\projects\csharp\cioda\logs");
        
            co.oconnect("orcl", "fox", "redfox", ref idcon1);
            co.oconnect("orcl", "fox", "redfox", ref idcon2);

            co.osetstatement(".//Select[@name=\"test insert1\"]", ref idstmt1, @"F:\csharp\cioda\logs\appcfg.xml");
            co.osetparameter(idstmt1, ":datetime", _now);            
            co.osetparameter(idstmt1, ":density", _dens.ToString());


            co.osetstatement( ".//Select[@name=\"test insert2\"]", ref idstmt2, @"F:\csharp\cioda\logs\appcfg.xml" );
            co.osetparameter(idstmt2, ":datetime", _now);
            _dens += 0.1;
            co.osetparameter(idstmt2, ":density", _dens.ToString());
            
            co.oexecute(idcon2,idstmt2);
            co.oclrstatement(idstmt2);

            co.oexecute(idcon1, idstmt1);
            co.oclrstatement(idstmt1);

            co.odisconnect(idcon1);

            co.osetstatement( ".//Select[@name=\"TANKINFO\"]", ref idstmt2, @"F:\csharp\cioda\logs\appcfg.xml" );
            co.osetparameter( idstmt2, ":seqno", "10" );
            co.oaddbindlist(idstmt2, ".//BindList[@name=\"TANKINI\"]", @"F:\csharp\cioda\logs\appcfg.xml" );
            co.oexecute(idcon2, idstmt2);
            co.ogetrecord(idcon2, idstmt2, 1);
            co.oend(idcon2, idstmt2);
            co.odisconnect(idcon2);
            
            Console.Write("Press any key");
            Console.ReadKey();
        }
    }
}
