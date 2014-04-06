﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Net;
using Apr.TeamFoundation.Tracking.Linq;
using System.Linq.Expressions;
using System.Diagnostics;

namespace Apr.TeamFoundation.Tracking.WiqLinq.Tests
{
	/// <summary>
	/// Summary description for WiqlPartsTest
	/// </summary>
	[TestClass]
	public class WiqlPartsTest
	{
		const string TFS_SERVER = "https://rdavis.visualstudio.com";

		const string MINIMAL_WIQL = "SELECT [System.Id] FROM WORKITEMS";

		private static TeamFoundationServer server;
        private static TfsTeamProjectCollection server2;

		private static WorkItemStore store;

		private TestContext testContextInstance;

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		[ClassInitialize]
		public static void MyClassInitialize(TestContext testContext)
		{
            NetworkCredential networkCredential = new NetworkCredential(@"gurge60@msn.com","zippyd00da");
            BasicAuthToken basicAuthToken = new BasicAuthToken(networkCredential);
            BasicAuthCredential basicAuthCredential = new BasicAuthCredential(basicAuthToken);
            TfsClientCredentials tfsClientCredentials = new TfsClientCredentials(basicAuthCredential);
            tfsClientCredentials.AllowInteractive = false;

            server2 = new TfsTeamProjectCollection(new Uri("https://rdavis.visualstudio.com/DefaultCollection"), tfsClientCredentials);
            
            server2.Connect(Microsoft.TeamFoundation.Framework.Common.ConnectOptions.None);
            server2.EnsureAuthenticated();

            server2.Authenticate();

			if (!server2.HasAuthenticated)
				throw new InvalidOperationException("Not authenticated");

            store = new WorkItemStore(server2);
		}

        

		//[DebuggerStepThrough]
        //public WiqlPartsTest()
        //{
        //    store = new WorkItemStore(server);
        //}

		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		public static void MyClassCleanup()
		{
			server.Dispose();
			server = null;
		}
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		private static string TrimQueryText(IQueryable<WorkItem> actual)
		{
			string actualWiql = string.Join(" ", actual.ToString().Split(new string[] { Environment.NewLine, " " }, StringSplitOptions.RemoveEmptyEntries).ToArray());
			return actualWiql;
		}

