using Microsoft.AspNetCore.Components;
using QuizManager.Models;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyProfessorSearchSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;

        // Form Visibility
        private bool isCompanySearchProfessorVisible = false;

        // Search Fields
        private string searchNameSurnameAsCompanyToFindProfessor = "";
        private List<string> professorNameSurnameSuggestions = new List<string>();
        private string searchSchoolAsCompanyToFindProfessor = "";
        private string searchDepartmentAsCompanyToFindProfessor = "";
        private string searchAreasOfInterestAsCompanyToFindProfessor = "";
        private List<string> areasOfInterestSuggestions = new List<string>();
        private List<string> selectedAreasOfInterest = new List<string>();

        // Search Results and Pagination
        private List<QuizManager.Models.Professor> searchResultsAsCompanyToFindProfessor = new List<QuizManager.Models.Professor>();
        private int ProfessorsPerPage_SearchForProfessorsAsStudent = 10;
        private int[] pageSizeOptions_SearchForProfessorsAsStudent = new[] { 10, 50, 100 };
        private int currentProfessorPage_SearchForProfessorsAsStudent = 1;

        // Professor Details Modal
        private bool showProfessorDetailsModalWhenSearchForProfessorsAsCompany = false;
        private QuizManager.Models.Professor selectedProfessorWhenSearchForProfessorsAsCompany;

        // University Departments Dictionary
        private Dictionary<string, List<string>> universityDepartments = new()
        {
            ["ΑΓΡΟΤΙΚΗΣ ΑΝΑΠΤΥΞΗΣ, ΔΙΑΤΡΟΦΗΣ ΚΑΙ ΑΕΙΦΟΡΙΑΣ"] = new List<string>
            {
                "ΤΜΗΜΑ ΑΓΡΟΤΙΚΗΣ ΑΝΑΠΤΥΞΗΣ, ΑΓΡΟΔΙΑΤΡΟΦΗΣ ΚΑΙ ΔΙΑΧΕΙΡΙΣΗΣ ΦΥΣΙΚΩΝ ΠΟΡΩΝ"
            },
            ["ΕΠΙΣΤΗΜΩΝ ΑΓΩΓΗΣ"] = new List<string>
            {
                "ΠΑΙΔΑΓΩΓΙΚΟ ΤΜΗΜΑ ΔΗΜΟΤΙΚΗΣ ΕΚΠΑΙΔΕΥΣΗΣ",
                "ΤΜΗΜΑ ΕΚΠΑΙΔΕΥΣΗΣ ΚΑΙ ΑΓΩΓΗΣ ΣΤΗΝ ΠΡΟΣΧΟΛΙΚΗ ΗΛΙΚΙΑ"
            },
            ["ΕΠΙΣΤΗΜΩΝ ΥΓΕΙΑΣ"] = new List<string>
            {
                "ΤΜΗΜΑ ΙΑΤΡΙΚΗΣ",
                "ΤΜΗΜΑ ΝΟΣΗΛΕΥΤΙΚΗΣ",
                "ΤΜΗΜΑ ΟΔΟΝΤΙΑΤΡΙΚΗΣ",
                "ΤΜΗΜΑ ΦΑΡΜΑΚΕΥΤΙΚΗΣ"
            },
            ["ΕΠΙΣΤΗΜΗΣ ΦΥΣΙΚΗΣ ΑΓΩΓΗΣ ΚΑΙ ΑΘΛΗΤΙΣΜΟΥ"] = new List<string>
            {
                "ΤΜΗΜΑ ΕΠΙΣΤΗΜΗΣ ΦΥΣΙΚΗΣ ΑΓΩΓΗΣ ΚΑΙ ΑΘΛΗΤΙΣΜΟΥ"
            },
            ["ΘΕΟΛΟΓΙΚΗ"] = new List<string>
            {
                "ΤΜΗΜΑ ΘΕΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΚΟΙΝΩΝΙΚΗΣ ΘΕΟΛΟΓΙΑΣ ΚΑΙ ΘΡΗΣΚΕΙΟΛΟΓΙΑΣ"
            },
            ["ΘΕΤΙΚΩΝ ΕΠΙΣΤΗΜΩΝ"] = new List<string>
            {
                "ΤΜΗΜΑ ΑΕΡΟΔΙΑΣΤΗΜΙΚΗΣ ΕΠΙΣΤΗΜΗΣ ΚΑΙ ΤΕΧΝΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΒΙΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΓΕΩΛΟΓΙΑΣ ΚΑΙ ΓΕΩΠΕΡΙΒΑΛΛΟΝΤΟΣ",
                "ΤΜΗΜΑ ΙΣΤΟΡΙΑΣ ΚΑΙ ΦΙΛΟΣΟΦΙΑΣ ΤΗΣ ΕΠΙΣΤΗΜΗΣ",
                "ΤΜΗΜΑ ΜΑΘΗΜΑΤΙΚΩΝ",
                "ΤΜΗΜΑ ΠΛΗΡΟΦΟΡΙΚΗΣ ΚΑΙ ΤΗΛΕΠΙΚΟΙΝΩΝΙΩΝ",
                "ΤΜΗΜΑ ΤΕΧΝΟΛΟΓΙΩΝ ΨΗΦΙΑΚΗΣ ΒΙΟΜΗΧΑΝΙΑΣ",
                "ΤΜΗΜΑ ΦΥΣΙΚΗΣ",
                "ΤΜΗΜΑ ΧΗΜΕΙΑΣ"
            },
            ["ΝΟΜΙΚΗ"] = new List<string>
            {
                "ΝΟΜΙΚΗ ΣΧΟΛΗ"
            },
            ["ΟΙΚΟΝΟΜΙΚΩΝ ΚΑΙ ΠΟΛΙΤΙΚΩΝ ΕΠΙΣΤΗΜΩΝ"] = new List<string>
            {
                "ΤΜΗΜΑ ΔΙΑΧΕΙΡΙΣΗΣ ΛΙΜΕΝΩΝ ΚΑΙ ΝΑΥΤΙΛΙΑΣ",
                "ΤΜΗΜΑ ΕΠΙΚΟΙΝΩΝΙΑΣ ΚΑΙ ΜΕΣΩΝ ΜΑΖΙΚΗΣ ΕΝΗΜΕΡΩΣΗΣ",
                "ΤΜΗΜΑ ΟΙΚΟΝΟΜΙΚΩΝ ΕΠΙΣΤΗΜΩΝ",
                "ΤΜΗΜΑ ΠΟΛΙΤΙΚΗΣ ΕΠΙΣΤΗΜΗΣ ΚΑΙ ΔΗΜΟΣΙΑΣ ΔΙΟΙΚΗΣΗΣ",
                "ΤΜΗΜΑ ΤΟΥΡΚΙΚΩΝ ΣΠΟΥΔΩΝ ΚΑΙ ΣΥΓΧΡΟΝΩΝ ΑΣΙΑΤΙΚΩΝ ΣΠΟΥΔΩΝ",
                "ΤΜΗΜΑ ΔΙΟΙΚΗΣΗΣ ΕΠΙΧΕΙΡΗΣΕΩΝ ΚΑΙ ΟΡΓΑΝΙΣΜΩΝ",
                "ΤΜΗΜΑ ΚΟΙΝΩΝΙΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΨΗΦΙΑΚΩΝ ΤΕΧΝΩΝ ΚΑΙ ΚΙΝΗΜΑΤΟΓΡΑΦΟΥ"
            },
            ["ΦΙΛΟΣΟΦΙΚΗ"] = new List<string>
            {
                "ΠΑΙΔΑΓΩΓΙΚΟ ΤΜΗΜΑ ΔΕΥΤΕΡΟΒΑΘΜΙΑΣ ΕΚΠΑΙΔΕΥΣΗΣ",
                "ΤΜΗΜΑ ΑΓΓΛΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΓΑΛΛΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΓΕΡΜΑΝΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΘΕΑΤΡΙΚΩΝ ΣΠΟΥΔΩΝ",
                "ΤΜΗΜΑ ΙΣΠΑΝΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΙΣΤΟΡΙΑΣ ΚΑΙ ΑΡΧΑΙΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΙΤΑΛΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΜΟΥΣΙΚΩΝ ΣΠΟΥΔΩΝ",
                "ΤΜΗΜΑ ΡΩΣΙΚΗΣ ΓΛΩΣΣΑΣ ΚΑΙ ΦΙΛΟΛΟΓΙΑΣ ΚΑΙ ΣΛΑΒΙΚΩΝ ΣΠΟΥΔΩΝ",
                "ΤΜΗΜΑ ΦΙΛΟΛΟΓΙΑΣ",
                "ΤΜΗΜΑ ΦΙΛΟΣΟΦΙΑΣ",
                "ΤΜΗΜΑ ΨΥΧΟΛΟΓΙΑΣ"
            }
        };

        // Computed Properties
        private List<string> researchGroupSchools => universityDepartments.Keys.ToList();

        private List<string> filteredProfessorDepartments =>
            string.IsNullOrEmpty(searchSchoolAsCompanyToFindProfessor)
                ? GetAllProfessorDepartments()
                : universityDepartments.ContainsKey(searchSchoolAsCompanyToFindProfessor)
                    ? universityDepartments[searchSchoolAsCompanyToFindProfessor]
                    : new List<string>();

        private int totalProfessorPages_SearchForProfessorsAsStudent =>
            searchResultsAsCompanyToFindProfessor != null && searchResultsAsCompanyToFindProfessor.Any()
                ? (int)Math.Ceiling((double)searchResultsAsCompanyToFindProfessor.Count / ProfessorsPerPage_SearchForProfessorsAsStudent)
                : 0;

        // Visibility Toggle
        private async Task ToggleCompanySearchProfessorVisible()
        {
            isCompanySearchProfessorVisible = !isCompanySearchProfessorVisible;
            StateHasChanged();
        }

        // Search Field Handlers
        private async Task HandleProfessorInput(ChangeEventArgs e)
        {
            searchNameSurnameAsCompanyToFindProfessor = e.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(searchNameSurnameAsCompanyToFindProfessor) && searchNameSurnameAsCompanyToFindProfessor.Length >= 2)
            {
                var results = await CompanyDashboardService.SearchProfessorsAsync(new ProfessorSearchFilter
                {
                    Name = searchNameSurnameAsCompanyToFindProfessor,
                    MaxResults = 25
                });

                professorNameSurnameSuggestions = results
                    .Select(p => $"{p.ProfName} {p.ProfSurname}".Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                professorNameSurnameSuggestions.Clear();
            }
            StateHasChanged();
        }

        private void SelectProfessorNameSurnameSuggestion(string suggestion)
        {
            searchNameSurnameAsCompanyToFindProfessor = suggestion;
            professorNameSurnameSuggestions.Clear();
            StateHasChanged();
        }

        private void HandleProfessorSchoolChanged(ChangeEventArgs e)
        {
            searchSchoolAsCompanyToFindProfessor = e.Value?.ToString();
            searchDepartmentAsCompanyToFindProfessor = ""; // Clear department when school changes
            StateHasChanged();
        }

        private List<string> GetAllProfessorDepartments()
        {
            return universityDepartments.Values.SelectMany(depts => depts).Distinct().ToList();
        }

        // Areas of Interest Handlers
        private async Task HandleAreasOfInterestInput(ChangeEventArgs e)
        {
            searchAreasOfInterestAsCompanyToFindProfessor = e.Value?.ToString().Trim() ?? string.Empty;
            areasOfInterestSuggestions = new List<string>();

            if (searchAreasOfInterestAsCompanyToFindProfessor.Length >= 1)
            {
                try
                {
                    var suggestions = await CompanyDashboardService.SearchAreasAsync(searchAreasOfInterestAsCompanyToFindProfessor);
                    areasOfInterestSuggestions = suggestions.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching areas: {ex.Message}");
                    areasOfInterestSuggestions = new List<string>();
                }
            }
            else
            {
                areasOfInterestSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectAreasOfInterestSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasOfInterest.Contains(suggestion))
            {
                selectedAreasOfInterest.Add(suggestion);
                areasOfInterestSuggestions.Clear();
                searchAreasOfInterestAsCompanyToFindProfessor = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaOfInterest(string area)
        {
            selectedAreasOfInterest.Remove(area);
            StateHasChanged();
        }

        // Clear Search Fields
        private void ClearSearchFieldsAsCompanyToFindProfessor()
        {
            searchNameSurnameAsCompanyToFindProfessor = string.Empty;
            searchSchoolAsCompanyToFindProfessor = string.Empty;
            searchDepartmentAsCompanyToFindProfessor = string.Empty;
            searchAreasOfInterestAsCompanyToFindProfessor = string.Empty;
            searchResultsAsCompanyToFindProfessor = new List<QuizManager.Models.Professor>();
            areasOfInterestSuggestions.Clear();
            selectedAreasOfInterest.Clear();
            professorNameSurnameSuggestions.Clear();
            StateHasChanged();
        }

        // Search Method
        private async Task SearchProfessorsAsCompanyToFindProfessor()
        {
            var filter = new ProfessorSearchFilter
            {
                Name = searchNameSurnameAsCompanyToFindProfessor,
                Department = string.IsNullOrWhiteSpace(searchDepartmentAsCompanyToFindProfessor) ? null : searchDepartmentAsCompanyToFindProfessor,
                School = string.IsNullOrWhiteSpace(searchSchoolAsCompanyToFindProfessor) ? null : searchSchoolAsCompanyToFindProfessor,
                AreasOfInterest = BuildAreasOfInterestFilter(),
                MaxResults = 200
            };

            var results = await CompanyDashboardService.SearchProfessorsAsync(filter);
            searchResultsAsCompanyToFindProfessor = results.ToList();

            currentProfessorPage_SearchForProfessorsAsStudent = 1;
            StateHasChanged();
        }

        private string? BuildAreasOfInterestFilter()
        {
            var areas = selectedAreasOfInterest.ToList();
            if (!string.IsNullOrWhiteSpace(searchAreasOfInterestAsCompanyToFindProfessor))
            {
                areas.Add(searchAreasOfInterestAsCompanyToFindProfessor);
            }

            return areas.Any() ? string.Join(", ", areas) : null;
        }

        // Pagination Methods
        private IEnumerable<QuizManager.Models.Professor> GetPaginatedProfessorResults()
        {
            return searchResultsAsCompanyToFindProfessor?
                .Skip((currentProfessorPage_SearchForProfessorsAsStudent - 1) * ProfessorsPerPage_SearchForProfessorsAsStudent)
                .Take(ProfessorsPerPage_SearchForProfessorsAsStudent)
                ?? Enumerable.Empty<QuizManager.Models.Professor>();
        }

        private void OnPageSizeChange_SearchForProfessorsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                ProfessorsPerPage_SearchForProfessorsAsStudent = newSize;
                currentProfessorPage_SearchForProfessorsAsStudent = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstProfessorPage()
        {
            currentProfessorPage_SearchForProfessorsAsStudent = 1;
            StateHasChanged();
        }

        private void PreviousProfessorPage()
        {
            if (currentProfessorPage_SearchForProfessorsAsStudent > 1)
            {
                currentProfessorPage_SearchForProfessorsAsStudent--;
                StateHasChanged();
            }
        }

        private void NextProfessorPage()
        {
            if (currentProfessorPage_SearchForProfessorsAsStudent < totalProfessorPages_SearchForProfessorsAsStudent)
            {
                currentProfessorPage_SearchForProfessorsAsStudent++;
                StateHasChanged();
            }
        }

        private void GoToLastProfessorPage()
        {
            currentProfessorPage_SearchForProfessorsAsStudent = totalProfessorPages_SearchForProfessorsAsStudent;
            StateHasChanged();
        }

        private void GoToProfessorPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalProfessorPages_SearchForProfessorsAsStudent)
            {
                currentProfessorPage_SearchForProfessorsAsStudent = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisibleProfessorPages()
        {
            var pages = new List<int>();
            int currentPage = currentProfessorPage_SearchForProfessorsAsStudent;
            int totalPages = totalProfessorPages_SearchForProfessorsAsStudent;

            if (totalPages == 0) return pages;

            pages.Add(1);

            if (currentPage > 3) pages.Add(-1);

            int start = Math.Max(2, currentPage - 1);
            int end = Math.Min(totalPages - 1, currentPage + 1);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (currentPage < totalPages - 2) pages.Add(-1);

            if (totalPages > 1) pages.Add(totalPages);

            return pages;
        }

        // Modal Methods
        private void ShowProfessorDetailsOnEyeIconWhenSearchForProfessorAsCompany(QuizManager.Models.Professor professor)
        {
            selectedProfessorWhenSearchForProfessorsAsCompany = professor;
            showProfessorDetailsModalWhenSearchForProfessorsAsCompany = true;
            StateHasChanged();
        }

        private void CloseModalProfessorDetailsOnEyeIconWhenSearchForProfessorsAsCompany()
        {
            showProfessorDetailsModalWhenSearchForProfessorsAsCompany = false;
            selectedProfessorWhenSearchForProfessorsAsCompany = null;
            StateHasChanged();
        }

        // Helper Methods
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
                expandedAreas.Add(area);

                if (area.Contains('/'))
                {
                    var parts = area.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                .Select(part => part.Trim().ToLower())
                                .Where(part => !string.IsNullOrEmpty(part))
                                .ToList();

                    if (parts.Count >= 2)
                    {
                        expandedAreas.Add(parts[0]);
                        expandedAreas.Add(parts[1]);
                        expandedAreas.AddRange(parts);
                    }
                }
            }

            return expandedAreas.Distinct().ToList();
        }
    }
}
