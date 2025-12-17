using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SplitIt.Shared.Professor
{
    public partial class ProfessorEventsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

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
        }

        private async Task LoadInitialData()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
            }

            CompanyEventsToShowAtFrontPage = await FetchCompanyEventsAsync();
            ProfessorEventsToShowAtFrontPage = await FetchProfessorEventsAsync();
            LoadEventsForCalendar();
        }

        private async Task<List<CompanyEvent>> FetchCompanyEventsAsync()
        {
            var companyevents = await dbContext.CompanyEvents
                .Include(e => e.Company)
                .AsNoTracking()
                .ToListAsync();
            return companyevents;
        }

        private async Task<List<ProfessorEvent>> FetchProfessorEventsAsync()
        {
            var professorevents = await dbContext.ProfessorEvents
                .Include(e => e.Professor)
                .AsNoTracking()
                .ToListAsync();
            return professorevents;
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

            // Retrieve the latest event status
            var latestEvent = await dbContext.CompanyEvents
                .AsNoTracking()
                .Where(e => e.RNGForEventUploadedAsCompany == companyEvent.RNGForEventUploadedAsCompany)
                .Select(e => new { e.CompanyEventStatus })
                .FirstOrDefaultAsync();

            if (latestEvent == null || latestEvent.CompanyEventStatus != "Δημοσιευμένη")
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

            // Check for existing interest
            var existingInterest = await dbContext.InterestInCompanyEventsAsProfessor
                .FirstOrDefaultAsync(i => 
                    i.ProfessorEmailShowInterestForCompanyEvent == professor.ProfEmail &&
                    i.RNGForCompanyEventInterestAsProfessor == companyEvent.RNGForEventUploadedAsCompany);

            if (existingInterest != null)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", $"Έχετε ήδη δείξει ενδιαφέρον για: {companyEvent.CompanyEventTitle}!");
                return false;
            }

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create main interest record with details
                var interest = new InterestInCompanyEventAsProfessor
                {
                    RNGForCompanyEventInterestAsProfessor = companyEvent.RNGForEventUploadedAsCompany,
                    RNGForCompanyEventInterestAsProfessor_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID,
                    DateTimeProfessorShowInterestForCompanyEvent = DateTime.Now,
                    CompanyEventStatus_ShowInterestAsProfessor_AtCompanySide = "Προς Επεξεργασία",
                    CompanyEventStatus_ShowInterestAsProfessor_AtProfessorSide = "Έχετε Δείξει Ενδιαφέρον",
                    ProfessorEmailShowInterestForCompanyEvent = professor.ProfEmail,
                    ProfessorUniqueIDShowInterestForCompanyEvent = professor.Professor_UniqueID,
                    CompanyEmailWhereProfessorShowedInterest = companyEvent.CompanyEmailUsedToUploadEvent,
                    CompanyUniqueIDWhereProfessorShowedInterest = company.Company_UniqueID,

                    ProfessorDetails = new InterestInCompanyEventAsProfessor_ProfessorDetails
                    {
                        ProfessorUniqueIDShowInterestForCompanyEvent = professor.Professor_UniqueID,
                        ProfessorEmailShowInterestForCompanyEvent = professor.ProfEmail,
                        DateTimeProfessorShowInterestForCompanyEvent = DateTime.Now,
                        RNGForCompanyEventShowInterestAsProfessor_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID
                    },

                    CompanyDetails = new InterestInCompanyEventAsProfessor_CompanyDetails
                    {
                        CompanyUniqueIDWhereProfessorShowInterestForCompanyEvent = company.Company_UniqueID,
                        CompanyEmailWhereProfessorShowInterestForCompanyEvent = companyEvent.CompanyEmailUsedToUploadEvent
                    }
                };

                dbContext.InterestInCompanyEventsAsProfessor.Add(interest);

                // Add platform action
                dbContext.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "PROFESSOR",
                    ForWhat_PerformedAction = "COMPANY_EVENT",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany),
                    TypeOfAction_PerformedAction = "SHOW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send emails
                try
                {
                    await InternshipEmailService.SendConfirmationToProfessorForInterestInCompanyEvent(
                        professor.ProfEmail,
                        professor.ProfName,
                        professor.ProfSurname,
                        companyEvent.CompanyEventTitle,
                        company.CompanyName);

                    await InternshipEmailService.SendNotificationToCompanyForProfessorInterestInEvent(
                        companyEvent.CompanyEmailUsedToUploadEvent,
                        company.CompanyName,
                        companyEvent.CompanyEventTitle,
                        professor.ProfName,
                        professor.ProfSurname);
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
                await transaction.RollbackAsync();
                Console.WriteLine($"Error showing interest in company event: {ex.Message}");
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Σφάλμα κατά την ένδειξη ενδιαφέροντος. Παρακαλώ δοκιμάστε ξανά.");
                return false;
            }
        }

        private async Task<Professor> GetProfessorDetails(string email)
        {
            return await dbContext.Professors
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfEmail == email);
        }

        private async Task<Company> GetCompanyDetails(string email)
        {
            return await dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail == email);
        }

        public bool HasAlreadyExpressedInterest(CompanyEvent companyEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserEmail) || companyEvent == null)
                return false;

            return dbContext.InterestInCompanyEventsAsProfessor
                .Any(i => i.RNGForCompanyEventInterestAsProfessor == companyEvent.RNGForEventUploadedAsCompany 
                        && i.ProfessorEmailShowInterestForCompanyEvent == CurrentUserEmail);
        }
    }
}