		[TestMethod]
		public void WiqlBasicTest()
		{
			var actual = store.WorkItems();

			string actualWiql = TrimQueryText(actual);
			string expectedWiql = MINIMAL_WIQL;

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		[TestMethod]
		public void WiqlAsofTest()
		{
			DateTime asof = DateTime.MinValue; // TODO: Initialize to an appropriate value

			var actual = store.WorkItems(asof);

			string actualWiql = TrimQueryText(actual);
			string expectedWiql = MINIMAL_WIQL + " ASOF 1/1/0001 12:00:00 AM";

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}


		[TestMethod]
		public void LinqExprSimpleTest()
		{
			var query = from item in store.WorkItems()
							select item;
            List<WorkItem> list = query.ToList();
			string actualWiql = TrimQueryText(query);
			string expectedWiql = MINIMAL_WIQL;

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		[TestMethod]
		public void LinqExprWhereFieldsCollectionKeyEqlConstantTest()
		{
            var query = from item in store.WorkItems()
                        where item.Fields[CoreFieldReferenceNames.WorkItemType].Value == "Bug"
                        select item;

            var list = query.ToList();

			string actualWiql = TrimQueryText(query);
			string expectedWiql = MINIMAL_WIQL + " WHERE ([System.WorkItemType] = 'Bug')";

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		[TestMethod]
		public void LinqExprWhereORTest()
		{
			var query = from item in store.WorkItems()
							where
								item.Fields[CoreFieldReferenceNames.WorkItemType].Value == "Bug" ||
								item.Fields[CoreFieldReferenceNames.WorkItemType].Value == "Change Request"
							select item;

			string actualWiql = TrimQueryText(query);
			string expectedWiql = MINIMAL_WIQL + " WHERE (([System.WorkItemType] = 'Bug') OR ([System.WorkItemType] = 'Change Request'))";

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		[TestMethod]
		public void LinqExprWhereIntPropertyAccessTest()
		{
			var query = from item in store.WorkItems()
							where
								item.Id == 12000
							select item;

			string actualWiql = TrimQueryText(query);
			string expectedWiql = MINIMAL_WIQL + " WHERE ([System.Id] = 12000)";

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		[TestMethod]
		public void WiqlEveryPartTest()
		{
			/*
				SELECT Select_List
				FROM workitems
				[WHERE Conditions]			AB
				[ORDER BY Column_List]		CD
				[ASOF DateTimeConditions]	EF
			*/

			DateTime dateTimeConditions = DateTime.MinValue;
			string expectedWherePart = " WHERE ([System.Id] = 12000)";
			string expectedOrderPartAsc = " ORDER BY [System.Title] asc";
			string expectedOrderPartDesc = " ORDER BY [System.Title] desc";
			string expectedDatePart = " ASOF 1/1/0001 12:00:00 AM";

			string expectedACE = MINIMAL_WIQL;
			string expectedBCE = MINIMAL_WIQL + expectedWherePart;
			string expectedADE = MINIMAL_WIQL + expectedOrderPartAsc;
			string expectedBDE = MINIMAL_WIQL + expectedWherePart + expectedOrderPartDesc;
			string expectedACF = MINIMAL_WIQL + expectedDatePart;
			string expectedBCF = MINIMAL_WIQL + expectedWherePart + expectedDatePart;
			string expectedADF = MINIMAL_WIQL + expectedOrderPartAsc + expectedDatePart;
			string expectedBDF = MINIMAL_WIQL + expectedWherePart + expectedOrderPartDesc + expectedDatePart;

			var queryACE = from item in store.WorkItems()
								select item;
			var queryBCE = from item in store.WorkItems()
								where item.Id == 12000
								select item;
			var queryADE = from item in store.WorkItems()
								orderby item.Title ascending
								select item;
			var queryBDE = from item in store.WorkItems()
								where item.Id == 12000
								orderby item.Title descending
								select item;
			var queryACF = from item in store.WorkItems(dateTimeConditions)
								select item;
			var queryBCF = from item in store.WorkItems(dateTimeConditions)
								where item.Id == 12000
								select item;
			var queryADF = from item in store.WorkItems(dateTimeConditions)
								orderby item.Title ascending
								select item;
			var queryBDF = from item in store.WorkItems(dateTimeConditions)
								where item.Id == 12000
								orderby item.Title descending
								select item;

			Assert.AreEqual<string>(expectedACE, TrimQueryText(queryACE));
			Assert.AreEqual<string>(expectedBCE, TrimQueryText(queryBCE));
			Assert.AreEqual<string>(expectedADE, TrimQueryText(queryADE));
			Assert.AreEqual<string>(expectedBDE, TrimQueryText(queryBDE));
			Assert.AreEqual<string>(expectedACF, TrimQueryText(queryACF));
			Assert.AreEqual<string>(expectedBCF, TrimQueryText(queryBCF));
			Assert.AreEqual<string>(expectedADF, TrimQueryText(queryADF));
			Assert.AreEqual<string>(expectedBDF, TrimQueryText(queryBDF));
		}

		[TestMethod]
		public void ProviderMultiplicationTest()
		{
			var query = from item in store.WorkItems()
							//where item.Id == 12000
							select item;

			var queryWithWhere = query.Where(item => item.Field(CoreFieldReferenceNames.Title).Contains("error"));

			Expression<Func<WorkItem, bool>> standaloneWhere = item => item.Title == "test";

			var queryWithStandaloneWhere = query.Where(standaloneWhere);

			//string expectedWherePart1 = " WHERE (([System.Id] = 12000))";
			string expectedWherePart2 = " WHERE ([System.Title] CONTAINS 'error')";
			string expectedWherePart3 = " WHERE ([System.Title] = 'test')";


			Assert.AreEqual<string>(MINIMAL_WIQL + expectedWherePart2, TrimQueryText(queryWithWhere));
			Assert.AreEqual<string>(MINIMAL_WIQL, TrimQueryText(query));
			Assert.AreEqual<string>(MINIMAL_WIQL + expectedWherePart3, TrimQueryText(queryWithStandaloneWhere));
		}


		[TestMethod]
		public void WhereClauseANDTest()
		{
			string expected = MINIMAL_WIQL + " WHERE (([System.AssignedTo] = 'Andrew Revinsky') AND ([System.Title] CONTAINS 'error'))";

			var query = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.AssignedTo) == "Andrew Revinsky" &&
								item.Field(CoreFieldReferenceNames.Title).Contains("error")
							select item;
			string actual = TrimQueryText(query);

			Assert.AreEqual<string>(expected, actual);
			/*
				SELECT [System.Id], [System.Title] 
				FROM WorkItems 
				WHERE [System.TeamProject] = @project 
				AND [System.AssignedTo] = 'Judy Lew'
			 */
		}

		[TestMethod]
		public void WhereClauseEVERTest()
		{
			/*
				SELECT [System.Id], [System.Title] 
				FROM WorkItems 
				WHERE [System.TeamProject] = @project 
				AND  [System.AssignedTo] EVER 'Anne Wallace'
			*/

			string expected = MINIMAL_WIQL + " WHERE ([System.AssignedTo] EVER 'Andrew Revinsky')";

			var query = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.AssignedTo).Ever("Andrew Revinsky")
							select item;
			string actual = TrimQueryText(query);

			Assert.AreEqual<string>(expected, actual);


			string expected2 = MINIMAL_WIQL + " WHERE ([System.AssignedTo] NOT EVER 'Andrew Revinsky')";

			var query2 = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.AssignedTo).NotEver("Andrew Revinsky")
							select item;
			string actual2 = TrimQueryText(query2);

			Assert.AreEqual<string>(expected2, actual2);

			
		}

		[TestMethod]
		public void WhereClauseUNDERTest()
		{
			string expected = MINIMAL_WIQL + " WHERE ([System.AreaPath] UNDER 'CorpTools')";

			var query = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.AreaPath).Under("CorpTools")
							select item;
			string actual = TrimQueryText(query);

			Assert.AreEqual<string>(expected, actual);


			string expected2 = MINIMAL_WIQL + " WHERE ([System.AreaPath] NOT UNDER 'CorpTools')";

			var query2 = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.AreaPath).NotUnder("CorpTools")
							select item;
			string actual2 = TrimQueryText(query2);

			Assert.AreEqual<string>(expected2, actual2);

/*
	SELECT [System.Id], [System.Title] 
	FROM WorkItems 
	WHERE [System.TeamProject] = @project 
	AND [System.AssignedTo] EVER 'David Galvin'
	AND [System.AreaPath] UNDER 'Agile1\Area 0'
 */
//         string expected3 = @"
// SELECT [System.Id], [System.Title]
// FROM WorkItems 
// WHERE [System.TeamProject] = 'CorpTools' 
// AND [System.AssignedTo] EVER 'David Galvin'
// AND [System.AreaPath] UNDER 'Agile1\Area 0'";
//         var query3 = from item in store.WorkItems()
//                      where item.Field(CoreFieldReferenceNames.TeamProject) == "CorpTools" &&
//                        item.Field(CoreFieldReferenceNames.AssignedTo).Ever("David Galvin") &&
//                        item.Field(CoreFieldReferenceNames.AreaPath).Under("")
//                      select item.Field(CoreFieldReferenceNames.Id);

		}

