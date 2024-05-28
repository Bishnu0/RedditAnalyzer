using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Reddit;
using Reddit.Controllers;
using Reddit.AuthTokenRetriever;
using Reddit.Models;
using Reddit.Things;

namespace RedditAnalyzer.Controller
{
    /// <summary>
    /// The <c>RedditAnalyzerController</c> class provides functionality to authenticate a user with Reddit and monitor posts in a specified subreddit.
    /// </summary>
    public class RedditAnalyzerController
    {
        private  RedditClient _redditClient;
        private readonly string _subredditName = null;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent requests

        public RedditAnalyzerController(string clientId, string clientSecret)
        {
            try
            {
                _redditClient = AuthenticateUser(clientId, clientSecret);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
                throw;
            }
        }

        public RedditClient AuthenticateUser(string clientId, string clientSecret)
        {
            var tokenRetriever = new AuthTokenRetrieverLib(clientId, Constants.Constants.callbackPort, Constants.Constants.LOCALHOST, appSecret: clientSecret);

            tokenRetriever.AwaitCallback();
            OpenBrowser(tokenRetriever.AuthURL());

            // Wait until the refresh token is obtained
            while (string.IsNullOrWhiteSpace(tokenRetriever.RefreshToken))
            {
                Thread.Sleep(1000);
            }

            tokenRetriever.StopListening();
            return new RedditClient(clientId, tokenRetriever.RefreshToken, clientSecret, tokenRetriever.AccessToken);
        }

        private static void OpenBrowser(string authUrl)
        {
            string browserPath = Constants.Constants.browserPath;

            try
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
        /// <summary>
        /// Monitors new posts in the specified subreddit and prints information(the post with the most upvotes) about the top post and users with the most posts.
        /// </summary>
        public async void MonitorPosts()
        {
            try
            {
                Reddit.Controllers.Post topPost = null;
                var userPostCounts = new Dictionary<string, int>();
                int maxUpVotes = 0;

                while (true)
                {
                    await _semaphore.WaitAsync(); // Wait for semaphore slot
                    var recentPosts = _redditClient.Subreddit(_subredditName).Posts.GetNew();

                    if (recentPosts.Count == 0)
                    {
                        Console.WriteLine("No new posts found.");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                        _semaphore.Release(); // Release semaphore slot
                        continue;
                    }

                    // Define a dictionary to store the set of posts each user has made
                    Dictionary<string, HashSet<string>> userPosts = new Dictionary<string, HashSet<string>>();

                    foreach (var post in recentPosts)
                    {
                        UpdateMaxUpVotes(post, ref maxUpVotes, ref topPost);
                        UpdateUserPostCounts(post, userPostCounts, userPosts);
                    }

                    // Calculate the count of unique posts for each user
                    foreach (var kv in userPosts)
                    {
                        userPostCounts[kv.Key] = kv.Value.Count;
                    }

                    DisplayTopPost(topPost);
                    DisplayUsersMostPostCount(userPostCounts);

                    _semaphore.Release(); // Release semaphore slot
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void UpdateMaxUpVotes(Reddit.Controllers.Post post, ref int maxUpVotes, ref Reddit.Controllers.Post topPost)
        {
            if (post.UpVotes > maxUpVotes)
            {
                maxUpVotes = post.UpVotes;
                topPost = post;
            }
        }

        private void UpdateUserPostCounts(Reddit.Controllers.Post post, Dictionary<string, int> userPostCounts, Dictionary<string, HashSet<string>> userPosts)
        {
            var author = post.Author;
            var postId = post.Id;

            if (!userPosts.ContainsKey(author))
            {
                userPosts[author] = new HashSet<string>(); // Initialize the set for the user if not present
            }

            // Add the post ID to the user's set of posts
            userPosts[author].Add(postId);

            // Increment post count for the user
            userPostCounts.TryGetValue(author, out int count);
            userPostCounts[author] = count + 1;
        }

        private void DisplayTopPost(Reddit.Controllers.Post topPost)
        {
            if (topPost != null)
            {
                Console.WriteLine($"Top Post: {topPost.Title} (UpVotes: {topPost.UpVotes})");
            }
        }

        private void DisplayUsersMostPostCount(Dictionary<string, int> userPostCounts)
        {
            if (userPostCounts.Count == 0)
            {
                Console.WriteLine("No users found.");
                return;
            }

            var maxPostsUser = userPostCounts.OrderByDescending(kv => kv.Value).First();

            Console.WriteLine($"User with most posts: {maxPostsUser.Key}, Posts: {maxPostsUser.Value}");
        }
        // Method to set the RedditClient for testing purposes
        public void SetRedditClient(RedditClient redditClient)
        {
            _redditClient = redditClient;
        }
    }
}
