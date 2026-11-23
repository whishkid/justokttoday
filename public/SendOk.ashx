<%@ WebHandler Language="C#" Class="SetOk" %>

using System;
using System.Web;

public class SetOk : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
       
        var db = new DBManager.DBManager();
        db.InitCommand();
        var isOk  = Convert.ToBoolean(context.Request.QueryString["IsOk"]);
        db.AddParam("@isok",isOk);
        db.AddParam("@ip", context.Request.UserHostAddress ?? "");
        db.ExecuteNonQueryParams("Insert into Status (isok, datetime, ip) values(@isok, getdate(), @ip)");

        
       if(isOk) {
           context.Response.Write( "Ok melding is opgeslagen");
           return;
       }

       Functions.SendMail("info@humancontenthosting.nl","whishkid@gmail.com","NIET OK JustokToday","Niet ok melding ontvangen!!");
       Functions.SendSms("0031655145854","NIET OK JustokToday");
       context.Response.Write( "NIET OK melding is opgeslagen, en doorgestuurd");

    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}