﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinkIT.Data
{
	public static class StringExtensions
	{
		public static string[] SplitForSeparator(this string input, char separator)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new ArgumentNullException(nameof(input));

			return input.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();
		}

		public static string[] SplitCommaSeparated(this string input) =>
			SplitForSeparator(input, ',');

		/// <summary>
		/// Splits an input string formatted like "key1: value1, key2: value2" into a dictionary.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static IDictionary<string, string> SplitKeyValuePairs(this string input)
		{
			var result = new Dictionary<string, string>();

			var pairs = input.SplitCommaSeparated();
			foreach (var pair in pairs)
			{
				var splitted = pair.SplitForSeparator(':');
				result.Add(splitted[0], splitted[1]);
			}

			return result;
		}
	}
}