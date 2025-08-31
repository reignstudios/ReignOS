using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Collections.Generic;

namespace ReignOS.Core;

public static class EmailUtils
{
	public delegate void EmailSendCallback(bool success, string message);
	
    public static void SendEmail(string fromName, string fromAddress, string fromPass, string toName, string toAddress, string subject, string body, EmailSendCallback callback, string[] attachments)
    {
	    var streams = new List<FileStream>();
	    void DisposeStreams()
	    {
		    try
		    {
			    if (streams != null)
			    {
				    foreach (var stream in streams) stream.Dispose();
				    streams = null;
			    }
		    }
		    catch {}
	    }
	    
		try
		{
			//var smtpClient = new SmtpClient("smtp.gmail.com", 587)// Gmail
			var smtpClient = new SmtpClient("smtp.mailersend.net", 587);// https://app.mailersend.com/
			
			// set auth
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(fromAddress, fromPass);
			smtpClient.EnableSsl = true;

			// set from/to
			var from = new MailAddress(fromAddress, fromName);// this sender seems to maybe have issues
			var to = new MailAddress(toAddress, toName);
			var myMail = new MailMessage(from, to);

			// set subject
			myMail.Subject = subject;
			myMail.SubjectEncoding = Encoding.UTF8;

			// set body
			myMail.Body = body;
			myMail.BodyEncoding = Encoding.UTF8;
			myMail.IsBodyHtml = false;

			// set attachments
			if (attachments != null)
			{
				foreach (var attachment in attachments)
				{
					var stream = new FileStream(attachment, FileMode.Open, FileAccess.Read);
					streams.Add(stream);
					var logAttatchment = new Attachment(stream, attachment, "text/plain");
					myMail.Attachments.Add(logAttatchment);
				}
			}

			// send
			void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
			{
				DisposeStreams();

				if (callback != null)
				{
					try
					{
						if (e.Error != null) callback(false, e.Error.Message);
						else if (e.Cancelled) callback(false, "Was Cancelled");
						else callback(true, "Success!");
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex);
					}
				}

				if (e.Error != null) Log.WriteLine("Log email send failed: " + e.Error.Message);
				else if (e.Cancelled) Log.WriteLine("Log email send cancelled");
				else Log.WriteLine("Log email send Success!");
				
				smtpClient.Dispose();
			}
			
			smtpClient.SendCompleted += SmtpClient_SendCompleted;
			smtpClient.SendAsync(myMail, null);

			Log.WriteLine("Log email sending...");
		}
		catch (Exception e)
		{
			DisposeStreams();
			Log.WriteLine(e);

			try
			{
				callback?.Invoke(false, e.Message);
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex);
			}
		}
	}
}