using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace DBManager
{
    /// <summary> 
    /// The DBManager, can manage calls to a database. 
    /// A DBmanager has ONE connection which it uses. 
    /// It can have 1 active transaction in which ALL activity will participate
    /// Currently it can have one CMD with paramater collection which can be used accessed
    /// The Paramaters which are stored in the Sql private SqlCommand dbCmdParams are used in all methods which are suffixed with Params
    /// </summary>
    /// <history>
    /// BO 19/04/04	Initial Version
    /// </history>
    /// TODO: make it disposable, or destructor which closes all cmds etc
    public class DBManager
    {
        private SqlConnection dbCon;
        private string sConn;
        private SqlTransaction dbTransaction;
        private bool inTransaction;
        private bool connectionOpen;
        private SqlCommand dbCmdParams;
        private bool logAll;

        public DBManager(string connectionString)
        {

            sConn = connectionString;

            //			object x = System.Configuration.ConfigurationManager.AppSettings["LOGDBMANAGER"];
            SetLog();

        }

        public DBManager()
        {


            sConn = System.Configuration.ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString ;
            SetLog();



        }

        private void SetLog()
        {
            object x = System.Configuration.ConfigurationManager.AppSettings["LOGDBMANAGER"];
            if (x != null && x.ToString() == "YES")
                logAll = true;

        }

        /*
                public static string DBNumberValue(object thevalue,bool ConvertZeroToNull)
                {
                    string ret="null";
                    if(thevalue!=null)
                    {
                        try 
                        {
                            ret = long.Parse(thevalue.ToString()).ToString();
                            if(ret=="0" && ConvertZeroToNull)
                                ret="null";
                        }
                        catch (Exception)
                        {}
                    }
                    return ret;

                }
        */
        public string ConnString
        {
            get { return sConn; }
            set { sConn = value; }
        }


        private object DataRowSaveInsert(DataRow dr)
        {
            InitCommand();
            string sql = " insert into  " + dr.Table.TableName;
            string values = " (";
            string fields = "(";
            string comma = "";
            foreach (DataColumn dc in dr.Table.Columns)
            {
                bool bPrimaryKey = false;
                for (int i = 0; i < dr.Table.PrimaryKey.Length; i++)
                {
                    if (dr.Table.PrimaryKey[i] == dc)
                        bPrimaryKey = true;
                }
                if (!bPrimaryKey)
                {
                    fields += comma + " " + dc.ColumnName;
                    values += comma + " @" + dc.ColumnName;
                    comma = ",";
                    if (dr[dc.ColumnName] == DBNull.Value)
                    {
                        AddParam("@" + dc.ColumnName, DBNull.Value);

                    }
                    else
                    {
                        switch (dc.DataType.ToString())
                        {

                            case "System.DateTime":
                                AddParam("@" + dc.ColumnName, Convert.ToDateTime(dr[dc.ColumnName]));
                                break;
                            case "System.Int32":
                                AddParam("@" + dc.ColumnName, Convert.ToInt32(dr[dc.ColumnName]));
                                break;
                            case "System.Decimal":
                                AddParam("@" + dc.ColumnName, Convert.ToDouble(dr[dc.ColumnName]));
                                break;
                            case "System.Boolean":
                                AddParam("@" + dc.ColumnName, Convert.ToBoolean(dr[dc.ColumnName]));
                                break;
                            //case "System.String":
                            default:
                                AddParam("@" + dc.ColumnName, Convert.ToString(dr[dc.ColumnName]));
                                break;

                        }
                    }
                }


            }
            values += ")";
            fields += ")";
            sql += fields + " values " + values + "; Select SCOPE_IDENTITY()";

            return ExecuteScalarParams(sql);


        }

        private object DataRowSaveUpdate(DataRow dr)
        {
            object returnval = "";
            InitCommand();
            string sql = " update   " + dr.Table.TableName + " set ";

            string fields = "";
            string comma = "";
            string and = "";
            string whr = " where ";
            foreach (DataColumn dc in dr.Table.Columns)
            {
                bool bPrimaryKey = dr.Table.PrimaryKey.Any(t => t == dc);
                if (bPrimaryKey)
                {
                    whr += and + " " + dc.ColumnName + "=" + dr[dc.ColumnName];

                    and = " and ";
                    returnval = dr[dc.ColumnName];
                }
                else
                {
                    fields += comma + " " + dc.ColumnName + " = @" + dc.ColumnName;

                    comma = ",";
                    if (dr[dc.ColumnName] == DBNull.Value)
                    {
                        AddParam("@" + dc.ColumnName, DBNull.Value);

                    }
                    else
                    {
                        //System.Web.HttpContext.Current.Trace.IsEnabled=true;
                        //System.Web.HttpContext.Current.Trace.Warn(dc.ColumnName + dr[dc.ColumnName] ,dc.DataType.ToString());
                        switch (dc.DataType.ToString())
                        {

                            case "System.DateTime":
                                AddParam("@" + dc.ColumnName, Convert.ToDateTime(dr[dc.ColumnName]));
                                break;
                            case "System.Int32":
                                AddParam("@" + dc.ColumnName, Convert.ToInt32(dr[dc.ColumnName]));
                                break;
                            case "System.Decimal":
                                AddParam("@" + dc.ColumnName, Convert.ToDouble(dr[dc.ColumnName]));
                                break;
                            case "System.Boolean":
                                AddParam("@" + dc.ColumnName, Convert.ToBoolean(dr[dc.ColumnName]));
                                break;
                            //case "System.String":
                            default:
                                AddParam("@" + dc.ColumnName, Convert.ToString(dr[dc.ColumnName]));
                                break;

                        }
                    }
                }
            }

            fields += "";
            sql += fields + whr + " ; select @@rowcount";
            object ret = ExecuteScalarParams(sql);
            if (ret.ToString() == "0")
            {


                DataRowSaveInsert(dr);
            }
            return returnval;


        }
        public object DataRowSave(DataRow dr)
        {
            if (dr.Table.PrimaryKey.Length > 0 && dr[dr.Table.PrimaryKey[0].ColumnName].ToString().Length > 0)
            {
                return DataRowSaveUpdate(dr);
            }
            return DataRowSaveInsert(dr);
        }

        public object DataRowSave(DataRow dr, string dbTablename)
        {

            object retval;
            string tmpTableName = dr.Table.TableName;
            dr.Table.TableName = dbTablename;
            retval = DataRowSave(dr);
            dr.Table.TableName = tmpTableName;
            return retval;

        }
        public static bool DBBool(object tristate)
        {
            if (tristate == DBNull.Value || Convert.ToBoolean(tristate) == false)
                return false;
            return true;
        }

        #region NonParameter Execution Methods
        public void ExecuteNonQuery(string sql)
        {
            ExecuteNonQuery(sql, "");
        }
        public void ExecuteNonQuery(string sql, string codeComment)
        {
            if (!inTransaction && logAll)
            {
                BeginTransaction();
                ExecuteNonQueryNoLog(sql);
                LogSQL(sql, codeComment);
                CommitTransaction();
            }
            else if (inTransaction && logAll)
            {
                ExecuteNonQueryNoLog(sql);
                LogSQL(sql, codeComment);

            }
            else
            {
                ExecuteNonQueryNoLog(sql);
                if (logAll)
                    LogSQL(sql, codeComment);

            }



        }

        private void LogSQL(string sql, string codeComment)
        {

            ExecuteNonQueryNoLog("insert into sqlhistory (sqlhistorySQL,sqlhistoryCodeComment,sqlhistoryUserName) values ('" + sql.Replace("'", "''") + "','" + codeComment.Replace("'", "''") + "' ,'" + System.Web.HttpContext.Current.User.Identity.Name + "')");


        }

        private void LogSQL(SqlCommand cmd, string codeComment)
        {

            string sql = cmd.CommandText;
            sql.Replace("'", "''");
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (param.Value == DBNull.Value)
                {
                    sql = sql.Replace(param.ParameterName, "null");

                }
                else
                {

                    switch (param.SqlDbType)
                    {
                        case SqlDbType.DateTime:
                            sql = sql.Replace(param.ParameterName, "'" + param.Value + "'");
                            break;
                        case SqlDbType.NVarChar:
                            sql = sql.Replace(param.ParameterName, "'" + param.Value.ToString().Replace("'", "''") + "'");
                            break;
                        case SqlDbType.Int:
                            sql = sql.Replace(param.ParameterName, param.Value.ToString());
                            break;
                        case SqlDbType.Bit:
                            sql = sql.Replace(param.ParameterName, param.Value.ToString());
                            break;


                    }
                }

            }
            ExecuteNonQueryNoLog("insert into sqlhistory (sqlhistorySQL,sqlhistoryCodeComment,sqlhistoryUserName) values ('" + sql.Replace("'", "''") + "','" + codeComment.Replace("'", "''") + "','" + System.Web.HttpContext.Current.User.Identity.Name + "' )");


        }



        /// <summary>
        /// Executes the SQL on the Connection for this DBManager. Has no return value. 
        /// IF this DBManager is in a transaction, it will participate
        /// </summary>
        /// 
        /// <param name="sql">The sql statement to be executed</param>
        /// <returns>void</returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        private void ExecuteNonQueryNoLog(string sql)
        {
            SqlCommand command = new SqlCommand(sql);
            if (connectionOpen)
            {
                command.Connection = dbCon;
                command.CommandTimeout = dbCon.ConnectionTimeout;
                if (inTransaction)
                    command.Transaction = dbTransaction;
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sql, e);
                    throw exc;
                }
            }
            else
            {
                OpenConnection(false);
                command.Connection = dbCon;
                command.CommandTimeout = dbCon.ConnectionTimeout;
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sql, e);
                    throw exc;
                }
                finally
                {
                    CloseConnection();
                }
            }
            command.Dispose();
        }



        /// <summary>
        /// Executes the SQL on the Connection for this DBManager.  
        /// IF this DBManager is in a transaction, it will participate
        /// </summary>
        /// 
        /// <param name="sql">The sql statement to be executed</param>
        /// <returns>returns an object containing the item from the first column of the first row of the resultset</returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public object ExecuteScalar(string sql)
        {
            return ExecuteScalar(sql, "");

        }
        public object ExecuteScalar(string sql, string codeComment)
        {
            object tmp;
            SqlCommand command = new SqlCommand(sql);
            if (connectionOpen)
            {
                command.Connection = dbCon;
                command.CommandTimeout = dbCon.ConnectionTimeout;
                if (inTransaction)
                    command.Transaction = dbTransaction;
                try
                {

                    if (!inTransaction && logAll)
                    {
                        BeginTransaction();
                        command.Transaction = dbTransaction;
                        tmp = command.ExecuteScalar();
                        LogSQL(sql, codeComment);
                        CommitTransaction();

                    }
                    else if (inTransaction && logAll)
                    {
                        tmp = command.ExecuteScalar();
                        LogSQL(sql, codeComment);
                    }

                    else
                        tmp = command.ExecuteScalar();

                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sql, e);
                    throw exc;
                }
            }
            else
            {
                OpenConnection(false);
                command.Connection = dbCon;
                command.CommandTimeout = dbCon.ConnectionTimeout;
                try
                {
                    if (logAll)
                    {
                        BeginTransaction();
                        command.Transaction = dbTransaction;
                        command.Connection = dbTransaction.Connection;
                        tmp = command.ExecuteScalar();
                        LogSQL(sql, codeComment);
                        CommitTransaction();
                    }
                    else
                        tmp = command.ExecuteScalar();

                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sql, e);
                    throw exc;
                }
                finally
                {
                    CloseConnection();

                }
            }
            command.Dispose();
            return tmp;
        }


        /// <summary>
        /// Fills the datatable which was passed in with the resultset from the query which was passed in
        /// 
        /// </summary>
        /// 
        /// <param name="sql">query to execute</param>
        /// <param name="dtToFill">DataTable to fill. The datatable can be part of a dataset, and can be a typed dataset</param>
        /// <returns></returns>
        /// <history>
        /// BO April 19, 2004
        ///		Initial Version
        /// </history>
        public void FillDataTable(DataTable dtToFill, string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(sql));


            if (inTransaction)
                da.SelectCommand.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);

            da.SelectCommand.Connection = dbCon;
     //       da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            da.SelectCommand.CommandTimeout = dbCon.ConnectionTimeout;


            try
            {
                da.Fill(dtToFill);
            }
            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sql, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
            }
            da.Dispose();
        }


        /// <summary>
        /// Create a datatable  and fills it with the resultset from the query which was passed in
        /// 
        /// </summary>
        /// 
        /// <param name="sql">query to execute</param>
        /// <returns></returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public DataTable FillNewDataTable(string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(sql));

            DataTable dtReturn = new DataTable();
            if (inTransaction)
                da.SelectCommand.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);
            da.SelectCommand.Connection = dbCon;
            da.SelectCommand.CommandTimeout = dbCon.ConnectionTimeout;
            da.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            try
            {
                da.Fill(dtReturn); 
            }

            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sql, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
            }

            da.Dispose();
            return dtReturn;
        }

        public DataSet FillNewDataSet(string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(sql));

            DataSet dsReturn = new DataSet();
            if (inTransaction)
                da.SelectCommand.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);
            da.SelectCommand.Connection = dbCon;
            da.SelectCommand.CommandTimeout = dbCon.ConnectionTimeout;
            //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            try
            {
                da.Fill(dsReturn);
            }

            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sql, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
            }

            da.Dispose();
            return dsReturn;
        }
        public DataTable FillNewDataTableNoSchema(string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(sql));

            DataTable dtReturn = new DataTable();
            if (inTransaction)
                da.SelectCommand.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);
            da.SelectCommand.Connection = dbCon;
            da.SelectCommand.CommandTimeout = dbCon.ConnectionTimeout;
            try
            {
                da.Fill(dtReturn);
            }

            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sql, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
            }

            da.Dispose();
            return dtReturn;
        }


        public DataTable FillNewDataTableSchema(string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(sql));

            DataTable dtReturn = new DataTable();
            if (inTransaction)
                da.SelectCommand.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);
            da.SelectCommand.Connection = dbCon;
            //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            da.SelectCommand.CommandTimeout = dbCon.ConnectionTimeout;

            try
            {
                da.FillSchema(dtReturn, SchemaType.Source);
            }

            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sql, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
            }

            da.Dispose();
            return dtReturn;
        }


        /// <summary>
        /// Returns an IDataReader, which was created using the sql passed in
        /// Beware to call the DBManager.ClosReader(IDataReader reader) method in order to free the connection used by the reader
        /// 
        /// </summary>
        /// 
        /// <param name="sql">query to execute</param>
        /// <returns></returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public IDataReader ExecuteReader(string sql)
        {
            SqlCommand command = new SqlCommand(sql);

            if (inTransaction)
                command.Transaction = dbTransaction;
            if (!connectionOpen)
                OpenConnection(false);
            command.Connection = dbCon;
            command.CommandTimeout = dbCon.ConnectionTimeout;
            return command.ExecuteReader();

        }

        public static IDataReader ExecuteReaderStatic(string sql)
        {
            string sConn = System.Configuration.ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;
            DBManager tmpDB = new DBManager(sConn);
            return tmpDB.ExecuteReader(sql);
        }
        public static IDataReader ExecuteReaderStatic(string sql, string connkey)
        {
            string sConn = System.Configuration.ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;
            DBManager tmpDB = new DBManager(sConn);
            return tmpDB.ExecuteReader(sql);
        }

        /// <summary>
        /// Closes an open reader, and frees up the global connection
        /// </summary>
        /// <param name="reader"></param>
        public void CloseReader(IDataReader reader)
        {
            reader.Close();
            if (!connectionOpen)
                CloseConnection();


        }


        #endregion


        #region Transaction Methods
        public void BeginTransaction()
        {
            if (!inTransaction)
            {
                inTransaction = true;
                if (!connectionOpen)
                    OpenConnection();
                dbTransaction = dbCon.BeginTransaction();
            }

        }

        public void CommitTransaction()
        {
            dbTransaction.Commit();

            inTransaction = false;
            CloseConnection();
        }

        public void RollbackTransaction()
        {
            dbTransaction.Rollback();
            CloseConnection();
            inTransaction = false;
        }

        #endregion

        #region Connection Methods
        private void OpenConnection(bool keepOpen)
        {
            if (keepOpen)
                connectionOpen = true;
            dbCon = new SqlConnection(sConn);
            try
            {
                dbCon.Open();
            } 

            catch (Exception e)
            {
//                Exception exc = new Exception(e.Message + " ", e);
  //              throw exc;
                throw;
            }
        }

        public void OpenConnection()
        {
            OpenConnection(true);
        }



        public void CloseConnection()
        {
            try
            {
                connectionOpen = false;
                dbCon.Close();
                dbCon.Dispose();
            }
            catch
            { }

        }

        #endregion

        #region Parameters Execution Methods

        /// <summary>
        /// Executes the dbCmdParams which has been prepared. Has no return value. 
        /// IF this DBManager is in a transaction, it will participate
        /// </summary>
        ///  
        /// <param name="sqlCommand"></param>
        /// <returns>void</returns> 
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public void ExecuteNonQueryParams(string sqlCommand)
        {
            ExecuteNonQueryParams(sqlCommand, "");
        }

        public void ExecuteNonQueryParams(string sqlCommand, string codeComment)
        {
            dbCmdParams.CommandText = sqlCommand;
            if (connectionOpen)
            {
                dbCmdParams.Connection = dbCon;
                if (inTransaction)
                    dbCmdParams.Transaction = dbTransaction;
                if (!inTransaction && logAll)
                {
                    BeginTransaction();
                    dbCmdParams.Transaction = dbTransaction;
                    dbCmdParams.ExecuteNonQuery();

                    LogSQL(dbCmdParams, codeComment);
                    CommitTransaction();

                }
                else if (inTransaction && logAll)
                {
                    dbCmdParams.ExecuteNonQuery();

                    LogSQL(dbCmdParams, codeComment);
                }
                else
                    dbCmdParams.ExecuteNonQuery();

            }
            else
            {
                OpenConnection(false);
                dbCmdParams.Connection = dbCon;

                try
                {
                    if (logAll)
                    {
                        BeginTransaction();

                        dbCmdParams.Transaction = dbTransaction;
                        dbCmdParams.Connection = dbTransaction.Connection;

                        dbCmdParams.ExecuteNonQuery();
                        LogSQL(dbCmdParams, codeComment);
                        CommitTransaction();
                    }
                    else
                    {
                        dbCmdParams.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                    throw exc;
                }
                finally
                {
                    CloseConnection();
                }
            }
        }


        /// <summary>
        /// Executes the dbCmdParams which has been prepared.  
        /// IF this DBManager is in a transaction, it will participate
        /// </summary>
        /// 
        /// <param name="sqlCommand"></param>
        /// <returns>returns an object containing the item from the first column of the first row of the resultset</returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public object ExecuteScalarParams(string sqlCommand)
        {
            return ExecuteScalarParams(sqlCommand, "");
        }
        public object ExecuteScalarParams(string sqlCommand, string codeComment)
        {
            object tmp;
            dbCmdParams.CommandText = sqlCommand;
            if (connectionOpen)
            {
                dbCmdParams.Connection = dbCon;
                if (inTransaction)
                    dbCmdParams.Transaction = dbTransaction;
                if (!inTransaction && logAll)
                {
                    BeginTransaction();
                    dbCmdParams.Transaction = dbTransaction;

                    tmp = dbCmdParams.ExecuteScalar();
                    LogSQL(dbCmdParams, codeComment);
                    CommitTransaction();

                }
                else if (inTransaction && logAll)
                {
                    tmp = dbCmdParams.ExecuteScalar();
                    LogSQL(dbCmdParams, codeComment);
                }
                else
                    tmp = dbCmdParams.ExecuteScalar();

            }
            else
            {
                OpenConnection(false);
                dbCmdParams.Connection = dbCon;
                try
                {
                    if (logAll)
                    {
                        if (inTransaction)
                            dbCmdParams.Transaction = dbTransaction;

                        tmp = dbCmdParams.ExecuteScalar();

                        LogSQL(dbCmdParams, codeComment);

                    }
                    else
                    {
                        tmp = dbCmdParams.ExecuteScalar();


                    }
                }
                catch (Exception e)
                {
                    Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                    throw exc;
                }
                finally
                {
                    CloseConnection();
                }
            }
            dbCmdParams.Dispose();
            return tmp;
        }


        /// <summary>
        /// Fills the datatable which was passed in with the resultset from the query which was passed in
        /// 
        /// </summary>
        /// 
        /// <param name="dtToFill">DataTable to fill. The datatable can be part of a dataset, and can be a typed dataset</param>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public void FillDataTableParams(DataTable dtToFill, string sqlCommand)
        {

            SqlDataAdapter da = new SqlDataAdapter();


            dbCmdParams.CommandText = sqlCommand;
            da.SelectCommand = dbCmdParams;

            if (!connectionOpen)
                OpenConnection(false);

            dbCmdParams.Connection = dbCon;

            try
            {
                da.Fill(dtToFill);
            }
            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
                da.Dispose();

            }

        }


        /// <summary>
        /// Fills the datatable which was passed in with the resultset from the query which was passed in.
        /// Can specify the CommandType of the query.  Normally, this is either CommandType.StoredProcedure
        /// (for stored procedures that return result set as Ref Cursor) or CommandType.Text (for standard 
        /// SQL
        /// 
        /// </summary>
        /// 
        /// <param name="dtToFill">DataTable to fill. The datatable can be part of a dataset, and can be a typed dataset</param>
        /// <param name="sqlCommand"></param>
        /// <param name="sqlCommandType">CommandType of the query</param>
        /// <returns></returns>
        /// <history>
        /// WT 17/08/04	Initial Version - to support stored procedures that return resultset as Ref Cursor
        /// </history>
        public void FillDataTableParams(DataTable dtToFill, string sqlCommand, CommandType sqlCommandType)
        {

            SqlDataAdapter da = new SqlDataAdapter();


            dbCmdParams.CommandText = sqlCommand;
            dbCmdParams.CommandType = sqlCommandType;
            da.SelectCommand = dbCmdParams;
            //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            if (!connectionOpen)
                OpenConnection(false);

            dbCmdParams.Connection = dbCon;

            try
            {
                da.Fill(dtToFill);
            }
            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
                da.Dispose();

            }

        }


        /// <summary>
        /// Create a datatable  and fills it with the resultset from the query which was passed in
        /// 
        /// </summary>
        /// 
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public DataTable FillNewDataTableParams(string sqlCommand)
        {
            SqlDataAdapter da = new SqlDataAdapter();
            dbCmdParams.CommandText = sqlCommand;
            da.SelectCommand = dbCmdParams;
            //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            DataTable dtReturn = new DataTable();

            if (!connectionOpen)
                OpenConnection(false);
            dbCmdParams.Connection = dbCon;
            try { da.Fill(dtReturn); }


            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
                da.Dispose();
            }

            return dtReturn;
        }

        public DataSet FillNewDataSetParams(string sqlCommand)
        {
            System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter();
            dbCmdParams.CommandText = sqlCommand;
            da.SelectCommand = dbCmdParams;
            //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            DataSet dsReturn = new DataSet();

            if (!connectionOpen)
                OpenConnection(false);
            dbCmdParams.Connection = dbCon;
            try { da.Fill(dsReturn); }


            catch (Exception e)
            {
                Exception exc = new Exception(e.Message + " - " + sqlCommand, e);
                throw exc;
            }
            finally
            {
                if (!connectionOpen)
                    CloseConnection();
                da.Dispose();
            }

            return dsReturn;
        }

        public  DataSet FillNewCachedDataSetParams(string sqlCommand, string paramString, int minutes = 120)
        {
            
            if (minutes > 0 && 1==0
                && HttpContext.Current.Application[sqlCommand + paramString] != null 
                && HttpContext.Current.Application[sqlCommand + paramString] is DataSet)
            {
                if (Convert.ToDateTime(HttpContext.Current.Application[sqlCommand + paramString + "datetime"]).AddMinutes(minutes) < DateTime.Now)
                {
                    RefillDataSet(sqlCommand, paramString);
                }
                var ret = HttpContext.Current.Application[sqlCommand + paramString] as DataSet;
                
                return ret;
            }
            DataSet dsTmp = FillNewDataSetParams(sqlCommand);
            if (minutes > 0)
            {
                HttpContext.Current.Application[sqlCommand + paramString] = dsTmp;
                HttpContext.Current.Application[sqlCommand + paramString + "datetime"] = DateTime.Now;
                HttpContext.Current.Application[sqlCommand + paramString + "loading"] = "false";
            }
            return dsTmp;
        }
        private static object locker = new object();
        private  void RefillDataSet(string sql, string  paramString)
        {
            if (HttpContext.Current.Application[sql + paramString + "loading"] + "" != "true")
            {
                var app = HttpContext.Current.Application;
                HttpContext.Current.Application[sql + paramString + "loading"] = "true";
                var action = new Action(() =>
                {
                    lock (locker)
                    {
                        app[sql + paramString + "datetime"] = DateTime.Now;
                        app[sql + paramString] = FillNewDataSetParams(sql);
                        app[sql + paramString + "loading"] = "false";
                    }

                });
                System.Threading.ThreadStart threadStart = new System.Threading.ThreadStart(action);
                System.Threading.Thread thread = new System.Threading.Thread(threadStart);
                thread.Start();
            }
        }









        /// <summary>
        /// Returns an IDataReader, which was created using the sql passed in
        /// Beware to call the DBManager.ClosReader(IDataReader reader) method in order to free the connection used by the reader
        /// 
        /// </summary>
        /// 
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        /// <history>
        /// BO 19/04/04	Initial Version
        /// </history>
        public IDataReader ExecuteReaderParams(string sqlCommand)
        {
            dbCmdParams.CommandText = sqlCommand;


            if (!connectionOpen)
                OpenConnection(false);

            dbCmdParams.Connection = dbCon;
            return dbCmdParams.ExecuteReader();

        }

        public static IDataReader ExecuteReaderStaticParams(string sqlCommand)
        {
            DBManager tmpDB = new DBManager();
            return tmpDB.ExecuteReaderParams(sqlCommand);

        }
        /// <summary>
        /// Closes an open reader, and frees up the global connection
        /// </summary>
        /// <param name="reader"></param>
        public void CloseReaderParams(IDataReader reader)
        {
            reader.Close();
            if (!connectionOpen)
                CloseConnection();

        }

        #endregion

        #region ParameterPreperation
        /// <summary>
        /// Sets the internally used Command object to its initial state, thus clearing 
        /// any previously added parameters.
        /// Call this method before starting to use the AddInParam and AddOutParam methods
        /// </summary>
        public void InitCommand()
        {
            InitCommand(CommandType.Text);
        }
        public void InitCommand(CommandType commandType)
        {
            dbCmdParams = new SqlCommand {CommandType = commandType};
        }


        /// <summary>
        ///		Add a paramter that has a null value
        /// </summary>
        ///<param name="par"></param>
        /// 
        public void AddParam(System.Data.Common.DbParameter par)
        {

            if (par.Value == DBNull.Value)
            {
                AddParamNull(par.ParameterName);
            }
            else
            {
                //throw new ApplicationException(par.DbType.ToString());
                switch (par.DbType)
                {
                    case DbType.Int16:
                    case DbType.Int32:
                    case DbType.Int64:

                        AddParam(par.ParameterName, Convert.ToInt32(par.Value));
                        break;

                    case DbType.Double:
                    case DbType.Decimal:
                        AddParam(par.ParameterName, Convert.ToDouble(par.Value));


                        break;
                    case DbType.DateTime:
                    case DbType.DateTime2:
                        AddParam(par.ParameterName, Convert.ToDateTime(par.Value));
                        break;
                    case DbType.Boolean:
                        AddParam(par.ParameterName, Convert.ToBoolean(par.Value));
                        break;


                    case DbType.String:
                        AddParam(par.ParameterName, Convert.ToString(par.Value));
                        break;
                }
            }


        }
        public void AddParam(string name, int paramValue)
        {

            dbCmdParams.Parameters.Add(name, SqlDbType.Int).Value = paramValue;

        }
        public void AddParam(string name, double paramValue)
        {

            dbCmdParams.Parameters.Add(name, SqlDbType.Real).Value = paramValue;

        }
        public void AddParam(string name, string paramValue)
        {
            if (paramValue == null)
                AddParamNull(name);
            else

                dbCmdParams.Parameters.AddWithValue(name, SqlDbType.NVarChar).Value = paramValue;

        }
        public void AddParam(string name, DateTime paramValue)
        {

            if (paramValue == DateTime.MinValue)
                AddParamNull(name);
            else

                dbCmdParams.Parameters.AddWithValue(name, SqlDbType.DateTime).Value = paramValue;

        }
        public void AddParam(string name, DBNull paramValue)
        {
            dbCmdParams.Parameters.AddWithValue(name, DBNull.Value).Value = paramValue;
        }

        public void AddParam(string name, bool paramValue)
        {

            dbCmdParams.Parameters.AddWithValue(name, SqlDbType.Bit).Value = paramValue;

        }

        public void AddParamNull(string paramName)
        {
            dbCmdParams.Parameters.AddWithValue(paramName, DBNull.Value);
        }

        #endregion

  

        public DataTable ExecuteDataTable(SqlCommand command)
        {
            DataTable result = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter
            {
                SelectCommand = command
            };

            if (!connectionOpen)
                OpenConnection(false);

            command.Connection = dbCon;
            adapter.Fill(result);

           

            return result.Rows.Count == 0 ? null : result;
        }


    }

}
