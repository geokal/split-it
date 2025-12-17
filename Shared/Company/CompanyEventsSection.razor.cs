using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Helpers;
using QuizManager.Models;
using QuizManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplitIt.Shared.Company
{
    public partial class CompanyEventsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
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
                companyData = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyEmail == CurrentUserEmail);
            }

            CompanyEventsToShowAtFrontPage = await FetchCompanyEventsAsync();
            ProfessorEventsToShowAtFrontPage = await FetchProfessorEventsAsync();
            Areas = await dbContext.Areas.Include(a => a.SubFields).ToListAsync();

            InitializeStaticData();
            LoadEventsForCalendar();
        }

        private void InitializeStaticData()
        {
            Regions = new List<string>
            {
                "Ανατολική Μακεδονία και Θράκη", "Κεντρική Μακεδονία", "Δυτική Μακεδονία",
                "Ήπειρος", "Θεσσαλία", "Ιόνια Νησιά", "Δυτική Ελλάδα", "Κεντρική Ελλάδα",
                "Αττική", "Πελοπόννησος", "Βόρειο Αιγαίο", "Νότιο Αιγαίο", "Κρήτη"
            };

            RegionToTownsMap = new Dictionary<string, List<string>>
            {
                {"Αττική", new List<string> {"Αθήνα", "Πειραιάς", "Περιστέρι", "Καλλιθέα", "Νίκαια", "Γλυφάδα", "Βούλα", "Μαρούσι", "Χαλάνδρι", "Κηφισιά"}},
                {"Κεντρική Μακεδονία", new List<string> {"Θεσσαλονίκη", "Κατερίνη", "Σέρρες", "Κιλκίς", "Πολύγυρος", "Ναούσα", "Έδεσσα", "Γιαννιτσά"}},
                {"Θεσσαλία", new List<string> {"Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα"}},
                // Add more regions as needed
            };
        }

        private async Task<List<CompanyEvent>> FetchCompanyEventsAsync()
        {
            return await dbContext.CompanyEvents
                .Include(e => e.Company)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<ProfessorEvent>> FetchProfessorEventsAsync()
        {
            return await dbContext.ProfessorEvents
                .Include(e => e.Professor)
                .AsNoTracking()
                .ToListAsync();
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

        private void ShowPreviousMonth()
        {
            currentMonth = currentMonth.AddMonths(-1);
            LoadEventsForCalendar();
        }

        private void ShowNextMonth()
        {
            currentMonth = currentMonth.AddMonths(1);
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

        private void OnSubFieldCheckedChangedForCompanyEvent(ChangeEventArgs e, Area area, SubField subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyEvent.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyEvent[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyEvent[area.Id].Add(subField.SubFieldName);
            else
                SelectedSubFieldsForCompanyEvent[area.Id].Remove(subField.SubFieldName);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyEvent(Area area, SubField subField)
        {
            return SelectedSubFieldsForCompanyEvent.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyEvent[area.Id].Contains(subField.SubFieldName);
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
            return !string.IsNullOrEmpty(companyEvent.CompanyEventLocation);
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
            companyEvent.CompanyEventOrganizerVisibility = isVisible;
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

                dbContext.CompanyEvents.Add(companyEvent);
                await dbContext.SaveChangesAsync();

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

            var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyEmail == CurrentUserEmail);
            if (company == null)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν βρέθηκαν στοιχεία εταιρείας.");
                return;
            }

            var existingInterest = await dbContext.InterestInProfessorEventsAsCompany
                .FirstOrDefaultAsync(i =>
                    i.CompanyEmailShowInterestForProfessorEvent == company.CompanyEmail &&
                    i.RNGForProfessorEventInterestAsCompany == professorEvent.RNGForEventUploadedAsProfessor);

            if (existingInterest != null)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", $"Έχετε ήδη δείξει ενδιαφέρον!");
                return;
            }

            if (!numberOfCompanyPeopleInputWhenCompanyShowsInterestInProfessorEvent.TryGetValue(professorEvent.RNGForEventUploadedAsProfessor, out var numberOfPeople))
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Παρακαλώ επιλέξτε αριθμό ατόμων.");
                return;
            }

            try
            {
                var interest = new InterestInProfessorEventAsCompany
                {
                    RNGForProfessorEventInterestAsCompany = professorEvent.RNGForEventUploadedAsProfessor,
                    RNGForProfessorEventInterestAsCompany_HashedAsUniqueID = professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID,
                    DateTimeCompanyShowInterestForProfessorEvent = DateTime.Now,
                    ProfessorEventStatus_ShowInterestAsCompany_AtCompanySide = "Έχετε Δείξει Ενδιαφέρον",
                    ProfessorEventStatus_ShowInterestAsCompany_AtProfessorSide = "Προς Επεξεργασία",
                    ProfessorEmailWhereCompanyShowedInterest = professorEvent.ProfessorEmailUsedToUploadEvent,
                    CompanyEmailShowInterestForProfessorEvent = company.CompanyEmail,
                    CompanyNumberOfPeopleToShowUpWhenShowInterestForProfessorEvent = numberOfPeople.ToString()
                };

                dbContext.InterestInProfessorEventsAsCompany.Add(interest);
                await dbContext.SaveChangesAsync();

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
            UploadedCompanyEvents = await dbContext.CompanyEvents
                .Where(e => e.CompanyEmailUsedToUploadEvent == CurrentUserEmail)
                .OrderByDescending(e => e.CompanyEventUploadedDate)
                .ToListAsync();
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
            var eventToUpdate = await dbContext.CompanyEvents.FindAsync(eventId);
            if (eventToUpdate != null)
            {
                eventToUpdate.CompanyEventStatus = newStatus;
                await dbContext.SaveChangesAsync();
                await LoadUploadedEventsAsync();
                await ApplyFiltersAndUpdateCountsForEvents();
            }
        }

        private async Task DeleteEvent(int eventId)
        {
            var eventToDelete = await dbContext.CompanyEvents.FindAsync(eventId);
            if (eventToDelete != null)
            {
                dbContext.CompanyEvents.Remove(eventToDelete);
                await dbContext.SaveChangesAsync();
                await LoadUploadedEventsAsync();
                await ApplyFiltersAndUpdateCountsForEvents();
            }
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

            var eventsToUpdate = await dbContext.CompanyEvents
                .Where(e => selectedEventIds.Contains(e.Id))
                .ToListAsync();

            foreach (var ev in eventsToUpdate)
            {
                if (selectedBulkActionForEvents == "Δημοσίευση")
                    ev.CompanyEventStatus = "Δημοσιευμένη";
                else if (selectedBulkActionForEvents == "Αποδημοσίευση")
                    ev.CompanyEventStatus = "Μη Δημοσιευμένη";
                else if (selectedBulkActionForEvents == "Διαγραφή")
                    dbContext.CompanyEvents.Remove(ev);
            }

            await dbContext.SaveChangesAsync();

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

            var ev = await dbContext.CompanyEvents.FindAsync(currentEventForEdit.Id);
            if (ev != null)
            {
                ev.CompanyEventTitle = currentEventForEdit.CompanyEventTitle;
                ev.CompanyEventDescription = currentEventForEdit.CompanyEventDescription;
                ev.CompanyEventActiveDate = currentEventForEdit.CompanyEventActiveDate;
                await dbContext.SaveChangesAsync();
            }

            CloseEditEventModal();
            await LoadUploadedEventsAsync();
            await ApplyFiltersAndUpdateCountsForEvents();
        }
    }
}
