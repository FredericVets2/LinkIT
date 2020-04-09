﻿using LinkIT.Data.DTO;
using LinkIT.Data.IntegrationTests.RepositoryTests.Helpers;
using LinkIT.Data.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LinkIT.Data.IntegrationTests.RepositoryTests.SpecialOwnerRepo
{
	[TestClass]
	public class WhenCheckingForExistence
	{
		private SpecialOwnerDto _specialOwner;
		private SpecialOwnerRepository _sut;

		[TestInitialize]
		public void Setup()
		{
			_sut = new SpecialOwnerRepository(ConnectionString.Get());

			_specialOwner = new SpecialOwnerDto
			{
				CreatedBy = "user1",
				Name = "Special one",
				Remark = "I'm special"
			};

			_specialOwner.Id = _sut.Insert(_specialOwner);
		}

		[TestMethod]
		public void ThenTheSpecialOwnerExists()
		{
			bool actual = _sut.Exists(_specialOwner.Id.Value);
			Assert.IsTrue(actual);
		}

		[TestCleanup]
		public void Cleanup() =>
			new DatabaseHelper().HardDeleteAll();
	}
}