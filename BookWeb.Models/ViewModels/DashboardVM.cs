using System.Collections.Generic;

namespace BookWeb.Models.ViewModels
{
    public class DashboardVM
    {
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int UserCount { get; set; }
        public int AdminCount { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
    }
}
