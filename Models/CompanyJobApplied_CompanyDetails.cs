using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class CompanyJobApplied_CompanyDetails
    {
        [Key]
        public int Id { get; set; }
        
        // Explicit foreign key to CompanyJobApplied
        public int CompanyJobAppliedId { get; set; }
        
        public string CompanysUniqueIDWhereStudentAppliedForCompanyJob { get; set; }
        public string CompanysEmailWhereStudentAppliedForCompanyJob { get; set; }
        public CompanyJobApplied Application { get; set; }
    }
}
