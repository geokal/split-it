using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;
using QuizManager.Services.FrontPage;

namespace QuizManager.Services.FrontPage
{
    public interface IFrontPageService
    {
        Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default);
        
        Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default);
        
        Task<FrontPageData> LoadFrontPageDataAsync(CancellationToken cancellationToken = default);
    }
}

