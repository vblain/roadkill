﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mindscape.LightSpeed;
using NUnit.Framework;
using Roadkill.Core.Configuration;
using Roadkill.Core.Database.LightSpeed;
using Roadkill.Core.Database.MongoDB;
using IRepository = Roadkill.Core.Database.IRepository;

namespace Roadkill.Tests.Integration.Repository.LightSpeed
{
	[TestFixture]
	[Category("Integration")]
	public class LightSpeedPageRepositoryTests : PageRepositoryTests
	{
		protected override string ConnectionString
		{
			get { return TestConstants.CONNECTION_STRING; }
		}

		protected override IRepository GetRepository()
		{
			return new LightSpeedRepository(DataProvider.SqlServer2008, ApplicationSettings.ConnectionString);
		}

		protected override void Clearup()
		{
			TestHelpers.SqlServerSetup.RecreateTables();
		}
	}
}
