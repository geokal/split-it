using System;

namespace QuizManager.ViewModels
{
    public class JobApplicant
    {
        public int Id { get; set; }
        public string StudentEmail { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public string StudentCv { get; set; }
        public string StudentMotivationLetter { get; set; }
        public string CompanyJobId { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}
