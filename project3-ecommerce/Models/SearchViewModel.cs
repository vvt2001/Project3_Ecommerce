using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList;
namespace project3_ecommerce.Models
{
    public class SearchViewModel
    {
        public SearchModel SearchModel { get; set; }
        public IPagedList<Product> ProductModel { get; set; }
    }
}