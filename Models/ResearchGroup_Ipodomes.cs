using Org.BouncyCastle.Crypto.Paddings;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;

namespace QuizManager.Models
{
    public class ResearchGroup_Ipodomes
    {
        [Key]
        public int Id { get; set; }
        public string? ResearchGroupEmail { get; set; }
        public string? ResearchGroup_UniqueID { get; set; }

        public string? ResearchGroup_Ipodomes_Title { get; set; }
        public string? ResearchGroup_Ipodomes_Description { get; set; }
        public string? ResearchGroup_Ipodomes_Keywords { get; set; }
        public byte[]? ResearchGroup_Ipodomes_Attachment { get; set; }
        public byte[]? ResearchGroup_Ipodomes_Images { get; set; }

    }
}
