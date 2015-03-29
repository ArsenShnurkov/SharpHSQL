//==========================================================================================
//
//		OpenNETCF.EnumEx
//		Copyright (c) 2003-2004, OpenNETCF.org
//
//		This library is free software; you can redistribute it and/or modify it under 
//		the terms of the OpenNETCF.org Shared Source License.
//
//		This library is distributed in the hope that it will be useful, but 
//		WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
//		FITNESS FOR A PARTICULAR PURPOSE. See the OpenNETCF.org Shared Source License 
//		for more details.
//
//		You should have received a copy of the OpenNETCF.org Shared Source License 
//		along with this library; if not, email licensing@opennetcf.org to request a copy.
//
//		If you wish to contact the OpenNETCF Advisory Board to discuss licensing, please 
//		email licensing@opennetcf.org.
//
//		For general enquiries, email enquiries@opennetcf.org or visit our website at:
//		http://www.opennetcf.org
//
//==========================================================================================
using System;
using System.Reflection;

namespace OpenNETCF
{
	/// <summary>
	/// Provides helper functions for Enumerations.
	/// </summary>
	/// <remarks>Extends the <see cref="T:System.Enum">System.Enum Class</see>.</remarks>
	/// <seealso cref="T:System.Enum">System.Enum Class</seealso>
	public sealed class EnumEx
	{
		private EnumEx(){}

