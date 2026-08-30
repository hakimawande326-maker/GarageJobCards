using System.Collections.Generic;

namespace GarageJobCards.Models
{
    public class AnalyticsViewModel
    {
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int CancelledJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AvgTurnaroundHours { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalMechanics { get; set; }

        public List<StatusCount> StatusBreakdown { get; set; }
        public List<MechanicStat> MechanicPerformance { get; set; }

        public AnalyticsViewModel()
        {
            StatusBreakdown = new List<StatusCount>();
            MechanicPerformance = new List<MechanicStat>();
        }
    }

    public class StatusCount
    {
        public JobStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class MechanicStat
    {
        public string Name { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
    }
}
