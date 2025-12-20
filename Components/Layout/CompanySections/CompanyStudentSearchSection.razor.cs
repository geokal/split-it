using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyStudentSearchSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;

        // Form Visibility
        private bool isCompanySearchStudentVisible = false;

        // Search Fields
        private string searchNameOrSurname = "";
        private List<string> nameSurnameSuggestions = new List<string>();
        private string selectedDegreeLevel = "";
        private List<string> DegreeLevel = new List<string>
        {
            "Προπτυχιακός Φοιτητής",
            "Μεταπτυχιακός Φοιτητής",
            "Υποψήφιος Διδάκτορας"
        };
        private string searchRegNumberAsCompanyToFindStudent = "";
        private string searchSchoolAsCompanyToFindStudent = "";
        private string searchDepartmentAsCompanyToFindStudent = "";
        private string InternshipStatus = "";
        private string ThesisStatus = "";

        // Areas of Expertise
        private string searchAreasOfExpertise = "";
        private List<string> areasOfExpertiseSuggestions = new List<string>();
        private List<string> selectedAreasOfExpertise = new List<string>();

        // Keywords
        private string searchKeywords = "";
        private List<string> keywordsSuggestions = new List<string>();
        private List<string> selectedKeywords = new List<string>();

        // Search Results and Pagination
        private List<QuizManager.Models.Student> searchResultsAsCompanyToFindStudent = new List<QuizManager.Models.Student>();
        private int StudentsPerPage_SearchForStudentsAsCompany = 10;
        private int[] pageSizeOptions_SearchForStudentsAsCompany = new[] { 10, 50, 100 };
        private int currentPageForStudents_SearchForStudentsAsCompany = 1;

        // Student Details Modal
        private bool showStudentDetailsModalWhenSearchForStudentsAsCompany = false;
        private QuizManager.Models.Student selectedStudentWhenSearchForStudentsAsCompany;

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

        private List<string> filteredStudentDepartments =>
            string.IsNullOrEmpty(searchSchoolAsCompanyToFindStudent)
                ? GetAllStudentDepartments()
                : universityDepartments.ContainsKey(searchSchoolAsCompanyToFindStudent)
                    ? universityDepartments[searchSchoolAsCompanyToFindStudent]
                    : new List<string>();

        private int totalPagesForStudents_SearchForStudentsAsCompany =>
            searchResultsAsCompanyToFindStudent != null && searchResultsAsCompanyToFindStudent.Any()
                ? (int)Math.Ceiling((double)searchResultsAsCompanyToFindStudent.Count / StudentsPerPage_SearchForStudentsAsCompany)
                : 0;

        // Visibility Toggle
        private async Task ToggleCompanySearchStudentVisible()
        {
            isCompanySearchStudentVisible = !isCompanySearchStudentVisible;
            StateHasChanged();
        }

        // Search Field Handlers
        private async Task HandleInput(ChangeEventArgs e)
        {
            searchNameOrSurname = e.Value?.ToString().Trim() ?? string.Empty;

            if (searchNameOrSurname.Length >= 2)
            {
                try
                {
                    nameSurnameSuggestions = await Task.Run(() =>
                        dbContext.Students
                            .Where(s =>
                                s.Name.Contains(searchNameOrSurname) ||
                                s.Surname.Contains(searchNameOrSurname))
                            .Select(s => s.Name + " " + s.Surname)
                            .Distinct()
                            .Take(10)
                            .ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching student suggestions: {ex.Message}");
                    nameSurnameSuggestions.Clear();
                }
            }
            else
            {
                nameSurnameSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectNameSurnameSuggestion(string suggestion)
        {
            searchNameOrSurname = suggestion;
            nameSurnameSuggestions.Clear();
            StateHasChanged();
        }

        private async Task OnStudentSchoolChanged(ChangeEventArgs e)
        {
            searchSchoolAsCompanyToFindStudent = e.Value?.ToString() ?? "";
            searchDepartmentAsCompanyToFindStudent = ""; // Clear department when school changes
            StateHasChanged();
        }

        private List<string> GetAllStudentDepartments()
        {
            return universityDepartments.Values.SelectMany(depts => depts).Distinct().ToList();
        }

        // Areas of Expertise Handlers
        private async Task HandleAreasOfExpertiseInput(ChangeEventArgs e)
        {
            searchAreasOfExpertise = e.Value?.ToString().Trim() ?? string.Empty;
            areasOfExpertiseSuggestions = new List<string>();

            if (searchAreasOfExpertise.Length >= 1)
            {
                try
                {
                    var allAreas = await dbContext.Areas
                        .Where(a => a.AreaName.Contains(searchAreasOfExpertise) ||
                                (a.AreaSubFields != null && a.AreaSubFields.Contains(searchAreasOfExpertise)))
                        .ToListAsync();

                    var suggestionsSet = new HashSet<string>();

                    foreach (var area in allAreas)
                    {
                        if (area.AreaName.Contains(searchAreasOfExpertise, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestionsSet.Add(area.AreaName);
                        }

                        if (!string.IsNullOrEmpty(area.AreaSubFields))
                        {
                            var subfields = area.AreaSubFields
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(sub => sub.Trim())
                                .Where(sub => !string.IsNullOrEmpty(sub) &&
                                            sub.Contains(searchAreasOfExpertise, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            foreach (var subfield in subfields)
                            {
                                var combination = $"{area.AreaName} - {subfield}";
                                suggestionsSet.Add(combination);
                            }
                        }
                    }

                    areasOfExpertiseSuggestions = suggestionsSet.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching areas: {ex.Message}");
                    areasOfExpertiseSuggestions = new List<string>();
                }
            }
            else
            {
                areasOfExpertiseSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectAreasOfExpertiseSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasOfExpertise.Contains(suggestion))
            {
                selectedAreasOfExpertise.Add(suggestion);
                areasOfExpertiseSuggestions.Clear();
                searchAreasOfExpertise = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaOfExpertise(string area)
        {
            selectedAreasOfExpertise.Remove(area);
            StateHasChanged();
        }

        // Keywords Handlers
        private async Task HandleKeywordsInput(ChangeEventArgs e)
        {
            searchKeywords = e.Value?.ToString().Trim() ?? string.Empty;
            keywordsSuggestions = new List<string>();

            if (searchKeywords.Length >= 1)
            {
                try
                {
                    keywordsSuggestions = await Task.Run(() =>
                        dbContext.Skills
                            .Where(k => k.SkillName.Contains(searchKeywords))
                            .Select(k => k.SkillName)
                            .Distinct()
                            .Take(10)
                            .ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching keywords: {ex.Message}");
                    keywordsSuggestions = new List<string>();
                }
            }
            else
            {
                keywordsSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectKeywordsSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedKeywords.Contains(suggestion))
            {
                selectedKeywords.Add(suggestion);
                searchKeywords = string.Empty;
                keywordsSuggestions.Clear();
                StateHasChanged();
            }
        }

        private void RemoveKeyword(string keyword)
        {
            selectedKeywords.Remove(keyword);
            StateHasChanged();
        }

        // Search Method
        private void SearchStudentsAsCompanyToFindStudent()
        {
            var combinedSearchAreas = new List<string>();

            if (selectedAreasOfExpertise != null && selectedAreasOfExpertise.Any())
                combinedSearchAreas.AddRange(selectedAreasOfExpertise);

            if (!string.IsNullOrWhiteSpace(searchAreasOfExpertise))
                combinedSearchAreas.Add(searchAreasOfExpertise);

            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            var combinedSearchKeywords = new List<string>();

            if (selectedKeywords != null && selectedKeywords.Any())
                combinedSearchKeywords.AddRange(selectedKeywords);

            if (!string.IsNullOrWhiteSpace(searchKeywords))
                combinedSearchKeywords.Add(searchKeywords);

            var normalizedSearchKeywords = combinedSearchKeywords
                .SelectMany(keyword => NormalizeKeywords(keyword))
                .Distinct()
                .ToList();

            searchResultsAsCompanyToFindStudent = dbContext.Students
                .AsEnumerable()
                .Where(s =>
                {
                    var normalizedStudentAreas = NormalizeAreas(s.AreasOfExpertise).ToList();
                    var expandedStudentAreas = ExpandAreasWithSubfields(normalizedStudentAreas);
                    var normalizedStudentKeywords = NormalizeKeywords(s.Keywords).ToList();

                    bool schoolMatch = true;
                    if (!string.IsNullOrEmpty(searchSchoolAsCompanyToFindStudent))
                    {
                        var schoolDepartments = universityDepartments[searchSchoolAsCompanyToFindStudent];
                        schoolMatch = schoolDepartments.Contains(s.Department);
                    }

                    var areaMatch = !normalizedSearchAreas.Any() ||
                        normalizedSearchAreas.Any(searchArea =>
                            expandedStudentAreas.Any(studentArea =>
                                studentArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                                searchArea.Contains(studentArea, StringComparison.OrdinalIgnoreCase)));

                    var keywordMatch = !normalizedSearchKeywords.Any() ||
                        normalizedSearchKeywords.Any(searchKeyword =>
                            normalizedStudentKeywords.Any(studentKeyword =>
                                studentKeyword.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                                searchKeyword.Contains(studentKeyword, StringComparison.OrdinalIgnoreCase)));

                    return (string.IsNullOrEmpty(searchNameOrSurname) ||
                                (s.Name + " " + s.Surname).Contains(searchNameOrSurname, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(searchRegNumberAsCompanyToFindStudent) ||
                                s.RegNumber.ToString().Contains(searchRegNumberAsCompanyToFindStudent)) &&
                        (string.IsNullOrEmpty(searchSchoolAsCompanyToFindStudent) || schoolMatch) &&
                        (string.IsNullOrEmpty(searchDepartmentAsCompanyToFindStudent) ||
                                s.Department == searchDepartmentAsCompanyToFindStudent) &&
                        (string.IsNullOrEmpty(InternshipStatus) ||
                                s.InternshipStatus == InternshipStatus) &&
                        (string.IsNullOrEmpty(ThesisStatus) ||
                                s.ThesisStatus == ThesisStatus) &&
                        areaMatch &&
                        keywordMatch &&
                        (string.IsNullOrEmpty(selectedDegreeLevel) ||
                                s.LevelOfDegree == selectedDegreeLevel);
                })
                .ToList();

            currentPageForStudents_SearchForStudentsAsCompany = 1;
            StateHasChanged();
        }

        // Clear Search Fields
        private void ClearSearchFieldsAsCompanyToFindStudent()
        {
            searchNameOrSurname = string.Empty;
            searchRegNumberAsCompanyToFindStudent = string.Empty;
            searchSchoolAsCompanyToFindStudent = string.Empty;
            searchDepartmentAsCompanyToFindStudent = string.Empty;
            InternshipStatus = string.Empty;
            ThesisStatus = string.Empty;
            searchAreasOfExpertise = string.Empty;
            searchKeywords = string.Empty;
            selectedDegreeLevel = string.Empty;
            searchResultsAsCompanyToFindStudent = new List<QuizManager.Models.Student>();

            nameSurnameSuggestions.Clear();
            areasOfExpertiseSuggestions.Clear();
            keywordsSuggestions.Clear();

            selectedKeywords.Clear();
            selectedAreasOfExpertise.Clear();
            StateHasChanged();
        }

        // Pagination Methods
        private IEnumerable<QuizManager.Models.Student> GetPaginatedStudents_SearchForStudentsAsCompany()
        {
            return searchResultsAsCompanyToFindStudent?
                .Skip((currentPageForStudents_SearchForStudentsAsCompany - 1) * StudentsPerPage_SearchForStudentsAsCompany)
                .Take(StudentsPerPage_SearchForStudentsAsCompany)
                ?? Enumerable.Empty<QuizManager.Models.Student>();
        }

        private void OnPageSizeChange_SearchForStudentsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                StudentsPerPage_SearchForStudentsAsCompany = newSize;
                currentPageForStudents_SearchForStudentsAsCompany = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstPageForStudents_SearchForStudentsAsCompany()
        {
            currentPageForStudents_SearchForStudentsAsCompany = 1;
            StateHasChanged();
        }

        private void PreviousPageForStudents_SearchForStudentsAsCompany()
        {
            if (currentPageForStudents_SearchForStudentsAsCompany > 1)
            {
                currentPageForStudents_SearchForStudentsAsCompany--;
                StateHasChanged();
            }
        }

        private void NextPageForStudents_SearchForStudentsAsCompany()
        {
            if (currentPageForStudents_SearchForStudentsAsCompany < totalPagesForStudents_SearchForStudentsAsCompany)
            {
                currentPageForStudents_SearchForStudentsAsCompany++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForStudents_SearchForStudentsAsCompany()
        {
            currentPageForStudents_SearchForStudentsAsCompany = totalPagesForStudents_SearchForStudentsAsCompany;
            StateHasChanged();
        }

        private void GoToPageForStudents_SearchForStudentsAsCompany(int page)
        {
            if (page > 0 && page <= totalPagesForStudents_SearchForStudentsAsCompany)
            {
                currentPageForStudents_SearchForStudentsAsCompany = page;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForStudents_SearchForStudentsAsCompany()
        {
            var pages = new List<int>();
            int currentPage = currentPageForStudents_SearchForStudentsAsCompany;
            int totalPages = totalPagesForStudents_SearchForStudentsAsCompany;

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
        private void ShowStudentDetailsOnEyeIconWhenSearchForStudentsAsCompany(QuizManager.Models.Student student)
        {
            selectedStudentWhenSearchForStudentsAsCompany = student;
            showStudentDetailsModalWhenSearchForStudentsAsCompany = true;
            StateHasChanged();
        }

        private void CloseModalStudentDetailsOnEyeIconWhenSearchForStudentsAsCompany()
        {
            showStudentDetailsModalWhenSearchForStudentsAsCompany = false;
            selectedStudentWhenSearchForStudentsAsCompany = null;
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

        private IEnumerable<string> NormalizeKeywords(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                return Array.Empty<string>();

            return keywords
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(keyword => keyword.Trim().ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(keyword => !string.IsNullOrEmpty(keyword));
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

