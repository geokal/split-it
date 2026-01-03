using Microsoft.AspNetCore.Components;
using QuizManager.Models;
using QuizManager.Services.ResearchGroupDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ResearchGroupSections
{
    public partial class ResearchGroupProfessorSearchSection : ComponentBase
    {
        [Inject] private IResearchGroupDashboardService ResearchGroupDashboardService { get; set; } = default!;

        // Form Visibility
        private bool isRGSearchProfessorVisible = false;
        private ResearchGroupDashboardLookups lookups = ResearchGroupDashboardLookups.Empty;

        // Search Fields
        private string searchNameSurnameAsRGToFindProfessor = "";
        private List<string> professorNameSurnameSuggestionsAsRG = new List<string>();
        private string searchSchoolAsRGToFindProfessor = "";
        private string searchDepartmentAsRGToFindProfessor = "";
        private string searchAreasOfInterestAsRGToFindProfessor = "";
        private List<string> areasOfInterestSuggestionsAsRG = new List<string>();
        private List<string> selectedAreasOfInterestAsRG = new List<string>();

        // Search Results
        private List<QuizManager.Models.Professor> searchResultsAsRGToFindProfessor;

        // Pagination
        private int ProfessorsPerPage_SearchForProfessorsAsRG = 10;
        private int currentProfessorPage_SearchForProfessorsAsRG = 1;
        private int[] pageSizeOptions_SearchForProfessorsAsRG = new[] { 10, 50, 100 };

        // Professor Details Modal
        private bool showProfessorDetailsModalWhenSearchForProfessorsAsRG = false;
        private QuizManager.Models.Professor selectedProfessorWhenSearchForProfessorsAsRG;

        // University Departments Data
        private Dictionary<string, List<string>> universityDepartments = new Dictionary<string, List<string>>
        {
            {"Πολυτεχνική Σχολή", new List<string> {"Τμήμα Ηλεκτρολόγων Μηχανικών και Μηχανικών Υπολογιστών", "Τμήμα Μηχανολόγων Μηχανικών", "Τμήμα Χημικών Μηχανικών", "Τμήμα Πολιτικών Μηχανικών", "Τμήμα Αρχιτεκτόνων Μηχανικών", "Τμήμα Αγρονόμων και Τοπογράφων Μηχανικών"}},
            {"Σχολή Θετικών Επιστημών", new List<string> {"Τμήμα Μαθηματικών", "Τμήμα Φυσικής", "Τμήμα Χημείας", "Τμήμα Βιολογίας", "Τμήμα Γεωλογίας", "Τμήμα Πληροφορικής"}},
            {"Σχολή Επιστημών Υγείας", new List<string> {"Τμήμα Ιατρικής", "Τμήμα Οδοντιατρικής", "Τμήμα Φαρμακευτικής", "Τμήμα Κτηνιατρικής"}},
            {"Σχολή Γεωπονικών Επιστημών", new List<string> {"Τμήμα Γεωπονίας", "Τμήμα Δασολογίας και Φυσικού Περιβάλλοντος"}},
            {"Σχολή Οικονομικών και Πολιτικών Επιστημών", new List<string> {"Τμήμα Οικονομικών Επιστημών", "Τμήμα Πολιτικών Επιστημών", "Τμήμα Δημοσιογραφίας και ΜΜΕ"}},
            {"Σχολή Νομικής", new List<string> {"Τμήμα Νομικής"}},
            {"Φιλοσοφική Σχολή", new List<string> {"Τμήμα Φιλολογίας", "Τμήμα Ιστορίας και Αρχαιολογίας", "Τμήμα Φιλοσοφίας και Παιδαγωγικής", "Τμήμα Ψυχολογίας", "Τμήμα Αγγλικής Γλώσσας και Φιλολογίας", "Τμήμα Γαλλικής Γλώσσας και Φιλολογίας", "Τμήμα Γερμανικής Γλώσσας και Φιλολογίας", "Τμήμα Ιταλικής Γλώσσας και Φιλολογίας"}},
            {"Παιδαγωγική Σχολή", new List<string> {"Τμήμα Δημοτικής Εκπαίδευσης", "Τμήμα Προσχολικής Αγωγής και Εκπαίδευσης", "Τμήμα Επιστημών Προσχολικής Αγωγής και Εκπαίδευσης"}},
            {"Σχολή Καλών Τεχνών", new List<string> {"Τμήμα Εικαστικών και Εφαρμοσμένων Τεχνών", "Τμήμα Μουσικών Σπουδών", "Τμήμα Θεάτρου", "Τμήμα Κινηματογράφου"}},
            {"Σχολή Επιστήμης Φυσικής Αγωγής και Αθλητισμού", new List<string> {"Τμήμα Επιστήμης Φυσικής Αγωγής και Αθλητισμού (Θεσσαλονίκη)", "Τμήμα Επιστήμης Φυσικής Αγωγής και Αθλητισμού (Σέρρες)"}}
        };

        // Computed Properties
        private IEnumerable<string> researchGroupSchools => universityDepartments.Keys;
        private List<string> filteredProfessorDepartments => !string.IsNullOrEmpty(searchSchoolAsRGToFindProfessor) && universityDepartments.ContainsKey(searchSchoolAsRGToFindProfessor)
            ? universityDepartments[searchSchoolAsRGToFindProfessor]
            : new List<string>();
        private List<string> filteredProfessorDepartmentsAsRG => 
            string.IsNullOrEmpty(searchSchoolAsRGToFindProfessor) 
                ? GetAllProfessorDepartments() 
                : universityDepartments.ContainsKey(searchSchoolAsRGToFindProfessor)
                    ? universityDepartments[searchSchoolAsRGToFindProfessor]
                    : new List<string>();

        private int totalProfessorPages_SearchForProfessorsAsRG => searchResultsAsRGToFindProfessor != null
            ? (int)Math.Ceiling((double)searchResultsAsRGToFindProfessor.Count / ProfessorsPerPage_SearchForProfessorsAsRG)
            : 1;

        protected override async Task OnInitializedAsync()
        {
            lookups = await ResearchGroupDashboardService.GetLookupsAsync();
        }

        // Visibility Toggle
        private void ToggleRGSearchProfessorVisible()
        {
            isRGSearchProfessorVisible = !isRGSearchProfessorVisible;
            StateHasChanged();
        }

        // Search Input Handlers
        private async Task HandleProfessorInputWhenSearchForProfessorAsRG(ChangeEventArgs e)
        {
            searchNameSurnameAsRGToFindProfessor = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchNameSurnameAsRGToFindProfessor) && searchNameSurnameAsRGToFindProfessor.Length >= 2)
            {
                var matches = await ResearchGroupDashboardService.SearchProfessorsAsync(searchNameSurnameAsRGToFindProfessor);

                professorNameSurnameSuggestionsAsRG = matches
                    .Select(p => $"{p.ProfName} {p.ProfSurname}".Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .Take(10)
                    .ToList();
            }
            else
            {
                professorNameSurnameSuggestionsAsRG.Clear();
            }
        }

        private void SelectProfessorNameSurnameSuggestionAsRG(string suggestion)
        {
            searchNameSurnameAsRGToFindProfessor = suggestion;
            professorNameSurnameSuggestionsAsRG.Clear();
        }

        private async Task HandleAreasOfInterestInputAsRG(ChangeEventArgs e)
        {
            searchAreasOfInterestAsRGToFindProfessor = e.Value?.ToString().Trim() ?? string.Empty;
            areasOfInterestSuggestionsAsRG = new List<string>();

            if (searchAreasOfInterestAsRGToFindProfessor.Length >= 1)
            {
                try
                {
                    var allAreas = lookups.Areas.Where(a =>
                            a.AreaName.Contains(searchAreasOfInterestAsRGToFindProfessor, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(a.AreaSubFields) && a.AreaSubFields.Contains(searchAreasOfInterestAsRGToFindProfessor, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var suggestionsSet = new HashSet<string>();

                    foreach (var area in allAreas)
                    {
                        if (area.AreaName.Contains(searchAreasOfInterestAsRGToFindProfessor, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestionsSet.Add(area.AreaName);
                        }

                        if (!string.IsNullOrEmpty(area.AreaSubFields))
                        {
                            var subfields = area.AreaSubFields
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(sub => sub.Trim())
                                .Where(sub => !string.IsNullOrEmpty(sub) &&
                                            sub.Contains(searchAreasOfInterestAsRGToFindProfessor, StringComparison.OrdinalIgnoreCase));

                            foreach (var subfield in subfields)
                            {
                                suggestionsSet.Add($"{area.AreaName} - {subfield}");
                            }
                        }
                    }

                    areasOfInterestSuggestionsAsRG = suggestionsSet.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    areasOfInterestSuggestionsAsRG = new List<string>();
                }
            }

            StateHasChanged();
        }

        private void SelectAreasOfInterestSuggestionAsRG(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasOfInterestAsRG.Contains(suggestion))
            {
                selectedAreasOfInterestAsRG.Add(suggestion);
                areasOfInterestSuggestionsAsRG.Clear();
                searchAreasOfInterestAsRGToFindProfessor = string.Empty;
            }
        }

        private void RemoveSelectedAreaOfInterestAsRG(string area)
        {
            selectedAreasOfInterestAsRG.Remove(area);
            StateHasChanged();
        }

        // Search Execution
        private async Task SearchProfessorsAsRGToFindProfessor()
        {
            var combinedSearchAreas = new List<string>();

            if (selectedAreasOfInterestAsRG != null && selectedAreasOfInterestAsRG.Any())
                combinedSearchAreas.AddRange(selectedAreasOfInterestAsRG);

            if (!string.IsNullOrEmpty(searchAreasOfInterestAsRGToFindProfessor))
                combinedSearchAreas.Add(searchAreasOfInterestAsRGToFindProfessor);

            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            var request = new ResearchGroupProfessorSearchRequest
            {
                NameOrSurname = string.IsNullOrWhiteSpace(searchNameSurnameAsRGToFindProfessor) ? null : searchNameSurnameAsRGToFindProfessor,
                School = string.IsNullOrWhiteSpace(searchSchoolAsRGToFindProfessor) ? null : searchSchoolAsRGToFindProfessor,
                Department = string.IsNullOrWhiteSpace(searchDepartmentAsRGToFindProfessor) ? null : searchDepartmentAsRGToFindProfessor,
                Areas = normalizedSearchAreas
            };

            var professors = await ResearchGroupDashboardService.FilterProfessorsAsync(request);

            var filtered = professors.AsEnumerable();
            if (normalizedSearchAreas.Any())
            {
                filtered = filtered.Where(p =>
                {
                    var normalizedProfessorAreas = NormalizeAreas(p.ProfGeneralFieldOfWork).ToList();
                    var expandedProfessorAreas = ExpandAreasWithSubfields(normalizedProfessorAreas);

                    return normalizedSearchAreas.Any(searchArea =>
                        expandedProfessorAreas.Any(professorArea =>
                            professorArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                            searchArea.Contains(professorArea, StringComparison.OrdinalIgnoreCase)));
                });
            }

            searchResultsAsRGToFindProfessor = filtered.ToList();
            currentProfessorPage_SearchForProfessorsAsRG = 1;
        }

        private void ClearSearchFieldsAsRGToFindProfessor()
        {
            searchNameSurnameAsRGToFindProfessor = string.Empty;
            searchSchoolAsRGToFindProfessor = string.Empty;
            searchDepartmentAsRGToFindProfessor = string.Empty;
            searchAreasOfInterestAsRGToFindProfessor = string.Empty;
            searchResultsAsRGToFindProfessor = null;
            areasOfInterestSuggestionsAsRG.Clear();
            selectedAreasOfInterestAsRG.Clear();
            StateHasChanged();
        }

        // Pagination
        private IEnumerable<QuizManager.Models.Professor> GetPaginatedProfessorResultsAsRG()
        {
            return searchResultsAsRGToFindProfessor?
                .Skip((currentProfessorPage_SearchForProfessorsAsRG - 1) * ProfessorsPerPage_SearchForProfessorsAsRG)
                .Take(ProfessorsPerPage_SearchForProfessorsAsRG)
                ?? Enumerable.Empty<QuizManager.Models.Professor>();
        }

        private void OnPageSizeChange_SearchForProfessorsAsRG(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                ProfessorsPerPage_SearchForProfessorsAsRG = newSize;
                currentProfessorPage_SearchForProfessorsAsRG = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstProfessorPageAsRG()
        {
            currentProfessorPage_SearchForProfessorsAsRG = 1;
            StateHasChanged();
        }

        private void PreviousProfessorPageAsRG()
        {
            if (currentProfessorPage_SearchForProfessorsAsRG > 1)
            {
                currentProfessorPage_SearchForProfessorsAsRG--;
                StateHasChanged();
            }
        }

        private void NextProfessorPageAsRG()
        {
            if (currentProfessorPage_SearchForProfessorsAsRG < totalProfessorPages_SearchForProfessorsAsRG)
            {
                currentProfessorPage_SearchForProfessorsAsRG++;
                StateHasChanged();
            }
        }

        private void GoToLastProfessorPageAsRG()
        {
            currentProfessorPage_SearchForProfessorsAsRG = totalProfessorPages_SearchForProfessorsAsRG;
            StateHasChanged();
        }

        private void GoToProfessorPageAsRG(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalProfessorPages_SearchForProfessorsAsRG)
            {
                currentProfessorPage_SearchForProfessorsAsRG = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisibleProfessorPagesAsRG()
        {
            var pages = new List<int>();
            int current = currentProfessorPage_SearchForProfessorsAsRG;
            int total = totalProfessorPages_SearchForProfessorsAsRG;

            if (total == 0) return pages;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

            return pages;
        }

        // Professor Details Modal
        private void ShowProfessorDetailsOnEyeIconWhenSearchForProfessorAsRG(QuizManager.Models.Professor professor)
        {
            selectedProfessorWhenSearchForProfessorsAsRG = professor;
            showProfessorDetailsModalWhenSearchForProfessorsAsRG = true;
        }

        private void CloseModalProfessorDetailsOnEyeIconWhenSearchForProfessorsAsRG()
        {
            showProfessorDetailsModalWhenSearchForProfessorsAsRG = false;
            selectedProfessorWhenSearchForProfessorsAsRG = null;
        }

        // Helper Methods
        private List<string> GetAllProfessorDepartments()
        {
            return universityDepartments.Values.SelectMany(depts => depts).Distinct().ToList();
        }

        private async Task OnProfessorSchoolChangedAsRG(ChangeEventArgs e)
        {
            searchSchoolAsRGToFindProfessor = e.Value?.ToString() ?? "";
            // Clear department selection when school changes
            searchDepartmentAsRGToFindProfessor = "";
            await InvokeAsync(StateHasChanged);
        }

        private IEnumerable<string> NormalizeAreas(string areas)
        {
            if (string.IsNullOrWhiteSpace(areas))
                return Enumerable.Empty<string>();

            return areas.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(a => a.Trim().ToLowerInvariant())
                       .Where(a => !string.IsNullOrEmpty(a));
        }

        private HashSet<string> ExpandAreasWithSubfields(IEnumerable<string> areas)
        {
            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var area in areas)
            {
                expanded.Add(area);
                var lookupArea = lookups.Areas.FirstOrDefault(a => a.AreaName.Equals(area, StringComparison.OrdinalIgnoreCase));
                if (lookupArea != null && !string.IsNullOrEmpty(lookupArea.AreaSubFields))
                {
                    var subfields = lookupArea.AreaSubFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToLowerInvariant());
                    foreach (var sub in subfields)
                        expanded.Add(sub);
                }
            }
            return expanded;
        }
    }
}
