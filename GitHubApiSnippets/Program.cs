// Copyright © 2019 Transeric Solutions. All rights reserved.
// Author: Eric David Lynch
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Octokit;
using System.Net;
using System.Linq;
using System.IO;

namespace GitHubApiSnippets
{
	public class Program
	{
		private static readonly string GitHubIdentity = Assembly
			.GetEntryAssembly()
			.GetCustomAttribute<AssemblyProductAttribute>()
			.Product;

		public static void Main(string[] args)
		{
			var productInformation = new ProductHeaderValue(GitHubIdentity);

			if (!TryGetClient(args, productInformation, out GitHubClient client))
				return;

			TestFeature(client)
				.GetAwaiter()
				.GetResult();
		}

		private static async Task<string> CreateOAuthToken(string clientId, string clientSecret, string authenticationCode)
		{
			var client = new GitHubClient(new ProductHeaderValue(GitHubIdentity));

			OauthToken tokenInfo = await client.Oauth.CreateAccessToken(
				new OauthTokenRequest(clientId, clientSecret, authenticationCode));

			string token = tokenInfo.AccessToken;
			return token;
		}

		private static GitHubClient GetClient(ProductHeaderValue productInformation)
		{
			var client = new GitHubClient(productInformation);
			return client;
		}

		private static GitHubClient GetClient(ProductHeaderValue productInformation,
			string token)
		{
			var credentials = new Credentials(token);

			var client = new GitHubClient(productInformation) { Credentials = credentials };

			return client;
		}

		private static GitHubClient GetClient(ProductHeaderValue productInformation,
			string username, string password)
		{
			var credentials = new Credentials(username, password, AuthenticationType.Basic);

			var client = new GitHubClient(productInformation) { Credentials = credentials };

			return client;
		}

        private static GitHubClient GetEnterpriseClient(ProductHeaderValue productInformation,
            Credentials credentials, string enterpriseUrl)
        {
            var client = new GitHubClient(productInformation, new Uri(enterpriseUrl))
            {
                Credentials = credentials
            };

            return client;
        }

       

