namespace QuizManager.Models
{
    public class AnnouncementAsResearchGroup
    {
        public int Id { get; set; }
        public long? ResearchGroupAnnouncementRNG { get; set; }
        public string? ResearchGroupAnnouncementRNG_HashedAsUniqueID { get; set; }
        public string? ResearchGroupAnnouncementTitle { get; set; }
        public string? ResearchGroupAnnouncementDescription { get; set; }
        public string? ResearchGroupAnnouncementStatus { get; set; }
        public DateTime ResearchGroupAnnouncementUploadDate { get; set; }
        public string? ResearchGroupAnnouncementResearchGroupEmail { get; set; } // Foreign key to ResearchGroup
        public DateTime ResearchGroupAnnouncementTimeToBeActive { get; set; }
        public byte[]? ResearchGroupAnnouncementAttachmentFile { get; set; }

        // Navigation property to ResearchGroup
        public ResearchGroup? ResearchGroup { get; set; }
    }
}

