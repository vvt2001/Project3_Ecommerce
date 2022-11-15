using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace project3_ecommerce.Models
{
    public class SearchModel
    {
        public string Name { get; set; }
        public long? LowerPrice { get; set; }
        public long? UpperPrice { get; set; }
        public Category? Category { get; set; }
    }
}