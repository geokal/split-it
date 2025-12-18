using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SplitIt.Shared.ResearchGroup
{
    public partial class ResearchGroupEventsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;

        // Calendar State
        private DateTime currentMonth = DateTime.Today;
        private string[] daysOfWeek = { "Δ", "Τ", "Τ", "Π", "Π", "Σ", "Κ" };
        private int selectedDay = 0;
        private int highlightedDay = 0;
        private DateTime? selectedDate;

        // Computed Properties for Calendar
        private int firstDayOfMonth => (int)new DateTime(currentMonth.Year, currentMonth.Month, 1).DayOfWeek;
        private int daysInCurrentMonth => DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        private int adjustedFirstDayOfMonth => (firstDayOfMonth == 0) ? 6 : firstDayOfMonth - 1;

        // Events Data
        private Dictionary<int, List<CompanyEvent>> eventsForDate = new Dictionary<int, List<CompanyEvent>>();
        private Dictionary<int, List<ProfessorEvent>> eventsForDateForProfessors = new Dictionary<int, List<ProfessorEvent>>();
        public List<CompanyEvent> CompanyEventsToShowAtFrontPage { get; set; } = new List<CompanyEvent>();
        public List<ProfessorEvent> ProfessorEventsToShowAtFrontPage { get; set; } = new List<ProfessorEvent>();

        // Modal State
        private bool isModalVisibleToShowEventsOnCalendarForEachClickedDay = false;
        private List<CompanyEvent> selectedDateEvents = new List<CompanyEvent>();
        private List<ProfessorEvent> selectedProfessorDateEvents = new List<ProfessorEvent>();

        // Event Filter
        private string selectedEventFilter { get; set; } = "All";
        private List<CompanyEvent> filteredCompanyEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Company"
                ? selectedDateEvents
                : new List<CompanyEvent>();

        private List<ProfessorEvent> filteredProfessorEvents =>
            selectedEventFilter == "All" || selectedEventFilter == "Professor"
                ? selectedProfessorDateEvents
                : new List<ProfessorEvent>();

        // Event Details
        private CompanyEvent companyEventDetails;
        private ProfessorEvent professorEventDetails;
        private bool isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadEventsData();
            LoadEventsForCalendar();
        }

        private async Task LoadEventsData()
        {
            CompanyEventsToShowAtFrontPage = await FetchCompanyEventsAsync();
            ProfessorEventsToShowAtFrontPage = await FetchProfessorEventsAsync();
        }

        private async Task<List<CompanyEvent>> FetchCompanyEventsAsync()
        {
            return await dbContext.CompanyEvents.AsNoTracking().ToListAsync();
        }

        private async Task<List<ProfessorEvent>> FetchProfessorEventsAsync()
        {
            return await dbContext.ProfessorEvents.AsNoTracking().ToListAsync();
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

            if (highlightedDay != 0 && !eventsForDate.ContainsKey(highlightedDay) && !eventsForDateForProfessors.ContainsKey(highlightedDay))
            {
                highlightedDay = 0;
            }

            if (selectedDay != 0 && (eventsForDate.ContainsKey(selectedDay) || eventsForDateForProfessors.ContainsKey(selectedDay)))
            {
                highlightedDay = selectedDay;
            }

            StateHasChanged();
        }

        // Calendar Navigation
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

        // Date Click Handler
        private void OnDateClicked(DateTime clickedDate)
        {
            selectedDay = clickedDate.Day;
            highlightedDay = selectedDay;
            selectedDate = clickedDate;

            selectedDateEvents = dbContext.CompanyEvents
                .Include(e => e.Company)
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη" &&
                        e.CompanyEventActiveDate.Date == clickedDate.Date)
                .ToList();

            selectedProfessorDateEvents = dbContext.ProfessorEvents
                .Include(e => e.Professor)
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη" &&
                        e.ProfessorEventActiveDate.Date == clickedDate.Date)
                .ToList();

            if (selectedDateEvents.Any() || selectedProfessorDateEvents.Any())
            {
                isModalVisibleToShowEventsOnCalendarForEachClickedDay = true;
            }

            StateHasChanged();
        }

        // Modal Methods
        private void CloseModalForCompanyAndProfessorEventTitles()
        {
            isModalVisibleToShowEventsOnCalendarForEachClickedDay = false;
            selectedDateEvents.Clear();
            selectedProfessorDateEvents.Clear();
            LoadEventsForCalendar();
            StateHasChanged();
        }

        private void ShowEventDetails(object eventObj)
        {
            if (eventObj is CompanyEvent companyEvent)
            {
                companyEventDetails = companyEvent;
                professorEventDetails = null;
            }
            else if (eventObj is ProfessorEvent professorEvent)
            {
                professorEventDetails = professorEvent;
                companyEventDetails = null;
            }
            isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = true;
            StateHasChanged();
        }

        private void CloseEventDetails()
        {
            isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;
            companyEventDetails = null;
            professorEventDetails = null;
            StateHasChanged();
        }
    }
}

