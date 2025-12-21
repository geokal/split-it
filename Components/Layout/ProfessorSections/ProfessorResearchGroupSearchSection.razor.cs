using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ProfessorSections
{
    public partial class ProfessorResearchGroupSearchSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private Microsoft.AspNetCore.Components.NavigationManager NavigationManager { get; set; } = default!;

        // Form Visibility
        private bool isProfessorSearchResearchGroupVisible = false;

        // Selected Entities
        private Company selectedCompany;
        private ResearchGroup selectedResearchGroupWhenSearchForResearchGroupsAsProfessor;
        private ProfessorEvent currentProfessorEvent;
        private Student selectedStudentFromCache;
        private ProfessorInternship selectedProfessorInternship;
        private Company selectedCompanyNameAsHyperlinkToShowDetailsToTheProfessor;
        private CompanyThesis selectedCompanyThesisToSeeDetailsOnEyeIconAsProfessor;
        private ProfessorThesis currentThesisAsProfessor;
        private InterestInProfessorEventAsCompany selectedCompanyToShowDetailsForInterestinProfessorEvent;
        private InterestInProfessorEvent selectedStudentToShowDetailsForInterestinProfessorEvent;

        // Modal Visibility
        private bool showErrorMessage = false;
        private bool isThesisDetailEyeIconModalVisibleToSeeAsProfessor = false;
        private bool showModalForCompaniesAtProfessorEventInterest = false;

        // Edit Modal Flags
        private bool showExpandedSkillsInProfessorThesisEditModal = false;
        private bool showCheckboxesForEditProfessorThesis = false;
        private bool showCheckboxesForEditProfessorInternship = false;
        private bool showCheckboxesForEditProfessorEvent = false;
        private HashSet<int> ExpandedAreasForEditProfessorEvent = new HashSet<int>();
        private HashSet<int> ExpandedAreasForEditProfessorThesis = new HashSet<int>();
        private HashSet<int> ExpandedAreasForEditProfessorInternship = new HashSet<int>();
        private bool isEditModalVisibleForEventsAsProfessor = false;
        private bool isEditPopupVisibleForProfessorInternships = false;
        private bool isEditModalVisibleForThesesAsProfessor = false;
        private bool showCompanyDetailsModal = false;
        private bool showModalForStudentsAtProfessorEventInterest = false;

        // Research Group Details
        private List<Professor> professorFacultyMembers = new List<Professor>();
        private List<Student> professorNonFacultyMembers = new List<Student>();
        private List<Company> professorSpinOffCompanies = new List<Company>();
        private int professorPatentsCount = 0;
        private int professorActiveResearchActionsCount = 0;

        // ForeasType
        private List<string> ForeasType = new List<string>
        {
            "Ιδιωτικός Φορέας",
            "Δημόσιος Φορέας",
            "Μ.Κ.Ο.",
            "Άλλο"
        };

        // Areas
        private List<Area> Areas = new();

        // Spin-off Companies
        private List<Company> spinOffCompanies = new();

        // Region to Towns Map
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
            {"Κρήτη", new List<string> {"Ηράκλειο", "Χανιά", "Ρέθυμνο", "Αγία Νικόλαος", "Ιεράπετρα", "Σητεία", "Κίσαμος", "Παλαιόχωρα", "Αρχάνες", "Ανώγεια"}},
        };

        // Search Fields
        private string searchResearchGroupNameAsProfessorToFindResearchGroup = "";
        private List<string> professorResearchGroupNameSuggestions = new List<string>();
        private string searchResearchGroupSchoolAsProfessorToFindResearchGroup = "";
        private string searchResearchGroupUniversityDepartmentAsProfessorToFindResearchGroup = "";
        private string searchResearchGroupAreasAsProfessorToFindResearchGroup = "";
        private List<string> professorResearchGroupAreasSuggestions = new List<string>();
        private HashSet<string> professorSelectedResearchGroupAreas = new HashSet<string>();
        private string searchResearchGroupSkillsAsProfessorToFindResearchGroup = "";
        private List<string> professorResearchGroupSkillsSuggestions = new List<string>();
        private HashSet<string> professorSelectedResearchGroupSkills = new HashSet<string>();
        private string searchResearchGroupKeywordsAsProfessorToFindResearchGroup = "";
        private bool showResearchGroupDetailsModalWhenSearchForResearchGroupsAsProfessor = false;

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
        
        private List<string> professorFilteredDepartmentsForSearchForResearchGroup =>
            string.IsNullOrEmpty(searchResearchGroupSchoolAsProfessorToFindResearchGroup)
                ? new List<string>()
                : universityDepartments.ContainsKey(searchResearchGroupSchoolAsProfessorToFindResearchGroup)
                    ? universityDepartments[searchResearchGroupSchoolAsProfessorToFindResearchGroup]
                    : new List<string>();
                    
        private bool isCompanyDetailModalVisibleForHyperlinkNameToShowCompanyDetailsToTheProfessor = false;

        // Search Results and Pagination
        private List<QuizManager.Models.ResearchGroup> professorSearchResultsToFindResearchGroup = new List<QuizManager.Models.ResearchGroup>();
        private bool professorHasSearchedForResearchGroups = false;
        private int professorResearchGroupsPerPage_SearchForResearchGroups = 10;
        private int[] professorPageSizeOptions_SearchForResearchGroups = new[] { 10, 50, 100 };
        private int professorCurrentResearchGroupPage_SearchForResearchGroups = 1;
        private int professorTotalResearchGroupPages_SearchForResearchGroups => 
            (int)Math.Ceiling(professorSearchResultsToFindResearchGroup.Count / (double)professorResearchGroupsPerPage_SearchForResearchGroups);

        private async Task ToggleProfessorSearchResearchGroupVisible()
        {
            isProfessorSearchResearchGroupVisible = !isProfessorSearchResearchGroupVisible;
            StateHasChanged();
        }

        private async Task HandleProfessorResearchGroupNameInput(ChangeEventArgs e)
        {
            searchResearchGroupNameAsProfessorToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            professorResearchGroupNameSuggestions = new List<string>();

            if (searchResearchGroupNameAsProfessorToFindResearchGroup.Length >= 1)
            {
                try
                {
                    professorResearchGroupNameSuggestions = await Task.Run(() =>
                        dbContext.ResearchGroups
                            .Where(rg => rg.ResearchGroupName.Contains(searchResearchGroupNameAsProfessorToFindResearchGroup))
                            .Select(rg => rg.ResearchGroupName)
                            .Distinct()
                            .Take(10)
                            .ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Πρόβλημα στην Ανάκτηση Ονομάτων Ερευνητικών Ομάδων: {ex.Message}");
                    professorResearchGroupNameSuggestions = new List<string>();
                }
            }
            else
            {
                professorResearchGroupNameSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectProfessorResearchGroupNameSuggestion(string suggestion)
        {
            searchResearchGroupNameAsProfessorToFindResearchGroup = suggestion;
            professorResearchGroupNameSuggestions.Clear();
            StateHasChanged();
        }

        private async Task OnProfessorSchoolSelectionChanged(ChangeEventArgs e)
        {
            searchResearchGroupSchoolAsProfessorToFindResearchGroup = e.Value?.ToString() ?? "";
            // Clear department selection when school changes
            searchResearchGroupUniversityDepartmentAsProfessorToFindResearchGroup = "";
            StateHasChanged();
        }

        private async Task HandleProfessorResearchGroupAreasInput(ChangeEventArgs e)
        {
            searchResearchGroupAreasAsProfessorToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            professorResearchGroupAreasSuggestions = new List<string>();

            if (searchResearchGroupAreasAsProfessorToFindResearchGroup.Length >= 1)
            {
                try
                {
                    // Get all areas from the database that match the search
                    var allAreas = await dbContext.Areas
                        .Where(a => a.AreaName.Contains(searchResearchGroupAreasAsProfessorToFindResearchGroup) ||
                                (a.AreaSubFields != null && a.AreaSubFields.Contains(searchResearchGroupAreasAsProfessorToFindResearchGroup)))
                        .ToListAsync();

                    // Use HashSet to prevent duplicates
                    var suggestionsSet = new HashSet<string>();

                    foreach (var area in allAreas)
                    {
                        // Add the main area name if it matches
                        if (area.AreaName.Contains(searchResearchGroupAreasAsProfessorToFindResearchGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestionsSet.Add(area.AreaName);
                        }

                        // Process subfields - ONLY add if the subfield itself matches
                        if (!string.IsNullOrEmpty(area.AreaSubFields))
                        {
                            var subfields = area.AreaSubFields
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(sub => sub.Trim())
                                .Where(sub => !string.IsNullOrEmpty(sub) &&
                                            sub.Contains(searchResearchGroupAreasAsProfessorToFindResearchGroup, StringComparison.OrdinalIgnoreCase));

                            foreach (var subfield in subfields)
                            {
                                // Use " - " as separator
                                var combination = $"{area.AreaName} - {subfield}";
                                suggestionsSet.Add(combination);
                            }
                        }
                    }

                    professorResearchGroupAreasSuggestions = suggestionsSet
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Πρόβλημα στην Ανάκτηση Περιοχών Έρευνας: {ex.Message}");
                    professorResearchGroupAreasSuggestions = new List<string>();
                }
            }
            else
            {
                professorResearchGroupAreasSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void SelectProfessorResearchGroupAreasSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !professorSelectedResearchGroupAreas.Contains(suggestion))
            {
                professorSelectedResearchGroupAreas.Add(suggestion);
                professorResearchGroupAreasSuggestions.Clear();
                searchResearchGroupAreasAsProfessorToFindResearchGroup = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedResearchGroupArea(string area)
        {
            professorSelectedResearchGroupAreas.Remove(area);
            StateHasChanged();
        }

        private void RemoveProfessorSelectedResearchGroupArea(string area)
        {
            RemoveSelectedResearchGroupArea(area);
        }

        private void HandleProfessorResearchGroupAreasKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "Tab")
            {
                if (!string.IsNullOrWhiteSpace(searchResearchGroupAreasAsProfessorToFindResearchGroup) &&
                    !professorSelectedResearchGroupAreas.Contains(searchResearchGroupAreasAsProfessorToFindResearchGroup))
                {
                    professorSelectedResearchGroupAreas.Add(searchResearchGroupAreasAsProfessorToFindResearchGroup);
                    searchResearchGroupAreasAsProfessorToFindResearchGroup = string.Empty;
                    professorResearchGroupAreasSuggestions.Clear();
                }
            }
        }

        private async Task HandleProfessorResearchGroupSkillsInput(ChangeEventArgs e)
        {
            searchResearchGroupSkillsAsProfessorToFindResearchGroup = e.Value?.ToString().Trim() ?? string.Empty;
            professorResearchGroupSkillsSuggestions = new List<string>();

            if (searchResearchGroupSkillsAsProfessorToFindResearchGroup.Length >= 1)
            {
                try
                {
                    professorResearchGroupSkillsSuggestions = await dbContext.Skills
                        .Where(s => s.SkillName.Contains(searchResearchGroupSkillsAsProfessorToFindResearchGroup))
                        .Select(s => s.SkillName)
                        .Distinct()
                        .Take(10)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Πρόβλημα στην Ανάκτηση Τεχνολογιών: {ex.Message}");
                    professorResearchGroupSkillsSuggestions = new List<string>();
                }
            }
            else
            {
                professorResearchGroupSkillsSuggestions.Clear();
            }

            StateHasChanged();
        }

        private void HandleProfessorResearchGroupSkillsKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "Tab")
            {
                if (!string.IsNullOrWhiteSpace(searchResearchGroupSkillsAsProfessorToFindResearchGroup) &&
                    !professorSelectedResearchGroupSkills.Contains(searchResearchGroupSkillsAsProfessorToFindResearchGroup))
                {
                    professorSelectedResearchGroupSkills.Add(searchResearchGroupSkillsAsProfessorToFindResearchGroup);
                    searchResearchGroupSkillsAsProfessorToFindResearchGroup = string.Empty;
                    professorResearchGroupSkillsSuggestions.Clear();
                }
            }
        }

        private void SelectProfessorResearchGroupSkillsSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !professorSelectedResearchGroupSkills.Contains(suggestion))
            {
                professorSelectedResearchGroupSkills.Add(suggestion);
                professorResearchGroupSkillsSuggestions.Clear();
                searchResearchGroupSkillsAsProfessorToFindResearchGroup = string.Empty;
            }
        }

        private void RemoveProfessorSelectedResearchGroupSkill(string skill)
        {
            professorSelectedResearchGroupSkills.Remove(skill);
            StateHasChanged();
        }

        private async Task SearchResearchGroupsAsProfessor()
        {
            await SearchResearchGroupsAsProfessorToFindResearchGroup();
        }

        private async Task SearchResearchGroupsAsProfessorToFindResearchGroup()
        {
            try
            {
                professorHasSearchedForResearchGroups = true;

                // First get all research groups from the database
                var allResearchGroups = await dbContext.ResearchGroups.ToListAsync();

                // Then filter on the client side
                var filteredResearchGroups = allResearchGroups.AsEnumerable();

                if (!string.IsNullOrEmpty(searchResearchGroupNameAsProfessorToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupName.Contains(searchResearchGroupNameAsProfessorToFindResearchGroup, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(searchResearchGroupSchoolAsProfessorToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupSchool == searchResearchGroupSchoolAsProfessorToFindResearchGroup);
                }

                if (!string.IsNullOrEmpty(searchResearchGroupUniversityDepartmentAsProfessorToFindResearchGroup))
                {
                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => rg.ResearchGroupUniversityDepartment == searchResearchGroupUniversityDepartmentAsProfessorToFindResearchGroup);
                }

                // UPDATED AREAS FILTER: Support both selected areas AND manual text input
                if (professorSelectedResearchGroupAreas.Any() || !string.IsNullOrEmpty(searchResearchGroupAreasAsProfessorToFindResearchGroup))
                {
                    // Create a combined list of search terms
                    var areaSearchTerms = new List<string>();

                    // Add manually typed text (if any)
                    if (!string.IsNullOrEmpty(searchResearchGroupAreasAsProfessorToFindResearchGroup))
                    {
                        areaSearchTerms.Add(searchResearchGroupAreasAsProfessorToFindResearchGroup.Trim());
                    }

                    // Add selected areas from dropdown
                    areaSearchTerms.AddRange(professorSelectedResearchGroupAreas);

                    // Remove duplicates and empty entries
                    areaSearchTerms = areaSearchTerms.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();

                    filteredResearchGroups = filteredResearchGroups
                        .Where(rg => areaSearchTerms.Any(area =>
                        {
                            var researchGroupAreas = rg.ResearchGroupAreas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(a => a.Trim())
                                .ToList();

                            // Check if any research group area contains the search term
                            return researchGroupAreas.Any(researchArea =>
                                researchArea.Contains(area, StringComparison.OrdinalIgnoreCase) ||
                                area.Contains(researchArea, StringComparison.OrdinalIgnoreCase));
                        }));
                }

                professorSearchResultsToFindResearchGroup = filteredResearchGroups.ToList();
                professorCurrentResearchGroupPage_SearchForResearchGroups = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Πρόβλημα στην Αναζήτηση Ερευνητικών Ομάδων: {ex.Message}");
                professorSearchResultsToFindResearchGroup = new List<QuizManager.Models.ResearchGroup>();
            }

            StateHasChanged();
        }

        private void ClearProfessorSearchFieldsToFindResearchGroup()
        {
            searchResearchGroupNameAsProfessorToFindResearchGroup = "";
            searchResearchGroupSchoolAsProfessorToFindResearchGroup = "";
            searchResearchGroupUniversityDepartmentAsProfessorToFindResearchGroup = "";
            searchResearchGroupAreasAsProfessorToFindResearchGroup = "";
            searchResearchGroupSkillsAsProfessorToFindResearchGroup = "";
            searchResearchGroupKeywordsAsProfessorToFindResearchGroup = "";
        
            professorSelectedResearchGroupAreas.Clear();
            professorSelectedResearchGroupSkills.Clear();
            professorResearchGroupNameSuggestions.Clear();
            professorResearchGroupAreasSuggestions.Clear();
            professorResearchGroupSkillsSuggestions.Clear();
            professorSearchResultsToFindResearchGroup.Clear();
            professorHasSearchedForResearchGroups = false;
            professorCurrentResearchGroupPage_SearchForResearchGroups = 1;
            StateHasChanged();
        }

        private IEnumerable<QuizManager.Models.ResearchGroup> GetProfessorPaginatedResearchGroupResults()
        {
            var skip = (professorCurrentResearchGroupPage_SearchForResearchGroups - 1) * professorResearchGroupsPerPage_SearchForResearchGroups;
            return professorSearchResultsToFindResearchGroup.Skip(skip).Take(professorResearchGroupsPerPage_SearchForResearchGroups);
        }

        private void OnProfessorPageSizeChange_SearchForResearchGroups(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                professorResearchGroupsPerPage_SearchForResearchGroups = newSize;
                professorCurrentResearchGroupPage_SearchForResearchGroups = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstResearchGroupPage()
        {
            professorCurrentResearchGroupPage_SearchForResearchGroups = 1;
            StateHasChanged();
        }

        private void GoToProfessorFirstResearchGroupPage()
        {
            GoToFirstResearchGroupPage();
        }

        private void PreviousResearchGroupPage()
        {
            if (professorCurrentResearchGroupPage_SearchForResearchGroups > 1)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups--;
                StateHasChanged();
            }
        }

        private void PreviousProfessorResearchGroupPage()
        {
            PreviousResearchGroupPage();
        }

        private void GoToResearchGroupPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= professorTotalResearchGroupPages_SearchForResearchGroups)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups = pageNumber;
                StateHasChanged();
            }
        }

        private void GoToProfessorResearchGroupPage(int pageNumber)
        {
            GoToResearchGroupPage(pageNumber);
        }

        private void NextResearchGroupPage()
        {
            if (professorCurrentResearchGroupPage_SearchForResearchGroups < professorTotalResearchGroupPages_SearchForResearchGroups)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups++;
                StateHasChanged();
            }
        }

        private void NextProfessorResearchGroupPage()
        {
            NextResearchGroupPage();
        }

        private void GoToLastResearchGroupPage()
        {
            professorCurrentResearchGroupPage_SearchForResearchGroups = professorTotalResearchGroupPages_SearchForResearchGroups;
            StateHasChanged();
        }

        private void GoToProfessorLastResearchGroupPage()
        {
            GoToLastResearchGroupPage();
        }

        private IEnumerable<int> GetProfessorVisibleResearchGroupPages()
        {
            var currentPage = professorCurrentResearchGroupPage_SearchForResearchGroups;
            var totalPages = professorTotalResearchGroupPages_SearchForResearchGroups;
            var pages = new List<int>();

            if (totalPages <= 7)
            {
                for (int i = 1; i <= totalPages; i++)
                    pages.Add(i);
            }
            else
            {
                if (currentPage <= 4)
                {
                    for (int i = 1; i <= 5; i++)
                        pages.Add(i);
                    pages.Add(-1); // Ellipsis
                    pages.Add(totalPages);
                }
                else if (currentPage >= totalPages - 3)
                {
                    pages.Add(1);
                    pages.Add(-1); // Ellipsis
                    for (int i = totalPages - 4; i <= totalPages; i++)
                        pages.Add(i);
                }
                else
                {
                    pages.Add(1);
                    pages.Add(-1); // Ellipsis
                    for (int i = currentPage - 1; i <= currentPage + 1; i++)
                        pages.Add(i);
                    pages.Add(-1); // Ellipsis
                    pages.Add(totalPages);
                }
            }

            return pages;
        }

        // Helper Methods
        private void ClearProfessorEventField(int fieldNumber)
        {
            if (currentProfessorEvent == null) return;
            switch (fieldNumber)
            {
                case 1:
                    currentProfessorEvent.ProfessorEventStartingPointLocationToTransportPeopleToEvent1 = string.Empty;
                    break;
                case 2:
                    currentProfessorEvent.ProfessorEventStartingPointLocationToTransportPeopleToEvent2 = string.Empty;
                    break;
                case 3:
                    currentProfessorEvent.ProfessorEventStartingPointLocationToTransportPeopleToEvent3 = string.Empty;
                    break;
            }
            StateHasChanged();
        }

        private void CloseCompanyDetailsModalAtProfessorEventInterest()
        {
            showModalForCompaniesAtProfessorEventInterest = false;
            selectedCompanyToShowDetailsForInterestinProfessorEvent = null;
            StateHasChanged();
        }

        // Additional Missing Properties
        private List<Professor> facultyMembers = new List<Professor>();
        private List<Student> nonFacultyMembers = new List<Student>();
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

        private async Task LoadResearchGroupDetailsData(string researchGroupEmail)
        {
            try
            {
                // Load Faculty Members
                var facultyMembersData = await dbContext.ResearchGroup_Professors
                    .Where(rp => rp.PK_ResearchGroupEmail == researchGroupEmail)
                    .Join(dbContext.Professors,
                        rp => rp.PK_ProfessorEmail,
                        p => p.ProfEmail,
                        (rp, p) => p)
                    .ToListAsync();

                professorFacultyMembers = facultyMembersData;

                // Load Non-Faculty Members
                var nonFacultyMembersData = await dbContext.ResearchGroup_NonFacultyMembers
                    .Where(rn => rn.PK_ResearchGroupEmail == researchGroupEmail)
                    .Join(dbContext.Students,
                        rn => rn.PK_NonFacultyMemberEmail,
                        s => s.Email,
                        (rn, s) => s)
                    .ToListAsync();

                professorNonFacultyMembers = nonFacultyMembersData;

                // Load Spin-off Companies
                // Note: ResearchGroup_SpinOffCompany table may not join directly with Companies table
                // Loading just the company information from ResearchGroup_SpinOffCompany for now
                var spinOffCompanyData = await dbContext.ResearchGroup_SpinOffCompany
                    .Where(s => s.ResearchGroupEmail == researchGroupEmail)
                    .ToListAsync();
                
                // Try to find companies by AFM if possible
                // If CompanyAFM doesn't exist, we'll need to adjust this logic
                professorSpinOffCompanies = new List<Company>();
                foreach (var spinOff in spinOffCompanyData)
                {
                    var company = await dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyName == spinOff.ResearchGroup_SpinOff_CompanyTitle);
                    if (company != null)
                    {
                        professorSpinOffCompanies.Add(company);
                    }
                }

                // Count Active Research Actions
                professorActiveResearchActionsCount = await dbContext.ResearchGroup_ResearchActions
                    .Where(r => r.ResearchGroupEmail == researchGroupEmail && 
                            r.ResearchGroup_ProjectStatus == "OnGoing")
                    .CountAsync();

                // Count Patents (assuming there's a patents table or count field)
                professorPatentsCount = 0; // Placeholder - adjust based on actual data model
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading research group details: {ex.Message}");
            }
        }

        private async Task ShowProfessorResearchGroupDetailsModal(ResearchGroup researchGroup)
        {
            selectedResearchGroupWhenSearchForResearchGroupsAsProfessor = researchGroup;
            showResearchGroupDetailsModalWhenSearchForResearchGroupsAsProfessor = true;
        
            // Load additional data
            if (!string.IsNullOrEmpty(researchGroup.ResearchGroupEmail))
            {
                await LoadResearchGroupDetailsData(researchGroup.ResearchGroupEmail);
            }
        
            StateHasChanged();
        }

        private void CloseModalResearchGroupDetailsOnEyeIconWhenSearchForResearchGroupsAsProfessor()
        {
            showResearchGroupDetailsModalWhenSearchForResearchGroupsAsProfessor = false;
            selectedResearchGroupWhenSearchForResearchGroupsAsProfessor = null;
            professorFacultyMembers.Clear();
            professorNonFacultyMembers.Clear();
            professorSpinOffCompanies.Clear();
            professorPatentsCount = 0;
            professorActiveResearchActionsCount = 0;
            StateHasChanged();
        }

        private void CloseCompanyDetailsModalWhenSearchAsProfessor()
        {
            showCompanyDetailsModal = false;
            selectedCompanyNameAsHyperlinkToShowDetailsToTheProfessor = null;
            isCompanyDetailModalVisibleForHyperlinkNameToShowCompanyDetailsToTheProfessor = false;
            StateHasChanged();
        }

        private bool isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;

        private void CloseCompanyModalToShowCompanyDetailsFromHyperlinkNameToTheProfessor()
        {
            isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;
            selectedCompanyNameAsHyperlinkToShowDetailsToTheProfessor = null;
            StateHasChanged();
        }

        private void OpenUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                NavigationManager.NavigateTo(url, true);
            }
        }

        private void OpenMap(string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                var encodedLocation = Uri.EscapeDataString(location);
                var googleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={encodedLocation}";
                NavigationManager.NavigateTo(googleMapsUrl, true);
            }
        }

        // Placeholder methods for edit modals (these may belong to other sections but are referenced in Razor)
        private void CloseEditModalForThesesAsProfessor()
        {
            isEditModalVisibleForThesesAsProfessor = false;
            currentThesisAsProfessor = null;
            StateHasChanged();
        }

        private void CloseEditPopupForProfessorInternships()
        {
            isEditPopupVisibleForProfessorInternships = false;
            selectedProfessorInternship = null;
            StateHasChanged();
        }

        // Skills property for edit modal
        private List<Skill> Skills = new();

        // Selected skills/areas for edit
        private HashSet<Skill> SelectedSkillsToEditForProfessorThesis = new HashSet<Skill>();
        private List<Area> SelectedAreasToEditForProfessorThesis = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForEditProfessorThesis = new Dictionary<string, List<string>>();

        private void ToggleCheckboxesForEditProfessorThesis()
        {
            showCheckboxesForEditProfessorThesis = !showCheckboxesForEditProfessorThesis;
            StateHasChanged();
        }

        private void ToggleSkillsInEditProfessorThesisModal()
        {
            showExpandedSkillsInProfessorThesisEditModal = !showExpandedSkillsInProfessorThesisEditModal;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditProfessorThesis(ChangeEventArgs e, Area area)
        {
            if ((bool)e.Value!)
            {
                if (!SelectedAreasToEditForProfessorThesis.Contains(area))
                {
                    SelectedAreasToEditForProfessorThesis.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForProfessorThesis.Remove(area);
                if (SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditProfessorThesis(Area area)
        {
            return SelectedAreasToEditForProfessorThesis.Contains(area);
        }

        private void ToggleSubFieldsForEditProfessorThesis(Area area)
        {
            if (ExpandedAreasForEditProfessorThesis.Contains(area.Id))
            {
                ExpandedAreasForEditProfessorThesis.Remove(area.Id);
            }
            else
            {
                ExpandedAreasForEditProfessorThesis.Add(area.Id);
            }
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditProfessorThesis(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;

            if (!SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditProfessorThesis[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForEditProfessorThesis[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForEditProfessorThesis[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForEditProfessorThesis[area.AreaName].Remove(subField);
                if (!SelectedSubFieldsForEditProfessorThesis[area.AreaName].Any())
                {
                    SelectedSubFieldsForEditProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditProfessorThesis(Area area, string subField)
        {
            return SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName) &&
                SelectedSubFieldsForEditProfessorThesis[area.AreaName].Contains(subField);
        }

        private void OnCheckedChangedForEditProfessorThesisSkills(ChangeEventArgs e, Skill skill)
        {
            if ((bool)e.Value!)
            {
                SelectedSkillsToEditForProfessorThesis.Add(skill);
            }
            else
            {
                SelectedSkillsToEditForProfessorThesis.Remove(skill);
            }
            StateHasChanged();
        }

        private async Task HandleProfessorThesisFileUpload(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
        {
            // Placeholder implementation
            await Task.CompletedTask;
        }

        private async Task UpdateThesisAsProfessor()
        {
            // Placeholder implementation
            await Task.CompletedTask;
        }

        private async Task UpdateThesisAsProfessor(ProfessorThesis thesis)
        {
            currentThesisAsProfessor = thesis;
            await UpdateThesisAsProfessor();
        }

        // Missing Methods for Professor Internship Edit Modal
        private void ToggleCheckboxesForEditProfessorInternship()
        {
            showCheckboxesForProfessorInternship = !showCheckboxesForProfessorInternship;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditProfessorInternship(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;
            if (isChecked)
            {
                if (!SelectedAreasToEditForProfessorInternship.Contains(area))
                {
                    SelectedAreasToEditForProfessorInternship.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForProfessorInternship.Remove(area);
                if (SelectedSubFieldsForEditProfessorInternship.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditProfessorInternship.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditProfessorInternship(Area area)
        {
            return SelectedAreasToEditForProfessorInternship.Contains(area);
        }

        private void ToggleSubFieldsForEditProfessorInternship(Area area)
        {
            if (ExpandedAreasForProfessorInternship.Contains(area.Id))
                ExpandedAreasForProfessorInternship.Remove(area.Id);
            else
                ExpandedAreasForProfessorInternship.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditProfessorInternship(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;
            if (!SelectedSubFieldsForEditProfessorInternship.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditProfessorInternship[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForEditProfessorInternship[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForEditProfessorInternship[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForEditProfessorInternship[area.AreaName].Remove(subField);
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditProfessorInternship(Area area, string subField)
        {
            return SelectedSubFieldsForEditProfessorInternship.ContainsKey(area.AreaName) && 
                SelectedSubFieldsForEditProfessorInternship[area.AreaName].Contains(subField);
        }

        private async Task HandleFileUploadToEditProfessorInternshipAttachment(InputFileChangeEventArgs e)
        {
            // TODO: Implement file upload
            await Task.CompletedTask;
        }

        private async Task SaveEditedProfessorInternship()
        {
            // TODO: Implement save logic
            await Task.CompletedTask;
        }

        // Missing Methods for Professor Event Edit Modal
        private void ToggleCheckboxesForEditProfessorEvent()
        {
            showCheckboxesForProfessorEvent = !showCheckboxesForProfessorEvent;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditProfessorEvent(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;
            if (isChecked)
            {
                if (!SelectedAreasToEditForProfessorEvent.Contains(area))
                {
                    SelectedAreasToEditForProfessorEvent.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForProfessorEvent.Remove(area);
                if (SelectedSubFieldsForProfessorEvent.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForProfessorEvent.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditProfessorEvent(Area area)
        {
            return SelectedAreasToEditForProfessorEvent.Contains(area);
        }

        private void ToggleSubFieldsForEditProfessorEvent(Area area)
        {
            if (ExpandedAreasForProfessorEvent.Contains(area.Id))
                ExpandedAreasForProfessorEvent.Remove(area.Id);
            else
                ExpandedAreasForProfessorEvent.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditProfessorEvent(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;
            if (!SelectedSubFieldsForProfessorEvent.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForProfessorEvent[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForProfessorEvent[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForProfessorEvent[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForProfessorEvent[area.AreaName].Remove(subField);
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditProfessorEvent(Area area, string subField)
        {
            return SelectedSubFieldsForProfessorEvent.ContainsKey(area.AreaName) && 
                SelectedSubFieldsForProfessorEvent[area.AreaName].Contains(subField);
        }

        private async Task HandleFileUploadToEditProfessorEventAttachment(InputFileChangeEventArgs e)
        {
            // TODO: Implement file upload
            await Task.CompletedTask;
        }

        private void CloseEditModalForProfessorEvent()
        {
            isEditModalVisibleForEventsAsProfessor = false;
            StateHasChanged();
        }

        private async Task UpdateProfessorEvent()
        {
            // TODO: Implement update logic
            await Task.CompletedTask;
        }

        private async Task UpdateProfessorEvent(ProfessorEvent professorEvent)
        {
            currentProfessorEvent = professorEvent;
            await UpdateProfessorEvent();
        }

        private void CloseStudentDetailsModalAtProfessorEventInterest()
        {
            showModalForStudentsAtProfessorEventInterest = false;
            StateHasChanged();
        }

        // Additional missing properties (checking for duplicates)
        private Dictionary<string, Company> companyDataCache = new Dictionary<string, Company>();
        private bool showCheckboxesForProfessorInternship = false;
        private List<Area> SelectedAreasToEditForProfessorInternship = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForEditProfessorInternship = new Dictionary<string, List<string>>();
        private HashSet<int> ExpandedAreasForProfessorInternship = new HashSet<int>();
        private bool showCheckboxesForProfessorEvent = false;
        private List<Area> SelectedAreasToEditForProfessorEvent = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForProfessorEvent = new Dictionary<string, List<string>>();
        private HashSet<int> ExpandedAreasForProfessorEvent = new HashSet<int>();
        private bool uploadSuccess = false;
        private string uploadErrorMessage = string.Empty;
    }
}
