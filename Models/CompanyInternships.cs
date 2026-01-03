using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class CompanyInternship
    {
        [Key]
        public int Id { get; set; }
        public long RNGForInternshipUploadedAsCompany { get; set; }
        public string? CompanyInternshipESPA { get; set; }
        public string? CompanyInternshipType { get; set; }
        public string? CompanyInternshipTitle { get; set; }
        public string? CompanyInternshipForeas { get; set; }
        public string? CompanyInternshipContactPerson { get; set; }
        public string? CompanyInternshipContactTelephonePerson { get; set; }
        public string? CompanyInternshipAddress { get; set; }
        public string? CompanyInternshipPerifereiaLocation { get; set; }
        public string? CompanyInternshipDimosLocation { get; set; }
        public string? CompanyInternshipPostalCodeLocation { get; set; }
        public bool CompanyInternshipTransportOffer { get; set; }
        public string? CompanyInternshipAreas { get; set; }
        public DateTime CompanyInternshipActivePeriod { get; set; }
        public DateTime CompanyInternshipFinishEstimation { get; set; }
        public DateTime CompanyInternshipLastUpdate { get; set; }
        public string? CompanyInternshipDescription { get; set; }
        public byte[]? CompanyInternshipAttachment { get; set; }
        public string? CompanyUploadedInternshipStatus { get; set; }
        public string? CompanyInternshipEKPASupervisor { get; set; }
        public DateTime CompanyInternshipUploadDate { get; set; }
        public string? RNGForInternshipUploadedAsCompany_HashedAsUniqueID { get; set; }

        [Required(ErrorMessage = "The Open Slots field is required.")]
        [Range(3, int.MaxValue, ErrorMessage = "Open Slots must be at least 3.")]
        public int OpenSlots_CompanyInternships { get; set; } = 3; // Default to 3 slots

        // Foreign key to Company
        public string? CompanyEmailUsedToUploadInternship { get; set; }

        // Navigation property to Company
        public Company? Company { get; set; }
    }
}
