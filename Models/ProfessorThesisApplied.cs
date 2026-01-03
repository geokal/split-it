using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class ProfessorThesisApplied
    {
        [Key]
        public int Id { get; set; }
        public string ProfessorEmailWhereStudentAppliedForProfessorThesis { get; set; }
        public string ProfessorUniqueIDWhereStudentAppliedForProfessorThesis { get; set; }
        public string StudentEmailAppliedForProfessorThesis { get; set; }
        public string StudentUniqueIDAppliedForProfessorThesis { get; set; }
        public long RNGForProfessorThesisApplied { get; set; }
        public string RNGForProfessorThesisApplied_HashedAsUniqueID { get; set; }
        public DateTime DateTimeStudentAppliedForProfessorThesis { get; set; }

        // --- NEW STATISTICS PROPERTIES ---
        public bool ProfessorThesisApplied_CandidateInfoSeenFromModal { get; set; } = false;
        public bool ProfessorThesisApplied_CandidateCVDownloaded { get; set; } = false;

        public string ProfessorThesisStatusAppliedAtProfessorSide { get; set; }
        public string ProfessorThesisStatusAppliedAtStudentSide { get; set; }

        public ProfessorThesisApplied_StudentDetails StudentDetails { get; set; }
        public ProfessorThesisApplied_ProfessorDetails ProfessorDetails { get; set; }
    }

}
