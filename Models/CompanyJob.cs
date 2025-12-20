using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class CompanyJob
    {
        public int Id { get; set; }
        public DateTime UploadDateTime { get; set; }
        public string? EmailUsedToUploadJobs { get; set; }
        public string? PositionType { get; set; }
        public string? PositionTitle { get; set; }
        public string? PositionForeas { get; set; }
        public string? PositionContactPerson { get; set; }
        public string? PositionContactTelephonePerson { get; set; }
        public string? PositionAddressLocation { get; set; }
        public string? PositionPerifereiaLocation { get; set; }
        public string? PositionDimosLocation { get; set; }
        public string? PositionPostalCodeLocation { get; set; }
        public bool PositionTransportOffer { get; set; }
        public string? PositionAreas { get; set; }
        public DateTime PositionActivePeriod { get; set; }
        public string? PositionStatus { get; set; }
        public string? PositionDepartment { get; set; }

        [Required(ErrorMessage = "The Position Description field is required.")]
        [MaxLength(1000)]
        public string PositionDescription { get; set; } = string.Empty;

        // Add the Open Slots field - minimum 3, no maximum
        [Required(ErrorMessage = "The Open Slots field is required.")]
        [Range(3, int.MaxValue, ErrorMessage = "Open Slots must be at least 3.")]
        public int OpenSlots { get; set; } = 3; // Default to 3 slots

        public long RNGForPositionUploaded { get; set; }
        public byte[]? PositionAttachment { get; set; }
        public int TimesUpdated { get; set; } = 0;
        public DateTime UpdateDateTime { get; set; }
        public string? RNGForPositionUploaded_HashedAsUniqueID { get; set; }

        // Navigation property to Company Model 
        public Company? Company { get; set; }
    }
}