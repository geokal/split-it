using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
	public class ProfessorInternship
	{
		[Key]
		public int Id { get; set; }
		public long RNGForInternshipUploadedAsProfessor { get; set; }
		public string? ProfessorInternshipESPA { get; set; }
		public string? ProfessorInternshipType { get; set; }
		public string? ProfessorInternshipTitle { get; set; }
		public string? ProfessorInternshipForeas { get; set; }
		public string? ProfessorInternshipContactPerson { get; set; }
		public string? ProfessorInternshipContactTelephonePerson { get; set; }
		public string? ProfessorInternshipAddress { get; set; }
		public string? ProfessorInternshipPerifereiaLocation { get; set; }
		public string? ProfessorInternshipDimosLocation { get; set; }
		public string? ProfessorInternshipPostalCodeLocation { get; set; }
		public bool ProfessorInternshipTransportOffer { get; set; }
		public string? ProfessorInternshipAreas { get; set; }
		public DateTime ProfessorInternshipActivePeriod { get; set; }
		public DateTime ProfessorInternshipFinishEstimation { get; set; }
		public DateTime ProfessorInternshipLastUpdate { get; set; }
		public string? ProfessorInternshipDescription { get; set; }

        // ADDED: Open Slots field for professor thesis
        [Required(ErrorMessage = "The Open Slots field is required.")]
        [Range(3, int.MaxValue, ErrorMessage = "Open Slots must be at least 3.")]
        public int OpenSlots_ProfessorInternship { get; set; } = 3; // Default to 3 slots

        public byte[]? ProfessorInternshipAttachment { get; set; }
		public string? ProfessorUploadedInternshipStatus { get; set; }
		public string? ProfessorInternshipEKPASupervisor { get; set; }
		public DateTime ProfessorInternshipUploadDate { get; set; }
		public string? RNGForInternshipUploadedAsProfessor_HashedAsUniqueID { get; set; }

		// Foreign key to Professor
		public string? ProfessorEmailUsedToUploadInternship { get; set; }

		// Navigation property to Professor
		public Professor? Professor { get; set; }
	}
}
