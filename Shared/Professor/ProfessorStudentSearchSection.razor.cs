using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplitIt.Shared.Professor
{
    public partial class ProfessorStudentSearchSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;

        // Form Visibility
        private bool isProfessorSearchStudentFormVisible = false;

        // Search Fields
        private string searchEmailAsProfessorToFindStudent = "";
        private string searchNameAsProfessorToFindStudent = "";
        private List<string> studentNameSuggestions = new List<string>();
        private string searchSurnameAsProfessorToFindStudent = "";
        private List<string> studentSurnameSuggestions = new List<string>();
        private string searchRegNumberAsProfessorToFindStudent = "";
        private string searchSchoolAsProfessorToFindStudent = "";
        private string searchDepartmentAsProfessorToFindStudent = "";
        private string searchAreasOfExpertiseAsProfessorToFindStudent = "";
        private string searchKeywordsAsProfessorToFindStudent = "";

        // Selected values
        private List<string> selectedAreasOfExpertise = new List<string>();
        private List<string> selectedKeywords = new List<string>();

        // University Departments
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
                "ΤΜΗΜΑ ΑΕΡΟΔΙΑΣΤΗΜΙΑΚΗΣ ΕΠΙΣΤΗΜΗΣ ΚΑΙ ΤΕΧΝΟΛΟΓΙΑΣ",
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
            string.IsNullOrEmpty(searchSchoolAsProfessorToFindStudent)
                ? new List<string>()
                : universityDepartments.ContainsKey(searchSchoolAsProfessorToFindStudent)
                    ? universityDepartments[searchSchoolAsProfessorToFindStudent]
                    : new List<string>();

        // Search Results and Pagination
        private List<Student> searchResultsAsProfessorToFindStudent = new List<Student>();
        private int StudentsPerPage_SearchForStudentsAsProfessor = 10;
        private int[] pageSizeOptions_SearchForStudentsAsProfessor = new[] { 10, 50, 100 };
        private int currentStudentPage_SearchForStudentsAsProfessor = 1;
        private int totalStudentPages_SearchForStudentsAsProfessor = 0;

        private void ToggleFormVisibilityForSearchStudentAsProfessor()
        {
            isProfessorSearchStudentFormVisible = !isProfessorSearchStudentFormVisible;
            StateHasChanged();
        }

        private void HandleStudentNameInput(ChangeEventArgs e)
        {
            searchNameAsProfessorToFindStudent = e.Value?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(searchNameAsProfessorToFindStudent) && searchNameAsProfessorToFindStudent.Length >= 2)
            {
                studentNameSuggestions = dbContext.Students
                    .Where(s => s.Name.Contains(searchNameAsProfessorToFindStudent))
                    .Select(s => s.Name)
                    .Distinct()
                    .Take(10)
                    .ToList();
            }
            else
            {
                studentNameSuggestions.Clear();
            }
            StateHasChanged();
        }

        private void SelectStudentNameSuggestion(string suggestion)
        {
            searchNameAsProfessorToFindStudent = suggestion;
            studentNameSuggestions.Clear();
            StateHasChanged();
        }

        private void HandleStudentSurnameInput(ChangeEventArgs e)
        {
            searchSurnameAsProfessorToFindStudent = e.Value?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(searchSurnameAsProfessorToFindStudent) && searchSurnameAsProfessorToFindStudent.Length >= 2)
            {
                studentSurnameSuggestions = dbContext.Students
                    .Where(s => s.Surname.Contains(searchSurnameAsProfessorToFindStudent))
                    .Select(s => s.Surname)
                    .Distinct()
                    .Take(10)
                    .ToList();
            }
            else
            {
                studentSurnameSuggestions.Clear();
            }
            StateHasChanged();
        }

        private void SelectStudentSurnameSuggestion(string suggestion)
        {
            searchSurnameAsProfessorToFindStudent = suggestion;
            studentSurnameSuggestions.Clear();
            StateHasChanged();
        }

        private void OnStudentSchoolChangedAsProfessor(ChangeEventArgs e)
        {
            searchSchoolAsProfessorToFindStudent = e.Value?.ToString() ?? "";
            // Clear department selection when school changes
            searchDepartmentAsProfessorToFindStudent = "";
            StateHasChanged();
        }

        private List<string> GetAllStudentDepartments()
        {
            return dbContext.Students
                .Select(s => s.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }

        private void SearchStudentsAsProfessorToFindStudent()
        {
            var combinedSearchAreas = new List<string>();

            if (selectedAreasOfExpertise != null && selectedAreasOfExpertise.Any())
                combinedSearchAreas.AddRange(selectedAreasOfExpertise);

            if (!string.IsNullOrEmpty(searchAreasOfExpertiseAsProfessorToFindStudent))
                combinedSearchAreas.Add(searchAreasOfExpertiseAsProfessorToFindStudent);

            // Use NormalizeAreas method for search terms
            var normalizedSearchAreas = combinedSearchAreas
                .SelectMany(area => NormalizeAreas(area))
                .Distinct()
                .ToList();

            var students = dbContext.Students
                .AsEnumerable()
                .Where(s =>
                {
                    // Use NormalizeAreas method for student areas
                    var normalizedStudentAreas = NormalizeAreas(s.AreasOfExpertise).ToList();

                    // Extract expanded areas including subfields
                    var expandedStudentAreas = ExpandAreasWithSubfields(normalizedStudentAreas);

                    var normalizedStudentKeywords = NormalizeKeywords(s.Keywords).ToList();

                    // Enhanced area matching that includes subfields
                    var areaMatch = !normalizedSearchAreas.Any() ||
                        normalizedSearchAreas.Any(searchArea =>
                            expandedStudentAreas.Any(studentArea =>
                                studentArea.Contains(searchArea, StringComparison.OrdinalIgnoreCase) ||
                                searchArea.Contains(studentArea, StringComparison.OrdinalIgnoreCase)));

                    var keywordMatch = string.IsNullOrEmpty(searchKeywordsAsProfessorToFindStudent) ||
                        normalizedStudentKeywords.Any(studentKeyword =>
                            studentKeyword.Contains(searchKeywordsAsProfessorToFindStudent.Trim().ToLower()) ||
                            searchKeywordsAsProfessorToFindStudent.Trim().ToLower().Contains(studentKeyword));

                    // Filter by school if selected
                    bool schoolMatch = true;
                    if (!string.IsNullOrEmpty(searchSchoolAsProfessorToFindStudent))
                    {
                        var schoolDepartments = universityDepartments[searchSchoolAsProfessorToFindStudent];
                        schoolMatch = schoolDepartments.Contains(s.Department);
                    }

                    return (string.IsNullOrEmpty(searchEmailAsProfessorToFindStudent) || s.Email.Contains(searchEmailAsProfessorToFindStudent)) &&
                        (string.IsNullOrEmpty(searchNameAsProfessorToFindStudent) || s.Name.Contains(searchNameAsProfessorToFindStudent)) &&
                        (string.IsNullOrEmpty(searchSurnameAsProfessorToFindStudent) || s.Surname.Contains(searchSurnameAsProfessorToFindStudent)) &&
                        (string.IsNullOrEmpty(searchRegNumberAsProfessorToFindStudent) || s.RegNumber.ToString().Contains(searchRegNumberAsProfessorToFindStudent)) &&
                        (string.IsNullOrEmpty(searchSchoolAsProfessorToFindStudent) || schoolMatch) &&
                        (string.IsNullOrEmpty(searchDepartmentAsProfessorToFindStudent) || s.Department.Contains(searchDepartmentAsProfessorToFindStudent)) &&
                        areaMatch &&
                        keywordMatch;
                })
                .ToList();

            searchResultsAsProfessorToFindStudent = students;
            currentStudentPage_SearchForStudentsAsProfessor = 1;
            UpdateTotalPages();
            StateHasChanged();
        }

        private IEnumerable<Student> GetPaginatedStudentResults()
        {
            if (searchResultsAsProfessorToFindStudent == null || !searchResultsAsProfessorToFindStudent.Any())
                return Enumerable.Empty<Student>();

            return searchResultsAsProfessorToFindStudent
                .Skip((currentStudentPage_SearchForStudentsAsProfessor - 1) * StudentsPerPage_SearchForStudentsAsProfessor)
                .Take(StudentsPerPage_SearchForStudentsAsProfessor);
        }

        private void OnPageSizeChange_SearchForStudentsAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                StudentsPerPage_SearchForStudentsAsProfessor = newSize;
                currentStudentPage_SearchForStudentsAsProfessor = 1;
                UpdateTotalPages();
                StateHasChanged();
            }
        }

        private void UpdateTotalPages()
        {
            if (searchResultsAsProfessorToFindStudent == null || !searchResultsAsProfessorToFindStudent.Any())
            {
                totalStudentPages_SearchForStudentsAsProfessor = 0;
                return;
            }

            totalStudentPages_SearchForStudentsAsProfessor = (int)Math.Ceiling((double)searchResultsAsProfessorToFindStudent.Count / StudentsPerPage_SearchForStudentsAsProfessor);
        }

        private void GoToFirstStudentPage()
        {
            currentStudentPage_SearchForStudentsAsProfessor = 1;
            StateHasChanged();
        }

        private void PreviousStudentPage()
        {
            if (currentStudentPage_SearchForStudentsAsProfessor > 1)
            {
                currentStudentPage_SearchForStudentsAsProfessor--;
                StateHasChanged();
            }
        }

        private void GoToStudentPage(int page)
        {
            if (page >= 1 && page <= totalStudentPages_SearchForStudentsAsProfessor)
            {
                currentStudentPage_SearchForStudentsAsProfessor = page;
                StateHasChanged();
            }
        }

        private void NextStudentPage()
        {
            if (currentStudentPage_SearchForStudentsAsProfessor < totalStudentPages_SearchForStudentsAsProfessor)
            {
                currentStudentPage_SearchForStudentsAsProfessor++;
                StateHasChanged();
            }
        }

        private void GoToLastStudentPage()
        {
            currentStudentPage_SearchForStudentsAsProfessor = totalStudentPages_SearchForStudentsAsProfessor;
            StateHasChanged();
        }

        private List<int> GetVisibleStudentPages()
        {
            var pages = new List<int>();
            int current = currentStudentPage_SearchForStudentsAsProfessor;
            int total = totalStudentPages_SearchForStudentsAsProfessor;

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
    }
}

