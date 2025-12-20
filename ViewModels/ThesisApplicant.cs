using System;

namespace QuizManager.ViewModels
{
    public class ThesisApplicant
    {
        public int Id { get; set; }
        public string StudentEmail { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public string StudentCv { get; set; }
        public string StudentMotivationLetter { get; set; }
        public string CompanyThesisId { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}
