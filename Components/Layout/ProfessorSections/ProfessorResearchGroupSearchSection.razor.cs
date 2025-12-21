using Microsoft.AspNetCore.Components;
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
        private Student selectedStudentToShowDetailsForInterestinProfessorEvent;

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
        private List<string> professorFilteredDepartmentsForSearchForResearchGroup =>
            string.IsNullOrEmpty(searchResearchGroupSchoolAsProfessorToFindResearchGroup)
                ? new List<string>()
                : universityDepartments.ContainsKey(searchResearchGroupSchoolAsProfessorToFindResearchGroup)
                    ? universityDepartments[searchResearchGroupSchoolAsProfessorToFindResearchGroup]
                    : new List<string>();

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

        private void PreviousResearchGroupPage()
        {
            if (professorCurrentResearchGroupPage_SearchForResearchGroups > 1)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups--;
                StateHasChanged();
            }
        }

        private void GoToResearchGroupPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= professorTotalResearchGroupPages_SearchForResearchGroups)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups = pageNumber;
                StateHasChanged();
            }
        }

        private void NextResearchGroupPage()
        {
            if (professorCurrentResearchGroupPage_SearchForResearchGroups < professorTotalResearchGroupPages_SearchForResearchGroups)
            {
                professorCurrentResearchGroupPage_SearchForResearchGroups++;
                StateHasChanged();
            }
        }

        private void GoToLastResearchGroupPage()
        {
            professorCurrentResearchGroupPage_SearchForResearchGroups = professorTotalResearchGroupPages_SearchForResearchGroups;
            StateHasChanged();
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

        // Additional Methods (already defined above, but ensuring they're accessible)
        // These methods are already in the file, so no need to duplicate
    }
}

