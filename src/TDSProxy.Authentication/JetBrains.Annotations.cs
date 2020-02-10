// ReSharper disable all
using System;

namespace JetBrains.Annotations
{
	/// <summary>
	/// Indicates that the value of the marked element could be <c>null</c> sometimes,
	/// so checking for <c>null</c> is required before its usage.
	/// </summary>
	/// <example><code>
	/// [CanBeNull] object Test() => null;
	/// 
	/// void UseTest() {
	///   var p = Test();
	///   var s = p.ToString(); // Warning: Possible 'System.NullReferenceException'
	/// }
	/// </code></example>
	[AttributeUsage(
		AttributeTargets.Method |
		AttributeTargets.Parameter |
		AttributeTargets.Property |
		AttributeTargets.Delegate |
		AttributeTargets.Field |
		AttributeTargets.Event |
		AttributeTargets.Class |
		AttributeTargets.Interface |
		AttributeTargets.GenericParameter)]
	internal sealed class CanBeNullAttribute : Attribute
	{
	}

	/// <summary>
	/// Indicates that the value of the marked element can never be <c>null</c>.
	/// </summary>
	/// <example><code>
	/// [NotNull] object Foo() {
	///   return null; // Warning: Possible 'null' assignment
	/// }
	/// </code></example>
	[AttributeUsage(
		AttributeTargets.Method |
		AttributeTargets.Parameter |
		AttributeTargets.Property |
		AttributeTargets.Delegate |
		AttributeTargets.Field |
		AttributeTargets.Event |
		AttributeTargets.Class |
		AttributeTargets.Interface |
		AttributeTargets.GenericParameter)]
	internal sealed class NotNullAttribute : Attribute
	{
	}

	/// <summary>
	/// Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
	/// so this symbol will not be reported as unused (as well as by other usage inspections).
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	internal sealed class UsedImplicitlyAttribute : Attribute
	{
		public UsedImplicitlyAttribute()
			: this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
			: this(useKindFlags, ImplicitUseTargetFlags.Default)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
			: this(ImplicitUseKindFlags.Default, targetFlags)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
		{
			UseKindFlags = useKindFlags;
			TargetFlags = targetFlags;
		}

		public ImplicitUseKindFlags UseKindFlags { get; }

		public ImplicitUseTargetFlags TargetFlags { get; }
	}

	/// <summary>
	/// Can be applied to attributes, type parameters, and parameters of a type assignable from <see cref="System.Type"/> .
	/// When applied to an attribute, the decorated attribute behaves the same as <see cref="UsedImplicitlyAttribute"/>.
	/// When applied to a type parameter or to a parameter of type <see cref="System.Type"/>,  indicates that the corresponding type
	/// is used implicitly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.GenericParameter | AttributeTargets.Parameter)]
	internal sealed class MeansImplicitUseAttribute : Attribute
	{
		public MeansImplicitUseAttribute()
			: this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
		{
		}

		public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
			: this(useKindFlags, ImplicitUseTargetFlags.Default)
		{
		}

		public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
			: this(ImplicitUseKindFlags.Default, targetFlags)
		{
		}

		public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
		{
			UseKindFlags = useKindFlags;
			TargetFlags = targetFlags;
		}

		[UsedImplicitly]
		public ImplicitUseKindFlags UseKindFlags { get; }

		[UsedImplicitly]
		public ImplicitUseTargetFlags TargetFlags { get; }
	}

	/// <summary>
	/// Specify the details of implicitly used symbol when it is marked
	/// with <see cref="MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>.
	/// </summary>
	[Flags]
	internal enum ImplicitUseKindFlags
	{
		Default = Access | Assign | InstantiatedWithFixedConstructorSignature,

		/// <summary>Only entity marked with attribute considered used.</summary>
		Access = 1,

		/// <summary>Indicates implicit assignment to a member.</summary>
		Assign = 2,

		/// <summary>
		/// Indicates implicit instantiation of a type with fixed constructor signature.
		/// That means any unused constructor parameters won't be reported as such.
		/// </summary>
		InstantiatedWithFixedConstructorSignature = 4,

		/// <summary>Indicates implicit instantiation of a type.</summary>
		InstantiatedNoFixedConstructorSignature = 8,
	}

	/// <summary>
	/// Specify what is considered to be used implicitly when marked
	/// with <see cref="MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>.
	/// </summary>
	[Flags]
	internal enum ImplicitUseTargetFlags
	{
		Default = Itself,
		Itself = 1,

		/// <summary>Members of entity marked with attribute are considered used.</summary>
		Members = 2,

		/// <summary>Entity marked with attribute and all its members considered used.</summary>
		WithMembers = Itself | Members
	}

	/// <summary>
	/// This attribute is intended to mark publicly available API
	/// which should not be removed and so is treated as used.
	/// </summary>
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class PublicAPIAttribute : Attribute
	{
		public PublicAPIAttribute()
		{
		}

		public PublicAPIAttribute([NotNull] string comment)
		{
			Comment = comment;
		}

		[CanBeNull]
		public string Comment { get; }
	}
}
