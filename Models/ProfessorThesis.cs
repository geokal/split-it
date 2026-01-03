using Org.BouncyCastle.Crypto.Paddings;
using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class ProfessorThesis
    {
        // Thesis Details
        public int Id { get; set; }
        public long RNGForThesisUploaded { get; set; }
        public string? RNGForThesisUploaded_HashedAsUniqueID { get; set; }

        // Foreign key to Professor
        public string? ProfessorEmailUsedToUploadThesis { get; set; }

        // Thesis Information
        public string? ThesisTitle { get; set; }
        public string? ThesisDescription { get; set; }
        public string? ThesisAreas { get; set; }
        public string? ThesisSkills { get; set; }
        public byte[]? ThesisAttachment { get; set; }
        public DateTime ThesisUploadDateTime { get; set; }
        public DateTime ThesisActivePeriod { get; set; }
        public DateTime ThesisUpdateDateTime { get; set; }
        public string? ThesisStatus { get; set; }
        public int ThesisTimesUpdated { get; set; } = 0;
        public ThesisType ThesisType { get; set; }

        // ADDED: Open Slots field for professor thesis
        [Required(ErrorMessage = "The Open Slots field is required.")]
        [Range(3, int.MaxValue, ErrorMessage = "Open Slots must be at least 3.")]
        public int OpenSlots_ProfessorThesis { get; set; } = 3; // Default to 3 slots

        // Company Interest Information
        public bool IsCompanyInteresetedInProfessorThesis { get; set; }
        public string? IsCompanyInterestedInProfessorThesisStatus { get; set; }
        public string? CompanyEmailInterestedInProfessorThesis { get; set; } // Foreign key to Company

        // Navigation Properties
        public Professor? Professor { get; set; }
        public Company? CompanyInterested { get; set; }
    }
}
