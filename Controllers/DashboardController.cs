using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            //Last 7 days
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            CultureInfo culture = CultureInfo.CreateSpecificCulture("hi-IN");

            List<Transacion> SelectedTransaction = await _context.Transactions
                .Include(x => x.Category).
                Where(y => y.Date >= startDate && y.Date <= endDate)
                .ToListAsync();


            //Transactions Sheet

            int TotalIncome = SelectedTransaction
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);

            ViewBag.TotalIncome = TotalIncome.ToString("C0", culture);

            int TotalExpense = SelectedTransaction
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);

            ViewBag.TotalExpense = TotalExpense.ToString("C0", culture);

            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = Balance.ToString("C0", culture);


            //Doughnut for Expense

            ViewBag.DoughnutChart = SelectedTransaction
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(i => i.Amount),
                    formattedAmount = k.Sum(i => i.Amount).ToString("C0", culture)
                })
                .OrderByDescending(l => l.amount)
                .ToList();


            //Spline Chart

            List<SplineChartData> IncomeSummary = SelectedTransaction
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(i => i.Amount)
                })
                .ToList();

            List<SplineChartData> ExpenseSummary = SelectedTransaction
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(i => i.Amount)
                })
                .ToList();

            string[] last7days = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in last7days
                                      join income in IncomeSummary on day equals income.day into dayIncome
                                      from income in dayIncome.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpense
                                      from expense in dayExpense.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense
                                      };


            //Recent Transactions

            ViewBag.RecentTransaction = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
