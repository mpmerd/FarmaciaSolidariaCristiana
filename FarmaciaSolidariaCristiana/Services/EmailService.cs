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

        public async Task<bool> SendTurnoAprobadoEmailAsync(string toEmail, string userName, int numeroTurno, 
            DateTime fechaTurno, string pdfPath)
        {
            try
            {
                var subject = $"Turno #{numeroTurno} Aprobado - Farmacia Solidaria Cristiana";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #198754; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 30px; background-color: #f8f9fa; }}
                            .turno-box {{ background-color: white; border-left: 4px solid #198754; padding: 20px; margin: 20px 0; }}
                            .turno-numero {{ font-size: 48px; font-weight: bold; color: #198754; text-align: center; }}
                            .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
                            .button {{ display: inline-block; padding: 12px 30px; background-color: #198754; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>✅ Turno Aprobado</h1>
                            </div>
                            <div class='content'>
                                <h2>¡Excelentes noticias, {userName}!</h2>
                                <p>Tu solicitud de turno ha sido <strong>aprobada</strong> por nuestro equipo farmacéutico.</p>
                                
                                <div class='turno-box'>
                                    <p style='text-align: center; margin: 0; color: #6c757d;'>Tu número de turno es:</p>
                                    <div class='turno-numero'>{numeroTurno:000}</div>
                                </div>

                                <p><strong>📅 Fecha del turno:</strong> {fechaTurno:dddd, dd MMMM yyyy}</p>
                                <p><strong>🕐 Hora:</strong> {fechaTurno:HH:mm}</p>
                                
                                <h3>Instrucciones importantes:</h3>
                                <ul>
                                    <li>Llega 10 minutos antes de tu hora asignada</li>
                                    <li>Trae tu documento de identidad original</li>
                                    <li>Si tienes receta médica, tráela impresa</li>
                                    <li>El PDF adjunto contiene los detalles de tu turno</li>
                                </ul>

                                <p style='background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107;'>
                                    <strong>⚠️ Nota:</strong> Si no puedes asistir, por favor cancela tu turno con anticipación 
                                    para que otra persona pueda aprovechar el cupo.
                                </p>

                                <p style='text-align: center;'>
                                    <strong>¡Nos vemos pronto en la farmacia!</strong>
                                </p>
                            </div>
                            <div class='footer'>
                                <p>Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de turno aprobado a {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTurnoRechazadoEmailAsync(string toEmail, string userName, string motivo)
        {
            try
            {
                var subject = "Solicitud de Turno - Farmacia Solidaria Cristiana";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 30px; background-color: #f8f9fa; }}
                            .motivo-box {{ background-color: #fff; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; }}
                            .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Solicitud de Turno</h1>
                            </div>
                            <div class='content'>
                                <h2>Hola {userName},</h2>
                                <p>Lamentamos informarte que tu solicitud de turno no ha podido ser aprobada en este momento.</p>
                                
                                <div class='motivo-box'>
                                    <p><strong>Motivo:</strong></p>
                                    <p>{motivo}</p>
                                </div>

                                <h3>¿Qué puedes hacer?</h3>
                                <ul>
                                    <li>Revisar el motivo y corregir la información si es necesario</li>
                                    <li>Contactar con la farmacia para más detalles</li>
                                    <li>Solicitar un nuevo turno cuando se resuelva el inconveniente</li>
                                </ul>

                                <p>Estamos aquí para ayudarte. No dudes en contactarnos si tienes preguntas.</p>
                            </div>
                            <div class='footer'>
                                <p>Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de turno rechazado a {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTurnoSolicitadoEmailAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "Solicitud de Turno Recibida - Farmacia Solidaria Cristiana";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 30px; background-color: #f8f9fa; }}
                            .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>📋 Solicitud Recibida</h1>
                            </div>
                            <div class='content'>
                                <h2>Hola {userName},</h2>
                                <p>Hemos recibido tu solicitud de turno para la <strong>Farmacia Solidaria Cristiana</strong>.</p>
                                
                                <p style='background-color: #d1ecf1; padding: 15px; border-left: 4px solid #0dcaf0;'>
                                    <strong>ℹ️ Estado:</strong> Tu solicitud está siendo revisada por nuestro equipo farmacéutico.
                                </p>

                                <h3>¿Qué sigue?</h3>
                                <ol>
                                    <li>Nuestro equipo revisará tu solicitud y los documentos adjuntos</li>
                                    <li>Verificaremos la disponibilidad de los medicamentos solicitados</li>
                                    <li>Recibirás un email de confirmación con tu número de turno o más instrucciones</li>
                                </ol>

                                <p><strong>⏰ Tiempo estimado de respuesta:</strong> 24-48 horas hábiles</p>

                                <p>Gracias por tu paciencia. Te notificaremos por este medio una vez revisemos tu solicitud.</p>
                            </div>
                            <div class='footer'>
                                <p>Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                                <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de turno solicitado a {Email}", toEmail);
                return false;
            }
        }
    }
}