		[TestMethod]
		public void WhereClauseINTest()
		{
			var query = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.Id).In(12000, 13000, 14000)
							//where item.Field( .In(CoreFieldReferenceNames.Id, new int[] { 12000, 13000, 14000 })
							select item;
		}

		[TestMethod]
		public void FieldOperatorTest()
		{
			string expected = MINIMAL_WIQL + " WHERE ([System.Id] IN (12000, 12001, 12002))";

			var query = from item in store.WorkItems()
							where item.Field(CoreFieldReferenceNames.Id).In(12000, 12001, 12002)
							select item;
			string actual = TrimQueryText(query);

			Assert.AreEqual<string>(expected, actual);

			var query2 = from item in store.WorkItems()
							 where item.Field<int>(CoreFieldReferenceNames.Id).In(12000, 12001, 12002)
							 select item;
			string actual2 = TrimQueryText(query);

			Assert.AreEqual<string>(expected, actual2);
		}

		[TestMethod]
		public void SeveralSelectsTest()
		{
			string expected = MINIMAL_WIQL;
			string expected2 = MINIMAL_WIQL.Replace("[System.Id]", "[System.Id], [System.Title]");

			var query = from item in store.WorkItems()
							select item;

			var query2 = query.Select(item => item.Columns());
			string actual2 = TrimQueryText(query2);
			Assert.AreEqual<string>(expected, actual2);


			var query3 = query.Select(item => item.Columns("System.Id", CoreFieldReferenceNames.Title));
			string actual3 = TrimQueryText(query3);


			Assert.AreEqual<string>(expected, actual2);
			Assert.AreEqual<string>(expected2, actual3);

			var query4 = query3.Where(item => item.Field<int>(CoreFieldReferenceNames.Id) == 12345);

			WorkItemCollection result = query4.Provider.Execute<WorkItemCollection>(query4.Expression);
			int displayFields3 = result.DisplayFields.Count;

			Assert.AreEqual<int>(2, displayFields3);
		}

