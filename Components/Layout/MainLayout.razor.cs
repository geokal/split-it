using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using QuizManager.Models;
using QuizManager.Services.FrontPage;
using QuizManager.Services.UserContext;

namespace QuizManager.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private IUserContextService UserContextService { get; set; } = default!;
        
        [Inject] private IFrontPageService FrontPageService { get; set; } = default!;

        // Front page data properties
        public List<CompanyEvent> CompanyEventsToShowAtFrontPage { get; set; } = new();
        public List<ProfessorEvent> ProfessorEventsToShowAtFrontPage { get; set; } = new();

        // User authentication state properties (public for Razor file access)
        public string UserRole { get; private set; } = "";
        private string CurrentUserEmail = "";
        public bool isStudentRegistered { get; private set; }
        public bool isCompanyRegistered { get; private set; }
        public bool isProfessorRegistered { get; private set; }
        public bool isResearchGroupRegistered { get; private set; }
        public bool isInitializedAsStudentUser { get; private set; } = false;
        public bool isInitializedAsCompanyUser { get; private set; } = false;
        public bool isInitializedAsProfessorUser { get; private set; } = false;
        public bool isInitializedAsResearchGroupUser { get; private set; } = false;
        
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
            // Subscribe to front page state changes
            FrontPageService.StateChanged += HandleFrontPageStateChanged;
            
            // Load front page data (will trigger state change event)
            await FrontPageService.EnsureDataLoadedAsync();
            UpdateFrontPageDataFromState();
            
            // Load user authentication state
            await LoadUserAuthenticationState();
        }

        private void HandleFrontPageStateChanged(FrontPageDataState state)
        {
            UpdateFrontPageDataFromState();
            _ = InvokeAsync(StateHasChanged);
        }

        private void UpdateFrontPageDataFromState()
        {
            var state = FrontPageService.CurrentState;
            CompanyEventsToShowAtFrontPage = state.CompanyEvents.ToList();
            ProfessorEventsToShowAtFrontPage = state.ProfessorEvents.ToList();
        }

        public void Dispose()
        {
            FrontPageService.StateChanged -= HandleFrontPageStateChanged;
            GC.SuppressFinalize(this);
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

        public bool HideAllInitialCards()
        {
            var uri = NavigationManager.Uri;
            return uri.Contains("profile")
                || uri.Contains("settings")
                || uri.Contains("uploadjobs")
                || uri.Contains("searchjobs")
                || uri.Contains("companyRegistration")
                || uri.Contains("studentRegistration")
                || uri.Contains("uploadJobs")
                || uri.Contains("professorRegistration")
                || uri.Contains("uploadthesis")
                || uri.Contains("searchthesis")
                || uri.Contains("uploadinternship")
                || uri.Contains("researchGroupRegistration");
        }

        // Wrapper method for CascadingValue (functions can't be cascaded directly)
        private bool GetHideAllInitialCards() => HideAllInitialCards();

        private bool ShouldShowAdminTable()
        {
            return UserRole == "Admin" && NavigationManager.Uri.Contains("/profile", StringComparison.OrdinalIgnoreCase) == false;
        }
    }
}
