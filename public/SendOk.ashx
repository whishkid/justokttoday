<%@ WebHandler Language="C#" Class="SetOk" %>

using System;
using System.Web;

public class SetOk : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        var req = context.Request;
        var resp = context.Response;
        resp.ContentType = "text/plain";
        resp.TrySkipIisCustomErrors = true;
        resp.Cache.SetCacheability(HttpCacheability.NoCache);

        bool isPost = req.HttpMethod == "POST";
        // GET is only still accepted for NOT OK, from pages cached before the switch to POST
        string isOkRaw = isPost ? req.Form["isok"] : req.QueryString["isok"];
        bool isOk;
        if (!bool.TryParse(isOkRaw, out isOk))
        {
            resp.StatusCode = 400;
            resp.Write("Ongeldige melding, er is niets opgeslagen.");
            return;
        }

        string website = isPost ? req.Form["website"] : null;
        string suspicion = !isPost ? "verstuurd via GET (oude pagina of bot)"
            : website == null ? "honeypot-veld ontbreekt"
            : website != "" ? "honeypot-veld ingevuld"
            : null;

        if (isOk && suspicion != null)
        {
            resp.StatusCode = 400;
            resp.Write("OK is NIET opgeslagen. Herlaad de pagina en druk opnieuw op OK.");
            return;
        }

        string ip = req.UserHostAddress ?? "";
        var db = new DBManager.DBManager();
        db.InitCommand();
        db.AddParam("@isok", isOk);
        db.AddParam("@ip", ip);
        db.ExecuteNonQueryParams("Insert into Status (isok, datetime, ip) values(@isok, getdate(), @ip)");

        if (isOk)
        {
            resp.Write("Ok melding is opgeslagen");
            return;
        }

        // A suspicious red alert is still forwarded (labelled) so a real one is never silently dropped
        string mailBody = "Niet ok melding ontvangen!! IP: " + HttpUtility.HtmlEncode(ip)
            + (suspicion != null ? "<br/>Let op, mogelijk bot: " + suspicion : "");
        string smsText = "NIET OK JustokToday" + (suspicion != null ? " (mogelijk bot)" : "");

        bool mailSent = TrySend(() => Functions.SendMail("info@humancontenthosting.nl", "whishkid@gmail.com", "NIET OK JustokToday", mailBody));
        bool smsSent = TrySend(() => Functions.SendSms("0031655145854", smsText));

        if (mailSent && smsSent)
        {
            resp.Write("NIET OK melding is opgeslagen, en doorgestuurd");
            return;
        }
        resp.StatusCode = 500;
        resp.Write("NIET OK melding is opgeslagen, maar doorsturen is " + (mailSent || smsSent ? "deels " : "") + "mislukt. Bel even!");
    }

    static bool TrySend(Action send)
    {
        try
        {
            send();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("SendOk alert failed: " + ex);
            return false;
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}