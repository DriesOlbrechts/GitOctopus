using LibGit2Sharp;
using Microsoft.WindowsAPICodePack.Dialogs;
using Octo.Domain;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using static Octo.Domain.Controller;





namespace Octo.WPFVisul
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static System.Timers.Timer aTimer;

        public bool LoggedIn;

        public MainWindow()
        {
            InitializeComponent();
            
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);  // handle saving settings before shutdown
            SetTimer();
            if(Properties.Settings.Default.LastRepo != "")
            {
                Instance.repository = Properties.Settings.Default.LastRepo; // set current repository to the previous repository
            }
        }

        /// <summary>
        /// Called when the login button is loaded in
        /// </summary>
        private void Login_loaded(object sender, RoutedEventArgs e)
        {
            Controller.InstallDirectory = Directory.GetCurrentDirectory();
            Controller.Instance.LoggedIn += LoggedInHandler;

            if (Controller.Instance._TokenHandler.Token != "" && Controller.Instance._TokenHandler.Token != null) // check wether or not the user is logged in
            {
                Console.WriteLine(LoggedIn);
                Controller.Instance.OnLoggedIn(true);
            }
        }
        /// <summary>
        /// Called when the repository button is clicked
        /// </summary>
        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); // initialize a file picker
            dialog.InitialDirectory = Instance.repository != null ? Instance.repository : "C:\\Users"; // set the folder the file picker starts in
            dialog.Title = "Pick a repository";
            dialog.IsFolderPicker = true; //set as folder picker
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                bool valid = LibGit2Sharp.Repository.IsValid(dialog.FileName); // check if the folder is a valid repository
                if (!valid)
                {
                    LibGit2Sharp.Repository.Init(dialog.FileName); // initialize repository if not
                }
                Instance.repository = dialog.FileName;
                repo_label.Content = Instance.repository.Split('\\').Last(); // get the folder name without path and set as content for the label         
            }
        }

        /// <summary>
        /// Called when the login button is clicked
        /// </summary>
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "ClientSecret.json"); // combine the path to client secret
                string clientSecrets = System.IO.File.ReadAllText(path); // read the contents of client secret
                ClientSecret clientsecret = JsonSerializer.Deserialize<ClientSecret>(clientSecrets);

                Process.Start(String.Format(@"https://github.com/login/oauth/authorize?client_id={0}&scope=repo", clientsecret.client_id)); // start the default browser with github login link

                Octo.Domain.WebHandler.Run();  //start the webhandler
            }
            else
            {
                //removes the token to log out the user
                Instance._TokenHandler.deleteToken();
            }
        }

        /// <summary>
        /// Called when the Exit button is clicked
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);    //exits        
        }

        /// <summary>
        /// Function to call for login/logout events
        /// </summary>
        public async void LoggedInHandler(object sender, EventArgs e)
        {
            LoggedIn = ((LoggedInArgs)e).LoggedInB; // gets login state

            if (LoggedIn)
            {
                string token = Instance._TokenHandler.GetEncryptedToken(); // get the token
                dynamic user = Instance.UserInfo(token); //get info from github api
                await this.Dispatcher.InvokeAsync(() =>
                {
                    Login.Content = user.login; // set username as content of login button
                });
            }
            else
            {
                await this.Dispatcher.InvokeAsync(() =>
                {
                    Login.Content = "Login"; // sets content of the login button back to login
                });
            }
        }
        /// <summary>
        /// Called when the clone button is clicked
        /// </summary>
        private void Clone_click(object sender, RoutedEventArgs e)
        {
            var repo = Microsoft.VisualBasic.Interaction.InputBox("What repository would you like to clone?", "Clone"); // creates an input popup
            if(repo != null && repo != "") // check if there is any input
            {
                if (repo.Contains(".git")) // is the input a valid git repo? (if it contains .git but isnt a repo, the clone function will catch this)
                {
                    if (Controller.Instance._TokenHandler.Token != "" && Controller.Instance._TokenHandler.Token != null) // check if user is logged in
                    {
                        CommonOpenFileDialog dialog = new CommonOpenFileDialog(); // start file picker
                        dialog.InitialDirectory = "C:\\Users"; // start in users directory
                        dialog.Title = "Pick a location to clone";
                        dialog.IsFolderPicker = true; // set as folder picker
                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            Instance.GitClone(repo, dialog.FileName); // clone the repo
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please log in before trying to clone");
                    }
                }
                else
                {
                    MessageBox.Show("That is not a valid url");
                }
            }
        }
        /// <summary>
        /// Called when pull button is clicked
        /// </summary>
        private void Pull_click(object sender, RoutedEventArgs e)
        {
            if (Controller.Instance._TokenHandler.Token != "" && Controller.Instance._TokenHandler.Token != null) // check if user is logged in
            {
                if (Instance.repository != null) // check if a repository has been selected
                {
                    var repo = new Repository(Instance.repository); // create repository object
                    if (repo.Network.Remotes.Count() > 0) //check if there are remots to pull from
                    {
                        bool succes = Instance.GitPull(Instance.repository); //pull from the remote
                        /* visual succes or failure feedback*/
                        if (succes)
                        {
                            succesLabel.Foreground = Brushes.Green;
                            succesLabel.Content = "Pull succesful";
                        }
                        else
                        {
                            succesLabel.Foreground = Brushes.Red;
                            succesLabel.Content = "Pull failed";
                        }
                    }
                    else
                    {
                        MessageBox.Show("This repository is not linked to any remote repositories");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a repository before trying to pull");
                }
            }
            else
            {
                MessageBox.Show("Please log in before trying to pull");
            }
        }


        /// <summary>
        /// called when push button is clicked
        /// </summary>
      
        private void Push_click(object sender, RoutedEventArgs e)
        {
            if (Controller.Instance._TokenHandler.Token != "" && Controller.Instance._TokenHandler.Token != null) //check if the user is logged in
            {
                if (Instance.repository != null) // check if a repository has been selected
                {
                    var repo = new Repository(Instance.repository); // initialize repository object
                    if (repo.Network.Remotes.Count() > 0) // check if there are remotes to push to
                    {
                        bool succes = Instance.GitPush(Instance.repository); //push
                        /* visual feedback*/
                        commit_border.BorderBrush = Brushes.Transparent;
                        commit_info.Text = "";

                        if (succes)
                        {
                            succesLabel.Foreground = Brushes.Green;
                            succesLabel.Content = "Push succesful";
                        }
                        else
                        {
                            succesLabel.Foreground = Brushes.Red;
                            succesLabel.Content = "Push failed";
                        }
                    }
                    else
                    {
                        MessageBox.Show("This repository is not linked to any remote repositories");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a repository before trying to push");
                }
            }
            else
            {
                MessageBox.Show("Please log in before trying to push");
            }

        }
        /// <summary>
        /// Called when commit button is clicked
        /// </summary>
        
        private void Commit_click(object sender, RoutedEventArgs e)
        {
            if (Controller.Instance._TokenHandler.Token != "" && Controller.Instance._TokenHandler.Token != null) // check if user is logged in
            {
                if (Instance.repository != null) // check if repository has been selected
                {
                    var repo = new Repository(Instance.repository); // initialize repository object
                    if (repo.Network.Remotes.Count() > 0) // Check if there are repos to commit to
                    {
                        var status = repo.RetrieveStatus(); // get changed files
                        var files = String.Join("\n", status.Select(d => d.FilePath).ToArray()); // only the paths of the files
                        var message = Microsoft.VisualBasic.Interaction.InputBox("Enter a commit message", "Commit");
                        bool succes = Instance.GitCommit(Instance.repository, message); //commit
                        /*visual feedback */
                       
                        
                        if (succes)
                        {
                            commit_info.Text = "Latest commit: \n\n" + "Message: " + message + "\n\n" + "Files: \n" + files; // set commited files message
                            commit_border.BorderBrush = Brushes.Red;
                            succesLabel.Foreground = Brushes.Green;
                            succesLabel.Content = "Commit succesful";
                        }
                        else
                        {
                            succesLabel.Foreground = Brushes.Red;
                            succesLabel.Content = "Commit failed";
                        }
                    }
                    else
                    {
                        MessageBox.Show("This repository is not linked to any remote repositories");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a repository before trying to commit");
                }
            }
            else
            {
                MessageBox.Show("Please log in before trying to commit");
            }
        }

        /// <summary>
        /// Checks for changes every 2 seconds
        /// </summary>
        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += checkChanges;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// Checks for changes in the repo and sets the labels accordingly
        /// </summary>
       
        private void checkChanges(Object source, System.Timers.ElapsedEventArgs e)
        {
            if(Instance.repository != null)
            {
                var repo = new Repository(Instance.repository);
                var status = repo.RetrieveStatus();
                
                if(status.Count() > 0)
                {
                    var s = String.Join("\n", status.Select(d => d.FilePath).ToArray());
                    this.Dispatcher.Invoke(() =>
                    {
                        changes.Text = s;
                        changes_title.Content = "uncommited changes";
                    });                    
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        changes.Text = "";
                        changes_title.Content = "";
                    });
                }
            }
        }

        /// <summary>
        /// called when repo label is loaded
        /// </summary>
        
        private void repo_label_loaded(object sender, RoutedEventArgs e)
        {
            if(Instance.repository != null)//if there is already a repository
            {
                repo_label.Content =  Instance.repository.Split('\\').Last(); // set the label to the repo name
            }
        }
        /// <summary>
        ///  handles program exit
        /// </summary>
       
        static void OnProcessExit(object sender, EventArgs e)
        {
            if (Instance.repository != null)
            {
                Properties.Settings.Default.LastRepo = Instance.repository; // save last repo
                Properties.Settings.Default.Save(); // save settings
            }
        }

        
        
    }
}
