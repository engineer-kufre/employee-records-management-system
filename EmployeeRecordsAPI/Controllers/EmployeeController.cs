using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EmployeeRecordsAPI.DTOs;
using EmployeeRecordsAPI.Models;
using EmployeeRecordsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeRecordsAPI.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly UserManager<Employee> _userManager;
        private readonly SignInManager<Employee> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public EmployeeController(ILogger<EmployeeController> logger, UserManager<Employee> userManager, SignInManager<Employee> signInManager, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        //method to register a new user
        // /employee/register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterAsync([FromBody] SignUpDTO model)
        {
            UserManagerResponse response = null;
            if (ModelState.IsValid)
            {
                //check model object exists
                if (model == null)
                {
                    throw new NullReferenceException("Register model is null");
                }

                //confirm password
                if (model.Password != model.ConfirmPassword)
                {
                    response = new UserManagerResponse
                    {
                        Message = "Password does not match",
                        IsSuccess = false
                    };

                    return BadRequest(response);
                }

                //search database for an employee with same email as inputted email
                var emailMatch = await _userManager.FindByEmailAsync(model.Email);

                //email already taken(match found)
                if (emailMatch != null)
                {
                    response = new UserManagerResponse
                    {
                        Message = "Email already taken",
                        IsSuccess = false
                    };

                    return BadRequest(response);
                }

                //create a employee from data transfer object model
                Department department = _context.Departments.FirstOrDefault(d => d.DepartmentName == model.DepartmentName);

                string photo = "";
                if(model.Photo == "")
                {
                    photo = "https://ibb.co/sJTGnYs";
                } else
                {
                    photo = model.Photo;
                }

                var employee = new Employee
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    UserName = model.UserName,
                    DepartmentID = department.DepartmentId,
                    Photo = photo
                };

                //add employee to database
                var result = await _userManager.CreateAsync(employee, model.Password);

                //return success or error message
                if (result.Succeeded)
                {
                    response = new UserManagerResponse
                    {
                        Message = "User created",
                        IsSuccess = true
                    };

                    return Ok(response);
                }
                else
                {
                    response = new UserManagerResponse
                    {
                        Message = "User was not created",
                        IsSuccess = false,
                        Errors = result.Errors.Select(e => e.Description)
                    };

                    return BadRequest(response);
                }
            }
            else
            {
                return BadRequest("Some model properties are not valid");
            }
        }

        //method to login an existing user
        // /employee/login
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDTO model)
        {
            UserManagerResponse response = null;
            if (ModelState.IsValid)
            {
                //search database for an employee with inputted email
                var matchEmail = await _userManager.FindByEmailAsync(model.Email);

                //check that returned user's password matches that inputted
                var matchPassword = await _userManager.CheckPasswordAsync(matchEmail, model.Password);

                //check that an employee with that email and password exists in the database
                if (matchEmail == null || !matchPassword)
                {
                    response = new UserManagerResponse
                    {
                        Message = "Invalid credentials!",
                        IsSuccess = false
                    };

                    return BadRequest(response);
                }
                else
                {
                    //sign in
                    await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: model.RememberMe, false);

                    //create claims array
                    var claims = new[]
                    {
                        new Claim("Email", model.Email),
                        new Claim(ClaimTypes.NameIdentifier, matchEmail.Id)
                    };

                    //obtain JWT secret key to encrypt token
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

                    //generate signin credentials
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                    //create security token descriptor(builds the token)
                    var securityTokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = DateTime.Now.AddDays(1), //how many days before token expires
                        SigningCredentials = creds
                    };

                    //build token handler
                    var tokenHandler = new JwtSecurityTokenHandler();

                    //create token
                    var token = tokenHandler.CreateToken(securityTokenDescriptor);

                    var tokenAsString = tokenHandler.WriteToken(token);

                    response = new UserManagerResponse
                    {
                        Message = tokenAsString,
                        IsSuccess = true,
                        TokenExpiryDate = token.ValidTo
                    };

                    return Ok(response);
                }
            }
            else
            {
                return BadRequest("Some model properties are not valid");
            }
        }


        //method to fetch all registered users
        // /employee/allemployees
        [AllowAnonymous]
        [HttpGet("AllEmployees")]
        public IActionResult AllEmployees(int page = 1)
        {
            int zeroIndexedPage = page;
            int start = (zeroIndexedPage - 1) * 5;

            //get all employees from AspNetUsers table and include their departments
            var allEmployees = _userManager.Users
                                                  .Include(e => e.Department)
                                                  .Skip(start)
                                                  .Take(5)
                                                  .Select(e => new ReturnedUser {
                                                        FirstName = e.FirstName,
                                                        LastName = e.LastName,
                                                        Email = e.Email,
                                                        Photo = e.Photo,
                                                        Department = e.Department.DepartmentName
                                                  }).ToList();

            var result = new PaginatedReturnedUsersDTO
            {
                CurrentPage = $"Page {page}",
                ReturnedUsers = allEmployees
            };

            return Ok(result);
        }

        //add department
        //remove department
        //modify department
        //modify user
        //remove user
        //add roles
        //add authorization by role
    }
}
