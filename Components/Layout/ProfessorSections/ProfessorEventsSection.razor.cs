using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QuizManager.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Services.ProfessorDashboard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ProfessorSections
{
    public partial class ProfessorEventsSection : ComponentBase
    {
        [Inject] private IProfessorDashboardService ProfessorDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;
        private ProfessorDashboardData dashboardData = ProfessorDashboardData.Empty;

        // Events Calendar
        private DateTime currentMonth = DateTime.Today;
        private int firstDayOfMonth => (int)new DateTime(currentMonth.Year, currentMonth.Month, 1).DayOfWeek;
        private int daysInCurrentMonth => DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        private int adjustedFirstDayOfMonth => (firstDayOfMonth == 0) ? 6 : firstDayOfMonth - 1; // Adjust Sunday (0) to Saturday (6) and Monday (1) to 0
        private int selectedDay = 0;
        private int highlightedDay = 0;

        // Events Data
        private Dictionary<int, List<CompanyEvent>> eventsForDate = new Dictionary<int, List<CompanyEvent>>();
        private Dictionary<int, List<ProfessorEvent>> eventsForDateForProfessors = new Dictionary<int, List<ProfessorEvent>>();
        private List<CompanyEvent> CompanyEventsToShowAtFrontPage = new List<CompanyEvent>();
        private List<ProfessorEvent> ProfessorEventsToShowAtFrontPage = new List<ProfessorEvent>();

        // Events Modal
        private bool isModalVisibleToShowEventsOnCalendarForEachClickedDay = false;
        private DateTime? selectedDate;
        private List<CompanyEvent> selectedDateEvents = new List<CompanyEvent>();
        private List<ProfessorEvent> selectedProfessorDateEvents = new List<ProfessorEvent>();
        private string selectedEventFilter = "All";
        private object selectedEvent; // Can be CompanyEvent or ProfessorEvent

        // User Email
        private string CurrentUserEmail = "";

        // Event Details Modal
        private CompanyEvent companyEventDetails;
        private ProfessorEvent professorEventDetails;
        private bool isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;

        // Professor Event Upload Form
        private ProfessorEvent professorEvent = new ProfessorEvent();
        private bool isUploadProfessorEventFormVisible = false;
        private bool showErrorMessageForUploadingProfessorEvent = false;

        // Areas and Skills
        private List<Area> Areas = new();
        private List<Area> SelectedAreasWhenUploadEventAsProfessor = new();
        private bool showCheckboxesForProfessorEvent = false;
        private Dictionary<string, List<string>> SelectedSubFieldsForProfessorEvent = new Dictionary<string, List<string>>();
        private HashSet<int> ExpandedAreasForProfessorEvent = new HashSet<int>();

        // Filtered Events
        private List<ProfessorEvent> FilteredProfessorEvents { get; set; } = new List<ProfessorEvent>();

        // Details for Expanded Interest Modals
        private Company selectedCompanyToSeeDetailsOnExpandedInterestAsProfessor;
        private Student currentStudentDetails = new Student();
        private Company currentCompanyDetails = new Company();

        // Pagination for Professor Events
        private int currentPage_ProfessorEvents = 1;
        private int itemsPerPage_ProfessorEvents = 10;
        private int totalPages_ProfessorEvents =>
            (int)Math.Ceiling((double)(FilteredProfessorEvents?.Count ?? 0) / itemsPerPage_ProfessorEvents);

        // Bulk Edit for Professor Events
        private bool isBulkEditModeForProfessorEvents = false;
        private HashSet<int> selectedProfessorEventIds = new HashSet<int>();
        private string bulkActionForProfessorEvents = "";
        private bool showBulkActionModalForProfessorEvents = false;
        private List<ProfessorEvent> selectedProfessorEventsForAction = new List<ProfessorEvent>();

        // Status Filter
        private string selectedStatusFilterForEventsAsProfessor = "Όλα";

        // Loading States
        private bool isLoadingUploadedEventsAsProfessor = false;
        private bool isLoadingInterestedStudentsForProfessorEvent = false;
        private int loadingProgress = 0;

        // Visibility States
        private bool isUploadedEventsVisibleAsProfessor = false;

        // Current Event/Thesis
        private ProfessorEvent currentProfessorEvent;
        private ProfessorThesis currentProfessorThesis;

        // Modal Visibility States
        private bool isModalVisibleToShowStudentDetailsInNameAsHyperlinkForProfessorThesis = false;
        private bool isModalVisibleToShowProfessorThesisAsProfessor = false;
        private bool isModalVisibleToShowCompanyDetailsAtProfessorThesisInterest = false;

        // Interest Data
        private List<InterestInProfessorEvent> InterestedStudentsForProfessorEvent = new();
        private List<InterestInProfessorEventAsCompany> filteredCompanyInterestForProfessorEvents = new();

        // Data Caches
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private Dictionary<string, Company> companyDataCache = new Dictionary<string, Company>();

        // Messages
        private string saveEventAsProfessorMessage = string.Empty;
        private bool isSaveAnnouncementAsProfessorSuccessful = false;

        // Regions
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

        // Edit Modal
        private bool isEditModalVisibleForEventsAsProfessor = false;

        // Form Validation
        private bool isFormValidToSaveEventAsProfessor = true;
        private int remainingCharactersInEventTitleField = 200;
        private int remainingCharactersInProfessorEventDescription = 2000;
        private string ProfessorEventAttachmentErrorMessage = string.Empty;

        // Event Counts
        private int existingEventsCountToCheckAsProfessor = 0;
        private bool hasExistingEventsOnSelectedDateForProfessor = false;
        private int totalCountEventsAsProfessor = 0;
        private int publishedCountEventsAsProfessor = 0;
        private int unpublishedCountEventsAsProfessor = 0;

        // Loading States for Interest
        private bool isLoadingInterestedCompaniesForProfessorEvent = false;
        private long? loadingProfessorEventRNGForCompanies = null;
        private long? loadingProfessorEventRNGForStudents = null;
        private long? selectedEventIdForCompaniesWhenShowInterestForProfessorEvent = null;
        private long? selectedEventIdForStudentsWhenShowInterestForProfessorEvent = null;

        // Loading Modals
        private bool showLoadingModalForDeleteProfessorEvent = false;
        private bool showLoadingModalForProfessorEvent = false;

        // Bulk Action
        private string newStatusForBulkActionForProfessorEvents = "Μη Δημοσιευμένη";

        // Pagination Options
        private int[] pageSizeOptions_SeeMyUploadedEventsAsProfessor = new[] { 10, 50, 100 };

        // Menu Toggle
        private int? activeProfessorEventMenuId = null;

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

        // Computed Properties
        private List<CompanyEvent> filteredCompanyEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Company"
                ? selectedDateEvents
                : new List<CompanyEvent>();

        private List<ProfessorEvent> filteredProfessorEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Professor"
                ? selectedProfessorDateEvents
                : new List<ProfessorEvent>();

        private List<CompanyEvent> companyEventsForSelectedDate => filteredCompanyEvents;
        private List<ProfessorEvent> professorEventsForSelectedDate => filteredProfessorEvents;

        protected override async Task OnInitializedAsync()
        {
            await LoadInitialData();
            await LoadAreasAsync();
        }

        private async Task LoadInitialData()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
            }

            dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
            CompanyEventsToShowAtFrontPage = dashboardData.CompanyEvents.ToList();
            ProfessorEventsToShowAtFrontPage = dashboardData.Events.ToList();
            LoadEventsForCalendar();
        }

        private void LoadEventsForCalendar()
        {
            eventsForDate.Clear();
            eventsForDateForProfessors.Clear();
            int currentYear = currentMonth.Year;
            int currentMonthNumber = currentMonth.Month;

            // Loop through the events for the current month
            foreach (var eventItem in CompanyEventsToShowAtFrontPage)
            {
                if (eventItem.CompanyEventActiveDate.Year == currentYear &&
                    eventItem.CompanyEventActiveDate.Month == currentMonthNumber)
                {
                    int eventDay = eventItem.CompanyEventActiveDate.Day;
                    if (!eventsForDate.ContainsKey(eventDay))
                    {
                        eventsForDate[eventDay] = new List<CompanyEvent>();
                    }
                    eventsForDate[eventDay].Add(eventItem);
                }
            }

            // Loop through professor events
            foreach (var eventProfessorItem in ProfessorEventsToShowAtFrontPage)
            {
                if (eventProfessorItem.ProfessorEventActiveDate.Year == currentYear &&
                    eventProfessorItem.ProfessorEventActiveDate.Month == currentMonthNumber)
                {
                    int eventDay = eventProfessorItem.ProfessorEventActiveDate.Day;
                    if (!eventsForDateForProfessors.ContainsKey(eventDay))
                    {
                        eventsForDateForProfessors[eventDay] = new List<ProfessorEvent>();
                    }
                    eventsForDateForProfessors[eventDay].Add(eventProfessorItem);
                }
            }

            // If highlighted day is not valid for this month, reset it
            if (highlightedDay != 0 && !eventsForDate.ContainsKey(highlightedDay) && !eventsForDateForProfessors.ContainsKey(highlightedDay))
            {
                highlightedDay = 0;
            }

            // After loading events, ensure the selected and highlighted day is respected
            if (selectedDay != 0 && (eventsForDate.ContainsKey(selectedDay) || eventsForDateForProfessors.ContainsKey(selectedDay)))
            {
                highlightedDay = selectedDay;
            }

            StateHasChanged();
        }

        private void ShowPreviousMonth()
        {
            currentMonth = currentMonth.AddMonths(-1);
            LoadEventsForCalendar();
            StateHasChanged();
        }

        private void ShowNextMonth()
        {
            currentMonth = currentMonth.AddMonths(1);
            LoadEventsForCalendar();
            StateHasChanged();
        }

        private void OnDateClicked(DateTime clickedDate)
        {
            selectedDay = clickedDate.Day;
            highlightedDay = selectedDay;
            selectedDate = clickedDate;

            // Filter company events by status "Δημοσιευμένη" AND the specific date
            selectedDateEvents = CompanyEventsToShowAtFrontPage
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη" &&
                        e.CompanyEventActiveDate.Date == clickedDate.Date)
                .ToList();

            // Filter professor events by status "Δημοσιευμένη" AND the specific date
            selectedProfessorDateEvents = ProfessorEventsToShowAtFrontPage
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη" &&
                        e.ProfessorEventActiveDate.Date == clickedDate.Date)
                .ToList();

            // Only show modal if there are published events for this specific date
            if (selectedDateEvents.Any() || selectedProfessorDateEvents.Any())
            {
                isModalVisibleToShowEventsOnCalendarForEachClickedDay = true;
            }

            StateHasChanged();
        }

        private string GetDayCellClass(DateTime dayDate)
        {
            string dayCellClass = "day-cell";
            if (dayDate.Date == DateTime.Today.Date) dayCellClass += " today";
            if (dayDate.Day == highlightedDay) dayCellClass += " highlighted";
            
            bool hasPublishedEvent = (eventsForDate.ContainsKey(dayDate.Day) && 
                                     eventsForDate[dayDate.Day].Any(e => e.CompanyEventStatus == "Δημοσιευμένη")) ||
                                    (eventsForDateForProfessors.ContainsKey(dayDate.Day) && 
                                     eventsForDateForProfessors[dayDate.Day].Any(e => e.ProfessorEventStatus == "Δημοσιευμένη"));
            
            if (hasPublishedEvent) dayCellClass += " event-day";
            
            return dayCellClass;
        }

        private void CloseModalForCompanyAndProfessorEventTitles()
        {
            isModalVisibleToShowEventsOnCalendarForEachClickedDay = false;
            selectedDateEvents.Clear();
            selectedProfessorDateEvents.Clear();
            selectedEvent = null;
            selectedEventFilter = "All";

            // Re-render the calendar and highlight the selected day
            LoadEventsForCalendar();
            StateHasChanged();
        }

        private void ShowEventDetails(object eventDetails)
        {
            selectedEvent = eventDetails;
            if (eventDetails is CompanyEvent companyEvent)
            {
                companyEventDetails = companyEvent;
            }
            else if (eventDetails is ProfessorEvent professorEvent)
            {
                professorEventDetails = professorEvent;
            }
            StateHasChanged();
        }

        private void CloseEventDetails()
        {
            selectedEvent = null;
            companyEventDetails = null;
            professorEventDetails = null;
            StateHasChanged();
        }

        private async Task<bool> ShowInterestInCompanyEventAsProfessor(CompanyEvent companyEvent)
        {
            // First ask for confirmation
            var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                $"Πρόκεται να δείξετε Ενδιαφέρον για την Εκδήλωση: {companyEvent.CompanyEventTitle} της εταιρείας {companyEvent.Company?.CompanyName}. Είστε σίγουρος/η;");
            if (!confirmed) return false;

            var latest = dashboardData.CompanyEvents.FirstOrDefault(e => e.RNGForEventUploadedAsCompany == companyEvent.RNGForEventUploadedAsCompany);
            if (latest == null || latest.CompanyEventStatus != "Δημοσιευμένη")
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Η Εκδήλωση έχει Αποδημοσιευτεί. Παρακαλώ δοκιμάστε αργότερα.");
                return false;
            }

            var professor = await GetProfessorDetails(CurrentUserEmail);
            if (professor == null)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν βρέθηκαν στοιχεία καθηγητή.");
                return false;
            }

            var company = await GetCompanyDetails(companyEvent.CompanyEmailUsedToUploadEvent);
            if (company == null)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν βρέθηκαν στοιχεία εταιρείας.");
                return false;
            }

            try
            {
                var result = await ProfessorDashboardService.ShowInterestInCompanyEventAsProfessorAsync(companyEvent.RNGForEventUploadedAsCompany, CurrentUserEmail);
                if (!result.Success)
                {
                    await JS.InvokeVoidAsync("confirmActionWithHTML2", result.Error ?? "Αποτυχία καταγραφής ενδιαφέροντος.");
                    return false;
                }

                // Send emails
                try
                {
                    await InternshipEmailService.SendConfirmationToProfessorForInterestInCompanyEvent(
                        professor.ProfEmail,
                        professor.ProfName,
                        professor.ProfSurname,
                        companyEvent.CompanyEventTitle,
                        company.CompanyName,
                        companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? "");

                    await InternshipEmailService.SendNotificationToCompanyForProfessorInterestInEvent(
                        companyEvent.CompanyEmailUsedToUploadEvent,
                        company.CompanyName,
                        professor.ProfName ?? "",
                        professor.ProfSurname ?? "",
                        professor.ProfEmail ?? "",
                        professor.ProfWorkTelephone ?? "",
                        companyEvent.CompanyEventTitle ?? "",
                        companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? "");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending emails: {ex.Message}");
                }

                await JS.InvokeVoidAsync("confirmActionWithHTML2", $"Επιτυχής ένδειξη ενδιαφέροντος για: {companyEvent.CompanyEventTitle}!");
                StateHasChanged();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing interest in company event: {ex.Message}");
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Σφάλμα κατά την ένδειξη ενδιαφέροντος. Παρακαλώ δοκιμάστε ξανά.");
                return false;
            }
        }

        private async Task<QuizManager.Models.Professor?> GetProfessorDetails(string email) =>
            await ProfessorDashboardService.GetProfessorProfileAsync();

        private async Task<QuizManager.Models.Company?> GetCompanyDetails(string email) =>
            await ProfessorDashboardService.GetCompanyByEmailAsync(email);

        public bool HasAlreadyExpressedInterest(CompanyEvent companyEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserEmail) || companyEvent == null)
                return false;

            return false;
        }

        // Professor Event Upload Methods
        private void ToggleFormVisibilityForUploadProfessorEvent()
        {
            isUploadProfessorEventFormVisible = !isUploadProfessorEventFormVisible;
            StateHasChanged();
        }

        private void UpdateOrganizerVisibilityForProfessorEvents(bool value)
        {
            professorEvent.ProfessorEventOtherOrganizerToBeVisible = value;
            StateHasChanged();
        }

        // Areas Loading
        private async Task LoadAreasAsync()
        {
            var lookups = await ProfessorDashboardService.GetLookupsAsync();
            Areas = lookups.Areas.ToList();
        }

        // Area Selection Methods
        private void ToggleCheckboxesForProfessorEvent()
        {
            showCheckboxesForProfessorEvent = !showCheckboxesForProfessorEvent;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForProfessorEvent(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;
        
            if (isChecked)
            {
                if (!SelectedAreasWhenUploadEventAsProfessor.Contains(area))
                {
                    SelectedAreasWhenUploadEventAsProfessor.Add(area);
                }
            }
            else
            {
                SelectedAreasWhenUploadEventAsProfessor.Remove(area);
            
                // Remove all subfields for this area when area is deselected
                if (SelectedSubFieldsForProfessorEvent.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForProfessorEvent.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForProfessorEvent(Area area)
        {
            return SelectedAreasWhenUploadEventAsProfessor.Contains(area);
        }

        private void ToggleSubFieldsForProfessorEvent(Area area)
        {
            if (ExpandedAreasForProfessorEvent.Contains(area.Id))
            {
                ExpandedAreasForProfessorEvent.Remove(area.Id);
            }
            else
            {
                ExpandedAreasForProfessorEvent.Add(area.Id);
            }
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForProfessorEvent(ChangeEventArgs e, Area area, string subField)
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
            
                // Remove the area from subfields dictionary if no subfields are selected
                if (!SelectedSubFieldsForProfessorEvent[area.AreaName].Any())
                {
                    SelectedSubFieldsForProfessorEvent.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForProfessorEvent(Area area, string subField)
        {
            return SelectedSubFieldsForProfessorEvent.ContainsKey(area.AreaName) && 
                SelectedSubFieldsForProfessorEvent[area.AreaName].Contains(subField);
        }

        private bool HasAnySelectionForProfessorEvent()
        {
            return SelectedAreasWhenUploadEventAsProfessor.Any() || SelectedSubFieldsForProfessorEvent.Any();
        }

        // Pagination Methods
        private IEnumerable<ProfessorEvent> GetPaginatedProfessorEvents()
        {
            return FilteredProfessorEvents?
                .Skip((currentPage_ProfessorEvents - 1) * itemsPerPage_ProfessorEvents)
                .Take(itemsPerPage_ProfessorEvents) ?? Enumerable.Empty<ProfessorEvent>();
        }

        private List<int> GetVisiblePages_ProfessorEvents()
        {
            var pages = new List<int>();
            int current = currentPage_ProfessorEvents;
            int total = totalPages_ProfessorEvents;

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

        private void GoToFirstPage_ProfessorEvents() => currentPage_ProfessorEvents = 1;
        private void GoToLastPage_ProfessorEvents() => currentPage_ProfessorEvents = totalPages_ProfessorEvents;
        private void PreviousPage_ProfessorEvents()
        {
            if (currentPage_ProfessorEvents > 1) currentPage_ProfessorEvents--;
        }
        private void NextPage_ProfessorEvents()
        {
            if (currentPage_ProfessorEvents < totalPages_ProfessorEvents) currentPage_ProfessorEvents++;
        }
        private void GoToPage_ProfessorEvents(int page)
        {
            if (page >= 1 && page <= totalPages_ProfessorEvents)
                currentPage_ProfessorEvents = page;
        }

        // Menu Toggle
        private void ToggleProfessorEventMenu(int eventId)
        {
            if (activeProfessorEventMenuId == eventId)
            {
                activeProfessorEventMenuId = null;
            }
            else
            {
                activeProfessorEventMenuId = eventId;
            }
            StateHasChanged();
        }

        // Helper Methods
        private List<string> GetTownsForRegion(string region)
        {
            if (string.IsNullOrEmpty(region) || !RegionToTownsMap.ContainsKey(region))
            {
                return new List<string>();
            }

            return RegionToTownsMap[region];
        }

        // Bulk Edit Methods
        private void EnableBulkEditModeForProfessorEvents()
        {
            isBulkEditModeForProfessorEvents = true;
            selectedProfessorEventIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorEvents()
        {
            isBulkEditModeForProfessorEvents = false;
            selectedProfessorEventIds.Clear();
            StateHasChanged();
        }

        private void ToggleProfessorEventSelection(int eventId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                selectedProfessorEventIds.Add(eventId);
            }
            else
            {
                selectedProfessorEventIds.Remove(eventId);
            }
            StateHasChanged();
        }

        private void CloseBulkActionModalForProfessorEvents()
        {
            showBulkActionModalForProfessorEvents = false;
            bulkActionForProfessorEvents = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForProfessorEvents()
        {
            // Implementation needed - complex method with status/copy logic
            // For now, just close modal
            CloseBulkActionModalForProfessorEvents();
            await Task.CompletedTask;
        }

        private async Task ExecuteBulkStatusChangeForProfessorEvents(string newStatus)
        {
            if (selectedProfessorEventIds.Count == 0) return;
            bulkActionForProfessorEvents = "status";
            selectedProfessorEventsForAction = FilteredProfessorEvents
                .Where(ev => selectedProfessorEventIds.Contains(ev.Id))
                .ToList();
            await ExecuteBulkActionForProfessorEvents();
        }

        private async Task ExecuteBulkCopyForProfessorEvents()
        {
            if (selectedProfessorEventIds.Count == 0) return;
            bulkActionForProfessorEvents = "copy";
            selectedProfessorEventsForAction = FilteredProfessorEvents
                .Where(ev => selectedProfessorEventIds.Contains(ev.Id))
                .ToList();
            await ExecuteBulkActionForProfessorEvents();
        }

        // Event Management Methods
        private async Task DeleteProfessorEvent(int professoreventId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Εκδήλωση.<br><br>" + 
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                var deleted = await ProfessorDashboardService.DeleteEventAsync(professoreventId);
                if (deleted)
                {
                    dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                    ProfessorEventsToShowAtFrontPage = dashboardData.Events.ToList();
                    FilteredProfessorEvents = ProfessorEventsToShowAtFrontPage.ToList();
                    StateHasChanged();
                }
            }
        }

        private async Task ChangeProfessorEventStatus(int professoreventId, string newStatus)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] 
            { 
                $"Πρόκειται να αλλάξετε την κατάσταση αυτής της Εκδήλωσης σε '{newStatus}'. Είστε σίγουρος/η;" 
            });

            if (isConfirmed)
            {
                var result = await ProfessorDashboardService.UpdateEventStatusAsync(professoreventId, newStatus);
                if (result.Success)
                {
                    dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                    ProfessorEventsToShowAtFrontPage = dashboardData.Events.ToList();
                    FilteredProfessorEvents = FilteredProfessorEvents.ToList(); // Refresh
                    StateHasChanged();
                }
            }
        }

        private void CloseEditModalForProfessorEvent()
        {
            // Implementation depends on modal visibility property
            StateHasChanged();
        }

        // Placeholder methods for remaining functionality
        private void CheckCharacterLimitInEventTitleField() { }
        private void CheckCharacterLimitInProfessorEventDescription() { }
        private void HandleProfessorDateChange(ChangeEventArgs e) { }
        private async Task HandlePublishSaveProfessorEvent() { await Task.CompletedTask; }
        private async Task HandleTemporarySaveProfessorEvent() { await Task.CompletedTask; }
        private bool IsTimeInRestrictedRangeWhenUploadEventAsProfessor(TimeSpan time) => true;
        private void HandleStatusFilterChangeForProfessorEvents(ChangeEventArgs e) { }
        private void OnPageSizeChange_SeeMyUploadedEventsAsProfessor(ChangeEventArgs e) { }
        private async Task DownloadStudentListForInterestInProfessorEventAsProfessor(long eventRNG) { await Task.CompletedTask; }
        private async Task DownloadCompanyListForInterestInProfessorEventAsProfessor(long eventRNG) { await Task.CompletedTask; }
        private async Task DownloadStudentCVForProfessorThesis(string studentEmail) { await Task.CompletedTask; }

        // Additional Methods
        private async Task ToggleUploadedProfessorEventsVisibility()
        {
            isUploadedEventsVisibleAsProfessor = !isUploadedEventsVisibleAsProfessor;
            if (isUploadedEventsVisibleAsProfessor)
            {
                isLoadingUploadedEventsAsProfessor = true;
                StateHasChanged();
                dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                FilteredProfessorEvents = dashboardData.Events
                    .Where(e => e.ProfessorEmailUsedToUploadEvent == CurrentUserEmail)
                    .ToList();
                ProfessorEventsToShowAtFrontPage = FilteredProfessorEvents.ToList();
                isLoadingUploadedEventsAsProfessor = false;
            }
            StateHasChanged();
        }

        private void OpenEditModalForProfessorEvent(ProfessorEvent professorevent)
        {
            currentProfessorEvent = professorevent;
            isEditModalVisibleForEventsAsProfessor = true;
            StateHasChanged();
        }

        private async Task UpdateProfessorEvent(ProfessorEvent updatedProfessorEvent)
        {
            var result = await ProfessorDashboardService.CreateOrUpdateEventAsync(updatedProfessorEvent);
            if (result.Success)
            {
                isEditModalVisibleForEventsAsProfessor = false;
                dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                ProfessorEventsToShowAtFrontPage = dashboardData.Events.ToList();
                StateHasChanged();
            }
        }

        private void OnTransportOptionChangeForProfessorEvent(ChangeEventArgs e)
        {
            if (bool.TryParse(e.Value?.ToString(), out var result))
            {
                professorEvent.ProfessorEventOfferingTransportToEventLocation = result;
            }
        }

        private async Task UploadProfessorEventAttachmentFile(InputFileChangeEventArgs e)
        {
            try
            {
                if (e.File == null)
                {
                    professorEvent.ProfessorEventAttachmentFile = null;
                    ProfessorEventAttachmentErrorMessage = null;
                    return;
                }
                // File upload logic here
                ProfessorEventAttachmentErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ProfessorEventAttachmentErrorMessage = $"Σφάλμα: {ex.Message}";
            }
        }

        private async Task ShowInterestedStudentsInProfessorEvent(long professoreventRNG)
        {
            loadingProfessorEventRNGForStudents = professoreventRNG;
            isLoadingInterestedStudentsForProfessorEvent = true;
            StateHasChanged();
            // Load interested students logic
            isLoadingInterestedStudentsForProfessorEvent = false;
            StateHasChanged();
        }

        private async Task ShowInterestedCompaniesInProfessorEvent(long professoreventRNG)
        {
            loadingProfessorEventRNGForCompanies = professoreventRNG;
            isLoadingInterestedCompaniesForProfessorEvent = true;
            StateHasChanged();
            // Load interested companies logic
            isLoadingInterestedCompaniesForProfessorEvent = false;
            StateHasChanged();
        }

        private void ShowStudentDetailsAtProfessorEventInterest(Student student)
        {
            currentStudentDetails = student;
            isModalVisibleToShowStudentDetailsInNameAsHyperlinkForProfessorThesis = true;
            StateHasChanged();
        }

        private void ShowCompanyDetailsAtProfessorEventInterest(Company company)
        {
            currentCompanyDetails = company;
            isModalVisibleToShowCompanyDetailsAtProfessorThesisInterest = true;
            StateHasChanged();
        }
    }
}
