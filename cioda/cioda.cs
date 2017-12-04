using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Oracle.DataAccess.Client;
using Serilog;
using Serilog.Sinks.SystemConsole;
using Serilog.Sinks.File;
using Serilog.Sinks.RollingFile;
using System.Runtime.InteropServices;
using Citect.Util;

namespace ciodans {
    /// <summary>
    /// Это класс-обертка
    /// Описание функций смотри в базовом классе
    /// </summary>
    public class cioda : ciodabase {
        public int oconnect( string dsn, string uid, string pwd ) {
            int connectId = 0;
            int rc = base.oconnect( dsn, uid, pwd, ref connectId );
            return (rc == 0) ? connectId : 0;
        }
        public int oconnecthidden(string sxml, string sxpath) {
            int connectId = 0;
            int rc = base.oconnect(sxml, sxpath, ref connectId);
            return (rc == 0) ? connectId : 0;
        }
        public int log(string s) {
            return log( true, s, null );
        }
        public int logerr( string s ) {
            return log( false, s, null );
        }
        public int osetstatement( string sRequest, string sXml ) {
            int dummy=0;
            int rc = osetstatement( sRequest, ref dummy, sXml );
            return (rc == 0) ? dummy : 0;
        }
        public int oinsert( int connectId, string xpathbind, string xdoc, int suff ) {
            return oinsert( connectId, xpathbind, xdoc, suff, "" );
        }
        public int oupdate( int connectId, string xpathbind, string xdoc, int suff ) {
            return oupdate( connectId, xpathbind, xdoc, suff, "" );
        }
    }
}
