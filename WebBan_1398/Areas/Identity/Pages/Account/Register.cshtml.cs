// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using WebBan_1398.Models;

namespace WebBan_1398.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public bool CanSelectRole { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Display(Name = "Địa chỉ")]
            public string Address { get; set; }

            [Display(Name = "Tuổi")]
            public string Age { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }

            public string Role { get; set; }

            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; } = Enumerable.Empty<SelectListItem>();
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            await EnsureRolesCreatedAsync();
            await LoadRoleDataAsync();

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            await EnsureRolesCreatedAsync();
            await LoadRoleDataAsync();
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!CanSelectRole || string.IsNullOrWhiteSpace(Input.Role))
            {
                Input.Role = SD.Role_Customer;
            }

            if (!ModelState.IsValid)
            {
                ReturnUrl = returnUrl;
                return Page();
            }

            var user = CreateUser();
            user.FullName = Input.FullName;
            user.Address = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim();
            user.Age = string.IsNullOrWhiteSpace(Input.Age) ? null : Input.Age.Trim();

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _userManager.AddToRoleAsync(user, Input.Role);

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (User.IsInRole(SD.Role_Admin))
                {
                    TempData["SuccessMessage"] = $"Đã tạo tài khoản {Input.Email} với quyền {Input.Role}.";
                    return LocalRedirect("/Admin");
                }

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công.";
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

        private async Task EnsureRolesCreatedAsync()
        {
            string[] roles = [SD.Role_Customer, SD.Role_Employee, SD.Role_Admin, SD.Role_Company];

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task LoadRoleDataAsync()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
            CanSelectRole = User.IsInRole(SD.Role_Admin) || adminUsers.Count == 0;

            var availableRoles = CanSelectRole
                ? _roleManager.Roles.Select(role => role.Name).Where(role => !string.IsNullOrWhiteSpace(role))
                : Enumerable.Empty<string>();

            Input ??= new InputModel();
            Input.RoleList = availableRoles.Select(role => role!).Select(role => new SelectListItem
            {
                Text = role,
                Value = role
            });
        }
    }
}
