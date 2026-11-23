using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


public partial class CheckOk : System.Web.UI.Page
{


    const int HoursForReminderEmail = 15;
    const int HoursToRepeatReminder = 4;
    const int HoursToIndicateOK = 27;
    const int HoursTillNextSMS = 26;
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Expires = 0;
        var db = new DBManager.DBManager();

        var hoursSinceLastCall = db.ExecuteScalar(@"
                   select top 1 datediff(hh,datetime,getdate()) from status order by datetime desc

        ") + "";

        var hoursSinceLastReminder = db.ExecuteScalar(@"
                   select top 1 datediff(hh,datetime,getdate()) from lastreminder order by datetime desc

        ") + "";

        var hoursSinceLastSMS = db.ExecuteScalar(@"
                   select top 1 datediff(hh,datetime,getdate()) from lastSMS order by datetime desc

        ") + "";

        if ( Convert.ToInt32(hoursSinceLastCall) > HoursForReminderEmail && Convert.ToInt32(hoursSinceLastReminder) > HoursToRepeatReminder)
        {

            Functions.SendMail("info@humancontenthosting.nl", "edwadoorenbos@gmail.com", "geef aan of je ok bent", "<a href=\"http://ok.w22.nl\">klik hier om naar ok systeem te gaan</a>");
            db.ExecuteScalar("Update Lastreminder set datetime = getdate()");
        }


        var isOk = db.ExecuteScalar(@"
                   select isnull( (select top 1 isok from status where datetime > dateadd(hh,-" + HoursToIndicateOK + @",getdate()) order by datetime desc), 0)

        ") + "";

        if (isOk == "True")
        {


            Response.Write(isOk);
            return;


        }
        if (DateTime.Now.Hour > 10  && Convert.ToInt32(hoursSinceLastSMS) > HoursTillNextSMS)
        {

            Functions.SendMail("info@humancontenthosting.nl", "whishkid@gmail.com", "NIET OK JustokToday", "OF geen melding gemaakt of Niet ok melding ontvangen!!");
            Functions.SendSms("0031655145854", "NIET OK JustokToday, evt geen melding ontvangen");
            db.ExecuteScalar("Update LastSMS set datetime = getdate()");

        }
        if (DateTime.Now.Hour > 10 && Convert.ToInt32(hoursSinceLastCall) > HoursForReminderEmail && Convert.ToInt32(hoursSinceLastReminder) > HoursToRepeatReminder)
        {

            Functions.SendMail("info@humancontenthosting.nl", "whishkid@gmail.com", "NIET OK JustokToday", "OF geen melding gemaakt of Niet ok melding ontvangen!!");
        }
        Response.Write(isOk);

    }
}