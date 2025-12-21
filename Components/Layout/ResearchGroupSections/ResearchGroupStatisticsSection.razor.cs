using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ResearchGroupSections
{
    public partial class ResearchGroupStatisticsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private GoogleScholarService _googleScholarService { get; set; } = default!;

        // Current User
        private string CurrentUserEmail = "";
        private QuizManager.Models.ResearchGroup currentResearchGroup;

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

        private async Task LoadCurrentResearchGroup()
        {
            try
            {
                currentResearchGroup = await dbContext.ResearchGroups
                    .FirstOrDefaultAsync(rg => rg.ResearchGroupEmail == CurrentUserEmail);

                if (currentResearchGroup == null)
                {
                    Console.WriteLine("Research group not found for current user");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading research group: {ex.Message}");
            }
        }

        private async Task LoadResearchGroupStatistics()
        {
            isLoadingStatistics = true;
            StateHasChanged();
            try
            {
                if (currentResearchGroup == null)
                {
                    await LoadCurrentResearchGroup();
                }

                var currentResearchGroupEmail = CurrentUserEmail;

                numberOfFacultyMembers = await dbContext.ResearchGroup_Professors
                    .Where(rp => rp.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .CountAsync();

                numberOfCollaborators = await dbContext.ResearchGroup_NonFacultyMembers
                    .Where(rnf => rnf.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .CountAsync();

                numberOfActiveResearchActions = await dbContext.ResearchGroup_ResearchActions
                    .Where(ra => ra.ResearchGroupEmail == currentResearchGroupEmail &&
                                ra.ResearchGroup_ProjectStatus == "OnGoing")
                    .CountAsync();

                numberOfInactiveResearchActions = await dbContext.ResearchGroup_ResearchActions
                    .Where(ra => ra.ResearchGroupEmail == currentResearchGroupEmail &&
                                ra.ResearchGroup_ProjectStatus == "Past")
                    .CountAsync();

                numberOfActivePatents = await dbContext.ResearchGroup_Patents
                    .Where(p => p.ResearchGroupEmail == currentResearchGroupEmail &&
                            p.ResearchGroup_Patent_PatentStatus == "Ενεργή")
                    .CountAsync();

                numberOfInactivePatents = await dbContext.ResearchGroup_Patents
                    .Where(p => p.ResearchGroupEmail == currentResearchGroupEmail &&
                            p.ResearchGroup_Patent_PatentStatus == "Ανενεργή")
                    .CountAsync();

                numberOfIpodomes = await dbContext.ResearchGroup_Ipodomes
                    .Where(i => i.ResearchGroupEmail == currentResearchGroupEmail)
                    .CountAsync();

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

                var professorEmails = await dbContext.ResearchGroup_Professors
                    .Where(rp => rp.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .Select(rp => rp.PK_ProfessorEmail)
                    .ToListAsync();

                var professorsWithScholar = await dbContext.Professors
                    .Where(p => professorEmails.Contains(p.ProfEmail) &&
                            !string.IsNullOrEmpty(p.ProfScholarProfile))
                    .Select(p => new { p.ProfEmail, p.ProfScholarProfile })
                    .ToListAsync();

                var studentEmails = await dbContext.ResearchGroup_NonFacultyMembers
                    .Where(rnf => rnf.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .Select(rnf => rnf.PK_NonFacultyMemberEmail)
                    .ToListAsync();

                var studentsWithScholar = await dbContext.Students
                    .Where(s => studentEmails.Contains(s.Email) &&
                            !string.IsNullOrEmpty(s.StudentGoogleScholarProfile))
                    .Select(s => new { s.Email, s.StudentGoogleScholarProfile })
                    .ToListAsync();

                var allMembersWithScholar = professorsWithScholar
                    .Select(p => new { Email = p.ProfEmail, ScholarProfile = p.ProfScholarProfile, Type = "Professor" })
                    .Concat(studentsWithScholar
                        .Select(s => new { Email = s.Email, ScholarProfile = s.StudentGoogleScholarProfile, Type = "Student" }))
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

                facultyMembersDetails = await dbContext.ResearchGroup_Professors
                    .Where(rp => rp.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .Join(dbContext.Professors,
                        rp => rp.PK_ProfessorEmail,
                        p => p.ProfEmail,
                        (rp, p) => new FacultyMemberDetail
                        {
                            Image = p.ProfImage,
                            Name = p.ProfName ?? "",
                            Surname = p.ProfSurname ?? "",
                            School = p.ProfSchool ?? "",
                            Department = p.ProfDepartment ?? "",
                            Role = rp.PK_ProfessorRole ?? ""
                        })
                    .ToListAsync();

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

                nonFacultyMembersDetails = await dbContext.ResearchGroup_NonFacultyMembers
                    .Where(rnf => rnf.PK_ResearchGroupEmail == currentResearchGroupEmail)
                    .Join(dbContext.Students,
                        rnf => rnf.PK_NonFacultyMemberEmail,
                        s => s.Email,
                        (rnf, s) => new NonFacultyMemberDetail
                        {
                            Image = s.Image,
                            Name = s.Name,
                            Surname = s.Surname,
                            LevelOfStudies = rnf.PK_NonFacultyMemberLevelOfStudies ?? "",
                            Department = s.Department,
                            School = s.School,
                            RegistrationDate = rnf.DateOfRegistrationOnResearchGroup_ForNonFacultyMember
                        })
                    .ToListAsync();

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

                researchActionsDetails = await dbContext.ResearchGroup_ResearchActions
                    .Where(ra => ra.ResearchGroupEmail == currentResearchGroupEmail)
                    .Select(ra => new ResearchActionDetail
                    {
                        ProjectTitle = ra.ResearchGroup_ProjectTitle ?? "",
                        ProjectAcronym = ra.ResearchGroup_ProjectAcronym ?? "",
                        GrantAgreementNumber = ra.ResearchGroup_ProjectGrantAgreementNumber ?? "",
                        StartDate = ra.ResearchGroup_ProjectStartDate,
                        EndDate = ra.ResearchGroup_ProjectEndDate,
                        ProjectCoordinator = ra.ResearchGroup_ProjectCoordinator ?? "",
                        ELKECode = ra.ResearchGroup_ProjectELKECode ?? "",
                        ScientificResponsibleEmail = ra.ResearchGroup_ProjectScientificResponsibleEmail ?? "",
                        ProjectStatus = ra.ResearchGroup_ProjectStatus ?? ""
                    })
                    .ToListAsync();

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

                patentsDetails = await dbContext.ResearchGroup_Patents
                    .Where(p => p.ResearchGroupEmail == currentResearchGroupEmail)
                    .Select(p => new PatentDetail
                    {
                        PatentTitle = p.ResearchGroup_Patent_PatentTitle ?? "",
                        PatentType = p.ResearchGroup_Patent_PatentType ?? "",
                        PatentDOI = p.ResearchGroup_Patent_PatentDOI ?? "",
                        PatentURL = p.ResearchGroup_Patent_PatentURL ?? "",
                        PatentDescription = p.ResearchGroup_Patent_PatentDescription ?? "",
                        PatentStatus = p.ResearchGroup_Patent_PatentStatus ?? ""
                    })
                    .ToListAsync();

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
        private async Task ShowIpodomesDetails()
        {
            var currentResearchGroupEmail = CurrentUserEmail;

            ipodomesDetails = await dbContext.ResearchGroup_Ipodomes
                .Where(i => i.ResearchGroupEmail == currentResearchGroupEmail)
                .AsNoTracking()
                .ToListAsync();

            showIpodomesModal = true;
        }

        private void CloseIpodomesModal()
        {
            showIpodomesModal = false;
        }
    }
}

