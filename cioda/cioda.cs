﻿using System;
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


namespace ciodans {
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
    }
    public class oraset
    {
        public OracleConnection m_con;
        public OracleDataReader m_dr;
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
        /*
//        private List<KeyValuePair<int, statement>> m_ost = new List<KeyValuePair<int, statement>>();
//        private XmlNamespaceManager m_cfg_ns;
//        const string _uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
//        const string _lowercase = "abcdefghijklmnopqrstuvwxyz";
        */
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
            int rc=-1;
            try {
                if (xdoc != "") {
                    XmlDocument x = new XmlDocument();
                    x.Load(xdoc);
                    XmlNode xn = x.SelectSingleNode(xpath).Attributes.GetNamedItem(xnameattr);
                    attrvalue = ((XmlAttribute)xn).Value;
                    rc = 0;
                }
            }
            catch (XmlException e) {
                log("Ошибка чтения запроса {A} из файла {F} - {N}: {D}", xpath, xdoc, e.GetType().Name, e.Message);
                rc = -2;
            }
            catch (Exception e) {
                log("Ошибка чтения запроса {A} из файла {F} - {N}: {D}", xpath, xdoc, e.GetType().Name, e.Message);
                rc = -2;
            }
            return rc;
        }
        /// <summary>
        /// Инициализация системы логгирования
        /// </summary>
        /// <param name="path">Путь до папки для складывания логов</param>
        /// <returns>Код возврата (=0 - good, =-1 - не смогла найти путь, =-2 - не смогла создать логгер)</returns>
        public int initlog(string path) {
            int rc = -1;
            if (path != "" && Directory.Exists(path)) {
                string sp = path;
                if (sp.LastIndexOf('\\') < sp.Length - 1) sp += @"\";
                try {
                    sp = Path.GetDirectoryName(sp);
                    m_log = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.LiterateConsole()
                        .WriteTo.RollingFile(sp + "\\{Date}.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();
                    m_path = sp;
                    rc = 0;
                }
                catch (ArgumentException e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
                catch (IOException e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
            }
            else rc = -2;
            return rc;
        }
        /// <summary
        /// Положить в лог
        /// </summary>
        /// <param name="s">Основное сообщение</param>
        /// <param name="o">Массив значений тэгов сообщения o[0..n]</param>
        /// <example>log("Достигнут максисум {MAX}", nValue)</example>  
        /// <returns>Код возврата (=0 - good, =-1 - не смогла выполнить)</returns>
        public int log( string s, params object[] o ) {
            int rc = -1;
            if( s.Length>0 ) {
                try {
                    m_log.Information( s, o );
                    rc = 0;
                }
                catch (Exception e) {
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                }
            }
            return rc;
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
        /// <remarks>Метод запоминает в памяти данные регистрации в ОЗУ для повторной регистрации без параметров, например, из других методов</remarks>
        /// <seealso cref="cioda.oconnect()"/>
        /// <returns>Код возврата (=0 - good, =-1 - не смогла выполнить)</returns>
        public int oconnect(string dsn, string uid, string pwd, ref int connectId, string prc="", string role="eagle_park") {
			int rc=-1;
            string constr = "User Id=user;Password=pass;Data Source=sid; Enlist=false";
            try {
				if( dsn!="" && uid!="" && pwd!="" ) {
                    constr = constr.Replace("user", uid).Replace("pass", pwd).Replace("sid", dsn);
                    var oc = new oraset(new OracleConnection(constr)); 
                    oc.m_con.Open(); 
                    m_dsn = dsn; m_uid = uid; m_pwd = pwd;
                    rc = 0;
                    m_con_id++;
                    m_con_list.Add( m_con_id, oc );
                    connectId = m_con_id;
                    oexecute( m_con_id, "set role workspace identified by " + role, true );
                    oexecute( m_con_id, "call prc_connect(" + prc + ")", true );
                    log("Соединение #{N} {UID}@{DSN} установлено", m_con_id, uid, dsn);
                }
            }
			catch(OracleException e) {
                log("Ошибка установки соединения {UID}@{DSN} - {N}: {D}", uid, dsn, e.GetType().Name, e.Message);
            }
            return rc;
        }
        /// <summary>
        /// Отключение от БД
        /// </summary>
        /// <param name="connectID">Мдентификатор соединения</param>
        public void odisconnect(int connectId)
        {
			try {
                int _index = m_con_list.FindIndex(m => m._tkey == connectId);
                var oc = m_con_list[_index]._tvalue.m_con;

                if (oc.State == ConnectionState.Open) {
                    oc.Close();
                    oc.Dispose();
                    log("Соединение {N} разорвано", connectId);
                    m_con_list.RemoveAt(_index);
                }
            }
            catch (Exception e) {
                log("Ошибка отключения от базы данных {F} - {N}: {D}", connectId, e.GetType().Name, e.Message);
            }			
		}
        /// <summary>
        /// Установка запроса SQL для последующего заполнения параметрами и выполнения 
        /// </summary>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) <example>Ex.: xpath = "..//Select[@name="ASNINFO"]"</example></param>
        /// <example>xpath = "..//Select[@name="ASNINFO"]"</example>
        /// <param name="stmtId">out - Идентификатор запроса</param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <returns>Код возврата (=0 - good, =-1 и -2 - неудача)</returns>
        /// <seealso cref=""/>
        public int osetstatement(string sRequest, ref int stmtId, string sXml = "")
        {
            int rc = -1;
            string sask = "";

            if (sXml != "") rc = cfgReadAttr( sXml, sRequest, "ask", ref sask );

            if ( rc == 0 ) {
                try {
                    int _id = m_ost.Count==0? 1 : m_ost.Last()._id+1;
                    sask = sask.Replace("&gt", ">");
                    sask = sask.Replace("&amp;gt", ">");
                    sask = sask.Replace("&lt", "<");
                    sask = sask.Replace("&amp;lt", "<");
                    m_ost.Add(new statement(_id, sask));
                    stmtId = _id;
                }
                catch (Exception e) {
                    log("Ошибка записи запроса запроса {A} - {N}: {D}", sask, e.GetType().Name, e.Message);
                    rc = -3;
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
        /// <returns>Код возврата (=0 - good, =-1 и -2 - неудача)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
        public int osetparameter(int stmtId, string name, string value) {
            int rc = -1;
            try
            {
                statement items = m_ost.First(m => m._id == stmtId);
                string sval = value.Replace(",", ".");
                if (items._params.FindAll(n => n._tkey == name).Count == 0)
                    m_ost.First(m => m._id == stmtId)._params.Add(new Pair<string, string>(name, sval));
                else m_ost.First(m => m._id == stmtId)._params.Find(n => n._tkey == name)._tvalue = sval;
                rc = 0;
            }
            catch (InvalidOperationException e)
            {
                log("Ошибка записи значения {B} в параметр {A} запроса {F} - {N}: {D}", value, name, stmtId, e.GetType().Name, e.Message);
                rc = -2;
            }
            catch (Exception e)
            {
                log("Ошибка записи значения {B} в параметр {A} запроса {F} - {N}: {D}", value, name, stmtId, e.GetType().Name, e.Message);
                rc = -2;
            }
            return rc;
        }
        /// <summary>
        /// Добавление бинд-листа к подготавливаемому запросу
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="xpath">xpath-запрос до бинд-листа <example>Ex.: xpath = "..//BindList[@name="ASNINI"]"</example></param>
        /// <param name="xdoc">Xml-файл с полным путем</param>
        /// <returns>Код возврата (=0 - good, =-1 и -2 - неудача)</returns>
        public int oaddbindlist(int stmtId, string xpath, string xdoc)
        {
            int rc = -1;
            
            try {
                if (xdoc != "") {
                    var x = new XmlDocument();
                    x.Load(xdoc);
                    var xn = x.SelectSingleNode(xpath);
                    m_ost.First(m => m._id == stmtId)._binds.Clear();
                    foreach(XmlNode it in xn.ChildNodes) {
                        m_ost.First(m => m._id == stmtId)._binds.Add(
                                                            new Pair<string,string> (
                                                                it.Attributes.Item(0).Value,
                                                                it.Attributes.Item(1).Value )
                                                                );
                    }
                    if(xn.ChildNodes.Count > 0) rc = 0;
                }
            }
            catch (Exception e) {
                log("Ошибка привязки биндлиста {B} к запросу {A} - {N}: {D}", xpath, stmtId, e.GetType().Name, e.Message);
                rc = -2;
            }
            return rc;
        }
        /// <summary>
        /// Удаление заданного ранее osetstatement оператора
        /// </summary>
        /// <param name="stmtId">Идентификатор запроса</param>
        /// <param name="name">Название параметра</param>
        /// <param name="value">Значение параметра</param>
        /// <returns>Идентификатор был получен osetstatement</returns>
        public int oclrstatement(int stmtId)
        {
            int rc = -1;
            try {
                m_ost.Remove( new statement(stmtId, "") );
                rc = 0;
            }
            catch (Exception e) {
                log("Ошибка очистки запроса {F} - {N}: {D}", stmtId, e.GetType().Name, e.Message);
                rc = -2;
            }
            return rc;
        }
        /// <summary>
        /// Выполнить подготовленный запрос SQL
        /// </summary>
        /// <param name="connectID">Идентификатор соединения</param>
        /// <param name="stmtId">Идентификатор запросa</param>
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, =-1 - запрос не выполнен, =-2 - ошибка чтения XML)</returns>
        /// <remarks>Идентификатор был получен osetstatement</remarks>
        public int oexecute(int connectId, int stmtId, bool fLog = true)
        {
            int rc = -1;
            try
            {
                statement _st = m_ost.First(m => m._id == stmtId);
                string _ask = _st._request;
                var _params = _st._params;
                foreach (Pair<string, string> m in _params) _ask = _ask.Replace(m._tkey, m._tvalue);
                //            var oc = m_con_list.First(m => m._tkey == connectId)._tvalue;
                rc = oexecute(connectId, _ask, _st._binds.Count == 0, fLog);
            }
            catch (Exception e)
            {
                log("Ошибка выполнения запроса '{A}' - {N}: {D}", stmtId, e.GetType().Name, e.Message);
                rc = -3;
            }            
            return rc;
        }
        /// <summary>
        /// Выполнить запрос SQL (предложение или из файла)
        /// </summary>
        /// <param name="connectID">Идентификатор соединения</param>
        /// <param name="sRequest">Запрос(или имя запроса в Xml-файле) <example>Ex.:.//Select[@name="readTanks"]</example></param>
        /// <param name="sXml">[Имя Xml-файла с полным путем]</param>
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, =-1 - запрос не выполнен, =-2 - ошибка чтения XML)</returns>
        public int oexecute(int connectId, string sRequest = "", string sXml = "", bool fLog = true)
        {
            int rc = -1;
            string sask = sRequest;

            if (sXml != "") rc = cfgReadAttr(sXml, sRequest, "ask", ref sask);
            
            if(rc==0) {
                oraset oc = m_con_list.First(m => m._tkey == connectId)._tvalue;
                oexecute(connectId, sask, true, fLog);
            }
            return rc;
        }
        /// <summary>
        /// Выполнить запрос SQL
        /// </summary>
        /// <param name="oc">Ссылка на соединение OracleConnection</param>
        /// <param name="ask">Запрос(или имя запроса в Xml-файле)</param>
        /// <param name="fLog">[true - положить результат в лог]</param>
        /// <returns>Код возврата (=0 - good, =-1 - запрос не выполнен, =-2 - ошибка чтения XML)</returns>
        private int oexecute(int connectId, string ask, bool fNoRec, bool fLog = true)
        {
            int rc = -1;
            string sask="";
            try {
                var oc = m_con_list.First(m => m._tkey == connectId)._tvalue.m_con;
                if(oc.State == ConnectionState.Open) {
                    sask=ask;
                    var ocm = oc.CreateCommand();
                    ocm.CommandText = ask;
                    if (fNoRec) {
                        ocm.ExecuteNonQuery();
                        ocm.Dispose();
                    }
                    else {
                        m_con_list.First(m => m._tkey == connectId)._tvalue.m_dr = ocm.ExecuteReader();
                    }
                    string scont = "set role workspace";
                    if (sask.ToLower().Contains(scont)) sask = scont;
                    if (fLog) log("{A}", sask);
                    rc = 0;
                }
            }
            catch (OracleException e) {
                log("Ошибка выполнения команды '{A}' - {N}: {D}", ask, e.GetType().Name, e.Message);
                rc = -3;
            }
            catch (Exception e) {
                log("Ошибка выполнения команды '{A}' - {N}: {D}", ask, e.GetType().Name, e.Message);
                rc = -3;
            }
            return rc;
        }
        /// <summary>
        /// Получение данных из DataReader
        /// </summary>
        /// <param name="connectId">Идентификатор соединения</param>
        /// <param name="numRec">Номер выбираемой записи</param>
        /// <returns></returns>
        public int ogetrecord(int connectId, int stmtId, int numRec) {
            int rc = -1;
            try {
                var _reader = m_con_list.First(m => m._tkey == connectId)._tvalue.m_dr;
                var _st = m_ost.First(m => m._id == stmtId);
                while (_reader.Read()) {
                    foreach (var col in _st._binds)
                    {
                        log("{VAR} | {COL} | {VALUE}", col._tkey, col._tvalue, _reader[col._tvalue]);
                    }
                }
                rc=0;
            }
            catch (OracleException e)
            {
                log("Ошибка выборки записи '{A}' соединения {B} - {N}: {D}", numRec, connectId, e.GetType().Name, e.Message);
                rc = -2;
            }
            catch (Exception e)
            {
                log("Ошибка выборки записи '{A}' соединения {B} - {N}: {D}", numRec, connectId, e.GetType().Name, e.Message);
                rc = -2;
            }

            return rc;
        }
        public int oend(int connectId, int stmtId=0) {
            int rc = -1;
            try {            
                m_con_list.First(m => m._tkey == connectId)._tvalue.m_dr.Close();
                if (stmtId!=0) rc=oclrstatement(stmtId);
                rc=0;
            }
            catch (Exception e)
            {
                log("Ошибка oend({A},{B}) - {N}: {D}", connectId, stmtId, e.GetType().Name, e.Message);
                rc = -3;
            }
            return rc;
        }
    }
}