		[TestMethod]
		public void QueryExecutionTest()
		{
			var query = from item in store.WorkItems()
							where item.Field<int>(CoreFieldReferenceNames.Id) > 33000 &&
								item.Field<string>(CoreFieldReferenceNames.Title).Contains("test")
							select item.Columns(
								CoreFieldReferenceNames.Id,
								CoreFieldReferenceNames.AreaPath,
								CoreFieldReferenceNames.CreatedBy,
								CoreFieldReferenceNames.AssignedTo);
			var result = ((IQueryableWorkitemStore)query).Execute();

			Assert.AreEqual<int>(4, result.DisplayFields.Count);
			Assert.IsTrue(result.Count > 0);
		}

		[TestMethod]
		public void ArbitraryColumnsTest()
		{
			List<string> columns = new List<string>()
			{
				CoreFieldReferenceNames.Id,
				CoreFieldReferenceNames.AreaPath,
				CoreFieldReferenceNames.CreatedBy,
				CoreFieldReferenceNames.AssignedTo,
				CoreFieldReferenceNames.TeamProject
			};

			Dictionary<string, Expression<Func<WorkItem, bool>>> conditions = new Dictionary<string, Expression<Func<WorkItem, bool>>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "id_gt", item => item.Field<int>(CoreFieldReferenceNames.Id) > 33300 },
				{ "title_contains_value", item => item.Field<string>(CoreFieldReferenceNames.Title).Contains("q") },
				{ "assignedever", item => item.Field<string>(CoreFieldReferenceNames.AssignedTo).Ever("Andrew Revinsky") }
			};

			var query = from item in store.WorkItems()
							select item;

			var cols = (from int i in (new int[] { 0, 2, 4 })
							select columns[i]).ToArray();

			query = query.Select(item => item.Columns(cols));

			query = query.Where(conditions["title_contains_value"]);
			query = query.Where(conditions["id_gt"]);

			var cols2 = columns.Where((s, i) => (i == 1) || (i == 3)).Reverse().ToArray();
			query = query.Select(item => item.Columns(cols2));

			var result = ((IQueryableWorkitemStore)query).Execute();

			Assert.IsTrue(result.Count > 0);
			Assert.AreEqual<int>(columns.Count, result.DisplayFields.Count);
		}

		[TestMethod]
		public void FieldRecognitionTest()
		{
			string[] clauses = new string[] {
				string.Format("[{0}] > 33000", CoreFieldReferenceNames.Id),
				string.Format("[{0}] CONTAINS 'error'", CoreFieldReferenceNames.Title),
				string.Format("[{0}] UNDER 'Rtx'", CoreFieldReferenceNames.AreaPath),
				string.Format("([{0}] <> 'Andrew Revinsky')", CoreFieldReferenceNames.CreatedBy),
				string.Format("[{0}] CONTAINS '<'", CoreFieldReferenceNames.Description),
			};

			Queue<string> queue = new Queue<string>(clauses);

			string current = queue.Dequeue();
			while (queue.Count > 0)
			{
				current = string.Format("({0}) AND {1}", current, queue.Dequeue());
			}

			string expectedWiql = MINIMAL_WIQL + string.Format(" WHERE ({0})", current);
			
			var query = from item in store.WorkItems()
							where
								item.Id > 33000 &&
								item.Title.Contains("error") &&
								item.AreaPath.StartsWith("Rtx") &&
								item.CreatedBy != "Andrew Revinsky" &&
								item.Description.Contains("<")
							select item;

			string actualWiql = TrimQueryText(query);

			Assert.AreEqual<string>(expectedWiql, actualWiql);
		}

		/*
		[TestMethod]
		public void UnitTest()
		{
		}
		*/

	}
}
