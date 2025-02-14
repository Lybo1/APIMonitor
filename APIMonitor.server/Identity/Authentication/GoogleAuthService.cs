using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity.Authentication;

public class GoogleAuthService
{
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<GoogleAuthService> logger;

    public GoogleAuthService(UserManager<User> userManager, 
                            SignInManager<User> signInManager, 
                            IHttpClientFactory httpClientFactory, 
                            IConfiguration configuration, 
                            ILogger<GoogleAuthService> logger
    )
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> AuthenticateAsync(HttpContext? httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        
        AuthenticateResult authResult = await httpContext.AuthenticateAsync();

        if (!authResult.Succeeded)
        {
            return null;
        }
        
        IEnumerable<AuthenticationToken>? tokens = authResult.Properties?.GetTokens();
        AuthenticationToken? idToken = tokens?.FirstOrDefault(t => t.Name == "id_token");

        if (string.IsNullOrWhiteSpace(idToken?.Value))
        {
            return null;
        }

        try
        {
            HttpClient client = httpClientFactory.CreateClient();
            
            string tokenInfoEndpoint = configuration["Authentication:Google:TokenInfoUrl"];

            if (string.IsNullOrWhiteSpace(tokenInfoEndpoint))
            {
                tokenInfoEndpoint = "https://oauth2.googleapis.com/tokeninfo?id_token=";
            }
            
            HttpResponseMessage response = await client.GetAsync($"{tokenInfoEndpoint}{idToken}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            string jsonPayload = await response.Content.ReadAsStringAsync();

            var tokenPayload = JsonSerializer.Deserialize<GoogleTokenPayload>(jsonPayload);

            if ()
            {
                
            }


        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        
    }
}