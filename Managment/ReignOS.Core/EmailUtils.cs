using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ReignOS.Core;

public class EmailUtils
{
	public delegate void EmailSendCallback(bool success, string message);
	
    private static void SendEmail(string address, string addressName, string subject, string body, EmailSendCallback callback, params string[] attachments)
	{
		Stream logStream = null;
		try
		{
			using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))// Gmail
			{
				// set auth
				smtpClient.UseDefaultCredentials = false;
				smtpClient.Credentials = new NetworkCredential("bounceshotrelay@gmail.com", "Gridspace3000");
				smtpClient.EnableSsl = true;

				// set from/to
				var from = new MailAddress("devs@forfunlabs.com", "ForFunLabs");// this sender seems to maybe have issues
				var to = new MailAddress(address, addressName);
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
						var logAttatchment = new Attachment(logStream, "log.txt", "text/plain");
						myMail.Attachments.Add(logAttatchment);
					}
				}

				// send
				void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
				{
					try
					{
						if (logStream != null)
						{
							logStream.Dispose();
							logStream = null;
						}
					}
					catch { }

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
					//OpenLogFile(FileMode.Append);// make sure file is-reopened after sending
				}
				
				smtpClient.SendCompleted += SmtpClient_SendCompleted;
				smtpClient.SendAsync(myMail, null);
			}

			Log.WriteLine("Log email sending...");
		}
		catch (Exception e)
		{
			if (logStream != null)
			{
				logStream.Dispose();
				logStream = null;
			}

			Log.WriteLine(e);

			try
			{
				callback?.Invoke(false, e.Message);
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex);
			}
			//OpenLogFile(FileMode.Append);// make sure file is-reopened after sending
		}
		finally
		{
			//stream.Seek(0, SeekOrigin.End);
		}
	}
}