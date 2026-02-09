using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmailService(
            IConfiguration configuration, 
            ILogger<EmailService> logger,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            await SendEmailAsync(toEmail, subject, body, null);
        }

        /// <summary>
        /// Envía email con soporte para adjuntos
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath)
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

                // Adjuntar archivo si existe
                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    mailMessage.Attachments.Add(attachment);
                    _logger.LogInformation("Adjuntando archivo: {Path}", attachmentPath);
                }

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
                                    <li>Trae tu receta médica y/o documentos médicos que evidencien la necesidad del medicamento</li>
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

                // Llamar con el PDF adjunto
                await SendEmailAsync(toEmail, subject, body, pdfPath);
                _logger.LogInformation("Email de turno aprobado enviado a {Email} con PDF adjunto: {PdfPath}", toEmail, pdfPath);
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

        public async Task<bool> SendTurnoNotificationToFarmaceuticosAsync(string userName, int turnoId, string tipoSolicitud)
        {
            try
            {
                // Obtener configuración de URL base
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://farmaciasolidaria.somee.com";
                var turnoDetailsUrl = $"{baseUrl}/Turnos/Details/{turnoId}";
                
                var tipoIcono = tipoSolicitud == "Medicamentos" ? "💊" : "🩹";
                var tipoTexto = tipoSolicitud == "Medicamentos" ? "medicamentos" : "insumos médicos";

                var subject = $"⚕️ Nueva Solicitud de Turno ({tipoSolicitud}) - Revisión Pendiente";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                            .button {{ display: inline-block; padding: 12px 30px; background-color: #667eea; color: white !important; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                            .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #777; }}
                            .highlight {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>🔔 Nueva Solicitud de Turno</h1>
                            </div>
                            <div class='content'>
                                <p>Estimado/a Farmacéutico/a,</p>
                                <p>Se ha recibido una <strong>nueva solicitud de turno para {tipoTexto}</strong> {tipoIcono} que requiere tu revisión y aprobación.</p>
                                
                                <div class='highlight'>
                                    <strong>Detalles de la Solicitud:</strong><br/>
                                    • <strong>Usuario:</strong> {userName}<br/>
                                    • <strong>Tipo:</strong> {tipoIcono} {tipoSolicitud}<br/>
                                    • <strong>ID de Turno:</strong> #{turnoId}<br/>
                                    • <strong>Fecha de Solicitud:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}<br/>
                                </div>

                                <p>Por favor, revisa la solicitud cuando tengas un momento disponible. Recuerda que esto es voluntario y puedes revisar según tu disponibilidad.</p>

                                <p style='text-align: center;'>
                                    <a href='{turnoDetailsUrl}' class='button'>📋 Revisar Solicitud Ahora</a>
                                </p>

                                <p><strong>Acciones disponibles:</strong></p>
                                <ul>
                                    <li>✅ <strong>Aprobar:</strong> Se asignará automáticamente una fecha y hora (Martes/Jueves 1-4 PM)</li>
                                    <li>❌ <strong>Rechazar:</strong> Se notificará al usuario el motivo</li>
                                </ul>

                                <p><small>💡 Al aprobar, el sistema asignará automáticamente el próximo slot disponible (cada 6 minutos, máximo 30 turnos/día).</small></p>
                            </div>
                            <div class='footer'>
                                <p>Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                                <p>Este es un mensaje automático de notificación para farmacéuticos.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                // Obtener todos los usuarios con rol "Farmaceutico" o "Admin" desde la base de datos
                var farmaceuticoRole = await _roleManager.FindByNameAsync("Farmaceutico");
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                
                if (farmaceuticoRole == null && adminRole == null)
                {
                    _logger.LogWarning("No se encontraron los roles 'Farmaceutico' ni 'Admin' en la base de datos");
                    return false;
                }

                var farmaceuticos = await _userManager.GetUsersInRoleAsync("Farmaceutico");
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                
                // Combinar y obtener emails únicos
                var allRecipients = farmaceuticos.Union(admins)
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email!)
                    .Distinct()
                    .ToList();

                if (!allRecipients.Any())
                {
                    _logger.LogWarning("No hay usuarios con rol 'Farmaceutico' o 'Admin' que tengan email configurado");
                    return false;
                }

                _logger.LogInformation("Enviando notificación de turno #{TurnoId} a {Count} farmacéuticos/admins", 
                    turnoId, allRecipients.Count);

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

                int successCount = 0;
                foreach (var email in allRecipients)
                {
                    try
                    {
                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(fromEmail!, fromName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        };

                        mailMessage.To.Add(email);
                        await client.SendMailAsync(mailMessage);
                        successCount++;
                        _logger.LogInformation("Notificación de turno enviada a farmacéutico: {Email}", email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error enviando notificación a {Email}", email);
                        // Continuar con el siguiente email
                    }
                }

                _logger.LogInformation("Notificaciones enviadas exitosamente: {Success}/{Total}", 
                    successCount, allRecipients.Count);

                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificaciones de turno a farmacéuticos");
                return false;
            }
        }

        public async Task SendTurnoCanceladoByUserEmailAsync(
            string destinatario, 
            string nombreUsuario, 
            int numeroTurno, 
            DateTime fechaTurno,
            string motivo)
        {
            var subject = $"Turno #{numeroTurno:000} Cancelado";
            
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background-color: #f8d7da; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❌ Turno Cancelado</h1>
                        </div>
                        <div class='content'>
                            <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                            
                            <p>Tu turno ha sido cancelado según tu solicitud.</p>
                            
                            <div class='info-box'>
                                <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                                <p><strong>Fecha del Turno:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                                <p><strong>Motivo:</strong> {motivo}</p>
                                <p><strong>Cancelado el:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                            
                            <p>Si necesitas solicitar un nuevo turno, puedes hacerlo desde nuestra plataforma.</p>
                            
                            <p><strong>Recuerda:</strong> Puedes solicitar hasta 2 turnos por mes.</p>
                            
                            <div class='footer'>
                                <p>Saludos,<br/>
                                <strong>Farmacia Solidaria Cristiana</strong><br/>
                                Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(destinatario, subject, body);
        }

        public async Task SendNotificacionTurnoCanceladoAsync(
            string destinatario,
            string nombreFarmaceutico,
            int numeroTurno,
            DateTime fechaTurno,
            string motivo)
        {
            var subject = $"Usuario canceló Turno #{numeroTurno:000}";
            
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #ffc107; color: #000; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background-color: #fff3cd; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>⚠️ Turno Cancelado por Usuario</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreFarmaceutico}</strong>,</p>
                            
                            <p>Un usuario ha cancelado su turno aprobado:</p>
                            
                            <div class='info-box'>
                                <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                                <p><strong>Fecha del Turno:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                                <p><strong>Motivo de cancelación:</strong> {motivo}</p>
                                <p><strong>Cancelado el:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                            
                            <p>✅ El slot de tiempo ha quedado disponible para otros usuarios.</p>
                            
                            <p><em>Este es un mensaje informativo. No requiere acción por tu parte.</em></p>
                            
                            <div class='footer'>
                                <p>Sistema Automático<br/>
                                <strong>Farmacia Solidaria Cristiana</strong><br/>
                                Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(destinatario, subject, body);
        }

        public async Task SendTurnoReprogramadoEmailAsync(
            string destinatario,
            string nombreUsuario,
            int numeroTurno,
            DateTime fechaOriginal,
            DateTime fechaNueva,
            string motivo)
        {
            var subject = $"Turno #{numeroTurno:000} Reprogramado";
            
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #0dcaf0; color: #000; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background-color: #cff4fc; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #0dcaf0; }}
                        .highlight {{ background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📅 Turno Reprogramado</h1>
                        </div>
                        <div class='content'>
                            <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                            
                            <p>Tu turno ha sido <strong>reprogramado</strong> debido a circunstancias excepcionales.</p>
                            
                            <div class='info-box'>
                                <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                                <p><strong>Fecha Original:</strong> <s>{fechaOriginal:dd/MM/yyyy HH:mm}</s></p>
                                <p><strong>Nueva Fecha:</strong> <span style='color: #0d6efd; font-size: 1.2em;'>{fechaNueva:dd/MM/yyyy HH:mm}</span></p>
                                <p><strong>Día:</strong> {fechaNueva:dddd, dd 'de' MMMM 'de' yyyy}</p>
                            </div>
                            
                            <div class='highlight'>
                                <p><strong>Motivo de la reprogramación:</strong></p>
                                <p>{motivo}</p>
                            </div>
                            
                            <p><strong>⚠️ Importante:</strong></p>
                            <ul>
                                <li>Tu turno sigue siendo <strong>válido</strong></li>
                                <li>No necesitas hacer nada adicional</li>
                                <li>Llega 10 minutos antes de la nueva hora</li>
                                <li>Trae tu documento de identidad original</li>
                                <li>Presenta las recetas médicas correspondientes</li>
                            </ul>
                            
                            <p>Lamentamos las molestias que esto pueda ocasionar. Nuestro equipo está trabajando para brindarte el mejor servicio.</p>
                            
                            <p><strong>Si no puedes asistir en la nueva fecha:</strong></p>
                            <p>Puedes cancelar tu turno desde la plataforma (con al menos 7 días de anticipación) y solicitar uno nuevo.</p>
                            
                            <div class='footer'>
                                <p>Saludos,<br/>
                                <strong>Farmacia Solidaria Cristiana</strong><br/>
                                Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(destinatario, subject, body);
        }

        public async Task SendTurnoNoAsistenciaUsuarioEmailAsync(
            string destinatario,
            string nombreUsuario,
            int numeroTurno,
            DateTime fechaTurno)
        {
            var subject = $"Turno #{numeroTurno:000} Cancelado por No Asistencia";
            
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background-color: #f8d7da; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; }}
                        .warning {{ background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #ffc107; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❌ Turno Cancelado</h1>
                        </div>
                        <div class='content'>
                            <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                            
                            <p>Lamentamos informarte que tu turno ha sido <strong>cancelado automáticamente</strong> porque no asististe a la farmacia en la fecha y hora programadas.</p>
                            
                            <div class='info-box'>
                                <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                                <p><strong>Fecha Programada:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                                <p><strong>Estado:</strong> <span style='color: #dc3545;'>CANCELADO</span></p>
                            </div>
                            
                            <div class='warning'>
                                <p><strong>⚠️ Importante:</strong></p>
                                <ul>
                                    <li>Este turno cuenta como uno de tus <strong>2 turnos mensuales permitidos</strong></li>
                                    <li>Las cantidades reservadas han sido devueltas al stock</li>
                                    <li>Si aún necesitas medicamentos/insumos, debes solicitar un nuevo turno</li>
                                    <li>Recuerda: máximo 2 turnos por mes</li>
                                </ul>
                            </div>
                            
                            <p><strong>¿Qué puedes hacer ahora?</strong></p>
                            <ul>
                                <li>Verifica cuántos turnos te quedan disponibles este mes</li>
                                <li>Si tienes disponibilidad, solicita un nuevo turno desde la plataforma</li>
                                <li>Asegúrate de asistir puntualmente a futuros turnos</li>
                                <li>Si tienes alguna emergencia, cancela el turno con anticipación (mínimo 7 días antes)</li>
                            </ul>
                            
                            <p>Si tuviste algún inconveniente o emergencia que te impidió asistir, por favor comunícate con nosotros.</p>
                            
                            <div class='footer'>
                                <p>Saludos,<br/>
                                <strong>Farmacia Solidaria Cristiana</strong><br/>
                                Iglesia Metodista de Cárdenas</p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(destinatario, subject, body);
        }

        public async Task SendTurnoNoAsistenciaFarmaceuticoEmailAsync(
            string destinatario,
            string nombreFarmaceutico,
            int numeroTurno,
            string nombreUsuario,
            DateTime fechaTurno,
            string itemsReservados)
        {
            var subject = $"[Sistema] Turno #{numeroTurno:000} Cancelado por No Asistencia";
            
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background-color: #e9ecef; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #6c757d; }}
                        .items {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔔 Notificación del Sistema</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreFarmaceutico}</strong>,</p>
                            
                            <p>El sistema ha cancelado automáticamente un turno por <strong>no asistencia del usuario</strong>.</p>
                            
                            <div class='info-box'>
                                <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                                <p><strong>Usuario:</strong> {nombreUsuario}</p>
                                <p><strong>Fecha Programada:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                                <p><strong>Procesado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                            
                            <div class='items'>
                                <p><strong>Stock devuelto:</strong></p>
                                <pre style='white-space: pre-wrap;'>{itemsReservados}</pre>
                            </div>
                            
                            <p><strong>✅ Acciones realizadas automáticamente:</strong></p>
                            <ul>
                                <li>Turno marcado como CANCELADO</li>
                                <li>Stock reservado devuelto al inventario</li>
                                <li>Email enviado al usuario</li>
                                <li>Contador de turnos mensuales del usuario <strong>NO recuperado</strong></li>
                            </ul>
                            
                            <p><strong>ℹ️ Nota:</strong> Este turno cuenta como uno de los 2 turnos mensuales permitidos para este usuario.</p>
                            
                            <div class='footer'>
                                <p>Sistema Automático de Gestión de Turnos<br/>
                                <strong>Farmacia Solidaria Cristiana</strong></p>
                                <p>© 2025 Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(destinatario, subject, body);
        }
    }
}