		#region Get Name
		/// <summary>
		/// Retrieves the name of the constant in the specified enumeration that has the specified value.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
		/// <returns> A string containing the name of the enumerated constant in enumType whose value is value, or null if no such constant is found.</returns>
		/// <exception cref="System.ArgumentException"> enumType is not an System.Enum.  -or-  value is neither of type enumType nor does it have the same underlying type as enumType.</exception>
		/// <example>The following code sample illustrates the use of GetName (Based on the example provided with desktop .NET Framework):
		/// <code>[Visual Basic] 
		/// Imports System
		/// 
		///		Public Class GetNameTest
		/// 
		/// 		Enum Colors
		/// 			Red
		/// 			Green
		/// 			Blue
		/// 			Yellow
		/// 		End Enum 'Colors
		/// 
		///			Enum Styles
		/// 			Plaid
		/// 			Striped
		/// 			Tartan
		/// 			Corduroy
		/// 		End Enum 'Styles
		/// 
		///		Public Shared Sub Main() 
		/// 		MessageBox.Show("The 4th value of the Colors Enum is " + [OpenNETCF.Enum].GetName(GetType(Colors), 3))
		///			MessageBox.Show("The 4th value of the Styles Enum is " + [OpenNETCF.Enum].GetName(GetType(Styles), 3))
		///		End Sub 'Main
		///		
		/// End Class 'GetNameTest</code>
		/// <code>[C#] 
		/// using System;
		/// 
		/// public class GetNameTest 
		/// {
		/// 	enum Colors { Red, Green, Blue, Yellow };
		/// 	enum Styles { Plaid, Striped, Tartan, Corduroy };
		/// 
		/// 	public static void Main() 
		/// 	{
		/// 		MessageBox.Show("The 4th value of the Colors Enum is " + OpenNETCF.Enum.GetName(typeof(Colors), 3));
		/// 		MessageBox.Show("The 4th value of the Styles Enum is " + OpenNETCF.Enum.GetName(typeof(Styles), 3));
		/// 	}
		/// }</code>
		/// </example>
		/// <seealso cref="M:System.Enum.GetName(System.Type,System.Object)">System.Enum.GetName Method</seealso>
		public static string GetName(Type enumType, object value)
		{
			//check that the type supplied inherits from System.Enum
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get details of all the public static fields (enum items)
				FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
				
				//cycle through the enum values
				foreach(FieldInfo thisField in fi)
				{
					object numericValue = 0;

					try
					{
						//convert the enum value to the numeric type supplied
						numericValue = Convert.ChangeType(thisField.GetValue(null), value.GetType(), null);
					}
					catch
					{
						throw new ArgumentException();
					}

					//if value matches return the name
					if(numericValue.Equals(value))
					{
						return thisField.Name;
					}
				}
				//if there is no match return null
				return null;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Get Names
		/// <summary>
		/// Retrieves an array of the names of the constants in a specified enumeration.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>A string array of the names of the constants in enumType. The elements of the array are sorted by the values of the enumerated constants.</returns>
		/// <exception cref="System.ArgumentException">enumType parameter is not an System.Enum</exception>
		/// <example>The follow example shows how to enumerate the members of the System.DayOfWeek enumeration by adding them to a ComboBox:-
		/// <code>[Visual Basic]
		/// Dim thisDOW As New DayOfWeek
		/// For Each thisDOW In OpenNETCF.Enum.GetValues(Type.GetType("System.DayOfWeek"))
		///		ComboBox1.Items.Add(thisDOW)
		/// Next</code>
		/// <code>[C#]
		/// foreach(DayOfWeek thisdow in OpenNETCF.Enum.GetValues(typeof(DayOfWeek)))
		/// {
		///		comboBox1.Items.Add(thisdow);
		/// }</code></example>
		/// <seealso cref="M:System.Enum.GetNames(System.Type)">System.Enum.GetNames Method</seealso>
		public static string[] GetNames(Type enumType)
		{
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get the public static fields (members of the enum)
				System.Reflection.FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
			
				//create a new enum array
				string[] names = new string[fi.Length];

				//populate with the values
				for(int iEnum = 0; iEnum < fi.Length; iEnum++)
				{
					names[iEnum] = fi[iEnum].Name;
				}

				//return the array
				return names;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Get Underlying Type
		/// <summary>
		/// Returns the underlying type of the specified enumeration.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>The underlying <see cref="System.Type"/> of <paramref>enumType</paramref>.</returns>
		/// <seealso cref="M:System.Enum.GetUnderlyingType(System.Type)">System.Enum.GetUnderlyingType Method</seealso>
		public static Type GetUnderlyingType(Type enumType)
		{
			return System.Enum.GetUnderlyingType(enumType);
		}
		#endregion

		#region Get Values
		/// <summary>
		/// Retrieves an array of the values of the constants in a specified enumeration.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>An System.Array of the values of the constants in enumType. The elements of the array are sorted by the values of the enumeration constants.</returns>
		/// <exception cref="System.ArgumentException">enumType parameter is not an System.Enum</exception>
		/// <seealso cref="M:System.Enum.GetValues(System.Type)">System.Enum.GetValues Method</seealso>
		public static System.Enum[] GetValues(Type enumType)
		{
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get the public static fields (members of the enum)
				System.Reflection.FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
			
				//create a new enum array
				System.Enum[] values = new System.Enum[fi.Length];

				//populate with the values
				for(int iEnum = 0; iEnum < fi.Length; iEnum++)
				{
					values[iEnum] = (System.Enum)fi[iEnum].GetValue(null);
				}

				//return the array
				return values;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Is Defined
		/// <summary>
		/// Returns an indication whether a constant with a specified value exists in a specified enumeration.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <param name="value">The value or name of a constant in enumType.</param>
		/// <returns><b>true</b> if a constant in <paramref>enumType</paramref> has a value equal to value; otherwise, <b>false</b>.</returns>
		/// <seealso cref="M:System.Enum.IsDefined(System.Type,System.Object)">System.Enum.IsDefined Method</seealso>
		public static bool IsDefined(Type enumType, object value)
		{
			return System.Enum.IsDefined(enumType, value);
		}
		#endregion

		#region Parse
		/// <summary>
		/// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
		/// </summary>
		/// <param name="enumType">The <see cref="T:System.Type"/> of the enumeration.</param>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <returns>An object of type enumType whose value is represented by value.</returns>
		public static object Parse(System.Type enumType, string value)
		{
			//do case sensitive parse
			return Parse(enumType, value, false);
		}
		/// <summary>
		/// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
		/// A parameter specifies whether the operation is case-sensitive.
		/// </summary>
		/// <param name="enumType">The <see cref="T:System.Type"/> of the enumeration.</param>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <param name="ignoreCase">If true, ignore case; otherwise, regard case.</param>
		/// <returns>An object of type enumType whose value is represented by value.</returns>
		/// <exception cref="System.ArgumentException">enumType is not an <see cref="T:System.Enum"/>.
		///  -or-  value is either an empty string ("") or only contains white space.
		///  -or-  value is a name, but not one of the named constants defined for the enumeration.</exception>
		///  <seealso cref="M:System.Enum.Parse(System.Type,System.String,System.Boolean)">System.Enum.Parse Method</seealso>
		public static object Parse(System.Type enumType, string value, bool ignoreCase)
		{
			//throw an exception on null value
			if(value.TrimEnd(' ')=="")
			{
				throw new ArgumentException("value is either an empty string (\"\") or only contains white space.");
			}
			else
			{
				//type must be a derivative of enum
				if(enumType.BaseType==Type.GetType("System.Enum"))
				{
					//remove all spaces
					string[] memberNames = value.Replace(" ","").Split(',');
					
					//collect the results
					//we are cheating and using a long regardless of the underlying type of the enum
					//this is so we can use ordinary operators to add up each value
					//I suspect there is a more efficient way of doing this - I will update the code if there is
					long returnVal = 0;

					//for each of the members, add numerical value to returnVal
					foreach(string thisMember in memberNames)
					{
						//skip this string segment if blank
						if(thisMember!="")
						{
							try
							{
								if(ignoreCase)
								{
									returnVal += (long)Convert.ChangeType(enumType.GetField(thisMember, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).GetValue(null),returnVal.GetType(), null);
								}
								else
								{
									returnVal += (long)Convert.ChangeType(enumType.GetField(thisMember, BindingFlags.Public | BindingFlags.Static).GetValue(null),returnVal.GetType(), null);
								}
							}
							catch
							{
								try
								{
									//try getting the numeric value supplied and converting it
									returnVal += (long)Convert.ChangeType(System.Enum.ToObject(enumType, Convert.ChangeType(thisMember, System.Enum.GetUnderlyingType(enumType), null)),typeof(long),null);
								}
								catch
								{
									throw new ArgumentException("value is a name, but not one of the named constants defined for the enumeration.");
								}
								//
							}
						}
					}


					//return the total converted back to the correct enum type
					return System.Enum.ToObject(enumType, returnVal);
				}
				else
				{
					//the type supplied does not derive from enum
					throw new ArgumentException("enumType parameter is not an System.Enum");
				}
			}
		}
		#endregion

		#region To Object
		/// <summary>
		/// Returns an instance of the specified enumeration set to the specified value.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration.</param>
		/// <param name="value">The value.</param>
		/// <returns>An enumeration object whose value is <paramref>value</paramref>.</returns>
		/// <seealso cref="System.Enum.ToObject(System.Type, System.Object)">System.Enum.ToObject Method</seealso>
		public static object ToObject(System.Type enumType, object value)
		{
			return System.Enum.ToObject(enumType, value);
		}
		#endregion
	}

	#region Obsolete Enum
	/// <summary>
	/// Obsolete. Provides helper functions for Enumerations.
	/// </summary>
	/// <remarks>Replaced with EnumEx to fit with naming schema.</remarks>
	/// <seealso cref="EnumEx"/>
	[Obsolete("Use OpenNETCF.EnumEx instead", true)]
	public sealed class Enum
	{
		private Enum(){}

		#region Get Name
		/// <summary>
		/// Retrieves the name of the constant in the specified enumeration that has the specified value.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
		/// <returns> A string containing the name of the enumerated constant in enumType whose value is value, or null if no such constant is found.</returns>
		/// <exception cref="System.ArgumentException"> enumType is not an System.Enum.  -or-  value is neither of type enumType nor does it have the same underlying type as enumType.</exception>
		/// <example>The following code sample illustrates the use of GetName (Based on the example provided with desktop .NET Framework):
		/// <code>[Visual Basic] 
		/// Imports System
		/// 
		///		Public Class GetNameTest
		/// 
		/// 		Enum Colors
		/// 			Red
		/// 			Green
		/// 			Blue
		/// 			Yellow
		/// 		End Enum 'Colors
		/// 
		///			Enum Styles
		/// 			Plaid
		/// 			Striped
		/// 			Tartan
		/// 			Corduroy
		/// 		End Enum 'Styles
		/// 
		///		Public Shared Sub Main() 
		/// 		MessageBox.Show("The 4th value of the Colors Enum is " + [OpenNETCF.Enum].GetName(GetType(Colors), 3))
		///			MessageBox.Show("The 4th value of the Styles Enum is " + [OpenNETCF.Enum].GetName(GetType(Styles), 3))
		///		End Sub 'Main
		///		
		/// End Class 'GetNameTest</code>
		/// <code>[C#] 
		/// using System;
		/// 
		/// public class GetNameTest 
		/// {
		/// 	enum Colors { Red, Green, Blue, Yellow };
		/// 	enum Styles { Plaid, Striped, Tartan, Corduroy };
		/// 
		/// 	public static void Main() 
		/// 	{
		/// 		MessageBox.Show("The 4th value of the Colors Enum is " + OpenNETCF.Enum.GetName(typeof(Colors), 3));
		/// 		MessageBox.Show("The 4th value of the Styles Enum is " + OpenNETCF.Enum.GetName(typeof(Styles), 3));
		/// 	}
		/// }</code>
		/// </example>
		/// <seealso cref="M:System.Enum.GetName(System.Type,System.Object)">System.Enum.GetName Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.GetName instead", true)]
		public static string GetName(Type enumType, object value)
		{
			//check that the type supplied inherits from System.Enum
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get details of all the public static fields (enum items)
				FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
				
				//cycle through the enum values
				foreach(FieldInfo thisField in fi)
				{
					object numericValue = 0;

					try
					{
						//convert the enum value to the numeric type supplied
						numericValue = Convert.ChangeType(thisField.GetValue(null), value.GetType(), null);
					}
					catch
					{
						throw new ArgumentException();
					}

					//if value matches return the name
					if(numericValue.Equals(value))
					{
						return thisField.Name;
					}
				}
				//if there is no match return null
				return null;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Get Names
		/// <summary>
		/// Retrieves an array of the names of the constants in a specified enumeration.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>A string array of the names of the constants in enumType. The elements of the array are sorted by the values of the enumerated constants.</returns>
		/// <exception cref="System.ArgumentException">enumType parameter is not an System.Enum</exception>
		/// <example>The follow example shows how to enumerate the members of the System.DayOfWeek enumeration by adding them to a ComboBox:-
		/// <code>[Visual Basic]
		/// Dim thisDOW As New DayOfWeek
		/// For Each thisDOW In OpenNETCF.Enum.GetValues(Type.GetType("System.DayOfWeek"))
		///		ComboBox1.Items.Add(thisDOW)
		/// Next</code>
		/// <code>[C#]
		/// foreach(DayOfWeek thisdow in OpenNETCF.Enum.GetValues(typeof(DayOfWeek)))
		/// {
		///		comboBox1.Items.Add(thisdow);
		/// }</code></example>
		/// <seealso cref="M:System.Enum.GetNames(System.Type)">System.Enum.GetNames Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.GetNames instead", true)]
		public static string[] GetNames(Type enumType)
		{
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get the public static fields (members of the enum)
				System.Reflection.FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
			
				//create a new enum array
				string[] names = new string[fi.Length];

				//populate with the values
				for(int iEnum = 0; iEnum < fi.Length; iEnum++)
				{
					names[iEnum] = fi[iEnum].Name;
				}

				//return the array
				return names;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Get Underlying Type
		/// <summary>
		/// Returns the underlying type of the specified enumeration.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>The underlying <see cref="System.Type"/> of <paramref>enumType</paramref>.</returns>
		/// <seealso cref="M:System.Enum.GetUnderlyingType(System.Type)">System.Enum.GetUnderlyingType Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.GetUnderlyingType instead", true)]
		public static Type GetUnderlyingType(Type enumType)
		{
			return System.Enum.GetUnderlyingType(enumType);
		}
		#endregion

		#region Get Values
		/// <summary>
		/// Retrieves an array of the values of the constants in a specified enumeration.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <returns>An System.Array of the values of the constants in enumType. The elements of the array are sorted by the values of the enumeration constants.</returns>
		/// <exception cref="System.ArgumentException">enumType parameter is not an System.Enum</exception>
		/// <seealso cref="M:System.Enum.GetValues(System.Type)">System.Enum.GetValues Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.GetValues instead", true)]
		public static System.Enum[] GetValues(Type enumType)
		{
			if(enumType.BaseType==Type.GetType("System.Enum"))
			{
				//get the public static fields (members of the enum)
				System.Reflection.FieldInfo[] fi = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
			
				//create a new enum array
				System.Enum[] values = new System.Enum[fi.Length];

				//populate with the values
				for(int iEnum = 0; iEnum < fi.Length; iEnum++)
				{
					values[iEnum] = (System.Enum)fi[iEnum].GetValue(null);
				}

				//return the array
				return values;
			}
			else
			{
				//the type supplied does not derive from enum
				throw new ArgumentException("enumType parameter is not an System.Enum");
			}
		}
		#endregion

		#region Is Defined
		/// <summary>
		/// Returns an indication whether a constant with a specified value exists in a specified enumeration.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <param name="value">The value or name of a constant in enumType.</param>
		/// <returns><b>true</b> if a constant in <paramref>enumType</paramref> has a value equal to value; otherwise, <b>false</b>.</returns>
		/// <seealso cref="M:System.Enum.IsDefined(System.Type,System.Object)">System.Enum.IsDefined Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.IsDefined instead", true)]
		public static bool IsDefined(Type enumType, object value)
		{
			return System.Enum.IsDefined(enumType, value);
		}
		#endregion

		#region Parse
		/// <summary>
		/// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
		/// </summary>
		/// <param name="enumType">The <see cref="T:System.Type"/> of the enumeration.</param>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <returns>An object of type enumType whose value is represented by value.</returns>
		[Obsolete("Use OpenNETCF.EnumEx.Parse instead", true)]
		public static object Parse(System.Type enumType, string value)
		{
			//do case sensitive parse
			return Parse(enumType, value, false);
		}
		/// <summary>
		/// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
		/// A parameter specifies whether the operation is case-sensitive.
		/// </summary>
		/// <param name="enumType">The <see cref="T:System.Type"/> of the enumeration.</param>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <param name="ignoreCase">If true, ignore case; otherwise, regard case.</param>
		/// <returns>An object of type enumType whose value is represented by value.</returns>
		/// <exception cref="System.ArgumentException">enumType is not an <see cref="T:System.Enum"/>.
		///  -or-  value is either an empty string ("") or only contains white space.
		///  -or-  value is a name, but not one of the named constants defined for the enumeration.</exception>
		///  <seealso cref="M:System.Enum.Parse(System.Type,System.String,System.Boolean)">System.Enum.Parse Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.Parse instead", true)]
		public static object Parse(System.Type enumType, string value, bool ignoreCase)
		{
			//throw an exception on null value
			if(value.TrimEnd(' ')=="")
			{
				throw new ArgumentException("value is either an empty string (\"\") or only contains white space.");
			}
			else
			{
				//type must be a derivative of enum
				if(enumType.BaseType==Type.GetType("System.Enum"))
				{
					//remove all spaces
					string[] memberNames = value.Replace(" ","").Split(',');
					
					//collect the results
					//we are cheating and using a long regardless of the underlying type of the enum
					//this is so we can use ordinary operators to add up each value
					//I suspect there is a more efficient way of doing this - I will update the code if there is
					long returnVal = 0;

					//for each of the members, add numerical value to returnVal
					foreach(string thisMember in memberNames)
					{
						//skip this string segment if blank
						if(thisMember!="")
						{
							try
							{
								if(ignoreCase)
								{
									returnVal += (long)Convert.ChangeType(enumType.GetField(thisMember, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).GetValue(null),returnVal.GetType(), null);
								}
								else
								{
									returnVal += (long)Convert.ChangeType(enumType.GetField(thisMember, BindingFlags.Public | BindingFlags.Static).GetValue(null),returnVal.GetType(), null);
								}
							}
							catch
							{
								try
								{
									//try getting the numeric value supplied and converting it
									returnVal += (long)Convert.ChangeType(System.Enum.ToObject(enumType, Convert.ChangeType(thisMember, System.Enum.GetUnderlyingType(enumType), null)),typeof(long),null);
								}
								catch
								{
									throw new ArgumentException("value is a name, but not one of the named constants defined for the enumeration.");
								}
								//
							}
						}
					}


					//return the total converted back to the correct enum type
					return System.Enum.ToObject(enumType, returnVal);
				}
				else
				{
					//the type supplied does not derive from enum
					throw new ArgumentException("enumType parameter is not an System.Enum");
				}
			}
		}
		#endregion

		#region To Object
		/// <summary>
		/// Returns an instance of the specified enumeration set to the specified value.
		/// <para><b>New in v1.1</b></para>
		/// </summary>
		/// <param name="enumType">An enumeration.</param>
		/// <param name="value">The value.</param>
		/// <returns>An enumeration object whose value is <paramref>value</paramref>.</returns>
		/// <seealso cref="System.Enum.ToObject(System.Type, System.Object)">System.Enum.ToObject Method</seealso>
		[Obsolete("Use OpenNETCF.EnumEx.ToObject instead", true)]
		public static object ToObject(System.Type enumType, object value)
		{
			return System.Enum.ToObject(enumType, value);
		}
		#endregion
	}
	#endregion
}
