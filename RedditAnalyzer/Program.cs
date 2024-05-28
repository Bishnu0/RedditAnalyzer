using RedditAnalyzer.Controller;
using System;
using System.IO;
using RedditAnalyzer.Constants;

namespace RedditAnalyzer
{
    /// <summary>
    /// The Program class serves as the entry point for the RedditAnalyzer application.
    /// It reads configuration settings from a file, initializes the RedditAnalyzerController,
    /// and starts monitoring Reddit posts from a specified subreddit.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The Main method is the entry point of the application.
        /// It reads the configuration file, extracts necessary values, and
        /// initializes and starts the RedditAnalyzerController.
        /// </summary>
        /// <param name="args">Command-line arguments (not used in this application).</param>

        static void Main(string[] args)
        {
         
            //Ideally we want to get it from runtime or from a secure location that is hosted outside of this location
            var configPath = Constants.Constants.configFilePath;
            
            var config = File.ReadAllLines(configPath);
            
            var clientId = GetValueFromConfig(config, Constants.Constants.CLIENT_ID);
            var clientSecret = GetValueFromConfig(config, Constants.Constants.CLIENT_SECRET);
            var subredditName = GetValueFromConfig(config, Constants.Constants.SUBREDDIT_NAME);
         
            var redditController = new RedditAnalyzerController(clientId, clientSecret);
            redditController.MonitorPosts();
        }

        
        
        private static string GetValueFromConfig(string[] configLines, string key)
        {
            foreach (var line in configLines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    return parts[1].Trim();
                }
            }
            throw new Exception($"Key '{key}' not found in configuration file.");
        }
    }
}