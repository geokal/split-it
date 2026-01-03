using System;

namespace QuizManager.ViewModels
{
    public class InternshipApplicant
    {
        public int Id { get; set; }
        public string StudentEmail { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public string StudentCv { get; set; }
        public string StudentMotivationLetter { get; set; }
        public string CompanyInternshipId { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}
