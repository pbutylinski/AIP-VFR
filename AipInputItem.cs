using System.Linq;

namespace AIP_VFR
{
    partial class Program
    {
        public class AipInputItem
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Url { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public string Filename => this.Url?.Split('/')?.Last();
        }
    }
}
