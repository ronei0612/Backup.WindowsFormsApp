using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace Backup {
    class Log {
        static public string _subject;
        static public string _message = "Com sucesso!" + Environment.NewLine;
        static public string _attachment = "";
        public static string AttachmentFile { get; set; }
        public static string EmailTo { get; set; }
        public static string EmailFrom { get; set; }
        public static string[] Cc { get; set; }
        public static string Pass { get; set; }

        
        public static string saveLog = Application.ExecutablePath;

        public static void SendEmail(string recipient, string subject, string message) {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            MailMessage mail = new MailMessage {
                From = new MailAddress(EmailTo),
                Subject = subject,
                Body = message
            };
            mail.To.Add(recipient);
            SmtpClient smtp = new SmtpClient {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(EmailFrom, Pass)
            };
            try {
                smtp.Send(mail);
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static void SendEmail(string recipient) {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            MailMessage mail = new MailMessage {
                From = new MailAddress(EmailTo),
                Subject = _subject,
                Body = _message
            };
            mail.To.Add(recipient);
            SmtpClient smtp = new SmtpClient {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(EmailFrom, Pass)
            };
            try {
                smtp.Send(mail);
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static void SendEmail(string recipient, string attachment, string[] cc = null) {
            if (File.ReadAllText(attachment) == "")
                File.AppendAllText(attachment, "Nenhuma modificação");
            _attachment = _attachment + Environment.NewLine + "Enviando e-mail";
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            MailMessage mail = new MailMessage();
            _attachment = _attachment + Environment.NewLine + "De: " + EmailFrom;
            mail.From = new MailAddress(EmailTo);
            _attachment = _attachment + Environment.NewLine + "Para: " + recipient;
            mail.To.Add(recipient);
            mail.Subject = _subject;
            mail.Body = _message;
            _attachment = _attachment + Environment.NewLine + "Adicionar anexo: " + attachment;
            if (cc != null)
                foreach (string _cc in cc) {
                    if (!string.IsNullOrEmpty(_cc)) {
                        mail.CC.Add(_cc);
                        _attachment = _attachment + Environment.NewLine + "Adicionado Cc: " + _cc;
                    }
                }
            SmtpClient smtp = new SmtpClient {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(EmailFrom, Pass)
            };
            _attachment = _attachment + Environment.NewLine + "Enviando... ";
            try {
                File.AppendAllText(AttachmentFile, _attachment);
            } catch { }
            try {
                smtp.Send(mail);
            } catch (Exception e) {
                File.AppendAllText("backup-error.log", Environment.NewLine + "Ocorreu um erro ao enviar: " + e.Message);
            }
        }

        public static void _Log(string subject, string message) {
            string log;
            // If is Linux OS
            if (Environment.OSVersion.Platform.ToString().Contains("Unix")) {
                log = Path.Combine("/tmp/", "backup-log.txt");
            } else {
                log = Path.Combine(Environment.ExpandEnvironmentVariables("%temp%"), "backup-log.txt");
            }
            File.AppendAllText(log, message);
            _message = File.ReadAllText(log);
        }

        public static string TelegramSendMessage(string apiToken, string destID, string text) {
            string urlString = $"https://api.telegram.org/bot{apiToken}/sendMessage?chat_id={destID}&text={text}";
            WebClient webclient = new WebClient();
            return webclient.DownloadString(urlString);
        }
    }
}