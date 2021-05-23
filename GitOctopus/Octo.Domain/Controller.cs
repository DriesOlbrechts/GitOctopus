using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace Octo.Domain
{
    public class Controller
    {
        // initialize variables

        public event EventHandler LoggedIn;
        public static string InstallDirectory;
        private static readonly object padlock = new object();
        private static Controller instance = null;
        public TokenHandler _TokenHandler = new TokenHandler(); 
        public bool LoggedInChecker = false;
        public string repository;


        public static Controller Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Controller(); //create new instance of Controller
                    }
                    return instance;
                }
            }
        }
        public class LoggedInArgs : EventArgs
        {
            public bool LoggedInB;
        }

        //Even that fires when logged in
        public void OnLoggedIn(bool LoggedInB)
        {
            LoggedInArgs e = new LoggedInArgs();
            e.LoggedInB = LoggedInB;
            EventHandler handler = LoggedIn;
            handler?.Invoke(this, e);
        }

        #region GitCommandHandling
        //GIT COMMAND HANDLING

        /// <summary>
        ///  Create Credentialshandler for Github authorization
        ///</summary>
        ///<param name="token">The users authorization token</param>
        public LibGit2Sharp.Handlers.CredentialsHandler GenerateCredentialsHandler(String token)
        {
            LibGit2Sharp.Handlers.CredentialsHandler generated = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = token, Password = "" };
            return generated;
        }

        /// <summary>
        /// Commit pending changes to the repository
        /// </summary>
        /// <param name="repo">Which repo to commit to</param>
        /// <param name="commit">The commit message</param>
        /// <returns>True if success, False if failure</returns>
        public bool GitCommit(string repo, string commit)
        {
           

            try
            {
                var repository = new Repository(repo);
                
                string token = Instance._TokenHandler.GetEncryptedToken();
                dynamic user = UserInfo(token);

                Commands.Stage(repository, "*"); // add all files that had not been staged to the tracked files
                var signature = new Signature(user.login.Value, "email", DateTimeOffset.Now);
                repository.Commit(commit, signature, signature);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong while commiting: " + e);
                return false;
            }
        }
        /// <summary>
        /// Pushes pending commits to the repository
        /// </summary>
        /// <param name="repo"> Which repo to push to</param>
        /// <returns>True if success, False if failure</returns>
        public bool GitPush(string repo)
        {
            try
            {

                var repository = new Repository(repo);
                string token = Instance._TokenHandler.GetEncryptedToken(); // get the saved token
                var options = new PushOptions //initiate an options object
                {
                    CredentialsProvider = Instance.GenerateCredentialsHandler(token)
                };

                var remote = repository.Head; // Get the head branch
                repository.Network.Push(remote, options);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong while pushing: " + e);
                return false;
            }
        }
        /// <summary>
        /// Pulls changes from remote repository
        /// </summary>
        /// <param name="repo"> The link to the repository </param>
        /// <returns>True if success, False if failure</returns>
        public bool GitPull(string repo)
        {
            try
            {
                var repository = new Repository(repo);


                string token = Instance._TokenHandler.GetEncryptedToken(); // get the saved token
                var options = new PullOptions //initiate options
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = Instance.GenerateCredentialsHandler(token)
                    }
                };
                dynamic user = UserInfo(token);

                var signature = new Signature(user.login.Value, "email", DateTimeOffset.Now); // email is a required field, but not all users have email as a public value so we pass "email"
                Commands.Pull(repository, signature, options);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong while pulling: " + e);
                return false;
            }
        }
        /// <summary>
        /// Clones a repository
        /// </summary>
        /// <param name="repo"> Which repository to clone </param>
        /// <param name="path"> Where the repository should be cloned to </param>
        /// <returns>True if success, False if failure</returns>
        public bool GitClone(string repo, string path)
        {
            try
            {


                string token = Instance._TokenHandler.GetEncryptedToken(); // get the saved token

                var options = new CloneOptions // initiate options
                {
                    CredentialsProvider = Instance.GenerateCredentialsHandler(token)
                };
                Repository.Clone(repo, path, options);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong while cloning: " + e);
                return false;
            }
        }
        /// <summary>
        /// Gets userinfo from the github api
        /// </summary>
        /// <param name="token"> The user authentication token </param>
        /// <returns>An object with user info</returns>
        public dynamic UserInfo(string token)
        {
            try
            {


                var url = "https://api.github.com/user"; // api endpoint
                string result;
                var httpRequest = (HttpWebRequest)WebRequest.Create(url);

                //setup headers for the request
                httpRequest.Headers["Authorization"] = string.Format("token {0}", token);
                httpRequest.Accept = "application/vnd.github.v3+json";
                httpRequest.UserAgent = "foo"; //useragent needs to have any value or github api does not accept the request



                var httpResponse = (HttpWebResponse)httpRequest.GetResponse(); // get the request response
                
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                dynamic ob = JsonConvert.DeserializeObject(result); // convert the received object to a json object
                return ob;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new { login = "Error" };
            }
        }        


        #endregion
    }
}
