using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services.StudentDashboard;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentInternshipsSection : ComponentBase
    {
        [Inject] private IStudentDashboardService StudentDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // Internships Display Visibility
        private bool showStudentInternshipApplications = false;
        private bool isLoadingStudentInternshipApplications = false;

        // Internship Applications Data
        private List<InternshipApplied> internshipApplications = new List<InternshipApplied>();
        private List<ProfessorInternshipApplied> professorInternshipApplications = new List<ProfessorInternshipApplied>();
        private bool showCompanyInternshipApplications = true;
        private bool showProfessorInternshipApplications = true;
        private Dictionary<long, CompanyInternship> internshipDataCache = new Dictionary<long, CompanyInternship>();
        private Dictionary<long, ProfessorInternship> professorInternshipDataCache = new Dictionary<long, ProfessorInternship>();
        private List<AllInternships> publishedInternships = new List<AllInternships>();
        private int totalInternshipCountForInternshipsToSee = 0;
        private StudentDashboardData _dashboardData = StudentDashboardData.Empty;

        // Pagination for Applications
        private int currentPageForInternshipsToSee = 1;
        private int pageSizeForInternshipsToSee = 10;
        private int totalPagesForInternshipsToSee = 1;
        private int[] pageSizeOptions_SeeMyInternshipApplicationsAsStudent = new[] { 10, 50, 100 };

        // Withdraw Application
        private bool showLoadingModalWhenWithdrawInternshipApplication = false;
        private int loadingProgressWhenWithdrawInternshipApplication = 0;

        // Company Details Modals
        private QuizManager.Models.Company selectedCompanyDetails_StudentInternshipApplications;
        private bool showCompanyDetailsModal_StudentInternshipApplications = false;
        private Dictionary<string, QuizManager.Models.Company> companyDataCache = new Dictionary<string, QuizManager.Models.Company>();

        // Internship Details Modals
        private CompanyInternship selectedCompanyInternshipDetails_StudentInternshipApplications;
        private bool showCompanyInternshipDetailsModal_StudentInternshipApplications = false;

        // Company Details from Internship Display (Search Results)
        private QuizManager.Models.Company selectedCompanyDetailsFromInternship;
        private bool showCompanyDetailsModalFromInternship = false;

        // Selected Entities for Details
        private QuizManager.Models.Company selectedCompany;
        private QuizManager.Models.Professor selectedProfessor;
        private CompanyInternship selectedCompanyInternshipDetails;
        private CompanyInternship currentInternship;
        private ProfessorInternship currentProfessorInternship;

        // Details for Hyperlink Names
        private QuizManager.Models.Company selectedCompanyDetailsForHyperlinkNameInInternshipAsStudent;
        private QuizManager.Models.Professor selectedProfessorDetailsForHyperlinkNameInInternshipAsStudent;

        // Search Filters
        private bool isInternshipSearchAsStudentFiltersVisible = false;
        private bool isInternshipAreasVisible = false;
        private HashSet<int> expandedInternshipAreas = new HashSet<int>();
        private string globalInternshipSearch = string.Empty;
        private string companyinternshipSearch = string.Empty;
        private string companyinternshipSearchByType = string.Empty;
        private string companyinternshipSearchByESPA = string.Empty;
        private string companyinternshipSearchByRegion = "";
        private string companyinternshipSearchByTown = string.Empty;
        private bool companyinternshipSearchByTransportOffer;
        private DateTime? finishEstimationDateToSearchInternship = null;
        private List<string> internshipTitleAutocompleteSuggestionsWhenSearchInternshipAsStudent = new List<string>();
        private bool isLoadingSearchInternshipsAsStudent = false;

        // Pagination for Search
        private int currentInternshipPage = 1;
        private int totalInternshipPages = 1;

        // Region to Towns Map
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

        // Computed Properties
        private List<object> allInternshipApplications
        {
            get
            {
                var all = new List<object>();
                if (showCompanyInternshipApplications && internshipApplications != null)
                    all.AddRange(internshipApplications);
                if (showProfessorInternshipApplications && professorInternshipApplications != null)
                    all.AddRange(professorInternshipApplications);
                return all;
            }
        }

        private List<object> paginatedInternships
        {
            get
            {
                return allInternshipApplications
                    .Skip((currentPageForInternshipsToSee - 1) * pageSizeForInternshipsToSee)
                    .Take(pageSizeForInternshipsToSee)
                    .ToList();
            }
        }

        // Main Methods
        private async Task ToggleAndLoadStudentInternshipApplications()
        {
            showStudentInternshipApplications = !showStudentInternshipApplications;

            if (showStudentInternshipApplications)
            {
                isLoadingStudentInternshipApplications = true;
                StateHasChanged();
            
                try
                {
                    await LoadUserInternshipApplications();
                }
                finally
                {
                    isLoadingStudentInternshipApplications = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUserInternshipApplications()
        {
            _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
            internshipApplications = _dashboardData.CompanyInternshipApplications.ToList();
            professorInternshipApplications = _dashboardData.ProfessorInternshipApplications.ToList();

            // Build caches from DB to keep full model details
            internshipDataCache.Clear();
            professorInternshipDataCache.Clear();

            var companyIds = internshipApplications.Select(a => a.RNGForInternshipApplied).Distinct().ToList();
            if (companyIds.Count > 0)
            {
                var companyInternships = await StudentDashboardService.GetCompanyInternshipsByIdsAsync(companyIds);
                foreach (var kvp in companyInternships)
                {
                    internshipDataCache[kvp.Key] = kvp.Value;
                }
            }

            var professorIds = professorInternshipApplications.Select(a => a.RNGForProfessorInternshipApplied).Distinct().ToList();
            if (professorIds.Count > 0)
            {
                var professorInternships = await StudentDashboardService.GetProfessorInternshipsByIdsAsync(professorIds);
                foreach (var kvp in professorInternships)
                {
                    professorInternshipDataCache[kvp.Key] = kvp.Value;
                }
            }

            showStudentInternshipApplications = _dashboardData.IsAuthenticated && _dashboardData.IsRegisteredStudent;

            UpdateTotalInternshipCount();
            StateHasChanged();
        }

        private async Task FilterInternshipApplications(ChangeEventArgs e)
        {
            var filterValue = e.Value?.ToString() ?? "all";

            if (filterValue == "company")
            {
                showCompanyInternshipApplications = true;
                showProfessorInternshipApplications = false;
            }
            else if (filterValue == "professor")
            {
                showCompanyInternshipApplications = false;
                showProfessorInternshipApplications = true;
            }
            else
            {
                showCompanyInternshipApplications = true;
                showProfessorInternshipApplications = true;
            }

            await Task.Delay(100);
            UpdateTotalInternshipCount();
            currentPageForInternshipsToSee = 1;
            StateHasChanged();
        }

        private void SetTotalInternshipCount(int count)
        {
            totalInternshipCountForInternshipsToSee = count;
            UpdatePagination();
        }

        private void UpdateTotalInternshipCount()
        {
            totalInternshipCountForInternshipsToSee = allInternshipApplications.Count;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            totalPagesForInternshipsToSee = (int)Math.Ceiling((double)totalInternshipCountForInternshipsToSee / pageSizeForInternshipsToSee);
            if (totalPagesForInternshipsToSee < 1) totalPagesForInternshipsToSee = 1;
            StateHasChanged();
        }

        // Pagination Methods
        private void GoToFirstPageForInternships()
        {
            currentPageForInternshipsToSee = 1;
            StateHasChanged();
        }

        private void GoToLastPageForInternships()
        {
            currentPageForInternshipsToSee = totalPagesForInternshipsToSee;
            StateHasChanged();
        }

        private void GoToPageForInternships(int page)
        {
            if (page > 0 && page <= totalPagesForInternshipsToSee)
            {
                currentPageForInternshipsToSee = page;
                StateHasChanged();
            }
        }

        private void PreviousPageForInternshipsToSee()
        {
            if (currentPageForInternshipsToSee > 1)
            {
                currentPageForInternshipsToSee--;
                StateHasChanged();
            }
        }

        private void NextPageForInternshipsToSee()
        {
            if (currentPageForInternshipsToSee < totalPagesForInternshipsToSee)
            {
                currentPageForInternshipsToSee++;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForInternships()
        {
            var pages = new List<int>();
            int current = currentPageForInternshipsToSee;
            int total = totalPagesForInternshipsToSee;
        
            pages.Add(1);
            if (current > 3) pages.Add(-1);
        
            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
        
            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }
        
            if (current < total - 2) pages.Add(-1);
        
            if (total > 1) pages.Add(total);
        
            return pages;
        }

        private void OnPageSizeChangeForApplications_SeeMyInternshipApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForInternshipsToSee = newSize;
                currentPageForInternshipsToSee = 1;
                UpdatePagination();
                StateHasChanged();
            }
        }

        // Withdraw Application
        private async Task WithdrawInternshipApplicationMadeByStudent(object applicationObj)
        {
            await WithdrawInternshipApplication(applicationObj);
        }

        private async Task WithdrawInternshipApplication(object applicationObj)
        {
            try
            {
                if (applicationObj is InternshipApplied companyInternship)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawInternshipApplication = true;
                    loadingProgressWhenWithdrawInternshipApplication = 0;
                    StateHasChanged();

                    await UpdateProgressWhenWithdrawInternshipApplication(10, 200);
                    var internship = internshipDataCache.TryGetValue(companyInternship.RNGForInternshipApplied, out var cachedInternship)
                        ? cachedInternship
                        : null;
                    var student = _dashboardData.Student;

                    if (!await StudentDashboardService.WithdrawCompanyInternshipApplicationAsync(companyInternship.RNGForInternshipApplied))
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                        return;
                    }

                    await UpdateProgressWhenWithdrawInternshipApplication(50, 200);
                    await StudentDashboardService.RefreshDashboardCacheAsync();
                    await LoadUserInternshipApplications();

                    await UpdateProgressWhenWithdrawInternshipApplication(80, 200);

                    if (internship != null && student != null)
                    {
                        var companyName = internship.Company?.CompanyName ?? "Άγνωστη Εταιρεία";

                        await InternshipEmailService.SendInternshipWithdrawalNotificationToCompany_AsStudent(
                            companyInternship.CompanyEmailWhereStudentAppliedForInternship,
                            companyName,
                            student.Name,
                            student.Surname,
                            internship.CompanyInternshipTitle,
                            companyInternship.RNGForInternshipAppliedAsStudent_HashedAsUniqueID);

                        await InternshipEmailService.SendInternshipWithdrawalConfirmationToStudent_AsCompany(
                            companyInternship.StudentEmailAppliedForInternship,
                            student.Name,
                            student.Surname,
                            internship.CompanyInternshipTitle,
                            companyInternship.RNGForInternshipAppliedAsStudent_HashedAsUniqueID,
                            companyName);
                    }

                    await UpdateProgressWhenWithdrawInternshipApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawInternshipApplication = false;
                    StateHasChanged();

                    // AFTER user clicks OK, show the navigation loader
                    await Task.Delay(100);
            
                    // Show the navigation loader
                    await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                    // Give time for loader to render
                    await Task.Delay(300);
                
                    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                }
                else if (applicationObj is ProfessorInternshipApplied professorInternship)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawInternshipApplication = true;
                    loadingProgressWhenWithdrawInternshipApplication = 0;
                    StateHasChanged();

                    await UpdateProgressWhenWithdrawInternshipApplication(10, 200);
                    var internship = professorInternshipDataCache.TryGetValue(professorInternship.RNGForProfessorInternshipApplied, out var cachedInternship)
                        ? cachedInternship
                        : null;
                    var student = _dashboardData.Student;

                    if (!await StudentDashboardService.WithdrawProfessorInternshipApplicationAsync(professorInternship.RNGForProfessorInternshipApplied))
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                        return;
                    }

                    await UpdateProgressWhenWithdrawInternshipApplication(50, 200);
                    await StudentDashboardService.RefreshDashboardCacheAsync();
                    await LoadUserInternshipApplications();

                    await UpdateProgressWhenWithdrawInternshipApplication(80, 200);

                    var professorName = internship != null && internship.Professor != null
                        ? $"{internship.Professor.ProfName} {internship.Professor.ProfSurname}"
                        : "Άγνωστος Καθηγητής";

                    // Step 5: Send notifications
                    await UpdateProgressWhenWithdrawInternshipApplication(90, 200);
                
                    if (internship != null && student != null)
                    {
                        await InternshipEmailService.SendProfessorInternshipWithdrawalNotificationToProfessor(
                            professorInternship.ProfessorDetails?.ProfessorEmailWhereStudentAppliedForProfessorInternship
                                ?? professorInternship.ProfessorEmailWhereStudentAppliedForInternship,
                            professorName,
                            student.Name,
                            student.Surname,
                            internship.ProfessorInternshipTitle,
                            professorInternship.RNGForProfessorInternshipApplied_HashedAsUniqueID);

                        await InternshipEmailService.SendProfessorInternshipWithdrawalConfirmationToStudent(
                            professorInternship.StudentEmailAppliedForProfessorInternship,
                            student.Name,
                            student.Surname,
                            internship.ProfessorInternshipTitle,
                            professorInternship.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                            professorName);
                    }

                    await UpdateProgressWhenWithdrawInternshipApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawInternshipApplication = false;
                    StateHasChanged();

                    // AFTER user clicks OK, show the navigation loader
                    await Task.Delay(100);
            
                    // Show the navigation loader
                    await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                    // Give time for loader to render
                    await Task.Delay(300);
                
                    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                }
            }
            catch (Exception ex)
            {
                // Hide loading modal on error
                showLoadingModalWhenWithdrawInternshipApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error withdrawing internship application: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την απόσυρση της αίτησης.");
            }
        }

        private async Task UpdateProgressWhenWithdrawInternshipApplication(int progress, int delayMs = 0)
        {
            loadingProgressWhenWithdrawInternshipApplication = progress;
            StateHasChanged();

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }

        // Company Details Modals
        private async Task ShowCompanyDetailsInInternshipCompanyName_StudentInternshipApplications(string companyEmail)
        {
            try
            {
                if (companyDataCache.TryGetValue(companyEmail, out var cachedCompany))
                {
                    selectedCompanyDetails_StudentInternshipApplications = cachedCompany;
                }
                else
                {
                    selectedCompanyDetails_StudentInternshipApplications = await StudentDashboardService.GetCompanyByEmailAsync(companyEmail);

                    if (selectedCompanyDetails_StudentInternshipApplications != null)
                    { 
                        companyDataCache[companyEmail] = selectedCompanyDetails_StudentInternshipApplications;
                    }
                }

                if (selectedCompanyDetails_StudentInternshipApplications != null)
                {
                    showCompanyDetailsModal_StudentInternshipApplications = true;
                    StateHasChanged();
                }
                else
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία εταιρείας");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading company details: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα φόρτωσης στοιχείων εταιρείας");
            }
        }

        private void CloseCompanyDetailsModal_StudentInternshipApplications()
        {
            showCompanyDetailsModal_StudentInternshipApplications = false;
            StateHasChanged();
        }

        // Internship Details Modals
        private async Task ShowInternshipDetailsInInternshipTitleAsHyperlink(long internshipRNG)
        {
            await ShowCompanyInternshipDetailsModal_StudentInternshipApplications(internshipRNG);
        }

        private async Task ShowCompanyInternshipDetailsModal_StudentInternshipApplications(long internshipRNG)
        {
            selectedCompanyInternshipDetails_StudentInternshipApplications = internshipDataCache.TryGetValue(internshipRNG, out var cached)
                ? cached
                : null;

            if (selectedCompanyInternshipDetails_StudentInternshipApplications != null)
            {
                // Open the modal if the internship details are found
                showCompanyInternshipDetailsModal_StudentInternshipApplications = true;
                StateHasChanged();
            }
            else
            {
                // Show an alert if no internship details are found
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν μπορούν να εμφανιστούν οι λεπτομέρειες της Πρακτικής. <span style='color:darkred;'>Η Πρακτική Δεν Είναι Πλέον Διαθέσιμη από τον Φορέα</span>");
            }
        }

        private void CloseCompanyInternshipDetailsModal_StudentInternshipApplications()
        {
            showCompanyInternshipDetailsModal_StudentInternshipApplications = false;
            StateHasChanged();
        }

        // Company Details from Internship Display (Search Results)
        private async Task ShowCompanyDetailsinTitleAsHyperlink(string companyEmail)
        {
            await ShowCompanyHyperlinkNameDetailsModalInStudentInternships(companyEmail);
        }

        private async Task ShowCompanyHyperlinkNameDetailsModalInStudentInternships(string companyEmail)
        {
            selectedCompanyDetailsFromInternship = await StudentDashboardService.GetCompanyByEmailAsync(companyEmail);

            if (selectedCompanyDetailsFromInternship != null)
            {
                showCompanyDetailsModalFromInternship = true;
                StateHasChanged();
            }
        }

        private void CloseCompanyDetailsModalFromInternship()
        {
            showCompanyDetailsModalFromInternship = false;
            StateHasChanged();
        }

        // Modal Visibility States
        private bool isProfessorDetailsModalVisible_StudentInternshipApplicationsShow = false;
        private bool isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = false;
        private bool isInternshipDetailsModalVisible = false;
        private bool iscompanyDetailsModalVisible = false;
        private Professor selectedProfessorDetailsFromInternship = null;
        private bool showProfessorDetailsModalFromInternship = false;

        // Loading States for Withdrawal
        private bool showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
        private int loadingProgressWhenWithdrawProfessorInternshipApplication = 0;

        // Helper method to get internship details from cache
        private CompanyInternship GetCompanyInternshipFromCache(long rng)
        {
            return internshipDataCache.TryGetValue(rng, out var internship) ? internship : null;
        }

        private ProfessorInternship GetProfessorInternshipFromCache(long rng)
        {
            return professorInternshipDataCache.TryGetValue(rng, out var internship) ? internship : null;
        }

        // Toggle Methods
        private void ToggleInternshipSearchAsStudentFiltersVisibility()
        {
            isInternshipSearchAsStudentFiltersVisible = !isInternshipSearchAsStudentFiltersVisible;
            StateHasChanged();
        }

        // Download Methods
        private async Task DownloadInternshipAttachmentAsStudent(long internshipId)
        {
            var attachment = await StudentDashboardService.GetCompanyInternshipAttachmentAsync(internshipId);
            if (attachment == null)
            {
                await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε συνημμένο αρχείο.");
                return;
            }

            await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", attachment.Value.FileName, "application/pdf", attachment.Value.Data);
        }

        private async Task DownloadStudentCVForProfessorInternships(string studentEmail)
        {
            try
            {
                var student = studentDataCache.Values.FirstOrDefault(s => s.Email == studentEmail);
                if (student == null)
                {
                    student = await StudentDashboardService.GetStudentByEmailAsync(studentEmail);
                    if (student != null)
                    {
                        studentDataCache[studentEmail] = student;
                    }
                }
                if (student?.Attachment == null)
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε βιογραφικό για αυτόν τον φοιτητή");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading CV: {ex.Message}");
            }
        }

        // Show Details Methods
        private async Task ShowStudentDetailsInNameAsHyperlink(string studentUniqueId, int applicationId, string applicationType)
        {
            try
            {
                var student = await StudentDashboardService.GetStudentByUniqueIdAsync(studentUniqueId);
                if (student != null)
                {
                    studentDataCache[student.Email] = student;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing student details: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        // Close Modal Methods
        private void CloseProfessorInternshipDetailsModal_StudentInternshipApplicationsShow()
        {
            isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = false;
            StateHasChanged();
        }

        private void CloseProfessorDetailsModal_StudentInternshipApplications()
        {
            isProfessorDetailsModalVisible_StudentInternshipApplicationsShow = false;
            StateHasChanged();
        }

        private async Task UpdateProgressWhenWithdrawProfessorInternshipApplication(int current, int total)
        {
            loadingProgressWhenWithdrawProfessorInternshipApplication = (int)((double)current / total * 100);
            StateHasChanged();
            await Task.Delay(50);
        }

        private async Task WithdrawProfessorInternshipApplicationMadeByStudent(ProfessorInternshipApplied application)
        {
            try
            {
                var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                    $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                if (!confirmed) return;

                showLoadingModalWhenWithdrawProfessorInternshipApplication = true;
                loadingProgressWhenWithdrawProfessorInternshipApplication = 0;
                StateHasChanged();

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(10, 200);
                var internship = professorInternshipDataCache.TryGetValue(application.RNGForProfessorInternshipApplied, out var cachedInternship)
                    ? cachedInternship
                    : null;
                var student = _dashboardData.Student;

                if (!await StudentDashboardService.WithdrawProfessorInternshipApplicationAsync(application.RNGForProfessorInternshipApplied))
                {
                    showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                    return;
                }

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(50, 200);
                await StudentDashboardService.RefreshDashboardCacheAsync();
                await LoadUserInternshipApplications();

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(80, 200);

                if (internship != null && student != null)
                {
                    var professorName = internship.Professor != null
                        ? $"{internship.Professor.ProfName} {internship.Professor.ProfSurname}"
                        : "Άγνωστος Καθηγητής";

                    await InternshipEmailService.SendProfessorInternshipWithdrawalNotificationToProfessor(
                        application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship,
                        professorName,
                        student.Name,
                        student.Surname,
                        internship.ProfessorInternshipTitle,
                        application.RNGForProfessorInternshipApplied_HashedAsUniqueID);

                    await InternshipEmailService.SendProfessorInternshipWithdrawalConfirmationToStudent(
                        application.StudentDetails.StudentEmailAppliedForProfessorInternship,
                        student.Name,
                        student.Surname,
                        internship.ProfessorInternshipTitle,
                        application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        professorName);
                }

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(100, 200);
                await Task.Delay(500);
                showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                StateHasChanged();
                await Task.Delay(100);
                await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
                await Task.Delay(300);
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error saving withdrawal: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την αποθήκευση της απόσυρσης.");
            }
        }

        // Additional Missing Properties
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private bool showInternships = false;
        private HashSet<string> selectedInternshipAreas = new HashSet<string>();
        private HashSet<string> selectedInternshipSubFields = new HashSet<string>();
        private DateTime? selectedDateToSearchInternship = null;
        private bool searchPerformedForInternships = false;
        private HashSet<long> professorInternshipIdsApplied = new HashSet<long>();
        private bool showLoadingModalWhenApplyForCompanyInternshipAsStudent = false;
        private bool showLoadingModalWhenApplyForProfessorInternshipAsStudent = false;

        // Helper Method
        private string HashLong(long value)
        {
            return HashingHelper.HashLong(value);
        }

        // Modal Close Methods
        private void CloseModalForInternships()
        {
            StateHasChanged();
        }

        private void CloseInternshipDetailsModal()
        {
            StateHasChanged();
        }

        private void CloseModalForCompanyNameHyperlinkDetailsInInternship()
        {
            selectedCompanyDetailsFromInternship = null;
            showCompanyDetailsModalFromInternship = false;
            StateHasChanged();
        }

        private void CloseModalForProfessorNameHyperlinkDetailsInInternship()
        {
            selectedProfessorDetailsFromInternship = null;
            showProfessorDetailsModalFromInternship = false;
            StateHasChanged();
        }

        private void CloseModalforHyperLinkTitle()
        {
            StateHasChanged();
        }

        // Additional Missing Properties
        private List<Area> Areas = new List<Area>();
        private int InternshipsPerPage = 10;
        private int TotalInternshipPages => (int)Math.Ceiling((double)publishedInternships.Count / InternshipsPerPage);

        // Search and Pagination Methods
        private void ClearSearchFieldsForInternshipsAsStudent()
        {
            selectedInternshipAreas.Clear();
            selectedInternshipSubFields.Clear();
            selectedDateToSearchInternship = null;
            StateHasChanged();
        }

        private async Task ConfirmApplyForInternship(CompanyInternship internship)
        {
            var message = $"Πρόκεται να κάνετε αίτηση για την Θέση \"{internship.CompanyInternshipTitle}\". Είστε σίγουρος/η;";
            var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", message);

            if (confirmed)
            {
                // TODO: Wire to ApplyForInternshipAsStudent when service refactor is complete.
            }
        }

        private async Task ConfirmApplyForProfessorInternship(ProfessorInternship internship)
        {
            var message = $"Πρόκεται να κάνετε αίτηση για την Θέση \"{internship.ProfessorInternshipTitle}\". Είστε σίγουρος/η;";
            var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", message);

            if (confirmed)
            {
                // TODO: Wire to ApplyForProfessorInternshipAsStudent when service refactor is complete.
            }
        }

        private CompanyInternship ConvertToCompanyInternship(AllInternships internship)
        {
            return new CompanyInternship
            {
                RNGForInternshipUploadedAsCompany = internship.RNGForCompanyInternship,
                CompanyInternshipTitle = internship.InternshipTitle,
                CompanyInternshipType = internship.InternshipType,
                CompanyInternshipESPA = internship.InternshipFundingType,
                CompanyInternshipActivePeriod = internship.InternshipActivePeriod,
                CompanyInternshipFinishEstimation = internship.InternshipFinishEstimation,
                CompanyUploadedInternshipStatus = internship.InternshipStatus,
                CompanyInternshipTransportOffer = internship.InternshipTransportOffer,
                CompanyInternshipDimosLocation = internship.InternshipDimosLocation,
                CompanyInternshipPerifereiaLocation = internship.InternshipPerifereiaLocation,
                CompanyInternshipDescription = internship.InternshipDescription,
                CompanyInternshipAreas = internship.InternshipAreas,
                CompanyInternshipUploadDate = internship.CompanyInternshipUploadDate
            };
        }

        private ProfessorInternship ConvertToProfessorInternship(AllInternships internship)
        {
            return new ProfessorInternship
            {
                RNGForInternshipUploadedAsProfessor = internship.RNGForProfessorInternship,
                ProfessorInternshipTitle = internship.InternshipTitle,
                ProfessorInternshipType = internship.InternshipType,
                ProfessorInternshipESPA = internship.InternshipFundingType,
                ProfessorInternshipActivePeriod = internship.InternshipActivePeriod,
                ProfessorInternshipFinishEstimation = internship.InternshipFinishEstimation,
                ProfessorUploadedInternshipStatus = internship.InternshipStatus,
                ProfessorInternshipTransportOffer = internship.InternshipTransportOffer,
                ProfessorInternshipDimosLocation = internship.InternshipDimosLocation,
                ProfessorInternshipPerifereiaLocation = internship.InternshipPerifereiaLocation,
                ProfessorInternshipDescription = internship.InternshipDescription,
                ProfessorInternshipAreas = internship.ProfessorInternshipAreas,
                ProfessorInternshipUploadDate = internship.ProfessorInternshipUploadDate
            };
        }

        private IEnumerable<AllInternships> GetPaginatedInternships()
        {
            return publishedInternships
                .Skip((currentInternshipPage - 1) * InternshipsPerPage)
                .Take(InternshipsPerPage);
        }

        private List<int> GetVisibleInternshipPages()
        {
            var pages = new List<int>();
            int current = currentInternshipPage;
            int total = TotalInternshipPages;

            if (total <= 0)
                return pages;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1);

            if (total > 1) pages.Add(total);
            return pages;
        }

        private void GoToFirstInternshipPage()
        {
            currentInternshipPage = 1;
            StateHasChanged();
        }

        private void GoToInternshipPage(int page)
        {
            if (page < 1 || page > TotalInternshipPages)
                return;
            currentInternshipPage = page;
            StateHasChanged();
        }

        private void GoToLastInternshipPage()
        {
            currentInternshipPage = Math.Max(1, TotalInternshipPages);
            StateHasChanged();
        }

        private void NextInternshipPage()
        {
            if (currentInternshipPage < TotalInternshipPages)
                currentInternshipPage++;
            StateHasChanged();
        }

        private void PreviousInternshipPage()
        {
            if (currentInternshipPage > 1)
                currentInternshipPage--;
            StateHasChanged();
        }

        private async Task HandleInternshipTitleAutocompleteInputWhenSearchInternshipAsStudent(ChangeEventArgs e)
        {
            // TODO: Implement autocomplete
            await Task.CompletedTask;
            StateHasChanged();
        }

        private void OnInternshipAreaCheckboxChanged(ChangeEventArgs e, string areaName)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedInternshipAreas.Add(areaName);
            else
                selectedInternshipAreas.Remove(areaName);
            StateHasChanged();
        }

        private void OnInternshipSubFieldCheckboxChanged(ChangeEventArgs e, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedInternshipSubFields.Add(subField);
            else
                selectedInternshipSubFields.Remove(subField);
            StateHasChanged();
        }

        private void OnInternshipFilterChange(ChangeEventArgs e)
        {
            StateHasChanged();
        }

        private void OnPageSizeChange_SearchForInternshipsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                InternshipsPerPage = newSize;
                StateHasChanged();
            }
        }

        private void OnPageSizeChange_SeeMyInternshipApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForInternshipsToSee = newSize;
                StateHasChanged();
            }
        }

        // Missing Methods - extracted from MainLayout.razor.cs.backup
        private async Task ShowProfessorDetailsinTitleAsHyperlink_StudentInternshipApplicationsShow(string professorEmail)
        {
            try
            {
                var professor = await StudentDashboardService.GetProfessorByEmailAsync(professorEmail);
                
                if (professor != null)
                {
                    selectedProfessorDetailsFromInternship = professor;
                    showProfessorDetailsModalFromInternship = true;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading professor details: {ex.Message}");
            }
        }

        private async Task ShowProfessorInternshipDetailsInInternshipTitleAsHyperlink_StudentInternshipApplicationsShow(long internshipId)
        {
            currentProfessorInternship = await StudentDashboardService.GetProfessorInternshipByRngAsync(internshipId);
            
            if (currentProfessorInternship != null)
            {
                isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = true;
                StateHasChanged();
            }
        }

        private void SelectInternshipTitleAutocompleteSuggestionWhenSearchInternshipAsStudent(string suggestion)
        {
            companyinternshipSearch = suggestion;
            internshipTitleAutocompleteSuggestionsWhenSearchInternshipAsStudent.Clear();
            StateHasChanged();
        }

        private void ToggleInternshipAreasVisibility()
        {
            isInternshipAreasVisible = !isInternshipAreasVisible;
            StateHasChanged();
        }

        private void ToggleInternshipSubFields(Area area)
        {
            if (expandedInternshipAreas.Contains(area.Id))
                expandedInternshipAreas.Remove(area.Id);
            else
                expandedInternshipAreas.Add(area.Id);
            StateHasChanged();
        }

        // Additional missing properties (checking for duplicates)
        private HashSet<long> internshipIdsApplied = new HashSet<long>();
        private bool isModalVisibleForInternshipsAsStudent = false;
        private bool isCompanyDetailsModalOpenForHyperlinkNameAsStudentForCompanyInternship = false;
        private int loadingProgressWhenApplyForCompanyInternshipAsStudent = 0;
        private int loadingProgressWhenApplyForProfessorInternshipAsStudent = 0;
        
        // Helper methods
        private string ShowProfileImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return "/images/default-profile.png";
            return $"data:image/png;base64,{Convert.ToBase64String(imageData)}";
        }
        
        private async Task ShowCompanyHyperlinkNameDetailsModalInStudentInternship(string companyEmail)
        {
            var company = await StudentDashboardService.GetCompanyByEmailAsync(companyEmail);
            if (company != null)
            {
                selectedCompanyDetailsForHyperlinkNameInInternshipAsStudent = company;
                showCompanyDetailsModalFromInternship = true;
                StateHasChanged();
            }
        }
        
        private async Task ShowProfessorHyperlinkNameDetailsModalInStudentInternship(string professorEmail)
        {
            var professor = await StudentDashboardService.GetProfessorByEmailAsync(professorEmail);
            if (professor != null)
            {
                selectedProfessorDetailsFromInternship = professor;
                showProfessorDetailsModalFromInternship = true;
                StateHasChanged();
            }
        }
        
        private async Task ShowCompanyInternshipDetailsAsStudent(long internshipRNG)
        {
            var internship = await StudentDashboardService.GetCompanyInternshipByRngAsync(internshipRNG);
            
            if (internship != null)
            {
                selectedCompanyInternshipDetails_StudentInternshipApplications = internship;
                showCompanyInternshipDetailsModal_StudentInternshipApplications = true;
                StateHasChanged();
            }
        }
        
        private async Task ShowProfessorInternshipDetailsAsStudent(long internshipRNG)
        {
            currentProfessorInternship = await StudentDashboardService.GetProfessorInternshipByRngAsync(internshipRNG);
            
            if (currentProfessorInternship != null)
            {
                isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = true;
                StateHasChanged();
            }
        }
        
        // Search method
        private async Task SearchInternshipsAsStudent()
        {
            isLoadingSearchInternshipsAsStudent = true;
            StateHasChanged();
            // TODO: Implement search logic
            await Task.Delay(100);
            isLoadingSearchInternshipsAsStudent = false;
            StateHasChanged();
        }
        
        // Additional missing properties
        private int[] pageSizeOptions_SearchForInternshipsAsStudent = new[] { 10, 50, 100 };
    }
}
