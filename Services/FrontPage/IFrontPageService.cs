using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.FrontPage
{
    public interface IFrontPageService
    {
        FrontPageDataState CurrentState { get; }
        event Action<FrontPageDataState>? StateChanged;
        
        Task EnsureDataLoadedAsync(CancellationToken cancellationToken = default);
        Task RefreshAsync(CancellationToken cancellationToken = default);
        
        // Backward compatibility methods
        Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default);
        Task<FrontPageData> LoadFrontPageDataAsync(CancellationToken cancellationToken = default);
    }
}

