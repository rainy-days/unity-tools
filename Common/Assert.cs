using System;
using System.Diagnostics;

namespace RainyDays
{
	public class AssertionFailed : Exception
	{
		public AssertionFailed(string description)
			: base(description)
		{
		}

		/// <summary>
		/// Overrides the base exception StackTrace to skip the first entry.
		/// </summary>
		/// <remarks>
		/// Unity uses this property in the Console window; skipping the first entry
		/// allows you to open the script at the offending line rather than at the
		/// Assert method that threw the AssertionFailed exception.
		/// </remarks>
		public override string StackTrace
		{
			get
			{
				var trace = base.StackTrace;
				return trace.Substring(trace.IndexOf(Environment.NewLine) + 1);
			}
		}
	}

	public static class Assert
	{
		private const string DevelopmentBuild = "DEVELOPMENT_BUILD";
		private const string EditorBuild = "UNITY_EDITOR";
		private const string UserOverride = "RAINY_DAYS_ASSERTS_ENABLED";

		[Conditional(DevelopmentBuild), Conditional(EditorBuild), Conditional(UserOverride)]
		public static void Fatal()
		{
			throw new AssertionFailed("Fatal.");
		}

		[Conditional(DevelopmentBuild), Conditional(EditorBuild), Conditional(UserOverride)]
		public static void IsTrue(bool condition)
		{
			if (!condition)
			{
				throw new AssertionFailed("condition is False.");
			}
		}

		[Conditional(DevelopmentBuild), Conditional(EditorBuild), Conditional(UserOverride)]
		public static void IsFalse(bool condition)
		{
			if (condition)
			{
				throw new AssertionFailed("condition is True.");
			}
		}

		[Conditional(DevelopmentBuild), Conditional(EditorBuild), Conditional(UserOverride)]
		public static void AreEqual(object a, object b)
		{
			if (!object.Equals(a, b))
			{
				var description = string.Format("\"{0}\" and \"{1}\" are not equal.", PrettyPrint(a), PrettyPrint(b));
				throw new AssertionFailed(description);
			}
		}

		[Conditional(DevelopmentBuild), Conditional(EditorBuild), Conditional(UserOverride)]
		public static void AreNotEqual(object a, object b)
		{
			if (object.Equals(a, b))
			{
				var description = string.Format("\"{0}\" and \"{1}\" are equal.", PrettyPrint(a), PrettyPrint(b));
				throw new AssertionFailed(description);
			}
		}

		private static object PrettyPrint(object o)
		{
			return o ?? "<null>";
		}

	}
}
