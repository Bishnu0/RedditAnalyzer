using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using RedditAnalyzer.Controller;
using Reddit.Controllers;
using Reddit.AuthTokenRetriever;
using Reddit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

//I need to modify my test class to make it work. 
namespace RedditAnalyzerTests.Controller
{
    [TestFixture]
    internal class RedditAnalyzerControllerTests
    {
        [Test]
        public void AuthenticateUser_ValidCredentials_ReturnsNonNullClient()
        {
            // Arrange
            string clientId = "testClientId";
            string clientSecret = "testClientSecret";
            var controller = new RedditAnalyzerController(clientId, clientSecret);

            // Act
            var redditClient = controller.AuthenticateUser(clientId, clientSecret);

            // Assert
            Assert.IsNotNull(redditClient);
            
        }

        [Test]
        public async Task MonitorPosts_NoNewPostsFound_PrintsMessage()
        {
            // Arrange
            string clientId = "testClientId";
            string clientSecret = "testClientSecret";

            // Mock the Subreddit and setup its Posts property
            var mockSubreddit = new Mock<Subreddit>("funny", null);

            //mockSubreddit.Setup(s => s.Posts.GetNew()).Returns(new List<Post>());

            // Mock the RedditClient and setup its Subreddit method to return the mock Subreddit
            var mockRedditClient = new Mock<RedditClient>(clientId, "refreshToken", clientSecret, "accessToken");

           // mockRedditClient.Setup(c => c.Subreddit("funny")).Returns(mockSubreddit.Object);

            // Create the controller and set the mock RedditClient
            var controller = new RedditAnalyzerController(clientId, clientSecret);
            controller.SetRedditClient(mockRedditClient.Object);

            // Redirect Console output to capture the print statements
            var consoleOutput = new System.IO.StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            await Task.Run(() => controller.MonitorPosts());

            // Assert
            Assert.IsTrue(consoleOutput.ToString().Contains("No new posts found."));
        }
    }
}
