using System.Net;
using System.Net.Mail;

namespace FarmaciaSolidariaCristiana.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"] ?? "Farmacia Solidaria Cristiana";
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Recuperación de Contraseña - Farmacia Solidaria Cristiana";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; background-color: #f8f9fa; }}
                        .button {{ display: inline-block; padding: 12px 30px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Farmacia Solidaria Cristiana</h1>
                        </div>
                        <div class='content'>
                            <h2>Recuperación de Contraseña</h2>
                            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
                            <p>Para restablecer tu contraseña, haz clic en el siguiente enlace:</p>
                            <p style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Restablecer Contraseña</a>
                            </p>
                            <p><strong>Nota:</strong> Este enlace expirará en 24 horas por seguridad.</p>
                            <p>Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Farmacia Solidaria Cristiana. Todos los derechos reservados.</p>
                            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Bienvenido a Farmacia Solidaria Cristiana";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #198754; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 30px; background-color: #f8f9fa; }}
                        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>¡Bienvenido!</h1>
                        </div>
                        <div class='content'>
                            <h2>Hola {userName},</h2>
                            <p>Tu cuenta ha sido creada exitosamente en <strong>Farmacia Solidaria Cristiana</strong>.</p>
                            <p>Ahora puedes acceder al sistema para consultar información sobre medicamentos, donaciones y entregas.</p>
                            <p><strong>Importante:</strong> Tu cuenta tiene permisos de visualización. Si necesitas permisos adicionales, contacta con el administrador del sistema.</p>
                            <p>Gracias por formar parte de nuestra comunidad solidaria.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Farmacia Solidaria Cristiana. Todos los derechos reservados.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
