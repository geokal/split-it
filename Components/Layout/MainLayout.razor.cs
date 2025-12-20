using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using QuizManager.Models;
using QuizManager.Services.FrontPage;
using QuizManager.Services.UserContext;

namespace QuizManager.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] private IUserContextService UserContextService { get; set; } = default!;
        
        [Inject] private IFrontPageService FrontPageService { get; set; } = default!;

        // Front page data properties
        public List<CompanyEvent> CompanyEventsToShowAtFrontPage { get; set; } = new();
        public List<ProfessorEvent> ProfessorEventsToShowAtFrontPage { get; set; } = new();

        // User authentication state properties
        private string UserRole = "";
        private string CurrentUserEmail = "";
        private bool isStudentRegistered;
        private bool isCompanyRegistered;
        private bool isProfessorRegistered;
        private bool isResearchGroupRegistered;
        private bool isInitializedAsStudentUser = false;
        private bool isInitializedAsCompanyUser = false;
        private bool isInitializedAsProfessorUser = false;
        private bool isInitializedAsResearchGroupUser = false;
        
        private Student? userData;
        private Company? companyData;
        private Professor? professorData;
        private ResearchGroup? researchGroupData;
        
        private bool ShowStudentRegistrationButton = false;
        private bool ShowCompanyRegistrationButton = false;
        private bool ShowProfessorRegistrationButton = false;
        private bool ShowAdminRegistrationButton = false;

        protected override async Task OnInitializedAsync()
        {
            // Load front page data
            await LoadFrontPageDataAsync();
            
            // Load user authentication state
            await LoadUserAuthenticationState();
        }

        private async Task LoadFrontPageDataAsync()
        {
            try
            {
                var frontPageData = await FrontPageService.LoadFrontPageDataAsync();
                CompanyEventsToShowAtFrontPage = frontPageData.CompanyEvents.ToList();
                ProfessorEventsToShowAtFrontPage = frontPageData.ProfessorEvents.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading front page data: {ex.Message}");
                CompanyEventsToShowAtFrontPage = new List<CompanyEvent>();
                ProfessorEventsToShowAtFrontPage = new List<ProfessorEvent>();
            }
        }

        private async Task LoadUserAuthenticationState()
        {
            try
            {
                var userState = await UserContextService.GetStateAsync();
                if (!userState.IsAuthenticated)
                {
                    return;
                }

                UserRole = userState.Role;
                CurrentUserEmail = userState.Email;

                isStudentRegistered = userState.IsStudentRegistered;
                isCompanyRegistered = userState.IsCompanyRegistered;
                isProfessorRegistered = userState.IsProfessorRegistered;
                isResearchGroupRegistered = userState.IsResearchGroupRegistered;

                userData = userState.Student;
                companyData = userState.Company;
                professorData = userState.Professor;
                researchGroupData = userState.ResearchGroup;

                // Set initialization flags
                isInitializedAsStudentUser = true;
                isInitializedAsCompanyUser = true;
                isInitializedAsProfessorUser = true;
                isInitializedAsResearchGroupUser = true;

                // Set registration button visibility based on role
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.IsInRole("Student"))
                {
                    ShowStudentRegistrationButton = true;
                }
                if (user.IsInRole("Company"))
                {
                    ShowCompanyRegistrationButton = true;
                }
                if (user.IsInRole("Professor"))
                {
                    ShowProfessorRegistrationButton = true;
                }
                if (user.IsInRole("Admin") || user.IsInRole("ResearchGroup"))
                {
                    ShowAdminRegistrationButton = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user authentication state: {ex.Message}");
            }
        }

        private bool ShouldShowAdminTable()
        {
            return UserRole == "Admin" && NavigationManager.Uri.Contains("/profile", StringComparison.OrdinalIgnoreCase) == false;
        }
    }
}
