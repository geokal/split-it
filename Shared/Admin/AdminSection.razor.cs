using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;

namespace SplitIt.Shared.Admin
{
    public partial class AdminSection : ComponentBase
    {
        [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // User role
        private string UserRole = "";

        // Student stats visibility
        private bool isStudentStatsFormVisibleToShowStudentStatsAsAdmin = false;
        private bool isAnalyticsVisible = false;

        // Student data with Auth0 details
        private List<StudentWithAuth0Details> StudentsWithAuth0Details { get; set; } = new();

        // Analytics data
        private Dictionary<string, int> areaDistribution = new();
        private Dictionary<string, int> skillDistributionforadmin = new();

        // Filter options
        private List<string> AvailableSchools = new();
        private List<string> AvailableDepartments = new();
        private string SelectedAreaSchool = "";
        private string SelectedAreaDepartment = "";
        private string SelectedSkillSchool = "";
        private string SelectedSkillDepartment = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadUserRole();
            if (UserRole == "Admin")
            {
                await LoadStudentsWithAuth0DetailsAsync();
                AvailableSchools = StudentsWithAuth0Details.Select(s => s.School).Distinct().ToList();
                AvailableDepartments = StudentsWithAuth0Details.Select(s => s.Department).Distinct().ToList();
            }
        }

