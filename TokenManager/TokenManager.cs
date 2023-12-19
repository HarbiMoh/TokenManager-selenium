using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V117.IndexedDB;
using OpenQA.Selenium.Support.UI;

public class TokenManager
{

        private readonly IJavaScriptExecutor js;
        private readonly IWebDriver Driver;

        private readonly string loginUrl;

        public TimeSpan WAIT_TIME = TimeSpan.FromSeconds(60);

        private string storeLocation = "localStorage";
        
        private readonly List<string> keys;
    

        public TokenManager (IWebDriver driver, string loginUrl)
        {
            Driver = driver;
            js = (IJavaScriptExecutor)Driver;
            this.loginUrl = loginUrl;
            keys = new();
        }

        /// <summary>
        /// used to specify the items keys to store thier value
        /// </summary>
        /// <param name="keys">list of items keys </param>
        public void SetKeys(string[] keys){
            this.keys.Clear();
            foreach(string key in keys)
                this.keys.Add(key);
        }
        /// <summary>
        /// store the items specified by thier keys by SetKeys()
        /// </summary>
        public void StoreTokens(){
            WriteToken();
        }

        /// <summary>
        /// use the previously saved keys by StoreTokens()
        /// </summary>
        public void UseTokens(){

            var tokens = GetActiveTokens();
            foreach(var token in tokens)
                AddItem(token.Key, token.Value);
        }

        /// <summary>
        /// sets what storage to take and store items from
        /// </summary>
        /// <param name="storeLocation">storage to use for operations</param>
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
            StreamWriter sw = File.AppendText(@"./../../../tokens.txt");
            foreach(var key in keys){
                sw.WriteLine(GetItem(key));
            }
            sw.WriteLine("------------------------");
            sw.Close();
            if (keys.Count > 0)
            {
                sw = File.CreateText(@"./../../../active_tokens.txt");
                foreach(var key in keys){
                    sw.WriteLine(GetItem(key));
                }
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
            try{
                var file = File.ReadLines(@"./../../../active_token.txt");
                if (file.Any())
                    for(int i=0; i< keys.Count; i++)
                        tokens.Add(keys[i], file.ElementAt(i));
            }catch(FileNotFoundException){}
            return tokens;

        }

        public enum StoreLocation {
            LOCAL_STORAGE,
            SESSION_STORAGE
        }



}
