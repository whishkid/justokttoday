<%@ WebHandler Language="C#" Class="GetLastOk" %>

using System;
using System.Web;

public class GetLastOk : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {

        var db = new DBManager.DBManager();
        db.InitCommand();
        var isOk  = Convert.ToBoolean(context.Request.QueryString["IsOk"]);
        db.AddParam("@isok",isOk);
        var dt  = db.FillNewDataTable("select top 10 datetime, ip from status where isok=1 order by  datetime desc");

        string ret = "";
        foreach(System.Data.DataRow dr in dt.Rows)
        {
                var dte = Convert.ToDateTime(dr["datetime"]);
            ret += dte.ToShortDateString() +" " + dte.ToLongTimeString() + " - " + HttpUtility.HtmlEncode(Convert.ToString(dr["ip"])) + "<br/>\r\n";
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