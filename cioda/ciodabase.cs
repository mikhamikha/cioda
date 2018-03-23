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
    static class constants {
        public enum err { s_ok, e_fault, e_param, e_cfgread, e_cfgwrite, e_noitems };
    }
    /// <summary>
    /// Класс, реализующий пару ключ-значение
    /// </summary>
    /// <typeparam name="T1">Ключ</typeparam>
    /// <typeparam name="T2">Значение</typeparam>
    public sealed class Pair<T1, T2> : IEquatable<Pair<T1, T2>> {
        public T1 _tkey { get; set; }
        public T2 _tvalue { get; set; }
        public Pair(T1 t1, T2 t2) { _tkey = t1; _tvalue = t2; }
        
        public override int GetHashCode() {
            return _tkey.GetHashCode();
        }        
        
        public override bool Equals(object obj) {
            if (obj == null) return false;
            var objAsPart = obj as Pair<T1, T2>;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }

        public bool Equals(Pair<T1, T2> other) {
            if (other == null) return false;
            return (this._tkey.Equals(other._tkey));
        }

        public static bool operator ==(Pair<T1, T2> point1, Pair<T1, T2> point2) {
            return point1.Equals(point2);
        }

        public static bool operator !=(Pair<T1, T2> point1, Pair<T1, T2> point2) {
            return !point1.Equals(point2);
        }
    }
    /// <summary>
    /// Класс, реализующий список пар ключ-значение
    /// </summary>
    /// <typeparam name="T1">Ключ</typeparam>
    /// <typeparam name="T2">Значение</typeparam>
    public class PairList<T1, T2> : List<Pair<T1, T2>> {
        public void Add(T1 key, T2 value) {
            Add(new Pair<T1, T2>(key, value));
        }
    }
    /// <summary>
    /// Класс, реализующий ячейку хранения запроса и результата
    /// </summary>
    public class statement : IEquatable<statement> {
        public int _id { get; set; }
        public string _request { get; set; }
        public bool _log { get; set; }
        public PairList<string, string> _params = new PairList<string, string>();
        public PairList<string, string> _binds = new PairList<string, string>();
        public DataTable _dt = new DataTable();

        public void AddParam(string key, string value="''") {
            _params.Add(new Pair<string, string>(key, value));
        }
        public override int GetHashCode()
        {
            return _id;
        }      
        public void SetParam(string key, string value) {
            _params.First(m => m._tkey == key)._tvalue = value;
        }
        public statement(int id, string request, bool log=false) {
            _id = id;
            _request = request;
            _log = log;
        }
        public bool Equals(statement other) {
            if (other == null) return false;
            return (this._id.Equals(other._id));
        }
        ~statement () {
            /*
            using (var log = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.LiterateConsole()
                    .WriteTo.RollingFile("C:\\ProgramData\\Schneider Electric\\Citect SCADA 2016\\User\\Example\\log\\{Date}.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger()) {
                log.Information("statement destructor #{stid}", _id);
            }
             */ 
            _dt.Clear();
            _dt.Dispose();
            _params.Clear();
            _binds.Clear();
        }
    }
    /// <summary>
    /// Класс работы с БД Оракл через Oracle.DataAccess
    /// </summary>
    public class ciodabase {
        public string configFile { get; set; }
        public string userRole { get; set; }
        public string userPrc { get; set; }
        public string xpathAttr { get; set; }
        public int fLog { get; set; }
        public int connectionId { get { return m_con_id; } }
        public int statementId { get { return m_stmt_id; } }
        private List<statement> m_ost = new List<statement>();  // список запросов к БД
        private PairList<int, OracleConnection> m_con_list = new PairList<int, OracleConnection>();
        private static int m_con_id = 0;
        private static int m_stmt_id = 0;
        private string m_path;

        public ciodabase() {
        }
        /// <summary>
        /// Инициализация системы логгирования
        /// </summary>
        /// <param name="path">Путь до папки для складывания логов</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int initlog(string path) {
            int rc = (int)constants.err.e_fault;
            if (path != "" && Directory.Exists(path)) {
                string sp = path;
                if (sp.LastIndexOf('\\') < sp.Length - 1) sp += @"\";
                try {
                    sp = Path.GetDirectoryName(sp);
                    m_path = sp;
                    rc = (int)constants.err.s_ok;
                }
                catch (ArgumentException e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
                catch (IOException e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
            }
            return rc;
        }
        /// <summary>
        /// Логгирование сообщения
        /// </summary>
        /// <param name="type">Тип сообщения: =тру - инфо, =фолс - об ошибке</param>
        /// <param name="s">Основное сообщение</param>
        /// <param name="o">Массив значений тэгов сообщения o[0..n]</param>
        /// <example>log("Достигнут максисум {MAX}", nValue)</example>  
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int log( bool type, string s, params object[] o ) {
            int rc = (int)constants.err.e_fault;
            try {
                using ( var log = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.LiterateConsole()
                        .WriteTo.RollingFile(m_path + "\\{Date}.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger() ) {
                    if(type) log.Information( s, o );
                    else log.Error( s, o );
                    rc = (int)constants.err.s_ok;
                }
            }
            catch (Exception e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Логгирование информационных сообщений
        /// </summary>
        /// <param name="s">Основное сообщение</param>
        /// <param name="o">Массив значений тэгов сообщения o[0..n]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int log(string s, params object[] o) {
            return log(true, s, o);
        }
        /// <summary>
        /// Логгирование сообщений об ошибке
        /// </summary>
        /// <param name="s">Основное сообщение</param>
        /// <param name="o">Массив значений тэгов сообщения o[0..n]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int logerr(string s, params object[] o) {
            return log(false, s, o);
        }
        /// <summary>
        /// Чтение значения атрибута по заданному пути Xml-файла
        /// </summary>
        /// <param name="xdoc">Имя файла с полным путем</param>
        /// <param name="xpath">Путь до узла с требуемым атрибутом
        /// <example>xpath = ".//Select[@name="ASNINFO"]"</example>
        /// </param>
        /// <param name="xnameattr">Название считываемого атрибута</param>
        /// <param name="attrvalue">Считанное значение атрибута</param>
        /// <param name="bLog">Требуется ли логгирование</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int cfgReadAttr(string xdoc, string xpath, string xnameattr, ref string attrvalue, bool bLog=true) {
            int rc = (int)constants.err.e_cfgread;
            try {
                if (xdoc != "") {
                    XmlDocument x = new XmlDocument();
                    x.Load(xdoc);
                    XmlNode xn = x.SelectSingleNode(xpath).Attributes.GetNamedItem(xnameattr);
                    attrvalue = ((XmlAttribute)xn).Value;
                    rc = (int)constants.err.s_ok;
                }
            }
            catch (XmlException e) {
                if (bLog) log("Ошибка чтения атрибута {Attr} запроса {Ask} из файла {F} - {eName}: {e:Desc}", xnameattr, xpath, xdoc, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                if (bLog) log("Ошибка чтения атрибута {Attr} запроса {Ask} из файла {F} - {eName}: {e:Desc}", xnameattr, xpath, xdoc, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Запись атрибута в Xml-файл
        /// </summary>
        /// <param name="xdoc">Имя файла с полным путем</param>
        /// <param name="xpath">Путь до узла с требуемым атрибутом
        /// <example>xpath = ".//SQL"</example>
        /// </param>
        /// <param name="xnameattr">Название записываемого атрибута</param>
        /// <param name="attrvalue">Записываемое значение атрибута</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int cfgWriteAttr(string xdoc, string xpath, string xnameattr, string attrvalue) {
            int rc = (int)constants.err.e_cfgwrite;
            try {
                if (xdoc != "") {
                    XmlDocument x = new XmlDocument();
                    x.Load(xdoc);
                    XmlNode xn = x.SelectSingleNode(xpath).Attributes.GetNamedItem(xnameattr);
                    if (xn == null) {
                        var xa = x.CreateAttribute(xnameattr);
                        xa.Value = attrvalue;
                        x.SelectSingleNode(xpath).Attributes.Append(xa);
                    }
                    else {
                        ((XmlAttribute)xn).Value = attrvalue;
                    }
                    x.Save(xdoc);
                    rc = (int)constants.err.s_ok;
                }
            }
            catch (XmlException e) {
                log("Ошибка чтения запроса {A} из файла {F} - {eName}: {e:Desc}", xpath, xdoc, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                log("Ошибка чтения запроса {A} из файла {F} - {eName}: {e:Desc}", xpath, xdoc, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Проверка подключения к БД
        /// </summary>
        /// <param name="dsn">Источник данных</param>
        /// <param name="uid">Имя пользователя</param>
        /// <param name="pwd">Пароль</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int otestconnect(string dsn, string uid, string pwd) {
            int rc = (int)constants.err.e_fault;
            StringBuilder constr = new StringBuilder(); 

            try {
                constr.AppendFormat("User Id={0};Password={1};Data Source={2}; Enlist=false", uid, pwd, dsn);
                var oc = new OracleConnection(constr.ToString());
                oc.Open();
                oc.Close();
                oc.Dispose();
                rc = (int)constants.err.s_ok;
                if ((fLog & 1) != 0) log("Подключение {UID}@{DSN} проверено", uid, dsn);
             }
            catch (OracleException e) {
                logerr("Ошибка проверки подключения {UID}@{DSN} - {eName}: {e:Desc}", uid, dsn, e.GetType().Name, e.Message);
            }
            return rc;
        }  
        /// <summary>
        /// Сохранение зашифрованного пароля пользователя в Xml-файле с проверкой подключения
        /// </summary>
        /// <param name="sXML">Имя файла с полным путем</param>
        /// <param name="sxpath">Путь до узла с требуемым атрибутом
        /// <example>xpath = ".//SQL"</example>
        /// </param>
        /// <param name="dsn">Источник данных</param>
        /// <param name="uid">Имя пользователя</param>
        /// <param name="pwd">Пароль</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int osavedbuser( string sXML, string sxpath, string dsn, string uid, string pwd ) {
            int rc = otestconnect( dsn, uid, pwd);
            if (rc == (int)constants.err.s_ok) {
                cfgWriteAttr(sXML, sxpath, "user", uid);
                rc=cfgWriteAttr(sXML, sxpath, "code", RijndaelManagedEncryption.RijndaelManagedEncryption.EncryptRijndael(pwd, sXML));
            }
            return rc;
        }

        /// <summary>
        /// Подключение к базе данных Oracle через Oracle.DataAccess
        /// </summary>
        /// <param name="dsn">Источник данных</param>
        /// <param name="uid">Имя пользователя</param>
        /// <param name="pwd">Пароль</param>
        /// <param name="connectId">[out] Идентификатор соединения</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Метод после соединения выполняет сетроле и прц-коннект</remarks>
        public int oconnect(string dsn, string uid, string pwd, ref int connectId) {
            int rc = (int)constants.err.e_fault;
            StringBuilder constr = new StringBuilder(); 

            connectId = 0;
            try {
                constr.AppendFormat("User Id={0};Password={1};Data Source={2}; Enlist=false", uid, pwd, dsn);
                var oc = new OracleConnection(constr.ToString()); 
                oc.Open();
                if (m_con_list.Count == 0) m_con_id = 1;
                else m_con_id = m_con_list.Last()._tkey + 1;
                m_con_list.Add( m_con_id, oc );
                rc = (int)constants.err.s_ok;
                if ((fLog & 1) != 0) log("Подключение #{CONID} {UID}@{DSN} установлено", m_con_id, uid, dsn);
                connectId = m_con_id;
            }
			catch(OracleException e) {
                logerr("Ошибка установки подключения {UID}@{DSN} - {eName}: {e:Desc}", uid, dsn, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Инициализация пользователя (role, prc_connect)
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="sRole">Роль пользователя</param>
        /// <param name="sPrc">Идентификатор пользователя</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public int opostconnect(int connectId, string sRole, string sPrc) {
            int rc = (int)constants.err.e_fault;
            
            try {
                rc = oexecute(connectId, "set role workspace identified by " + sRole, "");
                if (rc == (int)constants.err.s_ok) 
                    rc = oexecute(connectId, "call prc_connect(" + sPrc + ")", "");
                if (rc != (int)constants.err.s_ok) {
                    logerr("Ошибка инициализации пользователя подключения #{CONID}", connectId);
//                    odisconnect(connectId);
                }
            }
            catch (OracleException e) {
                logerr("Ошибка инициализации пользователя подключения #{CONID}- {eName}: {e:Desc}", connectId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Подключение к БД с помощью зашифрованного пароля в Xml-файле
        /// </summary>
        /// <param name="sXML">Имя файла с полным путем</param>
        /// <param name="sXpath">Путь до узла с требуемым атрибутом
        /// <example>xpath = ".//SQL"</example>
        /// </param>
        /// <param name="connectId">[out] Идентификатор соединения</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int oconnect(string sXML, string sXpath, ref int connectId) {
            int rc = (int)constants.err.e_fault;
            string dsn = "";
            string uid = "";
            string pwd = "";

            cfgReadAttr(sXML, sXpath, "dsn", ref dsn);
            cfgReadAttr(sXML, sXpath, "user", ref uid);
            cfgReadAttr(sXML, sXpath, "code", ref pwd);
            pwd = RijndaelManagedEncryption.RijndaelManagedEncryption.DecryptRijndael(pwd, sXML);
            rc = oconnect(dsn, uid, pwd, ref connectId);
            return rc;
        }
        /// <summary>
        /// Отключение от БД
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <remarks>Метод после соединения выполняет прц-дисконнект</remarks>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public void odisconnect(int connectId)
        {
			try {
                int _index = m_con_list.FindIndex(m => m._tkey == connectId);
                var oc = m_con_list[_index]._tvalue;

                if (oc.State == ConnectionState.Open) {
                    oexecute(connectId, "BEGIN PCK_ADM.PRC_REGISTRATION_DEL; END;", "" ); 
                    oc.Close();
                    oc.Dispose();
                    if ((fLog & 1) != 0) log("Подключение #{CONID} закрыто", connectId);
                    m_con_list.RemoveAt(_index);
                }
            }
            catch (Exception e) {
                logerr("Ошибка отключения от базы данных #{CONID} - {eName}: {e:Desc}", connectId, e.GetType().Name, e.Message);
            }			
		}
        /// <summary>
        /// Установка запроса SQL для последующего заполнения параметрами и выполнения 
        /// </summary>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) 
        /// <example>Ex.: xpath = ".//Select[@name="ASNINFO"]"</example>
        /// </param>
        /// <param name="stmtId">[out] Идентификатор запроса</param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int osetstatement(string sRequest, ref int stmtId, string sXml = "")
        {
            int rc = (int)constants.err.e_fault;
            string sask = sRequest;

            if (sXml != "") rc = cfgReadAttr( sXml, sRequest, "ask", ref sask );

            if (rc != (int)constants.err.e_cfgread) {
                try {
                    if (m_ost.Count == 0) m_stmt_id = 1;
                    else m_stmt_id = m_ost.Last()._id + 1;
                    sask = sask.Replace("&gt", ">");
                    sask = sask.Replace("&amp;gt", ">");
                    sask = sask.Replace("&lt", "<");
                    sask = sask.Replace("&amp;lt", "<");
                    string slog = "   ";
                    bool blog;
                    rc = cfgReadAttr(sXml, sRequest, "log", ref slog, false);
                    blog = ((rc == (int)constants.err.s_ok) && (slog.Trim() == "1"));
//                  log("setstatement {sreq} slog={slog} log={log} rc={rc}", sRequest, slog.Trim(), blog, rc);
                    m_ost.Add(new statement(m_stmt_id, sask, blog));
                    stmtId = m_stmt_id;
                    rc = (int)constants.err.s_ok;
                }
                catch (Exception e) {
                    logerr("Ошибка записи запроса запроса {A} - {eName}: {e:Desc}", sask, e.GetType().Name, e.Message);
                }
            }
            return rc;
        }
        /// <summary>
        /// Установка значений параметров в сохраненный в списке запрос
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="sname">Название параметра</param>
        /// <param name="svalue">Значение параметра</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int osetparameter(int stmtId, string sname, string svalue) {
            int rc = (int)constants.err.e_fault;
            try {
                statement item = m_ost.First(m => m._id == stmtId);
                var val = svalue;
//                val = val.Replace(',', '.');                                // меняем в параметрах ',' на '.'
                val = val.Replace(';', ',');                                // меняем в параметрах ';' на ','
                string name = sname.Trim();
                if (item._params.FindAll(n => n._tkey == name).Count == 0)
                    item._params.Add(new Pair<string, string>(name, val));
                else item._params.Find(n => n._tkey == name)._tvalue = val;
                rc = (int)constants.err.s_ok;
                //log("setparameter {Name} = {Value1} -> {Value2}", sname, svalue, val);
            }
            catch (InvalidOperationException e) {
                logerr("Ошибка записи значения {B} в параметр {A} запроса {F} - {eName}: {e:Desc}", svalue, sname, stmtId, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка записи значения {B} в параметр {A} запроса {F} - {eName}: {e:Desc}", svalue, sname, stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Добавление бинд-листа к подготавливаемому запросу
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="xpathbind">xpath-запрос до бинд-листа 
        /// <example>Ex.: xpath = ".//BindList[@name="ASNINI"]"</example>
        /// </param>
        /// <param name="xdoc">Xml-файл с полным путем</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int oaddbindlist(int stmtId, string xpathbind, string xdoc)
        {
            int rc = (int)constants.err.e_fault;
            
            try {
                if (xdoc != "") {
                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpathbind);
                    m_ost.First(m => m._id == stmtId)._binds.Clear();
                    foreach(XmlNode it in xn.ChildNodes) {
                        m_ost.First(m => m._id == stmtId)._binds.Add(
                                                            new Pair<string,string> (
                                                                it.Attributes.Item(0).Value.Trim(),
                                                                it.Attributes.Item(1).Value.Trim() )
                                                                );
                    }
                    if (xn.ChildNodes.Count > 0) rc = (int)constants.err.s_ok;
                }
            }
            catch (Exception e) {
                logerr("Ошибка привязки биндлиста {B} к запросу {A} - {eName}: {e:Desc}", xpathbind, stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Удаление заданного ранее osetstatement оператора
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int oclrstatement(int stmtId)
        {
            int rc = (int)constants.err.e_fault;
            try {
//                m_ost.First(m => m._id == stmtId)._dt.Dispose();
                m_ost.Remove( new statement(stmtId, "") );
                rc = (int)constants.err.s_ok;
            }
            catch (Exception e) {
                logerr("Ошибка очистки запроса {F} - {eName}: {e:Desc}", stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Выполнить подготовленный запрос SQL
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="stmtId">Идентификатор запросa</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int oexecute(int connectId, int stmtId)
        {
            int rc = (int)constants.err.e_fault;
            try
            {
                statement _st = m_ost.First(m => m._id == stmtId);
                string _ask = _st._request;
                var _params = _st._params;
                foreach (Pair<string, string> m in _params) _ask = _ask.Replace(m._tkey, m._tvalue);
                m_ost.First(m => m._id == stmtId)._request = _ask;
                rc = oexecute(connectId, stmtId, _st._binds.Count == 0);
            }
            catch (Exception e)
            {
                logerr("Ошибка выполнения запроса '{A}' подключения #{CONID} - {eName}: {e:Desc}", stmtId, connectId, e.GetType().Name, e.Message);
                rc = (int)constants.err.e_param;
            }            
            return rc;
        }
        /// <summary>
        /// Выполнить запрос SQL (выражение или из файла)
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) 
        /// <example>Ex.:.//Select[@name="readTanks"]</example>
        /// </param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public int oexecute(int connectId, string sRequest = "", string sXml = "")
        {
            int rc = (int)constants.err.e_fault;
            string sask = sRequest;

            if (sXml != "") rc = cfgReadAttr(sXml, sRequest, "ask", ref sask);
            
            if(rc != (int)constants.err.e_cfgread) {
                int _id = 0;
                rc = osetstatement(sask, ref _id);
                if(rc==0) rc = oexecute(connectId, _id, true);
                oclrstatement(_id);
            }
            return rc;
        }
        /// <summary>
        /// Выполнить запрос SQL
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="fNoRec">Флаг выборки данных, если true - не надо</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        private int oexecute(int connectId, int stmtId, bool fNoRec)
        {
            int rc = (int)constants.err.e_fault;
            string sask="";
            try {
                var _oc = m_con_list.First(m => m._tkey == connectId)._tvalue;
                statement _st = m_ost.First(m => m._id == stmtId);
                if(_oc.State == ConnectionState.Open) {
                    sask=_st._request;
                    var _ocm = _oc.CreateCommand();
                    _ocm.CommandText = sask;
                    if (fNoRec) {
                        _ocm.ExecuteNonQuery();
                    }
                    else {
                        OracleDataReader _dr = _ocm.ExecuteReader();
                        m_ost.First(m => m._id == stmtId)._dt.Clear();
                        if(!_dr.IsClosed) {
                            m_ost.First(m => m._id == stmtId)._dt.Load(_dr);
                        }
                        _dr.Dispose();
                    }
                    _ocm.Dispose();
//                    string scont = "set role workspace";
                    if (sask.ToLower().Contains("set role workspace") || sask.ToLower().Contains("prc_connect")) sask = "";
                    if ((fLog & 1) != 0 && sask!="") log("{A}", sask);
                    rc = (int)constants.err.s_ok;
                }
            }
            catch (OracleException e) {
                logerr("Ошибка выполнения команды '{A}' подключения #{CONID} - {eName}: {e:Desc}", sask, connectId, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка выполнения команды '{A}' подключения #{CONID} - {eName}: {e:Desc}", sask, connectId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Получение количества выбранных строк 
        /// </summary>
        /// <param name="stmtId">Идентиыфикатор запроса</param>
        /// <returns>Количество выбранных строк</returns>
        public int ogetrecordnumber( int stmtId ) {
            int count=0;
            try {
                count = m_ost.First(m => m._id == stmtId)._dt.Rows.Count;
            }
            catch(Exception e) {
                logerr("Ошибка выполнения команды getrecordcount() запроса #'{ask}' - {eName}: {e:Desc}", stmtId, e.GetType().Name, e.Message);
            }
            return count;
        }
        /// <summary>
        /// Получение строки данных из выборки и запись в тэги Citect
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="recOff">Номер выбираемой записи</param>
        /// <param name="suff">Суффикс, добавляемый к имени тэга или объекта</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int ogetrecord(int stmtId, int recOff = 0, int suff = 0) {
            int rc = (int)constants.err.e_fault;
            uint cihndl = 0;
            try {
                var _st = m_ost.First(m => m._id == stmtId);
                if (_st._dt.Rows.Count != 0) {
                    bool _fCi = false;
                    cihndl = CTAPI.ctOpen("", "", "", CTAPI.CT_OPEN_BATCH);
                    StringBuilder _vars = new StringBuilder();
                    StringBuilder _cols = new StringBuilder();
                    StringBuilder _out = new StringBuilder();
                    string _sfmt = "|{0,-15}";
                    foreach (var col in _st._binds) {
                        string _var = _addSuffix((string)col._tkey, suff);
                        _vars.AppendFormat(_sfmt, _var);
                        _cols.AppendFormat(_sfmt, (string)col._tvalue);
                        string _val = _st._dt.Rows[recOff][col._tvalue].ToString();
//                        _val = _val.Replace(',', '.');
                        _fCi |= CTAPI.ctTagWrite(cihndl, _var, _val);
                        _out.AppendFormat(_sfmt, _val);
                    }
                    if ((fLog & 1) != 0 || _st._log) {
                        log("{variables}", _vars);
                        log("{columns}", _cols);
                        log("{values}", _out);
                    }
                    string mess = string.Format("Citect variables writed {0}", (_fCi ? "successfully" : "unsuccessfully"));
                    if (!_fCi)
                        logerr(mess);
                    //else if ((fLog & 1) != 0) log(mess);
                    rc = (_fCi) ? (int)constants.err.s_ok : (int)constants.err.e_noitems;
                }
            }
            catch (OracleException e) {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {eName}: {e:Desc}", recOff, stmtId, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {eName}: {e:Desc}", recOff, stmtId, e.GetType().Name, e.Message);
            }
            finally {
                CTAPI.ctClose(cihndl);
            }

            return rc;
        }
        /// <summary>
        /// Получение набора строк данных из выборки и запись в тэги Citect
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="recOff">Номер первой выбираемой записи</param>
        /// <param name="recOffLast">Номер последней выбираемой записи</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int ogetrecordset( int stmtId, int recOff = 1, int recOffLast = 0 )
        {
            int rc = (int)constants.err.e_fault;
            uint cihndl = 0;
//            CTOVERLAPPED lpOv = new CTOVERLAPPED();

            try
            {
                var _st = m_ost.First(m => m._id == stmtId);
                if (_st._dt.Rows.Count != 0)
                {
                    bool _fCi = false;
                    cihndl = CTAPI.ctOpen("", "", "", CTAPI.CT_OPEN_BATCH);
                    StringBuilder _vars = new StringBuilder();
                    StringBuilder _cols = new StringBuilder();
                    StringBuilder _out = new StringBuilder();
//                    StringBuilder _wr = new StringBuilder();
//                    StringBuilder _res = new StringBuilder();
                    string _sfmt = "|{0,-15}";
                    for (int nrow = recOff; nrow > 0 && nrow <= recOffLast; nrow++) {
                        _vars.Clear();
                        _cols.Clear();
                        _out.Clear();
                        foreach (var col in _st._binds) {
                            string _var = _addSuffix((string)col._tkey, nrow);
                            _vars.AppendFormat(_sfmt, _var);
                            _cols.AppendFormat(_sfmt, (string)col._tvalue);
                            string _val = _st._dt.Rows[nrow-1][col._tvalue].ToString();
//                            _wr.Clear();
//                            _wr.AppendFormat("TagWrite(\"{0}\", \"{1}\"", _var, _val);
//                            CTAPI.ctCicode(cihndl, _wr.ToString(), 0, 0, _res, 0, IntPtr.Zero);
                            _fCi |= CTAPI.ctTagWrite(cihndl, _var, _val);
                            _out.AppendFormat(_sfmt, _val);
                        }
                        if (nrow == recOff) 
                            log("{columns}", _cols.ToString() );
                        log("{variables}", _vars.ToString() );
                        log("{values}", _out.ToString() );
                    }
                    string mess = string.Format("Citect variables writed {0}", (_fCi ? "successfully" : "unsuccessfully"));
                    if (!_fCi)
                        logerr(mess);
                    else if ((fLog & 1) != 0) log(mess);
                    rc = (_fCi) ? (int)constants.err.s_ok : (int)constants.err.e_noitems;
                }
            }
            catch (OracleException e)
            {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {eName}: {e:Desc}", recOff, stmtId, e.GetType().Name, e.Message);
            }
            catch (Exception e)
            {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {eName}: {e:Desc}", recOff, stmtId, e.GetType().Name, e.Message);
            }
            finally
            {
                CTAPI.ctClose(cihndl);
            }

            return rc;
        }        /// <summary>
        /// Освобождение ресурсов после выборки данных
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        /// <remarks>Идентификатор stmtId был получен osetstatement()</remarks>
        public int oend( int stmtId = 0) {
            int rc = (int)constants.err.e_fault;
            try {
                if (stmtId != 0) {
                    m_ost.First(m => m._id == stmtId)._dt.Clear();
                    rc = oclrstatement(stmtId);
                    rc = (int)constants.err.s_ok;
                    //log("oend({id})", stmtId);
                }
            }
            catch (Exception e)
            {
                logerr("Ошибка oend({A}) - {eName}: {e:Desc}", stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Вставка новых данных из citect в указанную таблицу согласно биндлиста
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="xpathbind">xpath-запрос до биндлиста</param>
        /// <param name="xdoc">Имя Xml-файла с полным путем</param>
        /// <param name="suff">Суффикс, добавляемый к названию тэга или объекта</param>
        /// <param name="sztable">Имя таблицы <remarks>Если пустая строка, то берем из биндлиста</remarks></param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public int oinsert(int connectId, string xpathbind, string xdoc, int suff = 0, string sztable = "") {
            int rc = (int)constants.err.e_fault;
            uint cihndl = 0;
            var _table = sztable;

            try {
                if (xdoc != "") {
                    bool _fCi = false;
                    cihndl = CTAPI.ctOpen("", "", "", CTAPI.CT_OPEN_BATCH);
                    string _ask = "INSERT INTO {0} ({1}) VALUES ({2})";
                    StringBuilder _cols = new StringBuilder();
                    StringBuilder _vals = new StringBuilder();

                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpathbind);

                    if (_table.Length == 0) _table = xn.Attributes.GetNamedItem("table").Value;
                    
                    foreach (XmlNode it in xn.ChildNodes) {
                        string _var = it.Attributes.Item(0).Value.Trim();
                        string _apo = _removeApo(ref _var);
                        _var = _addSuffix(_var, suff);
                        string _col = it.Attributes.Item(1).Value.Trim();
                        StringBuilder pval = new StringBuilder(256);
                        _fCi |= CTAPI.ctTagRead(cihndl, _var, pval, 128);
                        _cols.AppendFormat("{0}, ", _col);
                        _vals.AppendFormat("{0}{1}{0}, ", _apo, pval.ToString());
                    }
                    _vals = _vals.Remove(_vals.ToString().LastIndexOf(','), _vals.Length - _vals.ToString().LastIndexOf(','));
                    _cols = _cols.Remove(_cols.ToString().LastIndexOf(','), _cols.Length - _cols.ToString().LastIndexOf(','));
                    string mess = string.Format("Citect variables readed {0}", (_fCi ? "successfully" : "unsuccessfully"));
                    if (!_fCi)
                        logerr(mess);
                    //else if ((fLog & 1) != 0) log(mess); 
                    
                    _ask = String.Format(_ask, _table, _cols.ToString(), _vals.ToString());
                    rc = oexecute(connectId, _ask, "");
                }
            }
            catch (Exception e) {
                logerr("Ошибка вставки данных биндлиста {B} в таблицу {T} подключения #{CONID} - {eName}: {e:Desc}", xpathbind, _table, connectId, e.GetType().Name, e.Message);
            }
            finally {
//                Marshal.FreeCoTaskMem(pval);
//                ciInterop.ctClose(cihndl);
                CTAPI.ctClose(cihndl);
            }
            return rc;
        }
        /// <summary>
        /// Обновление данных из citect в указанной таблице согласно биндлиста
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="xpathbind">xpath-запрос до биндлиста</param>
        /// <param name="xdoc">Имя Xml-файла с полным путем</param>
        /// <param name="suff">Суффикс, добавляемый к названию тэга или объекта</param>
        /// <param name="sztable">Имя таблицы <remarks>Если пустая строка, то берем из биндлиста</remarks></param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор connectId был получен oconnect()</remarks>
        public int oupdate(int connectId, string xpathbind, string xdoc, int suff = 0, string sztable = "") {
            int rc = (int)constants.err.e_fault;
            uint cihndl = 0;
            var _table = sztable;

            try {
                if (xdoc != "") {
                    bool _fCi = false;
                    string _ask = "UPDATE {0} set {1} WHERE {2}";
                    StringBuilder _vals = new StringBuilder();
                    StringBuilder _where = new StringBuilder();

                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpathbind);

                    if(_table.Length==0) _table = xn.Attributes.GetNamedItem("table").Value;

                    cihndl = CTAPI.ctOpen("", "", "", CTAPI.CT_OPEN_BATCH);
                    foreach (XmlNode it in xn.ChildNodes) {
                        string _var = it.Attributes.Item(0).Value.Trim();
                        string _apo = _removeApo(ref _var);
                        _var = _addSuffix(_var, suff);
                        string _col = it.Attributes.Item(1).Value.Trim();
                        StringBuilder pval = new StringBuilder(256);
                        _fCi |= CTAPI.ctTagRead(cihndl, _var, pval, 128);
                        if (it.Name.ToLower().Trim() == "item") {
                            _vals.AppendFormat("{0} = {1}{2}{1}, ", _col, _apo, pval.ToString());
                        }
                        else if (it.Name.ToLower().Trim() == "where") {
                            _where.Remove(0, _where.Length); _where.AppendFormat( "{0} = {1}", _col, pval.ToString() );
                        }
                    }
                    _vals = _vals.Remove(_vals.ToString().LastIndexOf(','), _vals.Length - _vals.ToString().LastIndexOf(','));
                    string mess = string.Format("Citect variables readed {0}", (_fCi ? "successfully" : "unsuccessfully"));
                    if (!_fCi)
                        logerr(mess);
                    //else if ((fLog & 1) != 0) log(mess); 
                    _ask = String.Format(_ask, _table, _vals.ToString(), _where.ToString());
                    rc = oexecute(connectId, _ask, "");
                }
            }
            catch (Exception e) {
                logerr("Ошибка обновления данных биндлиста {B} в таблице {T} подключения #{CONID} - {eName}: {e:Desc}", xpathbind, _table, connectId, e.GetType().Name, e.Message);
            }
            finally {
                CTAPI.ctClose(cihndl);
            }
            return rc;
        }
        /// <summary>
        /// Функция замены суффикса в имени тэга
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <param name="ival">Добавляемый суффикс</param>
        /// <returns>Возврат строки</returns>
        /// <remarks>
        /// Замена части строки, начинающейся символом '#' и заканчивающейся '.' или EOL, на суффикс, 
        /// дополненный нулями слева по длине заменяемой строки
        /// </remarks>
        private string _addSuffix(string str, int ival) {
            string _var = str;
            int _ind = _var.IndexOf('#');
            if (_ind > 0) {
                int _indE = _var.IndexOf('.', _ind + 1);
                int _len = ((_indE > 0) ? _indE : _var.Length) - _ind;
                _var = _var.Remove(_ind, _len);
                _var = _var.Insert(_ind, ival.ToString().PadLeft(_len, '0'));
            }
            return _var;
        }
        private string _removeApo(ref string str)
        {
            string _var = str;
            char[] charsToTrim = { '\'' };
            _var = _var.Trim(charsToTrim);

            string _res = (str.Length > _var.Length)?"\'":"";
            
            str = _var;

            return _res;
        }

    }
}
