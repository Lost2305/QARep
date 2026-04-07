using examprep.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;


namespace examprep

{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;


        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjOWMyMDY4ZS0wZmQ3LTRmYTctYWY5My04ZGM2ZWYwY2EzYjUiLCJpYXQiOiIwNC8wNy8yMDI2IDIxOjEwOjIxIiwiVXNlcklkIjoiOTA0YjgzODctYjliMi00N2Q0LTUzMzQtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJqYW5lLmRvZUBleGFtcGxlLmNvbSIsIlVzZXJOYW1lIjoiSmFuZURvZTIzIiwiZXhwIjoxNzc1NjE3ODIxLCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.vouL0kRa8URGAT8N8ANNhGdzsdx_Gu0ZJVlLsjci6No";
        private const string LoginEmail = "jane.doe@example.com";
        private const string LoginPassword = "janedoe23";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");

                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]

        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
			Assert.That(responseItems, Is.Not.Null);

            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;

		}

        [Order(3)]
        [Test]

        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editRequestData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an edited idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);

            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
			Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));


		}

        [Order(4)]
        [Test]

        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
			Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));

		}

		[Order(5)]
		[Test]

        public void CreateIdea_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "This is a test idea description.",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);
            
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

		[Order(6)]
		[Test]

        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "1516437";
            var editRequestData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an edited idea description",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

		[Order(7)]
		[Test]

		public void DeleteNonExistingIdea_ShouldReturnNotFound()
		{
            string nonExistingIdeaId = "523525";

			var request = new RestRequest("/api/Idea/Delete", Method.Delete);
			request.AddQueryParameter("ideaId", nonExistingIdeaId);
			var response = this.client.Execute(request);

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
			Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));

		}


		[OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }

}
