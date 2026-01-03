using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using QuizManager.Models;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyResearchGroupSearchSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;

        // Form Visibility
        private bool isCompanySearchResearchGroupVisible = false;

        // Search Fields
        private string searchResearchGroupNameAsCompanyToFindResearchGroup = "";
        private List<string> researchgroupNameSuggestions = new List<string>();
        private string searchResearchGroupSchoolAsCompanyToFindResearchGroup = "";
        private string searchResearchGroupUniversityDepartmentAsCompanyToFindResearchGroup = "";
        private string searchResearchGroupAreasAsCompanyToFindResearchGroup = "";
        private List<string> researchGroupAreasSuggestions = new List<string>();
        private List<string> selectedResearchGroupAreas = new List<string>();
        private string searchResearchGroupSkillsAsCompanyToFindResearchGroup = "";
        private List<string> researchGroupSkillsSuggestions = new List<string>();
        private List<string> selectedResearchGroupSkills = new List<string>();
        private string searchResearchGroupKeywordsAsCompanyToFindResearchGroup = "";

        // Search Results and Pagination
        private List<QuizManager.Models.ResearchGroup> searchResultsAsCompanyToFindResearchGroup = new List<QuizManager.Models.ResearchGroup>();
        private int ResearchGroupsPerPage_SearchForResearchGroupsAsCompany = 10;
        private int[] pageSizeOptions_SearchForResearchGroupsAsCompany = new[] { 10, 50, 100 };
        private int currentResearchGroupPage_SearchForResearchGroupsAsCompany = 1;
        private bool hasSearchedForResearchGroups = false;

        // Research Group Details Modal
        private bool showResearchGroupDetailsModalWhenSearchForResearchGroupsAsCompany = false;
        private QuizManager.Models.ResearchGroup selectedResearchGroupWhenSearchForResearchGroupsAsCompany;

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

        private List<string> filteredDepartments =>
            string.IsNullOrEmpty(searchResearchGroupSchoolAsCompanyToFindResearchGroup)
                ? new List<string>()
                : universityDepartments.ContainsKey(searchResearchGroupSchoolAsCompanyToFindResearchGroup)
                    ? universityDepartments[searchResearchGroupSchoolAsCompanyToFindResearchGroup]
                    : new List<string>();

        private int totalResearchGroupPages_SearchForResearchGroupsAsCompany =>
            searchResultsAsCompanyToFindResearchGroup != null && searchResultsAsCompanyToFindResearchGroup.Any()
                ? (int)Math.Ceiling((double)searchResultsAsCompanyToFindResearchGroup.Count / ResearchGroupsPerPage_SearchForResearchGroupsAsCompany)
                : 0;

        // Visibility Toggle
        private async Task ToggleCompanySearchResearchGroupVisible()
        {
            isCompanySearchResearchGroupVisible = !isCompanySearchResearchGroupVisible;
            StateHasChanged();
        }

        // Search Field Handlers
        private async Task HandleResearchGroupNameInput(ChangeEventArgs e)
        {
            searchResearchGroupNameAsCompanyToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            researchgroupNameSuggestions = new List<string>();

            if (searchResearchGroupNameAsCompanyToFindResearchGroup.Length >= 1)
            {
                try
                {
                    var results = await CompanyDashboardService.SearchResearchGroupsAsync(new ResearchGroupSearchFilter
                    {
                        Name = searchResearchGroupNameAsCompanyToFindResearchGroup,
                        MaxResults = 10
                    });

                    researchgroupNameSuggestions = results
                        .Select(rg => rg.ResearchGroupName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(10)
                        .ToList()!;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching research group suggestions: {ex.Message}");
                    researchgroupNameSuggestions = new List<string>();
                }
            }
            else
            {
                researchgroupNameSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectResearchGroupNameSuggestion(string suggestion)
        {
            searchResearchGroupNameAsCompanyToFindResearchGroup = suggestion;
            researchgroupNameSuggestions.Clear();
            StateHasChanged();
        }

        private async Task OnSchoolSelectionChanged(ChangeEventArgs e)
        {
            searchResearchGroupSchoolAsCompanyToFindResearchGroup = e.Value?.ToString() ?? "";
            searchResearchGroupUniversityDepartmentAsCompanyToFindResearchGroup = ""; // Clear department when school changes
            StateHasChanged();
        }

        // Research Areas Handlers
        private async Task HandleResearchGroupAreasInput(ChangeEventArgs e)
        {
            searchResearchGroupAreasAsCompanyToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            researchGroupAreasSuggestions = new List<string>();

            if (searchResearchGroupAreasAsCompanyToFindResearchGroup.Length >= 1)
            {
                try
                {
                    var suggestions = await CompanyDashboardService.SearchAreasAsync(searchResearchGroupAreasAsCompanyToFindResearchGroup);
                    researchGroupAreasSuggestions = suggestions.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching areas: {ex.Message}");
                    researchGroupAreasSuggestions = new List<string>();
                }
            }
            else
            {
                researchGroupAreasSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void HandleResearchGroupAreasKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "Tab")
            {
                if (!string.IsNullOrWhiteSpace(searchResearchGroupAreasAsCompanyToFindResearchGroup) &&
                    !selectedResearchGroupAreas.Contains(searchResearchGroupAreasAsCompanyToFindResearchGroup))
                {
                    selectedResearchGroupAreas.Add(searchResearchGroupAreasAsCompanyToFindResearchGroup);
                    searchResearchGroupAreasAsCompanyToFindResearchGroup = string.Empty;
                    researchGroupAreasSuggestions.Clear();
                    StateHasChanged();
                }
            }
        }

        private void SelectResearchGroupAreasSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedResearchGroupAreas.Contains(suggestion))
            {
                selectedResearchGroupAreas.Add(suggestion);
                researchGroupAreasSuggestions.Clear();
                searchResearchGroupAreasAsCompanyToFindResearchGroup = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedResearchGroupArea(string area)
        {
            selectedResearchGroupAreas.Remove(area);
            StateHasChanged();
        }

        // Skills Handlers
        private async Task HandleResearchGroupSkillsInput(ChangeEventArgs e)
        {
            searchResearchGroupSkillsAsCompanyToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            researchGroupSkillsSuggestions = new List<string>();

            if (searchResearchGroupSkillsAsCompanyToFindResearchGroup.Length >= 1)
            {
                try
                {
                    var suggestions = await CompanyDashboardService.SearchSkillsAsync(searchResearchGroupSkillsAsCompanyToFindResearchGroup);
                    researchGroupSkillsSuggestions = suggestions.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching skills: {ex.Message}");
                    researchGroupSkillsSuggestions = new List<string>();
                }
            }
            else
            {
                researchGroupSkillsSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void HandleResearchGroupSkillsKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "Tab")
            {
                if (!string.IsNullOrWhiteSpace(searchResearchGroupSkillsAsCompanyToFindResearchGroup) &&
                    !selectedResearchGroupSkills.Contains(searchResearchGroupSkillsAsCompanyToFindResearchGroup))
                {
                    selectedResearchGroupSkills.Add(searchResearchGroupSkillsAsCompanyToFindResearchGroup);
                    searchResearchGroupSkillsAsCompanyToFindResearchGroup = string.Empty;
                    researchGroupSkillsSuggestions.Clear();
                    StateHasChanged();
                }
            }
        }

        private void SelectResearchGroupSkillsSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedResearchGroupSkills.Contains(suggestion))
            {
                selectedResearchGroupSkills.Add(suggestion);
                researchGroupSkillsSuggestions.Clear();
                searchResearchGroupSkillsAsCompanyToFindResearchGroup = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedResearchGroupSkill(string skill)
        {
            selectedResearchGroupSkills.Remove(skill);
            StateHasChanged();
        }

        // Search Method
        private async Task SearchResearchGroupsAsCompany()
        {
            try
            {
                hasSearchedForResearchGroups = true;

                var areas = selectedResearchGroupAreas.ToList();
                if (!string.IsNullOrWhiteSpace(searchResearchGroupAreasAsCompanyToFindResearchGroup))
                {
                    areas.Add(searchResearchGroupAreasAsCompanyToFindResearchGroup);
                }

                var skills = selectedResearchGroupSkills.ToList();
                if (!string.IsNullOrWhiteSpace(searchResearchGroupSkillsAsCompanyToFindResearchGroup))
                {
                    skills.Add(searchResearchGroupSkillsAsCompanyToFindResearchGroup);
                }

                var filter = new ResearchGroupSearchFilter
                {
                    Name = searchResearchGroupNameAsCompanyToFindResearchGroup,
                    Areas = areas.Any() ? string.Join(", ", areas) : null,
                    Skills = skills.Any() ? string.Join(", ", skills) : null,
                    MaxResults = 200
                };

                var filteredResearchGroups = (await CompanyDashboardService.SearchResearchGroupsAsync(filter)).AsEnumerable();

                if (!string.IsNullOrEmpty(searchResearchGroupNameAsCompanyToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupName.Contains(searchResearchGroupNameAsCompanyToFindResearchGroup, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(searchResearchGroupSchoolAsCompanyToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupSchool == searchResearchGroupSchoolAsCompanyToFindResearchGroup);
                }

                if (!string.IsNullOrEmpty(searchResearchGroupUniversityDepartmentAsCompanyToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupUniversityDepartment == searchResearchGroupUniversityDepartmentAsCompanyToFindResearchGroup);
                }

                // Areas filter
                if (selectedResearchGroupAreas.Any() || !string.IsNullOrEmpty(searchResearchGroupAreasAsCompanyToFindResearchGroup))
                {
                    var areaSearchTerms = new List<string>();

                    if (!string.IsNullOrEmpty(searchResearchGroupAreasAsCompanyToFindResearchGroup))
                    {
                        areaSearchTerms.Add(searchResearchGroupAreasAsCompanyToFindResearchGroup.Trim());
                    }

                    areaSearchTerms.AddRange(selectedResearchGroupAreas);
                    areaSearchTerms = areaSearchTerms.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();

                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => areaSearchTerms.Any(area =>
                        {
                            var researchGroupAreas = rg.ResearchGroupAreas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(a => a.Trim())
                                .ToList();

                            return researchGroupAreas.Any(researchArea =>
                                researchArea.Contains(area, StringComparison.OrdinalIgnoreCase) ||
                                area.Contains(researchArea, StringComparison.OrdinalIgnoreCase));
                        }));
                }

                // Skills filter
                if (selectedResearchGroupSkills.Any() || !string.IsNullOrEmpty(searchResearchGroupSkillsAsCompanyToFindResearchGroup))
                {
                    var skillSearchTerms = new List<string>();

                    if (!string.IsNullOrEmpty(searchResearchGroupSkillsAsCompanyToFindResearchGroup))
                    {
                        skillSearchTerms.Add(searchResearchGroupSkillsAsCompanyToFindResearchGroup.Trim());
                    }

                    skillSearchTerms.AddRange(selectedResearchGroupSkills);
                    skillSearchTerms = skillSearchTerms.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => skillSearchTerms.Any(skill =>
                            rg.ResearchGroupSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Any(s => s.Contains(skill, StringComparison.OrdinalIgnoreCase))
                        ));
                }

                // Keywords filter
                if (!string.IsNullOrEmpty(searchResearchGroupKeywordsAsCompanyToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupKeywords != null &&
                                    rg.ResearchGroupKeywords.Contains(searchResearchGroupKeywordsAsCompanyToFindResearchGroup, StringComparison.OrdinalIgnoreCase));
                }

                searchResultsAsCompanyToFindResearchGroup = filteredResearchGroups.ToList();
                currentResearchGroupPage_SearchForResearchGroupsAsCompany = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching research groups: {ex.Message}");
                searchResultsAsCompanyToFindResearchGroup = new List<QuizManager.Models.ResearchGroup>();
            }

            StateHasChanged();
        }

        // Clear Search Fields
        private void ClearSearchFieldsAsCompanyToFindResearchGroup()
        {
            searchResearchGroupNameAsCompanyToFindResearchGroup = "";
            searchResearchGroupSchoolAsCompanyToFindResearchGroup = "";
            searchResearchGroupUniversityDepartmentAsCompanyToFindResearchGroup = "";
            searchResearchGroupAreasAsCompanyToFindResearchGroup = "";
            searchResearchGroupSkillsAsCompanyToFindResearchGroup = "";
            searchResearchGroupKeywordsAsCompanyToFindResearchGroup = "";

            selectedResearchGroupAreas.Clear();
            selectedResearchGroupSkills.Clear();

            researchgroupNameSuggestions.Clear();
            researchGroupAreasSuggestions.Clear();
            researchGroupSkillsSuggestions.Clear();

            searchResultsAsCompanyToFindResearchGroup = new List<QuizManager.Models.ResearchGroup>();
            hasSearchedForResearchGroups = false;

            StateHasChanged();
        }

        // Research Group Details Properties (for modal display)
        private List<FacultyMemberInfo> facultyMembers = new List<FacultyMemberInfo>();
        private List<NonFacultyMemberInfo> nonFacultyMembers = new List<NonFacultyMemberInfo>();
        private int patentsCount = 0;
        private int activeResearchActionsCount = 0;
        private List<SpinOffCompanyInfo> spinOffCompanies = new List<SpinOffCompanyInfo>();
        private List<IpodomiInfo> researchGroupIpodomes = new List<IpodomiInfo>();
        
        // DTOs for Research Group Details
        public class FacultyMemberInfo
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            public string Department { get; set; }
        }
        
        public class NonFacultyMemberInfo
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
        
        public class SpinOffCompanyInfo
        {
            public string CompanyName { get; set; }
            public string CompanyAFM { get; set; }
        }
        
        public class IpodomiInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        // Pagination Methods
        private IEnumerable<QuizManager.Models.ResearchGroup> GetPaginatedResearchGroupResults()
        {
            return searchResultsAsCompanyToFindResearchGroup?
                .Skip((currentResearchGroupPage_SearchForResearchGroupsAsCompany - 1) * ResearchGroupsPerPage_SearchForResearchGroupsAsCompany)
                .Take(ResearchGroupsPerPage_SearchForResearchGroupsAsCompany)
                ?? Enumerable.Empty<QuizManager.Models.ResearchGroup>();
        }
        
        // Load Research Group Details (placeholder - should load from service)
        private async Task LoadResearchGroupDetailsData(QuizManager.Models.ResearchGroup researchGroup)
        {
            // TODO: Load details from service
            facultyMembers.Clear();
            nonFacultyMembers.Clear();
            spinOffCompanies.Clear();
            researchGroupIpodomes.Clear();
            patentsCount = 0;
            activeResearchActionsCount = 0;
            await Task.CompletedTask;
        }

        private void OnPageSizeChange_SearchForResearchGroupsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                ResearchGroupsPerPage_SearchForResearchGroupsAsCompany = newSize;
                currentResearchGroupPage_SearchForResearchGroupsAsCompany = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstResearchGroupPage()
        {
            currentResearchGroupPage_SearchForResearchGroupsAsCompany = 1;
            StateHasChanged();
        }

        private void PreviousResearchGroupPage()
        {
            if (currentResearchGroupPage_SearchForResearchGroupsAsCompany > 1)
            {
                currentResearchGroupPage_SearchForResearchGroupsAsCompany--;
                StateHasChanged();
            }
        }

        private void NextResearchGroupPage()
        {
            if (currentResearchGroupPage_SearchForResearchGroupsAsCompany < totalResearchGroupPages_SearchForResearchGroupsAsCompany)
            {
                currentResearchGroupPage_SearchForResearchGroupsAsCompany++;
                StateHasChanged();
            }
        }

        private void GoToLastResearchGroupPage()
        {
            currentResearchGroupPage_SearchForResearchGroupsAsCompany = totalResearchGroupPages_SearchForResearchGroupsAsCompany;
            StateHasChanged();
        }

        private void GoToResearchGroupPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalResearchGroupPages_SearchForResearchGroupsAsCompany)
            {
                currentResearchGroupPage_SearchForResearchGroupsAsCompany = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisibleResearchGroupPages()
        {
            var pages = new List<int>();
            int currentPage = currentResearchGroupPage_SearchForResearchGroupsAsCompany;
            int totalPages = totalResearchGroupPages_SearchForResearchGroupsAsCompany;

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
        private void ShowResearchGroupDetailsModal(QuizManager.Models.ResearchGroup researchGroup)
        {
            selectedResearchGroupWhenSearchForResearchGroupsAsCompany = researchGroup;
            showResearchGroupDetailsModalWhenSearchForResearchGroupsAsCompany = true;
            StateHasChanged();
        }

        private void CloseModalResearchGroupDetailsOnEyeIconWhenSearchForResearchGroupsAsCompany()
        {
            showResearchGroupDetailsModalWhenSearchForResearchGroupsAsCompany = false;
            selectedResearchGroupWhenSearchForResearchGroupsAsCompany = null;
            StateHasChanged();
        }
    }
}
