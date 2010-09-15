using System;
using System.Reflection;

namespace IDLTools
{
	/// <summary>
	/// Summary description for IDLAttributes.
	/// </summary>
	public class IDLAttributes
	{
		public IDLAttributes()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}

	// create custom attribute to be assigned to class members
	[AttributeUsage(AttributeTargets.Class |
		 AttributeTargets.Constructor |
		 AttributeTargets.Field |
		 AttributeTargets.Method |
		 AttributeTargets.Property,
		 AllowMultiple = true)]
	public class Persist : System.Attribute
	{
		public enum PersistanceType
		{
			CONFIGURATION,
			CONTEXT
		}
		// attribute constructor for 
		// positional parameters
		public Persist
			(PersistanceType type)
		{
			this.Type = type;
		}

		// accessor
		public PersistanceType type
		{
			get
			{
				return Type;
			}
		}
        
		// private member data 
		private PersistanceType Type;
	}
}

