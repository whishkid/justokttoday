using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Net.Mail;
using System.Web.Security;
using System.Net;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


/// <summary>
/// Summary description for Functions
/// </summary>
public class Functions
{

    public Functions()
    {

    }

       public static void SendSms(string to, string msg, string from = "0612317692")
    {
        using (WebClient client = new WebClient())
        {
            string username = System.Configuration.ConfigurationManager.AppSettings["MessageBirdUsername"];
            string password = System.Configuration.ConfigurationManager.AppSettings["MessageBirdPassword"];
            string url = "https://api.messagebird.com/xml/sms/?username=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password) + "&type=long&originator=" + from + "&recipients=" + to + "&message=" + HttpUtility.UrlEncode(msg) + "&gateway=1";

            var ret = client.DownloadString(url);
        }

    }

    public static void SendMail(string fromEmail, string toEmail, string subject, string body)
    {

       

        if (string.IsNullOrEmpty(fromEmail))
            fromEmail = "Just ok Today <info@humancontenthosting.nl>";

        SmtpClient smtpClient = new SmtpClient();
        MailMessage mail = new MailMessage();


        mail.To.Add(new MailAddress(toEmail, toEmail));
        mail.From = new MailAddress(fromEmail, fromEmail);
        mail.Subject = subject;
        mail.IsBodyHtml = true;

        string BodyHtml = "";
        BodyHtml = File.ReadAllText(HttpContext.Current.Server.MapPath("/templates/_mailtemplate.html"), Encoding.UTF8);

        BodyHtml = BodyHtml.Replace("[[[MAIL-BODY]]]", body);
        mail.Body = BodyHtml;

        smtpClient.Send(mail);
        
        // }
        //  catch (Exception)
        // { }

    }


}