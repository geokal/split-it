using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.UserContext
{
    public interface IUserContextService
    {
        Task<UserContextState> GetStateAsync(CancellationToken cancellationToken = default);
    }
}

