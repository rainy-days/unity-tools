
#if DEVELOPMENT_BUILD || UNITY_EDITOR || RAINYDAYS_ASSERTIONS
#	define RAINY_DAYS_ASSERTIONS_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainyDays
{
	public class AssertionFailed : Exception
	{
		public int SkipFrames { get; set; }

		public AssertionFailed(string description)
			: base(description)
		{
		}

		public static string FormatStackTrace(string trace, int skipFrames)
		{
			if (trace == null)
			{
				return null;
			}
			int skipped = 0, index = 0;
			while (skipped < skipFrames)
			{
				index = trace.IndexOf(Environment.NewLine, index) + 1;
				if (index == 0) break;
				++skipped;
			}
			return trace.Substring(index);
		}

		/// <summary>
		/// Unity hack. Overrides the base exception StackTrace to skip the first SkipFrames entries.
		/// </summary>
		/// <remarks>
		/// Unity uses this property in the Console window; skipping the first entries
		/// allows you to open the script at the offending line rather than at the
		/// Assert method that threw the AssertionFailed exception.
		/// </remarks>
		public override string StackTrace
		{
			get
			{
				return FormatStackTrace(base.StackTrace, SkipFrames);
			}
		}
	}

	/// <summary>
	/// Utility class checking for and handling programmer errors.
	/// </summary>
	/// <remarks>
	/// This class is similar to UnityEngine.Assertions.Assert, but it:
	/// 1. Always raises exceptions (cannot be turned off programmatically by design)
	/// 2. Is always compiled in development or editor builds (cannot turn them off by design)
	/// 3. Can be forced always on in release builds by adding the RAINYDAYS_ASSERTIONS define
	/// </remarks>
	public static class Assert
	{

#if RAINY_DAYS_ASSERTIONS_ENABLED
		public const bool Enabled = true;
#else
		public const bool Enabled = false;
#endif

		private const string DefAssertionsEnabled = "RAINY_DAYS_ASSERTIONS_ENABLED";

		private static void Throw(AssertionFailed exception)
		{
			// skip this method and the assertion method
			exception.SkipFrames = 2;
			throw exception;
		}

		[Conditional(DefAssertionsEnabled)]
		public static void Fatal(string format, params object[] args)
		{
			Throw(new AssertionFailed(string.Format(format, args)));
		}

		[Conditional(DefAssertionsEnabled)]
		public static void IsTrue(bool condition)
		{
			if (!condition)
			{
				Throw(new AssertionFailed("condition is False."));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void IsFalse(bool condition)
		{
			if (condition)
			{
				Throw(new AssertionFailed("condition is True."));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void AreEqual(object a, object b)
		{
			if (!object.Equals(a, b))
			{
				var description = string.Format("\"{0}\" and \"{1}\" are not equal.", PrettyPrint(a), PrettyPrint(b));
				Throw(new AssertionFailed(description));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void AreNotEqual(object a, object b)
		{
			if (object.Equals(a, b))
			{
				var description = string.Format("\"{0}\" and \"{1}\" are equal.", PrettyPrint(a), PrettyPrint(b));
				Throw(new AssertionFailed(description));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void IsNull(object a)
		{
			if (!object.ReferenceEquals(null, a))
			{
				var description = string.Format("\"{0}\" is not null.", PrettyPrint(a));
				Throw(new AssertionFailed(description));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void IsNotNull(object a)
		{
			if (object.ReferenceEquals(null, a))
			{
				var description = string.Format("\"{0}\" is null.", PrettyPrint(a));
				Throw(new AssertionFailed(description));
			}
		}

		/// <summary>
		/// Asserts if the two single precision floating point numbers are not approximately equal
		/// with respect to the given epsilon.
		/// </summary>
		/// <param name="eps">The epsilon. Defaults to 1e-6f, or 0.000001f.</param>
		[Conditional(DefAssertionsEnabled)]
		public static void AreApproximatelyEqual(float a, float b, float eps = 1e-6f)
		{
			float diff = System.Math.Abs(a - b);
			if (diff > eps)
			{
				var description = string.Format("\"{0}\" is not approximately equal to \"{1}\" (diff = \"{2}\", eps = \"{3}\").", a, b, diff, eps);
				Throw(new AssertionFailed(description));
			}
		}

		/// <summary>
		/// Asserts if the two double precision floating point numbers are not approximately equal
		/// with respect to the given epsilon.
		/// </summary>
		/// <param name="eps">The epsilon. Defaults to 1e-6, or 0.000001.</param>
		[Conditional(DefAssertionsEnabled)]
		public static void AreApproximatelyEqual(double a, double b, double eps = 1e-6)
		{
			double diff = System.Math.Abs(a - b);
			if (diff > eps)
			{
				var description = string.Format("\"{0}\" is not approximately equal to \"{1}\" (diff = \"{2}\", eps = \"{3}\").", a, b, diff, eps);
				Throw(new AssertionFailed(description));
			}
		}

		[Conditional(DefAssertionsEnabled)]
		public static void IsNotNullOrEmpty(string a)
		{
			if (string.IsNullOrEmpty(a))
			{
				Throw(new AssertionFailed("String is null or empty."));
			}
		}

		private static object PrettyPrint(object o)
		{
			return o ?? "<null>";
		}

	}
}
