using Microsoft.AspNetCore.Mvc;
using AuthAppFrontend.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
namespace AuthAppFrontend.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;

        public AuthController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // GET: Auth/Register
        public IActionResult Register()
        {
            ViewData["Message"] = null;
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            // Use specific registration endpoint
            var response = await _httpClient.PostAsJsonAsync("https://fqzjarsewl.execute-api.us-east-2.amazonaws.com/Prod/api/values/signup", user);
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Registration successful! You can now log in.";
                return RedirectToAction("Login");
            }
            ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
            return View(user);
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            ViewData["Message"] = TempData["Message"];
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            var response = await _httpClient.PostAsJsonAsync("https://fqzjarsewl.execute-api.us-east-2.amazonaws.com/Prod/api/values/login", new { user.Email, user.passwordHash });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

                if (result.TryGetValue("token", out var token))
                {
                    HttpContext.Session.SetString("JwtToken", token);

                    // Decode the token to extract claims
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown User";
                    var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    var profileImageUrl = jwtToken.Claims.FirstOrDefault(c => c.Type == "profileImageUrl")?.Value;

                    HttpContext.Session.SetString("UserName", name);
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("ProfileImageUrl", profileImageUrl);

                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Token not found in response.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            }

            return View(user);
        }


        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
      
             var imageUrl = HttpContext.Session.GetString("ProfileImageUrl");
             
            
           
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var profile = new
            {
                Name = HttpContext.Session.GetString("UserName") ?? "Unknown User",
                Email = email,
                ImageUrl = imageUrl ?? "/img/default-profile.png"
            };

            ViewData["Profile"] = profile;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile profileImage)
        {
            var file = profileImage;
            if (file == null || file.Length == 0)
            {
                ViewData["Message"] = "Please select a valid image file.";
                return RedirectToAction("Profile");
            }

            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login");
                }

                using (var content = new MultipartFormDataContent())
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


                    var streamContent = new StreamContent(file.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    content.Add(streamContent, "file", file.FileName);

                    var response = await client.PostAsync("https://fqzjarsewl.execute-api.us-east-2.amazonaws.com/Prod/api/values/upload", content);

                   

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var imageUrl = JsonConvert.DeserializeObject<dynamic>(result).imageUrl?.ToString();

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // Serialize the string to a byte array
                            var imageUrlBytes = System.Text.Encoding.UTF8.GetBytes(imageUrl);

                            // Store the byte array in the session
                            HttpContext.Session.Set("ProfileImageUrl", imageUrlBytes);
                        }

                        ViewData["Message"] = "Image uploaded successfully.";
                    }
                    else
                    {
                        ViewData["Message"] = $"Image upload failed: {response.ReasonPhrase}";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["Message"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }


    }
}
