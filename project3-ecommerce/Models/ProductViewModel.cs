using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace project3_ecommerce.Models
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public IEnumerable<Info> Info { get; set; }
        public EmpFileModel EmpFileModel { get; set; }
    }
}