using EmployeeRecordsAPI.Services;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeRecordsAPI.Models
{
    //feeds the database with demo data for testing and staging
    public class Seeder
    {
        public static async Task SeederAsync(AppDbContext context, RoleManager<IdentityRole> roleManager, UserManager<Employee> userManager)
        {
            //ensure that a database exists 
            context.Database.EnsureCreated();

            //if no departments exist in the database
            var allDepartments = context.Departments.ToList();
            if (allDepartments.Count == 0)
            {
                Department department = new Department { DepartmentName = "HR" };
                context.Departments.Add(department);
                context.SaveChanges();
            }

            //if no roles exist in the database
            if (!roleManager.Roles.Any())
            {
                //create list of roles
                var allRoles = new List<IdentityRole>
                {
                    new IdentityRole("Employee")
                };

                //add role(s) to the database
                foreach (var role in allRoles)
                {
                    await roleManager.CreateAsync(role);
                }
            }

            if (!userManager.Users.Any())
            {
                //create list of employees
                var allEmployees = new List<Employee>
                {
                    new Employee{FirstName="Segun", LastName="Adaramaja", Email="seguna@gmail.com", UserName="seguna@gmail.com", PhoneNumber="08095784765", DepartmentID=1, Photo="https://ibb.co/sJTGnYs"},
                    new Employee{FirstName="Seun", LastName="Oyetoyan", Email="seuno@gmail.com", UserName="seuno@gmail.com", PhoneNumber="07057893783", DepartmentID=1, Photo="https://ibb.co/sJTGnYs"},
                    new Employee{FirstName="Micheal", LastName="Nwosu", Email="miken@gmail.com", UserName="miken@gmail.com", PhoneNumber="08036754890", DepartmentID=1, Photo="https://ibb.co/sJTGnYs"}
                };

                //add employee(s) to the database
                foreach (var employee in allEmployees)
                {
                    var result = await userManager.CreateAsync(employee, "P@$$word1");

                    //if employee successfully added
                    if (result.Succeeded)
                    {
                        //add employee role
                        await userManager.AddToRoleAsync(employee, "Employee");
                    }
                }
            }
        }
    }
}
