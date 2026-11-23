<%@ WebHandler Language="C#" Class="GetLastNotOk" %>

using System;
using System.Web;

public class GetLastNotOk : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {

        var db = new DBManager.DBManager();
        db.InitCommand();
        var isOk  = Convert.ToBoolean(context.Request.QueryString["IsOk"]);
        db.AddParam("@isok",isOk);
        var dt  = db.FillNewDataTable("select top 6 datetime, ip from status where isok=0 order by  datetime desc");

        string ret = "";
        foreach(System.Data.DataRow dr in dt.Rows)
        {
             var dte = Convert.ToDateTime(dr["datetime"]);
            ret += dte.ToShortDateString() +" " + dte.ToShortTimeString() + " - " + HttpUtility.HtmlEncode(Convert.ToString(dr["ip"])) + "<br/>\r\n";
        }
        context.Response.Write(ret);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}