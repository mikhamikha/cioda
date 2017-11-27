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

namespace ciodans {
    static class constants {
        public enum err { s_ok, e_fault, e_param, e_cfgread, e_noitems };
    }
    public sealed class ciInterop {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 300)]
        internal static string str;
        [DllImport("ctApi.dll")]
        public static extern IntPtr ctOpen(string sComp, string sUser, string sPass, int nMode);
        [DllImport("CtApi.dll")]
        public static extern bool ctClose(IntPtr handle);
        [DllImport("CtApi.dll")]
        public static extern bool ctTagWrite(IntPtr handle, string sTag, string sVal);
        [DllImport("CtApi.dll")]
        public static extern bool ctTagRead(IntPtr handle, string sTag, [In, Out] IntPtr sVal, uint dwLen);
        [DllImport("CtApi.dll")]
        public static extern bool ctTagGetProperty(IntPtr handle, string sTag, string sProperty, [In, Out] IntPtr pData, uint dwBufferLength, uint dwType);
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
    /// Класс, реализующий ячейку хранения запроса с параметрами
    /// </summary>
    public class statement : IEquatable<statement> {
        public int _id { get; set; }
        public string _request { get; set; }
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
        public statement(int id, string request) {
            _id = id;
            _request = request;
        }
        public bool Equals(statement other) {
            if (other == null) return false;
            return (this._id.Equals(other._id));
        }
        ~statement () {
            _dt.Clear();
            _dt.Dispose();
            _params.Clear();
            _binds.Clear();
        }
    }
    public class oraset
    {
        public OracleConnection m_con;
//        public OracleDataReader m_dr;
        public oraset(OracleConnection oc) {
            m_con = oc;
        }
    }
    /// <summary>
    /// Класс работы с БД Оракл через Oracle.DataAccess
    /// </summary>
    public class cioda {
        private string m_dsn;
        private string m_uid;
        private string m_pwd;
        private string m_path;
        private Serilog.Core.Logger m_log;
        private List<statement> m_ost = new List<statement>();  // список запросов к БД
        private PairList<int, oraset> m_con_list = new PairList<int, oraset>();
        private static int m_con_id = 0;

        /// <summary>
        /// Чтение значения атрибута по заданному пути
        /// </summary>
        /// <param name="xdoc">Имя файла с полным путем</param>
        /// <param name="xpath">Путь до узла с требуемым атрибутом</param>
        /// <example>xpath = "..//Select[@name="ASNINFO"]"</example>
        /// <param name="xnameattr">Название считываемого атрибута</param>
        /// <param name="attrvalue">Считанное значение атрибута</param>
        /// <returns>Код возврата (=0 - good, =-1 - не смогла найти путь, =-2 - не смогла прочитать запрос)</returns>
        public int cfgReadAttr(string xdoc, string xpath, string xnameattr, ref string attrvalue) {
            int rc=(int)constants.err.e_fault;
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
                log("Ошибка чтения запроса {A} из файла {F} - {N}: {D}", xpath, xdoc, e.GetType().Name, e.Message);
                rc = (int)constants.err.e_cfgread;
            }
            catch (Exception e) {
                log("Ошибка чтения запроса {A} из файла {F} - {N}: {D}", xpath, xdoc, e.GetType().Name, e.Message);
                rc = (int)constants.err.e_cfgread;
            }
            return rc;
        }
        /// <summary>
        /// Инициализация системы логгирования
        /// </summary>
        /// <param name="path">Путь до папки для складывания логов</param>
        /// <returns>Код возврата (=0 - good, =-1 - не смогла найти путь, =-2 - не смогла создать логгер)</returns>
        public int initlog(string path) {
            int rc = (int)constants.err.e_fault;
            if (path != "" && Directory.Exists(path)) {
                string sp = path;
                if (sp.LastIndexOf('\\') < sp.Length - 1) sp += @"\";
                try {
                    sp = Path.GetDirectoryName(sp);
                    m_log = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.LiterateConsole()
                        .WriteTo.RollingFile(sp + "\\{Date}.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();
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
        /// <summary
        /// Положить в лог
        /// </summary>
        /// <param name="type">Тип сообщения: =тру - инфо, =фолс - об ошибке</param>
        /// <param name="s">Основное сообщение</param>
        /// <param name="o">Массив значений тэгов сообщения o[0..n]</param>
        /// <example>log("Достигнут максисум {MAX}", nValue)</example>  
        /// <returns>Код возврата (=0 - good, =-1 - не смогла выполнить)</returns>
        public int log( bool type, string s, params object[] o ) {
            int rc = (int)constants.err.e_fault;
            if (s.Length > 0) {
                try {
                    if(type) m_log.Information( s, o );
                    else m_log.Error(s, o);
                    rc = (int)constants.err.s_ok;
                }
                catch (Exception e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
            }
            return rc;
        }
        public int log(string s, params object[] o) {
            return log(true, s, o);
        }
        public int logerr(string s, params object[] o) {
            return log(false, s, o);
        }
        /// <summary>
        /// Подключение к базе данных Oracle через Oracle.DataAccess
        /// </summary>
        /// <param name="dsn">Источник данных</param>
        /// <param name="uid">Имя пользователя</param>
        /// <param name="pwd">Пароль</param>
        /// <param name="connectID">Мдентификатор созданного соединения</param>
        /// <param name="prc">[Идентификатор принадлежности пользователя объекту]</param>
        /// <param name="role">[Установка роли]</param>
        /// <returns>Код возврата (=0 - good, =-1 - не смогла выполнить)</returns>
        /// <remarks>Метод после соединения выполняет сетроле и прц-коннект</remarks>
        public int oconnect(string dsn, string uid, string pwd, ref int connectId, string prc="", string role="eagle_park") {
            int rc = (int)constants.err.e_fault;
            string constr = "User Id=user;Password=pass;Data Source=sid; Enlist=false";
            try {
				if( dsn!="" && uid!="" && pwd!="" ) {
                    constr = constr.Replace("user", uid).Replace("pass", pwd).Replace("sid", dsn);
                    var oc = new oraset(new OracleConnection(constr)); 
                    oc.m_con.Open(); 
                    m_dsn = dsn; m_uid = uid; m_pwd = pwd;
                    m_con_id++;
                    m_con_list.Add( m_con_id, oc );
                    connectId = m_con_id;
                    rc=oexecute( m_con_id, "set role workspace identified by " + role, "", true );
                    if(rc==0) rc=oexecute( m_con_id, "call prc_connect(" + prc + ")", "", true );
                    log("Соединение #{N} {UID}@{DSN} установлено", m_con_id, uid, dsn);
                }
            }
			catch(OracleException e) {
                logerr("Ошибка установки соединения {UID}@{DSN} - {N}: {D}", uid, dsn, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Отключение от БД
        /// </summary>
        /// <param name="connectID">Мдентификатор соединения</param>
        /// <remarks>Метод после соединения выполняет прц-дисконнект</remarks>
        public void odisconnect(int connectId)
        {
			try {
                int _index = m_con_list.FindIndex(m => m._tkey == connectId);
                var oc = m_con_list[_index]._tvalue.m_con;

                if (oc.State == ConnectionState.Open) {
                    oexecute(connectId, "BEGIN PCK_ADM.PRC_REGISTRATION_DEL; END;", "", true); 
                    oc.Close();
                    oc.Dispose();
                    log("Соединение {N} разорвано", connectId);
                    m_con_list.RemoveAt(_index);
                }
            }
            catch (Exception e) {
                logerr("Ошибка отключения от базы данных {F} - {N}: {D}", connectId, e.GetType().Name, e.Message);
            }			
		}
        /// <summary>
        /// Установка запроса SQL для последующего заполнения параметрами и выполнения 
        /// </summary>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) <example>Ex.: xpath = "..//Select[@name="ASNINFO"]"</example></param>
        /// <param name="stmtId">out - Идентификатор запроса</param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int osetstatement(string sRequest, ref int stmtId, string sXml = "")
        {
            int rc = (int)constants.err.e_fault;
            string sask = sRequest;

            if (sXml != "") rc = cfgReadAttr( sXml, sRequest, "ask", ref sask );

            if (rc != (int)constants.err.e_cfgread) {
                try {
                    int _id = m_ost.Count==0? 1 : m_ost.Last()._id+1;
                    sask = sask.Replace("&gt", ">");
                    sask = sask.Replace("&amp;gt", ">");
                    sask = sask.Replace("&lt", "<");
                    sask = sask.Replace("&amp;lt", "<");
                    m_ost.Add(new statement(_id, sask));
                    stmtId = _id;
                    rc = (int)constants.err.s_ok;
                }
                catch (Exception e) {
                    logerr("Ошибка записи запроса запроса {A} - {N}: {D}", sask, e.GetType().Name, e.Message);
                }
            }
            return rc;
        }
        /// <summary>
        /// Установка значений параметров в сохраненный в списке запрос
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="name">Название параметра</param>
        /// <param name="value">Значение параметра</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
        public int osetparameter(int stmtId, string name, string value) {
            int rc = (int)constants.err.e_fault;
            try {
                statement items = m_ost.First(m => m._id == stmtId);
                string sval = value.Replace(",", ".");
                if (items._params.FindAll(n => n._tkey == name).Count == 0)
                    m_ost.First(m => m._id == stmtId)._params.Add(new Pair<string, string>(name.Trim(), sval));
                else m_ost.First(m => m._id == stmtId)._params.Find(n => n._tkey == name)._tvalue = sval;
                rc = (int)constants.err.s_ok;
            }
            catch (InvalidOperationException e) {
                logerr("Ошибка записи значения {B} в параметр {A} запроса {F} - {N}: {D}", value, name, stmtId, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка записи значения {B} в параметр {A} запроса {F} - {N}: {D}", value, name, stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Добавление бинд-листа к подготавливаемому запросу
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="xpathbind">xpath-запрос до бинд-листа <example>Ex.: xpath = "..//BindList[@name="ASNINI"]"</example></param>
        /// <param name="xdoc">Xml-файл с полным путем</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
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
                logerr("Ошибка привязки биндлиста {B} к запросу {A} - {N}: {D}", xpathbind, stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Удаление заданного ранее osetstatement оператора
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
        public int oclrstatement(int stmtId)
        {
            int rc = (int)constants.err.e_fault;
            try {
//                m_ost.First(m => m._id == stmtId)._dt.Dispose();
                m_ost.Remove( new statement(stmtId, "") );
                rc = (int)constants.err.s_ok;
            }
            catch (Exception e) {
                logerr("Ошибка очистки запроса {F} - {N}: {D}", stmtId, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Выполнить подготовленный запрос SQL
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="stmtId">Идентификатор запросa</param>
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
        public int oexecute(int connectId, int stmtId, bool fLog = true)
        {
            int rc = (int)constants.err.e_fault;
            try
            {
                statement _st = m_ost.First(m => m._id == stmtId);
                string _ask = _st._request;
                var _params = _st._params;
                foreach (Pair<string, string> m in _params) _ask = _ask.Replace(m._tkey, m._tvalue);
                m_ost.First(m => m._id == stmtId)._request = _ask;
                rc = oexecute(connectId, stmtId, _st._binds.Count == 0, fLog);
            }
            catch (Exception e)
            {
                logerr("Ошибка выполнения запроса '{A}' - {N}: {D}", stmtId, e.GetType().Name, e.Message);
                rc = (int)constants.err.e_param;
            }            
            return rc;
        }
        /// <summary>
        /// Выполнить запрос SQL (выражение или из файла)
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) <example>Ex.:.//Select[@name="readTanks"]</example></param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int oexecute(int connectId, string sRequest = "", string sXml = "", bool fLog = true)
        {
            int rc = (int)constants.err.e_fault;
            string sask = sRequest;

            if (sXml != "") rc = cfgReadAttr(sXml, sRequest, "ask", ref sask);
            
            if(rc != (int)constants.err.e_cfgread) {
                oraset oc = m_con_list.First(m => m._tkey == connectId)._tvalue;
                int _id = 0;
                rc = osetstatement(sask, ref _id);
                if(rc==0) rc = oexecute(connectId, _id, true, fLog);
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
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        private int oexecute(int connectId, int stmtId, bool fNoRec, bool fLog = true)
        {
            int rc = (int)constants.err.e_fault;
            string sask="";
            try {
                var _oc = m_con_list.First(m => m._tkey == connectId)._tvalue.m_con;
                statement _st = m_ost.First(m => m._id == stmtId);
                if(_oc.State == ConnectionState.Open) {
                    sask=_st._request;
                    var _ocm = _oc.CreateCommand();
                    _ocm.CommandText = sask;
                    if (fNoRec) {
                        _ocm.ExecuteNonQuery();
                    }
                    else {
//                        m_con_list.First(m => m._tkey == connectId)._tvalue.m_dr = _ocm.ExecuteReader();
                        OracleDataReader _dr = _ocm.ExecuteReader();
                        m_ost.First(m => m._id == stmtId)._dt.Clear();
                        if(!_dr.IsClosed) {
                            m_ost.First(m => m._id == stmtId)._dt.Load(_dr);
                        }
                        _dr.Dispose();
                    }
                    _ocm.Dispose();
                    string scont = "set role workspace";
                    if (sask.ToLower().Contains(scont)) sask = scont;
                    if (fLog) log("{A}", sask);
                    rc = (int)constants.err.s_ok;
                }
            }
            catch (OracleException e) {
                logerr("Ошибка выполнения команды '{A}' - {N}: {D}", sask, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка выполнения команды '{A}' - {N}: {D}", sask, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Получение строки данных из выборки и запись в тэги Citect
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="recOff">Номер выбираемой записи</param>
        /// <param name="suff">Суффикс, добавляемый к имени тэга или объекта</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int ogetrecord(int stmtId, int recOff=0, int suff=0) {
            int rc = (int)constants.err.e_fault;
            try {
                var _st = m_ost.First(m => m._id == stmtId);
                if (_st._dt.Rows.Count!=0) {
                    bool _fCi = false;
                    IntPtr cihndl = ciInterop.ctOpen("", "", "", 0);
                    string _vars = "";
                    string _cols = "";
                    string _out = "";
                    string _sfmt = "|{0,-15}";
                    foreach (var col in _st._binds) {
                        string _var = _addSuffix((string)col._tkey, suff);
                        _vars += String.Format(_sfmt, _var);
                        _cols += String.Format(_sfmt, (string)col._tvalue);
                        if (cihndl != null) {
                            string _val = _st._dt.Rows[recOff][col._tvalue].ToString();
                            _fCi |= ciInterop.ctTagWrite(cihndl, _var, _val);
                            _out += String.Format(_sfmt, _val);
                        }
                    }
                    log("{variables}", _vars);
                    log("{columns}", _cols);
                    log("{values}", _out);
                    log(_fCi, "Citect variables writed {ctWrite}", _fCi ? "successfully" : "unsuccessfully");
                    ciInterop.ctClose(cihndl);
                    rc = (_fCi) ? (int)constants.err.s_ok : (int)constants.err.e_noitems;
                }
            }
            catch (OracleException e) {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {N}: {D}", recOff, stmtId, e.GetType().Name, e.Message);
            }
            catch (Exception e) {
                logerr("Ошибка выборки записи '{A}' запроса {B} - {N}: {D}", recOff, stmtId, e.GetType().Name, e.Message);
            }

            return rc;
        }
        /// <summary>
        /// Освобождение ресурсов после выборки данных
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <returns>Код возврата (=0 - good, другие - неудача)</returns>
        public int oend(int connectId, int stmtId=0) {
            int rc = (int)constants.err.e_fault;
            try {
                m_ost.First(m => m._id == stmtId)._dt.Clear();
                if (stmtId!=0) rc=oclrstatement(stmtId);
                rc = (int)constants.err.s_ok;
            }
            catch (Exception e)
            {
                logerr("Ошибка oend({A},{B}) - {N}: {D}", connectId, stmtId, e.GetType().Name, e.Message);
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
        public int oinsert(int connectId, string xpathbind, string xdoc, int suff = 0, string sztable="") {
            int rc = (int)constants.err.e_fault;
            IntPtr pval = new IntPtr();
            IntPtr cihndl = new IntPtr(); 
            var _table = sztable;

            try {
                if (xdoc != "") {
                    bool _fCi = false;
                    cihndl = ciInterop.ctOpen("", "", "", 0);
                    string _ask = "INSERT INTO {0} ({1}) VALUES ({2})";
                    string _cols = "";
                    string _vals = "";
                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpathbind);

                    if (_table.Length == 0) _table = xn.Attributes.GetNamedItem("table").Value;

                    foreach (XmlNode it in xn.ChildNodes) {
                        string _var = _addSuffix(it.Attributes.Item(0).Value.Trim(), suff);
                        string _col = it.Attributes.Item(1).Value.Trim();
                        pval = Marshal.AllocCoTaskMem(300);
                        _fCi |= ciInterop.ctTagRead(cihndl, _var, pval , 128);
                        _cols += _col + ", ";
                        string _val = Marshal.PtrToStringAnsi(pval);
                        Marshal.FreeCoTaskMem(pval);
                        pval = (IntPtr)null;
                        _vals += _val + ", ";
                    }
                    _vals = _vals.Remove(_vals.LastIndexOf(','));
                    _cols = _cols.Remove(_cols.LastIndexOf(','));
                    log(_fCi, "Citect variables readed {ctRead}", _fCi ? "successfully" : "unsuccessfully");
                    _ask = String.Format(_ask, _table, _cols, _vals);
                    rc = oexecute(connectId, _ask, "");
                }
            }
            catch (Exception e) {
                logerr("Ошибка вставки данных биндлиста {B} в таблицу {T} - {N}: {D}", xpathbind, _table, e.GetType().Name, e.Message);
            }
            finally {
                Marshal.FreeCoTaskMem(pval);
                ciInterop.ctClose(cihndl);
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
        public int oupdate(int connectId, string xpathbind, string xdoc, int suff = 0, string sztable="") {
            int rc = (int)constants.err.e_fault;
            IntPtr pval = new IntPtr();
            IntPtr cihndl = new IntPtr();
            var _table = sztable;

            try {
                if (xdoc != "") {
                    bool _fCi = false;
                    string _ask = "UPDATE {0} set {1} WHERE {2}";
                    string _cols = "";
                    string _where = "";
                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpathbind);

                    if(_table.Length==0) _table = xn.Attributes.GetNamedItem("table").Value;
                    cihndl = ciInterop.ctOpen("", "", "", 0);
                    
                    foreach (XmlNode it in xn.ChildNodes) {
                        string _var = _addSuffix(it.Attributes.Item(0).Value.Trim(), suff);
                        string _col = it.Attributes.Item(1).Value.Trim();
                        pval = Marshal.AllocCoTaskMem(300);
                        _fCi |= ciInterop.ctTagRead(cihndl, _var, pval, 128);
                        string _val = Marshal.PtrToStringAnsi(pval);
                        Marshal.FreeCoTaskMem(pval);
                        pval = (IntPtr)null;
                        if (it.Name.ToLower().Trim() == "item") {
                            _cols += _col + " = " + _val + ", ";
                        }
                        else if (it.Name.ToLower().Trim() == "where") {
                            _where = _col + " = " + _val;
                        }
                    }
                    _cols = _cols.Remove(_cols.LastIndexOf(','));
                    log(_fCi, "Citect variables readed {ctRead}", _fCi ? "successfully" : "unsuccessfully");
                    _ask = String.Format(_ask, _table, _cols, _where);
                    rc = oexecute(connectId, _ask, "");
                }
            }
            catch (Exception e) {
                logerr("Ошибка обновления данных биндлиста {B} в таблице {T} - {N}: {D}", xpathbind, _table, e.GetType().Name, e.Message);
            }
            finally {
                Marshal.FreeCoTaskMem(pval);
                ciInterop.ctClose(cihndl);
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
    }
}