        private static async Task IssueExamples(GitHubClient client)
		{
			Issue issue = await client.Issue.Get("nostra13", "Android-Universal-Image-Loader", 1);
			Console.WriteLine($"Issue.Get: Id={issue.Id}, Title={issue.Title}");

			SearchIssuesResult result = await client.Search.SearchIssues(
				new SearchIssuesRequest("bug")
				{
					In = new IssueInQualifier[] { IssueInQualifier.Title },
					Repos = new RepositoryCollection { "nostra13/Android-Universal-Image-Loader" }
				});
			Console.WriteLine($"Search.SearchIssues (Simple Search): TotalCount={result.TotalCount}");

			await IssueAllFieldsExample(client);
		}
		public async static Task<List<GitHubCommit>> getAllCommits(GitHubClient github,string username,string repo)
		{
		
			var repository = await github.Repository.Get(username, repo);
			var commits = await github.Repository.Commit.GetAll(repository.Id);

			List<GitHubCommit> commitList = new List<GitHubCommit>();

			foreach (GitHubCommit commit in commits)
			{
				commitList.Add(commit);
			}

			return commitList;
		}
		private static async Task IssueAllFieldsExample(GitHubClient client)
		{
			var fromDate = new DateTime(2000, 3, 17);
			var toDate = new DateTime(2021, 7, 17);

			int fromNumber = 1;
			int toNumber = 10;

			string branch = "";
			string excludedBranch = "";
			string excludedLabel = "";
			string excludedMilestone = "";
			string excludedUser = "";
			string label = "Bug";
			string milestone = "";
			string repository = "nostra13/Android-Universal-Image-Loader";
			string term = "bug";
			string user = "nostra13";
			string repo = "Android-Universal-Image-Loader";
			var request = new SearchIssuesRequest(term)
			{
				
                //Archived = true,
                //Assignee = user,
                //Author = user,
                //Base = branch,
                //Closed = new DateRange(fromDate, toDate),
                //Commenter = user,
                //Comments = new Range(fromNumber, toNumber),
                //Created = new DateRange(fromDate, SearchQualifierOperator.GreaterThan),
                //Exclusions = new SearchIssuesRequestExclusions
                //{
                //	Assignee = excludedUser,
                //	Author = excludedUser,
                //	Base = excludedBranch,
                //	Commenter = excludedUser,
                //	Head = branch,
                //	Involves = excludedUser,
                //	Labels = new string[] { excludedLabel },
                //	Language = Language.Ada,
                //	Mentions = excludedUser,
                //	Milestone = excludedMilestone,
                //	State = ItemState.Open,
                //	Status = CommitState.Error
                //},
                //Head = branch,
                //In = new IssueInQualifier[] { IssueInQualifier.Title },
                //Involves = user,
                //Is = new IssueIsQualifier[] { IssueIsQualifier.Public },
                Labels = new string[] { label },
                //Language = Language.CSharp,
                //Mentions = user,
                //Merged = new DateRange(toDate, SearchQualifierOperator.LessThan),
                //Milestone = milestone,
                //No = IssueNoMetadataQualifier.Assignee,
                //Order = SortDirection.Descending,
                //Repos = new RepositoryCollection() { repository },
                //SortField = IssueSearchSort.Created,
                State = ItemState.Closed,
				//Status = CommitState.Success,
				//Type = IssueTypeQualifier.Issue,
				//Updated = new DateRange(toDate, SearchQualifierOperator.LessThanOrEqualTo),
				User = user
			};
			
			SearchIssuesResult result = await client.Search.SearchIssues(request);

			var commits = await getAllCommits(client, user, repo);

			//lista de eventos cerrados del repo
			foreach (var item in result.Items) {
				Console.WriteLine($"Events Url ={item.EventsUrl}");
				var events = await client.Issue.Events.GetAllForIssue(user, repo,item.Number);
				Console.WriteLine($"Event Count ={events.Count()}");
				Console.WriteLine($"--------------------------------------------------");
				//Lista de eventos por issue
				foreach (var ev in events) {
					
					var closedEvent = events.Where(x => x.Event.Equals("closed")).FirstOrDefault();
					if (closedEvent != null) {
						var withoutBugCommit = events.Where(x => x.CommitId != null && DateTime.Compare(x.CreatedAt.DateTime, closedEvent.CreatedAt.DateTime) <=0 ).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
						if (withoutBugCommit != null) {
							Console.WriteLine("Commit Without Bug");
							Console.WriteLine("Date Create: " + withoutBugCommit.CreatedAt.DateTime);
							Console.WriteLine($"https://github.com/{user}/{repo}/archive/{withoutBugCommit.CommitId}.zip");
							DownloadZip(user, repo, $"https://github.com/{user}/{repo}/archive/{withoutBugCommit.CommitId}.zip", withoutBugCommit.CommitId + "_WithoutBug.zip");
							
							var withBugCommit = commits.Where(x => DateTime.Compare(x.Commit.Committer.Date.DateTime, withoutBugCommit.CreatedAt.DateTime) < 0 && !x.Commit.Tree.Sha.Equals(withoutBugCommit.CommitId)).OrderByDescending(x => x.Commit.Committer.Date.DateTime).FirstOrDefault();
							Console.WriteLine("Commit With Bug");
							Console.WriteLine("Date Create: " + withBugCommit.Commit.Committer.Date.DateTime);
							Console.WriteLine($"https://github.com/{user}/{repo}/archive/{withBugCommit.Commit.Tree.Sha}.zip");
							DownloadZip(user, repo, $"https://github.com/{user}/{repo}/archive/{withBugCommit.Commit.Tree.Sha}.zip", withBugCommit.Commit.Tree.Sha + "_WithBug.zip");

							Console.WriteLine("-------------------------------------------------");
							}
						
					}


				}


			}
			Console.WriteLine($"Search.SearchIssues (All Fields): TotalCount={result.TotalCount}");
		}
        private static bool DownloadZip(string username, string repo, string url, string fileName)
        {
            bool done = true;
            Console.WriteLine("Downloading...");
            string root = @"C:\Repositorio";
            string subdir = @"C:\Repositorio\" + repo;
            // If directory does not exist, create it. 
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
                Directory.CreateDirectory(subdir);
            }
            else
            {
                if (!Directory.Exists(subdir))
                {
                    Directory.CreateDirectory(subdir);
                }

            }
            if (Directory.Exists(subdir))
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, subdir + @"\" + fileName);
                }
            }
            Console.WriteLine("Downloaded");
            return done;
        }


        private static GitHubClient AuthenticateToken(string[] args, ProductHeaderValue productionInformation)
		{
			string token;
			if (args.Length > 1)
				token = args[1];
			else
			{
				Console.Write("OAuth Token? ");
				token = Console.ReadLine();
			}

			return GetClient(productionInformation, token);
		}

		private static GitHubClient AuthenticateBasic(string[] args, ProductHeaderValue productInformation)
		{
			string username;
			if (args.Length > 1)
				username = args[1];
			else
			{
				Console.Write("Username? ");
				username = Console.ReadLine();
			}

			string password;
			if (args.Length > 2)
				password = args[2];
			else
			{
				Console.Write("Password? ");
				password = ReadPassword();
				if (password == null)
					return null;
			}

			return GetClient(productInformation, username, password);
		}

		private static bool IsAltOrControl(ConsoleKeyInfo keyInfo) =>
			(keyInfo.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0;

		private static string ReadPassword()
		{
			var builder = new StringBuilder();

			while (true)
			{
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);
				if (IsAltOrControl(keyInfo))
					continue;

				switch (keyInfo.Key)
				{
					case ConsoleKey.Backspace:
						if (builder.Length > 0)
							builder.Length--;
						break;

					case ConsoleKey.Enter:
						Console.WriteLine();
						return builder.ToString();

					case ConsoleKey.Escape:
						Console.WriteLine();
						return null;

					default:
						char chr = keyInfo.KeyChar;
						if (chr > 0)
							builder.Append(chr);
						else
							break;
						break;
				}
			}
		}

		private static bool TryGetClient(string[] args, ProductHeaderValue productionInformation, out GitHubClient client)
		{
			if (args.Length > 0 && args[0].Length > 0)
				return TryGetClient(args, args[0][0], productionInformation, out client);

			while (true)
			{
				if (!TryReadAuthenticationKey(out char key))
					continue;

				if (key == (char)ConsoleKey.Escape)
				{
					client = null;
					return false;
				}

				if (TryGetClient(args, key, productionInformation, out client))
					return true;
			}
		}

		private static bool TryGetClient(string[] args, char chr, ProductHeaderValue productionInformation, out GitHubClient client)
		{
			switch (chr)
			{
				case 'b':
				case 'B':
					client = AuthenticateBasic(args, productionInformation);
					return client != null;

				case 't':
				case 'T':
					client = AuthenticateToken(args, productionInformation);
					return client != null;

				case 'u':
				case 'U':
					client = new GitHubClient(productionInformation);
					return client != null;

				default:
					Console.WriteLine($"Invalid authentication type.");
					client = null;
					return false;
			}
		}

		private static bool TryReadAuthenticationKey(out char result)
		{
			Console.Write("Authentication (B=Basic, T=Token, U=Unauthenticated, Esc=Exit)? ");
			ConsoleKeyInfo keyInfo = Console.ReadKey();
			Console.WriteLine();

			if (IsAltOrControl(keyInfo))
			{
				result = (char)0;
				return false;
			}

			result = keyInfo.KeyChar;
			return true;
		}

		private static async Task TestFeature(GitHubClient client)
		{
			Console.WriteLine("Available features:");
			Console.WriteLine("  I=Issue");
			Console.WriteLine("  Esc=Exit");

			while (true)
			{
				Console.Write("Feature? ");
				ConsoleKeyInfo keyInfo = Console.ReadKey();
				Console.WriteLine();

				if (IsAltOrControl(keyInfo))
					continue;

				switch (keyInfo.Key)
				{
					

					case ConsoleKey.I:
						Console.WriteLine("Testing Issue...");
						await IssueExamples(client);
						break;

			

					case ConsoleKey.Escape:
						return;

					default:
						Console.WriteLine("Invalid selection.");
						break;
				}

			}
		}
	}
}
