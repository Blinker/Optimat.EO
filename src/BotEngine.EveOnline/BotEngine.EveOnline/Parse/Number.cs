﻿using Bib3;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BotEngine.EveOnline.Parse
{
	/// <summary>
	/// Number formatting in the eve online client depends on number format configured in windows.
	/// For this reason, the number parsing works with a range of different group separators and decimal separators.
	/// </summary>
	static public class Number
	{
		const string InNumberRegexPatternSignGroupName = "Sign";
		const string InNumberRegexPatternPreDecimalSeparatorGroupName = "PreDecimalSeparator";
		const string InNumberRegexPatternPostDecimalSeparatorGroupName = "PostDecimalSeparator";
		const string InNumberRegexPatternDecimalSeparatorGroupName = "DecimalSeparator";
		const string InNumberRegexPatternDigitGroupSeparatorGroupName = "DigitGroupSeparator";

		static readonly string DefaultNumberFormatRegexPattern = DefaultNumberFormatRegexPatternConstruct();

		static readonly public Regex DefaultNumberFormatRegex =
			new Regex(DefaultNumberFormatRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

		static public string DefaultNumberFormatRegexPatternConstruct(
			bool AllowLeadingCharacters = false,
			bool AllowTrailingCharacters = false,
			string GroupIdSufix = null)
		{
			return
				NumberFormatRegexPatternConstruct(
					new string[] { "+", "-" },
					new string[] { ".", "," },
					new string[] { ".", ",", "'", " ", ""
						//	،: "Pashto" (ps)
						,"،"
					},
					AllowLeadingCharacters,
					AllowTrailingCharacters,
					GroupIdSufix);
		}

		static public string RegexPatternAlternativeConstruct(
			string[] SetOption)
		{
			if (null == SetOption)
			{
				return null;
			}

			var SetCandidateEscaped =
				SetOption
				.WhereNotDefault()
				.Select(candidate =>
				{
					if (0 < candidate.Length && candidate?.Trim().Length < 1)
					{
						return @"\s";
					}

					return Regex.Escape(candidate);
				}).ToArray();

			return
				"(" +
				string.Join(
				"|",
				SetCandidateEscaped) +
				")";
		}

		static public string NumberFormatRegexPatternConstruct(
			string[] SetSignOption,
			string[] SetDecimalSeparatorOption,
			string[] SetDigitGroupSeparatorOption,
			bool AllowLeadingCharacters = false,
			bool AllowTrailingCharacters = false,
			string GroupIdSufix = null)
		{
			var InNumberRegexPatternDigitGroupSeparatorGroupName =
				Number.InNumberRegexPatternDigitGroupSeparatorGroupName + (GroupIdSufix ?? "");

			var InNumberRegexPatternSignGroupName =
				Number.InNumberRegexPatternSignGroupName + (GroupIdSufix ?? "");

			var InNumberRegexPatternPreDecimalSeparatorGroupName =
				Number.InNumberRegexPatternPreDecimalSeparatorGroupName + (GroupIdSufix ?? "");

			SetSignOption = (SetSignOption ?? new string[0]).WhereNotDefault().Concat(new string[] { "" }).ToArray();

			SetDigitGroupSeparatorOption = (SetDigitGroupSeparatorOption ?? new string[0]).WhereNotDefault().ToArray();

			var PatternSign = RegexPatternAlternativeConstruct(SetSignOption);

			var PatternDecimalSeparator = "(?!\\<" + InNumberRegexPatternDigitGroupSeparatorGroupName + ">)" + RegexPatternAlternativeConstruct(SetDecimalSeparatorOption);
			var PatternDigitGroupSeparator = RegexPatternAlternativeConstruct(SetDigitGroupSeparatorOption);

			//	Grupe direkt vor Dezimaltrenzaice mus drai Zifern enthalte. Grupe Linx davon dürfe zwai oder drai Zifern enthalte.
			//	d.h. inerhalb der optionaale Grupe welce ale Zaice zwisce inklusiiv früühescte Grupetrenzaice und Dezimaltrenzaice enthalt
			//	isc zuusäzlic auf linker Saite optionaale Grupe für Ziferngrupe mit variabler Anzaal von Zifern enthalte.
			//	da di Grupe welce das Zaice für Ziferngrupiirung ersctmaals fängt linx von ale andere vorkome des Ziferngrupiirungszaice scteehe sol werd inerhalb
			//	der optionaale Grupe di Folge von Ziferngrupe und Ziferngrupiirungszaice umgekeert.
			var TailVorDezimaaltrenzaiceTailOptioonTailOptioonVarAnz =
				@"(\d{2,3}" +
				@"\<" + InNumberRegexPatternDigitGroupSeparatorGroupName + @">)*";

			var TailVorDezimaaltrenzaiceTailOptioon =
				"((?<" + InNumberRegexPatternDigitGroupSeparatorGroupName + ">" + PatternDigitGroupSeparator + @")" +
				TailVorDezimaaltrenzaiceTailOptioonTailOptioonVarAnz +
				@"\d{3})";

			var PatternPreDecimalSeparator =
				@"\d+" +
				@"((" + TailVorDezimaaltrenzaiceTailOptioon + ")|)";

			//	post decimal seperator: allow for any number of digits except three.
			var PatternPostDecimalSeparator = @"(\d{0,2}|\d{4,})";

			var PatternBegin = AllowLeadingCharacters ? "" : "^\\s*";

			var PatternEnd =
				//	prevent trailing digits (negative lookahead)
				@"(?!\d)" +
				(AllowTrailingCharacters ? "" : "\\s*$");

			return
				PatternBegin +
				"(?<" + InNumberRegexPatternSignGroupName + ">" +
				PatternSign + ")" +

				//	spaces between sign and value.
				@"\s*" +

				"(?<" + InNumberRegexPatternPreDecimalSeparatorGroupName + ">" +
				PatternPreDecimalSeparator + ")" +
				"(|(?<" + InNumberRegexPatternDecimalSeparatorGroupName + ">" + PatternDecimalSeparator + ")" +
				"(?<" + InNumberRegexPatternPostDecimalSeparatorGroupName + ">" +
				PatternPostDecimalSeparator +
				"))" +
				PatternEnd;
		}

		/// <summary>
		/// parses a decimal number and returns the number multiplied by thousand.
		/// </summary>
		/// <param name="NumberString"></param>
		/// <returns></returns>
		static public Int64? NumberParseDecimalMilli(this string NumberString)
		{
			if (null == NumberString)
			{
				return null;
			}

			var RegexMatch = DefaultNumberFormatRegex.Match(NumberString.Trim());

			if (!(RegexMatch?.Success ?? false))
			{
				return null;
			}

			var Sign = 1;

			var SignString = RegexMatch.Groups[InNumberRegexPatternSignGroupName].Value;

			if ("-" == SignString)
			{
				Sign = -1;
			}

			var PreDecimalSeparator = RegexMatch.Groups[InNumberRegexPatternPreDecimalSeparatorGroupName].Value;
			var PostDecimalSeparator = RegexMatch.Groups[InNumberRegexPatternPostDecimalSeparatorGroupName].Value;

			var PreDecimalSeparatorLessSeparator = Regex.Replace(PreDecimalSeparator, "[^\\d]+", "");
			var PostDecimalSeparatorLessSeparator = Regex.Replace(PostDecimalSeparator, "[^\\d]+", "");

			var PreDecimalSeparatorValue =
				0 < PreDecimalSeparatorLessSeparator.Length ?
				Int64.Parse(PreDecimalSeparatorLessSeparator) :
				0;

			var PostDecimalSeparatorValueMikro =
				0 < PostDecimalSeparatorLessSeparator.Length ?
				(Int64)(Int64.Parse(PostDecimalSeparatorLessSeparator) * Math.Pow(10, 6 - PostDecimalSeparatorLessSeparator.Length)) :
				0;

			return Sign * (PreDecimalSeparatorValue * 1000 + PostDecimalSeparatorValueMikro / 1000);
		}

		static public Int64? NumberParseDecimal(this string NumberString) =>
			NumberParseDecimalMilli(NumberString) / 1000;

	}
}
