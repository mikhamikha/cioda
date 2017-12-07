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
    /// Это класс-обертка для ciodabase
    /// Описание функций смотри в базовом классе
    /// </summary>
    public class cioda : ciodabase {
        /// <summary>
        /// Открытое подключение к БД
        /// </summary>
        /// <param name="dsn">Источник данных</param>
        /// <param name="uid">Имя пользователя</param>
        /// <param name="pwd">Пароль</param>
        /// <returns>Номер соединения, если успешно, иначе 0</returns>
        public int oconnect( string dsn, string uid, string pwd ) {
            int connectId = 0;
            int rc = base.oconnect( dsn, uid, pwd, ref connectId );
            return (rc == 0) ? connectId : 0;
        }
        /// <summary>
        /// Подключение к БД с помощью зашифрованного пароля в Xml-файле
        /// </summary>
        /// <param name="sXML">Имя файла с полным путем</param>
        /// <param name="sXpath">Путь до узла с требуемым атрибутом
        /// <example>sXpath = ".//SQL"</example>
        /// </param>
        /// <returns>Номер соединения, если успешно, иначе 0</returns>
        public int oconnecthidden(string sXML, string sXpath) {
            int connectId = 0;
            int rc = base.oconnect(sXML, sXpath, ref connectId);
            return (rc == 0) ? connectId : 0;
        }
        /// <summary>
        /// Логгирование информационных сообщений
        /// </summary>
        /// <param name="s">Сообщение</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int log(string s) {
            return log( true, s, null );
        }
        /// <summary>
        /// Логгирование сообщений об ошибке
        /// </summary>
        /// <param name="s">Сообщение</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int logerr(string s) {
            return log( false, s, null );
        }
        /// <summary>
        /// Установка запроса SQL для последующего заполнения параметрами и выполнения 
        /// </summary>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) 
        /// <example>Ex.: xpath = ".//Select[@name="ASNINFO"]"</example>
        /// </param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <returns>Номер установленного запроса, если успешно, иначе - 0</returns>
        public int osetstatement(string sRequest, string sXml) {
            int dummy=0;
            int rc = osetstatement( sRequest, ref dummy, sXml );
            return (rc == 0) ? dummy : 0;
        }
        /// <summary>
        /// Вставка новых данных из citect в указанную таблицу согласно биндлиста
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="xpathbind">xpath-запрос до биндлиста</param>
        /// <param name="xdoc">Имя Xml-файла с полным путем</param>
        /// <param name="suff">Суффикс, добавляемый к названию тэга или объекта</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>        
        public int oinsert( int connectId, string xpathbind, string xdoc, int suff ) {
            return oinsert( connectId, xpathbind, xdoc, suff, "" );
        }
        /// <summary>
        /// Обновление данных из citect в указанной таблице согласно биндлиста
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="xpathbind">xpath-запрос до биндлиста</param>
        /// <param name="xdoc">Имя Xml-файла с полным путем</param>
        /// <param name="suff">Суффикс, добавляемый к названию тэга или объекта</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public int oupdate(int connectId, string xpathbind, string xdoc, int suff) {
            return oupdate( connectId, xpathbind, xdoc, suff, "" );
        }
    }
}
