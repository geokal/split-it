using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Shared.ResearchGroup
{
    public partial class ResearchGroupCompanySearchSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;

        // Form Visibility
        private bool isRGSearchCompanyFormVisible = false;

        // Search Fields
        private string searchCompanyEmailAsRGToFindCompany = string.Empty;
        private string searchCompanyNameENGAsRGToFindCompany = string.Empty;
        private List<string> companyNameSuggestionsAsRG = new List<string>();
        private string searchCompanyTypeAsRGToFindCompany = string.Empty;
        private string searchCompanyActivityrAsRGToFindCompany = string.Empty;
        private string searchCompanyTownAsRGToFindCompany = string.Empty;
        private string searchCompanyAreasAsRGToFindCompany = string.Empty;
        private List<string> areasOfInterestSuggestions = new List<string>();
        private List<string> selectedAreasOfInterest = new List<string>();
        private string searchCompanyDesiredSkillsInputAsRGToFindCompany = string.Empty;
        private List<string> companyDesiredSkillsSuggestionsAsRG = new List<string>();
        private List<string> selectedCompanyDesiredSkillsAsRG = new List<string>();

        // Search Results
        private List<Company> searchResultsAsRGToFindCompany;

        // Pagination
        private int currentPage_CompanySearchAsRG = 1;
        private int CompanySearchPerPageAsRG = 10;

        // Company Details Modal
        private bool showCompanyDetailsModal = false;
        private Company selectedCompany;

        // Company Types
        private List<string> companyTypes = new List<string>
        {
            "Ανώνυμη Εταιρεία (Α.Ε.)",
            "Εταιρεία Περιορισμένης Ευθύνης (Ε.Π.Ε.)",
            "Ιδιωτική Κεφαλαιουχική Εταιρεία (Ι.Κ.Ε.)",
            "Ομόρρυθμη Εταιρεία (Ο.Ε.)",
            "Ετερόρρυθμη Εταιρεία (Ε.Ε.)",
            "Ατομική Επιχείρηση",
            "Κοινοπραξία",
            "Αστική Εταιρεία",
            "Συνεταιρισμός",
            "Κοινωφελής Επιχείρηση",
            "Δημόσια Επιχείρηση",
            "Μη Κυβερνητική Οργάνωση (Μ.Κ.Ο.)"
        };

        // Towns by Region
        private Dictionary<string, List<string>> townsByRegion = new Dictionary<string, List<string>>
        {
            {"Αττικής", new List<string> {"Αθήνα", "Πειραιάς", "Καλλιθέα", "Νίκαια", "Γλυφάδα", "Αμαρούσιο", "Χαλάνδρι", "Κηφισιά", "Νέα Σμύρνη", "Παλαιό Φάληρο"}},
            {"Θεσσαλονίκης", new List<string> {"Θεσσαλονίκη", "Καλαμαριά", "Εύοσμος", "Σταυρούπολη", "Νεάπολη", "Πυλαία", "Τριανδρία", "Σίνδος", "Περαία", "Μίκρα"}},
            {"Κεντρικής Μακεδονίας", new List<string> {"Σέρρες", "Βέροια", "Κατερίνη", "Νάουσα", "Γιαννιτσά", "Έδεσσα", "Κιλκίς", "Αλεξάνδρεια", "Πολύγυρος", "Κασσάνδρεια"}},
            {"Δυτικής Μακεδονίας", new List<string> {"Κοζάνη", "Πτολεμαΐδα", "Φλώρινα", "Καστοριά", "Γρεβενά", "Σιάτιστα", "Άργος Ορεστικό", "Νεάπολη", "Αμύνταιο", "Δεσκάτη"}},
            {"Ανατολικής Μακεδονίας και Θράκης", new List<string> {"Καβάλα", "Αλεξανδρούπολη", "Κομοτηνή", "Ξάνθη", "Δράμα", "Ορεστιάδα", "Χρυσούπολη", "Σουφλί", "Σάπες", "Ελευθερούπολη"}},
            {"Ηπείρου", new List<string> {"Ιωάννινα", "Άρτα", "Πρέβεζα", "Ηγουμενίτσα", "Φιλιάτες", "Κόνιτσα", "Μέτσοβο", "Παραμυθιά", "Δελβινάκι", "Ζίτσα"}},
            {"Θεσσαλίας", new List<string> {"Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα", "Φάρσαλα", "Ελασσόνα", "Αλμυρός", "Νέα Ιωνία", "Καλαμπάκα", "Σοφάδες"}},
            {"Στερεάς Ελλάδας", new List<string> {"Λαμία", "Χαλκίδα", "Λιβαδειά", "Θήβα", "Άμφισσα", "Καρπενήσι", "Ιστιαία", "Κύμη", "Αταλάντη", "Μαρτίνο"}},
            {"Δυτικής Ελλάδας", new List<string> {"Πάτρα", "Αγρίνιο", "Μεσολόγγι", "Πύργος", "Αίγιο", "Αμαλιάδα", "Ναύπακτος", "Ζαχάρω", "Κάτω Αχαΐα", "Λεχαινά"}},
            {"Πελοποννήσου", new List<string> {"Τρίπολη", "Καλαμάτα", "Κόρινθος", "Σπάρτη", "Ναύπλιο", "Άργος", "Κιάτο", "Γύθειο", "Μεγαλόπολη", "Λεωνίδιο"}},
            {"Ιονίων Νήσων", new List<string> {"Κέρκυρα", "Ζάκυνθος", "Αργοστόλι", "Λευκάδα", "Λιξούρι", "Βασιλική", "Περατάτα", "Σάμη", "Γαστούρι", "Μπενίτσες"}},
            {"Βορείου Αιγαίου", new List<string> {"Μυτιλήνη", "Χίος", "Σάμος", "Καρλόβασι", "Μόλυβος", "Πλωμάρι", "Καλλονή", "Πυθαγόρειο", "Αγιάσος", "Ερεσός"}},
            {"Νοτίου Αιγαίου", new List<string> {"Ρόδος", "Σύρος", "Κως", "Μύκονος", "Σαντορίνη", "Νάξος", "Πάρος", "Κάλυμνος", "Λέρος", "Κάρπαθος"}},
            {"Κρήτης", new List<string> {"Ηράκλειο", "Χανιά", "Ρέθυμνο", "Άγιος Νικόλαος", "Ιεράπετρα", "Σητεία", "Μοίρες", "Τυμπάκι", "Αρκαλοχώρι", "Νεάπολη"}}
        };

        // Computed Properties
        private IEnumerable<string> allTowns => townsByRegion.Values.SelectMany(t => t).Distinct().OrderBy(t => t);

        private int totalPages_CompanySearchAsRG =>
            (int)Math.Ceiling((double)(searchResultsAsRGToFindCompany?.Count ?? 0) / CompanySearchPerPageAsRG);

        // Visibility Toggle
        private void ToggleFormVisibilityForSearchCompanyAsRG()
        {
            isRGSearchCompanyFormVisible = !isRGSearchCompanyFormVisible;
            StateHasChanged();
        }

        // Search Input Handlers
        private void HandleCompanyInputAsRG(ChangeEventArgs e)
        {
            searchCompanyNameENGAsRGToFindCompany = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchCompanyNameENGAsRGToFindCompany) && searchCompanyNameENGAsRGToFindCompany.Length >= 2)
            {
                companyNameSuggestionsAsRG = dbContext.Companies
                    .Where(c => c.CompanyName.Contains(searchCompanyNameENGAsRGToFindCompany))
                    .Select(c => c.CompanyName)
                    .Distinct()
                    .ToList();
            }
            else
            {
                companyNameSuggestionsAsRG.Clear();
            }
        }

        private void SelectCompanyNameSuggestionAsRG(string suggestion)
        {
            searchCompanyNameENGAsRGToFindCompany = suggestion;
            companyNameSuggestionsAsRG.Clear();
        }

        private async Task HandleAreasOfInterestInput_WhenSearchForCompanyAsRG(ChangeEventArgs e)
        {
            searchCompanyAreasAsRGToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
            areasOfInterestSuggestions = new List<string>();

            if (searchCompanyAreasAsRGToFindCompany.Length >= 1)
            {
                try
                {
                    var allAreas = await dbContext.Areas
                        .Where(a => a.AreaName.Contains(searchCompanyAreasAsRGToFindCompany) ||
                                (a.AreaSubFields != null && a.AreaSubFields.Contains(searchCompanyAreasAsRGToFindCompany)))
                        .ToListAsync();

                    var suggestionsSet = new HashSet<string>();

                    foreach (var area in allAreas)
                    {
                        if (area.AreaName.Contains(searchCompanyAreasAsRGToFindCompany, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestionsSet.Add(area.AreaName);
                        }

                        if (!string.IsNullOrEmpty(area.AreaSubFields))
                        {
                            var subfields = area.AreaSubFields
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(sub => sub.Trim())
                                .Where(sub => !string.IsNullOrEmpty(sub) &&
                                            sub.Contains(searchCompanyAreasAsRGToFindCompany, StringComparison.OrdinalIgnoreCase));

                            foreach (var subfield in subfields)
                            {
                                suggestionsSet.Add($"{area.AreaName} - {subfield}");
                            }
                        }
                    }

                    areasOfInterestSuggestions = suggestionsSet.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    areasOfInterestSuggestions = new List<string>();
                }
            }

            StateHasChanged();
        }

        private void SelectAreasOfInterestSuggestion_WhenSearchForCompanyAsRG(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion))
            {
                selectedAreasOfInterest.Add(suggestion);
                searchCompanyAreasAsRGToFindCompany = suggestion;
                areasOfInterestSuggestions.Clear();
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaOfInterest_WhenSearchForCompanyAsRG(string area)
        {
            selectedAreasOfInterest.Remove(area);
            StateHasChanged();
        }

        private async Task HandleCompanyDesiredSkillsInputAsRG(ChangeEventArgs e)
        {
            searchCompanyDesiredSkillsInputAsRGToFindCompany = e.Value?.ToString().Trim() ?? string.Empty;
            companyDesiredSkillsSuggestionsAsRG = new List<string>();

            if (searchCompanyDesiredSkillsInputAsRGToFindCompany.Length >= 1)
            {
                try
                {
                    companyDesiredSkillsSuggestionsAsRG = await Task.Run(() =>
                        dbContext.Skills
                            .Where(s => s.SkillName.Contains(searchCompanyDesiredSkillsInputAsRGToFindCompany))
                            .Select(s => s.SkillName)
                            .Distinct()
                            .Take(10)
                            .ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    companyDesiredSkillsSuggestionsAsRG = new List<string>();
                }
            }

            StateHasChanged();
        }

        private void SelectCompanyDesiredSkillSuggestionAsRG(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedCompanyDesiredSkillsAsRG.Contains(suggestion))
            {
                selectedCompanyDesiredSkillsAsRG.Add(suggestion);
                companyDesiredSkillsSuggestionsAsRG.Clear();
                searchCompanyDesiredSkillsInputAsRGToFindCompany = string.Empty;
            }
        }

        private void RemoveSelectedCompanyDesiredSkillAsRG(string skill)
        {
            selectedCompanyDesiredSkillsAsRG.Remove(skill);
            StateHasChanged();
        }

        // Search Execution
        private void SearchCompaniesAsRG()
        {
            var combinedSearchAreas = new List<string>();

            if (selectedAreasOfInterest != null && selectedAreasOfInterest.Any())
                combinedSearchAreas.AddRange(selectedAreasOfInterest);

            if (!string.IsNullOrEmpty(searchCompanyAreasAsRGToFindCompany))
                combinedSearchAreas.Add(searchCompanyAreasAsRGToFindCompany);

            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            var companies = dbContext.Companies
                .AsEnumerable()
                .Where(c =>
                {
                    var normalizedCompanyAreas = NormalizeAreas(c.CompanyAreas).ToList();
                    var expandedCompanyAreas = ExpandAreasWithSubfields(normalizedCompanyAreas);

                    var areaMatch = !normalizedSearchAreas.Any() ||
                        normalizedSearchAreas.Any(searchArea =>
                            expandedCompanyAreas.Any(companyArea =>
                                companyArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                                searchArea.Contains(companyArea, StringComparison.OrdinalIgnoreCase)));

                    var basicMatch = (string.IsNullOrEmpty(searchCompanyEmailAsRGToFindCompany) || c.CompanyEmail.Contains(searchCompanyEmailAsRGToFindCompany)) &&
                                (string.IsNullOrEmpty(searchCompanyNameENGAsRGToFindCompany) || c.CompanyNameENG.Contains(searchCompanyNameENGAsRGToFindCompany)) &&
                                (string.IsNullOrEmpty(searchCompanyTypeAsRGToFindCompany) || c.CompanyType.Contains(searchCompanyTypeAsRGToFindCompany)) &&
                                (string.IsNullOrEmpty(searchCompanyActivityrAsRGToFindCompany) || c.CompanyActivity.Contains(searchCompanyActivityrAsRGToFindCompany)) &&
                                (string.IsNullOrEmpty(searchCompanyTownAsRGToFindCompany) || c.CompanyTown.Contains(searchCompanyTownAsRGToFindCompany));

                    var skillsMatch = !selectedCompanyDesiredSkillsAsRG.Any() ||
                        selectedCompanyDesiredSkillsAsRG.All(skill =>
                            c.CompanyDesiredSkills != null && c.CompanyDesiredSkills.Contains(skill));

                    return basicMatch && areaMatch && skillsMatch;
                })
                .ToList();

            searchResultsAsRGToFindCompany = companies;
            currentPage_CompanySearchAsRG = 1;
        }

        private void ClearSearchFieldsAsRGToFindCompany()
        {
            searchCompanyEmailAsRGToFindCompany = string.Empty;
            searchCompanyNameENGAsRGToFindCompany = string.Empty;
            searchCompanyTypeAsRGToFindCompany = string.Empty;
            searchCompanyActivityrAsRGToFindCompany = string.Empty;
            searchCompanyTownAsRGToFindCompany = string.Empty;
            searchCompanyAreasAsRGToFindCompany = string.Empty;
            searchCompanyDesiredSkillsInputAsRGToFindCompany = string.Empty;
            selectedAreasOfInterest.Clear();
            selectedCompanyDesiredSkillsAsRG.Clear();
            areasOfInterestSuggestions.Clear();
            companyDesiredSkillsSuggestionsAsRG.Clear();
            searchResultsAsRGToFindCompany = null;
            StateHasChanged();
        }

        // Pagination
        private IEnumerable<Company> GetPaginatedCompanySearchResultsAsRG()
        {
            return searchResultsAsRGToFindCompany?
                .Skip((currentPage_CompanySearchAsRG - 1) * CompanySearchPerPageAsRG)
                .Take(CompanySearchPerPageAsRG) ?? Enumerable.Empty<Company>();
        }

        private void OnPageSizeChangeForCompanySearchAsRG(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                CompanySearchPerPageAsRG = newSize;
                currentPage_CompanySearchAsRG = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstPage_CompanySearchAsRG()
        {
            currentPage_CompanySearchAsRG = 1;
            StateHasChanged();
        }

        private void PreviousPage_CompanySearchAsRG()
        {
            if (currentPage_CompanySearchAsRG > 1)
            {
                currentPage_CompanySearchAsRG--;
                StateHasChanged();
            }
        }

        private void NextPage_CompanySearchAsRG()
        {
            if (currentPage_CompanySearchAsRG < totalPages_CompanySearchAsRG)
            {
                currentPage_CompanySearchAsRG++;
                StateHasChanged();
            }
        }

        private void GoToLastPage_CompanySearchAsRG()
        {
            currentPage_CompanySearchAsRG = totalPages_CompanySearchAsRG;
            StateHasChanged();
        }

        private void GoToPage_CompanySearchAsRG(int page)
        {
            if (page > 0 && page <= totalPages_CompanySearchAsRG)
            {
                currentPage_CompanySearchAsRG = page;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePages_CompanySearchAsRG()
        {
            var pages = new List<int>();
            int current = currentPage_CompanySearchAsRG;
            int total = totalPages_CompanySearchAsRG;

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

        // Company Details Modal
        private void ShowCompanyDetailsWhenSearchAsRG(Company company)
        {
            selectedCompany = company;
            showCompanyDetailsModal = true;
        }

        private void CloseCompanyDetailsModalWhenSearchAsProfessor()
        {
            showCompanyDetailsModal = false;
            selectedCompany = null;
        }

        // Helper Methods
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
                var dbArea = dbContext.Areas.FirstOrDefault(a => a.AreaName.ToLower() == area.ToLower());
                if (dbArea != null && !string.IsNullOrEmpty(dbArea.AreaSubFields))
                {
                    var subfields = dbArea.AreaSubFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToLowerInvariant());
                    foreach (var sub in subfields)
                        expanded.Add(sub);
                }
            }
            return expanded;
        }
    }
}

