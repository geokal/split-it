namespace QuizManager.Services
{
    // This is a wrapper/alias for the InternshipEmailService defined in Data/EmailVerificationServiceForJobAcceptanceByCompany.cs
    // The actual implementation is in the global namespace in that file
    public class InternshipEmailService : global::InternshipEmailService
    {
        public InternshipEmailService(string smtpUsername, string smtpPassword, string supportEmail, string noReplyEmail = null)
            : base(smtpUsername, smtpPassword, supportEmail, noReplyEmail)
        {
        }
    }
}
