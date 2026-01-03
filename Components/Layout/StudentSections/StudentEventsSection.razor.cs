using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuizManager.Models;
using QuizManager.Services.StudentDashboard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentEventsSection : ComponentBase
    {
        [Inject] private IStudentDashboardService StudentDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

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
        private bool isExpandedModalVisibleToSeeCompanyDetailsAsStudent = false;

        // Company Details from Events
        private QuizManager.Models.Company currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents;
        private bool showCompanyDetailsModalFromEvents = false;

        // Selected Entities
        private ProfessorEvent currentProfessorEvent;
        private CompanyEvent currentCompanyEvent;
        private QuizManager.Models.Professor currentProfessorDetailsToShowOnHyperlinkAsStudentForProfessorEvents;
        private ProfessorInternship currentProfessorInternship;
        private CompanyJob currentJob;

        // Transport Needs
        private Dictionary<long, bool> needsTransportForProfessorEvent = new Dictionary<long, bool>();
        private Dictionary<long, bool> needsTransportForCompanyEvent = new Dictionary<long, bool>();
        private Dictionary<long, string> selectedStartingPoint = new Dictionary<long, string>();

        // Event Lists
        private List<ProfessorEvent> professorEventsToSeeAsStudent = new List<ProfessorEvent>();
        private List<CompanyEvent> companyEventsToSeeAsStudent = new List<CompanyEvent>();
        private bool isProfessorEventsVisibleToSeeAsStudent = false;
        private bool isCompanyEventsVisibleToSeeAsStudent = false;

        // Interest Tracking
        private HashSet<long> interestedProfessorEventIds = new HashSet<long>();
        private HashSet<long> alreadyInterestedCompanyEventIds = new HashSet<long>();

        // Filtering and Pagination
        private string selectedEventType = "all";
        private string selectedEventStatus = "all";
        private int currentPageForEvents = 1;
        private int itemsPerPageForEvents = 10;
        private int totalPagesForEvents = 1;
        private bool isLoadingEvents = false;

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

        private StudentDashboardData _dashboardData = StudentDashboardData.Empty;

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
            }

            _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
            alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
            interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();

            CompanyEventsToShowAtFrontPage = await FetchCompanyEventsAsync();
            ProfessorEventsToShowAtFrontPage = await FetchProfessorEventsAsync();
            LoadEventsForCalendar();
        }

        private async Task<List<CompanyEvent>> FetchCompanyEventsAsync()
        {
            var companyevents = await StudentDashboardService.GetPublishedCompanyEventsAsync();
            return companyevents.ToList();
        }

        private async Task<List<ProfessorEvent>> FetchProfessorEventsAsync()
        {
            var professorevents = await StudentDashboardService.GetPublishedProfessorEventsAsync();
            return professorevents.ToList();
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

        private async Task ShowInterestInCompanyEventAsStudent(CompanyEvent companyEvent)
        {
            if (companyEvent == null)
            {
                return;
            }

            var needsTransport = needsTransportForCompanyEvent.TryGetValue(companyEvent.RNGForEventUploadedAsCompany, out var transport) && transport;
            var startingPoint = selectedStartingPoint.TryGetValue(companyEvent.RNGForEventUploadedAsCompany, out var start) ? start : string.Empty;

            var result = await StudentDashboardService.ShowInterestInCompanyEventAsync(
                companyEvent.RNGForEventUploadedAsCompany,
                needsTransport,
                startingPoint);

            if (result != null)
            {
                alreadyInterestedCompanyEventIds.Add(companyEvent.RNGForEventUploadedAsCompany);
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
            }

            StateHasChanged();
        }

        private async Task ShowInterestInProfessorEventAsStudent(ProfessorEvent professorEvent)
        {
            if (professorEvent == null)
            {
                return;
            }

            var needsTransport = needsTransportForProfessorEvent.TryGetValue(professorEvent.RNGForEventUploadedAsProfessor, out var transport) && transport;
            var startingPoint = selectedStartingPoint.TryGetValue(professorEvent.RNGForEventUploadedAsProfessor, out var start) ? start : string.Empty;

            var result = await StudentDashboardService.ShowInterestInProfessorEventAsync(
                professorEvent.RNGForEventUploadedAsProfessor,
                needsTransport,
                startingPoint);

            if (result != null)
            {
                interestedProfessorEventIds.Add(professorEvent.RNGForEventUploadedAsProfessor);
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
            }

            StateHasChanged();
        }

        private async Task ShowCompanyDetailsOnHyperlinkAsStudentForCompanyEvents(string companyEmail)
        {
            currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents = await StudentDashboardService.GetCompanyByEmailAsync(companyEmail);

            if (currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents != null)
            {
                isExpandedModalVisibleToSeeCompanyDetailsAsStudent = true;
                showCompanyDetailsModalFromEvents = true;
                StateHasChanged();
            }
        }

        private void CloseCompanyDetailsModalFromEvents()
        {
            showCompanyDetailsModalFromEvents = false;
            isExpandedModalVisibleToSeeCompanyDetailsAsStudent = false;
            StateHasChanged();
        }

        private async Task DownloadCompanyEventAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData == null || attachmentData.Length == 0)
            {
                return;
            }
            var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "company-event.pdf" : fileName;
            if (!normalizedFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFileName += ".pdf";
            }
            await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", normalizedFileName, "application/pdf", attachmentData);
        }

        private async Task DownloadProfessorEventAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData == null || attachmentData.Length == 0)
            {
                return;
            }
            var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "professor-event.pdf" : fileName;
            if (!normalizedFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFileName += ".pdf";
            }
            await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", normalizedFileName, "application/pdf", attachmentData);
        }

        // Helper Methods
        private bool IsPastEvent(DateTime? eventDate)
        {
            if (eventDate == null) return true; 
            return eventDate.Value.Date < DateTime.Today;
        }

        private async Task ToggleAndLoadCompanyAndProfessorEventsAsStudent()
        {
            if (isLoadingEvents) return;

            isLoadingEvents = true;
            StateHasChanged();

            try
            {
                bool isOpening = !isCompanyEventsVisibleToSeeAsStudent && !isProfessorEventsVisibleToSeeAsStudent;

                if (isOpening)
                {
                    companyEventsToSeeAsStudent = (await StudentDashboardService.GetPublishedCompanyEventsAsync()).ToList();
                    professorEventsToSeeAsStudent = (await StudentDashboardService.GetPublishedProfessorEventsAsync()).ToList();

                    // Refresh interest IDs from dashboard cache
                    _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                    alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                    interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                }

                isCompanyEventsVisibleToSeeAsStudent = !isCompanyEventsVisibleToSeeAsStudent;
                isProfessorEventsVisibleToSeeAsStudent = !isProfessorEventsVisibleToSeeAsStudent;

                currentPageForEvents = 1;
                UpdateTotalPages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading events: {ex.Message}");
            }
            finally
            {
                isLoadingEvents = false;
                StateHasChanged();
            }
        }

        private void UpdateTotalPages()
        {
            int totalItems = 0;
            if (selectedEventType == "all" || selectedEventType == "company")
                totalItems += companyEventsToSeeAsStudent.Count;
            if (selectedEventType == "all" || selectedEventType == "professor")
                totalItems += professorEventsToSeeAsStudent.Count;
            
            totalPagesForEvents = (int)Math.Ceiling((double)totalItems / itemsPerPageForEvents);
            if (totalPagesForEvents < 1) totalPagesForEvents = 1;
        }

        private IEnumerable<CompanyEvent> GetPaginatedCompanyEvents_StudentSearchEvents()
        {
            if (selectedEventType == "professor") return Enumerable.Empty<CompanyEvent>();
        
            var filtered = companyEventsToSeeAsStudent;
            if (selectedEventType == "all" || selectedEventType == "company")
            {
                return filtered
                    .Skip((currentPageForEvents - 1) * itemsPerPageForEvents)
                    .Take(itemsPerPageForEvents);
            }
            return Enumerable.Empty<CompanyEvent>();
        }

        private IEnumerable<ProfessorEvent> GetPaginatedProfessorEvents_StudentSearchEvents()
        {
            if (selectedEventType == "company") return Enumerable.Empty<ProfessorEvent>();
        
            var filtered = professorEventsToSeeAsStudent;
            if (selectedEventType == "all" || selectedEventType == "professor")
            {
                return filtered
                    .Skip((currentPageForEvents - 1) * itemsPerPageForEvents)
                    .Take(itemsPerPageForEvents);
            }
            return Enumerable.Empty<ProfessorEvent>();
        }

        // Pagination Methods
        private void GoToFirstPageForEvents()
        {
            currentPageForEvents = 1;
            StateHasChanged();
        }

        private void PreviousPageForEvents()
        {
            if (currentPageForEvents > 1)
            {
                currentPageForEvents--;
                StateHasChanged();
            }
        }

        private void NextPageForEvents()
        {
            if (currentPageForEvents < totalPagesForEvents)
            {
                currentPageForEvents++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForEvents()
        {
            currentPageForEvents = totalPagesForEvents;
            StateHasChanged();
        }

        private void GoToPageForEvents(int page)
        {
            if (page >= 1 && page <= totalPagesForEvents)
            {
                currentPageForEvents = page;
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

        // Transport Toggle
        private void ToggleTransport(long eventRNG, object value)
        {
            bool needsTransport = (bool)(value ?? false);
            if (selectedEvent is CompanyEvent companyEvent)
            {
                needsTransportForCompanyEvent[eventRNG] = needsTransport;
            }
            else if (selectedEvent is ProfessorEvent professorEvent)
            {
                needsTransportForProfessorEvent[eventRNG] = needsTransport;
            }
            StateHasChanged();
        }

        // Modal Visibility Properties
        private bool isModalOpenForCompanyEventToSeeAsStudent = false;
        private bool isModalOpenForProfessorEventToSeeAsStudent = false;
        private bool isModalOpenForCompanyDetailsToSeeAsStudent = false;
        private bool isModalOpenForProfessorDetailsToSeeAsStudent = false;
        private bool isJobDetailsModal2Visible = false;
        private bool isModalVisibleForProfessorInternshipsAsStudent = false;
        private bool showLoadingModalWhenShowInterestForCompanyEventAsStudent = false;
        private bool showLoadingModalWhenShowInterestForProfessorEventAsStudent = false;
        private int loadingProgressWhenShowInterestForCompanyEventAsStudent = 0;
        private int loadingProgressWhenShowInterestForProfessorEventAsStudent = 0;

        // Pagination Options
        private int[] pageSizeOptions_SearchForEventsAsStudent = new[] { 10, 50, 100 };

        // Event Details Methods
        private void ShowCompanyEventDetails(CompanyEvent eventDetails)
        {
            currentCompanyEvent = eventDetails;
            isModalOpenForCompanyEventToSeeAsStudent = true;
            StateHasChanged();
        }

        private void ShowProfessorEventDetails(ProfessorEvent eventDetails)
        {
            currentProfessorEvent = eventDetails;
            isModalOpenForProfessorEventToSeeAsStudent = true;
            StateHasChanged();
        }

        // Modal Close Methods
        private void CloseModalForCompanyEventToSeeAsStudent()
        {
            isModalOpenForCompanyEventToSeeAsStudent = false;
            currentCompanyEvent = null;
            StateHasChanged();
        }

        private void CloseModalForProfessorEventToSeeAsStudent()
        {
            isModalOpenForProfessorEventToSeeAsStudent = false;
            currentProfessorEvent = null;
            StateHasChanged();
        }

        private void ShowCompanyDetailsModalAtEventsAsStudent(Company company)
        {
            currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents = company;
            isModalOpenForCompanyDetailsToSeeAsStudent = true;
            StateHasChanged();
        }

        private void CloseCompanyDetailsModalAtEventsAsStudent()
        {
            isModalOpenForCompanyDetailsToSeeAsStudent = false;
            currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents = null;
            StateHasChanged();
        }

        private void ShowProfessorDetailsModalAtEventsAsStudent(QuizManager.Models.Professor professor)
        {
            currentProfessorDetailsToShowOnHyperlinkAsStudentForProfessorEvents = professor;
            isModalOpenForProfessorDetailsToSeeAsStudent = true;
            StateHasChanged();
        }

        private void CloseProfessorDetailsModalAtEventsAsStudent()
        {
            isModalOpenForProfessorDetailsToSeeAsStudent = false;
            currentProfessorDetailsToShowOnHyperlinkAsStudentForProfessorEvents = null;
            StateHasChanged();
        }

        private void CloseJobDetailsModal()
        {
            isJobDetailsModal2Visible = false;
            currentJob = null;
            StateHasChanged();
        }

        private void CloseProfessorInternshipDetailsModal()
        {
            isModalVisibleForProfessorInternshipsAsStudent = false;
            currentProfessorInternship = null;
            StateHasChanged();
        }

        // Profile Image
        private string ShowProfileImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "/images/default-profile.png";
            return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
        }

        // Event Status Change
        private async Task OnEventStatusChanged(ChangeEventArgs e)
        {
            selectedEventStatus = e.Value?.ToString() ?? "all";
            await OnEventStatusChange();
        }

        private async Task OnEventStatusChange()
        {
            if (isCompanyEventsVisibleToSeeAsStudent || isProfessorEventsVisibleToSeeAsStudent)
            {
                await ToggleAndLoadCompanyAndProfessorEventsAsStudent();
            }
        }

        // Page Size Change
        private void OnPageSizeChange_SearchForEventsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                itemsPerPageForEvents = newSize;
                currentPageForEvents = 1;
                UpdateTotalPages();
                StateHasChanged();
            }
        }

        // Helper: Get Student Details
        private async Task<Student> GetStudentDetails(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await StudentDashboardService.GetStudentByEmailAsync(email);
        }

        // Progress Update Methods
        private async Task UpdateProgressWhenShowInterestForCompanyEventAsStudent(int targetProgress, int delayMs)
        {
            int steps = targetProgress - loadingProgressWhenShowInterestForCompanyEventAsStudent;
            if (steps <= 0) return;

            for (int i = 0; i < steps; i++)
            {
                loadingProgressWhenShowInterestForCompanyEventAsStudent++;
                StateHasChanged();
                await Task.Delay(delayMs / steps);
            }
        }

        private async Task UpdateProgressWhenShowInterestForProfessorEventAsStudent(int targetProgress, int delayMs)
        {
            int steps = targetProgress - loadingProgressWhenShowInterestForProfessorEventAsStudent;
            if (steps <= 0) return;

            for (int i = 0; i < steps; i++)
            {
                loadingProgressWhenShowInterestForProfessorEventAsStudent++;
                StateHasChanged();
                await Task.Delay(delayMs / steps);
            }
        }

        private async Task<bool> ShowInterestInCompanyEvent(CompanyEvent companyEvent, bool needsTransport)
        {
            showLoadingModalWhenShowInterestForCompanyEventAsStudent = true;
            loadingProgressWhenShowInterestForCompanyEventAsStudent = 0;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenShowInterestForCompanyEventAsStudent(50, 300);

                var startingPoint = selectedStartingPoint.TryGetValue(companyEvent.RNGForEventUploadedAsCompany, out var start) ? start : string.Empty;
                var result = await StudentDashboardService.ShowInterestInCompanyEventAsync(
                    companyEvent.RNGForEventUploadedAsCompany,
                    needsTransport,
                    startingPoint);

                if (result == null)
                {
                    return false;
                }

                alreadyInterestedCompanyEventIds.Add(companyEvent.RNGForEventUploadedAsCompany);
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();

                await UpdateProgressWhenShowInterestForCompanyEventAsStudent(100, 300);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing interest: {ex.Message}");
                return false;
            }
            finally
            {
                showLoadingModalWhenShowInterestForCompanyEventAsStudent = false;
                StateHasChanged();
            }
        }

        private async Task<bool> ShowInterestInProfessorEvent(ProfessorEvent professorEvent, bool needsTransport)
        {
            showLoadingModalWhenShowInterestForProfessorEventAsStudent = true;
            loadingProgressWhenShowInterestForProfessorEventAsStudent = 0;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenShowInterestForProfessorEventAsStudent(50, 300);

                var startingPoint = selectedStartingPoint.TryGetValue(professorEvent.RNGForEventUploadedAsProfessor, out var start) ? start : string.Empty;
                var result = await StudentDashboardService.ShowInterestInProfessorEventAsync(
                    professorEvent.RNGForEventUploadedAsProfessor,
                    needsTransport,
                    startingPoint);

                if (result == null)
                {
                    return false;
                }

                interestedProfessorEventIds.Add(professorEvent.RNGForEventUploadedAsProfessor);
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                alreadyInterestedCompanyEventIds = _dashboardData.CompanyEventInterestIds?.ToHashSet() ?? new HashSet<long>();
                interestedProfessorEventIds = _dashboardData.ProfessorEventInterestIds?.ToHashSet() ?? new HashSet<long>();

                await UpdateProgressWhenShowInterestForProfessorEventAsStudent(100, 300);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing interest: {ex.Message}");
                return false;
            }
            finally
            {
                showLoadingModalWhenShowInterestForProfessorEventAsStudent = false;
                StateHasChanged();
            }
        }
    }
}
