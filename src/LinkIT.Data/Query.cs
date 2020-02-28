﻿namespace LinkIT.Data
{
	public abstract class Query
	{
		public Query()
		{
			LogicalOperator = LogicalOperator.AND;
		}

		public long? Id { get; set;}

		public LogicalOperator LogicalOperator { get; set; }
	}
}