using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Shared.Student
{
    public partial class StudentCompanySearchSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

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
        private IEnumerable<Company> searchResultsAsStudentToFindCompany = Enumerable.Empty<Company>();
        private int CompanySearchPerPageAsStudent = 10;
        private int[] companySearchPageSizeOptionsAsStudent = new[] { 10, 50, 100 };
        private int currentPage_CompanySearchAsStudent = 1;
        private int totalPages_CompanySearchAsStudent = 0;

        // Company Details Modal
        private bool showCompanyDetailsModalAsStudent = false;
        private Company selectedCompanyAsStudent = null;

        // Helper properties - initialized from MainLayout or loaded here
        private List<string> Activity { get; set; } = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            // Initialize static data
            InitializeStaticData();
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

        private void HandleCompanyInputAsStudent(ChangeEventArgs e)
        {
            searchCompanyNameENGAsStudentToFindCompany = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchCompanyNameENGAsStudentToFindCompany) && searchCompanyNameENGAsStudentToFindCompany.Length >= 2)
            {
                companyNameSuggestionsAsStudent = dbContext.Companies
                    .Where(c => c.CompanyNameENG.Contains(searchCompanyNameENGAsStudentToFindCompany))
                    .Select(c => c.CompanyNameENG)
                    .Distinct()
                    .ToList();
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

        private void HandleCompanyActivityInputAsStudent(ChangeEventArgs e)
        {
            searchCompanyActivityInputAsStudentToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
        
            if (string.IsNullOrWhiteSpace(searchCompanyActivityInputAsStudentToFindCompany))
            {
                companyActivitySuggestionsAsStudent.Clear();
                return;
            }

            companyActivitySuggestionsAsStudent = Activity
                .Where(activity => activity.Contains(searchCompanyActivityInputAsStudentToFindCompany, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
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

        private void SearchCompaniesAsStudent()
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

            var companies = dbContext.Companies
                .AsEnumerable()
                .Where(c =>
                {
                    // GLOBAL SEARCH - Multi-word search across all fields
                    if (!string.IsNullOrEmpty(globalCompanySearch))
                    {
                        var searchTerm = globalCompanySearch.Trim();
                        var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                        var globalMatch = searchWords.Any(word =>
                            (c.CompanyEmail?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyNameENG?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyType?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyActivity?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyTown?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyAreas?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyDesiredSkills?.Contains(word, StringComparison.OrdinalIgnoreCase) == true) ||
                            (c.CompanyDescription?.Contains(word, StringComparison.OrdinalIgnoreCase) == true)); 

                        if (!globalMatch) return false;
                    }
                    else
                    {
                        // Apply specific filters only when no global search is active
                        var basicMatch = (string.IsNullOrEmpty(searchCompanyEmailAsStudentToFindCompany) || 
                                        (c.CompanyEmail?.Contains(searchCompanyEmailAsStudentToFindCompany, StringComparison.OrdinalIgnoreCase) == true)) &&
                                    (string.IsNullOrEmpty(searchCompanyNameENGAsStudentToFindCompany) || 
                                        (c.CompanyNameENG?.Contains(searchCompanyNameENGAsStudentToFindCompany, StringComparison.OrdinalIgnoreCase) == true)) &&
                                    (string.IsNullOrEmpty(searchCompanyTypeAsStudentToFindCompany) || 
                                        (c.CompanyType?.Contains(searchCompanyTypeAsStudentToFindCompany, StringComparison.OrdinalIgnoreCase) == true)) &&
                                    (string.IsNullOrEmpty(searchCompanyTownAsStudentToFindCompany) || 
                                        (c.CompanyTown?.Contains(searchCompanyTownAsStudentToFindCompany, StringComparison.OrdinalIgnoreCase) == true));

                        if (!basicMatch) return false;
                    }

                    // Apply area matching (both global and specific searches)
                    var normalizedCompanyAreas = NormalizeAreas(c.CompanyAreas ?? "").ToList();
                    var expandedCompanyAreas = ExpandAreasWithSubfields(normalizedCompanyAreas);

                    var areaMatch = !normalizedSearchAreas.Any() ||
                        normalizedSearchAreas.Any(searchArea =>
                            expandedCompanyAreas.Any(companyArea =>
                                companyArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                                searchArea.Contains(companyArea, StringComparison.OrdinalIgnoreCase)));

                    // Apply activity matching (both global and specific searches)
                    var activityMatch = !selectedCompanyActivitiesAsStudent.Any() ||
                        selectedCompanyActivitiesAsStudent.Any(activity => 
                            c.CompanyActivity != null && c.CompanyActivity.Contains(activity, StringComparison.OrdinalIgnoreCase));

                    // Apply skills matching (both global and specific searches)
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
            searchResultsAsStudentToFindCompany = Enumerable.Empty<Company>();
            currentPage_CompanySearchAsStudent = 1;
            totalPages_CompanySearchAsStudent = 0;
            StateHasChanged();
        }

        private IEnumerable<Company> GetPaginatedCompanySearchResultsAsStudent()
        {
            return searchResultsAsStudentToFindCompany?
                .Skip((currentPage_CompanySearchAsStudent - 1) * CompanySearchPerPageAsStudent)
                .Take(CompanySearchPerPageAsStudent) ?? Enumerable.Empty<Company>();
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

        private void ShowCompanyDetailsWhenSearchAsStudent(Company company)
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
    }
}

