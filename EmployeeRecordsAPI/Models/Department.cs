using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeRecordsAPI.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(30, ErrorMessage = "Entry is longer than 30 characters")]
        public string DepartmentName { get; set; }
        public ICollection<Employee> Employees { get; set; }
    }
}
