using System;
using System.Configuration;
using System.IO;
using System.Security.Principal;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Roadkill.Core;
using Roadkill.Core.Database;

namespace Roadkill.Tests.Acceptance
{
	[TestFixture]
	[Category("Acceptance")]
	public class InstallerTests : AcceptanceTestBase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{	
			TestHelpers.CopyDevConnectionStringsConfig();
		}

		[SetUp]
		public void Setup()
		{
			Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10)); // for ajax calls
			TestHelpers.SetRoadkillConfigToUnInstalled();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			try
			{
				// Remove any attachment folders used by the installer tests
				string installerTestsAttachmentsPath = Path.Combine(TestConstants.WEB_PATH, "AcceptanceTests");
				Directory.Delete(installerTestsAttachmentsPath, true);
			}
			catch { }

			// Reset the db and web.config back for all other acceptance tests
			TestHelpers.SqlServerSetup.RecreateTables();
			TestHelpers.CopyDevConnectionStringsConfig();
		}


		protected void ClickLanguageLink()
		{
			int english = 0;
			Driver.FindElements(By.CssSelector("ul#language a"))[english].Click();
		}

		[Test]
		public void Installation_Page_Should_Display_For_Home_Page_When_Installed_Is_False()
		{
			// Arrange


			// Act
			Driver.Navigate().GoToUrl(BaseUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("div#installer-container")).Count, Is.EqualTo(1));
		}

		[Test]
		public void Installation_Page_Should_Display_For_Login_Page_When_Installed_Is_False()
		{
			// Arrange
			

			// Act
			Driver.Navigate().GoToUrl(LoginUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("div#installer-container")).Count, Is.EqualTo(1));
		}

		[Test]
		public void Language_Selection_Should_Display_For_First_Page()
		{
			// Arrange

			// Act
			Driver.Navigate().GoToUrl(BaseUrl);

			// Assert
			Assert.That(Driver.FindElements(By.CssSelector("ul#language li")).Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(Driver.FindElements(By.CssSelector("ul#language li"))[0].Text, Is.EqualTo("English"));
		}

		[Test]
		public void Step1_Web_Config_Test_Button_Should_Display_Success_Toast()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);

			// Act
			ClickLanguageLink();
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
		}

		[Test]
		public void Step1_Web_Config_Test_Button_Should_Display_Error_Modal_And_No_Continue_Link_For_Readonly_Webconfig()
		{
			// Arrange
			string sitePath = TestConstants.WEB_PATH;
			string webConfigPath = Path.Combine(sitePath, "web.config");
			File.SetAttributes(webConfigPath, FileAttributes.ReadOnly);

			// Cascades down
			string roadkillConfigPath = Path.Combine(sitePath, "roadkill.config");
			File.SetAttributes(roadkillConfigPath, FileAttributes.ReadOnly);

			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);
		}

		[Test]
		public void Step2_Connection_Test_Button_Should_Display_Success_Toast_For_Good_ConnectionString()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
			select.SelectByValue("SqlServer2008");

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(TestConstants.CONNECTION_STRING);
			Driver.FindElement(By.CssSelector("button[id=testdbconnection]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
		}

		[Test]
		public void Step2_Connection_Test_Button_Should_Display_Error_Modal_For_Bad_ConnectionString()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
			select.SelectByValue("SqlServer2008");

			Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Server=(local);Integrated Security=true;Connect Timeout=5;database=some-database-that-doesnt-exist");
			Driver.FindElement(By.CssSelector("button[id=testdbconnection]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);

		}

		[Test]
		public void Step2_Missing_Site_Name_Title_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).Clear();
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("SiteName")).Displayed, Is.True);
		}

		[Test]
		public void Step2_Missing_Site_Url_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).Clear();
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("SiteUrl")).Displayed, Is.True);
		}

		[Test]
		public void Step2_Missing_ConnectionString_Should_Prevent_Contine()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).Clear();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("ConnectionString")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Email_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).Clear();
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminEmail")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Password_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).Clear();
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminPassword")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Not_Min_Length_Admin_Password_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("1");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("AdminPassword")).Displayed, Is.True);
		}

		[Test]
		public void Step3_Missing_Admin_Password2_Should_Prevent_Continue()
		{
			// Arrange
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("not empty");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).Clear();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".help-block")), Is.True);
			Assert.That(Driver.FindElement(By.Id("password2")).Displayed, Is.True);
		}

		[Test]
		public void Step4_Test_Attachments_Folder_Button_With_Existing_Folder_Should_Display_Success_Toast()
		{
			// Arrange
			string sitePath = TestConstants.WEB_PATH;
			Guid folderGuid = Guid.NewGuid();
			string attachmentsFolder = Path.Combine(sitePath, "AcceptanceTests", folderGuid.ToString());		
			Directory.CreateDirectory(attachmentsFolder);

			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AttachmentsFolder")).Clear();
			Driver.FindElement(By.Id("AttachmentsFolder")).SendKeys("~/AcceptanceTests/" + folderGuid);
			Driver.FindElement(By.CssSelector("button[id=testattachments]")).Click();

			// Assert
			try
			{
				Assert.That(Driver.IsElementDisplayed(By.CssSelector("#toast-container")), Is.True);
			}
			finally
			{
				Directory.Delete(attachmentsFolder, true);
			}
		}

		[Test]
		public void Step4_Test_Attachments_Folder_Button_With_Missing_Folder_Should_Display_Failure_Modal()
		{
			// Arrange
			Guid folderGuid = Guid.NewGuid();
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).SendKeys("not empty");
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("not empty");
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
			Driver.FindElement(By.Id("AdminPassword")).SendKeys("not empty");
			Driver.FindElement(By.Id("password2")).SendKeys("not empty");
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.Id("AttachmentsFolder")).Clear();
			Driver.FindElement(By.Id("AttachmentsFolder")).SendKeys("~/" + folderGuid);
			Driver.FindElement(By.CssSelector("button[id=testattachments]")).Click();

			// Assert
			Assert.That(Driver.IsElementDisplayed(By.CssSelector(".bootbox")), Is.True);
		}

		[Test]
		public void Navigation_Persists_Field_Values_Correctly()
		{
			// Arrange
			string sitePath = TestConstants.WEB_PATH;
			Guid folderGuid = Guid.NewGuid();
			Driver.Navigate().GoToUrl(BaseUrl);
			ClickLanguageLink();

			// Act
			Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
			Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

			Driver.FindElement(By.Id("SiteName")).Clear();
			Driver.FindElement(By.Id("SiteName")).SendKeys("Site Name");

			Driver.FindElement(By.Id("SiteUrl")).Clear();
			Driver.FindElement(By.Id("SiteUrl")).SendKeys("Site Url");

			Driver.FindElement(By.Id("ConnectionString")).Clear();
			Driver.FindElement(By.Id("ConnectionString")).SendKeys("Connection String");
			SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
			select.SelectByValue("MySQL");

			Driver.FindElement(By.CssSelector("div.continue button")).Click();
			Driver.FindElement(By.CssSelector("div.continue button")).Click();

			Driver.FindElement(By.CssSelector("div.previous a")).Click();
			Driver.FindElement(By.CssSelector("div.previous a")).Click();

			// Assert
			Assert.That(Driver.FindElement(By.Id("SiteName")).GetAttribute("value"), Is.EqualTo("Site Name"));
			Assert.That(Driver.FindElement(By.Id("SiteUrl")).GetAttribute("value"), Is.EqualTo("Site Url"));
			Assert.That(Driver.FindElement(By.Id("ConnectionString")).GetAttribute("value"), Is.EqualTo("Connection String"));

			select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
			Assert.That(select.SelectedOption.GetAttribute("value"), Is.EqualTo("MySQL"));
		}

		[TestFixture]
		[Category("Acceptance")]
		public class OtherDatabases : AcceptanceTestBase
		{
			protected void ClickLanguageLink()
			{
				int english = 0;
				Driver.FindElements(By.CssSelector("ul#language a"))[english].Click();
			}

			[Test]
			[Explicit("Requires MySQL 5 installed on the machine the acceptance tests are running first.")]
			public void MySQL_All_Steps_With_Minimum_Required()
			{
				// Arrange
				Driver.Navigate().GoToUrl(BaseUrl);
				ClickLanguageLink();

				//
				// ***Act***
				//

				// step 1
				Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
				Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

				// step 2
				Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
				SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
				select.SelectByValue("MySQL");

				Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"server=localhost;database=roadkill;uid=root;pwd=Passw0rd;");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3b
				Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
				Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
				Driver.FindElement(By.Id("password2")).SendKeys("password");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 4
				Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step5
				Assert.That(Driver.FindElement(By.CssSelector(".alert strong")).Text, Is.EqualTo("Installation successful"), Driver.PageSource);
				Driver.FindElement(By.CssSelector(".continue a")).Click();

				// login, create a page
				LoginAsAdmin();
				CreatePageWithTitleAndTags("Homepage", "homepage");

				//
				// ***Assert***
				//
				Driver.Navigate().GoToUrl(BaseUrl);
				Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
				Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
			}

			[Test]
			[Explicit("Requires Postgres 9 server installed on the machine the acceptance tests are running first.")]
			public void Postgres_All_Steps_With_Minimum_Required()
			{
				// Arrange
				Driver.Navigate().GoToUrl(BaseUrl);
				ClickLanguageLink();

				//
				// ***Act***
				//

				// step 1
				Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
				Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

				// step 2
				Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
				SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
				select.SelectByValue("Postgres");

				Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"server=localhost;database=roadkill;uid=postgres;pwd=Passw0rd;");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3b
				Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
				Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
				Driver.FindElement(By.Id("password2")).SendKeys("password");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 4
				Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step5
				Assert.That(Driver.FindElement(By.CssSelector(".alert strong")).Text, Is.EqualTo("Installation successful"), Driver.PageSource);
				Driver.FindElement(By.CssSelector(".continue a")).Click();

				// login, create a page
				LoginAsAdmin();
				CreatePageWithTitleAndTags("Homepage", "homepage");

				//
				// ***Assert***
				//
				Driver.Navigate().GoToUrl(BaseUrl);
				Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
				Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
			}

			[Test]
			[Explicit("Requires SQL Server Express 2012 (but it uses the Lightspeed SQL Server 2005 driver) installed on the machine the acceptance tests are running first, using LocalDB.")]
			public void SQLServer2005Driver_All_Steps_With_Minimum_Required()
			{
				// Arrange
				Driver.Navigate().GoToUrl(BaseUrl);
				ClickLanguageLink();

				//
				// ***Act***
				//

				// step 1
				Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
				Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

				// step 2
				Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
				SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
				select.SelectByValue("SqlServer2008");

				Driver.FindElement(By.Id("ConnectionString")).SendKeys(TestConstants.CONNECTION_STRING);
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3b
				Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
				Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
				Driver.FindElement(By.Id("password2")).SendKeys("password");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 4
				Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step5
				Assert.That(Driver.FindElement(By.CssSelector(".alert strong")).Text, Is.EqualTo("Installation successful"), Driver.PageSource);
				Driver.FindElement(By.CssSelector(".continue a")).Click();

				// login, create a page
				LoginAsAdmin();
				CreatePageWithTitleAndTags("Homepage", "homepage");

				//
				// ***Assert***
				//
				Driver.Navigate().GoToUrl(BaseUrl);
				Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
				Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
			}

			[Test]
			[Explicit(@"This is really a helper test, it installs onto .\SQLEXPRESS, database 'roadkill' using integrated security")]
			public void SQLServerExpress_All_Steps_With_Minimum_Required()
			{
				// Arrange
				Driver.Navigate().GoToUrl(BaseUrl);
				ClickLanguageLink();

				//
				// ***Act***
				//

				// step 1
				Driver.FindElement(By.CssSelector("button[id=testwebconfig]")).Click();
				Driver.WaitForElementDisplayed(By.CssSelector("#bottom-buttons > a")).Click();

				// step 2
				Driver.FindElement(By.Id("SiteName")).SendKeys("Acceptance tests");
				SelectElement select = new SelectElement(Driver.FindElement(By.Id("DatabaseName")));
				select.SelectByValue("SqlServer2008");

				Driver.FindElement(By.Id("ConnectionString")).SendKeys(@"Server=.\SQLEXPRESS;Integrated Security=true;database=roadkill");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 3b
				Driver.FindElement(By.Id("AdminEmail")).SendKeys("admin@localhost");
				Driver.FindElement(By.Id("AdminPassword")).SendKeys("password");
				Driver.FindElement(By.Id("password2")).SendKeys("password");
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step 4
				Driver.FindElement(By.CssSelector("input[id=UseObjectCache]")).Click();
				Driver.FindElement(By.CssSelector("div.continue button")).Click();

				// step5
				Assert.That(Driver.FindElement(By.CssSelector(".alert strong")).Text, Is.EqualTo("Installation successful"), Driver.PageSource);
				Driver.FindElement(By.CssSelector(".continue a")).Click();

				// login, create a page
				LoginAsAdmin();
				CreatePageWithTitleAndTags("Homepage", "homepage");

				//
				// ***Assert***
				//
				Driver.Navigate().GoToUrl(BaseUrl);
				Assert.That(Driver.FindElement(By.CssSelector(".pagetitle")).Text, Contains.Substring("Homepage"));
				Assert.That(Driver.FindElement(By.CssSelector("#pagecontent p")).Text, Contains.Substring("Some content goes here"));
			}
		}
	}
}
