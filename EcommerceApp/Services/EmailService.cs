using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EcommerceApp.Services
{
    
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Send generic email
        /// </summary>
        public void SendEmail(string toEmail, string subject, string message)
        {
            try
            {
                var enabledStr = _config["EmailSettings:Enabled"] ?? "true";
                if (!bool.TryParse(enabledStr, out bool enabled))
                    enabled = true;

                if (!enabled)
                {
                    _logger.LogInformation("Email disabled in configuration. Skipping send. To={To} Subject={Subject} Body={Body}", toEmail, subject, message);
                    return;
                }

                var email = new MimeMessage();

                email.From.Add(MailboxAddress.Parse(
                    _config["EmailSettings:FromEmail"] ?? ""
                ));

                email.To.Add(MailboxAddress.Parse(toEmail));

                email.Subject = subject;

                email.Body = new TextPart("html")
                {
                    Text = message
                };

                using var smtp = new SmtpClient();

                smtp.Connect(
                    _config["EmailSettings:SmtpServer"] ?? "",
                    int.Parse(_config["EmailSettings:Port"] ?? "0"),
                    SecureSocketOptions.StartTls
                );

                smtp.Authenticate(
                    _config["EmailSettings:Username"] ?? "",
                    _config["EmailSettings:Password"] ?? ""
                );

                smtp.Send(email);

                smtp.Disconnect(true);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw;
            }
        }

        /// <summary>
        /// Send email verification code
        /// </summary>
        public void SendVerificationEmail(string toEmail, int verificationCode)
        {
            string subject = "Verify Your Ecommerce Account";

            string message = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>

                <div style='max-width:600px;margin:auto;padding:20px;border:1px solid #ddd;border-radius:10px;'>

                    <h2 style='color:#0d6efd;'>Email Verification</h2>

                    <p>Thank you for registering with our Ecommerce Store.</p>

                    <p>Your verification code is:</p>

                    <h1 style='color:#198754;'>{verificationCode}</h1>

                    <p>This code will expire in 15 minutes.</p>

                    <hr>

                    <p style='font-size:13px;color:gray;'>
                        If you did not create this account, please ignore this email.
                    </p>

                </div>

            </body>
            </html>";

            SendEmail(toEmail, subject, message);
        }

        /// <summary>
        /// Send password reset code
        /// </summary>
        public void SendPasswordResetEmail(string toEmail, string resetCode)
        {
            string subject = "Reset Your Password";

            string message = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>

                <div style='max-width:600px;margin:auto;padding:20px;border:1px solid #ddd;border-radius:10px;'>

                    <h2 style='color:#dc3545;'>Password Reset</h2>

                    <p>You requested a password reset.</p>

                    <p>Your reset code is:</p>

                    <h1 style='color:#fd7e14;'>{resetCode}</h1>

                    <p>This code will expire in 30 minutes.</p>

                    <hr>

                    <p style='font-size:13px;color:gray;'>
                        If you did not request this, please ignore this email.
                    </p>

                </div>

            </body>
            </html>";

            SendEmail(toEmail, subject, message);
        }

        /// <summary>
        /// Send order confirmation email
        /// </summary>
        public void SendOrderConfirmationEmail(
            string toEmail,
            string userName,
            string orderId,
            decimal totalAmount)
        {
            string subject = $"Order Confirmed - #{orderId}";

            string message = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background:#f5f5f5; padding:20px;'>

                <div style='max-width:600px;margin:auto;background:white;padding:30px;border-radius:10px;'>

                    <h2 style='color:#198754;text-align:center;'>
                        Order Confirmed
                    </h2>

                    <p>Hello <strong>{userName}</strong>,</p>

                    <p>
                        Thank you for shopping with us.
                        Your order has been placed successfully.
                    </p>

                    <div style='background:#f8f9fa;padding:20px;border-radius:10px;margin-top:20px;'>

                        <h3>Order Details</h3>

                        <p>
                            <strong>Order ID:</strong> #{orderId}
                        </p>

                        <p>
                            <strong>Total Amount:</strong> ${totalAmount}
                        </p>

                        <p>
                            <strong>Order Date:</strong> {DateTime.Now}
                        </p>

                    </div>

                    <p style='margin-top:20px;'>
                        Your order is now being processed.
                    </p>

                    <hr>

                    <p style='text-align:center;font-size:12px;color:gray;'>
                        © {DateTime.Now.Year} EcommerceApp. All rights reserved.
                    </p>

                </div>

            </body>
            </html>";

            SendEmail(toEmail, subject, message);
        }
    }
}