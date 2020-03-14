﻿using LinkIT.Data.DTO;
using LinkIT.Data.Paging;
using LinkIT.Data.Queries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace LinkIT.Data.Repositories
{
	public class DeviceRepository : Repository, IDeviceRepository
	{
		public const string TAG_COLUMN = "Tag";
		public const string OWNER_COLUMN = "Owner";
		public const string BRAND_COLUMN = "Brand";
		public const string TYPE_COLUMN = "Type";

		public static readonly string[] COLUMNS = new[] { ID_COLUMN, TAG_COLUMN, OWNER_COLUMN, BRAND_COLUMN, TYPE_COLUMN };

		public DeviceRepository(string connectionString) : base(connectionString, TableNames.DEVICE_TABLE) { }

		private static void AddWhereClause(SqlParameterCollection @params, StringBuilder sb, DeviceQuery query)
		{
			var where = new WhereClauseBuilder(@params, query.LogicalOperator, false);
			where.AddParameter(query.Id, ID_COLUMN, SqlDbType.BigInt);
			where.AddParameter(query.Tag, TAG_COLUMN, SqlDbType.NVarChar);
			where.AddParameter(query.Owner, OWNER_COLUMN, SqlDbType.NVarChar);
			where.AddParameter(query.Brand, BRAND_COLUMN, SqlDbType.NVarChar);
			where.AddParameter(query.Type, TYPE_COLUMN, SqlDbType.NVarChar);

			sb.Append(where.Build());
		}

		private static void AddSqlParameters(SqlParameterCollection @params, DeviceDto input)
		{
			var paramBuilder = new SqlParameterBuilder(@params);
			paramBuilder.Add(input.Id, ID_COLUMN, SqlDbType.BigInt);
			paramBuilder.Add(input.Tag, TAG_COLUMN, SqlDbType.NVarChar);
			paramBuilder.Add(input.Owner, OWNER_COLUMN, SqlDbType.NVarChar);
			paramBuilder.Add(input.Brand, BRAND_COLUMN, SqlDbType.NVarChar);
			paramBuilder.Add(input.Type, TYPE_COLUMN, SqlDbType.NVarChar);
		}

		private static IEnumerable<DeviceDto> ReadDtosFrom(SqlDataReader reader)
		{
			while (reader.Read())
			{
				yield return new DeviceDto
				{
					Id = GetColumnValue<long?>(reader, ID_COLUMN),
					Tag = GetColumnValue<string>(reader, TAG_COLUMN),
					Owner = GetColumnValue<string>(reader, OWNER_COLUMN),
					Brand = GetColumnValue<string>(reader, BRAND_COLUMN),
					Type = GetColumnValue<string>(reader, TYPE_COLUMN)
				};
			}
		}

		/// <summary>
		/// This will build the SqlCommand based on the optional query object. The specified Logical operator will be 
		/// used to combine the query arguments.
		/// Has support for paging. This is based on the new paging feature introduced in Sql Serever 2012.
		/// If no query or paging instance is supplied, a select without where clause will be generated.
		/// <see cref="https://social.technet.microsoft.com/wiki/contents/articles/23811.paging-a-query-with-sql-server.aspx#Paginacao_dentro"/>
		/// </summary>
		/// <param name="con"></param>
		/// <param name="tx"></param>
		/// <param name="query"></param>
		/// <param name="paging"></param>
		/// <returns></returns>
		private SqlCommand BuildSelectCommand(
			SqlConnection con,
			SqlTransaction tx,
			DeviceQuery query = null,
			PageInfo pageInfo = null)
		{
			var cmd = new SqlCommand { Connection = con, Transaction = tx };

			var sb = new StringBuilder();
			sb.AppendLine(CreateSelectStatement());

			if (query != null)
				AddWhereClause(cmd.Parameters, sb, query);

			if (pageInfo != null)
				AddPaging(cmd.Parameters, sb, pageInfo);

			cmd.CommandText = sb.ToString();

			return cmd;
		}

		private SqlCommand BuildSelectCountCommand(
			SqlConnection con,
			SqlTransaction tx,
			DeviceQuery query = null)
		{
			var cmd = new SqlCommand { Connection = con, Transaction = tx };

			var sb = new StringBuilder();
			sb.AppendLine(CreateSelectCountStatement());

			if (query != null)
				AddWhereClause(cmd.Parameters, sb, query);

			cmd.CommandText = sb.ToString();

			return cmd;
		}

		public DeviceDto GetById(long id) => GetById(new[] { id }).Single();

		public IEnumerable<DeviceDto> GetById(IEnumerable<long> ids)
		{
			if (ids == null || !ids.Any())
				throw new ArgumentNullException("ids");

			// Filter out possible duplicates.
			var distinctIds = ids.Distinct().ToArray();

			using (var con = new SqlConnection(ConnectionString))
			{
				con.Open();
				using (var tx = con.BeginTransaction())
				{
					using (var cmd = BuildSelectCountCommand(con, tx, distinctIds))
					{
						long count = Convert.ToInt64(cmd.ExecuteScalar());
						if (distinctIds.Length != count)
							throw new ArgumentException("Not all supplied id's exist.");
					}

					using (var cmd = BuildSelectCommand(con, tx, distinctIds))
					using (var reader = cmd.ExecuteReader())
					{
						return ReadDtosFrom(reader).ToList();
					}

					//tx.Commit();
				}
			}
		}

		public IEnumerable<DeviceDto> Query(DeviceQuery query = null)
		{
			using (var con = new SqlConnection(ConnectionString))
			{
				con.Open();
				using (var tx = con.BeginTransaction())
				{
					using (var cmd = BuildSelectCommand(con, tx, query))
					using (var reader = cmd.ExecuteReader())
					{
						return ReadDtosFrom(reader).ToList();
					}

					//tx.Commit();
				}
			}
		}

		public PagedResult<DeviceDto> PagedQuery(PageInfo pageInfo, DeviceQuery query = null)
		{
			if (pageInfo == null)
				throw new ArgumentNullException("pageInfo");

			if (!pageInfo.OrderBy.IsValidFor(COLUMNS))
				throw new ArgumentException($"'{pageInfo.OrderBy.Name}' is an unrecognized column.");

			using (var con = new SqlConnection(ConnectionString))
			{
				con.Open();
				using (var tx = con.BeginTransaction())
				{
					long totalCount;
					using (var cmd = BuildSelectCountCommand(con, tx, query))
					{
						totalCount = Convert.ToInt64(cmd.ExecuteScalar());
					}

					using (var cmd = BuildSelectCommand(con, tx, query, pageInfo))
					using (var reader = cmd.ExecuteReader())
					{
						var result = ReadDtosFrom(reader).ToList();

						return new PagedResult<DeviceDto>(result, pageInfo, totalCount);
					}

					//tx.Commit();
				}
			}
		}

		public long Insert(DeviceDto item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			if (item.Id.HasValue)
				throw new ArgumentException("Id can not be specified.");

			using (var con = new SqlConnection(ConnectionString))
			{
				con.Open();
				using (var tx = con.BeginTransaction())
				{
					string cmdText = $@"INSERT INTO [{TableName}] ([{TAG_COLUMN}], [{OWNER_COLUMN}], [{BRAND_COLUMN}], [{TYPE_COLUMN}]) 
						VALUES (@Tag, @Owner, @Brand, @Type)
						SELECT CONVERT(bigint, SCOPE_IDENTITY())";

					long newId;
					using (var cmd = new SqlCommand(cmdText, con, tx))
					{
						AddSqlParameters(cmd.Parameters, item);

						newId = (long)cmd.ExecuteScalar();
					}

					tx.Commit();

					return newId;
				}
			}
		}

		/// <summary>
		/// This is a full-update. So all required fields should be supplied.
		/// </summary>
		/// <param name="item"></param>
		public void Update(DeviceDto item) => Update(new[] { item });

		public void Update(IEnumerable<DeviceDto> items)
		{
			if (items == null || !items.Any())
				throw new ArgumentNullException("items");

			foreach (var item in items)
			{
				if (!item.Id.HasValue)
					throw new ArgumentException("Id is a required field.");
			}

			using (var con = new SqlConnection(ConnectionString))
			{
				con.Open();
				using (var tx = con.BeginTransaction())
				{
					string cmdText = $@"UPDATE [{TableName}] 
								SET [{TAG_COLUMN}]=@Tag, [{OWNER_COLUMN}]=@Owner, [{BRAND_COLUMN}]=@Brand, [{TYPE_COLUMN}]=@Type 
								WHERE [{ID_COLUMN}]=@Id";

					foreach (var item in items)
					{
						using (var cmd = new SqlCommand(cmdText, con, tx))
						{
							AddSqlParameters(cmd.Parameters, item);

							cmd.ExecuteNonQuery();
						}
					}

					tx.Commit();
				}
			}
		}
	}
}