        private async Task LoadUserRole()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                UserRole = user.Claims.FirstOrDefault(c => c.Type == "extension_Role")?.Value ?? "";
            }
        }

        private bool ShouldShowAdminTable()
        {
            return UserRole == "Admin" && !NavigationManager.Uri.Contains("/profile", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ToggleFormVisibilityToShowStudentStatsAsAdmin()
        {
            isStudentStatsFormVisibleToShowStudentStatsAsAdmin = !isStudentStatsFormVisibleToShowStudentStatsAsAdmin;
            StateHasChanged();
        }

        private void ToggleAnalyticsVisibility()
        {
            isAnalyticsVisible = !isAnalyticsVisible;
            if (isAnalyticsVisible)
            {
                _ = LoadAnalyticsAsync();
            }
        }

        private async Task LoadAnalyticsAsync()
        {
            try
            {
                using var context = DbFactory.CreateDbContext();

                var rawData = await context.Students
                    .AsNoTracking()
                    .Where(s => s.AreasOfExpertise != null || s.Keywords != null)
                    .Select(s => new { s.AreasOfExpertise, s.Keywords })
                    .ToListAsync();

                var tempAreaDist = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var tempSkillDist = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in rawData)
                {
                    if (!string.IsNullOrWhiteSpace(item.AreasOfExpertise))
                    {
                        var areas = item.AreasOfExpertise.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var area in areas)
                        {
                            var trimmed = area.Trim();
                            if (tempAreaDist.ContainsKey(trimmed))
                                tempAreaDist[trimmed]++;
                            else
                                tempAreaDist[trimmed] = 1;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.Keywords))
                    {
                        var skills = item.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var skill in skills)
                        {
                            var trimmed = skill.Trim();
                            if (tempSkillDist.ContainsKey(trimmed))
                                tempSkillDist[trimmed]++;
                            else
                                tempSkillDist[trimmed] = 1;
                        }
                    }
                }

                areaDistribution = tempAreaDist;
                skillDistributionforadmin = tempSkillDist;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading analytics: {ex.Message}");
            }
        }

        private async Task LoadStudentsWithAuth0DetailsAsync()
        {
            try
            {
                using var context = DbFactory.CreateDbContext();

                var students = await context.Students.AsNoTracking().ToListAsync();

                if (!students.Any())
                {
                    StudentsWithAuth0Details = new List<StudentWithAuth0Details>();
                    return;
                }

                // Map students to StudentWithAuth0Details
                StudentsWithAuth0Details = students.Select(s => new StudentWithAuth0Details
                {
                    Name = s.Name,
                    Surname = s.Surname,
                    Email = s.Email,
                    Department = s.Department,
                    School = s.School,
                    AreasOfExpertise = s.AreasOfExpertise,
                    Keywords = s.Keywords
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading students: {ex.Message}");
                StudentsWithAuth0Details = new List<StudentWithAuth0Details>();
            }
        }

        private void FilterAreasBySchool(ChangeEventArgs e)
        {
            SelectedAreaSchool = e.Value?.ToString() ?? "";
            UpdateAreasChart();
        }

        private void FilterAreasByDepartment(ChangeEventArgs e)
        {
            SelectedAreaDepartment = e.Value?.ToString() ?? "";
            UpdateAreasChart();
        }

        private void FilterSkillsBySchool(ChangeEventArgs e)
        {
            SelectedSkillSchool = e.Value?.ToString() ?? "";
            UpdateSkillsChart();
        }

        private void FilterSkillsByDepartment(ChangeEventArgs e)
        {
            SelectedSkillDepartment = e.Value?.ToString() ?? "";
            UpdateSkillsChart();
        }

        private void UpdateAreasChart()
        {
            var filtered = StudentsWithAuth0Details
                .Where(s => (string.IsNullOrEmpty(SelectedAreaSchool) || s.School == SelectedAreaSchool) &&
                        (string.IsNullOrEmpty(SelectedAreaDepartment) || s.Department == SelectedAreaDepartment))
                .ToList();

            var filteredAreaDistribution = new Dictionary<string, int>();
            foreach (var student in filtered)
            {
                if (!string.IsNullOrWhiteSpace(student.AreasOfExpertise))
                {
                    var areas = student.AreasOfExpertise.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var area in areas.Select(a => a.Trim()))
                    {
                        if (filteredAreaDistribution.ContainsKey(area))
                            filteredAreaDistribution[area]++;
                        else
                            filteredAreaDistribution[area] = 1;
                    }
                }
            }

            var areaLabels = filteredAreaDistribution.Keys.ToArray();
            var areaValues = filteredAreaDistribution.Values.ToArray();

            JS.InvokeVoidAsync("renderCharts",
                new { labels = areaLabels, values = areaValues },
                null);
        }

        private void UpdateSkillsChart()
        {
            var filtered = StudentsWithAuth0Details
                .Where(s => (string.IsNullOrEmpty(SelectedSkillSchool) || s.School == SelectedSkillSchool) &&
                        (string.IsNullOrEmpty(SelectedSkillDepartment) || s.Department == SelectedSkillDepartment))
                .ToList();

            var filteredSkillDistribution = new Dictionary<string, int>();
            foreach (var student in filtered)
            {
                if (!string.IsNullOrWhiteSpace(student.Keywords))
                {
                    var skills = student.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var skill in skills.Select(s => s.Trim()))
                    {
                        if (filteredSkillDistribution.ContainsKey(skill))
                            filteredSkillDistribution[skill]++;
                        else
                            filteredSkillDistribution[skill] = 1;
                    }
                }
            }

            var skillLabels = filteredSkillDistribution.Keys.ToArray();
            var skillValues = filteredSkillDistribution.Values.ToArray();

            JS.InvokeVoidAsync("renderCharts",
                null,
                new { labels = skillLabels, values = skillValues });
        }
    }

    // Local DTO class for student data with Auth0 details
    public class StudentWithAuth0Details
    {
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string School { get; set; } = "";
        public string AreasOfExpertise { get; set; } = "";
        public string Keywords { get; set; } = "";
        public DateTime? SignUpDate { get; set; }
        public DateTime? LatestLogin { get; set; }
        public string LoginBrowser { get; set; } = "";
        public bool IsMobile { get; set; }
        public LocationInfo? LocationInfo { get; set; }
        public int LoginTimes { get; set; }
        public string LastIp { get; set; } = "";
        public bool? IsEmailVerified { get; set; }
    }

    public class LocationInfo
    {
        public string CityName { get; set; } = "";
        public string CountryName { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string CountryCode3 { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZone { get; set; } = "";
        public string ContinentCode { get; set; } = "";
    }
}

