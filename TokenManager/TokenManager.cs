using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TokenManager;

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
    

        public TokenManager (IWebDriver driver, string loginUrl)
        {
            Driver = driver;
            js = (IJavaScriptExecutor)Driver;
            this.loginUrl = loginUrl;
        }

        /// <summary>
        /// Quality of life method that do all the steps to login the user
        /// </summary>
        /// <param name="nationalId">National ID to insert</param>
        /// <param name="password">Password to insert</param>
        public void Login(string nationalId, string password)
        {
            // OTP(Config.OTP);
            WaitForLogin();
            WriteToken();
            //EnsureLogin();
            //WaitForUserOTP();
            //WriteToken();
        }

        public void StoreToken(){
            WriteToken();
        }

        public void UseTokens(){
            var tokens = GetActiveTokens();
            AddItem(accessTokenKey, tokens["access"]);
            if (hasRefreshToken) AddItem(refreshTokenKey, tokens["refresh"]);
        }

        /// <summary>
        /// enters the provided OTP into it's input elements
        /// </summary>
        /// <param name="otp">otp numbers to enter</param>
        private void OTP(string otp)
        {
            WebDriverWait wait = new(Driver, TimeSpan.FromSeconds(5));
            wait.Until(c => c.FindElements(By.CssSelector("ng-otp-input input")).Count > 0);
            var otpInputs = Driver.FindElements(By.CssSelector("ng-otp-input input"));
            Console.WriteLine($"length otp inputs: {otpInputs.Count}");
            for (int i = 0; i < otpInputs.Count; i++)
            {
                otpInputs[i].Click();
                otpInputs[i].SendKeys(otp.ElementAt(i).ToString());
            }
            // OTPContinueButton();
        }

        /// <summary>
        /// if token exist in "active_token.txt" file it will use it instead of login, else Login normally and wait for user to enter OTP
        /// </summary>
        private void AddTokenElseWaitForLogin()
        {
            var tokens = GetActiveTokens();
            // DateTime tokenDate = File.GetLastWriteTime(@"./../../../active_token.txt");
            // double tokenAge = (DateTime.Now - tokenDate).TotalHours;
            // Console.WriteLine(tokenAge);
            Driver.Navigate().GoToUrl(loginUrl);
            if (!tokens["access"].Equals(""))
            {
                AddItem(accessTokenKey, tokens["access"]);
                if (hasRefreshToken) AddItem(refreshTokenKey, tokens["refresh"]);
                Driver.Navigate().Refresh();
            }
            else
            {
                WaitForLogin();
                EnsureLogin();
                WriteToken();
            }
        }

        /// <summary>
        /// if token exist in "active_token.txt" file it will use it instead of login, else Login normally
        /// </summary>
        /// <param name="nationalId">National ID to insert</param>
        /// <param name="password">Password to insert</param>
        private void AddTokenElseLogin()
        {
            var tokens = GetActiveTokens();
            DateTime tokenDate = File.GetLastWriteTime(@"./../../../active_token.txt");
            double tokenAge = (DateTime.Now - tokenDate).TotalHours;
            Console.WriteLine(tokenAge);
            if (!tokens["access"].Equals(""))
            {
                Driver.Navigate().GoToUrl(loginUrl);
                AddItem("vtsAccessToken", tokens["access"]);
                if (hasRefreshToken) AddItem("vtsRefreshToken", tokens["refresh"]);
                Driver.Navigate().Refresh();
            }
            else
            {
                EnsureLogin();
                WriteToken();
            }
        }

        // /// <summary>
        // /// Adds access token to the current window by saving it to local storage.
        // /// </summary>
        // public void AddToken(string key, string token)
        // {
        //     if()
        // }

        public void StoringMethod(StoreLocation storeLocation){
            switch(storeLocation){
                case StoreLocation.LOCAL_STORAGE:
                    this.storeLocation = "localStorage";
                    break;
                case StoreLocation.SESSION_STORAGE:
                    this.storeLocation = "storageStorage";
                    break;
            }
        }

        public string GetAccessToken(){
            return GetItem(accessTokenKey);
        }

        public string GetRefreshToken(){
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
        /// insert the provided National ID into it's input element
        /// </summary>
        /// <param name="nationalId">National ID to insert</param>
        public void NationalId(string nationalId)
        {
            var nationalIdInput = Driver.FindElement(By.CssSelector("app-input[controlname='nationalId'] input"));
            nationalIdInput.SendKeys(nationalId);
        }

        /// <summary>
        /// insert the provided password into it's input element
        /// </summary>
        /// <param name="password">Password to insert</param>
        public void Password(string password)
        {
            var passwordInput = Driver.FindElement(By.CssSelector("app-input[controlname='password'] input"));
            passwordInput.SendKeys(password);
        }

        /// <summary>
        /// Clicks "Log in" button
        /// </summary>
        public void LoginButton()
        {
            var loginButton = Driver.FindElement(By.XPath("//app-login-card//button[@type=\"submit\"]"));
            try
            {
                loginButton.Click();
            }
            catch (ElementClickInterceptedException)
            {
                loginButton.Click();

            }
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
