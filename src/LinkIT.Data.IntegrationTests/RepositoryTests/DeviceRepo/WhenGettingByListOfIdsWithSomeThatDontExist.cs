﻿using LinkIT.Data.DTO;
using LinkIT.Data.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace LinkIT.Data.IntegrationTests.RepositoryTests.DeviceRepo
{
	[TestClass]
	public class WhenGettingByListOfIdsWithSomeThatDontExist
	{
		private List<DeviceDto> _expected;
		private DeviceRepository _sut;

		[TestInitialize]
		public void Setup()
		{
			var conStr = ConfigurationManager.ConnectionStrings["LinkITConnectionString"].ConnectionString;
			_sut = new DeviceRepository(conStr);

			_expected = new List<DeviceDto>()
			{
				new DeviceDto
				{
					Brand = "HP",
					Type = "AwesomeBook",
					Owner = "Unknown",
					Tag = "CRD-X-11111"
				},
				new DeviceDto
				{
					Brand = "Dell",
					Type = "Latitude 7290",
					Owner = "Unknown",
					Tag = "CRD-X-22222"
				},
				new DeviceDto
				{
					Brand = "Dell",
					Type = "Latitude 7490",
					Owner = "Unknown",
					Tag = "CRD-X-33333"
				},
				new DeviceDto
				{
					Brand = "Dell",
					Type = "Latitude 5590",
					Owner = "Unknown",
					Tag = "CRD-X-44444"
				}
			};

			_expected.ForEach(x => x.Id = _sut.Insert(x));
		}

		[TestMethod]
		public void ThenAnExceptionIsThrown()
		{
			var ids = _expected.Select(x => x.Id.Value).ToList();

			// Add range of non existing items.
			ids.AddRange(new[] { -1L, -1L, -2L, -3L });

			Assert.ThrowsException<ArgumentException>(
				() => _sut.GetById(ids),
				"Not all supplied id's exist.");
		}

		[TestCleanup]
		public void CleanUp()
		{
			_expected.ForEach(x => _sut.Delete(x.Id.Value));
		}
	}
}