using System.Collections.Generic;

namespace QuizManager.ViewModels
{
    public class Region
    {
        public int Id { get; set; }
        public string RegionName { get; set; }
        public List<Town> Towns { get; set; } = new List<Town>();
    }
}
