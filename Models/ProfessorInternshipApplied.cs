using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class ProfessorInternshipApplied
    {
        [Key]
        public int Id { get; set; }
        public string ProfessorEmailWhereStudentAppliedForInternship { get; set; }
        public string ProfessorUniqueIDWhereStudentAppliedForInternship { get; set; }
        public string StudentEmailAppliedForProfessorInternship { get; set; }
        public string StudentUniqueIDAppliedForProfessorInternship { get; set; }
        public long RNGForProfessorInternshipApplied { get; set; }
        public string RNGForProfessorInternshipApplied_HashedAsUniqueID { get; set; }
        public DateTime DateTimeStudentAppliedForProfessorInternship { get; set; }

        // --- NEW STATISTICS PROPERTIES ---
        public bool ProfessorInternshipApplied_CandidateInfoSeenFromModal { get; set; } = false;
        public bool ProfessorInternshipApplied_CandidateCVDownloaded { get; set; } = false;

        public string InternshipStatusAppliedAtTheProfessorSide { get; set; }
        public string InternshipStatusAppliedAtTheStudentSide { get; set; }

        public ProfessorInternshipsApplied_StudentDetails StudentDetails { get; set; }
        public ProfessorInternshipsApplied_ProfessorDetails ProfessorDetails { get; set; }
    }
}