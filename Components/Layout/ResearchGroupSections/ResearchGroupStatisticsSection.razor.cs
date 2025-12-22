using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Services.ResearchGroupDashboard;
using QuizManager.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ResearchGroupSections
{
    public partial class ResearchGroupStatisticsSection : ComponentBase
    {
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private GoogleScholarService _googleScholarService { get; set; } = default!;
        [Inject] private IResearchGroupDashboardService ResearchGroupDashboardService { get; set; } = default!;

        // Current User
        private string CurrentUserEmail = "";
        private QuizManager.Models.ResearchGroup currentResearchGroup;
        private ResearchGroupDashboardData dashboardData = ResearchGroupDashboardData.Empty;

        // Visibility
        private bool isStatisticsVisible = false;
        private bool isLoadingStatistics = false;

        // Statistics Counts
        private int? numberOfFacultyMembers;
        private int? numberOfCollaborators;
        private int? numberOfTotalPublications;
        private int? numberOfRecentPublications;
        private int? numberOfActiveResearchActions;
        private int? numberOfInactiveResearchActions;
        private int? numberOfActivePatents;
        private int? numberOfInactivePatents;
        private int? numberOfIpodomes;

        // Modal States
        private bool showFacultyMembersModal = false;
        private bool showNonFacultyMembersModal = false;
        private bool showResearchActionsModal = false;
        private bool showPatentsModal = false;
        private bool showIpodomesModal = false;

        // Modal Data
        private List<FacultyMemberDetail> facultyMembersDetails = new();
        private List<NonFacultyMemberDetail> nonFacultyMembersDetails = new();
        private List<ResearchActionDetail> researchActionsDetails = new();
        private List<PatentDetail> patentsDetails = new();
        private List<ResearchGroup_Ipodomes> ipodomesDetails = new();

        // Detail Classes
        public class FacultyMemberDetail
        {
            public byte[]? Image { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string School { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        public class NonFacultyMemberDetail
        {
            public byte[]? Image { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string LevelOfStudies { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string School { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
        }

        public class ResearchActionDetail
        {
            public string ProjectTitle { get; set; } = string.Empty;
            public string ProjectAcronym { get; set; } = string.Empty;
            public string GrantAgreementNumber { get; set; } = string.Empty;
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string ProjectCoordinator { get; set; } = string.Empty;
            public string ELKECode { get; set; } = string.Empty;
            public string ScientificResponsibleEmail { get; set; } = string.Empty;
            public string ProjectStatus { get; set; } = string.Empty;
        }

        public class PatentDetail
        {
            public string PatentTitle { get; set; } = string.Empty;
            public string PatentType { get; set; } = string.Empty;
            public string PatentDOI { get; set; } = string.Empty;
            public string PatentURL { get; set; } = string.Empty;
            public string PatentDescription { get; set; } = string.Empty;
            public string PatentStatus { get; set; } = string.Empty;
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
            dashboardData = await ResearchGroupDashboardService.LoadDashboardDataAsync();
            currentResearchGroup = dashboardData.ResearchGroupProfile;
        }

        // Toggle Statistics Visibility
        private async Task ToggleStatisticsVisibility()
        {
            isStatisticsVisible = !isStatisticsVisible;

            if (isStatisticsVisible)
            {
                await LoadResearchGroupStatistics();
            }

            StateHasChanged();
        }

        private async Task LoadResearchGroupStatistics()
        {
            isLoadingStatistics = true;
            StateHasChanged();
            try
            {
                dashboardData = await ResearchGroupDashboardService.LoadDashboardDataAsync();
                currentResearchGroup = dashboardData.ResearchGroupProfile;

                numberOfFacultyMembers = dashboardData.Statistics.FacultyMembers;
                numberOfCollaborators = dashboardData.Statistics.Collaborators;
                numberOfActiveResearchActions = dashboardData.Statistics.ActiveResearchActions;
                numberOfInactiveResearchActions = dashboardData.Statistics.InactiveResearchActions;
                numberOfActivePatents = dashboardData.Statistics.ActivePatents;
                numberOfInactivePatents = dashboardData.Statistics.InactivePatents;
                numberOfIpodomes = 0; // ipodomes not provided by service

                await FetchPublicationsFromGoogleScholar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading statistics: {ex.Message}");
            }
            finally
            {
                isLoadingStatistics = false;
                StateHasChanged();
            }
        }

        private async Task FetchPublicationsFromGoogleScholar()
        {
            try
            {
                var currentResearchGroupEmail = CurrentUserEmail;

                var allMembersWithScholar = dashboardData.FacultyMembers
                    .Where(p => !string.IsNullOrWhiteSpace(p.Email) && !string.IsNullOrWhiteSpace(p.ScholarProfile))
                    .Select(p => new { Email = p.Email!, ScholarProfile = p.ScholarProfile!, Type = "Professor" })
                    .Concat(dashboardData.NonFacultyMembers
                        .Where(s => !string.IsNullOrWhiteSpace(s.Email) && !string.IsNullOrWhiteSpace(s.ScholarProfile))
                        .Select(s => new { Email = s.Email!, ScholarProfile = s.ScholarProfile!, Type = "Student" }))
                    .ToList();

                var allPublications = new List<ScholarPublication>();
                var fiveYearsAgo = DateTime.Now.AddYears(-5).Year;

                foreach (var member in allMembersWithScholar)
                {
                    try
                    {
                        var publications = await _googleScholarService.GetPublications(member.ScholarProfile);
                        allPublications.AddRange(publications);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching publications for {member.Email}: {ex.Message}");
                    }
                }

                numberOfTotalPublications = allPublications.Count;
                numberOfRecentPublications = allPublications
                    .Where(p => !string.IsNullOrEmpty(p.Year) &&
                            int.TryParse(p.Year, out int year) &&
                            year >= fiveYearsAgo)
                    .Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FetchPublicationsForStatistics: {ex.Message}");
                numberOfTotalPublications = 0;
                numberOfRecentPublications = 0;
            }
        }

        // Faculty Members Modal
        private async Task ShowFacultyMembersDetails()
        {
            try
            {
                var currentResearchGroupEmail = CurrentUserEmail;

                facultyMembersDetails = dashboardData.FacultyMembers.Select(p => new FacultyMemberDetail
                {
                    Image = p.Image,
                    Name = p.FirstName,
                    Surname = p.LastName,
                    School = p.School ?? "",
                    Department = p.Department ?? "",
                    Role = p.Role ?? ""
                }).ToList();

                showFacultyMembersModal = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading faculty members details: {ex.Message}");
            }
        }

        private void CloseFacultyMembersModal()
        {
            showFacultyMembersModal = false;
            StateHasChanged();
        }

        // Non-Faculty Members Modal
        private async Task ShowNonFacultyMembersDetails()
        {
            try
            {
                var currentResearchGroupEmail = CurrentUserEmail;

                nonFacultyMembersDetails = dashboardData.NonFacultyMembers.Select(n => new NonFacultyMemberDetail
                {
                    Image = null,
                    Name = n.FirstName,
                    Surname = n.LastName,
                    LevelOfStudies = n.LevelOfStudies ?? "",
                    Department = n.Department ?? "",
                    School = n.School ?? "",
                    RegistrationDate = n.RegistrationDate ?? DateTime.MinValue
                }).ToList();

                showNonFacultyMembersModal = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading non-faculty members details: {ex.Message}");
            }
        }

        private void CloseNonFacultyMembersModal()
        {
            showNonFacultyMembersModal = false;
            StateHasChanged();
        }

        // Research Actions Modal
        private async Task ShowResearchActionsDetails()
        {
            try
            {
                var currentResearchGroupEmail = CurrentUserEmail;

                researchActionsDetails = dashboardData.ResearchActions.Select(ra => new ResearchActionDetail
                {
                    ProjectTitle = ra.ProjectTitle ?? "",
                    ProjectAcronym = ra.ProjectAcronym ?? "",
                    GrantAgreementNumber = ra.GrantAgreementNumber ?? "",
                    StartDate = ra.StartDate,
                    EndDate = ra.EndDate,
                    ProjectCoordinator = ra.ProjectCoordinator ?? "",
                    ELKECode = ra.ElkeCode ?? "",
                    ScientificResponsibleEmail = ra.ScientificResponsibleEmail ?? "",
                    ProjectStatus = ra.ProjectStatus ?? ""
                }).ToList();

                showResearchActionsModal = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading research actions details: {ex.Message}");
            }
        }

        private void CloseResearchActionsModal()
        {
            showResearchActionsModal = false;
            StateHasChanged();
        }

        // Patents Modal
        private async Task ShowPatentsDetails()
        {
            try
            {
                var currentResearchGroupEmail = CurrentUserEmail;

                patentsDetails = dashboardData.Patents.Select(p => new PatentDetail
                {
                    PatentTitle = p.PatentTitle ?? "",
                    PatentType = p.PatentType ?? "",
                    PatentDOI = p.PatentDoi ?? "",
                    PatentURL = p.PatentUrl ?? "",
                    PatentDescription = p.PatentDescription ?? "",
                    PatentStatus = p.PatentStatus ?? ""
                }).ToList();

                showPatentsModal = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading patents details: {ex.Message}");
            }
        }

        private void ClosePatentsModal()
        {
            showPatentsModal = false;
            StateHasChanged();
        }

        // Ipodomes Modal
        private Task ShowIpodomesDetails()
        {
            ipodomesDetails = new();
            showIpodomesModal = true;
            return Task.CompletedTask;
        }

        private void CloseIpodomesModal()
        {
            showIpodomesModal = false;
        }
    }
}
