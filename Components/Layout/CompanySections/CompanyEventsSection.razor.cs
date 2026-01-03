using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyEventsSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // User Info
        private string CurrentUserEmail = "";
        private QuizManager.Models.Company companyData;

        // Events Calendar
        private DateTime currentMonth = DateTime.Today;
        private int firstDayOfMonth => (int)new DateTime(currentMonth.Year, currentMonth.Month, 1).DayOfWeek;
        private int daysInCurrentMonth => DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        private int adjustedFirstDayOfMonth => (firstDayOfMonth == 0) ? 6 : firstDayOfMonth - 1;
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
        private object selectedEvent;

        // Event Details
        private CompanyEvent companyEventDetails;
        private ProfessorEvent professorEventDetails;

        // Form Visibility
        private bool isUploadCompanyEventFormVisible = false;

        // Form Model and Validation
        private CompanyEvent companyEvent = new CompanyEvent();
        private bool showErrorMessageForUploadingCompanyEvent = false;
        private bool showLoadingModalForEvent = false;
        private int loadingProgress = 0;
        private bool isFormValidToSaveEventAsCompany = true;
        private string saveEventAsCompanyMessage = "";
        private bool isSaveAnnouncementAsCompanySuccessful = false;
        private bool showSuccessMessage = false;

        // Character Limits
        private int remainingCharactersInEventTitleField = 120;
        private int remainingCharactersInCompanyEventDescription = 1000;

        // Areas/Subfields
        private List<Area> Areas = new List<Area>();
        private HashSet<int> SelectedAreasWhenUploadEventAsCompany = new HashSet<int>();
        private Dictionary<int, HashSet<string>> SelectedSubFieldsForCompanyEvent = new Dictionary<int, HashSet<string>>();
        private HashSet<int> ExpandedAreasForCompanyEvent = new HashSet<int>();
        private bool areCheckboxesVisibleForCompanyEvent = false;

        // Location Data
        private List<string> Regions = new List<string>();
        private Dictionary<string, List<string>> RegionToTownsMap = new Dictionary<string, List<string>>();

        // File Upload
        private string CompanyEventAttachmentErrorMessage = "";

        // Participants for professor events
        private Dictionary<long, int> numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent = new Dictionary<long, int>();
        private HashSet<long> professorEventsCompanyHasInterestedIn = new HashSet<long>();

        // Uploaded Events Management
        private bool isUploadedEventsVisible = false;
        private bool isLoadingUploadedEvents = false;
        private string selectedStatusFilterForEvents = "Όλα";
        private List<CompanyEvent> UploadedCompanyEvents = new List<CompanyEvent>();
        private List<CompanyEvent> FilteredCompanyEvents = new List<CompanyEvent>();
        private int currentPageForEvents = 1;
        private int pageSizeForEvents = 3;
        private int[] pageSizeOptions_SeeMyUploadedEventsAsCompany = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private int totalCountEvents = 0;
        private int publishedCountEvents = 0;
        private int unpublishedCountEvents = 0;

        // Bulk Operations
        private bool isBulkEditModeForEvents = false;
        private HashSet<int> selectedEventIds = new HashSet<int>();
        private bool showBulkActionModalForEvents = false;
        private string selectedBulkActionForEvents = "";

        // Edit Modal
        private bool isEditEventModalVisible = false;
        private bool isEditModalVisibleForEventsAsCompany = false;
        private CompanyEvent currentEventForEdit;

        // Computed Properties
        private List<CompanyEvent> filteredCompanyEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Company"
                ? selectedDateEvents
                : new List<CompanyEvent>();

        private List<ProfessorEvent> filteredProfessorEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Professor"
                ? selectedProfessorDateEvents
                : new List<ProfessorEvent>();

        private int totalPagesForEvents => (int)Math.Ceiling((double)(FilteredCompanyEvents?.Count ?? 0) / pageSizeForEvents);
        
        // Additional Properties for Events Management (checking if already exists)
        private bool hasExistingEventsOnSelectedDate = false;
        private int existingEventsCount = 0;
        private long? loadingEventRNG = null;
        private long? loadingEventRNGForProfessors = null;
        private long? selectedEventIdForProfessors = null;
        private string newStatusForBulkActionForEvents = "Μη Δημοσιευμένη";
        private Dictionary<string, Professor> professorDataCache = new Dictionary<string, Professor>();
        private int publishedCountEventsAsCompany = 0;
        
        // Pagination properties (needed by methods)
        private int currentPageForCompanyEvents = 1;
        private int CompanyEventsPerPage = 10;
        private int totalPagesForCompanyEvents_CompanyEventsToSee => 
            (int)Math.Ceiling((double)(FilteredCompanyEvents?.Count ?? 0) / CompanyEventsPerPage);

        protected override async Task OnInitializedAsync()
        {
            await LoadInitialData();
        }

        private async Task LoadInitialData()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
                var dashboard = await CompanyDashboardService.LoadDashboardDataAsync();
                companyData = dashboard.CompanyProfile;
                CompanyEventsToShowAtFrontPage = dashboard.CompanyEvents.ToList();
                ProfessorEventsToShowAtFrontPage = dashboard.ProfessorEvents.ToList();
                professorEventsCompanyHasInterestedIn = dashboard.InterestInProfessorEvents
                    .Where(i => i.CompanyEmailShowInterestForProfessorEvent != null &&
                                i.CompanyEmailShowInterestForProfessorEvent.Equals(CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.RNGForProfessorEventInterestAsCompany)
                    .ToHashSet();
            }
            else
            {
                var monthCompanyEvents = await CompanyDashboardService.GetCompanyEventsForMonthAsync(currentMonth.Year, currentMonth.Month);
                var monthProfessorEvents = await CompanyDashboardService.GetProfessorEventsForMonthAsync(currentMonth.Year, currentMonth.Month);
                CompanyEventsToShowAtFrontPage = monthCompanyEvents.ToList();
                ProfessorEventsToShowAtFrontPage = monthProfessorEvents.ToList();
            }

            var lookups = await CompanyDashboardService.GetLookupsAsync();
            Areas = lookups.Areas.ToList();
            Regions = lookups.Regions.ToList();
            RegionToTownsMap = lookups.RegionToTownsMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());

            InitializeStaticData();
            LoadEventsForCalendar();
        }

        private void InitializeStaticData()
        {
            if (!Regions.Any())
            {
                Regions = new List<string>
                {
                    "Ανατολική Μακεδονία και Θράκη", "Κεντρική Μακεδονία", "Δυτική Μακεδονία",
                    "Ήπειρος", "Θεσσαλία", "Ιόνια Νησιά", "Δυτική Ελλάδα", "Κεντρική Ελλάδα",
                    "Αττική", "Πελοπόννησος", "Βόρειο Αιγαίο", "Νότιο Αιγαίο", "Κρήτη"
                };
            }

            if (!RegionToTownsMap.Any())
            {
                RegionToTownsMap = new Dictionary<string, List<string>>
                {
                    {"Αττική", new List<string> {"Αθήνα", "Πειραιάς", "Περιστέρι", "Καλλιθέα", "Νίκαια", "Γλυφάδα", "Βούλα", "Μαρούσι", "Χαλάνδρι", "Κηφισιά"}},
                    {"Κεντρική Μακεδονία", new List<string> {"Θεσσαλονίκη", "Κατερίνη", "Σέρρες", "Κιλκίς", "Πολύγυρος", "Ναούσα", "Έδεσσα", "Γιαννιτσά"}},
                    {"Θεσσαλία", new List<string> {"Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα"}},
                };
            }
        }

        private async Task RefreshEventsForCurrentMonth()
        {
            CompanyEventsToShowAtFrontPage = (await CompanyDashboardService.GetCompanyEventsForMonthAsync(currentMonth.Year, currentMonth.Month)).ToList();
            ProfessorEventsToShowAtFrontPage = (await CompanyDashboardService.GetProfessorEventsForMonthAsync(currentMonth.Year, currentMonth.Month)).ToList();
        }

        private void LoadEventsForCalendar()
        {
            eventsForDate.Clear();
            eventsForDateForProfessors.Clear();
            int currentYear = currentMonth.Year;
            int currentMonthNumber = currentMonth.Month;

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

            StateHasChanged();
        }

        private async Task ShowPreviousMonth()
        {
            currentMonth = currentMonth.AddMonths(-1);
            await RefreshEventsForCurrentMonth();
            LoadEventsForCalendar();
        }

        private async Task ShowNextMonth()
        {
            currentMonth = currentMonth.AddMonths(1);
            await RefreshEventsForCurrentMonth();
            LoadEventsForCalendar();
        }

        private void OnDateClicked(DateTime clickedDate)
        {
            selectedDay = clickedDate.Day;
            highlightedDay = selectedDay;
            selectedDate = clickedDate;

            selectedDateEvents = CompanyEventsToShowAtFrontPage
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη" &&
                        e.CompanyEventActiveDate.Date == clickedDate.Date)
                .ToList();

            selectedProfessorDateEvents = ProfessorEventsToShowAtFrontPage
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη" &&
                        e.ProfessorEventActiveDate.Date == clickedDate.Date)
                .ToList();

            if (selectedDateEvents.Any() || selectedProfessorDateEvents.Any())
            {
                isModalVisibleToShowEventsOnCalendarForEachClickedDay = true;
            }

            StateHasChanged();
        }

        private void CloseModalForCompanyAndProfessorEventTitles()
        {
            isModalVisibleToShowEventsOnCalendarForEachClickedDay = false;
            selectedDateEvents.Clear();
            selectedProfessorDateEvents.Clear();
            selectedEvent = null;
            selectedEventFilter = "All";
            LoadEventsForCalendar();
        }

        private void ShowEventDetails(object eventDetails)
        {
            selectedEvent = eventDetails;
            if (eventDetails is CompanyEvent ce) companyEventDetails = ce;
            else if (eventDetails is ProfessorEvent pe) professorEventDetails = pe;
            StateHasChanged();
        }

        private void CloseEventDetails()
        {
            selectedEvent = null;
            companyEventDetails = null;
            professorEventDetails = null;
            StateHasChanged();
        }

        // Form Visibility
        private void ToggleFormVisibilityForUploadCompanyEvent()
        {
            isUploadCompanyEventFormVisible = !isUploadCompanyEventFormVisible;
            StateHasChanged();
        }

        // Character Limits
        private void CheckCharacterLimitInEventTitleField(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInEventTitleField = Math.Max(0, 120 - text.Length);
        }

        private void CheckCharacterLimitInCompanyEventDescription(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInCompanyEventDescription = Math.Max(0, 1000 - text.Length);
        }

        // Areas/Subfields
        private void ToggleCheckboxesForCompanyEvent()
        {
            areCheckboxesVisibleForCompanyEvent = !areCheckboxesVisibleForCompanyEvent;
            StateHasChanged();
        }

        private bool HasAnySelectionForCompanyEvent()
        {
            return SelectedAreasWhenUploadEventAsCompany.Any() ||
                   SelectedSubFieldsForCompanyEvent.Any(kvp => kvp.Value.Any());
        }

        private void OnAreaCheckedChangedForCompanyEvent(ChangeEventArgs e, Area area)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                SelectedAreasWhenUploadEventAsCompany.Add(area.Id);
            else
            {
                SelectedAreasWhenUploadEventAsCompany.Remove(area.Id);
                SelectedSubFieldsForCompanyEvent.Remove(area.Id);
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForCompanyEvent(Area area)
        {
            return SelectedAreasWhenUploadEventAsCompany.Contains(area.Id);
        }

        private void ToggleSubFieldsForCompanyEvent(Area area)
        {
            if (ExpandedAreasForCompanyEvent.Contains(area.Id))
                ExpandedAreasForCompanyEvent.Remove(area.Id);
            else
                ExpandedAreasForCompanyEvent.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForCompanyEvent(ChangeEventArgs e, Area area, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyEvent.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyEvent[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyEvent[area.Id].Add(subField);
            else
                SelectedSubFieldsForCompanyEvent[area.Id].Remove(subField);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyEvent(Area area, string subField)
        {
            return SelectedSubFieldsForCompanyEvent.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyEvent[area.Id].Contains(subField);
        }

        // Location
        private List<string> GetTownsForRegion(string region)
        {
            return RegionToTownsMap.ContainsKey(region) ? RegionToTownsMap[region] : new List<string>();
        }

        // Validation
        private bool IsValidPhoneNumber(string phone)
        {
            return !string.IsNullOrEmpty(phone) && phone.Length >= 10;
        }

        private bool HasAtLeastOneStartingPointWhenUploadEventAsCompany()
        {
            return !string.IsNullOrEmpty(companyEvent.CompanyEventPlaceLocation) ||
                   !string.IsNullOrEmpty(companyEvent.CompanyEventPerifereiaLocation) ||
                   !string.IsNullOrEmpty(companyEvent.CompanyEventDimosLocation);
        }

        // File Upload
        private async Task UploadCompanyEventAttachmentFile(InputFileChangeEventArgs e)
        {
            try
            {
                if (e.File == null)
                {
                    companyEvent.CompanyEventAttachmentFile = null;
                    CompanyEventAttachmentErrorMessage = null;
                    return;
                }

                const long maxFileSize = 10485760; // 10MB
                if (e.File.Size > maxFileSize)
                {
                    CompanyEventAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    return;
                }

                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                companyEvent.CompanyEventAttachmentFile = memoryStream.ToArray();
                CompanyEventAttachmentErrorMessage = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading attachment: {ex.Message}");
                CompanyEventAttachmentErrorMessage = "Σφάλμα κατά τη μεταφόρτωση.";
            }
        }

        // Organizer Visibility
        private void UpdateOrganizerVisibility(bool isVisible)
        {
            companyEvent.CompanyEventOtherOrganizerToBeVisible = isVisible;
            StateHasChanged();
        }

        // Save Event
        private async Task HandleTemporarySaveCompanyEvent()
        {
            await SaveCompanyEvent(false);
        }

        private async Task HandlePublishSaveCompanyEvent()
        {
            await SaveCompanyEvent(true);
        }

        private async Task SaveCompanyEvent(bool publishEvent)
        {
            try
            {
                showLoadingModalForEvent = true;
                loadingProgress = 0;
                StateHasChanged();

                await UpdateProgressWhenSaveEventAsCompany(10);

                // Build areas of interest
                var selectedAreas = new List<string>();
                foreach (var areaId in SelectedAreasWhenUploadEventAsCompany)
                {
                    var area = Areas.FirstOrDefault(a => a.Id == areaId);
                    if (area != null) selectedAreas.Add(area.AreaName);
                }
                foreach (var areaSubFields in SelectedSubFieldsForCompanyEvent)
                {
                    selectedAreas.AddRange(areaSubFields.Value);
                }
                companyEvent.CompanyEventAreasOfInterest = string.Join(",", selectedAreas);

                await UpdateProgressWhenSaveEventAsCompany(30);

                // Validation
                if (string.IsNullOrWhiteSpace(companyEvent.CompanyEventTitle) ||
                    string.IsNullOrWhiteSpace(companyEvent.CompanyEventDescription) ||
                    companyEvent.CompanyEventActiveDate <= DateTime.Today ||
                    !HasAnySelectionForCompanyEvent())
                {
                    await HandleEventValidationErrorWhenSaveEventAsCompany();
                    return;
                }

                await UpdateProgressWhenSaveEventAsCompany(50);

                // Set event properties
                companyEvent.RNGForEventUploadedAsCompany = new Random().NextInt64();
                companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany);
                companyEvent.CompanyEventStatus = publishEvent ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";
                companyEvent.CompanyEventUploadedDate = DateTime.Now;
                companyEvent.CompanyEmailUsedToUploadEvent = CurrentUserEmail;

                if (companyData != null)
                {
                    companyEvent.CompanyEventResponsiblePerson = $"{companyData.CompanyHRName} {companyData.CompanyHRSurname}";
                    companyEvent.CompanyEventResponsiblePersonEmail = companyData.CompanyHREmail;
                    companyEvent.CompanyEventResponsiblePersonTelephone = companyData.CompanyHRTelephone;
                }

                var result = await CompanyDashboardService.CreateOrUpdateCompanyEventAsync(companyEvent);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save event.");
                }

                await UpdateProgressWhenSaveEventAsCompany(90);

                saveEventAsCompanyMessage = "Η Εκδήλωση Δημιουργήθηκε Επιτυχώς";
                isSaveAnnouncementAsCompanySuccessful = true;
                showSuccessMessage = true;

                await UpdateProgressWhenSaveEventAsCompany(100);
                await Task.Delay(500);

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForEvent = false;
                Console.WriteLine($"Error saving event: {ex.Message}");
                showErrorMessageForUploadingCompanyEvent = true;
                StateHasChanged();
            }
        }

        private async Task HandleEventValidationErrorWhenSaveEventAsCompany()
        {
            showLoadingModalForEvent = false;
            showErrorMessageForUploadingCompanyEvent = true;
            isFormValidToSaveEventAsCompany = false;
            StateHasChanged();
        }

        private async Task UpdateProgressWhenSaveEventAsCompany(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Show Interest in Professor Event
        private void HandleNumberChangeForParticipantsWhenShowInterestAsACompanyForAProfessorEvent(ChangeEventArgs e, ProfessorEvent professorEvent)
        {
            if (int.TryParse(e.Value?.ToString(), out int numberOfPeople))
            {
                numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent[professorEvent.RNGForEventUploadedAsProfessor] = numberOfPeople;
            }
        }

        private async Task ShowInterestInProfessorEventAsCompany(ProfessorEvent professorEvent)
        {
            var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                $"Πρόκεται να δείξετε Ενδιαφέρον για την Εκδήλωση: {professorEvent.ProfessorEventTitle}. Είστε σίγουρος/η;");
            if (!confirmed) return;

            if (!numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent.TryGetValue(professorEvent.RNGForEventUploadedAsProfessor, out var numberOfPeople))
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Παρακαλώ επιλέξτε αριθμό ατόμων.");
                return;
            }

            try
            {
                var result = await CompanyDashboardService.ShowInterestInProfessorEventAsCompanyAsync(
                    professorEvent.RNGForEventUploadedAsProfessor,
                    numberOfPeople);
                if (!result.Success)
                {
                    await JS.InvokeVoidAsync("confirmActionWithHTML2", result.Error ?? "Αποτυχία καταχώρησης ενδιαφέροντος.");
                    return;
                }
                professorEventsCompanyHasInterestedIn.Add(professorEvent.RNGForEventUploadedAsProfessor);
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Επιτυχής ένδειξη ενδιαφέροντος!");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Σφάλμα. Παρακαλώ δοκιμάστε ξανά.");
            }
        }

        // Uploaded Events Management
        private async Task ToggleUploadedEventsVisibility()
        {
            isUploadedEventsVisible = !isUploadedEventsVisible;
            if (isUploadedEventsVisible)
            {
                isLoadingUploadedEvents = true;
                StateHasChanged();
                try
                {
                    await LoadUploadedEventsAsync();
                    await ApplyFiltersAndUpdateCountsForEvents();
                }
                finally
                {
                    isLoadingUploadedEvents = false;
                    StateHasChanged();
                }
            }
        }

        private async Task LoadUploadedEventsAsync()
        {
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            UploadedCompanyEvents = data.CompanyEvents
                .Where(e => e.CompanyEmailUsedToUploadEvent == CurrentUserEmail)
                .OrderByDescending(e => e.CompanyEventUploadedDate)
                .ToList();
        }

        private async Task ApplyFiltersAndUpdateCountsForEvents()
        {
            FilteredCompanyEvents = selectedStatusFilterForEvents == "Όλα"
                ? UploadedCompanyEvents.ToList()
                : UploadedCompanyEvents.Where(e => e.CompanyEventStatus == selectedStatusFilterForEvents).ToList();

            totalCountEvents = UploadedCompanyEvents.Count;
            publishedCountEvents = UploadedCompanyEvents.Count(e => e.CompanyEventStatus == "Δημοσιευμένη");
            unpublishedCountEvents = UploadedCompanyEvents.Count(e => e.CompanyEventStatus == "Μη Δημοσιευμένη");
            currentPageForEvents = 1;
        }

        private void HandleStatusFilterChangeForEvents(ChangeEventArgs e)
        {
            selectedStatusFilterForEvents = e.Value?.ToString() ?? "Όλα";
            _ = ApplyFiltersAndUpdateCountsForEvents();
        }

        private void OnPageSizeChange_SeeMyUploadedEventsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
            {
                pageSizeForEvents = size;
                currentPageForEvents = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<CompanyEvent> GetPaginatedCompanyEvents()
        {
            return FilteredCompanyEvents?
                .Skip((currentPageForEvents - 1) * pageSizeForEvents)
                .Take(pageSizeForEvents)
                ?? Enumerable.Empty<CompanyEvent>();
        }

        // Pagination
        private void GoToFirstPageForEvents() => ChangePageForEvents(1);
        private void PreviousPageForEvents() => ChangePageForEvents(Math.Max(1, currentPageForEvents - 1));
        private void NextPageForEvents() => ChangePageForEvents(Math.Min(totalPagesForEvents, currentPageForEvents + 1));
        private void GoToLastPageForEvents() => ChangePageForEvents(totalPagesForEvents);
        private void GoToPageForEvents(int page) => ChangePageForEvents(page);

        private void ChangePageForEvents(int newPage)
        {
            if (newPage > 0 && newPage <= totalPagesForEvents)
            {
                currentPageForEvents = newPage;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForEvents()
        {
            var pages = new List<int>();
            int current = currentPageForEvents;
            int total = totalPagesForEvents;

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

        // Event Status and Actions
        private async Task ChangeEventStatus(int eventId, string newStatus)
        {
            await CompanyDashboardService.UpdateCompanyEventStatusAsync(eventId, newStatus);
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
        }

        private async Task DeleteEvent(int eventId)
        {
            await CompanyDashboardService.DeleteCompanyEventAsync(eventId);
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
        }

        // Bulk Operations
        private void EnableBulkEditModeForEvents()
        {
            isBulkEditModeForEvents = true;
            selectedEventIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForEvents()
        {
            isBulkEditModeForEvents = false;
            selectedEventIds.Clear();
            StateHasChanged();
        }

        private void ToggleEventSelection(int eventId, ChangeEventArgs e)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedEventIds.Add(eventId);
            else
                selectedEventIds.Remove(eventId);
            StateHasChanged();
        }

        private void ToggleAllEventsSelection(ChangeEventArgs e)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                foreach (var ev in FilteredCompanyEvents)
                    selectedEventIds.Add(ev.Id);
            }
            else
            {
                selectedEventIds.Clear();
            }
            StateHasChanged();
        }

        private void ShowBulkActionOptionsForEvents()
        {
            if (selectedEventIds.Any())
            {
                showBulkActionModalForEvents = true;
            }
        }

        private void CloseBulkActionModalForEvents()
        {
            showBulkActionModalForEvents = false;
            selectedBulkActionForEvents = "";
        }

        private async Task ExecuteBulkActionForEvents()
        {
            if (string.IsNullOrEmpty(selectedBulkActionForEvents) || !selectedEventIds.Any())
                return;

            foreach (var evId in selectedEventIds)
            {
                if (selectedBulkActionForEvents == "Δημοσίευση")
                    await CompanyDashboardService.UpdateCompanyEventStatusAsync(evId, "Δημοσιευμένη");
                else if (selectedBulkActionForEvents == "Αποδημοσίευση")
                    await CompanyDashboardService.UpdateCompanyEventStatusAsync(evId, "Μη Δημοσιευμένη");
                else if (selectedBulkActionForEvents == "Διαγραφή")
                    await CompanyDashboardService.DeleteCompanyEventAsync(evId);
            }

            CloseBulkActionModalForEvents();
            CancelBulkEditForEvents();
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
        }

        // Edit Modal
        private void OpenEditEventModal(CompanyEvent ev)
        {
            currentEventForEdit = new CompanyEvent
            {
                Id = ev.Id,
                CompanyEventTitle = ev.CompanyEventTitle,
                CompanyEventDescription = ev.CompanyEventDescription,
                CompanyEventActiveDate = ev.CompanyEventActiveDate,
                CompanyEventStatus = ev.CompanyEventStatus
            };
            isEditEventModalVisible = true;
        }

        private void CloseEditEventModal()
        {
            isEditEventModalVisible = false;
            currentEventForEdit = null;
        }

        private async Task UpdateEvent()
        {
            if (currentEventForEdit == null) return;

            var ev = UploadedCompanyEvents.FirstOrDefault(e => e.Id == currentEventForEdit.Id);
            if (ev == null)
            {
                var data = await CompanyDashboardService.LoadDashboardDataAsync();
                ev = data.CompanyEvents.FirstOrDefault(e => e.Id == currentEventForEdit.Id);
            }
            if (ev != null)
            {
                ev.CompanyEventTitle = currentEventForEdit.CompanyEventTitle;
                ev.CompanyEventDescription = currentEventForEdit.CompanyEventDescription;
                ev.CompanyEventActiveDate = currentEventForEdit.CompanyEventActiveDate;
                await CompanyDashboardService.CreateOrUpdateCompanyEventAsync(ev);
            }

            CloseEditEventModal();
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
        }

        // Additional Missing Properties
        private CompanyEvent currentCompanyEvent;
        private Student selectedStudentFromCache;
        private QuizManager.Models.Professor selectedProfessorToShowDetailsForInterestinCompanyEvent;
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private bool showModal = false;
        private bool showProfessorModal = false;
        private long? selectedEventIdForStudents = null;
        
        // Event Menu and Toggles
        private Dictionary<long, bool> activeEventMenuId = new Dictionary<long, bool>();
        
        private void ToggleEventMenu(long eventId)
        {
            if (activeEventMenuId.ContainsKey(eventId))
                activeEventMenuId[eventId] = !activeEventMenuId[eventId];
            else
                activeEventMenuId[eventId] = true;
            StateHasChanged();
        }

        // Toggle Visibility
        private void ToggleUploadedCompanyEventsVisibility()
        {
            isUploadedEventsVisible = !isUploadedEventsVisible;
            StateHasChanged();
        }

        // Pagination - already declared above, removing duplicate

        // Status Filter
        private string selectedStatusFilterForEventsAsCompany = "Όλα";

        // Edit Modal
        private bool showCheckboxesForEditCompanyEvent = false;
        private bool showCheckboxesForCompanyEvent = false;

        // Interest Management
        private List<InterestInCompanyEvent> InterestedStudents = new List<InterestInCompanyEvent>();
        private List<InterestInCompanyEventAsProfessor> filteredProfessorInterestForCompanyEvents = new List<InterestInCompanyEventAsProfessor>();
        private bool isLoadingInterestedStudents = false;
        private bool isLoadingInterestedProfessors = false;

        // Bulk Actions
        private string bulkActionForEvents = "";

        // Counts
        private int unpublishedCountEventsAsCompany = 0;
        private int totalCountEventsAsCompany = 0;

        // Loading Modals
        private bool showLoadingModalForDeleteCompanyEvent = false;

        // Modal Close Methods
        private void CloseEditModalForCompanyEvent()
        {
            isEditEventModalVisible = false;
            isEditModalVisibleForEventsAsCompany = false;
            currentEventForEdit = null;
            StateHasChanged();
        }

        private void CloseStudentDetailsModal()
        {
            selectedStudentFromCache = null;
            showModal = false;
            StateHasChanged();
        }

        private void CloseProfessorDetailsModal()
        {
            selectedProfessorToShowDetailsForInterestinCompanyEvent = null;
            showProfessorModal = false;
            StateHasChanged();
        }

        // Status Change
        private async Task ChangeCompanyEventStatus(CompanyEvent ev, string newStatus)
        {
            ev.CompanyEventStatus = newStatus;
            await CompanyDashboardService.UpdateCompanyEventStatusAsync(ev.Id, newStatus);
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
            StateHasChanged();
        }

        // Clear Field Helper
        private void ClearField(int fieldNumber)
        {
            if (companyEvent == null) return;
            switch (fieldNumber)
            {
                case 1:
                    companyEvent.CompanyEventStartingPointLocationToTransportPeopleToEvent1 = string.Empty;
                    break;
                case 2:
                    companyEvent.CompanyEventStartingPointLocationToTransportPeopleToEvent2 = string.Empty;
                    break;
                case 3:
                    companyEvent.CompanyEventStartingPointLocationToTransportPeopleToEvent3 = string.Empty;
                    break;
            }
            StateHasChanged();
        }

        // Time Validation
        private bool IsTimeInRestrictedRangeWhenUploadEventAsCompany(TimeSpan time)
        {
            var restrictedStart = new TimeSpan(22, 0, 0); // 10 PM
            var restrictedEnd = new TimeSpan(6, 0, 0); // 6 AM
            return time >= restrictedStart || time <= restrictedEnd;
        }

        // Bulk Status Change
        private async Task ExecuteBulkStatusChangeForEvents(string newStatus)
        {
            var eventsToUpdate = UploadedCompanyEvents.Where(e => selectedEventIds.Contains(e.Id)).ToList();
            foreach (var ev in eventsToUpdate)
            {
                ev.CompanyEventStatus = newStatus;
            }
            foreach (var ev in eventsToUpdate)
            {
                await CompanyDashboardService.UpdateCompanyEventStatusAsync(ev.Id, newStatus);
            }
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
            CloseBulkActionModalForEvents();
            CancelBulkEditForEvents();
        }

        // Get interested students/professors (placeholder - should delegate to CompanyDashboardService)
        private async Task LoadInterestedStudentsForEvent(long eventRNG)
        {
            isLoadingInterestedStudents = true;
            StateHasChanged();
            try
            {
                var interests = await CompanyDashboardService.GetCompanyEventInterestsAsync(eventRNG);
                InterestedStudents = interests.Select(i => i.Application).ToList();
            }
            finally
            {
                isLoadingInterestedStudents = false;
                StateHasChanged();
            }
        }

        private async Task LoadInterestedProfessorsForEvent(long eventRNG)
        {
            isLoadingInterestedProfessors = true;
            StateHasChanged();
            try
            {
                var interests = await CompanyDashboardService.GetProfessorCompanyEventInterestsAsync(eventRNG);
                filteredProfessorInterestForCompanyEvents = interests.ToList();
            }
            finally
            {
                isLoadingInterestedProfessors = false;
                StateHasChanged();
            }
        }

        // Missing Methods - extracted from MainLayout.razor.cs.backup
        
        private async Task HandleDateChange(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out DateTime newDate))
            {
                companyEvent.CompanyEventActiveDate = newDate;
                await CheckExistingEventsForDate();
            }
        }

        private async Task CheckExistingEventsForDate()
        {
            if (companyEvent.CompanyEventActiveDate.Date > DateTime.Today.Date)
            {
                var counts = await CompanyDashboardService.CountPublishedEventsOnDateAsync(
                    companyEvent.CompanyEventActiveDate.Date);
                existingEventsCount = counts.companyEvents + counts.professorEvents;
                hasExistingEventsOnSelectedDate = existingEventsCount > 0;
            }
            else
            {
                hasExistingEventsOnSelectedDate = false;
            }
            StateHasChanged();
        }

        private async Task DeleteCompanyEvent(int companyeventId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "<strong style='color: red;'>Προσοχή!</strong><br><br>" +
                "Πρόκειται να <strong style='color: red;'>διαγράψετε οριστικά</strong> αυτή την Εκδήλωση.<br><br>" +
                "Αυτή η ενέργεια <strong>δεν μπορεί να αναιρεθεί</strong>.<br><br>" +
                "Είστε σίγουρος/η;");

            if (isConfirmed)
            {
                showLoadingModalForDeleteCompanyEvent = true;
                loadingProgress = 0;
                StateHasChanged();
            
                try
                {
                    var deleted = await CompanyDashboardService.DeleteCompanyEventAsync(companyeventId);
                    if (deleted)
                    {
                        await LoadUploadedEventsAsync();
                        await ApplyFiltersAndUpdateCountsForEvents();
                    }
                    else
                    {
                        showLoadingModalForDeleteCompanyEvent = false;
                        await JS.InvokeVoidAsync("alert", "Η εκδήλωση δεν βρέθηκε.");
                        StateHasChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    showLoadingModalForDeleteCompanyEvent = false;
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά τη διαγραφή: {ex.Message}");
                    StateHasChanged();
                    return;
                }
                finally
                {
                    showLoadingModalForDeleteCompanyEvent = false;
                }
                StateHasChanged();
            }
        }

        private void HandleStatusFilterChangeForCompanyEvents(ChangeEventArgs e)
        {
            selectedStatusFilterForEvents = e.Value?.ToString() ?? "Όλα";
            StateHasChanged();
        }

        private void OpenEditModalForCompanyEvent(CompanyEvent companyEvent)
        {
            currentEventForEdit = companyEvent;
            isEditModalVisibleForEventsAsCompany = true;
            StateHasChanged();
        }

        private async Task ShowInterestedStudentsInCompanyEvent(long eventRNG)
        {
            if (selectedEventIdForStudents == eventRNG)
            {
                selectedEventIdForStudents = null;
                InterestedStudents.Clear();
                StateHasChanged();
            }
            else
            {
                selectedEventIdForProfessors = null;
                filteredProfessorInterestForCompanyEvents.Clear();
                if (selectedEventIdForStudents != null && selectedEventIdForStudents != eventRNG)
                {
                    selectedEventIdForStudents = null;
                    InterestedStudents.Clear();
                }
                isLoadingInterestedStudents = true;
                loadingEventRNG = eventRNG;
                StateHasChanged();
                
                var interests = await CompanyDashboardService.GetCompanyEventInterestsAsync(eventRNG);
                InterestedStudents = interests.Select(i => i.Application).ToList();
                foreach (var detail in interests)
                {
                    if (detail.Student != null && !studentDataCache.ContainsKey(detail.Student.Student_UniqueID))
                    {
                        studentDataCache[detail.Student.Student_UniqueID] = detail.Student;
                    }
                }
                
                isLoadingInterestedStudents = false;
                selectedEventIdForStudents = eventRNG;
                StateHasChanged();
            }
        }

        private async Task ShowInterestedProfessorsInCompanyEvent(long companyeventRNG)
        {
            if (selectedEventIdForProfessors == companyeventRNG)
            {
                selectedEventIdForProfessors = null;
                filteredProfessorInterestForCompanyEvents.Clear();
                StateHasChanged();
            }
            else
            {
                selectedEventIdForStudents = null;
                InterestedStudents.Clear();
                if (selectedEventIdForProfessors != null && selectedEventIdForProfessors != companyeventRNG)
                {
                    selectedEventIdForProfessors = null;
                    filteredProfessorInterestForCompanyEvents.Clear();
                }
                isLoadingInterestedProfessors = true;
                loadingEventRNGForProfessors = companyeventRNG;
                StateHasChanged();
                
                var interests = await CompanyDashboardService.GetProfessorCompanyEventInterestsAsync(companyeventRNG);
                filteredProfessorInterestForCompanyEvents = interests.ToList();
                
                isLoadingInterestedProfessors = false;
                selectedEventIdForProfessors = companyeventRNG;
                StateHasChanged();
            }
        }

        private async Task ShowStudentDetailsAtCompanyEventInterest(InterestInCompanyEvent application)
        {
            var studentUniqueId = application.StudentUniqueIDShowInterestForEvent;
            var student = studentDataCache.Values.FirstOrDefault(s => s.Student_UniqueID == studentUniqueId);
            
            if (student == null)
            {
                student = await CompanyDashboardService.GetStudentByUniqueIdAsync(studentUniqueId);
                if (student != null)
                    studentDataCache[student.Student_UniqueID] = student;
            }
            
            if (student != null)
            {
                selectedStudentToShowDetailsForInterestinCompanyEvent = application;
                selectedStudentFromCache = student;
                showModal = true;
                StateHasChanged();
            }
        }

        private async Task ShowProfessorDetailsAtCompanyEventInterest(InterestInCompanyEventAsProfessor interest)
        {
            if (!professorDataCache.TryGetValue(interest.ProfessorEmailShowInterestForCompanyEvent, out var professor))
            {
                professor = await CompanyDashboardService.GetProfessorByEmailAsync(interest.ProfessorEmailShowInterestForCompanyEvent);
                
                if (professor != null)
                    professorDataCache[interest.ProfessorEmailShowInterestForCompanyEvent] = professor;
            }
            
            if (professor != null)
            {
                selectedProfessorToShowDetailsForInterestinCompanyEvent = professor;
                showProfessorModal = true;
                StateHasChanged();
            }
        }

        private void OnTransportOptionChange(ChangeEventArgs e)
        {
            if (bool.TryParse(e.Value?.ToString(), out var result))
            {
                companyEvent.CompanyEventOfferingTransportToEventLocation = result;
                StateHasChanged();
            }
        }

        private void OnCompanyCreateEventPhoneNumberInput(ChangeEventArgs e)
        {
            companyEvent.CompanyEventResponsiblePersonTelephone = e.Value?.ToString() ?? "";
            StateHasChanged();
        }

        private int GetOrAddNumberOfPeople(long eventId)
        {
            if (!numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent.ContainsKey(eventId))
            {
                numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent[eventId] = 1;
            }
            return numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent[eventId];
        }

        private List<int> GetVisiblePagesForCompanyEvents()
        {
            var pages = new List<int>();
            int current = currentPageForCompanyEvents;
            int total = totalPagesForCompanyEvents_CompanyEventsToSee;
        
            pages.Add(1);
            if (current > 3) pages.Add(-1);
        
            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
        
            for (int i = start; i <= end; i++) pages.Add(i);
        
            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);
        
            return pages;
        }

        private void GoToFirstPageForCompanyEvents()
        {
            currentPageForCompanyEvents = 1;
            StateHasChanged();
        }

        private void GoToLastPageForCompanyEvents()
        {
            currentPageForCompanyEvents = totalPagesForCompanyEvents_CompanyEventsToSee;
            StateHasChanged();
        }

        private void PreviousPageForCompanyEvents()
        {
            if (currentPageForCompanyEvents > 1)
            {
                currentPageForCompanyEvents--;
                StateHasChanged();
            }
        }

        private void NextPageForCompanyEvents()
        {
            if (currentPageForCompanyEvents < totalPagesForCompanyEvents_CompanyEventsToSee)
            {
                currentPageForCompanyEvents++;
                StateHasChanged();
            }
        }

        private void GoToPageForCompanyEvents(int page)
        {
            if (page >= 1 && page <= totalPagesForCompanyEvents_CompanyEventsToSee)
            {
                currentPageForCompanyEvents = page;
                StateHasChanged();
            }
        }

        private async Task DownloadStudentListForInterestInCompanyEventAsCompany()
        {
            // TODO: Implement Excel download
            await JS.InvokeVoidAsync("alert", "Excel download functionality to be implemented");
        }

        private async Task DownloadProfessorListForInterestInCompanyEventAsCompany()
        {
            // TODO: Implement Excel download
            await JS.InvokeVoidAsync("alert", "Excel download functionality to be implemented");
        }

        private bool HasCompanyShownInterestInProfessorEvent(long professorEventRng)
        {
            return professorEventsCompanyHasInterestedIn.Contains(professorEventRng);
        }

        private async Task ExecuteBulkCopyForEvents()
        {
            // TODO: Implement bulk copy
            await Task.CompletedTask;
        }

        // Edit Modal Methods
        private void ToggleCheckboxesForEditCompanyEvent()
        {
            showCheckboxesForEditCompanyEvent = !showCheckboxesForEditCompanyEvent;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditCompanyEvent(ChangeEventArgs e, Area area)
        {
            bool isChecked = (bool)e.Value;
            // TODO: Implement area selection for edit
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditCompanyEvent(Area area)
        {
            // TODO: Implement area selection check for edit
            return false;
        }

        private void ToggleSubFieldsForEditCompanyEvent(Area area)
        {
            if (ExpandedAreasForEditCompanyEvent.Contains(area.Id))
                ExpandedAreasForEditCompanyEvent.Remove(area.Id);
            else
                ExpandedAreasForEditCompanyEvent.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditCompanyEvent(ChangeEventArgs e, Area area, string subField)
        {
            bool isChecked = (bool)e.Value;
            // TODO: Implement subfield selection for edit
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditCompanyEvent(Area area, string subField)
        {
            // TODO: Implement subfield selection check for edit
            return false;
        }

        private async Task UpdateCompanyEvent()
        {
            if (currentEventForEdit == null) return;
            // TODO: Implement update logic
            isEditModalVisibleForEventsAsCompany = false;
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
            StateHasChanged();
        }

        private async Task UploadFileToUpdateCompanyEventAttachment(InputFileChangeEventArgs e)
        {
            // TODO: Implement file upload for edit
            await Task.CompletedTask;
        }

        // Additional missing property
        private HashSet<int> ExpandedAreasForEditCompanyEvent = new HashSet<int>();
        private InterestInCompanyEvent selectedStudentToShowDetailsForInterestinCompanyEvent;
    }
}
