using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public class TokenManager
{

        private IJavaScriptExecutor js;
        private IWebDriver Driver;

        private string loginUrl;

        public TimeSpan WAIT_TIME = TimeSpan.FromSeconds(60);

        public string accessTokenKey = "token";

        public string refreshTokenKey = "refreshToken";

        public bool hasRefreshToken = true;

        private string storeLocation = "localStorage";
        
        public string tokensKey = "tokens";

        public bool json = false;
    

        public TokenManager (IWebDriver driver, string loginUrl)
        {
            Driver = driver;
            js = (IJavaScriptExecutor)Driver;
            this.loginUrl = loginUrl;
        }

        public void StoreToken(){
            WriteToken();
        }

        public void UseTokens(){
            var tokens = GetActiveTokens();
            AddItem(accessTokenKey, tokens["access"]);
            if (hasRefreshToken) AddItem(refreshTokenKey, tokens["refresh"]);
        }

        public void StoringMethod(StoreLocation storeLocation){
            switch(storeLocation){
                case StoreLocation.LOCAL_STORAGE:
                    this.storeLocation = "localStorage";
                    break;
                case StoreLocation.SESSION_STORAGE:
                    this.storeLocation = "sessionStorage";
                    break;
            }
        }

        public string GetAccessToken(){
            if(json){
                var tokens = GetTokens();
                return tokens?[accessTokenKey]?? "";
            }
            return GetItem(accessTokenKey);
        }

        public string GetRefreshToken(){
            if(json){
                var tokens = GetTokens();
                return tokens?[refreshTokenKey]?? "";
            }
            return GetItem(refreshTokenKey);
        }

        /// <summary>
        /// Adds key and value to browser window's storage.
        /// </summary>
        public void AddItem(string key, string value){
            js.ExecuteScript($"window.{storeLocation}.setItem('{key}','{value}')");
        }

        /// <summary>
        /// Ensures the user is logged in by checking the url, throws exception if not logged in
        /// </summary>
        /// <exception cref="Exception">if the user is not logged in</exception>
        public void EnsureLogin()
        {
            if (Driver.Url.Equals(loginUrl)) throw new Exception("Login failed");
        }

        /// <summary>
        /// Check the message for invalid login data 
        /// </summary>
        public string Get_InvalidLoginMessageError => Driver.FindElement(By.ClassName("text-error-shade-3")).Text;

        /// <summary>
        /// Saves a token in token.txt file, just incase to avoid session lockout
        /// </summary>
        private void WriteToken()
        {
            string accessToken = GetAccessToken();
            string refreshToken = GetRefreshToken();

            Console.WriteLine($"access token: {accessToken}");
            StreamWriter sw = File.AppendText(@"./../../../tokens.txt");
            sw.WriteLine(accessToken);
            sw.WriteLine("------------------------");
            sw.Close();
            if (!accessToken.Equals("") || refreshToken.Equals(""))
            {
                sw = File.CreateText(@"./../../../active_token.txt");
                sw.WriteLine(accessToken);
                sw.WriteLine(refreshToken);
                sw.Close();
            }
        }

        private Dictionary<string, string>? GetTokens(){
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(GetItem(tokensKey));
        }

        public string GetItem(string key){
            return (string)js.ExecuteScript($"return window.{storeLocation}.getItem('{key}')");
        }

        /// <summary>
        /// waits for login to finish.
        /// </summary>
        public void WaitForLogin()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException));
            wait.Until(c => !Driver.Url.Equals(loginUrl));
        }

        /// <summary>
        /// Gets the active tokens from active_token.txt file, if file not found it will return empty string
        /// </summary>
        /// <returns>Dictionary of tokens, has ("access" and "refresh") tokens</returns>
        public Dictionary<string, string> GetActiveTokens()
        {
            Dictionary<string, string> tokens = new();
            try
            {
                var file = File.ReadLines(@"./../../../active_token.txt");
                if (file.Any())
                {
                    tokens.Add("access", file.First());
                    tokens.Add("refresh", file.ElementAt(1));
                }
                return tokens;
            }
            catch (FileNotFoundException)
            {
                tokens.Add("access", "");
                tokens.Add("refresh", "");
                return tokens;
            }

        }

        public enum StoreLocation {
            LOCAL_STORAGE,
            SESSION_STORAGE
        }



}
