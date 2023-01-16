using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace project3_ecommerce.Models
{
    using System.Web;
    using System.ComponentModel.DataAnnotations;
    public class EmpFileModel
    {
        [DataType(DataType.Upload)]
        public HttpPostedFileBase[] fileUpload { get; set; }
    }
}