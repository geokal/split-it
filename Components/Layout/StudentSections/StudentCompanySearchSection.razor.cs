using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizManager.Models;
using QuizManager.Services.StudentDashboard;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentCompanySearchSection : ComponentBase
    {
        [Inject] private IStudentDashboardService StudentDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // Form Visibility
        private bool isStudentSearchCompanyFormVisible = false;

        // Global Search
        private string globalCompanySearch = "";

        // Search Fields
        private string searchCompanyEmailAsStudentToFindCompany = "";
        private string searchCompanyNameENGAsStudentToFindCompany = "";
        private List<string> companyNameSuggestionsAsStudent = new List<string>();
        private string searchCompanyTypeAsStudentToFindCompany = "";
        private List<string> ForeasType { get; set; } = new List<string>();
        private string searchCompanyActivityInputAsStudentToFindCompany = "";
        private List<string> companyActivitySuggestionsAsStudent = new List<string>();
        private HashSet<string> selectedCompanyActivitiesAsStudent = new HashSet<string>();
        private string searchCompanyTownAsStudentToFindCompany = "";
        private List<string> Regions { get; set; } = new List<string>();
        private Dictionary<string, List<string>> RegionToTownsMap { get; set; } = new Dictionary<string, List<string>>();
        private string searchCompanyAreasAsStudentToFindCompany = "";
        private List<string> areasOfInterestSuggestionsAsStudent = new List<string>();
        private HashSet<string> selectedAreasOfInterestAsStudent = new HashSet<string>();
        private string searchCompanyDesiredSkillsInputAsStudentToFindCompany = "";
        private List<string> companyDesiredSkillsSuggestionsAsStudent = new List<string>();
        private HashSet<string> selectedCompanyDesiredSkillsAsStudent = new HashSet<string>();

        // Search Results and Pagination
        private IEnumerable<QuizManager.Models.Company> searchResultsAsStudentToFindCompany = Enumerable.Empty<QuizManager.Models.Company>();
        private int CompanySearchPerPageAsStudent = 10;
        private int[] companySearchPageSizeOptionsAsStudent = new[] { 10, 50, 100 };
        private int currentPage_CompanySearchAsStudent = 1;
        private int totalPages_CompanySearchAsStudent = 0;

        // Company Details Modal
        private bool showCompanyDetailsModalAsStudent = false;
        private QuizManager.Models.Company selectedCompanyAsStudent = null;

        // Helper properties - initialized from MainLayout or loaded here
        private List<string> Activity { get; set; } = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            // Initialize static data
            InitializeStaticData();
            
            // Load dynamic data from service
            await LoadServiceDataAsync();
        }

        private async Task LoadServiceDataAsync()
        {
            try
            {
                // Load company types from service
                var companyTypes = await StudentDashboardService.GetCompanyTypeSuggestionsAsync();
                if (companyTypes.Any())
                {
                    ForeasType = companyTypes.ToList();
                }

                // Load regions from service
                var regions = await StudentDashboardService.GetRegionSuggestionsAsync();
                if (regions.Any())
                {
                    Regions = regions.ToList();
                }

                // Load region to towns map
                var regionToTownsMap = await StudentDashboardService.GetRegionToTownsMapAsync();
                RegionToTownsMap = regionToTownsMap;
            }
            catch (Exception ex)
            {
                // Keep static data as fallback if service fails
            }
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

            // Initialize Regions
            Regions = new List<string>
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

            // Initialize RegionToTownsMap
            RegionToTownsMap = new Dictionary<string, List<string>>
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
                {"Κρήτη", new List<string> {"Ηράκλειο", "Χανιά", "Ρέθυμνο", "Αγία Νικόλαος", "Ιεράπετρα", "Σητεία", "Κίσαμος", "Παλαιόχωρα", "Αρχάνες", "Ανώγεια"}},
            };

            // Activity list - this might need to be loaded from database or passed as parameter
            // For now, leaving it empty - will need to be populated
            Activity = new List<string>();
        }

        private void ToggleFormVisibilityForSearchCompanyAsStudent()
        {
            isStudentSearchCompanyFormVisible = !isStudentSearchCompanyFormVisible;
            StateHasChanged();
        }

        private async Task HandleCompanyInputAsStudent(ChangeEventArgs e)
        {
            searchCompanyNameENGAsStudentToFindCompany = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchCompanyNameENGAsStudentToFindCompany) && searchCompanyNameENGAsStudentToFindCompany.Length >= 2)
            {
                var suggestions = await StudentDashboardService.GetCompanyNameSuggestionsAsync(searchCompanyNameENGAsStudentToFindCompany);
                companyNameSuggestionsAsStudent = suggestions.ToList();
            }
            else
            {
                companyNameSuggestionsAsStudent.Clear();
            }
            StateHasChanged();
        }

        private void SelectCompanyNameSuggestionAsStudent(string suggestion)
        {
            searchCompanyNameENGAsStudentToFindCompany = suggestion;
            companyNameSuggestionsAsStudent.Clear();
            StateHasChanged();
        }

        private async Task HandleCompanyActivityInputAsStudent(ChangeEventArgs e)
        {
            searchCompanyActivityInputAsStudentToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
        
            if (string.IsNullOrWhiteSpace(searchCompanyActivityInputAsStudentToFindCompany))
            {
                companyActivitySuggestionsAsStudent.Clear();
                return;
            }

            if (!string.IsNullOrWhiteSpace(searchCompanyActivityInputAsStudentToFindCompany) && searchCompanyActivityInputAsStudentToFindCompany.Length >= 2)
            {
                var suggestions = await StudentDashboardService.GetCompanyActivitySuggestionsAsync(searchCompanyActivityInputAsStudentToFindCompany);
                companyActivitySuggestionsAsStudent = suggestions.Take(10).ToList();
            }
            else
            {
                companyActivitySuggestionsAsStudent.Clear();
            }
            StateHasChanged();
        }

        private void SelectCompanyActivitySuggestionAsStudent(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyActivitiesAsStudent.Contains(suggestion))
            {
                selectedCompanyActivitiesAsStudent.Add(suggestion);
                companyActivitySuggestionsAsStudent.Clear();
                searchCompanyActivityInputAsStudentToFindCompany = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedCompanyActivityAsStudent(string activity)
        {
            selectedCompanyActivitiesAsStudent.Remove(activity);
            StateHasChanged();
        }

        private void HandleAreasOfInterestInput_WhenSearchForCompanyAsStudent(ChangeEventArgs e)
        {
            // Similar implementation
            StateHasChanged();
        }

        private void SelectAreasOfInterestSuggestion_WhenSearchForCompanyAsStudent(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasOfInterestAsStudent.Contains(suggestion))
            {
                selectedAreasOfInterestAsStudent.Add(suggestion);
                areasOfInterestSuggestionsAsStudent.Clear();
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaOfInterest_WhenSearchForCompanyAsStudent(string area)
        {
            selectedAreasOfInterestAsStudent.Remove(area);
            StateHasChanged();
        }

        private void HandleCompanyDesiredSkillsInputAsStudent(ChangeEventArgs e)
        {
            // Similar implementation
            StateHasChanged();
        }

        private void SelectCompanyDesiredSkillsSuggestionAsStudent(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyDesiredSkillsAsStudent.Contains(suggestion))
            {
                selectedCompanyDesiredSkillsAsStudent.Add(suggestion);
                companyDesiredSkillsSuggestionsAsStudent.Clear();
                StateHasChanged();
            }
        }

        private void RemoveSelectedCompanyDesiredSkillAsStudent(string skill)
        {
            selectedCompanyDesiredSkillsAsStudent.Remove(skill);
            StateHasChanged();
        }

        private async Task SearchCompaniesAsStudent()
        {
            var combinedSearchAreas = new List<string>();

            if (selectedAreasOfInterestAsStudent != null && selectedAreasOfInterestAsStudent.Any())
                combinedSearchAreas.AddRange(selectedAreasOfInterestAsStudent);

            if (!string.IsNullOrEmpty(searchCompanyAreasAsStudentToFindCompany))
                combinedSearchAreas.Add(searchCompanyAreasAsStudentToFindCompany);

            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            // Build search request
            var searchRequest = new StudentCompanySearchRequest
            {
                Email = string.IsNullOrEmpty(globalCompanySearch) ? searchCompanyEmailAsStudentToFindCompany : globalCompanySearch,
                Name = string.IsNullOrEmpty(globalCompanySearch) ? searchCompanyNameENGAsStudentToFindCompany : globalCompanySearch,
                Type = string.IsNullOrEmpty(globalCompanySearch) ? searchCompanyTypeAsStudentToFindCompany : globalCompanySearch,
                Activity = selectedCompanyActivitiesAsStudent.FirstOrDefault(),
                Town = string.IsNullOrEmpty(globalCompanySearch) ? searchCompanyTownAsStudentToFindCompany : globalCompanySearch,
                Areas = selectedAreasOfInterestAsStudent.ToList(),
                DesiredSkills = selectedCompanyDesiredSkillsAsStudent.ToList()
            };

            var companies = (await StudentDashboardService.SearchCompaniesAsync(searchRequest))
                .Where(c =>
                {
                    // GLOBAL SEARCH - Additional client-side filtering for complex multi-word search
                    if (!string.IsNullOrEmpty(globalCompanySearch))
                    {
                        var searchTerm = globalCompanySearch.Trim();
                        var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                        var globalMatch = searchWords.Any(word =>
                            (c.CompanyDescription?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyAreas?.Contains(word, StringComparison.OrdinalIgnoreCase) == true)); 

                        if (!globalMatch) return false;
                    }

                    // Apply area matching (more complex than service handles)
                    var normalizedCompanyAreas = NormalizeAreas(c.CompanyAreas ?? "").ToList();
                    var expandedCompanyAreas = ExpandAreasWithSubfields(normalizedCompanyAreas);

                    var areaMatch = !normalizedSearchAreas.Any() ||
                        normalizedSearchAreas.Any(searchArea =>
                            expandedCompanyAreas.Any(companyArea =>
                                companyArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                                searchArea.Contains(companyArea, StringComparison.OrdinalIgnoreCase)));

                    // Apply activity matching (more complex than service handles)
                    var activityMatch = !selectedCompanyActivitiesAsStudent.Any() ||
                        selectedCompanyActivitiesAsStudent.Any(activity => 
                            c.CompanyActivity != null && c.CompanyActivity.Contains(activity, StringComparison.OrdinalIgnoreCase));

                    // Apply skills matching (more complex than service handles)
                    var skillsMatch = !selectedCompanyDesiredSkillsAsStudent.Any() ||
                        selectedCompanyDesiredSkillsAsStudent.All(skill => 
                            c.CompanyDesiredSkills != null && c.CompanyDesiredSkills.Contains(skill, StringComparison.OrdinalIgnoreCase));

                    return areaMatch && activityMatch && skillsMatch;
                })
                .ToList();

            searchResultsAsStudentToFindCompany = companies;
            currentPage_CompanySearchAsStudent = 1;
            UpdateTotalPages();
            StateHasChanged();
        }

        private void ClearSearchFieldsAsStudentToFindCompany()
        {
            // Clear global search
            globalCompanySearch = string.Empty;
            
            // Clear existing fields
            searchCompanyEmailAsStudentToFindCompany = string.Empty;
            searchCompanyNameENGAsStudentToFindCompany = string.Empty;
            searchCompanyTypeAsStudentToFindCompany = string.Empty;
            searchCompanyActivityInputAsStudentToFindCompany = string.Empty;
            searchCompanyTownAsStudentToFindCompany = string.Empty;
            searchCompanyAreasAsStudentToFindCompany = string.Empty;
            searchCompanyDesiredSkillsInputAsStudentToFindCompany = string.Empty;

            // Clear autocomplete suggestions
            companyNameSuggestionsAsStudent.Clear();
            companyActivitySuggestionsAsStudent.Clear();
            companyDesiredSkillsSuggestionsAsStudent.Clear();
            areasOfInterestSuggestionsAsStudent.Clear();

            // Clear selected items
            selectedCompanyActivitiesAsStudent.Clear();
            selectedAreasOfInterestAsStudent.Clear();
            selectedCompanyDesiredSkillsAsStudent.Clear();

            // Clear results
            searchResultsAsStudentToFindCompany = Enumerable.Empty<QuizManager.Models.Company>();
            currentPage_CompanySearchAsStudent = 1;
            totalPages_CompanySearchAsStudent = 0;
            StateHasChanged();
        }

        private IEnumerable<QuizManager.Models.Company> GetPaginatedCompanySearchResultsAsStudent()
        {
            return searchResultsAsStudentToFindCompany?
                .Skip((currentPage_CompanySearchAsStudent - 1) * CompanySearchPerPageAsStudent)
                .Take(CompanySearchPerPageAsStudent) ?? Enumerable.Empty<QuizManager.Models.Company>();
        }

        private List<int> GetVisiblePages_CompanySearchAsStudent()
        {
            var pages = new List<int>();
            int current = currentPage_CompanySearchAsStudent;
            int total = totalPages_CompanySearchAsStudent;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1);

            if (total > 1) pages.Add(total);
            return pages;
        }

        private void GoToFirstPage_CompanySearchAsStudent()
        {
            currentPage_CompanySearchAsStudent = 1;
            StateHasChanged();
        }

        private void GoToLastPage_CompanySearchAsStudent()
        {
            currentPage_CompanySearchAsStudent = totalPages_CompanySearchAsStudent;
            StateHasChanged();
        }

        private void PreviousPage_CompanySearchAsStudent()
        {
            if (currentPage_CompanySearchAsStudent > 1)
            {
                currentPage_CompanySearchAsStudent--;
                StateHasChanged();
            }
        }

        private void NextPage_CompanySearchAsStudent()
        {
            if (currentPage_CompanySearchAsStudent < totalPages_CompanySearchAsStudent)
            {
                currentPage_CompanySearchAsStudent++;
                StateHasChanged();
            }
        }

        private void GoToPage_CompanySearchAsStudent(int page)
        {
            if (page > 0 && page <= totalPages_CompanySearchAsStudent)
            {
                currentPage_CompanySearchAsStudent = page;
                StateHasChanged();
            }
        }

        private void OnPageSizeChangeForCompanySearchAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                CompanySearchPerPageAsStudent = newSize;
                currentPage_CompanySearchAsStudent = 1;
                UpdateTotalPages();
                StateHasChanged();
            }
        }

        private void UpdateTotalPages()
        {
            if (searchResultsAsStudentToFindCompany == null || !searchResultsAsStudentToFindCompany.Any())
            {
                totalPages_CompanySearchAsStudent = 0;
                return;
            }

            totalPages_CompanySearchAsStudent = (int)Math.Ceiling((double)searchResultsAsStudentToFindCompany.Count() / CompanySearchPerPageAsStudent);
        }

        private void ShowCompanyDetailsWhenSearchAsStudent(QuizManager.Models.Company company)
        {
            selectedCompanyAsStudent = company;
            showCompanyDetailsModalAsStudent = true;
            StateHasChanged();
        }

        private void CloseCompanyDetailsModalWhenSearchAsStudent()
        {
            showCompanyDetailsModalAsStudent = false;
            selectedCompanyAsStudent = null;
            StateHasChanged();
        }

        private void OpenUrl(string url)
        {
            JS.InvokeVoidAsync("open", url, "_blank");
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

        // SelectCompanyDesiredSkillSuggestionAsStudent (from backup MainLayout)
        private void SelectCompanyDesiredSkillSuggestionAsStudent(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyDesiredSkillsAsStudent.Contains(suggestion))
            {
                selectedCompanyDesiredSkillsAsStudent.Add(suggestion);
                companyDesiredSkillsSuggestionsAsStudent.Clear();
                searchCompanyDesiredSkillsInputAsStudentToFindCompany = string.Empty;
            }
        }

        // OpenMap (from backup MainLayout)
        private void OpenMap(string location)
        {
            if (!string.IsNullOrWhiteSpace(location))
            {
                var mapUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(location)}";
                NavigationManager.NavigateTo(mapUrl, true);
            }
        }
    }
}
