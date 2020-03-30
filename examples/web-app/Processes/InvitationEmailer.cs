﻿using Dapper;
using Runly;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Examples.WebApp.Processes
{
	public class InvitationEmailer : Process<InvitationEmailerConfig, string, DbConnection, IEmailService>
	{
		readonly DbConnection db;

		public InvitationEmailer(InvitationEmailerConfig config, DbConnection db)
			: base(config)
		{
			this.db = db;
		}

		public override async Task<IEnumerable<string>> GetItemsAsync()
		{
			// run our database query to get the emails we want to send
			await db.OpenAsync();
			var emails = await db.QueryAsync<string>("select [Email] from [User] where HasBeenEmailed = 0");
			return emails;
		}

		public override async Task<Result> ProcessAsync(string email, DbConnection db, IEmailService emails)
		{
			// use method-scoped db rather than the class-scoped db for parallel tasks
			// https://www.runly.io/docs/dependency-injection/#method-injection

			// send our fake email
			await emails.SendEmail(email, "You are invited!", "Join us. We have cake.");

			// Open the connection if it is not already opened. Since we register the DbConnection as Scoped,
			// a previous call to ProcessAsync with this Task could have opened the connection. In that case,
			// the connection would already be open. Though multiple parralel tasks could be calling ProcessAsync
			// at the same time, the DbConnection is used only with a single Task.
			if (db.State != ConnectionState.Open)
				await db.OpenAsync();

			// mark the user as invited in the database
			await db.ExecuteAsync("update [User] set HasBeenEmailed = 1 where [Email] = @email", new { email });

			return Result.Success();
		}
	}

	public class InvitationEmailerConfig : Config
	{
		public string ConnectionString { get; set; }
		public string EmailServiceApiKey { get; set; }
	}
}
