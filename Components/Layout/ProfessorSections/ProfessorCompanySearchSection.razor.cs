using Microsoft.AspNetCore.Components;
using QuizManager.Models;
using QuizManager.Services.ProfessorDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ProfessorSections
{
    public partial class ProfessorCompanySearchSection : ComponentBase
    {
        [Inject] private IProfessorDashboardService ProfessorDashboardService { get; set; } = default!;

        // Form Visibility
        private bool isProfessorSearchCompanyFormVisible = false;

        // Search Fields
        private string searchCompanyEmailAsProfessorToFindCompany = "";
        private string searchCompanyNameENGAsProfessorToFindCompany = "";
        private List<string> companyNameSuggestions = new List<string>();
        private string searchCompanyTypeAsProfessorToFindCompany = "";
        private List<string> ForeasType { get; set; } = new List<string>();
        private string searchCompanyActivityInputAsProfessorToFindCompany = "";
        private List<string> companyActivitySuggestions = new List<string>();
        private HashSet<string> selectedCompanyActivities = new HashSet<string>();
        private string searchCompanyTownAsProfessorToFindCompany = "";
        private string searchCompanyAreasAsProfessorToFindCompany = "";
        private List<string> areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor = new List<string>();
        private HashSet<string> selectedAreasOfInterest_WhenSearchForCompanyAsProfessor = new HashSet<string>();
        // Properties for Razor file (without suffix)
        private List<string> areasOfInterestSuggestions = new List<string>();
        private HashSet<string> selectedAreasOfInterest = new HashSet<string>();
        // Company desired skills
        private string searchCompanyDesiredSkillsInputAsProfessorToFindCompany = string.Empty;
        private List<string> companyDesiredSkillsSuggestions = new List<string>();
        private List<string> selectedCompanyDesiredSkills = new List<string>();
        // Pagination
        private int CompanySearchPerPage = 10;
        private int[] companySearchPageSizeOptions = new[] { 10, 50, 100 };
        private int currentPage_CompanySearch = 1;

        // Search Results and Pagination
        private List<QuizManager.Models.Company> searchResultsAsProfessorToFindCompany = new List<QuizManager.Models.Company>();
        private int CompaniesPerPage_SearchForCompaniesAsProfessor = 10;
        private int[] pageSizeOptions_SearchForCompaniesAsProfessor = new[] { 10, 50, 100 };
        private int currentCompanyPage_SearchForCompaniesAsProfessor = 1;
        private int totalCompanyPages_SearchForCompaniesAsProfessor = 0;
        
        // Company Details Modal
        private bool showCompanyDetailsModal = false;
        private QuizManager.Models.Company selectedCompany;

        // Helper properties
        private List<string> Activity { get; set; } = new List<string>();
        private List<string> Regions = new List<string>
        {
            "Ανατολική Μακεδονία και Θράκη",
            "Κεντρική Μακεδονία",
            "Δυτική Μακεδονία",
            "Ήπειρος",
            "Θεσσαλία",
            "Ιόνια Νησιά",
            "Δυτική Ελλάδα",
            "Κεντρική Ελλάδα",
            "Αττική",
            "Πελοπόννησος",
            "Βόρειο Αιγαίο",
            "Νότιο Αιγαίο",
            "Κρήτη"
        };
        private Dictionary<string, List<string>> RegionToTownsMap = new Dictionary<string, List<string>>
        {
            {"Ανατολική Μακεδονία και Θράκη", new List<string> {"Κομοτηνή", "Αλεξανδρούπολη", "Καβάλα", "Ξάνθη", "Δράμα", "Ορεστιάδα", "Διδυμότειχο", "Ίασμος", "Νέα Βύσσα", "Φέρες"}},
            {"Κεντρική Μακεδονία", new List<string> {"Θεσσαλονίκη", "Κατερίνη", "Σέρρες", "Κιλκίς", "Πολύγυρος", "Ναούσα", "Έδεσσα", "Γιαννιτσά", "Καβάλα", "Άμφισσα"}},
            {"Δυτική Μακεδονία", new List<string> {"Κοζάνη", "Φλώρινα", "Καστοριά", "Γρεβενά"}},
            {"Ήπειρος", new List<string> {"Ιωάννινα", "Άρτα", "Πρέβεζα", "Ηγουμενίτσα"}},
            {"Θεσσαλία", new List<string> {"Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα"}},
            {"Ιόνια Νησιά", new List<string> {"Κέρκυρα", "Λευκάδα", "Κεφαλονιά", "Ζάκυνθος", "Ιθάκη", "Παξοί", "Κυθήρα"}},
            {"Δυτική Ελλάδα", new List<string> {"Πάτρα", "Μεσολόγγι", "Αμφιλοχία", "Πύργος", "Αιγίο", "Ναύπακτος"}},
            {"Κεντρική Ελλάδα", new List<string> {"Λαμία", "Χαλκίδα", "Λιβαδειά", "Θήβα", "Αλιάρτος", "Αμφίκλεια"}},
            {"Αττική", new List<string> {"Αθήνα", "Πειραιάς", "Κηφισιά", "Παλλήνη", "Αγία Παρασκευή", "Χαλάνδρι", "Καλλιθέα", "Γλυφάδα", "Περιστέρι", "Αιγάλεω"}},
            {"Πελοπόννησος", new List<string> {"Πάτρα", "Τρίπολη", "Καλαμάτα", "Κορίνθος", "Άργος", "Ναύπλιο", "Σπάρτη", "Κυπαρισσία", "Πύργος", "Μεσσήνη"}},
            {"Βόρειο Αιγαίο", new List<string> {"Μυτιλήνη", "Χίος", "Λήμνος", "Σάμος", "Ίκαρος", "Λέσβος", "Θάσος", "Σκύρος", "Ψαρά"}},
            {"Νότιο Αιγαίο", new List<string> {"Ρόδος", "Κως", "Κρήτη", "Κάρπαθος", "Σαντορίνη", "Μύκονος", "Νάξος", "Πάρος", "Σύρος", "Άνδρος"}},
            {"Κρήτη", new List<string> {"Ηράκλειο", "Χανιά", "Ρέθυμνο", "Αγία Νικόλαος", "Ιεράπετρα", "Σητεία", "Κίσαμος", "Παλαιόχωρα", "Αρχάνες", "Ανώγεια"}}
        };

        protected override async Task OnInitializedAsync()
        {
            InitializeStaticData();
            await LoadLookups();
        }

        private void InitializeStaticData()
        {
            // Initialize ForeasType
            ForeasType = new List<string>
            {
                "Ιδιωτικός Φορέας",
                "Δημόσιος Φορέας",
                "Μ.Κ.Ο.",
                "Άλλο"
            };

            Activity = new List<string>();
        }

        private async Task LoadLookups()
        {
            Activity = new List<string>(); // Placeholder until service exposes activities
            var lookups = await ProfessorDashboardService.GetLookupsAsync();
            // Lookups currently expose Areas/Skills/Companies/ResearchGroups; Regions/Towns remain static defaults
        }

        private void ToggleFormVisibilityForSearchCompanyAsProfessor()
        {
            isProfessorSearchCompanyFormVisible = !isProfessorSearchCompanyFormVisible;
            StateHasChanged();
        }

        private async Task HandleCompanyInput(ChangeEventArgs e)
        {
            searchCompanyNameENGAsProfessorToFindCompany = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchCompanyNameENGAsProfessorToFindCompany) && searchCompanyNameENGAsProfessorToFindCompany.Length >= 2)
            {
                var results = await ProfessorDashboardService.SearchCompaniesAsync(new CompanySearchFilter
                {
                    CompanyName = searchCompanyNameENGAsProfessorToFindCompany,
                    MaxResults = 10
                });
                companyNameSuggestions = results
                    .Select(c => c.CompanyNameENG ?? c.CompanyName ?? string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();
            }
            else
            {
                companyNameSuggestions.Clear();
            }
            StateHasChanged();
        }

        private void SelectCompanyNameSuggestion(string suggestion)
        {
            searchCompanyNameENGAsProfessorToFindCompany = suggestion;
            companyNameSuggestions.Clear();
            StateHasChanged();
        }

        private void HandleCompanyActivityInput(ChangeEventArgs e)
        {
            searchCompanyActivityInputAsProfessorToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchCompanyActivityInputAsProfessorToFindCompany))
            {
                companyActivitySuggestions.Clear();
                return;
            }

            // Filter activities from the predefined list
            companyActivitySuggestions = Activity
                .Where(activity => activity.Contains(searchCompanyActivityInputAsProfessorToFindCompany, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
            StateHasChanged();
        }

        private void SelectCompanyActivitySuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyActivities.Contains(suggestion))
            {
                selectedCompanyActivities.Add(suggestion);
                companyActivitySuggestions.Clear();
                searchCompanyActivityInputAsProfessorToFindCompany = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedCompanyActivity(string activity)
        {
            selectedCompanyActivities.Remove(activity);
            StateHasChanged();
        }

        private async Task HandleAreasOfInterestInput_WhenSearchForCompanyAsProfessor(ChangeEventArgs e)
        {
            searchCompanyAreasAsProfessorToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
            areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor = new List<string>();

            if (searchCompanyAreasAsProfessorToFindCompany.Length >= 1)
            {
                try
                {
                    var areas = await ProfessorDashboardService.SearchAreasAsync(searchCompanyAreasAsProfessorToFindCompany);
                    areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor = areas
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving Areas of Interest from Database: {ex.Message}");
                    areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor = new List<string>();
                }
            }
            else
            {
                areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor.Clear();
            }

            StateHasChanged();
        }

        private void SelectAreasOfInterestSuggestion_WhenSearchForCompanyAsProfessor(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasOfInterest_WhenSearchForCompanyAsProfessor.Contains(suggestion))
            {
                selectedAreasOfInterest_WhenSearchForCompanyAsProfessor.Add(suggestion);
                areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor.Clear();
                searchCompanyAreasAsProfessorToFindCompany = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaOfInterest_WhenSearchForCompanyAsProfessor(string area)
        {
            selectedAreasOfInterest_WhenSearchForCompanyAsProfessor.Remove(area);
            StateHasChanged();
        }

        private async Task SearchCompaniesAsProfessorToFindCompany()
        {
            var combinedSearchAreas = new List<string>();

            // Use properties without suffix (from Razor file)
            if (selectedAreasOfInterest != null && selectedAreasOfInterest.Any())
                combinedSearchAreas.AddRange(selectedAreasOfInterest);
            else if (selectedAreasOfInterest_WhenSearchForCompanyAsProfessor != null && selectedAreasOfInterest_WhenSearchForCompanyAsProfessor.Any())
                combinedSearchAreas.AddRange(selectedAreasOfInterest_WhenSearchForCompanyAsProfessor);

            if (!string.IsNullOrEmpty(searchCompanyAreasAsProfessorToFindCompany))
                combinedSearchAreas.Add(searchCompanyAreasAsProfessorToFindCompany);

            // Use NormalizeAreas method for search terms
            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            var results = await ProfessorDashboardService.SearchCompaniesAsync(new CompanySearchFilter
            {
                CompanyName = searchCompanyNameENGAsProfessorToFindCompany,
                CompanyEmail = searchCompanyEmailAsProfessorToFindCompany,
                CompanyType = searchCompanyTypeAsProfessorToFindCompany,
                CompanyTown = searchCompanyTownAsProfessorToFindCompany,
                CompanyAreas = string.Join(',', normalizedSearchAreas),
                CompanyDesiredSkills = string.Join(',', selectedCompanyDesiredSkills),
                CompanyActivity = string.Join(',', selectedCompanyActivities),
                MaxResults = 500
            });

            searchResultsAsProfessorToFindCompany = results.ToList();
            currentCompanyPage_SearchForCompaniesAsProfessor = 1;
            currentPage_CompanySearch = 1;
            UpdateTotalPages();
            StateHasChanged();
        }

        private IEnumerable<QuizManager.Models.Company> GetPaginatedCompanyResults()
        {
            if (searchResultsAsProfessorToFindCompany == null || !searchResultsAsProfessorToFindCompany.Any())
                return Enumerable.Empty<QuizManager.Models.Company>();

            return searchResultsAsProfessorToFindCompany
                .Skip((currentCompanyPage_SearchForCompaniesAsProfessor - 1) * CompaniesPerPage_SearchForCompaniesAsProfessor)
                .Take(CompaniesPerPage_SearchForCompaniesAsProfessor);
        }

        private void OnPageSizeChange_SearchForCompaniesAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                CompaniesPerPage_SearchForCompaniesAsProfessor = newSize;
                currentCompanyPage_SearchForCompaniesAsProfessor = 1;
                UpdateTotalPages();
                StateHasChanged();
            }
        }

        private void UpdateTotalPages()
        {
            if (searchResultsAsProfessorToFindCompany == null || !searchResultsAsProfessorToFindCompany.Any())
            {
                totalCompanyPages_SearchForCompaniesAsProfessor = 0;
                return;
            }

            totalCompanyPages_SearchForCompaniesAsProfessor = (int)Math.Ceiling((double)searchResultsAsProfessorToFindCompany.Count / CompaniesPerPage_SearchForCompaniesAsProfessor);
        }

        private void GoToFirstCompanyPage()
        {
            currentCompanyPage_SearchForCompaniesAsProfessor = 1;
            StateHasChanged();
        }

        private void PreviousCompanyPage()
        {
            if (currentCompanyPage_SearchForCompaniesAsProfessor > 1)
            {
                currentCompanyPage_SearchForCompaniesAsProfessor--;
                StateHasChanged();
            }
        }

        private void GoToCompanyPage(int page)
        {
            if (page >= 1 && page <= totalCompanyPages_SearchForCompaniesAsProfessor)
            {
                currentCompanyPage_SearchForCompaniesAsProfessor = page;
                StateHasChanged();
            }
        }

        private void NextCompanyPage()
        {
            if (currentCompanyPage_SearchForCompaniesAsProfessor < totalCompanyPages_SearchForCompaniesAsProfessor)
            {
                currentCompanyPage_SearchForCompaniesAsProfessor++;
                StateHasChanged();
            }
        }

        private void GoToLastCompanyPage()
        {
            currentCompanyPage_SearchForCompaniesAsProfessor = totalCompanyPages_SearchForCompaniesAsProfessor;
            StateHasChanged();
        }

        private List<int> GetVisibleCompanyPages()
        {
            var pages = new List<int>();
            int current = currentCompanyPage_SearchForCompaniesAsProfessor;
            int total = totalCompanyPages_SearchForCompaniesAsProfessor;

            if (total == 0) return pages;

            pages.Add(1);
            if (current > 3) pages.Add(-1); // Placeholder for ellipsis

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1); // Placeholder for ellipsis

            if (total > 1) pages.Add(total);
            return pages;
        }

        // Helper methods
        private IEnumerable<string> NormalizeAreas(string areas)
        {
            if (string.IsNullOrWhiteSpace(areas))
                return Array.Empty<string>();

            return areas
                .Split(new string[] { ",", "/", ", ", " / ", " ," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(area => area.Trim().ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(area => !string.IsNullOrEmpty(area));
        }

        private List<string> ExpandAreasWithSubfields(List<string> normalizedAreas)
        {
            var expandedAreas = new List<string>();

            foreach (var area in normalizedAreas)
            {
                // Add the original area
                expandedAreas.Add(area);

                // If the area contains a subfield (has '/'), expand it
                if (area.Contains('/'))
                {
                    var parts = area.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                .Select(part => part.Trim().ToLower())
                                .Where(part => !string.IsNullOrEmpty(part))
                                .ToList();

                    if (parts.Count >= 2)
                    {
                        // Add main area only (first part)
                        expandedAreas.Add(parts[0]);

                        // Add subfield only (second part)
                        expandedAreas.Add(parts[1]);

                        // Add all individual parts
                        expandedAreas.AddRange(parts);
                    }
                }
            }

            return expandedAreas.Distinct().ToList();
        }

        // Company Desired Skills Methods (from backup MainLayout)
        private async Task HandleCompanyDesiredSkillsInput(ChangeEventArgs e)
        {
            searchCompanyDesiredSkillsInputAsProfessorToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
            companyDesiredSkillsSuggestions = new();

            if (searchCompanyDesiredSkillsInputAsProfessorToFindCompany.Length >= 1)
            {
                try
                {
                    var skills = await ProfessorDashboardService.SearchSkillsAsync(searchCompanyDesiredSkillsInputAsProfessorToFindCompany);
                    companyDesiredSkillsSuggestions = skills
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Σφάλμα κατά την ανάκτηση δεξιοτήτων: {ex.Message}");
                    companyDesiredSkillsSuggestions = new List<string>();
                }
            }
            else
            {
                companyDesiredSkillsSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectCompanyDesiredSkillSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyDesiredSkills.Contains(suggestion))
            {
                selectedCompanyDesiredSkills.Add(suggestion);
                companyDesiredSkillsSuggestions.Clear();
                searchCompanyDesiredSkillsInputAsProfessorToFindCompany = string.Empty;
            }
        }

        private void RemoveSelectedCompanyDesiredSkill(string skill)
        {
            selectedCompanyDesiredSkills.Remove(skill);
            StateHasChanged();
        }

        // SearchCompaniesAsProfessor (method expected by Razor file)
        private async Task SearchCompaniesAsProfessor()
        {
            await SearchCompaniesAsProfessorToFindCompany();
        }

        private void ClearSearchFieldsAsProfessorToFindCompany()
        {
            // Clear all search fields
            searchCompanyEmailAsProfessorToFindCompany = string.Empty;
            searchCompanyNameENGAsProfessorToFindCompany = string.Empty;
            searchCompanyTypeAsProfessorToFindCompany = string.Empty;
            searchCompanyActivityInputAsProfessorToFindCompany = string.Empty;
            searchCompanyTownAsProfessorToFindCompany = string.Empty;
            searchCompanyAreasAsProfessorToFindCompany = string.Empty;
            searchCompanyDesiredSkillsInputAsProfessorToFindCompany = string.Empty;

            // Clear selected areas, activities, skills and suggestions
            selectedAreasOfInterest.Clear();
            selectedAreasOfInterest_WhenSearchForCompanyAsProfessor.Clear();
            selectedCompanyActivities.Clear();
            selectedCompanyDesiredSkills.Clear();
            areasOfInterestSuggestions.Clear();
            areasOfInterestSuggestions_WhenSearchForCompanyAsProfessor.Clear();
            companyActivitySuggestions.Clear();
            companyDesiredSkillsSuggestions.Clear();

            // Clear search results
            searchResultsAsProfessorToFindCompany = null;
            currentCompanyPage_SearchForCompaniesAsProfessor = 1;
            currentPage_CompanySearch = 1;
            UpdateTotalPages();
            StateHasChanged();
        }

        private void OnPageSizeChangeForCompanySearch(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                CompanySearchPerPage = newSize;
                CompaniesPerPage_SearchForCompaniesAsProfessor = newSize;
                currentPage_CompanySearch = 1;
                currentCompanyPage_SearchForCompaniesAsProfessor = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<QuizManager.Models.Company> GetPaginatedCompanySearchResults()
        {
            return searchResultsAsProfessorToFindCompany?
                .Skip((currentPage_CompanySearch - 1) * CompanySearchPerPage)
                .Take(CompanySearchPerPage) ?? Enumerable.Empty<QuizManager.Models.Company>();
        }

        // Pagination methods for CompanySearch (from backup MainLayout)
        private int totalPages_CompanySearch => 
            (int)Math.Ceiling((double)(searchResultsAsProfessorToFindCompany?.Count ?? 0) / CompanySearchPerPage);

        private void GoToFirstPage_CompanySearch()
        {
            currentPage_CompanySearch = 1;
            StateHasChanged();
        }

        private void GoToLastPage_CompanySearch()
        {
            currentPage_CompanySearch = totalPages_CompanySearch;
            StateHasChanged();
        }

        private void PreviousPage_CompanySearch()
        {
            if (currentPage_CompanySearch > 1)
            {
                currentPage_CompanySearch--;
                StateHasChanged();
            }
        }

        private void NextPage_CompanySearch()
        {
            if (currentPage_CompanySearch < totalPages_CompanySearch)
            {
                currentPage_CompanySearch++;
                StateHasChanged();
            }
        }

        private void GoToPage_CompanySearch(int page)
        {
            if (page > 0 && page <= totalPages_CompanySearch)
            {
                currentPage_CompanySearch = page;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePages_CompanySearch()
        {
            var pages = new List<int>();
            int current = currentPage_CompanySearch;
            int total = totalPages_CompanySearch;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1);

            if (total > 1) pages.Add(total);
            return pages;
        }

        // ShowCompanyDetailsWhenSearchAsProfessor (from backup MainLayout)
        private void ShowCompanyDetailsWhenSearchAsProfessor(QuizManager.Models.Company company)
        {
            selectedCompany = company;
            showCompanyDetailsModal = true;
            StateHasChanged();
        }
    }
}
