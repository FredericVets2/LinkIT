﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using LinkIT.Data.DTO;
using LinkIT.Data.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LinkIT.Data.IntegrationTests.RepositoryTests.SpecialOwnerRepo
{
	[TestClass]
	public class WhenInsertingANewSpecialOwner
	{
		private IEnumerable<SpecialOwnerDto> _expected;
		private SpecialOwnerRepository _sut;

		private void AssertDto(SpecialOwnerDto expected, DateTime created)
		{
			expected.CreationDate = expected.ModificationDate = created;
			expected.ModifiedBy = expected.CreatedBy;

			var actual = _sut.GetById(expected.Id.Value);

			Assert.IsNotNull(actual);
			Assert.AreEqual(expected, actual);
		}

		[TestInitialize]
		public void Setup()
		{
			var conStr = ConfigurationManager.ConnectionStrings["LinkITConnectionString"].ConnectionString;
			_sut = new SpecialOwnerRepository(conStr);

			_expected = new List<SpecialOwnerDto>
			{
				new SpecialOwnerDto
				{
					CreatedBy = "user1",
					Name = "Special one",
					Remark = "I'm special"
				},
				new SpecialOwnerDto
				{
					CreatedBy = "user2",
					Name = "The default one"
					// Don't set the remark
				}
			};
		}

		[TestMethod]
		public void ThenTheDataIsInserted()
		{
			var created = DateTime.Now;
			DateTimeProvider.SetDateTime(created);

			_expected.ToList().ForEach(x => x.Id = _sut.Insert(x));

			foreach (var dto in _expected)
				AssertDto(dto, created);
		}

		[TestCleanup]
		public void Cleanup()
		{
			_expected.ToList().ForEach(x => _sut.Delete(x.Id.Value));
		}
	}
}