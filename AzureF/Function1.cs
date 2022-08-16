using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AzureF.Models;
using Microsoft.EntityFrameworkCore;


namespace AzureF
{
    public class Function1
    {
        #region Property
        private const string Route = "func";
        private readonly AppDbContext _appDbContext;
        #endregion

        #region Constructor
        public Function1(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        #endregion


        [FunctionName(nameof(UserAuthenication))]
        [Obsolete]
         public static async Task<IActionResult> UserAuthenication(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] UserCredentials userCredentials, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            // TODO: Perform custom authentication here; we're just using a simple hard coded check for this example
            bool authenticated = userCredentials?.User.Equals("syed.raziuddin@mail.weir", StringComparison.InvariantCultureIgnoreCase) ?? false;
            if (!authenticated)
            {
                return await Task.FromResult(new UnauthorizedResult()).ConfigureAwait(false);
            }
            else
            {
                GenerateJWTToken generateJWTToken = new();
                string token = generateJWTToken.IssuingJWT(userCredentials.User);
                return await Task.FromResult(new OkObjectResult(token)).ConfigureAwait(false);
            }
        }

        #region Function Get Employees
        /// <summary>
        /// Get List of Employees
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetAllEmployees")]
        [Obsolete]
        public async Task<IActionResult> GetAllEmployees(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
          HttpRequest req, ILogger log)
        {
            // Check if we have authentication info.
            ValidateJWT auth = new ValidateJWT(req);
            if (!auth.IsValid)
            {
                return new UnauthorizedResult(); // No authentication info.
            }
            try
            {
                log.LogInformation("Getting Employee list items");
                return new OkObjectResult(await _appDbContext.Employee.ToListAsync());
            }
            catch (System.Exception)
            {
                throw;
            }

        }
        #endregion

        #region Get Employee Based on Employee Id
        /// <summary>
        /// Get Employee by Querying with Id
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        [FunctionName("GetEmployeebyId")]
        [Obsolete]
        public async Task<IActionResult> GetEmployeebyId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{Id}")]
          HttpRequest req, ILogger log, int Id)
        {
            // Check if we have authentication info.
            ValidateJWT auth = new ValidateJWT(req);
            if (!auth.IsValid)
            {
                return new UnauthorizedResult(); // No authentication info.
            }
            try
            {
                var result = await _appDbContext.Employee.FindAsync(Id);
                if (result is null)
                {
                    log.LogInformation($"Item {Id} not found");
                    return new NotFoundResult();
                }
                return new OkObjectResult(result);
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        #endregion

        #region Create Employee
        /// <summary>
        /// Create New Employee
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("CreateEmployee")]
        [Obsolete]
        public async Task<IActionResult> CreateEmployee(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route +"/Create")]
          HttpRequest req, ILogger log)
        {
            // Check if we have authentication info.
            ValidateJWT auth = new ValidateJWT(req);
            if (!auth.IsValid)
            {
                return new UnauthorizedResult(); // No authentication info.
            }
            log.LogInformation("Creating a new employee list item");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<Employee>(requestBody);
            var employee = new Employee { Name = input.Name, Designation = input.Designation, City = input.City };
            await _appDbContext.Employee.AddAsync(employee);
            await _appDbContext.SaveChangesAsync();
            return new OkObjectResult(new { Message = "Record Saved SuccessFully", Data = employee });
        }
        #endregion

       
    }

    public class UserCredentials
    {
        public string User
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }
    }

}